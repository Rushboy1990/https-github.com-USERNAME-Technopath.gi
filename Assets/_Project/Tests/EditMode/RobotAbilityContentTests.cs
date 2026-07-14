using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using UnityEditor;

namespace Technopath.Tests.EditMode
{
    public sealed class RobotAbilityContentTests
    {
        private const string RobotsFolder = "Assets/_Project/Content/Definitions/Robots";

        [Test]
        public void EveryRobotModel_HasOneCompleteMatchingAbilityDefinition()
        {
            var guids = AssetDatabase.FindAssets("t:RobotArchetypeDefinition", new[] { RobotsFolder });
            Assert.That(guids, Has.Length.EqualTo(10));
            var abilities = new HashSet<CombatAbilityDefinition>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var robot = AssetDatabase.LoadAssetAtPath<RobotArchetypeDefinition>(path);
                Assert.That(robot.AbilityDefinition, Is.Not.Null, $"{robot.name} has no Ability Definition.");
                Assert.That(robot.HasConsistentAbilityConfiguration, Is.True,
                    $"{robot.name} has mismatching legacy and content ability kinds.");
                Assert.That(robot.AbilityKind, Is.Not.EqualTo(RobotAbilityKind.None),
                    $"{robot.name} has no special ability kind.");
                Assert.That(robot.AbilityName, Is.Not.Empty, $"{robot.name} ability has no display name.");
                Assert.That(robot.AbilityRulesText, Is.Not.Empty, $"{robot.name} ability has no rules text.");
                Assert.That(abilities.Add(robot.AbilityDefinition), Is.True,
                    $"{robot.name} shares an ability definition with another model.");
            }
        }

        [Test]
        public void AbilityExecutionMode_MatchesItsRuntimeImplementation()
        {
            var robots = AssetDatabase.FindAssets("t:RobotArchetypeDefinition", new[] { RobotsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RobotArchetypeDefinition>)
                .ToArray();

            foreach (var robot in robots)
            {
                var ability = robot.AbilityDefinition;
                var shouldResolveAutomatically = robot.AbilityKind == RobotAbilityKind.RegenerativeArmor;
                Assert.That(ability.AutomaticallyResolveEffects, Is.EqualTo(shouldResolveAutomatically),
                    $"{robot.name} has the wrong ability execution mode.");
            }

            var emitter = robots.Single(robot => robot.AbilityKind == RobotAbilityKind.ForceFieldPulse);
            Assert.That(emitter.AbilityDefinition.Effects.Count(effect => effect != null &&
                effect.Kind == CombatAbilityEffectKind.ApplyStatus && effect.Status != null), Is.EqualTo(1));
        }

        [TestCase(RobotAbilityKind.CoordinateStrike, 25)]
        [TestCase(RobotAbilityKind.PhaseVolley, 10)]
        [TestCase(RobotAbilityKind.DepartureStrike, 10)]
        [TestCase(RobotAbilityKind.MomentumBarrage, 10)]
        [TestCase(RobotAbilityKind.RegenerativeArmor, 5)]
        [TestCase(RobotAbilityKind.ForceFieldPulse, 1)]
        public void NumericAbility_UsesConfiguredEffectValue(RobotAbilityKind kind, int expectedValue)
        {
            var robot = AssetDatabase.FindAssets("t:RobotArchetypeDefinition", new[] { RobotsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<RobotArchetypeDefinition>)
                .Single(candidate => candidate.AbilityKind == kind);

            Assert.That(robot.EffectValue, Is.EqualTo(expectedValue));
        }
    }
}
