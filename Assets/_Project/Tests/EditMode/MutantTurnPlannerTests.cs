using System.Collections.Generic;
using NUnit.Framework;
using Technopath.Combat.Board;
using Technopath.Combat.Round;

namespace Technopath.Tests.EditMode
{
    public sealed class MutantTurnPlannerTests
    {
        [Test]
        public void Prepare_UsesTypePriorityAndPlansOnlyOrthogonalFreeStep()
        {
            var grid = new BattleGridModel(BoardSide.Enemy);
            grid.TryOccupy(new GridPosition(1, 1), CellOccupancyKind.Unit, "slow");
            grid.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "fast");
            grid.TryOccupy(new GridPosition(0, 1), CellOccupancyKind.TemporaryObject, "mine");
            var profiles = new List<MutantProfile>
            {
                new("slow", 20, 2),
                new("fast", 10, 1)
            };

            var intents = new MutantTurnPlanner().Prepare(grid, profiles, seed: 7);

            Assert.That(intents[0].MutantId, Is.EqualTo("fast"));
            Assert.That(intents[0].PlannedDestination, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(intents[1].PlannedDestination.HasValue, Is.True);
            var destination = intents[1].PlannedDestination.Value;
            Assert.That(System.Math.Abs(destination.Row - 1) + System.Math.Abs(destination.Column - 1), Is.EqualTo(1));
            Assert.That(grid[destination].IsEmpty, Is.True);
        }

        [Test]
        public void Prepare_IsDeterministicForSeedAndSkipsMissingMutants()
        {
            var grid = new BattleGridModel(BoardSide.Enemy);
            grid.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, "alive");
            var profiles = new List<MutantProfile>
            {
                new("dead", 1, 5),
                new("alive", 1, 2)
            };

            var first = new MutantTurnPlanner().Prepare(grid, profiles, 42);
            var second = new MutantTurnPlanner().Prepare(grid, profiles, 42);

            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(second[0].PlannedDestination, Is.EqualTo(first[0].PlannedDestination));
        }
    }
}
