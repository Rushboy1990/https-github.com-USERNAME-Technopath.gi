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
    }
}
