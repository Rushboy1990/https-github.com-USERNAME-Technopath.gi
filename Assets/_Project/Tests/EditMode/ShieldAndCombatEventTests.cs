using NUnit.Framework;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Rules;

namespace Technopath.Tests.EditMode
{
    public sealed class ShieldAndCombatEventTests
    {
        [Test]
        public void Damage_IsAbsorbedByShieldBeforeHealth()
        {
            var unit = new CombatUnitState("robot", BoardSide.Player, health: 10, attackDamage: 2, maxShield: 3);

            var result = unit.TakeDamage(5);

            Assert.That(result.AbsorbedByShield, Is.EqualTo(3));
            Assert.That(result.HealthDamage, Is.EqualTo(2));
            Assert.That(unit.Shield, Is.Zero);
            Assert.That(unit.Health, Is.EqualTo(8));
        }

        [Test]
        public void PlayerShield_RestoresAtBeginningOfNewTurn()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant");
            var turn = new PlayerTurnModel(field);
            turn.ApplyDamage("robot", 2);
            turn.ApplyDamage("mutant", 1);
            turn.FinishTurn();

            turn.BeginNewTurn();

            Assert.That(turn.GetUnit("robot").Shield, Is.EqualTo(3));
            Assert.That(turn.GetUnit("mutant").Shield, Is.EqualTo(1));
        }

        [Test]
        public void DamageEvent_ReportsOnlyHealthDamage()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot");
            var turn = new PlayerTurnModel(field);
            turn.Events.Clear();

            turn.ApplyDamageDetailed("robot", 1);
            CombatEvent damageEvent = null;
            foreach (var combatEvent in turn.Events.Drain())
            {
                if (combatEvent.Kind == CombatEventKind.Damage)
                {
                    damageEvent = combatEvent;
                    break;
                }
            }

            Assert.That(damageEvent.Value, Is.Zero);
        }

        [Test]
        public void EventQueue_RejectsUnboundedReactionChain()
        {
            var queue = new CombatEventQueue(maximumEventsPerChain: 2);
            queue.Enqueue(new CombatEvent(CombatEventKind.Attack));
            queue.Enqueue(new CombatEvent(CombatEventKind.Damage));

            Assert.Throws<System.InvalidOperationException>(() =>
                queue.Enqueue(new CombatEvent(CombatEventKind.Kill)));
        }

        [Test]
        public void EventQueue_PreservesDeterministicInsertionOrder()
        {
            var queue = new CombatEventQueue();
            queue.Enqueue(new CombatEvent(CombatEventKind.Attack));
            queue.Enqueue(new CombatEvent(CombatEventKind.Damage));

            Assert.That(queue.Dequeue().Kind, Is.EqualTo(CombatEventKind.Attack));
            Assert.That(queue.Dequeue().Kind, Is.EqualTo(CombatEventKind.Damage));
        }

        [Test]
        public void PlayerMove_ProducesMovementAttackAndDamageEventsInOrder()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant");
            var turn = new PlayerTurnModel(field);
            turn.Events.Clear();

            turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));
            var events = turn.Events.Drain();

            Assert.That(events[0].Kind, Is.EqualTo(CombatEventKind.Movement));
            Assert.That(events[1].Kind, Is.EqualTo(CombatEventKind.Attack));
            Assert.That(events[2].Kind, Is.EqualTo(CombatEventKind.Damage));
        }

        [Test]
        public void AddShield_IsCappedByMaximumShield()
        {
            var unit = new CombatUnitState("robot", BoardSide.Player, 10, 2, maxShield: 3);
            unit.TakeDamage(2);

            Assert.That(unit.AddShield(10), Is.EqualTo(2));
            Assert.That(unit.Shield, Is.EqualTo(3));
        }
    }
}
