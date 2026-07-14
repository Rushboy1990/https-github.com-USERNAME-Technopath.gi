using NUnit.Framework;
using Technopath.Combat.Round;

namespace Technopath.Tests.EditMode
{
    public sealed class MutantProfileTests
    {
        [Test]
        public void Profile_UsesDefaultCombatStatsForLegacyConstruction()
        {
            var profile = new MutantProfile("mutant", 10, 2);

            Assert.That(profile.MaximumHealth, Is.EqualTo(6));
            Assert.That(profile.MaximumShield, Is.EqualTo(1));
            Assert.That(profile.DisplayName, Is.EqualTo("mutant"));
        }

        [Test]
        public void Profile_ExposesExplicitTypeAndCombatStats()
        {
            var profile = new MutantProfile("brute", 20, 3, "Brute", "Crushing strike", 9, 2);

            Assert.That(profile.DisplayName, Is.EqualTo("Brute"));
            Assert.That(profile.RoleName, Is.EqualTo("Crushing strike"));
            Assert.That(profile.MaximumHealth, Is.EqualTo(9));
            Assert.That(profile.MaximumShield, Is.EqualTo(2));
        }
    }
}
