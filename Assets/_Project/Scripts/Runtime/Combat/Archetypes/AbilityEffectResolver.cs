using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Rules;
using Technopath.Combat.Statuses;

namespace Technopath.Combat.Archetypes
{
    public sealed class AbilityEffectResolver
    {
        public bool Apply(AbilityActivation activation, CombatEvent sourceEvent,
            PlayerTurnModel combatState, StatusCollection statuses)
        {
            switch (activation.Definition.Role)
            {
                case ArchetypeRole.Attacker when !string.IsNullOrEmpty(sourceEvent.TargetId):
                    statuses.Add(sourceEvent.TargetId, "status.target-lock", 1,
                        activation.EffectValue, StatusTickMoment.UnitMoved);
                    return true;
                case ArchetypeRole.Defender when !string.IsNullOrEmpty(sourceEvent.TargetId):
                    if (!combatState.TryGetUnit(sourceEvent.TargetId, out var damaged) || damaged.Side != BoardSide.Player)
                        return false;
                    combatState.AddArmor(sourceEvent.TargetId, activation.EffectValue);
                    return true;
                case ArchetypeRole.Support when sourceEvent.TargetId == activation.UnitId &&
                                                 !string.IsNullOrEmpty(sourceEvent.SourceId):
                    combatState.AddArmor(sourceEvent.SourceId, activation.EffectValue);
                    return true;
                default:
                    return false;
            }
        }
    }
}
