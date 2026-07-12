using System;
using UnityEngine;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CombatLogView : MonoBehaviour
    {
        private BattlefieldPresenter _presenter;
        [SerializeField] private RectTransform expandedBody;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Text titleText;
        [SerializeField] private Text collapseButtonText;
        [SerializeField] private Button collapseButton;
        [SerializeField] private CombatLogEntryView entryPrefab;
        [SerializeField] private bool expanded = true;
        [SerializeField, Min(52f)] private float expandedHeight = 276f;
        [SerializeField, Min(52f)] private float collapsedHeight = 52f;

        private int _renderedEntries;

        private void Awake()
        {
            ValidateReferences();
            collapseButton.onClick.AddListener(Toggle);
            ApplyExpandedState();
        }

        public void Initialize(BattlefieldPresenter presenter) => _presenter = presenter;

        private void LateUpdate()
        {
            if (_presenter == null) return;
            var entries = _presenter.CombatLogEntries;
            titleText.text = $"COMBAT LOG  {entries.Count}";
            while (_renderedEntries < entries.Count)
            {
                var item = Instantiate(entryPrefab, contentRoot);
                item.Bind(_renderedEntries + 1, entries[_renderedEntries]);
                _renderedEntries++;
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void Toggle()
        {
            expanded = !expanded;
            ApplyExpandedState();
        }

        private void ApplyExpandedState()
        {
            expandedBody.gameObject.SetActive(expanded);
            var offsetMax = panelRoot.offsetMax;
            offsetMax.y = panelRoot.offsetMin.y + (expanded ? expandedHeight : collapsedHeight);
            panelRoot.offsetMax = offsetMax;
            collapseButtonText.text = expanded ? "COLLAPSE  [L]" : "EXPAND  [L]";
        }

        private void OnDestroy()
        {
            if (collapseButton != null)
                collapseButton.onClick.RemoveListener(Toggle);
        }

        private void ValidateReferences()
        {
            if (panelRoot == null || expandedBody == null || contentRoot == null || scrollRect == null ||
                titleText == null || collapseButtonText == null || collapseButton == null || entryPrefab == null)
                throw new InvalidOperationException("CombatLogView requires all uGUI and prefab references.");
        }
    }
}
