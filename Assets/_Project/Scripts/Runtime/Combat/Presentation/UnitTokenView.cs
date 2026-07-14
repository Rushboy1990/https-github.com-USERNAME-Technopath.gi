using UnityEngine;
using System.Collections;
using Technopath.Combat.Board;
using Technopath.Combat.Rules;

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

        private string _displayName;
        private Color _baseColor;

        public string UnitId { get; private set; }

        public void Bind(string unitId, BoardSide side, bool isTechnopath, string displayName)
        {
            UnitId = unitId;
            name = $"Unit_{unitId}";
            _displayName = string.IsNullOrWhiteSpace(displayName) ? unitId : displayName;
            if (body != null)
            {
                _baseColor = side == BoardSide.Enemy ? mutantColor : isTechnopath ? technopathColor : robotColor;
                body.color = _baseColor;
            }
            UpdateVitals(0, 0, 0, 0);
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

        public void UpdateVitals(int health, int maximumHealth, int shield, int maximumShield)
        {
            if (label == null) return;
            var shieldText = maximumShield > 0 ? $"  SHD {shield}/{maximumShield}" : string.Empty;
            label.text = maximumHealth > 0
                ? $"{_displayName}\nHP {health}/{maximumHealth}{shieldText}"
                : _displayName;
        }

        public void ShowAttackIntent(int damage, int targetRow, string intentName)
        {
            if (intentLabel != null)
                intentLabel.text = $"{intentName}\nATK {damage}  → ROW {targetRow + 1}";
        }

        public void HideIntent()
        {
            if (intentLabel != null)
                intentLabel.text = string.Empty;
        }

        public void ShowArchetype(string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                _displayName = displayName;
        }

        public void SetActivating(bool active)
        {
            if (body != null)
                body.color = active ? Color.Lerp(_baseColor, Color.white, 0.55f) : _baseColor;
        }

        public void ShowDamage(DamageResult damage)
        {
            if (damage == null) return;
            StopAllCoroutines();
            StartCoroutine(FlashDamage(damage.Killed ? new Color(1f, 0.2f, 0.2f, 1f) : Color.white));
        }

        public void ShowDestroyed()
        {
            if (intentLabel != null)
                intentLabel.text = "DESTROYED";
            if (body != null)
                body.color = new Color(1f, 0.1f, 0.1f, 1f);
        }

        private IEnumerator FlashDamage(Color color)
        {
            if (body == null) yield break;
            body.color = color;
            yield return new WaitForSeconds(0.15f);
            body.color = _baseColor;
        }

        private void Reset()
        {
            body = GetComponent<SpriteRenderer>();
            label = GetComponentInChildren<TextMesh>();
        }
    }
}
