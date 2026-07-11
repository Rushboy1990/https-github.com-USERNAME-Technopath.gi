namespace Technopath.Combat.Rules
{
    public sealed class DamageResult
    {
        public DamageResult(int incomingDamage, int absorbedByArmor, int healthDamage, bool killed)
        {
            IncomingDamage = incomingDamage;
            AbsorbedByArmor = absorbedByArmor;
            HealthDamage = healthDamage;
            Killed = killed;
        }

        public int IncomingDamage { get; }
        public int AbsorbedByArmor { get; }
        public int HealthDamage { get; }
        public bool Killed { get; }
    }
}
