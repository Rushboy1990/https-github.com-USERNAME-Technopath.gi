using System.Reflection;
using NUnit.Framework;
using Technopath.Combat.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Technopath.Tests.EditMode
{
    public sealed class CombatDebugSpawnMenuTests : InputTestFixture
    {
        [Test]
        public void F2_TogglesSpawnMenu()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gameObject = new GameObject("CombatDebugSpawnMenuTest");
            var menu = gameObject.AddComponent<CombatDebugSpawnMenu>();

            Press(keyboard.f2Key);
            InvokeUpdate(menu);

            Assert.That(IsOpen(menu), Is.True);

            Release(keyboard.f2Key);
            Press(keyboard.f2Key);
            InvokeUpdate(menu);

            Assert.That(IsOpen(menu), Is.False);
            Object.DestroyImmediate(gameObject);
        }

        private static void InvokeUpdate(CombatDebugSpawnMenu menu)
        {
            var method = typeof(CombatDebugSpawnMenu).GetMethod("Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(menu, null);
        }

        private static bool IsOpen(CombatDebugSpawnMenu menu)
        {
            var field = typeof(CombatDebugSpawnMenu).GetField("_open",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(menu);
        }
    }
}
