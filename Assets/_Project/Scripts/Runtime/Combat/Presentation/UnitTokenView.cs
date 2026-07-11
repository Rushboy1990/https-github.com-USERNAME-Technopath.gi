using UnityEngine;
using System.Collections;
using Technopath.Combat.Board;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class UnitTokenView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private TextMesh label;
        [SerializeField] private TextMesh intentLabel;
        [SerializeField] private Color technopathColor = new(0.16f, 0.78f, 0.9f, 1f);
        [SerializeField] private Color robotColor = new(0.42f, 0.72f, 0.35f, 1f);
        [SerializeField] private Color mutantColor = new(0.82f, 0.28f, 0.3f, 1f);

        public string UnitId { get; private set; }

        public void Bind(string unitId, BoardSide side, bool isTechnopath)
        {
            UnitId = unitId;
            name = $"Unit_{unitId}";
            if (body != null)
                body.color = side == BoardSide.Enemy ? mutantColor : isTechnopath ? technopathColor : robotColor;
            if (label != null)
                label.text = side == BoardSide.Enemy ? "M" : isTechnopath ? "T" : "R";
        }

        public IEnumerator MoveTo(Vector3 destination, bool jump, float duration = 0.3f)
        {
            var origin = transform.position;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var position = Vector3.Lerp(origin, destination, progress);
                if (jump)
                    position.y += Mathf.Sin(progress * Mathf.PI) * 0.6f;
                transform.position = position;
                yield return null;
            }
            transform.position = destination;
        }

        public void ShowAttackIntent(int damage)
        {
            if (intentLabel != null)
                intentLabel.text = $"ATK {damage}";
        }

        public void HideIntent()
        {
            if (intentLabel != null)
                intentLabel.text = string.Empty;
        }

        public void ShowArchetype(string displayName)
        {
            if (label != null && !string.IsNullOrWhiteSpace(displayName))
                label.text = displayName.Substring(0, 1).ToUpperInvariant();
        }

        private void Reset()
        {
            body = GetComponent<SpriteRenderer>();
            label = GetComponentInChildren<TextMesh>();
        }
    }
}
