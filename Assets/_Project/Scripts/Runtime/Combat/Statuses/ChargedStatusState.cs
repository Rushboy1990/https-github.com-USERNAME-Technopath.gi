using System;

namespace Technopath.Combat.Statuses
{
    public sealed class ChargedStatusState
    {
        public ChargedStatusState(string id, int charges, int valuePerCharge, StatusTickMoment tickMoment)
        {
            if (charges < 0) throw new ArgumentOutOfRangeException(nameof(charges));
            Id = id;
            Charges = charges;
            ValuePerCharge = valuePerCharge;
            TickMoment = tickMoment;
        }

        public string Id { get; }
        public int Charges { get; private set; }
        public int ValuePerCharge { get; }
        public StatusTickMoment TickMoment { get; }
        public bool IsExpired => Charges == 0;

        public void AddCharges(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Charges += amount;
        }

        public int TryTick(StatusTickMoment moment)
        {
            if (moment != TickMoment || IsExpired)
                return 0;
            var value = Charges * ValuePerCharge;
            Charges--;
            return value;
        }
    }
}
