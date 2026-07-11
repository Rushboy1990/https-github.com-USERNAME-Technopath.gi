using System.Collections.Generic;

namespace Technopath.Combat.Modules
{
    public sealed class RobotStatSummary
    {
        public RobotStatSummary(int health, int armor, int attack, IReadOnlyList<string> sources)
        {
            Health = health;
            Armor = armor;
            Attack = attack;
            Sources = sources;
        }

        public int Health { get; }
        public int Armor { get; }
        public int Attack { get; }
        public IReadOnlyList<string> Sources { get; }
    }
}
