using Technopath.Combat.Board;
using Technopath.Combat.Rules;

namespace Technopath.Combat.Round
{
    public sealed class MutantActionResult
    {
        public MutantActionResult(string mutantId, AutoAttackResult attack, GridPosition origin, GridPosition? destination)
        {
            MutantId = mutantId;
            Attack = attack;
            Origin = origin;
            Destination = destination;
        }

        public string MutantId { get; }
        public AutoAttackResult Attack { get; }
        public GridPosition Origin { get; }
        public GridPosition? Destination { get; }
    }
}
