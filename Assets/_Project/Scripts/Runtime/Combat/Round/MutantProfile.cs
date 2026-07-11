namespace Technopath.Combat.Round
{
    public sealed class MutantProfile
    {
        public MutantProfile(string unitId, int priority, int attackDamage)
        {
            UnitId = unitId;
            Priority = priority;
            AttackDamage = attackDamage;
        }

        public string UnitId { get; }
        public int Priority { get; }
        public int AttackDamage { get; }
    }
}
