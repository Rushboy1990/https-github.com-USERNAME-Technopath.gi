using System;
using Technopath.Combat.Archetypes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CombatDebugSpawnMenu : MonoBehaviour
    {
        [SerializeField] private BattlefieldPresenter presenter;
        [SerializeField] private RobotArchetypeDefinition[] robots = Array.Empty<RobotArchetypeDefinition>();
        private bool _open;

        public void Initialize(BattlefieldPresenter battlePresenter, RobotArchetypeDefinition[] definitions)
        {
            presenter = battlePresenter;
            robots = definitions ?? Array.Empty<RobotArchetypeDefinition>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) _open = !_open;
        }

        private void OnGUI()
        {
            if (!_open || presenter == null) return;
            GUILayout.BeginArea(new Rect(16, 96, 240, 440), GUI.skin.box);
            GUILayout.Label("DEBUG: SPAWN ROBOT (F2)");
            foreach (var robot in robots)
                if (robot != null && GUILayout.Button(robot.DisplayName)) presenter.TryDebugSpawn(robot);
            GUILayout.EndArea();
        }
    }
}
