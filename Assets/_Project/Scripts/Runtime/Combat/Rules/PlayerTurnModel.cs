using System;
using System.Collections.Generic;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Modules;

namespace Technopath.Combat.Rules
{
    public sealed class PlayerTurnModel
    {
        public const int StartingActionPoints = 3;

        private readonly BattlefieldModel _battlefield;
        private readonly Dictionary<string, CombatUnitState> _units = new();
        private readonly HashSet<string> _independentlyActivated = new();
        private readonly IReadOnlyDictionary<string, RobotArchetypeDefinition> _archetypes;
        private readonly IReadOnlyDictionary<string, RobotLoadout> _loadouts;

        public PlayerTurnModel(BattlefieldModel battlefield, int actionPoints = StartingActionPoints,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes = null)
            : this(battlefield, actionPoints, archetypes, null)
        {
        }

        public PlayerTurnModel(BattlefieldModel battlefield, int actionPoints,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes,
            IReadOnlyDictionary<string, RobotLoadout> loadouts,
            IReadOnlyDictionary<string, int> initialHealth = null,
            IReadOnlyDictionary<string, CombatUnitSetup> enemySetups = null)
        {
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            _archetypes = archetypes;
            _loadouts = loadouts;
            ActionPoints = actionPoints;
            RegisterUnits(battlefield.Player, 10, 2, initialHealth, null);
            RegisterUnits(battlefield.Enemy, 6, 1, null, enemySetups);
            Events.Enqueue(new CombatEvent(CombatEventKind.PhaseStarted));
        }

        public int ActionPoints { get; private set; }
        public bool IsFinished => ActionPoints == 0;
        public CombatEventQueue Events { get; } = new();

        public CombatUnitState GetUnit(string id) => _units[id];
        public bool IsAlive(string id) => _units.TryGetValue(id, out var unit) && unit.IsAlive;
        public bool TryGetUnit(string id, out CombatUnitState unit) => _units.TryGetValue(id, out unit);
        public int AddShield(string unitId, int amount) => _units[unitId].AddShield(amount);

        public bool ApplyDamage(string unitId, int damage)
        {
            return ApplyDamageDetailed(unitId, damage).Killed;
        }

        public DamageResult ApplyDamageDetailed(string unitId, int damage)
        {
            var unit = _units[unitId];
            var result = unit.TakeDamage(damage);
            Events.Enqueue(new CombatEvent(CombatEventKind.Damage, targetId: unitId, value: damage));
            if (result.Killed)
            {
                _battlefield.GetGrid(unit.Side).RemoveUnit(unitId);
                Events.Enqueue(new CombatEvent(CombatEventKind.Kill, targetId: unitId));
                Events.Enqueue(new CombatEvent(CombatEventKind.Destroyed, targetId: unitId));
            }
            return result;
        }

        public bool CanMove(GridPosition from, GridPosition to)
        {
            if (IsFinished || ManhattanDistance(from, to) != 1)
                return false;

            var source = _battlefield.Player[from];
            var destination = _battlefield.Player[to];
            return source.Occupancy == CellOccupancyKind.Unit &&
                   destination.Occupancy != CellOccupancyKind.TemporaryObject &&
                   !_independentlyActivated.Contains(source.OccupantId);
        }

        public MoveResult Move(GridPosition from, GridPosition to)
        {
            if (!CanMove(from, to))
                throw new InvalidOperationException("Requested player move is not valid.");

            var grid = _battlefield.Player;
            var initiatorId = grid[from].OccupantId;
            var displacedId = grid[to].Occupancy == CellOccupancyKind.Unit ? grid[to].OccupantId : null;

            if (displacedId == null)
            {
                grid.MoveUnit(from, to);
                Events.Enqueue(new CombatEvent(CombatEventKind.Movement, initiatorId));
            }
            else
            {
                grid.SwapUnits(from, to);
                Events.Enqueue(new CombatEvent(CombatEventKind.Swap, initiatorId, displacedId));
            }

            ActionPoints--;
            _independentlyActivated.Add(initiatorId);

            var attacks = new List<AutoAttackResult>(2)
            {
                ResolveAutoAttack(initiatorId, to.Row)
            };
            if (displacedId != null)
                attacks.Add(ResolveAutoAttack(displacedId, from.Row));

            return new MoveResult(from, to, displacedId != null, attacks);
        }

        public void FinishTurn()
        {
            ActionPoints = 0;
            Events.Enqueue(new CombatEvent(CombatEventKind.PhaseEnded));
        }

        public void BeginNewTurn(int actionPoints = StartingActionPoints)
        {
            if (!IsFinished)
                throw new InvalidOperationException("Current player turn must be finished first.");
            if (actionPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(actionPoints));

            ActionPoints = actionPoints;
            _independentlyActivated.Clear();
            RestoreShields(BoardSide.Player);
            Events.Enqueue(new CombatEvent(CombatEventKind.PhaseStarted));
        }

        public void RestoreShields(BoardSide side)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.Side == side && unit.IsAlive)
                    unit.RestoreShield();
            }
        }

        private AutoAttackResult ResolveAutoAttack(string attackerId, int row)
        {
            var attacker = _units[attackerId];
            for (var column = 0; column < GridPosition.Size; column++)
            {
                var cell = _battlefield.Enemy[new GridPosition(row, column)];
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;

                var target = _units[cell.OccupantId];
                Events.Enqueue(new CombatEvent(CombatEventKind.Attack, attackerId, target.Id, attacker.AttackDamage));
                var damageResult = ApplyDamageDetailed(target.Id, attacker.AttackDamage);
                return new AutoAttackResult(attackerId, target.Id, attacker.AttackDamage, row, damageResult);
            }

            Events.Enqueue(new CombatEvent(CombatEventKind.Attack, attackerId));
            return new AutoAttackResult(attackerId, null, 0, row);
        }

        private void RegisterUnits(BattleGridModel grid, int health, int damage, IReadOnlyDictionary<string, int> initialHealth,
            IReadOnlyDictionary<string, CombatUnitSetup> setups)
        {
            foreach (var cell in grid.Cells)
            {
                if (cell.Occupancy == CellOccupancyKind.Unit)
                {
                    if (setups != null && setups.TryGetValue(cell.OccupantId, out var setup))
                    {
                        _units.Add(cell.OccupantId, new CombatUnitState(cell.OccupantId, grid.Side,
                            setup.MaximumHealth, setup.AttackDamage, setup.MaximumShield));
                        continue;
                    }
                    if (_loadouts != null && _loadouts.TryGetValue(cell.OccupantId, out var loadout))
                    {
                        var stats = loadout.CalculateStats();
                        _units.Add(cell.OccupantId, new CombatUnitState(cell.OccupantId, grid.Side,
                            stats.Health, GetInitialHealth(cell.OccupantId, stats.Health, initialHealth), stats.Attack, stats.Shield));
                        continue;
                    }
                    if (_archetypes != null && _archetypes.TryGetValue(cell.OccupantId, out var archetype))
                    {
                        _units.Add(cell.OccupantId, new CombatUnitState(cell.OccupantId, grid.Side,
                            archetype.MaximumHealth, GetInitialHealth(cell.OccupantId, archetype.MaximumHealth, initialHealth),
                            archetype.AutoAttackDamage, archetype.MaximumShield));
                        continue;
                    }
                    var shield = grid.Side == BoardSide.Player ? 3 : 2;
                    _units.Add(cell.OccupantId, new CombatUnitState(cell.OccupantId, grid.Side, health,
                        GetInitialHealth(cell.OccupantId, health, initialHealth), damage, shield));
                }
            }
        }

        private static int GetInitialHealth(string id, int maximumHealth, IReadOnlyDictionary<string, int> initialHealth) =>
            initialHealth != null && initialHealth.TryGetValue(id, out var value)
                ? Math.Clamp(value, 0, maximumHealth)
                : maximumHealth;

        private static int ManhattanDistance(GridPosition first, GridPosition second) =>
            Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column);
    }
}
