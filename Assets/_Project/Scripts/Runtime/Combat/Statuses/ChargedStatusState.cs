using System;

namespace Technopath.Combat.Statuses
{
    public sealed class ChargedStatusState
    {
        public ChargedStatusState(string id, int charges, int valuePerCharge, StatusTickMoment tickMoment,
            int maximumCharges = 0, StatusEffectKind effectKind = StatusEffectKind.Damage)
        {
            if (charges < 0) throw new ArgumentOutOfRangeException(nameof(charges));
            if (maximumCharges < 0) throw new ArgumentOutOfRangeException(nameof(maximumCharges));
            Id = id;
            MaximumCharges = maximumCharges;
            Charges = maximumCharges > 0 ? Math.Min(charges, maximumCharges) : charges;
            ValuePerCharge = valuePerCharge;
            TickMoment = tickMoment;
            EffectKind = effectKind;
        }

        public string Id { get; }
        public int Charges { get; private set; }
        public int ValuePerCharge { get; }
        public StatusTickMoment TickMoment { get; }
        public StatusEffectKind EffectKind { get; }
        public int MaximumCharges { get; }
        public bool IsExpired => Charges == 0;

        public void AddCharges(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Charges = MaximumCharges > 0 ? Math.Min(Charges + amount, MaximumCharges) : Charges + amount;
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
