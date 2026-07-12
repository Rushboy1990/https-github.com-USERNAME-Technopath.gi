using System;
using System.Collections.Generic;

namespace Technopath.Run
{
    public sealed class RunFlowController
    {
        public const int TestRegularBattleCount = 3;

        public RunFlowController(RunState state, int seed)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Seed = seed;
            State.BeginEncounter(CreateOpeningEncounter());
        }

        public RunState State { get; }
        public int Seed { get; }

        public void EnterReward() => State.SetPhase(RunPhase.Reward);
        public void EnterRestStop()
        {
            State.RestoreTechnopathPercent(10);
            State.SetPhase(RunPhase.RestStop);
        }

        public IReadOnlyList<RunEncounter> CreateRouteChoices()
        {
            State.SetPhase(RunPhase.RouteSelection);
            if (State.CompletedBattles >= TestRegularBattleCount)
                return new[] { CreateBossEncounter() };

            var result = new List<RunEncounter>(3);
            for (var index = 0; index < 3; index++)
            {
                var elite = index == 2 && State.CompletedBattles > 0;
                var difficulty = 1 + State.CompletedBattles + (elite ? 1 : 0);
                result.Add(new RunEncounter(
                    $"node-{State.CompletedBattles + 1}-{index + 1}",
                    elite ? "Elite mutant pack" : $"Mutant pack {index + 1}",
                    elite ? RunEncounterKind.Elite : RunEncounterKind.Combat,
                    difficulty,
                    elite ? 8 : 4 + index,
                    elite ? 3 : 2,
                    Seed + State.CompletedBattles * 101 + index * 17));
            }
            return result;
        }

        public void SelectEncounter(RunEncounter encounter) => State.BeginEncounter(encounter);

        public void CompleteBattle()
        {
            State.CompleteCurrentEncounter();
            State.SetPhase(State.CurrentEncounter.IsBoss ? RunPhase.Victory : RunPhase.Reward);
        }

        public void Defeat() => State.SetPhase(RunPhase.Defeat);

        private RunEncounter CreateOpeningEncounter() =>
            new("opening-battle", "Opening mutant pack", RunEncounterKind.Combat, 1, 4, 2, Seed);

        private RunEncounter CreateBossEncounter() =>
            new("test-boss", "Hive guardian — test boss", RunEncounterKind.Boss, 5, 12, 3, Seed + 909);
    }
}
