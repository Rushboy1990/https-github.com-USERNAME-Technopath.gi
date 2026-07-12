using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ModifierInspectionItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Text label;
        private string _tooltip;
        private RobotInspectionPanel _owner;

        public void Bind(ModifierInspectionData data, RobotInspectionPanel owner)
        {
            var separator = data.Name.IndexOf(':');
            label.text = separator > 0
                ? $"{data.Name[..separator].ToUpperInvariant()}\n{data.Name[(separator + 1)..].Trim()}"
                : data.Name;
            _tooltip = data.Tooltip;
            _owner = owner;
        }

        public void OnPointerEnter(PointerEventData eventData) => _owner.ShowModuleTooltip(_tooltip, eventData.position);
        public void OnPointerExit(PointerEventData eventData) => _owner.HideModuleTooltip();
    }
}
