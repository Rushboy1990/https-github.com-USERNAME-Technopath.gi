using Technopath.Combat.Presentation;
using Technopath.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Technopath.Debugging
{
    [DisallowMultipleComponent]
    public sealed class FoundationDebugOverlay : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private BattlefieldPresenter battlefieldPresenter;

        private GUIStyle _panelStyle;
        private GUIStyle _labelStyle;

        private void OnGUI()
        {
            if (gameConfig != null && !gameConfig.ShowDebugOverlay)
                return;

            EnsureStyles();

            const float width = 360f;
            const float height = 246f;
            var rect = new Rect(16f, 16f, width, height);

            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 10f, width - 24f, height - 20f));
            GUILayout.Label("TECHNOPATH • COMBAT SANDBOX", _labelStyle);
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}", _labelStyle);
            GUILayout.Label($"Round: {(battlefieldPresenter != null ? battlefieldPresenter.RoundNumber : 0)} • Phase: {(battlefieldPresenter != null ? battlefieldPresenter.PhaseDescription : "None")}", _labelStyle);
            GUILayout.Label($"Selection: {(battlefieldPresenter != null ? battlefieldPresenter.SelectionDescription : "None")}", _labelStyle);
            GUILayout.Label($"Action Points: {(battlefieldPresenter != null ? battlefieldPresenter.ActionPoints : 0)}", _labelStyle);
            GUILayout.Label($"Log: {(battlefieldPresenter != null ? battlefieldPresenter.BattleLog : "—")}", _labelStyle);
            GUILayout.Label($"Events: {(battlefieldPresenter != null ? battlefieldPresenter.DetailedCombatLog : "—")}", _labelStyle);
            if (battlefieldPresenter != null)
            {
                if (battlefieldPresenter.PhaseDescription == "PlayerTurn" && GUILayout.Button("Finish player phase"))
                    battlefieldPresenter.FinishTurn();
            }
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
