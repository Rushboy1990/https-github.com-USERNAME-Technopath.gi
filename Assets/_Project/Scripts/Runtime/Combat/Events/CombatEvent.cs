namespace Technopath.Combat.Events
{
    public sealed class CombatEvent
    {
        public CombatEvent(CombatEventKind kind, string sourceId = null, string targetId = null, int value = 0)
        {
            Kind = kind;
            SourceId = sourceId;
            TargetId = targetId;
            Value = value;
        }

        public CombatEventKind Kind { get; }
        public string SourceId { get; }
        public string TargetId { get; }
        public int Value { get; }
    }
}
