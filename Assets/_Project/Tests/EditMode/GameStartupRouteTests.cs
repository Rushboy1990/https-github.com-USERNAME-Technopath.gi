using System;
using NUnit.Framework;
using Technopath.Bootstrap;

namespace Technopath.Tests
{
    public sealed class GameStartupRouteTests
    {
        [Test]
        public void RequiresSceneLoad_WhenScenesDiffer_ReturnsTrue()
        {
            Assert.That(GameStartupRoute.RequiresSceneLoad("Bootstrap", "CombatSandbox"), Is.True);
        }

        [Test]
        public void RequiresSceneLoad_WhenAlreadyAtTarget_ReturnsFalse()
        {
            Assert.That(GameStartupRoute.RequiresSceneLoad("CombatSandbox", "CombatSandbox"), Is.False);
        }

        [Test]
        public void RequiresSceneLoad_WhenTargetIsEmpty_Throws()
        {
            Assert.Throws<ArgumentException>(() => GameStartupRoute.RequiresSceneLoad("Bootstrap", ""));
        }
    }
}
