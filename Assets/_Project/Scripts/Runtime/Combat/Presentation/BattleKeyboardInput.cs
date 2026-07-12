using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleKeyboardInput : MonoBehaviour
    {
        private BattlefieldPresenter _presenter;
        private CombatLogView _combatLog;
        private RobotInspectionPanel _inspectionPanel;

        public void Initialize(BattlefieldPresenter presenter, CombatLogView combatLog, RobotInspectionPanel inspectionPanel)
        {
            if (presenter == null || combatLog == null || inspectionPanel == null)
                throw new ArgumentNullException(nameof(presenter));
            _presenter = presenter;
            _combatLog = combatLog;
            _inspectionPanel = inspectionPanel;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _presenter == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame) _presenter.FinishTurn();
            if (keyboard.lKey.wasPressedThisFrame) _combatLog.Toggle();
            if (keyboard.escapeKey.wasPressedThisFrame) _presenter.ClearInspection();
        }
    }
}
