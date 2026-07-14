using NUnit.Framework;
using Technopath.Combat.Content;
using UnityEditor;

namespace Technopath.Tests.EditMode
{
    public sealed class TechnopathContentTests
    {
        private const string DefinitionPath =
            "Assets/_Project/Content/Definitions/Technopath/Technopath.asset";

        [Test]
        public void Definition_HasExpectedCombatStats()
        {
            var definition = AssetDatabase.LoadAssetAtPath<TechnopathDefinition>(DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Id, Is.EqualTo("technopath"));
            Assert.That(definition.MaximumHealth, Is.EqualTo(30));
            Assert.That(definition.AutoAttackDamage, Is.EqualTo(10));
            Assert.That(definition.MaximumShield, Is.EqualTo(3));
        }
    }
}
