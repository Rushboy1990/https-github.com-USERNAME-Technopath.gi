using Technopath.Combat.Archetypes;
using UnityEngine;

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
        [SerializeField] private int armorModifier;
        [SerializeField] private int attackModifier;

        public string Id => id;
        public string DisplayName => displayName;
        public string RulesText => rulesText;
        public ModuleSlotType SlotType => slotType;
        public ModuleRarity Rarity => rarity;
        public int Level => level;
        public int HealthModifier => healthModifier;
        public int ArmorModifier => armorModifier;
        public int AttackModifier => attackModifier;

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
            level = Mathf.Max(1, level);
        }
    }
}
