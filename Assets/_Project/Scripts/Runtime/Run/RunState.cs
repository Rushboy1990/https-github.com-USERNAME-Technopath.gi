using System;
using System.Collections.Generic;
using Technopath.Combat.Modules;
using Technopath.Combat.Archetypes;

namespace Technopath.Run
{
    public sealed class RunState
    {
        private readonly List<RobotModuleDefinition> _moduleInventory = new();
        private readonly List<CampRobotState> _robots = new();

        public int Parts { get; private set; }
        public IReadOnlyList<RobotModuleDefinition> ModuleInventory => _moduleInventory;
        public IReadOnlyList<CampRobotState> Robots => _robots;

        public void AddParts(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Parts += amount;
        }

        public void AddModule(RobotModuleDefinition module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            _moduleInventory.Add(module);
        }

        public void AddRobot(CampRobotState robot)
        {
            if (robot == null) throw new ArgumentNullException(nameof(robot));
            if (_robots.Count >= 8) throw new InvalidOperationException("Robot squad is full.");
            _robots.Add(robot);
        }

        public bool TrySpendParts(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Parts < amount) return false;
            Parts -= amount;
            return true;
        }

        public bool TryEquip(CampRobotState robot, RobotModuleDefinition module, ModuleSlotType slot, int modifierIndex = 0)
        {
            if (robot == null || module == null || !_moduleInventory.Contains(module) || module.SlotType != slot)
                return false;
            var previous = robot.Loadout.Get(slot, modifierIndex);
            if (!robot.Loadout.TryEquip(module, modifierIndex)) return false;
            _moduleInventory.Remove(module);
            if (previous != null) _moduleInventory.Add(previous);
            robot.RefreshMaximumHealth();
            return true;
        }

        public bool TryUnequip(CampRobotState robot, ModuleSlotType slot, int modifierIndex = 0)
        {
            if (robot == null) return false;
            var module = robot.Loadout.Get(slot, modifierIndex);
            if (module == null) return false;
            robot.Loadout.Clear(slot, modifierIndex);
            _moduleInventory.Add(module);
            robot.RefreshMaximumHealth();
            return true;
        }

        public bool TryRepair(CampRobotState robot, int parts, int healthPerPart)
        {
            if (robot == null || !robot.IsDamaged || parts <= 0 || healthPerPart <= 0 || !TrySpendParts(parts)) return false;
            robot.Repair(parts * healthPerPart);
            return true;
        }

        public bool TryDismantleModule(RobotModuleDefinition module, int rarityMultiplier)
        {
            if (module == null || !_moduleInventory.Remove(module)) return false;
            AddParts((1 + (int)module.Rarity) * Math.Max(1, module.Level) * Math.Max(1, rarityMultiplier));
            return true;
        }

        public bool TryDismantleRobot(CampRobotState robot, int returnedParts)
        {
            if (robot == null || !_robots.Remove(robot)) return false;
            foreach (ModuleSlotType slot in Enum.GetValues(typeof(ModuleSlotType)))
            {
                var count = slot == ModuleSlotType.Modifier ? 3 : 1;
                for (var index = 0; index < count; index++)
                {
                    var module = robot.Loadout.Get(slot, index);
                    if (module != null) _moduleInventory.Add(module);
                }
            }
            AddParts(Math.Max(0, returnedParts));
            return true;
        }

        public CampRobotState BuildRobot(string id, RobotArchetypeDefinition archetype, int cost)
        {
            if (archetype == null || _robots.Count >= 8 || !TrySpendParts(cost)) return null;
            var loadout = new RobotLoadout(archetype);
            var robot = new CampRobotState(id, loadout, archetype.MaximumHealth);
            _robots.Add(robot);
            return robot;
        }
    }
}
