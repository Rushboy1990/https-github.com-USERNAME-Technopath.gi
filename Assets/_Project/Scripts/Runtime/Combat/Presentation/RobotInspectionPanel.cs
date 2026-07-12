using System;
using System.Text;
using UnityEngine;
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
        [SerializeField] private Text hoverArmorText;
        [SerializeField] private Image hoverArmorFill;
        [SerializeField] private Text hoverDamageText;
        [SerializeField] private Text hoverAbilityText;

        [Header("Pinned details card")]
        [SerializeField] private RectTransform detailsPanel;
        [SerializeField] private Text detailsTitle;
        [SerializeField] private Text detailsHealthText;
        [SerializeField] private Image detailsHealthFill;
        [SerializeField] private Text detailsArmorText;
        [SerializeField] private Image detailsArmorFill;
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
            hoverPanel.position = ClampToScreen(pointerPosition + new Vector2(18f, 18f), hoverPanel);
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
            detailsArmorText.text = $"ARMOR   {data.Armor}/{data.MaximumArmor}";
            detailsArmorFill.fillAmount = data.MaximumArmor > 0
                ? Mathf.Clamp01((float)data.Armor / data.MaximumArmor)
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
            tooltipPanel.position = ClampToScreen(pointerPosition + new Vector2(-tooltipPanel.rect.width - 18f, 18f), tooltipPanel);
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
            stats.text = $"HP {data.Health}/{data.MaximumHealth}   ARM {data.Armor}/{data.MaximumArmor}   ATK {data.Attack}";
            autoAttack.text = data.AutoAttack;
            primary.text = data.PrimaryAbility;
        }

        private void FillHover(RobotInspectionData data)
        {
            hoverTitle.text = $"{data.Name} — {data.Archetype}";
            hoverHealthText.text = $"HP     {data.Health}/{data.MaximumHealth}";
            hoverHealthFill.fillAmount = data.MaximumHealth > 0 ? Mathf.Clamp01((float)data.Health / data.MaximumHealth) : 0f;
            hoverArmorText.text = $"ARMOR  {data.Armor}/{data.MaximumArmor}";
            hoverArmorFill.fillAmount = data.MaximumArmor > 0 ? Mathf.Clamp01((float)data.Armor / data.MaximumArmor) : 0f;
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

        private static Vector2 ClampToScreen(Vector2 position, RectTransform panel)
        {
            var width = panel.rect.width;
            var height = panel.rect.height;
            return new Vector2(
                Mathf.Clamp(position.x, 8f, Screen.width - width - 8f),
                Mathf.Clamp(position.y, height + 8f, Screen.height - 8f));
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(ClearPinned);
        }

        private void ValidateReferences()
        {
            if (hoverPanel == null || hoverTitle == null || hoverHealthText == null || hoverHealthFill == null ||
                hoverArmorText == null || hoverArmorFill == null || hoverDamageText == null || hoverAbilityText == null ||
                detailsPanel == null || detailsTitle == null || detailsHealthText == null || detailsHealthFill == null ||
                detailsArmorText == null || detailsArmorFill == null || detailsDamageText == null ||
                detailsAutoAttack == null || detailsAbilityTitle == null || detailsPrimaryAbility == null || detailsUtilityAbility == null ||
                statusesText == null || modulesRoot == null || moduleItemPrefab == null || closeButton == null ||
                tooltipPanel == null || tooltipText == null)
                throw new InvalidOperationException("RobotInspectionPanel requires all uGUI and prefab references.");
        }
    }
}
