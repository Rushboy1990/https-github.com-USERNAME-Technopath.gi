using System.Collections;
using Technopath.Combat.Rules;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AttackTraceView : MonoBehaviour
    {
        [SerializeField] private LineRenderer line;
        [SerializeField] private TextMesh damageLabel;
        [SerializeField] private float lifetime = 0.35f;

        public void Play(Vector3 origin, Vector3 destination, AutoAttackResult attack)
        {
            line.SetPosition(0, origin);
            line.SetPosition(1, destination);
            damageLabel.transform.position = Vector3.Lerp(origin, destination, 0.65f) + Vector3.up * 0.35f;
            if (!attack.HasTarget)
                damageLabel.text = "MISS";
            else if (attack.DamageResult.AbsorbedByArmor == 0)
                damageLabel.text = $"HP -{attack.DamageResult.HealthDamage}";
            else if (attack.DamageResult.HealthDamage == 0)
                damageLabel.text = $"ARM -{attack.DamageResult.AbsorbedByArmor}";
            else
                damageLabel.text = $"ARM -{attack.DamageResult.AbsorbedByArmor}  HP -{attack.DamageResult.HealthDamage}";
            StartCoroutine(Expire());
        }

        private IEnumerator Expire()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }
    }
}
