using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Board;
using Technopath.Combat.Events;
using Technopath.Combat.Presentation;
using Technopath.Combat.Rules;
using Technopath.Run;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Technopath.Tests.PlayMode
{
    public sealed class CombatSandboxIntegrationTests : InputTestFixture
    {
        [UnitySetUp]
        public IEnumerator LoadCombatSandbox()
        {
            RunSession.Reset();
            yield return SceneManager.LoadSceneAsync("CombatSandbox", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmitterEvent_AppliesStatus_IgnoresAttack_AndRefreshesUi()
        {
            var presenter = Object.FindFirstObjectByType<BattlefieldPresenter>();
            Assert.That(presenter, Is.Not.Null);
            var archetypes = GetField<RobotArchetypeDefinition[]>(presenter, "testArchetypes");
            var emitter = archetypes.Single(definition => definition != null && definition.Id == "emitter");
            var views = GetField<Dictionary<string, UnitTokenView>>(presenter, "_unitViews");
            var originalCount = views.Count;

            presenter.TryDebugSpawn(emitter);

            Assert.That(views.Count, Is.EqualTo(originalCount + 1), "Debug spawn must create a real token view.");
            var emitterId = views.Keys.Single(id => id.StartsWith("debug-emitter-"));
            var turn = GetField<PlayerTurnModel>(presenter, "_turn");
            var runtime = GetField<CombatAbilityRuntime>(presenter, "_abilityRuntime");
            turn.Events.Enqueue(new CombatEvent(CombatEventKind.PhaseStarted));
            InvokePrivate(presenter, "AppendDetailedEvents");

            var protectedId = views.Keys.FirstOrDefault(id =>
                runtime.GetActiveStatuses(id).Any(status => status.Id == "status.force-field"));
            Assert.That(protectedId, Is.Not.Null, "Emitter must protect one living ally on phase start.");
            var chargesBefore = runtime.GetActiveStatuses(protectedId)
                .Single(status => status.Id == "status.force-field").Charges;
            var enemyId = views.Keys.First(id => turn.GetUnit(id).Side == BoardSide.Enemy);
            var protectedUnit = turn.GetUnit(protectedId);
            var healthBefore = protectedUnit.Health;
            var shieldBefore = protectedUnit.Shield;

            var attack = turn.ResolveDirectAttack(enemyId, protectedId, 50, 0);
            InvokePrivate(presenter, "AppendDetailedEvents");
            yield return null;

            Assert.That(attack.DamageResult.HealthDamage, Is.Zero);
            Assert.That(protectedUnit.Health, Is.EqualTo(healthBefore));
            Assert.That(protectedUnit.Shield, Is.EqualTo(shieldBefore));
            var chargesAfter = runtime.GetActiveStatuses(protectedId)
                .Where(status => status.Id == "status.force-field")
                .Select(status => status.Charges).DefaultIfEmpty(0).Single();
            Assert.That(chargesAfter, Is.EqualTo(chargesBefore - 1));
            Assert.That(presenter.DetailedCombatLog, Does.Contain("Status[status.force-field]"));
            Assert.That(GetTokenLabel(views[protectedId]), Does.Contain($"HP {healthBefore}/{protectedUnit.MaxHealth}"));
            Assert.That(views.ContainsKey(emitterId), Is.True);
        }

        [UnityTest]
        public IEnumerator F2_TogglesRealSceneDebugMenu()
        {
            var menu = Object.FindFirstObjectByType<CombatDebugSpawnMenu>();
            Assert.That(menu, Is.Not.Null);
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.f2Key);
            yield return null;

            Assert.That(GetField<bool>(menu, "_open"), Is.True);
        }

        private static string GetTokenLabel(UnitTokenView token) =>
            GetField<TextMesh>(token, "label").text;

        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            method.Invoke(target, null);
        }
    }
}
