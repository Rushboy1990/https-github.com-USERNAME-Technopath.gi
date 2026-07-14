namespace Technopath.Combat.Rules
{
    public sealed class DamageResult
    {
        public DamageResult(int incomingDamage, int absorbedByShield, int healthDamage, bool killed)
        {
            IncomingDamage = incomingDamage;
            AbsorbedByShield = absorbedByShield;
            HealthDamage = healthDamage;
            Killed = killed;
        }

        public int IncomingDamage { get; }
        public int AbsorbedByShield { get; }
        public int HealthDamage { get; }
        public bool Killed { get; }
    }
}
