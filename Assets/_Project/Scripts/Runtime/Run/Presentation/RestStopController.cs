using System;
using Technopath.Combat.Archetypes;
using UnityEngine;

namespace Technopath.Run.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RestStopController : MonoBehaviour
    {
        [SerializeField] private CampPanel campPanel;

        public void Initialize(RunState runState, RobotArchetypeDefinition[] archetypes, Action continueAction)
        {
            if (campPanel == null) throw new InvalidOperationException("RestStopController requires CampPanel reference.");
            campPanel.Show(runState, archetypes, continueAction);
        }

        private void OnValidate()
        {
            if (campPanel == null) campPanel = GetComponentInChildren<CampPanel>(true);
        }
    }
}
