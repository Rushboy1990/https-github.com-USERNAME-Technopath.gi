using System;
using System.Collections;
using System.Collections.Generic;
using Technopath.Combat.Archetypes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Technopath.Run.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RestStopController : MonoBehaviour
    {
        [SerializeField] private CampPanel campPanel;
        [SerializeField] private string combatScenePath = "Assets/_Project/Scenes/CombatSandbox.unity";
        private IReadOnlyList<RunEncounter> _routeChoices;
        private bool _showRoute;
        private bool _loading;

        public void Initialize(RunState runState, RobotArchetypeDefinition[] archetypes)
        {
            if (campPanel == null) throw new InvalidOperationException("RestStopController requires CampPanel reference.");
            if (!RunSession.IsActive || RunSession.Flow.State != runState)
                throw new InvalidOperationException("Rest Stop requires the active run session.");
            campPanel.Show(runState, archetypes, ShowRoute);
        }

        private void ShowRoute()
        {
            _routeChoices = RunSession.Flow.CreateRouteChoices();
            _showRoute = true;
        }

        private void OnGUI()
        {
            if (!_showRoute || _routeChoices == null) return;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            const float width = 760f;
            const float height = 420f;
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height), GUI.skin.box);
            GUILayout.Label("SELECT NEXT ENCOUNTER", new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(18f);
            GUILayout.BeginHorizontal();
            foreach (var encounter in _routeChoices)
            {
                GUI.enabled = !_loading;
                var reward = $"Reward: {encounter.RewardModuleCount} modules, {encounter.RewardParts} parts";
                if (GUILayout.Button($"{encounter.DisplayName}\n\nType: {encounter.Kind}\nDifficulty: {encounter.Difficulty}\n{reward}\n\nStart battle", GUILayout.Height(280f)))
                {
                    RunSession.Flow.SelectEncounter(encounter);
                    StartCoroutine(OpenCombat());
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private IEnumerator OpenCombat()
        {
            if (_loading) yield break;
            _loading = true;
            var restScene = gameObject.scene;
            var load = SceneManager.LoadSceneAsync(combatScenePath, LoadSceneMode.Additive);
            if (load == null) throw new InvalidOperationException($"Could not load combat scene: {combatScenePath}");
            yield return load;
            var combatScene = SceneManager.GetSceneByPath(combatScenePath);
            SceneManager.SetActiveScene(combatScene);
            yield return SceneManager.UnloadSceneAsync(restScene);
        }

        private void OnValidate()
        {
            if (campPanel == null) campPanel = GetComponentInChildren<CampPanel>(true);
        }
    }
}
