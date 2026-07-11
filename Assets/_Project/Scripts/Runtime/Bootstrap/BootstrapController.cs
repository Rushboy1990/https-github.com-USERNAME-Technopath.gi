using System.Collections;
using Technopath.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Technopath.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;

        private IEnumerator Start()
        {
            if (gameConfig == null)
            {
                Debug.LogError("Bootstrap requires a GameConfig reference.", this);
                yield break;
            }

            if (!gameConfig.IsValid(out var error))
            {
                Debug.LogError($"GameConfig is invalid: {error}", gameConfig);
                yield break;
            }

            var activeSceneName = SceneManager.GetActiveScene().name;
            var targetSceneName = gameConfig.CombatSandboxSceneName;

            if (!GameStartupRoute.RequiresSceneLoad(activeSceneName, targetSceneName))
                yield break;

            var operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            if (operation == null)
                Debug.LogError($"Failed to start loading scene '{targetSceneName}'.", this);
            else
                yield return operation;
        }
    }
}
