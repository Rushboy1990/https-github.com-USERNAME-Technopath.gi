using Technopath.Combat.Archetypes;

namespace Technopath.Combat.Modules
{
    public sealed class LoadoutAbilitySummary
    {
        public LoadoutAbilitySummary(string source, string name, string rulesText,
            AbilityTriggerMoment trigger, int effectValue)
        {
            Source = source;
            Name = name;
            RulesText = rulesText;
            Trigger = trigger;
            EffectValue = effectValue;
        }

        public string Source { get; }
        public string Name { get; }
        public string RulesText { get; }
        public AbilityTriggerMoment Trigger { get; }
        public int EffectValue { get; }
    }
}
