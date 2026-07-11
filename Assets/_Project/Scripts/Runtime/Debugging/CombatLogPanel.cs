using Technopath.Combat.Presentation;
using Technopath.Configuration;
using UnityEngine;

namespace Technopath.Debugging
{
    [DisallowMultipleComponent]
    public sealed class CombatLogPanel : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private BattlefieldPresenter battlefieldPresenter;
        [SerializeField] private bool expanded = true;

        private Vector2 _scrollPosition;
        private int _lastEntryCount;
        private GUIStyle _panelStyle;
        private GUIStyle _entryStyle;

        private void OnGUI()
        {
            if (gameConfig != null && !gameConfig.ShowDebugOverlay) return;
            EnsureStyles();
            const float margin = 16f;
            var height = expanded ? Mathf.Min(250f, Screen.height * 0.34f) : 42f;
            var rect = new Rect(margin, Screen.height - height - margin, Screen.width - margin * 2f, height);
            GUI.Box(rect, GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, rect.height - 14f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"COMBAT LOG ({battlefieldPresenter?.CombatLogEntries.Count ?? 0})", _entryStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(expanded ? "Collapse ▼" : "Expand ▲", GUILayout.Width(110f))) expanded = !expanded;
            GUILayout.EndHorizontal();

            if (expanded && battlefieldPresenter != null)
            {
                var entries = battlefieldPresenter.CombatLogEntries;
                if (entries.Count != _lastEntryCount)
                {
                    _lastEntryCount = entries.Count;
                    _scrollPosition.y = float.MaxValue;
                }
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
                for (var index = 0; index < entries.Count; index++)
                    GUILayout.Label($"{index + 1:000}. {entries[index]}", _entryStyle);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;
            _panelStyle = new GUIStyle(GUI.skin.box);
            _entryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
    }
}
