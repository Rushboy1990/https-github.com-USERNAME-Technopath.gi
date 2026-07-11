using System.Collections;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AttackTraceView : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField] private TextMesh damageLabel;
        [SerializeField] private float lifetime = 0.35f;

        public void Play(Vector3 origin, Vector3 destination, int damage)
        {
            line.SetPosition(0, origin);
            line.SetPosition(1, destination);
            damageLabel.transform.position = Vector3.Lerp(origin, destination, 0.65f) + Vector3.up * 0.35f;
            damageLabel.text = damage > 0 ? $"-{damage}" : "MISS";
            StartCoroutine(Expire());
        }

        private IEnumerator Expire()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }
    }
}
