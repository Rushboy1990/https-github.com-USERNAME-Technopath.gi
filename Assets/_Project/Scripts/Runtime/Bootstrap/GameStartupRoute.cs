using System;

namespace Technopath.Bootstrap
{
    public static class GameStartupRoute
    {
        public static bool RequiresSceneLoad(string activeSceneName, string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
                throw new ArgumentException("Target scene name is required.", nameof(targetSceneName));

            return !string.Equals(activeSceneName, targetSceneName, StringComparison.Ordinal);
        }
    }
}
