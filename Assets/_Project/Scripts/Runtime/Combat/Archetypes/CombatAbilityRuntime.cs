using System;
using System.Collections.Generic;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Modules;
using Technopath.Combat.Rules;
using Technopath.Combat.Statuses;

namespace Technopath.Combat.Archetypes
{
    public sealed class CombatAbilityRuntime
    {
        private readonly BattlefieldModel _battlefield;
        private readonly PlayerTurnModel _combatState;
        private readonly ConditionalAbilityEngine _engine = new();
        private readonly AbilityEffectResolver _effects = new();
        private readonly StatusCollection _statuses;
        private readonly CombatStatusRuntime _statusRuntime;
        private readonly Dictionary<string, RobotArchetypeDefinition> _archetypes = new();
        private readonly Dictionary<string, RobotLoadout> _loadouts = new();
        private readonly Random _random;

        public CombatAbilityRuntime(BattlefieldModel battlefield, PlayerTurnModel combatState,
            IReadOnlyDictionary<string, RobotArchetypeDefinition> archetypes,
            IReadOnlyDictionary<string, RobotLoadout> loadouts, int randomSeed)
        {
            _battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            _combatState = combatState ?? throw new ArgumentNullException(nameof(combatState));
            _statusRuntime = new CombatStatusRuntime(_combatState);
            _statuses = _statusRuntime.Statuses;
            _combatState.AttachStatusRuntime(_statusRuntime);
            _random = new Random(randomSeed);

            if (archetypes == null)
                return;
            foreach (var entry in archetypes)
            {
                RobotLoadout loadout = null;
                loadouts?.TryGetValue(entry.Key, out loadout);
                RegisterUnit(entry.Key, entry.Value, loadout);
            }
        }

        public void RegisterUnit(string unitId, RobotArchetypeDefinition archetype, RobotLoadout loadout = null)
        {
            if (string.IsNullOrWhiteSpace(unitId)) throw new ArgumentException("Unit id is required.", nameof(unitId));
            if (archetype == null) throw new ArgumentNullException(nameof(archetype));
            if (_archetypes.ContainsKey(unitId)) throw new InvalidOperationException($"Unit '{unitId}' is already registered.");

            _archetypes.Add(unitId, archetype);
            if (loadout != null)
                _loadouts.Add(unitId, loadout);

            if (loadout != null && loadout.HasPrimaryAbilityOverride)
                _engine.RegisterModule(unitId, loadout.Core, "core");
            else
                _engine.Register(unitId, archetype);

            if (loadout == null)
                return;
            _engine.RegisterModule(unitId, loadout.Processor, "processor");
            for (var index = 0; index < loadout.Modifiers.Count; index++)
                _engine.RegisterModule(unitId, loadout.Modifiers[index], $"modifier-{index + 1}");
        }

        public void BeginPhase() => _engine.BeginPhase();
        public void BeginAction() => _engine.BeginAction();
        public IReadOnlyList<ChargedStatusState> GetActiveStatuses(string unitId) => _statuses.GetActive(unitId);

        public IReadOnlyList<CombatResolutionEntry> ResolvePendingEvents()
        {
            var result = new List<CombatResolutionEntry>();
            var processedEvents = 0;
            while (_combatState.Events.Count > 0)
            {
                if (++processedEvents > _combatState.Events.MaximumEventsPerChain)
                    throw new InvalidOperationException(
                        $"Combat event chain exceeded {_combatState.Events.MaximumEventsPerChain} processed events.");

                var combatEvent = _combatState.Events.Dequeue();
                result.Add(CombatResolutionEntry.FromEvent(combatEvent));

                if (combatEvent.Kind == CombatEventKind.StatusTriggered)
                    result.Add(CombatResolutionEntry.FromStatus(combatEvent.SourceId,
                        combatEvent.TargetId, combatEvent.Value));

                if (combatEvent.Kind == CombatEventKind.PhaseStarted)
                    ApplyEmitterForceFields(result);

                foreach (var activation in _engine.Evaluate(combatEvent))
                {
                    if (!_statusRuntime.CanAct(activation.UnitId))
                        continue;
                    if (_effects.Apply(activation, combatEvent, _combatState, _statuses))
                        result.Add(CombatResolutionEntry.FromAbility(activation));
                }
            }
            return result;
        }

        private void ApplyEmitterForceFields(ICollection<CombatResolutionEntry> result)
        {
            foreach (var entry in _archetypes)
            {
                if (!_combatState.IsAlive(entry.Key) || !_statusRuntime.CanAct(entry.Key) ||
                    GetPrimaryAbilityKind(entry.Key, entry.Value) != RobotAbilityKind.ForceFieldPulse)
                    continue;

                var ability = GetPrimaryAbilityDefinition(entry.Key, entry.Value);
                var effect = FindStatusEffect(ability);
                if (effect == null)
                    throw new InvalidOperationException($"Emitter ability for '{entry.Key}' requires an ApplyStatus effect.");

                var livingAllies = new List<string>();
                foreach (var cell in _battlefield.Player.Cells)
                {
                    if (cell.Occupancy == CellOccupancyKind.Unit && _combatState.IsAlive(cell.OccupantId))
                        livingAllies.Add(cell.OccupantId);
                }
                if (livingAllies.Count == 0)
                    continue;

                var targetId = livingAllies[_random.Next(livingAllies.Count)];
                _statuses.Add(targetId, effect.Status, effect.Value);
                var sourceLabel = _loadouts.TryGetValue(entry.Key, out var loadout) && loadout.HasPrimaryAbilityOverride
                    ? loadout.Core.DisplayName
                    : entry.Value.DisplayName;
                result.Add(CombatResolutionEntry.FromAbility(entry.Key, sourceLabel, ability.DisplayName,
                    effect.Value, targetId));
            }
        }

        private RobotAbilityKind GetPrimaryAbilityKind(string unitId, RobotArchetypeDefinition archetype) =>
            _loadouts.TryGetValue(unitId, out var loadout) ? loadout.PrimaryAbilityKind : archetype.AbilityKind;

        private CombatAbilityDefinition GetPrimaryAbilityDefinition(string unitId, RobotArchetypeDefinition archetype) =>
            _loadouts.TryGetValue(unitId, out var loadout) ? loadout.PrimaryAbilityDefinition : archetype.AbilityDefinition;

        private static CombatAbilityEffectDefinition FindStatusEffect(CombatAbilityDefinition ability)
        {
            if (ability?.Effects == null)
                return null;
            foreach (var effect in ability.Effects)
                if (effect != null && effect.Kind == CombatAbilityEffectKind.ApplyStatus && effect.Status != null)
                    return effect;
            return null;
        }
    }

    public enum CombatResolutionEntryKind
    {
        Event = 0,
        Ability = 1,
        Status = 2
    }

    public sealed class CombatResolutionEntry
    {
        private CombatResolutionEntry(CombatResolutionEntryKind kind, CombatEvent combatEvent = null,
            string unitId = null, string sourceLabel = null, string abilityName = null,
            string statusId = null, string targetId = null, int value = 0)
        {
            Kind = kind;
            CombatEvent = combatEvent;
            UnitId = unitId;
            SourceLabel = sourceLabel;
            AbilityName = abilityName;
            StatusId = statusId;
            TargetId = targetId;
            Value = value;
        }

        public CombatResolutionEntryKind Kind { get; }
        public CombatEvent CombatEvent { get; }
        public string UnitId { get; }
        public string SourceLabel { get; }
        public string AbilityName { get; }
        public string StatusId { get; }
        public string TargetId { get; }
        public int Value { get; }

        public static CombatResolutionEntry FromEvent(CombatEvent combatEvent) =>
            new(CombatResolutionEntryKind.Event, combatEvent: combatEvent);

        public static CombatResolutionEntry FromAbility(AbilityActivation activation) =>
            FromAbility(activation.UnitId, activation.SourceLabel, activation.AbilityName, activation.EffectValue);

        public static CombatResolutionEntry FromAbility(string unitId, string sourceLabel, string abilityName,
            int value, string targetId = null) =>
            new(CombatResolutionEntryKind.Ability, unitId: unitId, sourceLabel: sourceLabel,
                abilityName: abilityName, targetId: targetId, value: value);

        public static CombatResolutionEntry FromStatus(string statusId, string targetId, int value) =>
            new(CombatResolutionEntryKind.Status, statusId: statusId, targetId: targetId, value: value);
    }
}
