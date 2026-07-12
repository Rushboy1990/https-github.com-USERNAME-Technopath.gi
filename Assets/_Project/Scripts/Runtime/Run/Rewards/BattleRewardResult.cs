using System.Collections.Generic;
using Technopath.Combat.Modules;

namespace Technopath.Run.Rewards
{
    public sealed class BattleRewardResult
    {
        public BattleRewardResult(int parts, IReadOnlyList<RobotModuleDefinition> modules)
        {
            Parts = parts;
            Modules = modules;
        }

        public int Parts { get; }
        public IReadOnlyList<RobotModuleDefinition> Modules { get; }
    }
}
