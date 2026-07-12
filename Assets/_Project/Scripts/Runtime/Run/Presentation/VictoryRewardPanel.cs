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
        private RunState _resultRun;
        private bool _runWon;
        private Texture2D _background;
        private GUIStyle _panel;
        private GUIStyle _title;
        private GUIStyle _text;

        public void Show(BattleRewardResult reward, Action continueAction)
        {
            _reward = reward ?? throw new ArgumentNullException(nameof(reward));
            _resultRun = null;
            _continueAction = continueAction;
        }

        public void ShowRunResult(bool victory, RunState run, Action restartAction)
        {
            _reward = null;
            _runWon = victory;
            _resultRun = run ?? throw new ArgumentNullException(nameof(run));
            _continueAction = restartAction;
        }

        private void OnGUI()
        {
            if (_reward == null && _resultRun == null) return;
            EnsureStyles();
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _panel);
            const float width = 560f;
            const float height = 480f;
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height), GUIContent.none, _panel);
            if (_resultRun != null)
            {
                DrawRunResult();
                GUILayout.EndArea();
                return;
            }
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

        private void DrawRunResult()
        {
            GUILayout.Label(_runWon ? "RUN COMPLETE" : "RUN FAILED", _title);
            GUILayout.Space(18f);
            GUILayout.Label(_runWon
                ? "The test Hive Guardian has been destroyed."
                : "The Technopath was destroyed. The expedition is over.", _text);
            GUILayout.Space(16f);
            GUILayout.Label($"Battles completed: {_resultRun.CompletedBattles}", _title);
            GUILayout.Label($"Surviving robots: {_resultRun.Robots.Count}", _text);
            GUILayout.Label($"Parts collected: {_resultRun.Parts}", _text);
            GUILayout.Label($"Modules in inventory: {_resultRun.ModuleInventory.Count}", _text);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("START NEW TEST RUN", GUILayout.Height(42f)))
            {
                _resultRun = null;
                var action = _continueAction;
                _continueAction = null;
                action?.Invoke();
            }
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
