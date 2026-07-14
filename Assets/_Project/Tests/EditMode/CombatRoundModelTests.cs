using System.Collections.Generic;
using NUnit.Framework;
using Technopath.Combat.Board;
using Technopath.Combat.Round;
using Technopath.Combat.Rules;

namespace Technopath.Tests.EditMode
{
    public sealed class CombatRoundModelTests
    {
        [Test]
        public void TechnopathSetup_ConfiguresStatsAndPreservesRunHealth()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, StartingFormationFactory.TechnopathId);
            var initialHealth = new Dictionary<string, int>
            {
                [StartingFormationFactory.TechnopathId] = 17
            };

            var combat = new CombatRoundModel(field, new List<MutantProfile>(), seed: 1,
                archetypes: null, loadouts: null, initialHealth: initialHealth,
                technopathSetup: new CombatUnitSetup(30, 10, 3));
            var technopath = combat.PlayerTurn.GetUnit(StartingFormationFactory.TechnopathId);

            Assert.That(technopath.MaxHealth, Is.EqualTo(30));
            Assert.That(technopath.Health, Is.EqualTo(17));
            Assert.That(technopath.AttackDamage, Is.EqualTo(10));
            Assert.That(technopath.MaxShield, Is.EqualTo(3));
        }

        [Test]
        public void Round_PlayerActsFirstThenMutantsAttackAndMove()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, StartingFormationFactory.TechnopathId);
            field.Enemy.TryOccupy(new GridPosition(1, 1), CellOccupancyKind.Unit, "mutant");
            var combat = new CombatRoundModel(field,
                new List<MutantProfile> { new("mutant", 10, 3) }, seed: 5);
            var plannedDestination = combat.Intents[0].PlannedDestination;
            combat.PlayerTurn.ApplyDamage("mutant", 1);

            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.PlayerTurn));
            combat.FinishPlayerTurn();
            var actions = combat.ResolveMutantTurn(nextIntentSeed: 6);

            Assert.That(actions[0].Attack.TargetId, Is.EqualTo(StartingFormationFactory.TechnopathId));
            Assert.That(combat.PlayerTurn.GetUnit(StartingFormationFactory.TechnopathId).Health, Is.EqualTo(10));
            Assert.That(combat.PlayerTurn.GetUnit(StartingFormationFactory.TechnopathId).Shield, Is.EqualTo(3));
            Assert.That(combat.PlayerTurn.GetUnit("mutant").Shield, Is.EqualTo(1));
            Assert.That(actions[0].Destination, Is.EqualTo(plannedDestination));
            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.PlayerTurn));
            Assert.That(combat.RoundNumber, Is.EqualTo(2));
            Assert.That(combat.PlayerTurn.ActionPoints, Is.EqualTo(3));
        }

        [Test]
        public void MutantKilledBeforeItsTurn_DoesNotExecutePreparedIntent()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, StartingFormationFactory.TechnopathId);
            field.Enemy.TryOccupy(new GridPosition(1, 1), CellOccupancyKind.Unit, "mutant");
            var combat = new CombatRoundModel(field,
                new List<MutantProfile> { new("mutant", 10, 3) }, seed: 1);
            combat.PlayerTurn.ApplyDamage("mutant", 99);
            combat.FinishPlayerTurn();

            var actions = combat.ResolveMutantTurn(nextIntentSeed: 2);

            Assert.That(actions.Count, Is.Zero);
            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.Victory));
        }

        [Test]
        public void TechnopathDeath_IsDefeatAfterMutantChainCompletes()
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, StartingFormationFactory.TechnopathId);
            field.Enemy.TryOccupy(new GridPosition(1, 0), CellOccupancyKind.Unit, "first");
            field.Enemy.TryOccupy(new GridPosition(1, 2), CellOccupancyKind.Unit, "second");
            var profiles = new List<MutantProfile>
            {
                new("first", 1, 7),
                new("second", 2, 7)
            };
            var combat = new CombatRoundModel(field, profiles, seed: 3);
            combat.FinishPlayerTurn();

            var actions = combat.ResolveMutantTurn(nextIntentSeed: 4);

            Assert.That(actions.Count, Is.EqualTo(2));
            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.Defeat));
        }
    }
}
