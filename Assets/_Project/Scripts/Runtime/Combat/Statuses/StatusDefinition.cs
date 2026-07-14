using UnityEngine;

namespace Technopath.Combat.Statuses
{
    [CreateAssetMenu(menuName = "Technopath/Combat/Status", fileName = "Status")]
    public sealed class StatusDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string rulesText;
        [SerializeField] private StatusEffectKind effectKind;
        [SerializeField] private StatusTickMoment tickMoment;
        [SerializeField, Min(0)] private int valuePerCharge = 1;
        [SerializeField, Min(0)] private int maximumCharges;

        public string Id => id;
        public string DisplayName => displayName;
        public string RulesText => rulesText;
        public StatusEffectKind EffectKind => effectKind;
        public StatusTickMoment TickMoment => tickMoment;
        public int ValuePerCharge => valuePerCharge;
        public int MaximumCharges => maximumCharges;

        private void OnValidate()
        {
            id = id?.Trim().ToLowerInvariant();
            displayName = displayName?.Trim();
        }
    }
}
