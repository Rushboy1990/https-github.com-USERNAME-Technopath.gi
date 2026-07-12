using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattleHudView : MonoBehaviour
    {
        private BattlefieldPresenter _presenter;
        [SerializeField] private Text roundPhaseText;
        [SerializeField] private Text actionPointsText;
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
            roundPhaseText.text = $"ROUND {_presenter.RoundNumber}   {FormatPhase(_presenter.PhaseDescription)}";
            actionPointsText.text = FormatActionPoints(_presenter.ActionPoints);
            finishTurnButton.interactable = _presenter.PhaseDescription == "PlayerTurn";
        }

        private static string FormatPhase(string phase) => phase switch
        {
            "PlayerTurn" => "PLAYER TURN",
            "MutantTurn" => "MUTANT TURN",
            _ => phase.ToUpperInvariant()
        };

        private static string FormatActionPoints(int current)
        {
            var capacity = Math.Max(3, current);
            var builder = new StringBuilder(capacity * 2);
            for (var index = 0; index < capacity; index++)
            {
                if (index > 0) builder.Append(' ');
                builder.Append(index < current ? '●' : '○');
            }
            return builder.ToString();
        }

        private void FinishTurn() => _presenter?.FinishTurn();

        private void OnDestroy()
        {
            if (finishTurnButton != null)
                finishTurnButton.onClick.RemoveListener(FinishTurn);
        }

        private void ValidateReferences()
        {
            if (roundPhaseText == null || actionPointsText == null || finishTurnButton == null)
                throw new InvalidOperationException("BattleHudView requires round/phase, action points and End Turn button references.");
        }
    }
}
