namespace Technopath.Combat.Rules
{
    public sealed class AutoAttackResult
    {
        public AutoAttackResult(string attackerId, string targetId, int damage)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
        }

        public string AttackerId { get; }
        public string TargetId { get; }
        public int Damage { get; }
        public bool HasTarget => TargetId != null;
    }
}
