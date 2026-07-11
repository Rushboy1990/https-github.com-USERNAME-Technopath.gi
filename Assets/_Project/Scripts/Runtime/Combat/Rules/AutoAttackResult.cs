namespace Technopath.Combat.Rules
{
    public sealed class AutoAttackResult
    {
        public AutoAttackResult(string attackerId, string targetId, int damage, int firingRow, DamageResult damageResult = null)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
            FiringRow = firingRow;
            DamageResult = damageResult;
        }

        public string AttackerId { get; }
        public string TargetId { get; }
        public int Damage { get; }
        public int FiringRow { get; }
        public DamageResult DamageResult { get; }
        public bool HasTarget => TargetId != null;
    }
}
