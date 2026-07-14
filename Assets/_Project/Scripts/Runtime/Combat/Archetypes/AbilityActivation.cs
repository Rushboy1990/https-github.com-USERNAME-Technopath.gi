namespace Technopath.Combat.Archetypes
{
    public sealed class AbilityActivation
    {
        public AbilityActivation(string unitId, RobotArchetypeDefinition definition)
        {
            UnitId = unitId;
            Definition = definition;
            Ability = definition.AbilityDefinition;
            SourceId = definition.Id;
            SourceLabel = definition.DisplayName;
        }

        public AbilityActivation(string unitId, CombatAbilityDefinition ability, string sourceId, string sourceLabel)
        {
            UnitId = unitId;
            Ability = ability;
            SourceId = sourceId;
            SourceLabel = sourceLabel;
        }

        public string UnitId { get; }
        public RobotArchetypeDefinition Definition { get; }
        public string SourceId { get; }
        public string SourceLabel { get; }
        public string AbilityName => Ability != null ? Ability.DisplayName : Definition.AbilityName;
        public int EffectValue => Ability != null ? Ability.EffectValue : Definition?.EffectValue ?? 0;
        public CombatAbilityDefinition Ability { get; }
    }
}
