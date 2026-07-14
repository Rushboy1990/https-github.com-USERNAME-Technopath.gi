using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Modules;
using Technopath.Combat.Rules;
using UnityEditor;
using UnityEngine;

namespace Technopath.Tests.EditMode
{
    public sealed class CombatAbilityRuntimeTests
    {
        [Test]
        public void InitialPhase_EmitterAppliesStatusWithoutPresenter()
        {
            var emitter = LoadRobot("Emitter");
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, "emitter");
            var archetypes = new Dictionary<string, RobotArchetypeDefinition> { ["emitter"] = emitter };
            var loadouts = new Dictionary<string, RobotLoadout> { ["emitter"] = new(emitter) };
            var turn = new PlayerTurnModel(field, PlayerTurnModel.StartingActionPoints, archetypes, loadouts);
            var runtime = new CombatAbilityRuntime(field, turn, archetypes, loadouts, randomSeed: 12);

            runtime.BeginPhase();
            var resolution = runtime.ResolvePendingEvents();

            Assert.That(runtime.GetActiveStatuses("emitter").Single().Id, Is.EqualTo("status.force-field"));
            Assert.That(resolution.Any(entry => entry.Kind == CombatResolutionEntryKind.Ability &&
                entry.UnitId == "emitter"), Is.True);
        }

        [Test]
        public void AutomaticArchetypeAbility_ResolvesWithoutPresenter()
        {
            var regenerator = LoadRobot("Regenerator");
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, "regenerator");
            var archetypes = new Dictionary<string, RobotArchetypeDefinition> { ["regenerator"] = regenerator };
            var loadouts = new Dictionary<string, RobotLoadout> { ["regenerator"] = new(regenerator) };
            var turn = new PlayerTurnModel(field, PlayerTurnModel.StartingActionPoints, archetypes, loadouts);
            turn.ApplyDamageDetailed("regenerator", 10);
            turn.Events.Enqueue(new CombatEvent(CombatEventKind.Attack, "mutant", "regenerator", 10));
            var runtime = new CombatAbilityRuntime(field, turn, archetypes, loadouts, randomSeed: 3);

            runtime.BeginPhase();
            runtime.ResolvePendingEvents();

            Assert.That(turn.GetUnit("regenerator").Shield, Is.EqualTo(regenerator.EffectValue));
        }

        [Test]
        public void CoreOverride_DisablesArchetypeSpecialAndReactiveAbilities()
        {
            var suppressor = LoadRobot("Suppressor");
            var core = ScriptableObject.CreateInstance<RobotModuleDefinition>();
            SetPrivateField(core, "slotType", ModuleSlotType.Core);
            SetPrivateField(core, "abilityName", "Replacement ability");
            var loadout = new RobotLoadout(suppressor);
            Assert.That(loadout.TryEquip(core), Is.True);
            var field = new BattlefieldModel();
            field.Player.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, "suppressor");
            field.Enemy.TryOccupy(GridPosition.Center, CellOccupancyKind.Unit, "mutant");
            var archetypes = new Dictionary<string, RobotArchetypeDefinition> { ["suppressor"] = suppressor };
            var loadouts = new Dictionary<string, RobotLoadout> { ["suppressor"] = loadout };
            var enemies = new Dictionary<string, CombatUnitSetup> { ["mutant"] = new(100, 1) };

            var turn = new PlayerTurnModel(field, PlayerTurnModel.StartingActionPoints, archetypes, loadouts,
                initialHealth: null, enemySetups: enemies);
            var runtime = new CombatAbilityRuntime(field, turn, archetypes, loadouts, randomSeed: 5);
            runtime.BeginPhase();
            var resolution = runtime.ResolvePendingEvents();

            Assert.That(loadout.HasPrimaryAbilityOverride, Is.True);
            Assert.That(loadout.PrimaryAbilityKind, Is.EqualTo(RobotAbilityKind.None));
            Assert.That(turn.GetUnit("mutant").Health, Is.EqualTo(100));
            Assert.That(resolution.Any(entry => entry.Kind == CombatResolutionEntryKind.Ability &&
                entry.UnitId == "suppressor"), Is.False);
            Object.DestroyImmediate(core);
        }

        private static RobotArchetypeDefinition LoadRobot(string assetName) =>
            AssetDatabase.LoadAssetAtPath<RobotArchetypeDefinition>(
                $"Assets/_Project/Content/Definitions/Robots/{assetName}.asset");

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
