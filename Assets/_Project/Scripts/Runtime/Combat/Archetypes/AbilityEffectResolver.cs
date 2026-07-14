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
            if (activation.Ability != null)
                return ApplyContentAbility(activation, sourceEvent, combatState, statuses);

            if (activation.Definition == null)
                return false;

            switch (activation.Definition.Role)
            {
                case ArchetypeRole.Attacker when !string.IsNullOrEmpty(sourceEvent.TargetId):
                    statuses.Add(sourceEvent.TargetId, "status.target-lock", 1,
                        activation.EffectValue, StatusTickMoment.NextAttack,
                        effectKind: StatusEffectKind.BonusDamageTaken);
                    return true;
                case ArchetypeRole.Defender when !string.IsNullOrEmpty(sourceEvent.TargetId):
                    if (!combatState.TryGetUnit(sourceEvent.TargetId, out var damaged) || damaged.Side != BoardSide.Player)
                        return false;
                    combatState.AddShield(sourceEvent.TargetId, activation.EffectValue);
                    return true;
                case ArchetypeRole.Support when sourceEvent.TargetId == activation.UnitId &&
                                                 !string.IsNullOrEmpty(sourceEvent.SourceId):
                    combatState.AddShield(sourceEvent.SourceId, activation.EffectValue);
                    return true;
                default:
                    return false;
            }
        }

        private static bool ApplyContentAbility(AbilityActivation activation, CombatEvent sourceEvent,
            PlayerTurnModel combatState, StatusCollection statuses)
        {
            if (activation.Ability.RequiresHealthDamage && sourceEvent.Value <= 0)
                return false;

            var applied = false;
            foreach (var effect in activation.Ability.Effects)
            {
                if (effect == null || !effect.IsValid ||
                    !TryResolveTarget(effect.Target, activation.UnitId, sourceEvent, out var targetId) ||
                    !combatState.TryGetUnit(targetId, out _))
                    continue;

                switch (effect.Kind)
                {
                    case CombatAbilityEffectKind.DealDamage:
                        combatState.ApplyDamageDetailed(targetId, effect.Value);
                        applied = true;
                        break;
                    case CombatAbilityEffectKind.RestoreShield:
                        combatState.AddShield(targetId, effect.Value);
                        applied = true;
                        break;
                    case CombatAbilityEffectKind.ApplyStatus:
                        statuses.Add(targetId, effect.Status, effect.Value);
                        applied = true;
                        break;
                }
            }
            return applied;
        }

        private static bool TryResolveTarget(CombatAbilityEffectTarget target, string ownerId,
            CombatEvent sourceEvent, out string targetId)
        {
            targetId = target switch
            {
                CombatAbilityEffectTarget.AbilityOwner => ownerId,
                CombatAbilityEffectTarget.EventSource => sourceEvent.SourceId,
                CombatAbilityEffectTarget.EventTarget => sourceEvent.TargetId,
                _ => null
            };
            return !string.IsNullOrEmpty(targetId);
        }
    }
}
