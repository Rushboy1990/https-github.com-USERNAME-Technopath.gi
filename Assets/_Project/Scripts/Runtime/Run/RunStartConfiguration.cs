using System;

namespace Technopath.Run
{
    public enum StartingCrewId
    {
        Rustwalker,
        Scraphawk,
        Deepvault
    }

    public sealed class RunStartConfiguration
    {
        public RunStartConfiguration(StartingCrewId startingCrew, int difficultyLevel)
        {
            if (difficultyLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(difficultyLevel));

            StartingCrew = startingCrew;
            DifficultyLevel = difficultyLevel;
        }

        public StartingCrewId StartingCrew { get; }
        public int DifficultyLevel { get; }
    }
}
