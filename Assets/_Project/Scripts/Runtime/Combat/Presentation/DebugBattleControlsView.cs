using System;
using UnityEngine;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DebugBattleControlsView : MonoBehaviour
    {
        [SerializeField] private Button winBattleButton;
        private BattlefieldPresenter _presenter;

        private void Awake()
        {
            if (winBattleButton == null)
                throw new InvalidOperationException("DebugBattleControlsView requires Win Battle button reference.");
            winBattleButton.onClick.AddListener(WinBattle);
        }

        public void Initialize(BattlefieldPresenter presenter) => _presenter = presenter;

        private void Update()
        {
            if (_presenter != null)
                winBattleButton.interactable = _presenter.PhaseDescription != "Victory" && _presenter.PhaseDescription != "Defeat";
        }

        private void WinBattle() => _presenter?.DebugWinBattle();

        private void OnDestroy()
        {
            if (winBattleButton != null)
                winBattleButton.onClick.RemoveListener(WinBattle);
        }
    }
}
