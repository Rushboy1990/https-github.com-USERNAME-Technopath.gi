using System.Collections.Generic;
using NUnit.Framework;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;
using Technopath.Combat.Statuses;

namespace Technopath.Tests.EditMode
{
    public sealed class CombatStatusRuntimeTests
    {
        [Test]
        public void Damage_DealsChargeScaledDamageAtConfiguredMoment()
        {
            var (turn, statuses) = CreateCombat();
            statuses.Add("robot", "poison", 2, 2, StatusTickMoment.PhaseStarted,
                effectKind: StatusEffectKind.Damage);

            turn.ResolveStatusPhaseStart(BoardSide.Player);

            Assert.That(turn.GetUnit("robot").Health, Is.EqualTo(16));
            Assert.That(statuses.GetActive("robot")[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void BonusDamageTaken_AugmentsAndConsumesOnNextAttack()
        {
            var (turn, statuses) = CreateCombat();
            statuses.Add("mutant", "target-lock", 2, 2, StatusTickMoment.NextAttack,
                effectKind: StatusEffectKind.BonusDamageTaken);

            turn.ResolveDirectAttack("robot", "mutant", 2, 0);

            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(14));
            Assert.That(statuses.GetActive("mutant")[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void ShieldReduction_RemovesShieldWithoutDamagingHealth()
        {
            var (turn, statuses) = CreateCombat(playerShield: 5);
            statuses.Add("robot", "acid", 2, 2, StatusTickMoment.PhaseStarted,
                effectKind: StatusEffectKind.ShieldReduction);

            turn.ResolveStatusPhaseStart(BoardSide.Player);

            Assert.That(turn.GetUnit("robot").Shield, Is.EqualTo(1));
            Assert.That(turn.GetUnit("robot").Health, Is.EqualTo(20));
        }

        [Test]
        public void IgnoreAttack_PreventsEntireIncomingAttackAndConsumesOneCharge()
        {
            var (turn, statuses) = CreateCombat();
            statuses.Add("robot", "force-field", 2, 1, StatusTickMoment.PhaseStarted,
                effectKind: StatusEffectKind.IgnoreAttack);

            var attack = turn.ResolveDirectAttack("mutant", "robot", 7, 0);

            Assert.That(attack.DamageResult.HealthDamage, Is.Zero);
            Assert.That(turn.GetUnit("robot").Health, Is.EqualTo(20));
            Assert.That(statuses.GetActive("robot")[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void BonusAttackDamage_AppliesUntilOwnerPhaseEnds()
        {
            var (turn, statuses) = CreateCombat();
            statuses.Add("robot", "charged-attack", 2, 3, StatusTickMoment.PhaseEnded,
                effectKind: StatusEffectKind.BonusAttackDamage);

            turn.ResolveDirectAttack("robot", "mutant", 2, 0);
            turn.ResolveStatusPhaseEnd(BoardSide.Player, new HashSet<string>());

            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(12));
            Assert.That(statuses.GetActive("robot")[0].Charges, Is.EqualTo(1));
        }

        [Test]
        public void Stun_BlocksActionsUntilOwnerPhaseEnds()
        {
            var (turn, statuses) = CreateCombat();
            statuses.Add("robot", "stun", 1, 1, StatusTickMoment.PhaseEnded,
                effectKind: StatusEffectKind.Stun);

            Assert.That(turn.CanUnitAct("robot"), Is.False);
            turn.ResolveStatusPhaseEnd(BoardSide.Player, new HashSet<string>());
            Assert.That(turn.CanUnitAct("robot"), Is.True);
        }

        private static (PlayerTurnModel turn, StatusCollection statuses) CreateCombat(int playerShield = 0)
        {
            var field = new BattlefieldModel();
            field.Player.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "robot");
            field.Enemy.TryOccupy(new GridPosition(0, 0), CellOccupancyKind.Unit, "mutant");
            var enemies = new Dictionary<string, CombatUnitSetup> { ["mutant"] = new(20, 2, 0) };
            var players = new Dictionary<string, CombatUnitSetup> { ["robot"] = new(20, 2, playerShield) };
            var turn = new PlayerTurnModel(field, 3, null, null, null, enemies, 0, players);
            var runtime = new CombatStatusRuntime(turn);
            turn.AttachStatusRuntime(runtime);
            return (turn, runtime.Statuses);
        }
    }
}
