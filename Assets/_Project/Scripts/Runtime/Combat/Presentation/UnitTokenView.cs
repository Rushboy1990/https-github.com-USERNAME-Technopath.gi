using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class UnitTokenView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private TextMesh label;
        [SerializeField] private Color technopathColor = new(0.16f, 0.78f, 0.9f, 1f);
        [SerializeField] private Color robotColor = new(0.42f, 0.72f, 0.35f, 1f);

        public void Bind(string unitId, bool isTechnopath)
        {
            name = $"Unit_{unitId}";
            if (body != null)
                body.color = isTechnopath ? technopathColor : robotColor;
            if (label != null)
                label.text = isTechnopath ? "T" : "R";
        }

        private void Reset()
        {
            body = GetComponent<SpriteRenderer>();
            label = GetComponentInChildren<TextMesh>();
        }
    }
}
