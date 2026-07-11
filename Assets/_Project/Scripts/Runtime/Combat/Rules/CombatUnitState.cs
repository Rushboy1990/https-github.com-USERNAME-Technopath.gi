using Technopath.Combat.Board;

namespace Technopath.Combat.Rules
{
    public sealed class CombatUnitState
    {
        public CombatUnitState(string id, BoardSide side, int health, int attackDamage)
        {
            Id = id;
            Side = side;
            Health = health;
            AttackDamage = attackDamage;
        }

        public string Id { get; }
        public BoardSide Side { get; }
        public int Health { get; private set; }
        public int AttackDamage { get; }
        public bool IsAlive => Health > 0;

        public void TakeDamage(int damage)
        {
            if (damage < 0)
                throw new System.ArgumentOutOfRangeException(nameof(damage));
            Health = System.Math.Max(0, Health - damage);
        }
    }
}
