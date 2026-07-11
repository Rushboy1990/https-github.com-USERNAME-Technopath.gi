using System;
using System.Collections.Generic;

namespace Technopath.Combat.Board
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public const int Size = 3;

        public GridPosition(int row, int column)
        {
            if (!IsInside(row, column))
                throw new ArgumentOutOfRangeException(nameof(row), $"Position ({row}, {column}) is outside a {Size}x{Size} grid.");

            Row = row;
            Column = column;
        }

        public int Row { get; }
        public int Column { get; }
        public int Index => Row * Size + Column;

        public static GridPosition Center => new(1, 1);

        public static bool IsInside(int row, int column) =>
            row >= 0 && row < Size && column >= 0 && column < Size;

        public IReadOnlyList<GridPosition> GetOrthogonalNeighbors()
        {
            var result = new List<GridPosition>(4);
            AddIfInside(result, Row - 1, Column);
            AddIfInside(result, Row, Column + 1);
            AddIfInside(result, Row + 1, Column);
            AddIfInside(result, Row, Column - 1);
            return result;
        }

        public bool Equals(GridPosition other) => Row == other.Row && Column == other.Column;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Row, Column);
        public override string ToString() => $"({Row}, {Column})";

        public static bool operator ==(GridPosition left, GridPosition right) => left.Equals(right);
        public static bool operator !=(GridPosition left, GridPosition right) => !left.Equals(right);

        private static void AddIfInside(ICollection<GridPosition> result, int row, int column)
        {
            if (IsInside(row, column))
                result.Add(new GridPosition(row, column));
        }
    }
}
