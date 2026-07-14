using System.Collections.Generic;
using System.Linq;

namespace Technopath.Combat.Statuses
{
    public sealed class StatusCollection
    {
        private readonly Dictionary<string, Dictionary<string, ChargedStatusState>> _byUnit = new();

        public void Add(string unitId, string statusId, int charges, int valuePerCharge, StatusTickMoment tickMoment,
            int maximumCharges = 0, StatusEffectKind effectKind = StatusEffectKind.Damage)
        {
            if (!_byUnit.TryGetValue(unitId, out var statuses))
            {
                statuses = new Dictionary<string, ChargedStatusState>();
                _byUnit.Add(unitId, statuses);
            }
            if (statuses.TryGetValue(statusId, out var existing))
                existing.AddCharges(charges);
            else
                statuses.Add(statusId, new ChargedStatusState(statusId, charges, valuePerCharge, tickMoment,
                    maximumCharges, effectKind));
        }

        public void Add(string unitId, StatusDefinition definition, int charges)
        {
            Add(unitId, definition.Id, charges, definition.ValuePerCharge, definition.TickMoment,
                definition.MaximumCharges, definition.EffectKind);
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
                    results.Add(new StatusTickResult(status.Id, value, status.Charges, status.EffectKind));
                if (status.IsExpired)
                    statuses.Remove(status.Id);
            }
            return results;
        }

        public IReadOnlyList<ChargedStatusState> GetActive(string unitId)
        {
            if (!_byUnit.TryGetValue(unitId, out var statuses))
                return System.Array.Empty<ChargedStatusState>();
            return statuses.Values.Where(status => !status.IsExpired).ToArray();
        }

        public IReadOnlyList<string> GetAffectedUnitIds() => _byUnit
            .Where(entry => entry.Value.Values.Any(status => !status.IsExpired))
            .Select(entry => entry.Key)
            .ToArray();

        public bool HasEffect(string unitId, StatusEffectKind effectKind) =>
            GetActive(unitId).Any(status => status.EffectKind == effectKind);

        public int GetEffectValue(string unitId, StatusEffectKind effectKind) => GetActive(unitId)
            .Where(status => status.EffectKind == effectKind)
            .Sum(status => status.Charges * status.ValuePerCharge);

        public IReadOnlyList<StatusTickResult> Consume(string unitId, StatusTickMoment moment,
            StatusEffectKind? effectKind = null)
        {
            var results = new List<StatusTickResult>();
            if (!_byUnit.TryGetValue(unitId, out var statuses))
                return results;

            foreach (var status in statuses.Values.ToArray())
            {
                if (status.TickMoment != moment || effectKind.HasValue && status.EffectKind != effectKind.Value)
                    continue;
                var value = status.TryTick(moment);
                if (value > 0)
                    results.Add(new StatusTickResult(status.Id, value, status.Charges, status.EffectKind));
                if (status.IsExpired)
                    statuses.Remove(status.Id);
            }
            return results;
        }

        public bool TryConsumeFirst(string unitId, StatusTickMoment moment, StatusEffectKind effectKind,
            out StatusTickResult result)
        {
            result = null;
            if (!_byUnit.TryGetValue(unitId, out var statuses))
                return false;
            var status = statuses.Values.FirstOrDefault(candidate =>
                !candidate.IsExpired && candidate.TickMoment == moment && candidate.EffectKind == effectKind);
            if (status == null)
                return false;
            var value = status.TryTick(moment);
            result = new StatusTickResult(status.Id, value, status.Charges, status.EffectKind);
            if (status.IsExpired)
                statuses.Remove(status.Id);
            return true;
        }

        public bool TryConsumeFirst(string unitId, StatusEffectKind effectKind, out StatusTickResult result)
        {
            result = null;
            if (!_byUnit.TryGetValue(unitId, out var statuses))
                return false;
            var status = statuses.Values.FirstOrDefault(candidate =>
                !candidate.IsExpired && candidate.EffectKind == effectKind);
            if (status == null)
                return false;
            var value = status.TryTick(status.TickMoment);
            result = new StatusTickResult(status.Id, value, status.Charges, status.EffectKind);
            if (status.IsExpired)
                statuses.Remove(status.Id);
            return true;
        }
    }
}
