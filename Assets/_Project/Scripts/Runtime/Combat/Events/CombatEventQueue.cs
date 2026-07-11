using System;
using System.Collections.Generic;

namespace Technopath.Combat.Events
{
    public sealed class CombatEventQueue
    {
        private readonly Queue<CombatEvent> _events = new();

        public CombatEventQueue(int maximumEventsPerChain = 128)
        {
            if (maximumEventsPerChain <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumEventsPerChain));
            MaximumEventsPerChain = maximumEventsPerChain;
        }

        public int MaximumEventsPerChain { get; }
        public int Count => _events.Count;

        public void Enqueue(CombatEvent combatEvent)
        {
            if (combatEvent == null) throw new ArgumentNullException(nameof(combatEvent));
            if (_events.Count >= MaximumEventsPerChain)
                throw new InvalidOperationException($"Combat event chain exceeded {MaximumEventsPerChain} events.");
            _events.Enqueue(combatEvent);
        }

        public CombatEvent Dequeue() => _events.Dequeue();
        public IReadOnlyList<CombatEvent> Drain()
        {
            var result = new List<CombatEvent>(_events.Count);
            while (_events.Count > 0)
                result.Add(_events.Dequeue());
            return result;
        }
        public void Clear() => _events.Clear();
    }
}
