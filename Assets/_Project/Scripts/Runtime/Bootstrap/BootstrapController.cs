using Technopath.Configuration;
using UnityEngine;

namespace Technopath.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class BootstrapController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;

        private void Awake()
        {
            if (gameConfig == null)
            {
                Debug.LogError("Bootstrap requires a GameConfig reference.", this);
                return;
            }

            if (!gameConfig.IsValid(out var error))
            {
                Debug.LogError($"GameConfig is invalid: {error}", gameConfig);
                return;
            }

        }
    }
}
