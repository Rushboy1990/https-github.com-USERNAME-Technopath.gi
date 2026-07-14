using System.Collections.Generic;
using Technopath.Combat.Events;
using Technopath.Combat.Modules;

namespace Technopath.Combat.Archetypes
{
    public sealed class ConditionalAbilityEngine
    {
        private readonly Dictionary<string, RobotArchetypeDefinition> _definitions = new();
        private readonly List<RegisteredContentAbility> _moduleAbilities = new();
        private readonly AbilityUsageTracker _usage = new();

        public void Register(string unitId, RobotArchetypeDefinition definition)
        {
            _definitions.Add(unitId, definition);
        }

        public void RegisterModule(string unitId, RobotModuleDefinition module, string slotId)
        {
            if (module == null || module.AbilityDefinition == null)
                return;

            _moduleAbilities.Add(new RegisteredContentAbility(unitId, module.AbilityDefinition,
                $"{slotId}:{module.Id}", module.DisplayName));
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
                if (definition.AbilityDefinition != null &&
                    !definition.AbilityDefinition.AutomaticallyResolveEffects)
                    continue;
                if (!trigger.HasValue || GetTriggerMoment(definition) != trigger.Value)
                    continue;
                if (GetEventScope(definition) == AbilityEventScope.Self &&
                    combatEvent.SourceId != entry.Key && combatEvent.TargetId != entry.Key)
                    continue;
                if (_usage.TryUse($"{entry.Key}:{definition.Id}", GetFrequency(definition)))
                    activations.Add(new AbilityActivation(entry.Key, definition));
            }
            foreach (var registeredAbility in _moduleAbilities)
            {
                var ability = registeredAbility.Ability;
                if (!trigger.HasValue || ability.TriggerMoment != trigger.Value)
                    continue;
                if (ability.EventScope == AbilityEventScope.Self &&
                    combatEvent.SourceId != registeredAbility.UnitId && combatEvent.TargetId != registeredAbility.UnitId)
                    continue;
                if (_usage.TryUse($"{registeredAbility.UnitId}:{registeredAbility.SourceId}", ability.Frequency))
                    activations.Add(new AbilityActivation(registeredAbility.UnitId, ability,
                        registeredAbility.SourceId, registeredAbility.SourceLabel));
            }
            return activations;
        }

        private sealed class RegisteredContentAbility
        {
            public RegisteredContentAbility(string unitId, CombatAbilityDefinition ability, string sourceId,
                string sourceLabel)
            {
                UnitId = unitId;
                Ability = ability;
                SourceId = sourceId;
                SourceLabel = sourceLabel;
            }

            public string UnitId { get; }
            public CombatAbilityDefinition Ability { get; }
            public string SourceId { get; }
            public string SourceLabel { get; }
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

        private static AbilityTriggerMoment GetTriggerMoment(RobotArchetypeDefinition definition) => definition.TriggerMoment;

        private static AbilityEventScope GetEventScope(RobotArchetypeDefinition definition) => definition.EventScope;

        private static AbilityFrequency GetFrequency(RobotArchetypeDefinition definition) => definition.Frequency;
    }
}
