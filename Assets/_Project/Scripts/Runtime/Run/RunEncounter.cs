using System;

namespace Technopath.Run
{
    public enum RunEncounterKind
    {
        Combat,
        Elite,
        Boss
    }

    public sealed class RunEncounter
    {
        public RunEncounter(string id, string displayName, RunEncounterKind kind, int difficulty,
            int rewardParts, int rewardModuleCount, int seed)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Encounter id is required.", nameof(id)) : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Kind = kind;
            Difficulty = Math.Max(1, difficulty);
            RewardParts = Math.Max(0, rewardParts);
            RewardModuleCount = Math.Max(0, rewardModuleCount);
            Seed = seed;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public RunEncounterKind Kind { get; }
        public int Difficulty { get; }
        public int RewardParts { get; }
        public int RewardModuleCount { get; }
        public int Seed { get; }
        public bool IsBoss => Kind == RunEncounterKind.Boss;
    }
}
