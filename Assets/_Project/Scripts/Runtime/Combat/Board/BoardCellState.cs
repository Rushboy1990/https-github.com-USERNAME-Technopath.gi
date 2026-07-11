using System;

namespace Technopath.Combat.Board
{
    public sealed class BoardCellState
    {
        public BoardCellState(GridPosition position)
        {
            Position = position;
        }

        public GridPosition Position { get; }
        public CellOccupancyKind Occupancy { get; private set; }
        public string OccupantId { get; private set; }
        public bool IsEmpty => Occupancy == CellOccupancyKind.Empty;

        public void Occupy(CellOccupancyKind occupancy, string occupantId)
        {
            if (occupancy == CellOccupancyKind.Empty)
                throw new ArgumentException("Use Clear to make a cell empty.", nameof(occupancy));
            if (string.IsNullOrWhiteSpace(occupantId))
                throw new ArgumentException("An occupied cell requires an occupant ID.", nameof(occupantId));
            if (!IsEmpty)
                throw new InvalidOperationException($"Cell {Position} is already occupied by {OccupantId}.");

            Occupancy = occupancy;
            OccupantId = occupantId;
        }

        public void Clear()
        {
            Occupancy = CellOccupancyKind.Empty;
            OccupantId = null;
        }
    }
}
