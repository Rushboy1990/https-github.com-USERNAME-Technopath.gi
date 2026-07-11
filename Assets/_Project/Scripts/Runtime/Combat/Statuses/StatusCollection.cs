using System.Collections.Generic;
using System.Linq;

namespace Technopath.Combat.Statuses
{
    public sealed class StatusCollection
    {
        private readonly Dictionary<string, Dictionary<string, ChargedStatusState>> _byUnit = new();

        public void Add(string unitId, string statusId, int charges, int valuePerCharge, StatusTickMoment tickMoment)
        {
            if (!_byUnit.TryGetValue(unitId, out var statuses))
            {
                statuses = new Dictionary<string, ChargedStatusState>();
                _byUnit.Add(unitId, statuses);
            }
            if (statuses.TryGetValue(statusId, out var existing))
                existing.AddCharges(charges);
            else
                statuses.Add(statusId, new ChargedStatusState(statusId, charges, valuePerCharge, tickMoment));
        }

        public void Add(string unitId, StatusDefinition definition, int charges)
        {
            Add(unitId, definition.Id, charges, definition.ValuePerCharge, definition.TickMoment);
        }

        public bool TryConsume(string unitId, string statusId, out int value)
        {
            value = 0;
            if (!_byUnit.TryGetValue(unitId, out var statuses) || !statuses.TryGetValue(statusId, out var status))
                return false;
            value = status.TryTick(status.TickMoment);
            if (status.IsExpired) statuses.Remove(statusId);
            return value > 0;
        }

        public IReadOnlyList<StatusTickResult> Tick(string unitId, StatusTickMoment moment)
        {
            var results = new List<StatusTickResult>();
            if (!_byUnit.TryGetValue(unitId, out var statuses))
                return results;

            foreach (var status in statuses.Values.ToArray())
            {
                var value = status.TryTick(moment);
                if (value > 0)
                    results.Add(new StatusTickResult(status.Id, value, status.Charges));
                if (status.IsExpired)
                    statuses.Remove(status.Id);
            }
            return results;
        }
    }
}
