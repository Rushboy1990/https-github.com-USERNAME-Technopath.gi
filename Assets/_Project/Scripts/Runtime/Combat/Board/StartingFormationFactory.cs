using System;
using System.Collections.Generic;

namespace Technopath.Combat.Board
{
    public static class StartingFormationFactory
    {
        public const string TechnopathId = "technopath";

        public static BattlefieldModel Create(int seed, int robotCount = 3)
        {
            var ids = new List<string>(robotCount);
            for (var index = 0; index < robotCount; index++) ids.Add($"robot-{index + 1}");
            return Create(seed, ids);
        }

        public static BattlefieldModel Create(int seed, IReadOnlyList<string> robotIds)
        {
            if (robotIds == null) throw new ArgumentNullException(nameof(robotIds));
            if (robotIds.Count > 8) throw new ArgumentOutOfRangeException(nameof(robotIds));

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
            for (var index = 0; index < robotIds.Count; index++)
            {
                var choice = random.Next(available.Count);
                battlefield.Player.TryOccupy(available[choice], CellOccupancyKind.Unit, robotIds[index]);
                available.RemoveAt(choice);
            }


            battlefield.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant-1");
            battlefield.Enemy.TryOccupy(new GridPosition(1, 1), CellOccupancyKind.Unit, "mutant-2");
            battlefield.Enemy.TryOccupy(new GridPosition(2, 0), CellOccupancyKind.Unit, "mutant-3");

            return battlefield;
        }
    }
}
