namespace Technopath.Combat.Archetypes
{
    public enum AbilityTriggerMoment
    {
        None = -1,
        PlayerPhaseStarted = 0,
        UnitMoved = 1,
        UnitSwapped = 2,
        UnitAttacked = 3,
        UnitDamaged = 4,
        UnitKilled = 5,
        PlayerPhaseEnded = 6
    }
}
