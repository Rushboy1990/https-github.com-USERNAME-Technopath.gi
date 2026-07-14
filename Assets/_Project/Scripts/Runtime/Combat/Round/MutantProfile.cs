using System;

namespace Technopath.Combat.Round
{
    public sealed class MutantProfile
    {
        public MutantProfile(string unitId, int priority, int attackDamage, string displayName = null, string roleName = null,
            int maximumHealth = 6, int maximumShield = 1)
        {
            UnitId = unitId;
            Priority = priority;
            AttackDamage = attackDamage;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? unitId : displayName;
            RoleName = string.IsNullOrWhiteSpace(roleName) ? "Mutant" : roleName;
            MaximumHealth = maximumHealth > 0 ? maximumHealth : throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            MaximumShield = maximumShield >= 0 ? maximumShield : throw new ArgumentOutOfRangeException(nameof(maximumShield));
        }

        public string UnitId { get; }
        public int Priority { get; }
        public int AttackDamage { get; }
        public string DisplayName { get; }
        public string RoleName { get; }
        public int MaximumHealth { get; }
        public int MaximumShield { get; }
    }
}
