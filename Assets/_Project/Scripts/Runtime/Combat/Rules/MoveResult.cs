using System.Collections.Generic;
using Technopath.Combat.Board;

namespace Technopath.Combat.Rules
{
    public sealed class MoveResult
    {
        public MoveResult(GridPosition from, GridPosition to, bool wasSwap, IReadOnlyList<AutoAttackResult> attacks)
        {
            From = from;
            To = to;
            WasSwap = wasSwap;
            Attacks = attacks;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public bool WasSwap { get; }
        public IReadOnlyList<AutoAttackResult> Attacks { get; }
    }
}
