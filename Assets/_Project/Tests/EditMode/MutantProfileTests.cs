using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Round;
using UnityEngine;

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

        [Test]
        public void Definition_CreatesProfileWithConfiguredStatsAndEncounterBonus()
        {
            var definition = ScriptableObject.CreateInstance<MutantDefinition>();
            SetField(definition, "displayName", "Панцирь");
            SetField(definition, "roleName", "Защитник");
            SetField(definition, "maximumHealth", 20);
            SetField(definition, "maximumShield", 5);
            SetField(definition, "attackDamage", 5);

            var profile = definition.CreateProfile("mutant-1", 10, damageBonus: 2);

            Assert.That(profile.DisplayName, Is.EqualTo("Панцирь"));
            Assert.That(profile.RoleName, Is.EqualTo("Защитник"));
            Assert.That(profile.MaximumHealth, Is.EqualTo(20));
            Assert.That(profile.MaximumShield, Is.EqualTo(5));
            Assert.That(profile.AttackDamage, Is.EqualTo(7));

            Object.DestroyImmediate(definition);
        }

        private static void SetField<T>(MutantDefinition definition, string name, T value)
        {
            var field = typeof(MutantDefinition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(definition, value);
        }
    }
}
