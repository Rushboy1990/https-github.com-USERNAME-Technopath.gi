using System;
using System.Collections.Generic;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Modules;
using Technopath.Combat.Statuses;

namespace Technopath.Combat.Rules
{
    public sealed class PlayerTurnModel
    {
        public const int StartingActionPoints = 3;

        private readonly BattlefieldModel _battlefield;
        private readonly Dictionary<string, CombatUnitState> _units = new();
        private readonly HashSet<string> _independentlyActivated = new();
        private readonly HashSet<string> _movedThisTurn = new();
        private readonly Dictionary<string, int> _movesThisTurn = new();
        private readonly IReadOnlyDictionary<string, RobotArchetypeDefinition> _archetypes;
        private readonly IReadOnlyDictionary<string, RobotLoadout> _loadouts;
        private readonly Random _random;

        public PlayerTurnModel(BattlefieldModel battlefield, int actionPoints = StartingActionPoints,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes = null)
            : this(battlefield, actionPoints, archetypes, null)
        {
        }

        public PlayerTurnModel(BattlefieldModel battlefield, int actionPoints,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes,
            IReadOnlyDictionary<string, RobotLoadout> loadouts,
            IReadOnlyDictionary<string, int> initialHealth = null,
            IReadOnlyDictionary<string, CombatUnitSetup> enemySetups = null,
            int randomSeed = 0,
            IReadOnlyDictionary<string, CombatUnitSetup> playerSetups = null)
        {
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            _archetypes = archetypes;
            _loadouts = loadouts;
            _random = new Random(randomSeed);
            ActionPoints = actionPoints;
            RegisterUnits(battlefield.Player, 10, 2, initialHealth, playerSetups);
            RegisterUnits(battlefield.Enemy, 6, 1, null, enemySetups);
            Events.Enqueue(new CombatEvent(CombatEventKind.PhaseStarted));
            ResolvePhaseVolleys();
        }

        public int ActionPoints { get; private set; }
        public bool IsFinished => ActionPoints == 0;
        public CombatEventQueue Events { get; } = new();
        public CombatStatusRuntime StatusRuntime { get; private set; }

        public void AttachStatusRuntime(CombatStatusRuntime statusRuntime) =>
            StatusRuntime = statusRuntime ?? throw new ArgumentNullException(nameof(statusRuntime));

        public CombatUnitState GetUnit(string id) => _units[id];
        public bool IsAlive(string id) => _units.TryGetValue(id, out var unit) && unit.IsAlive;
        public bool TryGetUnit(string id, out CombatUnitState unit) => _units.TryGetValue(id, out unit);
        public int AddShield(string unitId, int amount) => _units[unitId].AddShield(amount);
        public bool CanUnitAct(string unitId) => IsAlive(unitId) && (StatusRuntime == null || StatusRuntime.CanAct(unitId));

        public bool TrySpawnPlayerUnit(string unitId, RobotArchetypeDefinition archetype, GridPosition position)
        {
            if (archetype == null || _units.ContainsKey(unitId) || !_battlefield.Player.TryOccupy(position, CellOccupancyKind.Unit, unitId))
                return false;
            _units.Add(unitId, new CombatUnitState(unitId, BoardSide.Player, archetype.MaximumHealth,
                archetype.AutoAttackDamage, archetype.MaximumShield));
            return true;
        }

        public bool ApplyDamage(string unitId, int damage)
        {
            return ApplyDamageDetailed(unitId, damage).Killed;
        }

        public DamageResult ApplyDamageDetailed(string unitId, int damage, bool allowInterception = true)
        {
            if (allowInterception && TryFindDistributor(unitId, out var distributorId))
            {
                var interceptedDamage = damage / 2;
                if (interceptedDamage > 0)
                {
                    damage -= interceptedDamage;
                    ApplyDamageDetailed(distributorId, interceptedDamage, false);
                }
            }
            var unit = _units[unitId];
            var result = unit.TakeDamage(damage);
            Events.Enqueue(new CombatEvent(CombatEventKind.Damage, targetId: unitId, value: result.HealthDamage));
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
            if (IsFinished)
                return false;

            var source = _battlefield.Player[from];
            var destination = _battlefield.Player[to];
            if (source.Occupancy != CellOccupancyKind.Unit || !CanUnitAct(source.OccupantId))
                return false;
            var canMoveRemotely = GetAbilityKind(source.OccupantId) == RobotAbilityKind.RemoteMovement;
            if (!canMoveRemotely && ManhattanDistance(from, to) != 1)
                return false;
            return destination.Occupancy != CellOccupancyKind.TemporaryObject &&
                   !_independentlyActivated.Contains(source.OccupantId);
        }

        public MoveResult Move(GridPosition from, GridPosition to)
        {
            if (!CanMove(from, to))
                throw new InvalidOperationException("Requested player move is not valid.");

            var grid = _battlefield.Player;
            var initiatorId = grid[from].OccupantId;
            var displacedId = grid[to].Occupancy == CellOccupancyKind.Unit ? grid[to].OccupantId : null;
            var ability = GetAbilityKind(initiatorId);
            var attacks = new List<AutoAttackResult>(4);

            if (ability == RobotAbilityKind.DepartureStrike)
                attacks.Add(ResolveAutoAttack(initiatorId, from.Row, GetAbilityValue(initiatorId)));

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

            if (!(displacedId != null && ability == RobotAbilityKind.FreeSwap))
                ActionPoints--;
            _independentlyActivated.Add(initiatorId);
            _movedThisTurn.Add(initiatorId);
            StatusRuntime?.ResolveUnitMoved(initiatorId);
            if (displacedId != null)
            {
                _movedThisTurn.Add(displacedId);
                StatusRuntime?.ResolveUnitMoved(displacedId);
            }
            if (ability == RobotAbilityKind.MomentumBarrage)
                _movesThisTurn[initiatorId] = _movesThisTurn.TryGetValue(initiatorId, out var moves) ? moves + 1 : 1;

            if (ability == RobotAbilityKind.CoordinateStrike)
                attacks.Add(ResolveCoordinateAttack(initiatorId, to, GetAbilityValue(initiatorId)));
            attacks.Add(ResolveAutoAttack(initiatorId, to.Row));
            if (displacedId != null)
            {
                attacks.Add(ResolveAutoAttack(displacedId, from.Row));
                if (ability == RobotAbilityKind.SwapDoubleAttack)
                    attacks.Add(ResolveAutoAttack(displacedId, from.Row));
            }

            return new MoveResult(from, to, displacedId != null, attacks);
        }

        public void FinishTurn()
        {
            foreach (var entry in _movesThisTurn)
                for (var index = 0; index < entry.Value; index++)
                    ResolveRandomEnemyAttack(entry.Key, GetAbilityValue(entry.Key));
            StatusRuntime?.ResolveWaitingMovementEffects(BoardSide.Player, _movedThisTurn);
            StatusRuntime?.ResolveSideMoment(BoardSide.Player, StatusTickMoment.PhaseEnded);
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
            _movesThisTurn.Clear();
            _movedThisTurn.Clear();
            RestoreShields(BoardSide.Player);
            StatusRuntime?.ResolveSideMoment(BoardSide.Player, StatusTickMoment.PhaseStarted);
            Events.Enqueue(new CombatEvent(CombatEventKind.PhaseStarted));
            ResolvePhaseVolleys();
        }

        public void RestoreShields(BoardSide side)
        {
            foreach (var unit in _units.Values)
            {
                if (unit.Side == side && unit.IsAlive)
                    unit.RestoreShield();
            }
        }

        public void ResolveStatusPhaseStart(BoardSide side) =>
            StatusRuntime?.ResolveSideMoment(side, StatusTickMoment.PhaseStarted);

        public void ResolveStatusPhaseEnd(BoardSide side, ISet<string> movedUnitIds)
        {
            StatusRuntime?.ResolveWaitingMovementEffects(side, movedUnitIds);
            StatusRuntime?.ResolveSideMoment(side, StatusTickMoment.PhaseEnded);
        }

        public void NotifyUnitMoved(string unitId) => StatusRuntime?.ResolveUnitMoved(unitId);

        public AutoAttackResult ResolveDirectAttack(string attackerId, string targetId, int baseDamage, int row)
        {
            if (!CanUnitAct(attackerId))
                return new AutoAttackResult(attackerId, null, 0, row);

            var damage = StatusRuntime?.AddOutgoingAttackDamage(attackerId, baseDamage) ?? baseDamage;
            if (string.IsNullOrEmpty(targetId) || !IsAlive(targetId))
            {
                Events.Enqueue(new CombatEvent(CombatEventKind.Attack, attackerId));
                return new AutoAttackResult(attackerId, null, 0, row);
            }

            Events.Enqueue(new CombatEvent(CombatEventKind.Attack, attackerId, targetId, damage));
            if (StatusRuntime?.TryIgnoreAttack(targetId) == true)
                return new AutoAttackResult(attackerId, targetId, damage, row,
                    new DamageResult(damage, 0, 0, false));

            damage += StatusRuntime?.ConsumeBonusDamageTaken(targetId) ?? 0;
            return new AutoAttackResult(attackerId, targetId, damage, row, ApplyDamageDetailed(targetId, damage));
        }

        private AutoAttackResult ResolveAutoAttack(string attackerId, int row, int damageOverride = -1)
        {
            var attacker = _units[attackerId];
            var damage = damageOverride >= 0 ? damageOverride : attacker.AttackDamage;
            for (var column = 0; column < GridPosition.Size; column++)
            {
                var cell = _battlefield.Enemy[new GridPosition(row, column)];
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;

                return ResolveDirectAttack(attackerId, cell.OccupantId, damage, row);
            }

            return ResolveDirectAttack(attackerId, null, damage, row);
        }

        private AutoAttackResult ResolveCoordinateAttack(string attackerId, GridPosition position, int damage)
        {
            var cell = _battlefield.Enemy[position];
            if (cell.Occupancy != CellOccupancyKind.Unit)
                return new AutoAttackResult(attackerId, null, 0, position.Row);
            return ResolveDirectAttack(attackerId, cell.OccupantId, damage, position.Row);
        }

        private AutoAttackResult ResolveRandomEnemyAttack(string attackerId, int damage)
        {
            var targets = new List<string>();
            foreach (var cell in _battlefield.Enemy.Cells)
                if (cell.Occupancy == CellOccupancyKind.Unit && IsAlive(cell.OccupantId))
                    targets.Add(cell.OccupantId);

            if (targets.Count == 0)
            {
                return ResolveDirectAttack(attackerId, null, damage, FindUnitRow(attackerId));
            }

            var targetId = targets[_random.Next(targets.Count)];
            _battlefield.Enemy.TryFindUnit(targetId, out var targetPosition);
            return ResolveDirectAttack(attackerId, targetId, damage, targetPosition.Row);
        }

        private RobotAbilityKind GetAbilityKind(string unitId) =>
            !CanUnitAct(unitId) ? RobotAbilityKind.None :
            _loadouts != null && _loadouts.TryGetValue(unitId, out var loadout)
                ? loadout.PrimaryAbilityKind
                : _archetypes != null && _archetypes.TryGetValue(unitId, out var archetype)
                    ? archetype.AbilityKind
                    : RobotAbilityKind.None;

        private int GetAbilityValue(string unitId)
        {
            if (_loadouts != null && _loadouts.TryGetValue(unitId, out var loadout))
                return loadout.PrimaryAbilityEffectValue;
            return _archetypes != null && _archetypes.TryGetValue(unitId, out var archetype)
                ? archetype.EffectValue
                : 0;
        }

        private void ResolvePhaseVolleys()
        {
            foreach (var unitId in _units.Keys)
                if (_units[unitId].Side == BoardSide.Player && _units[unitId].IsAlive &&
                    GetAbilityKind(unitId) == RobotAbilityKind.PhaseVolley)
                    ResolveAutoAttack(unitId, FindUnitRow(unitId), GetAbilityValue(unitId));
        }

        private int FindUnitRow(string unitId) => _battlefield.Player.TryFindUnit(unitId, out var position) ? position.Row : 0;

        private bool TryFindDistributor(string targetId, out string distributorId)
        {
            distributorId = null;
            if (!_units.TryGetValue(targetId, out var target) || target.Side != BoardSide.Player ||
                !_battlefield.Player.TryFindUnit(targetId, out var targetPosition))
                return false;
            foreach (var neighbor in targetPosition.GetOrthogonalNeighbors())
            {
                var cell = _battlefield.Player[neighbor];
                if (cell.Occupancy == CellOccupancyKind.Unit && cell.OccupantId != targetId &&
                    GetAbilityKind(cell.OccupantId) == RobotAbilityKind.DamageInterception)
                {
                    distributorId = cell.OccupantId;
                    return true;
                }
            }
            return false;
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
                            setup.MaximumHealth, GetInitialHealth(cell.OccupantId, setup.MaximumHealth, initialHealth),
                            setup.AttackDamage, setup.MaximumShield));
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
