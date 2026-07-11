using Technopath.Combat.Board;

namespace Technopath.Combat.Round
{
    public sealed class MutantIntent
    {
        public MutantIntent(string mutantId, int priority, int attackDamage, GridPosition? plannedDestination, int tieBreaker)
        {
            MutantId = mutantId;
            Priority = priority;
            AttackDamage = attackDamage;
            PlannedDestination = plannedDestination;
            TieBreaker = tieBreaker;
        }

        public string MutantId { get; }
        public int Priority { get; }
        public int AttackDamage { get; }
        public GridPosition? PlannedDestination { get; }
        internal int TieBreaker { get; }
    }
}
