using System.Collections.Generic;
using NUnit.Framework;
using Technopath.Combat.Modules;
using Technopath.Run;
using Technopath.Run.Rewards;
using Technopath.Combat.Archetypes;
using UnityEngine;

namespace Technopath.Tests
{
    public sealed class BattleRewardGeneratorTests
    {
        [Test]
        public void Grant_AddsModulesAndPartsToRunState()
        {
            var first = ScriptableObject.CreateInstance<RobotModuleDefinition>();
            var second = ScriptableObject.CreateInstance<RobotModuleDefinition>();
            var run = new RunState();

            var reward = new BattleRewardGenerator().Grant(run,
                new List<RobotModuleDefinition> { first, second }, 3, 4, 12);

            Assert.That(reward.Modules, Has.Count.EqualTo(3));
            Assert.That(run.ModuleInventory, Has.Count.EqualTo(3));
            Assert.That(run.Parts, Is.EqualTo(4));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void EquipAndUnequip_MoveModuleBetweenInventoryAndRobot()
        {
            var archetype = ScriptableObject.CreateInstance<RobotArchetypeDefinition>();
            var module = ScriptableObject.CreateInstance<RobotModuleDefinition>();
            SetField(module, "slotType", ModuleSlotType.Core);
            var loadout = new RobotLoadout(archetype);
            var robot = new CampRobotState("robot", loadout, 1);
            var run = new RunState();
            run.AddRobot(robot);
            run.AddModule(module);

            Assert.That(run.TryEquip(robot, module, ModuleSlotType.Core), Is.True);
            Assert.That(run.ModuleInventory, Is.Empty);
            Assert.That(loadout.Core, Is.SameAs(module));
            Assert.That(run.TryUnequip(robot, ModuleSlotType.Core), Is.True);
            Assert.That(run.ModuleInventory, Has.Count.EqualTo(1));
            Object.DestroyImmediate(archetype);
            Object.DestroyImmediate(module);
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(target, value);
    }
}
