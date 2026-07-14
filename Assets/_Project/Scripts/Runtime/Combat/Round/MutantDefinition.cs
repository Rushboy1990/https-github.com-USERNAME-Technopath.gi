using UnityEngine;

namespace Technopath.Combat.Round
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Mutant", fileName = "Mutant")]
    public sealed class MutantDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string roleName;
        [SerializeField, TextArea] private string abilityRulesText;

        [Header("Base stats")]
        [SerializeField, Min(1)] private int maximumHealth = 10;
        [SerializeField, Min(0)] private int maximumShield;
        [SerializeField, Min(0)] private int attackDamage = 1;
        [SerializeField, Min(0)] private int turnsBeforeAttack;

        public string Id => id;
        public string DisplayName => displayName;
        public string RoleName => roleName;
        public string AbilityRulesText => abilityRulesText;
        public int MaximumHealth => maximumHealth;
        public int MaximumShield => maximumShield;
        public int AttackDamage => attackDamage;
        public int TurnsBeforeAttack => turnsBeforeAttack;

        public MutantProfile CreateProfile(string unitId, int priority, int damageBonus = 0) =>
            new(unitId, priority, Mathf.Max(0, attackDamage + damageBonus), displayName, roleName,
                maximumHealth, maximumShield);

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
            roleName = roleName?.Trim();
        }
    }
}
