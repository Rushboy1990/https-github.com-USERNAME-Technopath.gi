using System;
using System.Collections.Generic;
using Technopath.Combat.Archetypes;

namespace Technopath.Combat.Modules
{
    public sealed class RobotLoadout
    {
        private readonly RobotModuleDefinition[] _modifiers = new RobotModuleDefinition[3];

        public RobotLoadout(RobotArchetypeDefinition archetype)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
        }

        public RobotArchetypeDefinition Archetype { get; }
        public RobotModuleDefinition Core { get; private set; }
        public RobotModuleDefinition Processor { get; private set; }
        public IReadOnlyList<RobotModuleDefinition> Modifiers => _modifiers;

        public LoadoutAbilitySummary GetPrimaryAbility()
        {
            if (Core != null && Core.HasAbility)
                return FromModule(Core);
            return new LoadoutAbilitySummary($"Archetype: {Archetype.DisplayName}", Archetype.AbilityName,
                Archetype.AbilityRulesText, Archetype.TriggerMoment, Archetype.EffectValue);
        }

        public LoadoutAbilitySummary GetProcessorAbility() =>
            Processor != null && Processor.HasAbility ? FromModule(Processor) : null;

        public bool TryEquip(RobotModuleDefinition module, int modifierIndex = 0)
        {
            if (module == null || !module.IsCompatible(Archetype.Role)) return false;
            switch (module.SlotType)
            {
                case ModuleSlotType.Core:
                    Core = module;
                    return true;
                case ModuleSlotType.Processor:
                    Processor = module;
                    return true;
                case ModuleSlotType.Modifier when modifierIndex >= 0 && modifierIndex < _modifiers.Length:
                    _modifiers[modifierIndex] = module;
                    return true;
                default:
                    return false;
            }
        }

        public void Clear(ModuleSlotType slotType, int modifierIndex = 0)
        {
            switch (slotType)
            {
                case ModuleSlotType.Core: Core = null; break;
                case ModuleSlotType.Processor: Processor = null; break;
                case ModuleSlotType.Modifier when modifierIndex >= 0 && modifierIndex < _modifiers.Length:
                    _modifiers[modifierIndex] = null;
                    break;
            }
        }

        public RobotModuleDefinition Get(ModuleSlotType slotType, int modifierIndex = 0)
        {
            return slotType switch
            {
                ModuleSlotType.Core => Core,
                ModuleSlotType.Processor => Processor,
                ModuleSlotType.Modifier when modifierIndex >= 0 && modifierIndex < _modifiers.Length => _modifiers[modifierIndex],
                _ => null
            };
        }

        public RobotStatSummary CalculateStats()
        {
            var health = Archetype.MaximumHealth;
            var shield = Archetype.MaximumShield;
            var attack = Archetype.AutoAttackDamage;
            var sources = new List<string> { $"Archetype: {Archetype.DisplayName}" };
            Apply(Core, ref health, ref shield, ref attack, sources);
            Apply(Processor, ref health, ref shield, ref attack, sources);
            foreach (var modifier in _modifiers) Apply(modifier, ref health, ref shield, ref attack, sources);
            return new RobotStatSummary(Math.Max(1, health), Math.Max(0, shield), Math.Max(0, attack), sources);
        }

        private static void Apply(RobotModuleDefinition module, ref int health, ref int shield, ref int attack,
            ICollection<string> sources)
        {
            if (module == null) return;
            health += module.HealthModifier;
            shield += module.ShieldModifier;
            attack += module.AttackModifier;
            sources.Add($"{module.SlotType}: {module.DisplayName} (Lv.{module.Level} {module.Rarity})");
        }

        private static LoadoutAbilitySummary FromModule(RobotModuleDefinition module) =>
            new($"{module.SlotType}: {module.DisplayName}", module.AbilityName, module.AbilityRulesText,
                module.AbilityTrigger, module.AbilityEffectValue);
    }
}
