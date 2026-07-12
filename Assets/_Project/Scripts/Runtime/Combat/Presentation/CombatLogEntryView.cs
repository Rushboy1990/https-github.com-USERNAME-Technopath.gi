using UnityEngine;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CombatLogEntryView : MonoBehaviour
    {
        [SerializeField] private Text label;

        public void Bind(int index, string message)
        {
            label.text = $"{index:000}. {message}";
        }
    }
}
