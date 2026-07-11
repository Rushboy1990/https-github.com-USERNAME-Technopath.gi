using System;
using System.Collections.Generic;
using Technopath.Combat.Board;

namespace Technopath.Combat.Rules
{
    public sealed class PlayerTurnModel
    {
        public const int StartingActionPoints = 3;

        private readonly BattlefieldModel _battlefield;
        private readonly Dictionary<string, CombatUnitState> _units = new();
        private readonly HashSet<string> _independentlyActivated = new();

        public PlayerTurnModel(BattlefieldModel battlefield, int actionPoints = StartingActionPoints)
        {
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            ActionPoints = actionPoints;
            RegisterUnits(battlefield.Player, 10, 2);
            RegisterUnits(battlefield.Enemy, 6, 1);
        }

        public int ActionPoints { get; private set; }
        public bool IsFinished => ActionPoints == 0;

        public CombatUnitState GetUnit(string id) => _units[id];
        public bool IsAlive(string id) => _units.TryGetValue(id, out var unit) && unit.IsAlive;

        public bool ApplyDamage(string unitId, int damage)
        {
            var unit = _units[unitId];
            unit.TakeDamage(damage);
            if (unit.IsAlive)
                return false;

            _battlefield.GetGrid(unit.Side).RemoveUnit(unitId);
            return true;
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
                grid.MoveUnit(from, to);
            else
                grid.SwapUnits(from, to);

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

        public void FinishTurn() => ActionPoints = 0;

        public void BeginNewTurn(int actionPoints = StartingActionPoints)
        {
            if (!IsFinished)
                throw new InvalidOperationException("Current player turn must be finished first.");
            if (actionPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(actionPoints));

            ActionPoints = actionPoints;
            _independentlyActivated.Clear();
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
                ApplyDamage(target.Id, attacker.AttackDamage);
                return new AutoAttackResult(attackerId, target.Id, attacker.AttackDamage);
            }

            return new AutoAttackResult(attackerId, null, 0);
        }

        private void RegisterUnits(BattleGridModel grid, int health, int damage)
        {
            foreach (var cell in grid.Cells)
            {
                if (cell.Occupancy == CellOccupancyKind.Unit)
                    _units.Add(cell.OccupantId, new CombatUnitState(cell.OccupantId, grid.Side, health, damage));
            }
        }

        private static int ManhattanDistance(GridPosition first, GridPosition second) =>
            Math.Abs(first.Row - second.Row) + Math.Abs(first.Column - second.Column);
    }
}
