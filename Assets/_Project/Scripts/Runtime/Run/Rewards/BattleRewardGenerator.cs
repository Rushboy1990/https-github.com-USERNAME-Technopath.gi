using System;
using System.Collections.Generic;
using Technopath.Combat.Modules;

namespace Technopath.Run.Rewards
{
    public sealed class BattleRewardGenerator
    {
        public BattleRewardResult Grant(RunState run, IReadOnlyList<RobotModuleDefinition> modulePool,
            int moduleCount, int parts, int seed)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (modulePool == null || modulePool.Count == 0)
                throw new ArgumentException("Reward module pool cannot be empty.", nameof(modulePool));
            if (moduleCount < 0) throw new ArgumentOutOfRangeException(nameof(moduleCount));
            if (parts < 0) throw new ArgumentOutOfRangeException(nameof(parts));

            var random = new Random(seed);
            var granted = new List<RobotModuleDefinition>(moduleCount);
            for (var index = 0; index < moduleCount; index++)
            {
                var module = modulePool[random.Next(modulePool.Count)];
                if (module == null) throw new InvalidOperationException("Reward module pool contains an empty entry.");
                granted.Add(module);
                run.AddModule(module);
            }
            run.AddParts(parts);
            return new BattleRewardResult(parts, granted);
        }
    }
}
