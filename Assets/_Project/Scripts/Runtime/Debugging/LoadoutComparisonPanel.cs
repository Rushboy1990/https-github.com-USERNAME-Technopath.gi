using Technopath.Combat.Modules;
using Technopath.Configuration;
using UnityEngine;

namespace Technopath.Debugging
{
    [DisallowMultipleComponent]
    public sealed class LoadoutComparisonPanel : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private RobotLoadoutPresetDefinition leftPreset;
        [SerializeField] private RobotLoadoutPresetDefinition rightPreset;
        [SerializeField] private bool expanded;

        private Vector2 _scroll;
        private GUIStyle _panelStyle;
        private GUIStyle _textStyle;

        private void OnGUI()
        {
            if (gameConfig != null && !gameConfig.ShowDebugOverlay) return;
            EnsureStyles();
            const float width = 470f;
            var height = expanded ? Mathf.Min(430f, Screen.height - 300f) : 42f;
            var rect = new Rect(Screen.width - width - 16f, 16f, width, height);
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, rect.height - 14f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("LOADOUT COMPARISON", _textStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(expanded ? "Collapse" : "Compare builds", GUILayout.Width(125f))) expanded = !expanded;
            GUILayout.EndHorizontal();

            if (expanded && leftPreset != null && rightPreset != null)
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                DrawPreset(leftPreset);
                GUILayout.Space(8f);
                DrawPreset(rightPreset);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void DrawPreset(RobotLoadoutPresetDefinition preset)
        {
            var loadout = preset.BuildRuntimeLoadout();
            var stats = loadout.CalculateStats();
            var primary = loadout.GetPrimaryAbility();
            var utility = loadout.GetProcessorAbility();
            GUILayout.Label($"{preset.DisplayName} — {preset.Archetype.DisplayName}", _textStyle);
            GUILayout.Label($"HP {stats.Health}  ARM {stats.Armor}  ATK {stats.Attack}", _textStyle);
            GUILayout.Label($"Core: {loadout.Core?.DisplayName ?? "Empty"} | Primary: {primary.Name}", _textStyle);
            GUILayout.Label($"Processor: {loadout.Processor?.DisplayName ?? "Empty"} | Utility: {utility?.Name ?? "None"}", _textStyle);
            for (var index = 0; index < loadout.Modifiers.Count; index++)
                GUILayout.Label($"Modifier {index + 1}: {loadout.Modifiers[index]?.DisplayName ?? "Empty"}", _textStyle);
            GUILayout.Label($"Sources: {string.Join("; ", stats.Sources)}", _textStyle);
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;
            _panelStyle = new GUIStyle(GUI.skin.box);
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }
    }
}
