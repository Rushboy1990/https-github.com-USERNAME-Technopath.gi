namespace Technopath.Run
{
    public static class RunSession
    {
        public static RunFlowController Flow { get; private set; }
        public static DifficultyProgress DifficultyProgress { get; } = new DifficultyProgress();
        public static bool IsActive => Flow != null;

        public static RunFlowController StartNew(int seed, RunStartConfiguration startConfiguration = null)
        {
            Flow = new RunFlowController(new RunState(startConfiguration), seed);
            return Flow;
        }

        public static bool CompleteSuccessfulRun()
        {
            if (Flow == null)
                return false;

            return DifficultyProgress.TryUnlockNextAfterVictory(Flow.State.StartConfiguration.DifficultyLevel);
        }

        public static void Reset() => Flow = null;
    }
}
