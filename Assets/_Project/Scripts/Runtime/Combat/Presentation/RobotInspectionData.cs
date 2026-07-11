using System;
using System.Collections.Generic;

namespace Technopath.Combat.Presentation
{
    public sealed class RobotInspectionData
    {
        public RobotInspectionData(string unitId, string name, string archetype, int health, int maximumHealth,
            int armor, int maximumArmor, int attack, string autoAttack, string primaryAbility,
            string utilityAbility, IReadOnlyList<ModifierInspectionData> modules,
            IReadOnlyList<string> statuses)
        {
            UnitId = unitId;
            Name = name;
            Archetype = archetype;
            Health = health;
            MaximumHealth = maximumHealth;
            Armor = armor;
            MaximumArmor = maximumArmor;
            Attack = attack;
            AutoAttack = autoAttack;
            PrimaryAbility = primaryAbility;
            UtilityAbility = utilityAbility;
            Modules = modules ?? Array.Empty<ModifierInspectionData>();
            Statuses = statuses ?? Array.Empty<string>();
        }

        public string UnitId { get; }
        public string Name { get; }
        public string Archetype { get; }
        public int Health { get; }
        public int MaximumHealth { get; }
        public int Armor { get; }
        public int MaximumArmor { get; }
        public int Attack { get; }
        public string AutoAttack { get; }
        public string PrimaryAbility { get; }
        public string UtilityAbility { get; }
        public IReadOnlyList<ModifierInspectionData> Modules { get; }
        public IReadOnlyList<string> Statuses { get; }
    }
}
