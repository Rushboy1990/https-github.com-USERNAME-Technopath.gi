using System;
using System.Collections.Generic;
using System.Linq;
using Technopath.Combat.Board;

namespace Technopath.Combat.Round
{
    public sealed class MutantTurnPlanner
    {
        public IReadOnlyList<MutantIntent> Prepare(
            BattleGridModel mutantGrid,
            IReadOnlyList<MutantProfile> profiles,
            int seed)
        {
            if (mutantGrid == null) throw new ArgumentNullException(nameof(mutantGrid));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var random = new Random(seed);
            var intents = new List<MutantIntent>(profiles.Count);
            foreach (var profile in profiles)
            {
                if (!mutantGrid.TryFindUnit(profile.UnitId, out var origin))
                    continue;

                var available = origin.GetOrthogonalNeighbors()
                    .Where(position => mutantGrid[position].IsEmpty)
                    .ToArray();
                GridPosition? destination = available.Length == 0
                    ? null
                    : available[random.Next(available.Length)];
                intents.Add(new MutantIntent(profile.UnitId, profile.Priority, profile.AttackDamage,
                    destination, random.Next()));
            }

            return intents
                .OrderBy(intent => intent.Priority)
                .ThenBy(intent => intent.TieBreaker)
                .ToArray();
        }
    }
}
