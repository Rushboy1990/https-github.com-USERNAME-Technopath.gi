using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Modules;
using UnityEngine;

namespace Technopath.Tests.EditMode
{
    public sealed class RobotLoadoutTests
    {
        [Test]
        public void Loadout_HasFiveOptionalSlotsAndCombinesStats()
        {
            var archetype = CreateArchetype(ArchetypeRole.Attacker, 8, 1, 3);
            var loadout = new RobotLoadout(archetype);
            var core = CreateModule("core", ModuleSlotType.Core, 2, 0, 1);
            var processor = CreateModule("processor", ModuleSlotType.Processor, 0, 2, 0);
            var modifier = CreateModule("modifier", ModuleSlotType.Modifier, 1, 1, 1);

            Assert.That(loadout.TryEquip(core), Is.True);
            Assert.That(loadout.TryEquip(processor), Is.True);
            Assert.That(loadout.TryEquip(modifier, 0), Is.True);
            Assert.That(loadout.TryEquip(modifier, 1), Is.True);
            Assert.That(loadout.TryEquip(modifier, 2), Is.True);
            var stats = loadout.CalculateStats();

            Assert.That(stats.Health, Is.EqualTo(13));
            Assert.That(stats.Shield, Is.EqualTo(6));
            Assert.That(stats.Attack, Is.EqualTo(7));
            Assert.That(stats.Sources.Count, Is.EqualTo(6));
        }

        [Test]
        public void IncompatibleModule_IsRejectedWithoutChangingSlot()
        {
            var archetype = CreateArchetype(ArchetypeRole.Defender, 10, 4, 1);
            var attackerCore = CreateModule("attacker-core", ModuleSlotType.Core, 0, 0, 2,
                new[] { ArchetypeRole.Attacker });
            var loadout = new RobotLoadout(archetype);

            Assert.That(loadout.TryEquip(attackerCore), Is.False);
            Assert.That(loadout.Core, Is.Null);
        }

        [Test]
        public void EmptySlots_AreValidAndKeepArchetypeStats()
        {
            var archetype = CreateArchetype(ArchetypeRole.Support, 9, 2, 2);

            var stats = new RobotLoadout(archetype).CalculateStats();

            Assert.That(stats.Health, Is.EqualTo(9));
            Assert.That(stats.Shield, Is.EqualTo(2));
            Assert.That(stats.Attack, Is.EqualTo(2));
        }

        [Test]
        public void CoreOverridesPrimaryAbility_AndProcessorAddsUtilityAbility()
        {
            var archetype = CreateArchetype(ArchetypeRole.Attacker, 8, 1, 3);
            Set(archetype, "abilityName", "Archetype Ability");
            var core = CreateModule("core", ModuleSlotType.Core, 0, 0, 0);
            Set(core, "abilityName", "Core Ability");
            Set(core, "abilityRulesText", "Replacement rules");
            var processor = CreateModule("cpu", ModuleSlotType.Processor, 0, 0, 0);
            Set(processor, "abilityName", "Utility Ability");
            var loadout = new RobotLoadout(archetype);
            loadout.TryEquip(core);
            loadout.TryEquip(processor);

            Assert.That(loadout.GetPrimaryAbility().Name, Is.EqualTo("Core Ability"));
            Assert.That(loadout.GetProcessorAbility().Name, Is.EqualTo("Utility Ability"));
        }

        private static RobotArchetypeDefinition CreateArchetype(ArchetypeRole role, int health, int shield, int attack)
        {
            var definition = ScriptableObject.CreateInstance<RobotArchetypeDefinition>();
            Set(definition, "id", $"robot.{role.ToString().ToLowerInvariant()}");
            Set(definition, "displayName", role.ToString());
            Set(definition, "role", role);
            Set(definition, "maximumHealth", health);
            Set(definition, "maximumShield", shield);
            Set(definition, "autoAttackDamage", attack);
            return definition;
        }

        private static RobotModuleDefinition CreateModule(string name, ModuleSlotType slot, int health, int shield,
            int attack, ArchetypeRole[] roles = null)
        {
            var definition = ScriptableObject.CreateInstance<RobotModuleDefinition>();
            Set(definition, "id", $"module.{name}");
            Set(definition, "displayName", name);
            Set(definition, "slotType", slot);
            Set(definition, "rarity", ModuleRarity.Rare);
            Set(definition, "level", 2);
            Set(definition, "healthModifier", health);
            Set(definition, "shieldModifier", shield);
            Set(definition, "attackModifier", attack);
            Set(definition, "compatibleRoles", roles ?? System.Array.Empty<ArchetypeRole>());
            return definition;
        }

        private static void Set<T>(object target, string fieldName, T value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
        }
    }
}
