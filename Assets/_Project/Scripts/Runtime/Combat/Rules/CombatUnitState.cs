using Technopath.Combat.Board;

namespace Technopath.Combat.Rules
{
    public sealed class CombatUnitState
    {
        public CombatUnitState(string id, BoardSide side, int health, int attackDamage, int maxShield = 0)
            : this(id, side, health, health, attackDamage, maxShield)
        {
        }

        public CombatUnitState(string id, BoardSide side, int maximumHealth, int currentHealth, int attackDamage, int maxShield = 0)
        {
            if (maximumHealth <= 0) throw new System.ArgumentOutOfRangeException(nameof(maximumHealth));
            if (maxShield < 0) throw new System.ArgumentOutOfRangeException(nameof(maxShield));
            Id = id;
            Side = side;
            MaxHealth = maximumHealth;
            Health = System.Math.Clamp(currentHealth, 0, maximumHealth);
            AttackDamage = attackDamage;
            MaxShield = maxShield;
            Shield = maxShield;
        }

        public string Id { get; }
        public BoardSide Side { get; }
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int AttackDamage { get; }
        public int MaxShield { get; }
        public int Shield { get; private set; }
        public bool IsAlive => Health > 0;

        public DamageResult TakeDamage(int damage)
        {
            if (damage < 0)
                throw new System.ArgumentOutOfRangeException(nameof(damage));

            var absorbed = System.Math.Min(Shield, damage);
            Shield -= absorbed;
            var healthDamage = damage - absorbed;
            Health = System.Math.Max(0, Health - healthDamage);
            return new DamageResult(damage, absorbed, healthDamage, !IsAlive);
        }

        public void RestoreShield() => Shield = MaxShield;

        public int AddShield(int amount)
        {
            if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            var previous = Shield;
            Shield = System.Math.Min(MaxShield, Shield + amount);
            return Shield - previous;
        }

        public int RemoveShield(int amount)
        {
            if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            var removed = System.Math.Min(Shield, amount);
            Shield -= removed;
            return removed;
        }
    }
}
