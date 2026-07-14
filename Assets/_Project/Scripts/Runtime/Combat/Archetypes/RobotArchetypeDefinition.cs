using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("maximumArmor")]
        [SerializeField, Min(0)] private int maximumShield = 3;
        [SerializeField, Min(0)] private int autoAttackDamage = 2;

        [SerializeField, HideInInspector] private string abilityName;
        [SerializeField, HideInInspector] private string abilityRulesText;
        [SerializeField, HideInInspector] private AbilityTriggerMoment triggerMoment;
        [SerializeField, HideInInspector] private AbilityEventScope eventScope;
        [SerializeField, HideInInspector] private AbilityFrequency frequency = AbilityFrequency.OncePerPhase;
        [SerializeField, HideInInspector, Min(0)] private int effectValue = 1;

        [SerializeField, HideInInspector] private RobotAbilityKind abilityKind;

        [Header("Content ability")]
        [SerializeField] private CombatAbilityDefinition abilityDefinition;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ArchetypeRole Role => role;
        public int MaximumHealth => maximumHealth;
        public int MaximumShield => maximumShield;
        public int AutoAttackDamage => autoAttackDamage;
        public string AbilityName => abilityDefinition != null && !string.IsNullOrWhiteSpace(abilityDefinition.DisplayName)
            ? abilityDefinition.DisplayName : abilityName;
        public string AbilityRulesText => abilityDefinition != null && !string.IsNullOrWhiteSpace(abilityDefinition.RulesText)
            ? abilityDefinition.RulesText : abilityRulesText;
        public AbilityTriggerMoment TriggerMoment => abilityDefinition != null
            ? abilityDefinition.TriggerMoment : triggerMoment;
        public AbilityEventScope EventScope => abilityDefinition != null
            ? abilityDefinition.EventScope : eventScope;
        public AbilityFrequency Frequency => abilityDefinition != null
            ? abilityDefinition.Frequency : frequency;
        public int EffectValue => abilityDefinition != null ? abilityDefinition.EffectValue : effectValue;
        public RobotAbilityKind AbilityKind => abilityDefinition != null ? abilityDefinition.AbilityKind : abilityKind;
        public CombatAbilityDefinition AbilityDefinition => abilityDefinition;
        public bool HasConsistentAbilityConfiguration => abilityDefinition == null ||
            abilityKind == RobotAbilityKind.None || abilityDefinition.AbilityKind == abilityKind;

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
            abilityName = abilityName?.Trim();
        }
    }
}
