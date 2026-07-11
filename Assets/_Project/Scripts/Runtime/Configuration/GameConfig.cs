using UnityEngine;

namespace Technopath.Configuration
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Technopath/Configuration/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Startup")]
        [SerializeField] private string combatSandboxSceneName = "CombatSandbox";

        [Header("Debug")]
        [SerializeField] private bool showDebugOverlay = true;

        public string CombatSandboxSceneName => combatSandboxSceneName;
        public bool ShowDebugOverlay => showDebugOverlay;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(combatSandboxSceneName))
            {
                error = "Combat Sandbox scene name is required.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
