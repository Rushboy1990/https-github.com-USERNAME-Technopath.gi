using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Statuses;

namespace Technopath.Tests.EditMode
{
    public sealed class AbilityAndStatusTests
    {
        [Test]
        public void OncePerPhase_CanTriggerAgainOnlyAfterNewPhase()
        {
            var tracker = new AbilityUsageTracker();
            tracker.BeginPhase();

            Assert.That(tracker.TryUse("robot:ability", AbilityFrequency.OncePerPhase), Is.True);
            Assert.That(tracker.TryUse("robot:ability", AbilityFrequency.OncePerPhase), Is.False);

            tracker.BeginPhase();
            Assert.That(tracker.TryUse("robot:ability", AbilityFrequency.OncePerPhase), Is.True);
        }

        [Test]
        public void OncePerAction_ResetsWithoutResettingCombatLimit()
        {
            var tracker = new AbilityUsageTracker();
            Assert.That(tracker.TryUse("action", AbilityFrequency.OncePerAction), Is.True);
            Assert.That(tracker.TryUse("combat", AbilityFrequency.OncePerCombat), Is.True);

            tracker.BeginAction();

            Assert.That(tracker.TryUse("action", AbilityFrequency.OncePerAction), Is.True);
            Assert.That(tracker.TryUse("combat", AbilityFrequency.OncePerCombat), Is.False);
        }

        [Test]
        public void ChargedStatus_AppliesValueForEveryChargeThenRemovesOneCharge()
        {
            var wound = new ChargedStatusState("status.wound", charges: 2, valuePerCharge: 2,
                StatusTickMoment.UnitMoved);

            var firstDamage = wound.TryTick(StatusTickMoment.UnitMoved);
            var secondDamage = wound.TryTick(StatusTickMoment.UnitMoved);

            Assert.That(firstDamage, Is.EqualTo(4));
            Assert.That(secondDamage, Is.EqualTo(2));
            Assert.That(wound.IsExpired, Is.True);
        }

        [Test]
        public void ChargedStatus_DoesNotTickAtDifferentMoment()
        {
            var poison = new ChargedStatusState("status.poison", 2, 1, StatusTickMoment.PhaseStarted);

            Assert.That(poison.TryTick(StatusTickMoment.PhaseEnded), Is.Zero);
            Assert.That(poison.Charges, Is.EqualTo(2));
        }
    }
}
