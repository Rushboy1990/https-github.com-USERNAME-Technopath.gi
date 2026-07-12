using System;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleUIScreen : MonoBehaviour
    {
        [SerializeField] private BattlefieldPresenter presenter;
        [SerializeField] private BattleHudView hud;
        [SerializeField] private CombatLogView combatLog;
        [SerializeField] private RobotInspectionPanel inspectionPanel;
        [SerializeField] private BattleKeyboardInput keyboardInput;
        [SerializeField] private DebugBattleControlsView debugControls;

        private void Awake()
        {
            if (presenter == null || hud == null || combatLog == null || inspectionPanel == null || keyboardInput == null || debugControls == null)
                throw new InvalidOperationException("BattleUIScreen requires scene presenter and all child view references.");
            hud.Initialize(presenter);
            combatLog.Initialize(presenter);
            keyboardInput.Initialize(presenter, combatLog, inspectionPanel);
            debugControls.Initialize(presenter);
        }
    }
}
