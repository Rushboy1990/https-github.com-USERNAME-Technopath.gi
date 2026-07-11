using System.Collections.Generic;

namespace Technopath.Combat.Board
{
    public sealed class BattleGridModel
    {
        private readonly BoardCellState[] _cells = new BoardCellState[GridPosition.Size * GridPosition.Size];

        public BattleGridModel(BoardSide side)
        {
            Side = side;
            for (var row = 0; row < GridPosition.Size; row++)
            for (var column = 0; column < GridPosition.Size; column++)
            {
                var position = new GridPosition(row, column);
                _cells[position.Index] = new BoardCellState(position);
            }
        }

        public BoardSide Side { get; }
        public IReadOnlyList<BoardCellState> Cells => _cells;
        public BoardCellState this[GridPosition position] => _cells[position.Index];

        public bool TryOccupy(GridPosition position, CellOccupancyKind occupancy, string occupantId)
        {
            var cell = this[position];
            if (!cell.IsEmpty)
                return false;

            cell.Occupy(occupancy, occupantId);
            return true;
        }

        public void MoveUnit(GridPosition from, GridPosition to)
        {
            var source = this[from];
            var destination = this[to];
            if (source.Occupancy != CellOccupancyKind.Unit || !destination.IsEmpty)
                throw new System.InvalidOperationException("Move requires a unit source and an empty destination.");

            var occupantId = source.TakeOccupant();
            destination.Occupy(CellOccupancyKind.Unit, occupantId);
        }

        public void SwapUnits(GridPosition first, GridPosition second)
        {
            var firstCell = this[first];
            var secondCell = this[second];
            if (firstCell.Occupancy != CellOccupancyKind.Unit || secondCell.Occupancy != CellOccupancyKind.Unit)
                throw new System.InvalidOperationException("Swap requires units in both cells.");

            var firstId = firstCell.TakeOccupant();
            var secondId = secondCell.TakeOccupant();
            firstCell.Occupy(CellOccupancyKind.Unit, secondId);
            secondCell.Occupy(CellOccupancyKind.Unit, firstId);
        }
    }
}
