using System;
using System.Collections.Generic;
using Technopath.Combat.Board;
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
        [SerializeField] private int formationSeed = 1701;

        private readonly List<UnitTokenView> _spawnedUnits = new();
        private BattlefieldModel _model;

        public string SelectionDescription { get; private set; } = "None";

        private void Awake()
        {
            ValidateReferences();
            InitializeCells(playerCells, BoardSide.Player);
            InitializeCells(enemyCells, BoardSide.Enemy);
            BuildFormation();
        }

        public void Select(GridCellView cell)
        {
            if (cell == null)
                return;

            ClearHighlights();
            cell.ShowSelected();
            var state = _model.GetGrid(cell.Side)[cell.Position];
            SelectionDescription = state.IsEmpty
                ? $"{cell.Side} {cell.Position}: Empty"
                : $"{cell.Side} {cell.Position}: {state.OccupantId}";

            foreach (var neighbor in cell.Position.GetOrthogonalNeighbors())
                GetCell(cell.Side, neighbor).ShowValidNeighbor();
        }

        private void BuildFormation()
        {
            _model = StartingFormationFactory.Create(formationSeed);
            foreach (var unit in _spawnedUnits)
                Destroy(unit.gameObject);
            _spawnedUnits.Clear();

            foreach (var cell in _model.Player.Cells)
            {
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;

                var token = Instantiate(unitPrefab, GetCell(BoardSide.Player, cell.Position).transform.position,
                    Quaternion.identity, unitsRoot);
                token.Bind(cell.OccupantId, cell.OccupantId == StartingFormationFactory.TechnopathId);
                _spawnedUnits.Add(token);
            }
        }

        private GridCellView GetCell(BoardSide side, GridPosition position) =>
            (side == BoardSide.Player ? playerCells : enemyCells)[position.Index];

        private void ClearHighlights()
        {
            foreach (var cell in playerCells)
                cell.ShowNormal();
            foreach (var cell in enemyCells)
                cell.ShowNormal();
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
            if (unitPrefab == null || unitsRoot == null)
                throw new InvalidOperationException("BattlefieldPresenter prefab and units root references are required.");
        }
    }
}
