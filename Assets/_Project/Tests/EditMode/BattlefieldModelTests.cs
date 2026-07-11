using System.Linq;
using NUnit.Framework;
using Technopath.Combat.Board;

namespace Technopath.Tests.EditMode
{
    public sealed class BattlefieldModelTests
    {
        [TestCase(0, 0, 2)]
        [TestCase(0, 1, 3)]
        [TestCase(1, 1, 4)]
        public void OrthogonalNeighbors_AreCorrect(int row, int column, int expectedCount)
        {
            var neighbors = new GridPosition(row, column).GetOrthogonalNeighbors();

            Assert.That(neighbors, Has.Count.EqualTo(expectedCount));
            Assert.That(neighbors.All(candidate =>
                System.Math.Abs(candidate.Row - row) + System.Math.Abs(candidate.Column - column) == 1), Is.True);
        }

        [Test]
        public void StartingFormation_KeepsTechnopathInCenterAndUsesUniqueCells()
        {
            var battlefield = StartingFormationFactory.Create(seed: 42);
            var occupied = battlefield.Player.Cells.Where(cell => !cell.IsEmpty).ToArray();

            Assert.That(occupied, Has.Length.EqualTo(4));
            Assert.That(battlefield.Player[GridPosition.Center].OccupantId,
                Is.EqualTo(StartingFormationFactory.TechnopathId));
            Assert.That(occupied.Select(cell => cell.Position).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void StartingFormation_IsDeterministicForSeed()
        {
            var first = StartingFormationFactory.Create(1701).Player.Cells.Select(cell => cell.OccupantId).ToArray();
            var second = StartingFormationFactory.Create(1701).Player.Cells.Select(cell => cell.OccupantId).ToArray();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Grid_DoesNotOverwriteOccupiedCell()
        {
            var grid = new BattleGridModel(BoardSide.Player);
            var position = new GridPosition(0, 0);

            Assert.That(grid.TryOccupy(position, CellOccupancyKind.Unit, "robot-1"), Is.True);
            Assert.That(grid.TryOccupy(position, CellOccupancyKind.TemporaryObject, "mine-1"), Is.False);
            Assert.That(grid[position].OccupantId, Is.EqualTo("robot-1"));
        }
    }
}
