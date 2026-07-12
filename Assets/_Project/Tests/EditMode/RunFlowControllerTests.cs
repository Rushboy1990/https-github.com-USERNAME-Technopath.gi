using NUnit.Framework;
using Technopath.Run;

namespace Technopath.Tests.EditMode
{
    public sealed class RunFlowControllerTests
    {
        [Test]
        public void ShortRun_OffersThreeChoicesThenBossAndCompletes()
        {
            var flow = new RunFlowController(new RunState(), 17);

            for (var battle = 0; battle < RunFlowController.TestRegularBattleCount; battle++)
            {
                flow.CompleteBattle();
                Assert.That(flow.State.Phase, Is.EqualTo(RunPhase.Reward));
                flow.EnterRestStop();
                var choices = flow.CreateRouteChoices();
                if (battle < RunFlowController.TestRegularBattleCount - 1)
                {
                    Assert.That(choices.Count, Is.EqualTo(3));
                    flow.SelectEncounter(choices[0]);
                }
                else
                {
                    Assert.That(choices.Count, Is.EqualTo(1));
                    Assert.That(choices[0].IsBoss, Is.True);
                    flow.SelectEncounter(choices[0]);
                }
            }

            flow.CompleteBattle();

            Assert.That(flow.State.Phase, Is.EqualTo(RunPhase.Victory));
            Assert.That(flow.State.CompletedBattles, Is.EqualTo(4));
        }

        [Test]
        public void Defeat_EndsRunWithoutChangingCompletedBattleCount()
        {
            var flow = new RunFlowController(new RunState(), 17);
            flow.Defeat();
            Assert.That(flow.State.Phase, Is.EqualTo(RunPhase.Defeat));
            Assert.That(flow.State.CompletedBattles, Is.Zero);
        }
    }
}
