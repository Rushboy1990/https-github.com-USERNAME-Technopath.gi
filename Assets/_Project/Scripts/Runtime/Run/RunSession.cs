namespace Technopath.Run
{
    public static class RunSession
    {
        public static RunFlowController Flow { get; private set; }
        public static bool IsActive => Flow != null;

        public static RunFlowController StartNew(int seed)
        {
            Flow = new RunFlowController(new RunState(), seed);
            return Flow;
        }

        public static void Reset() => Flow = null;
    }
}
