namespace Technopath.Combat.Archetypes
{
    public sealed class AbilityActivation
    {
        public AbilityActivation(string unitId, RobotArchetypeDefinition definition)
        {
            UnitId = unitId;
            Definition = definition;
        }

        public string UnitId { get; }
        public RobotArchetypeDefinition Definition { get; }
        public int EffectValue => Definition.EffectValue;
    }
}
