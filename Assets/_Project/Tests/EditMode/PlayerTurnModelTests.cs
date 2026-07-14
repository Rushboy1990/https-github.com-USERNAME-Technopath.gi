using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using UnityEngine;

namespace Technopath.Tests.EditMode
{
    public sealed class PlayerTurnModelTests
    {
        [Test]
        public void MoveToEmptyCell_CostsOneActionPointAndAttacksFirstEnemyInRow()
        {
            var field = CreateField();
            var turn = new PlayerTurnModel(field);

            var result = turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));

            Assert.That(turn.ActionPoints, Is.EqualTo(2));
            Assert.That(field.Player[new GridPosition(0, 1)].OccupantId, Is.EqualTo("robot-a"));
            Assert.That(result.Attacks, Has.Count.EqualTo(1));
            Assert.That(result.Attacks[0].TargetId, Is.EqualTo("mutant-near"));
            Assert.That(turn.GetUnit("mutant-near").Health, Is.EqualTo(6));
            Assert.That(turn.GetUnit("mutant-near").Shield, Is.Zero);
        }

        [Test]
        public void Swap_AttacksInInitiatorThenDisplacedOrder()
        {
            var field = CreateField();
            field.Player.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "robot-b");
            var turn = new PlayerTurnModel(field);

            var result = turn.Move(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(result.WasSwap, Is.True);
            Assert.That(result.Attacks[0].AttackerId, Is.EqualTo("robot-a"));
            Assert.That(result.Attacks[0].TargetId, Is.EqualTo("mutant-middle"));
            Assert.That(result.Attacks[0].FiringRow, Is.EqualTo(1));
            Assert.That(result.Attacks[1].AttackerId, Is.EqualTo("robot-b"));
            Assert.That(result.Attacks[1].TargetId, Is.EqualTo("mutant-near"));
            Assert.That(result.Attacks[1].FiringRow, Is.EqualTo(0));
        }

        [Test]
        public void DisplacedUnit_CanStillInitiateItsOwnMove()
        {
            var field = CreateField();
            field.Player.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "robot-b");
            var turn = new PlayerTurnModel(field);
            turn.Move(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(turn.CanMove(new GridPosition(0, 0), new GridPosition(0, 1)), Is.True);
            Assert.DoesNotThrow(() => turn.Move(new GridPosition(0, 0), new GridPosition(0, 1)));
        }

        [Test]
        public void Initiator_CannotInitiateTwiceInOneTurn()
        {
            var field = CreateField();
            var turn = new PlayerTurnModel(field);
            turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));

            Assert.That(turn.CanMove(new GridPosition(0, 1), new GridPosition(0, 2)), Is.False);
        }

        [Test]
        public void FinishTurn_BurnsRemainingActionPoints()
        {
            var turn = new PlayerTurnModel(CreateField());

            turn.FinishTurn();

            Assert.That(turn.ActionPoints, Is.Zero);
            Assert.That(turn.IsFinished, Is.True);
        }

        [Test]
        public void AccumulatorFinishAttack_UsesAbilityEffectValue()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "accumulator");
            field.Enemy.TryOccupy(new GridPosition(2, 0), CellOccupancyKind.Unit, "mutant");
            var ability = ScriptableObject.CreateInstance<CombatAbilityDefinition>();
            SetPrivateField(ability, "effectValue", 7);
            SetPrivateField(ability, "abilityKind", RobotAbilityKind.MomentumBarrage);
            var archetype = ScriptableObject.CreateInstance<RobotArchetypeDefinition>();
            SetPrivateField(archetype, "abilityKind", RobotAbilityKind.MomentumBarrage);
            SetPrivateField(archetype, "abilityDefinition", ability);
            SetPrivateField(archetype, "autoAttackDamage", 1);
            var archetypes = new Dictionary<string, RobotArchetypeDefinition> { ["accumulator"] = archetype };
            var enemies = new Dictionary<string, CombatUnitSetup> { ["mutant"] = new(50, 1, 0) };
            var turn = new PlayerTurnModel(field, 3, archetypes, null, null, enemies);

            turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));
            turn.FinishTurn();

            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(43));
            Object.DestroyImmediate(archetype);
            Object.DestroyImmediate(ability);
        }

        [Test]
        public void NewTurn_RestoresActionPointsAndIndependentActivations()
        {
            var field = CreateField();
            var turn = new PlayerTurnModel(field);
            turn.Move(new GridPosition(0, 0), new GridPosition(0, 1));
            turn.FinishTurn();

            turn.BeginNewTurn();

            Assert.That(turn.ActionPoints, Is.EqualTo(3));
            Assert.That(turn.CanMove(new GridPosition(0, 1), new GridPosition(0, 2)), Is.True);
        }

        private static BattlefieldModel CreateField()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot-a");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant-near");
            field.Enemy.TryOccupy(new GridPosition(0, 2), CellOccupancyKind.Unit, "mutant-far");
            field.Enemy.TryOccupy(new GridPosition(1, 1), CellOccupancyKind.Unit, "mutant-middle");
            return field;
        }

        private static void SetPrivateField<T>(object target, string name, T value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field: {name}");
            field.SetValue(target, value);
        }
    }
}
