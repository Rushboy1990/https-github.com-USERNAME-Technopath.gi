namespace Technopath.Combat.Rules
{
    public sealed class AutoAttackResult
    {
        public AutoAttackResult(string attackerId, string targetId, int damage, int firingRow)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
            FiringRow = firingRow;
        }

        public string AttackerId { get; }
        public string TargetId { get; }
        public int Damage { get; }
        public int FiringRow { get; }
        public bool HasTarget => TargetId != null;
    }
}
