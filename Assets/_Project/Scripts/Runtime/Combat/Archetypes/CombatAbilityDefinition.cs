using System;
using UnityEngine;

namespace Technopath.Combat.Archetypes
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Ability", fileName = "CombatAbility")]
    public sealed class CombatAbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string rulesText;

        [Header("Activation")]
        [SerializeField] private AbilityTriggerMoment triggerMoment;
        [SerializeField] private AbilityEventScope eventScope;
        [SerializeField] private AbilityFrequency frequency = AbilityFrequency.OncePerPhase;
        [SerializeField] private bool requiresHealthDamage;

        [Header("Special rule")]
        [SerializeField] private RobotAbilityKind abilityKind;
        [SerializeField, Min(0)] private int effectValue;

        [Header("Effects")]
        [SerializeField] private bool automaticallyResolveEffects;
        [SerializeField] private CombatAbilityEffectDefinition[] effects = { new CombatAbilityEffectDefinition() };

        public string Id => id;
        public string DisplayName => displayName;
        public string RulesText => rulesText;
        public AbilityTriggerMoment TriggerMoment => triggerMoment;
        public AbilityEventScope EventScope => eventScope;
        public AbilityFrequency Frequency => frequency;
        public bool RequiresHealthDamage => requiresHealthDamage;
        public RobotAbilityKind AbilityKind => abilityKind;
        public int EffectValue => effectValue;
        public bool AutomaticallyResolveEffects => automaticallyResolveEffects;
        public CombatAbilityEffectDefinition[] Effects => effects;

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
        }
    }
}
