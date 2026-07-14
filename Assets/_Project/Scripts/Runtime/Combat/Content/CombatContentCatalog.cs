using System;
using Technopath.Combat.Archetypes;
using Technopath.Combat.Modules;
using Technopath.Combat.Round;
using UnityEngine;

namespace Technopath.Combat.Content
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Content Catalog", fileName = "CombatContentCatalog")]
    public sealed class CombatContentCatalog : ScriptableObject
    {
        [SerializeField] private RobotArchetypeDefinition[] robotModels = Array.Empty<RobotArchetypeDefinition>();
        [SerializeField] private MutantDefinition[] mutants = Array.Empty<MutantDefinition>();
        [SerializeField] private RobotModuleDefinition[] modules = Array.Empty<RobotModuleDefinition>();

        public RobotArchetypeDefinition[] RobotModels => robotModels;
        public MutantDefinition[] Mutants => mutants;
        public RobotModuleDefinition[] Modules => modules;
    }
}
