using Technopath.Combat.Rules;
using UnityEngine;

namespace Technopath.Combat.Content
{
    [CreateAssetMenu(fileName = "Technopath", menuName = "Technopath/Combat/Technopath Definition")]
    public sealed class TechnopathDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "technopath";
        [SerializeField] private string displayName = "TECHNOPATH";

        [Header("Base stats")]
        [SerializeField, Min(1)] private int maximumHealth = 30;
        [SerializeField, Min(0)] private int maximumShield = 3;
        [SerializeField, Min(0)] private int autoAttackDamage = 10;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaximumHealth => Mathf.Max(1, maximumHealth);
        public int MaximumShield => Mathf.Max(0, maximumShield);
        public int AutoAttackDamage => Mathf.Max(0, autoAttackDamage);

        public CombatUnitSetup CreateSetup() =>
            new(MaximumHealth, AutoAttackDamage, MaximumShield);
    }
}
