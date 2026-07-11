namespace Technopath.Combat.Events
{
    public enum CombatEventKind
    {
        PhaseStarted = 0,
        Movement = 10,
        Swap = 20,
        Attack = 30,
        Damage = 40,
        Kill = 50,
        Destroyed = 60,
        PhaseEnded = 70
    }
}
