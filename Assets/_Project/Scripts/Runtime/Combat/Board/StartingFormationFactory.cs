using System;
using System.Collections.Generic;

namespace Technopath.Combat.Board
{
    public static class StartingFormationFactory
    {
        public const string TechnopathId = "technopath";

        public static BattlefieldModel Create(int seed, int robotCount = 3)
        {
            if (robotCount < 0 || robotCount > 8)
                throw new ArgumentOutOfRangeException(nameof(robotCount));

            var battlefield = new BattlefieldModel();
            battlefield.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, TechnopathId);

            var available = new List<GridPosition>(8);
            for (var row = 0; row < GridPosition.Size; row++)
            for (var column = 0; column < GridPosition.Size; column++)
            {
                var position = new GridPosition(row, column);
                if (position != GridPosition.Center)
                    available.Add(position);
            }

            var random = new Random(seed);
            for (var index = 0; index < robotCount; index++)
            {
                var choice = random.Next(available.Count);
                battlefield.Player.TryOccupy(available[choice], CellOccupancyKind.Unit, $"robot-{index + 1}");
                available.RemoveAt(choice);
            }

            return battlefield;
        }
    }
}
