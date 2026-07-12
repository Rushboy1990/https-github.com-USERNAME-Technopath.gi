using System;
using Technopath.Combat.Modules;

namespace Technopath.Run
{
    public sealed class CampRobotState
    {
        public CampRobotState(string id, RobotLoadout loadout, int health)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            var stats = loadout.CalculateStats();
            MaxHealth = stats.Health;
            Health = Math.Clamp(health, 1, MaxHealth);
        }

        public string Id { get; }
        public RobotLoadout Loadout { get; }
        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDamaged => Health < MaxHealth;

        public int Repair(int amount)
        {
            var previous = Health;
            Health = Math.Min(MaxHealth, Health + Math.Max(0, amount));
            return Health - previous;
        }

        public void RefreshMaximumHealth()
        {
            MaxHealth = Loadout.CalculateStats().Health;
            Health = Math.Min(Health, MaxHealth);
        }

        public void SetHealthAfterBattle(int health)
        {
            Health = Math.Clamp(health, 0, MaxHealth);
        }
    }
}
