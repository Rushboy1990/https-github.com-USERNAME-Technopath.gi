using System;
using Technopath.Run.Rewards;
using UnityEngine;

namespace Technopath.Run.Presentation
{
    [DisallowMultipleComponent]
    public sealed class VictoryRewardPanel : MonoBehaviour
    {
        private BattleRewardResult _reward;
        private Action _continueAction;
        private Texture2D _background;
        private GUIStyle _panel;
        private GUIStyle _title;
        private GUIStyle _text;

        public void Show(BattleRewardResult reward, Action continueAction)
        {
            _reward = reward ?? throw new ArgumentNullException(nameof(reward));
            _continueAction = continueAction;
        }

        private void OnGUI()
        {
            if (_reward == null) return;
            EnsureStyles();
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _panel);
            const float width = 560f;
            const float height = 480f;
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height), GUIContent.none, _panel);
            GUILayout.Label("VICTORY", _title);
            GUILayout.Label("The mutant group has been destroyed. Salvage secured:", _text);
            GUILayout.Space(18f);
            GUILayout.Label($"Parts: +{_reward.Parts}", _title);
            GUILayout.Space(12f);
            GUILayout.Label("Recovered modules", _title);
            foreach (var module in _reward.Modules)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{module.DisplayName} — {module.Rarity}, level {module.Level}", _title);
                GUILayout.Label(module.RulesText, _text);
                GUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Continue to camp", GUILayout.Height(42f)))
            {
                _reward = null;
                var action = _continueAction;
                _continueAction = null;
                action?.Invoke();
            }
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_panel != null) return;
            _background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _background.SetPixel(0, 0, new Color(0.015f, 0.02f, 0.025f, 1f));
            _background.Apply();
            _panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 18, 18) };
            _panel.normal.background = _background;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            _text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        }

        private void OnDestroy()
        {
            if (_background != null) Destroy(_background);
        }
    }
}
