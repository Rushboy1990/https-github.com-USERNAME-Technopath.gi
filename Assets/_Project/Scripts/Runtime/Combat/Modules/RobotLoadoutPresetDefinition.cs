using Technopath.Combat.Archetypes;
using UnityEngine;

namespace Technopath.Combat.Modules
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Robot Loadout Preset", fileName = "RobotLoadoutPreset")]
    public sealed class RobotLoadoutPresetDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private RobotArchetypeDefinition archetype;
        [SerializeField] private RobotModuleDefinition core;
        [SerializeField] private RobotModuleDefinition processor;
        [SerializeField] private RobotModuleDefinition[] modifiers = new RobotModuleDefinition[3];

        public string Id => id;
        public string DisplayName => displayName;
        public RobotArchetypeDefinition Archetype => archetype;

        public RobotLoadout BuildRuntimeLoadout()
        {
            var loadout = new RobotLoadout(archetype);
            if (core != null && !loadout.TryEquip(core)) throw new System.InvalidOperationException($"Core {core.name} is incompatible.");
            if (processor != null && !loadout.TryEquip(processor)) throw new System.InvalidOperationException($"Processor {processor.name} is incompatible.");
            for (var index = 0; index < modifiers.Length && index < 3; index++)
                if (modifiers[index] != null && !loadout.TryEquip(modifiers[index], index))
                    throw new System.InvalidOperationException($"Modifier {modifiers[index].name} is incompatible.");
            return loadout;
        }

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
            if (modifiers == null || modifiers.Length != 3)
                System.Array.Resize(ref modifiers, 3);
        }
    }
}
