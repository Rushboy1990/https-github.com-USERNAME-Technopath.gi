using System;

namespace Technopath.Combat.Rules
{
    public sealed class CombatUnitSetup
    {
        public CombatUnitSetup(int maximumHealth, int attackDamage, int maximumShield = 0)
        {
            if (maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (maximumShield < 0) throw new ArgumentOutOfRangeException(nameof(maximumShield));
            MaximumHealth = maximumHealth;
            AttackDamage = attackDamage;
            MaximumShield = maximumShield;
        }

        public int MaximumHealth { get; }
        public int AttackDamage { get; }
        public int MaximumShield { get; }
    }
}
