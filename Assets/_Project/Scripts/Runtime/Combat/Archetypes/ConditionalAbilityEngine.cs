using System.Collections.Generic;
using Technopath.Combat.Events;

namespace Technopath.Combat.Archetypes
{
    public sealed class ConditionalAbilityEngine
    {
        private readonly Dictionary<string, RobotArchetypeDefinition> _definitions = new();
        private readonly AbilityUsageTracker _usage = new();

        public void Register(string unitId, RobotArchetypeDefinition definition)
        {
            _definitions.Add(unitId, definition);
        }

        public void BeginPhase() => _usage.BeginPhase();
        public void BeginAction() => _usage.BeginAction();

        public IReadOnlyList<AbilityActivation> Evaluate(CombatEvent combatEvent)
        {
            var trigger = MapTrigger(combatEvent.Kind);
            var activations = new List<AbilityActivation>();
            foreach (var entry in _definitions)
            {
                var definition = entry.Value;
                if (!trigger.HasValue || definition.TriggerMoment != trigger.Value)
                    continue;
                if (definition.EventScope == AbilityEventScope.Self &&
                    combatEvent.SourceId != entry.Key && combatEvent.TargetId != entry.Key)
                    continue;
                if (_usage.TryUse($"{entry.Key}:{definition.Id}", definition.Frequency))
                    activations.Add(new AbilityActivation(entry.Key, definition));
            }
            return activations;
        }

        private static AbilityTriggerMoment? MapTrigger(CombatEventKind kind) => kind switch
        {
            CombatEventKind.PhaseStarted => AbilityTriggerMoment.PlayerPhaseStarted,
            CombatEventKind.Movement => AbilityTriggerMoment.UnitMoved,
            CombatEventKind.Swap => AbilityTriggerMoment.UnitSwapped,
            CombatEventKind.Attack => AbilityTriggerMoment.UnitAttacked,
            CombatEventKind.Damage => AbilityTriggerMoment.UnitDamaged,
            CombatEventKind.Kill => AbilityTriggerMoment.UnitKilled,
            CombatEventKind.PhaseEnded => AbilityTriggerMoment.PlayerPhaseEnded,
            _ => null
        };
    }
}
