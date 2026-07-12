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
        [SerializeField] private GameObject debugWinButtonObject;
        [SerializeField] private CombatLogEntryView entryPrefab;
        [SerializeField] private bool expanded = true;
        [SerializeField, Min(52f)] private float expandedHeight = 276f;
        [SerializeField, Min(52f)] private float collapsedHeight = 52f;

        private int _renderedEntries;
        private Vector2 _expandedPanelAnchorMin;
        private Vector2 _expandedPanelAnchorMax;
        private Vector2 _expandedPanelOffsetMin;
        private Vector2 _expandedPanelOffsetMax;
        private Vector2 _expandedButtonAnchorMin;
        private Vector2 _expandedButtonAnchorMax;
        private Vector2 _expandedButtonOffsetMin;
        private Vector2 _expandedButtonOffsetMax;

        private void Awake()
        {
            ValidateReferences();
            CaptureExpandedLayout();
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
            titleText.gameObject.SetActive(expanded);
            if (debugWinButtonObject != null) debugWinButtonObject.SetActive(expanded);

            if (expanded)
            {
                SetRect(panelRoot, _expandedPanelAnchorMin, _expandedPanelAnchorMax, _expandedPanelOffsetMin,
                    new Vector2(_expandedPanelOffsetMax.x, _expandedPanelOffsetMin.y + expandedHeight));
                SetRect((RectTransform)collapseButton.transform, _expandedButtonAnchorMin, _expandedButtonAnchorMax,
                    _expandedButtonOffsetMin, _expandedButtonOffsetMax);
                collapseButtonText.text = "COLLAPSE  [L]";
            }
            else
            {
                SetRect(panelRoot, new Vector2(1f, 0f), new Vector2(1f, 0f),
                    new Vector2(-24f - collapsedHeight, 24f), new Vector2(-24f, 24f + collapsedHeight));
                SetRect((RectTransform)collapseButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                collapseButtonText.text = "LOG";
            }
        }

        private void CaptureExpandedLayout()
        {
            _expandedPanelAnchorMin = panelRoot.anchorMin;
            _expandedPanelAnchorMax = panelRoot.anchorMax;
            _expandedPanelOffsetMin = panelRoot.offsetMin;
            _expandedPanelOffsetMax = panelRoot.offsetMax;
            var buttonRect = (RectTransform)collapseButton.transform;
            _expandedButtonAnchorMin = buttonRect.anchorMin;
            _expandedButtonAnchorMax = buttonRect.anchorMax;
            _expandedButtonOffsetMin = buttonRect.offsetMin;
            _expandedButtonOffsetMax = buttonRect.offsetMax;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
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
