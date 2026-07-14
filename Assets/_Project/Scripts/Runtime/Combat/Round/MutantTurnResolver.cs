using System.Collections.Generic;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using Technopath.Combat.Events;

namespace Technopath.Combat.Round
{
    public sealed class MutantTurnResolver
    {
        public IReadOnlyList<MutantActionResult> Resolve(
            BattlefieldModel battlefield,
            PlayerTurnModel combatState,
            IReadOnlyList<MutantIntent> intents)
        {
            var results = new List<MutantActionResult>(intents.Count);
            foreach (var intent in intents)
            {
                if (!combatState.CanUnitAct(intent.MutantId) ||
                    !battlefield.Enemy.TryFindUnit(intent.MutantId, out var origin))
                    continue;

                var attack = ResolveAttack(battlefield, combatState, intent, origin.Row);
                GridPosition? destination = null;
                if (intent.PlannedDestination.HasValue &&
                    battlefield.Enemy[intent.PlannedDestination.Value].IsEmpty)
                {
                    destination = intent.PlannedDestination.Value;
                    battlefield.Enemy.MoveUnit(origin, destination.Value);
                    combatState.Events.Enqueue(new CombatEvent(CombatEventKind.Movement, intent.MutantId));
                    combatState.NotifyUnitMoved(intent.MutantId);
                }

                results.Add(new MutantActionResult(intent.MutantId, attack, origin, destination));
            }
            return results;
        }

        private static AutoAttackResult ResolveAttack(
            BattlefieldModel battlefield,
            PlayerTurnModel combatState,
            MutantIntent intent,
            int row)
        {
            for (var column = GridPosition.Size - 1; column >= 0; column--)
            {
                var cell = battlefield.Player[new GridPosition(row, column)];
                if (cell.Occupancy != CellOccupancyKind.Unit)
                    continue;

                var targetId = cell.OccupantId;
                return combatState.ResolveDirectAttack(intent.MutantId, targetId, intent.AttackDamage, row);
            }
            return new AutoAttackResult(intent.MutantId, null, 0, row);
        }
    }
}
