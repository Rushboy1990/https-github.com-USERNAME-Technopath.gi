namespace Technopath.Combat.Statuses
{
    public sealed class StatusTickResult
    {
        public StatusTickResult(string statusId, int value, int remainingCharges,
            StatusEffectKind effectKind = StatusEffectKind.Damage)
        {
            StatusId = statusId;
            Value = value;
            RemainingCharges = remainingCharges;
            EffectKind = effectKind;
        }

        public string StatusId { get; }
        public int Value { get; }
        public int RemainingCharges { get; }
        public StatusEffectKind EffectKind { get; }
    }
}
