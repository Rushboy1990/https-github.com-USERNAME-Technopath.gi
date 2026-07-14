using Technopath.Combat.Archetypes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Technopath.Combat.Modules
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Robot Module", fileName = "RobotModule")]
    public sealed class RobotModuleDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string rulesText;
        [SerializeField] private ModuleSlotType slotType;
        [SerializeField] private ModuleRarity rarity;
        [SerializeField, Min(1)] private int level = 1;

        [Header("Compatibility; empty means all")]
        [SerializeField] private ArchetypeRole[] compatibleRoles = System.Array.Empty<ArchetypeRole>();

        [Header("Stat changes")]
        [SerializeField] private int healthModifier;
        [FormerlySerializedAs("armorModifier")]
        [SerializeField] private int shieldModifier;
        [SerializeField] private int attackModifier;

        [Header("Optional conditional ability")]
        [SerializeField] private string abilityName;
        [SerializeField, TextArea] private string abilityRulesText;
        [SerializeField] private AbilityTriggerMoment abilityTrigger;
        [SerializeField, Min(0)] private int abilityEffectValue;

        public string Id => id;
        public string DisplayName => displayName;
        public string RulesText => rulesText;
        public ModuleSlotType SlotType => slotType;
        public ModuleRarity Rarity => rarity;
        public int Level => level;
        public int HealthModifier => healthModifier;
        public int ShieldModifier => shieldModifier;
        public int AttackModifier => attackModifier;
        public string AbilityName => abilityName;
        public string AbilityRulesText => abilityRulesText;
        public AbilityTriggerMoment AbilityTrigger => abilityTrigger;
        public int AbilityEffectValue => abilityEffectValue;
        public bool HasAbility => !string.IsNullOrWhiteSpace(abilityName);

        public bool IsCompatible(ArchetypeRole role)
        {
            if (compatibleRoles == null || compatibleRoles.Length == 0) return true;
            foreach (var compatibleRole in compatibleRoles)
                if (compatibleRole == role) return true;
            return false;
        }

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
            abilityName = abilityName?.Trim();
            level = Mathf.Max(1, level);
        }
    }
}
