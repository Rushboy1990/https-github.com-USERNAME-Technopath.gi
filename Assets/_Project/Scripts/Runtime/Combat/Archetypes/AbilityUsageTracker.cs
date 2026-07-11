using System.Collections.Generic;

namespace Technopath.Combat.Archetypes
{
    public sealed class AbilityUsageTracker
    {
        private readonly HashSet<string> _combatUses = new();
        private readonly HashSet<string> _phaseUses = new();
        private readonly HashSet<string> _actionUses = new();

        public void BeginPhase()
        {
            _phaseUses.Clear();
            _actionUses.Clear();
        }

        public void BeginAction() => _actionUses.Clear();

        public bool TryUse(string key, AbilityFrequency frequency)
        {
            var set = frequency switch
            {
                AbilityFrequency.OncePerAction => _actionUses,
                AbilityFrequency.OncePerPhase => _phaseUses,
                AbilityFrequency.OncePerCombat => _combatUses,
                _ => null
            };
            return set == null || set.Add(key);
        }
    }
}
