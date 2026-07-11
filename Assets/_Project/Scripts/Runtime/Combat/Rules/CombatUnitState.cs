using Technopath.Combat.Board;

namespace Technopath.Combat.Rules
{
    public sealed class CombatUnitState
    {
        public CombatUnitState(string id, BoardSide side, int health, int attackDamage, int maxArmor = 0)
        {
            if (health <= 0) throw new System.ArgumentOutOfRangeException(nameof(health));
            if (maxArmor < 0) throw new System.ArgumentOutOfRangeException(nameof(maxArmor));
            Id = id;
            Side = side;
            MaxHealth = health;
            Health = health;
            AttackDamage = attackDamage;
            MaxArmor = maxArmor;
            Armor = maxArmor;
        }

        public string Id { get; }
        public BoardSide Side { get; }
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int AttackDamage { get; }
        public int MaxArmor { get; }
        public int Armor { get; private set; }
        public bool IsAlive => Health > 0;

        public DamageResult TakeDamage(int damage)
        {
            if (damage < 0)
                throw new System.ArgumentOutOfRangeException(nameof(damage));

            var absorbed = System.Math.Min(Armor, damage);
            Armor -= absorbed;
            var healthDamage = damage - absorbed;
            Health = System.Math.Max(0, Health - healthDamage);
            return new DamageResult(damage, absorbed, healthDamage, !IsAlive);
        }

        public void RestoreArmor() => Armor = MaxArmor;
    }
}
