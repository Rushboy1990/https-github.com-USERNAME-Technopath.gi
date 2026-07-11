using UnityEngine;

namespace Technopath.Combat.Archetypes
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Robot Archetype", fileName = "RobotArchetype")]
    public sealed class RobotArchetypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ArchetypeRole role;

        [Header("Base stats")]
        [SerializeField, Min(1)] private int maximumHealth = 10;
        [SerializeField, Min(0)] private int maximumArmor = 3;
        [SerializeField, Min(0)] private int autoAttackDamage = 2;

        [Header("Conditional main ability")]
        [SerializeField] private string abilityName;
        [SerializeField, TextArea] private string abilityRulesText;
        [SerializeField] private AbilityTriggerMoment triggerMoment;
        [SerializeField] private AbilityFrequency frequency = AbilityFrequency.OncePerPhase;
        [SerializeField, Min(0)] private int effectValue = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ArchetypeRole Role => role;
        public int MaximumHealth => maximumHealth;
        public int MaximumArmor => maximumArmor;
        public int AutoAttackDamage => autoAttackDamage;
        public string AbilityName => abilityName;
        public string AbilityRulesText => abilityRulesText;
        public AbilityTriggerMoment TriggerMoment => triggerMoment;
        public AbilityFrequency Frequency => frequency;
        public int EffectValue => effectValue;

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
            abilityName = abilityName?.Trim();
        }
    }
}
