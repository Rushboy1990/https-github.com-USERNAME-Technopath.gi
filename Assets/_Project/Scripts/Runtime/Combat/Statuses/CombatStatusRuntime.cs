using System;
using System.Collections.Generic;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Rules;

namespace Technopath.Combat.Statuses
{
    public sealed class CombatStatusRuntime
    {
        private readonly PlayerTurnModel _combatState;

        public CombatStatusRuntime(PlayerTurnModel combatState, StatusCollection statuses = null)
        {
            _combatState = combatState ?? throw new ArgumentNullException(nameof(combatState));
            Statuses = statuses ?? new StatusCollection();
        }

        public StatusCollection Statuses { get; }

        public bool CanAct(string unitId) => !Statuses.HasEffect(unitId, StatusEffectKind.Stun);

        public int AddOutgoingAttackDamage(string unitId, int damage) =>
            damage + Statuses.GetEffectValue(unitId, StatusEffectKind.BonusAttackDamage);

        public bool TryIgnoreAttack(string targetId)
        {
            if (!Statuses.TryConsumeFirst(targetId, StatusEffectKind.IgnoreAttack, out var result))
                return false;
            Enqueue(result, targetId);
            return true;
        }

        public int ConsumeBonusDamageTaken(string targetId)
        {
            var total = 0;
            foreach (var result in Statuses.Consume(targetId, StatusTickMoment.NextAttack,
                         StatusEffectKind.BonusDamageTaken))
            {
                total += result.Value;
                Enqueue(result, targetId);
            }
            return total;
        }

        public void ResolveUnitMoved(string unitId) => Resolve(unitId, StatusTickMoment.UnitMoved);

        public void ResolveSideMoment(BoardSide side, StatusTickMoment moment)
        {
            foreach (var unitId in Statuses.GetAffectedUnitIds())
                if (_combatState.TryGetUnit(unitId, out var unit) && unit.Side == side && unit.IsAlive)
                    Resolve(unitId, moment);
        }

        public void ResolveWaitingMovementEffects(BoardSide side, ISet<string> movedUnitIds)
        {
            foreach (var unitId in Statuses.GetAffectedUnitIds())
                if ((movedUnitIds == null || !movedUnitIds.Contains(unitId)) &&
                    _combatState.TryGetUnit(unitId, out var unit) && unit.Side == side && unit.IsAlive)
                    Resolve(unitId, StatusTickMoment.UnitMoved);
        }

        private void Resolve(string unitId, StatusTickMoment moment)
        {
            foreach (var result in Statuses.Tick(unitId, moment))
            {
                switch (result.EffectKind)
                {
                    case StatusEffectKind.Damage:
                        _combatState.ApplyDamageDetailed(unitId, result.Value, false);
                        break;
                    case StatusEffectKind.ShieldReduction:
                        if (_combatState.TryGetUnit(unitId, out var unit))
                            unit.RemoveShield(result.Value);
                        break;
                    case StatusEffectKind.BonusDamageTaken:
                    case StatusEffectKind.IgnoreAttack:
                    case StatusEffectKind.BonusAttackDamage:
                    case StatusEffectKind.Stun:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Enqueue(result, unitId);
            }
        }

        private void Enqueue(StatusTickResult result, string unitId) =>
            _combatState.Events.Enqueue(new CombatEvent(CombatEventKind.StatusTriggered,
                result.StatusId, unitId, result.Value));
    }
}
