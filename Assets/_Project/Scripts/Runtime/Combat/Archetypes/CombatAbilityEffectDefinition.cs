using System;
using Technopath.Combat.Statuses;
using UnityEngine;

namespace Technopath.Combat.Archetypes
{
    [Serializable]
    public sealed class CombatAbilityEffectDefinition
    {
        [SerializeField] private CombatAbilityEffectKind kind;
        [SerializeField] private CombatAbilityEffectTarget target;
        [SerializeField, Min(0)] private int value = 1;
        [SerializeField] private StatusDefinition status;

        public CombatAbilityEffectKind Kind => kind;
        public CombatAbilityEffectTarget Target => target;
        public int Value => value;
        public StatusDefinition Status => status;

        public bool IsValid => kind != CombatAbilityEffectKind.ApplyStatus || status != null;
    }
}
