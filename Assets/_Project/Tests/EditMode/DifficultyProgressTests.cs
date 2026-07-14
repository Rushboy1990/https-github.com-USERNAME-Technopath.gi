using NUnit.Framework;
using Technopath.Run;

namespace Technopath.Tests.EditMode
{
    public sealed class DifficultyProgressTests
    {
        [Test]
        public void NewProgress_OnlyUnlocksFirstLevel()
        {
            var progress = new DifficultyProgress();

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
        }

        [Test]
        public void WinningHighestUnlockedLevel_UnlocksOnlyNextLevel()
        {
            var progress = new DifficultyProgress();

            Assert.That(progress.TryUnlockNextAfterVictory(1), Is.True);
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(progress.TryUnlockNextAfterVictory(1), Is.False);
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2));
        }

        [Test]
        public void FinalDifficulty_DoesNotUnlockPastLevelTwentyFive()
        {
            var progress = new DifficultyProgress();
            for (var level = 1; level < DifficultyProgress.LevelCount; level++)
                Assert.That(progress.TryUnlockNextAfterVictory(level), Is.True);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(DifficultyProgress.LevelCount));
            Assert.That(progress.TryUnlockNextAfterVictory(DifficultyProgress.LevelCount), Is.False);
        }
    }
}
