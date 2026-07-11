using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattlefieldPresenter : MonoBehaviour
    {
        [SerializeField] private GridCellView[] playerCells = Array.Empty<GridCellView>();
        [SerializeField] private GridCellView[] enemyCells = Array.Empty<GridCellView>();
        [SerializeField] private UnitTokenView unitPrefab;
        [SerializeField] private Transform unitsRoot;
        [SerializeField] private AttackTraceView attackTracePrefab;
        [SerializeField] private int formationSeed = 1701;

        private readonly Dictionary<string, UnitTokenView> _unitViews = new();
        private BattlefieldModel _battlefield;
        private PlayerTurnModel _turn;
        private GridCellView _selectedSource;
        private bool _isAnimating;

        public string SelectionDescription { get; private set; } = "None";
        public string BattleLog { get; private set; } = "Select a player unit, then an adjacent cell.";
        public int ActionPoints => _turn?.ActionPoints ?? 0;

        private void Awake()
        {
            ValidateReferences();
            InitializeCells(playerCells, BoardSide.Player);
            InitializeCells(enemyCells, BoardSide.Enemy);
            BuildCombat();
        }

        public void Select(GridCellView cell)
        {
            if (cell == null || _isAnimating || _turn.IsFinished)
                return;

            if (_selectedSource != null && cell.Side == BoardSide.Player &&
                _turn.CanMove(_selectedSource.Position, cell.Position))
            {
                StartCoroutine(PerformMove(_selectedSource, cell));
                return;
            }

            ClearHighlights();
            _selectedSource = null;
            var state = _battlefield.GetGrid(cell.Side)[cell.Position];
            SelectionDescription = state.IsEmpty
                ? $"{cell.Side} {cell.Position}: Empty"
                : DescribeUnit(cell.Side, cell.Position, state.OccupantId);

            cell.ShowSelected();
            if (cell.Side != BoardSide.Player || state.Occupancy != CellOccupancyKind.Unit)
                return;

            _selectedSource = cell;
            foreach (var neighbor in cell.Position.GetOrthogonalNeighbors())
            {
                if (_turn.CanMove(cell.Position, neighbor))
                    GetCell(BoardSide.Player, neighbor).ShowValidNeighbor();
            }
        }

        public void FinishTurn()
        {
            if (_turn == null || _isAnimating)
                return;
            _turn.FinishTurn();
            _selectedSource = null;
            ClearHighlights();
            BattleLog = "Player phase finished. Remaining action points burned.";
        }

        public void BeginNewPlayerTurn()
        {
            if (_turn == null || !_turn.IsFinished || _isAnimating)
                return;
            _turn.BeginNewTurn();
            BattleLog = "New player phase started.";
        }

        private IEnumerator PerformMove(GridCellView source, GridCellView destination)
        {
            _isAnimating = true;
            var initiatorId = _battlefield.Player[source.Position].OccupantId;
            var displacedId = _battlefield.Player[destination.Position].OccupantId;
            var result = _turn.Move(source.Position, destination.Position);

            StartCoroutine(_unitViews[initiatorId].MoveTo(destination.transform.position, result.WasSwap));
            if (result.WasSwap)
                StartCoroutine(_unitViews[displacedId].MoveTo(source.transform.position, false));
            yield return new WaitForSeconds(0.32f);

            var log = new StringBuilder();
            log.Append(result.WasSwap ? "Swap. " : "Move. ");
            foreach (var attack in result.Attacks)
            {
                ShowAttackFeedback(attack, destination.Position.Row);
                if (attack.HasTarget)
                    log.Append($"{attack.AttackerId} dealt {attack.Damage} to {attack.TargetId}. ");
                else
                    log.Append($"{attack.AttackerId} fired into empty row. ");
            }
            BattleLog = log.ToString();
            SelectionDescription = "None";
            _selectedSource = null;
            ClearHighlights();
            _isAnimating = false;
        }

        private void BuildCombat()
        {
            _battlefield = StartingFormationFactory.Create(formationSeed);
            _turn = new PlayerTurnModel(_battlefield);
            SpawnGridUnits(_battlefield.Player);
            SpawnGridUnits(_battlefield.Enemy);
        }

        private void SpawnGridUnits(BattleGridModel grid)
        {
            foreach (var cell in grid.Cells)
            {
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;
                var token = Instantiate(unitPrefab, GetCell(grid.Side, cell.Position).transform.position,
                    Quaternion.identity, unitsRoot);
                token.Bind(cell.OccupantId, grid.Side, cell.OccupantId == StartingFormationFactory.TechnopathId);
                _unitViews.Add(cell.OccupantId, token);
            }
        }

        private void ShowAttackFeedback(AutoAttackResult attack, int row)
        {
            if (attackTracePrefab == null || !_unitViews.TryGetValue(attack.AttackerId, out var attacker))
                return;

            var destination = attack.HasTarget && _unitViews.TryGetValue(attack.TargetId, out var target)
                ? target.transform.position
                : GetCell(BoardSide.Enemy, new GridPosition(row, 2)).transform.position + Vector3.right;
            Instantiate(attackTracePrefab, unitsRoot).Play(attacker.transform.position, destination, attack.Damage);
        }

        private string DescribeUnit(BoardSide side, GridPosition position, string unitId)
        {
            var unit = _turn.GetUnit(unitId);
            return $"{side} {position}: {unitId}, HP {unit.Health}, ATK {unit.AttackDamage}";
        }

        private GridCellView GetCell(BoardSide side, GridPosition position) =>
            (side == BoardSide.Player ? playerCells : enemyCells)[position.Index];

        private void ClearHighlights()
        {
            foreach (var cell in playerCells) cell.ShowNormal();
            foreach (var cell in enemyCells) cell.ShowNormal();
        }

        private static void InitializeCells(IReadOnlyList<GridCellView> cells, BoardSide side)
        {
            for (var index = 0; index < cells.Count; index++)
                cells[index].Initialize(side, new GridPosition(index / GridPosition.Size, index % GridPosition.Size));
        }

        private void ValidateReferences()
        {
            if (playerCells.Length != 9 || enemyCells.Length != 9)
                throw new InvalidOperationException("BattlefieldPresenter requires exactly nine cells for each side.");
            if (unitPrefab == null || unitsRoot == null || attackTracePrefab == null)
                throw new InvalidOperationException("BattlefieldPresenter prefab and units root references are required.");
        }
    }
}
