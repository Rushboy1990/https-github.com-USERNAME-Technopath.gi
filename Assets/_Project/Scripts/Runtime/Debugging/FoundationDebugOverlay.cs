using Technopath.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Technopath.Debugging
{
    [DisallowMultipleComponent]
    public sealed class FoundationDebugOverlay : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;

        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;

        private void OnGUI()
        {
            if (gameConfig != null && !gameConfig.ShowDebugOverlay)
                return;

            EnsureStyles();

            const float width = 310f;
            const float height = 116f;
            var rect = new Rect(16f, 16f, width, height);

            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 10f, width - 24f, height - 20f));
            GUILayout.Label("TECHNOPATH • FOUNDATION", _labelStyle);
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}", _labelStyle);
            GUILayout.Label("Phase: Iteration 0", _labelStyle);
            GUILayout.Label("Action Points: —", _labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
                return;

            _panelStyle = new GUIStyle(GUI.skin.box);
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }
    }
}
