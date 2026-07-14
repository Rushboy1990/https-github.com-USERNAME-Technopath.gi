using System;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RobotInspectionPanel : MonoBehaviour
    {
        [Header("Compact hover card")]
        [SerializeField] private RectTransform hoverPanel;
        [SerializeField] private Text hoverTitle;
        [SerializeField] private Text hoverHealthText;
        [SerializeField] private Image hoverHealthFill;
        [FormerlySerializedAs("hoverArmorText")]
        [SerializeField] private Text hoverShieldText;
        [FormerlySerializedAs("hoverArmorFill")]
        [SerializeField] private Image hoverShieldFill;
        [SerializeField] private Text hoverDamageText;
        [SerializeField] private Text hoverAbilityText;

        [Header("Pinned details card")]
        [SerializeField] private RectTransform detailsPanel;
        [SerializeField] private Text detailsTitle;
        [SerializeField] private Text detailsHealthText;
        [SerializeField] private Image detailsHealthFill;
        [FormerlySerializedAs("detailsArmorText")]
        [SerializeField] private Text detailsShieldText;
        [FormerlySerializedAs("detailsArmorFill")]
        [SerializeField] private Image detailsShieldFill;
        [SerializeField] private Text detailsDamageText;
        [SerializeField] private Text detailsAutoAttack;
        [SerializeField] private Text detailsAbilityTitle;
        [SerializeField] private Text detailsPrimaryAbility;
        [SerializeField] private Text detailsUtilityAbility;
        [SerializeField] private Text statusesText;
        [SerializeField] private RectTransform modulesRoot;
        [SerializeField] private ModifierInspectionItemView moduleItemPrefab;
        [SerializeField] private Button closeButton;

        [Header("Module tooltip")]
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private Text tooltipText;

        private RobotInspectionData _hovered;
        private RobotInspectionData _pinned;

        private void Awake()
        {
            ValidateReferences();
            closeButton.onClick.AddListener(ClearPinned);
            hoverPanel.gameObject.SetActive(false);
            detailsPanel.gameObject.SetActive(false);
            tooltipPanel.gameObject.SetActive(false);
        }

        public void ShowHover(RobotInspectionData data, Vector2 pointerPosition)
        {
            _hovered = data;
            var visible = data != null && (_pinned == null || data.UnitId != _pinned.UnitId);
            hoverPanel.gameObject.SetActive(visible);
            if (!visible) return;
            FillHover(data);
            hoverPanel.position = GetTopRightTooltipPosition(pointerPosition, hoverPanel);
        }

        public void Pin(RobotInspectionData data)
        {
            _pinned = data;
            hoverPanel.gameObject.SetActive(false);
            detailsPanel.gameObject.SetActive(data != null);
            if (data == null) return;

            detailsTitle.text = $"{data.Name} — {data.Archetype}";
            detailsHealthText.text = $"HP   {data.Health}/{data.MaximumHealth}";
            detailsHealthFill.fillAmount = data.MaximumHealth > 0
                ? Mathf.Clamp01((float)data.Health / data.MaximumHealth)
                : 0f;
            detailsShieldText.text = $"SHIELD   {data.Shield}/{data.MaximumShield}";
            detailsShieldFill.fillAmount = data.MaximumShield > 0
                ? Mathf.Clamp01((float)data.Shield / data.MaximumShield)
                : 0f;
            detailsDamageText.text = $"AUTOATTACK   {data.Attack}";
            detailsAutoAttack.text = data.AutoAttack;
            SplitAbility(data.PrimaryAbility, out var abilityTitle, out var abilityDescription);
            detailsAbilityTitle.text = abilityTitle;
            detailsPrimaryAbility.text = abilityDescription;
            detailsUtilityAbility.text = string.IsNullOrWhiteSpace(data.UtilityAbility) ? "None" : data.UtilityAbility;
            statusesText.text = BuildStatuses(data);
            RebuildModules(data);
        }

        public void ClearPinned()
        {
            _pinned = null;
            detailsPanel.gameObject.SetActive(false);
            HideModuleTooltip();
        }

        public void ShowModuleTooltip(string content, Vector2 pointerPosition)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            tooltipText.text = content;
            tooltipPanel.gameObject.SetActive(true);
            tooltipPanel.position = GetTopRightTooltipPosition(pointerPosition, tooltipPanel);
            tooltipPanel.SetAsLastSibling();
        }

        public void HideModuleTooltip() => tooltipPanel.gameObject.SetActive(false);

        private void RebuildModules(RobotInspectionData data)
        {
            for (var index = modulesRoot.childCount - 1; index >= 0; index--)
                Destroy(modulesRoot.GetChild(index).gameObject);
            foreach (var module in data.Modules)
                Instantiate(moduleItemPrefab, modulesRoot).Bind(module, this);
        }

        private static void FillShared(RobotInspectionData data, Text title, Text stats, Text autoAttack, Text primary)
        {
            title.text = $"{data.Name} — {data.Archetype}";
            stats.text = $"HP {data.Health}/{data.MaximumHealth}   SHD {data.Shield}/{data.MaximumShield}   ATK {data.Attack}";
            autoAttack.text = data.AutoAttack;
            primary.text = data.PrimaryAbility;
        }

        private void FillHover(RobotInspectionData data)
        {
            hoverTitle.text = $"{data.Name} — {data.Archetype}";
            hoverHealthText.text = $"HP     {data.Health}/{data.MaximumHealth}";
            hoverHealthFill.fillAmount = data.MaximumHealth > 0 ? Mathf.Clamp01((float)data.Health / data.MaximumHealth) : 0f;
            hoverShieldText.text = $"SHIELD  {data.Shield}/{data.MaximumShield}";
            hoverShieldFill.fillAmount = data.MaximumShield > 0 ? Mathf.Clamp01((float)data.Shield / data.MaximumShield) : 0f;
            hoverDamageText.text = $"DAMAGE  {data.Attack}";
            hoverAbilityText.text = string.IsNullOrWhiteSpace(data.PrimaryAbility) ? "No primary ability" : data.PrimaryAbility;
        }

        private static string BuildStatuses(RobotInspectionData data)
        {
            if (data.Statuses.Count == 0) return "None";
            var builder = new StringBuilder();
            foreach (var status in data.Statuses)
            {
                if (builder.Length > 0) builder.AppendLine();
                builder.Append("• ").Append(status);
            }
            return builder.ToString();
        }

        private static void SplitAbility(string ability, out string title, out string description)
        {
            if (string.IsNullOrWhiteSpace(ability))
            {
                title = "NO PRIMARY ABILITY";
                description = "None";
                return;
            }

            var separator = ability.IndexOf(':');
            if (separator <= 0)
            {
                title = "PRIMARY ABILITY";
                description = ability;
                return;
            }

            title = ability[..separator].Trim().ToUpperInvariant();
            description = ability[(separator + 1)..].Trim();
        }

        private static Vector2 GetTopRightTooltipPosition(Vector2 pointerPosition, RectTransform panel)
        {
            const float margin = 32f;
            var size = panel.rect.size;
            var pivot = panel.pivot;
            var position = pointerPosition + new Vector2(
                margin + size.x * pivot.x,
                margin + size.y * pivot.y);

            return ClampToScreen(position, panel);
        }

        private static Vector2 ClampToScreen(Vector2 position, RectTransform panel)
        {
            const float padding = 8f;
            var size = panel.rect.size;
            var pivot = panel.pivot;
            return new Vector2(
                Mathf.Clamp(position.x, padding + size.x * pivot.x, Screen.width - padding - size.x * (1f - pivot.x)),
                Mathf.Clamp(position.y, padding + size.y * pivot.y, Screen.height - padding - size.y * (1f - pivot.y)));
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClearPinned);
        }

        private void ValidateReferences()
        {
            if (hoverPanel == null || hoverTitle == null || hoverHealthText == null || hoverHealthFill == null ||
                hoverShieldText == null || hoverShieldFill == null || hoverDamageText == null || hoverAbilityText == null ||
                detailsPanel == null || detailsTitle == null || detailsHealthText == null || detailsHealthFill == null ||
                detailsShieldText == null || detailsShieldFill == null || detailsDamageText == null ||
                detailsAutoAttack == null || detailsAbilityTitle == null || detailsPrimaryAbility == null || detailsUtilityAbility == null ||
                statusesText == null || modulesRoot == null || moduleItemPrefab == null || closeButton == null ||
                tooltipPanel == null || tooltipText == null)
                throw new InvalidOperationException("RobotInspectionPanel requires all uGUI and prefab references.");
        }
    }
}
