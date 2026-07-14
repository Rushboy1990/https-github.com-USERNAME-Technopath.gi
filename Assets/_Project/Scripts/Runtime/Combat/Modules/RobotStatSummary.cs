using System.Collections.Generic;

namespace Technopath.Combat.Modules
{
    public sealed class RobotStatSummary
    {
        public RobotStatSummary(int health, int shield, int attack, IReadOnlyList<string> sources)
        {
            Health = health;
            Shield = shield;
            Attack = attack;
            Sources = sources;
        }

        public int Health { get; }
        public int Shield { get; }
        public int Attack { get; }
        public IReadOnlyList<string> Sources { get; }
    }
}
