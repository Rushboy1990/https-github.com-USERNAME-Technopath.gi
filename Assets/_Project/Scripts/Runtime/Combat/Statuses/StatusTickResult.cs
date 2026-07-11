namespace Technopath.Combat.Statuses
{
    public sealed class StatusTickResult
    {
        public StatusTickResult(string statusId, int value, int remainingCharges)
        {
            StatusId = statusId;
            Value = value;
            RemainingCharges = remainingCharges;
        }

        public string StatusId { get; }
        public int Value { get; }
        public int RemainingCharges { get; }
    }
}
