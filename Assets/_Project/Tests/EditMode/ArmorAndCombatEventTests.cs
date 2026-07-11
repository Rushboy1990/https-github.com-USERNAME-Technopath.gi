using NUnit.Framework;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Rules;

namespace Technopath.Tests.EditMode
{
    public sealed class ArmorAndCombatEventTests
    {
        [Test]
        public void Damage_IsAbsorbedByArmorBeforeHealth()
        {
            var unit = new CombatUnitState("robot", BoardSide.Player, health: 10, attackDamage: 2, maxArmor: 3);

            var result = unit.TakeDamage(5);

            Assert.That(result.AbsorbedByArmor, Is.EqualTo(3));
            Assert.That(result.HealthDamage, Is.EqualTo(2));
            Assert.That(unit.Armor, Is.Zero);
            Assert.That(unit.Health, Is.EqualTo(8));
        }

        [Test]
        public void PlayerArmor_RestoresAtBeginningOfNewTurn_ButMutantArmorDoesNot()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant");
            var turn = new PlayerTurnModel(field);
            turn.ApplyDamage("robot", 2);
            turn.ApplyDamage("mutant", 1);
            turn.FinishTurn();

            turn.BeginNewTurn();

            Assert.That(turn.GetUnit("robot").Armor, Is.EqualTo(3));
            Assert.That(turn.GetUnit("mutant").Armor, Is.EqualTo(1));
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
    }
}
