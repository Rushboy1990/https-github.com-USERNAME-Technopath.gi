using System;
using UnityEngine;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleHudView : MonoBehaviour
    {
        private BattlefieldPresenter _presenter;
        [SerializeField] private Text roundText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text actionPointsText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Button finishTurnButton;

        private void Awake()
        {
            ValidateReferences();
            finishTurnButton.onClick.AddListener(FinishTurn);
        }

        public void Initialize(BattlefieldPresenter presenter) => _presenter = presenter;

        private void Update()
        {
            if (_presenter == null) return;
            roundText.text = $"ROUND {_presenter.RoundNumber}";
            phaseText.text = _presenter.PhaseDescription;
            actionPointsText.text = $"ACTION POINTS  {_presenter.ActionPoints}";
            selectionText.text = _presenter.SelectionDescription;
            finishTurnButton.interactable = _presenter.PhaseDescription == "PlayerTurn";
        }

        private void FinishTurn() => _presenter?.FinishTurn();

        private void OnDestroy()
        {
            if (finishTurnButton != null)
                finishTurnButton.onClick.RemoveListener(FinishTurn);
        }

        private void ValidateReferences()
        {
            if (roundText == null || phaseText == null || actionPointsText == null || selectionText == null || finishTurnButton == null)
                throw new InvalidOperationException("BattleHudView requires labels and Finish Turn button references.");
        }
    }
}
