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
            label.text = data.Name;
            _tooltip = data.Tooltip;
            _owner = owner;
        }

        public void OnPointerEnter(PointerEventData eventData) => _owner.ShowModuleTooltip(_tooltip, eventData.position);
        public void OnPointerExit(PointerEventData eventData) => _owner.HideModuleTooltip();
    }
}
