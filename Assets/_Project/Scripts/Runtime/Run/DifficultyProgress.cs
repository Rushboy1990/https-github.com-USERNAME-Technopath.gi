using System;

namespace Technopath.Run
{
    public sealed class DifficultyProgress
    {
        public const int LevelCount = 25;

        public int HighestUnlockedLevel { get; private set; } = 1;

        public bool IsUnlocked(int level) => level >= 1 && level <= HighestUnlockedLevel;

        public bool TryUnlockNextAfterVictory(int completedLevel)
        {
            if (completedLevel != HighestUnlockedLevel || HighestUnlockedLevel >= LevelCount)
                return false;

            HighestUnlockedLevel++;
            return true;
        }
    }
}
