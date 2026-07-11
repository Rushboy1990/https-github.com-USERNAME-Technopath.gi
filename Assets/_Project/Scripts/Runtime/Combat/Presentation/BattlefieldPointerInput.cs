using UnityEngine;
using UnityEngine.InputSystem;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BattlefieldPointerInput : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private BattlefieldPresenter presenter;

        private void Update()
        {
            if (!TryReadPointerPress(out var screenPosition))
                return;

            var worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            var hit = Physics2D.OverlapPoint(worldPosition);
            if (hit != null && hit.TryGetComponent<GridCellView>(out var cell))
                presenter.Select(cell);
        }

        private static bool TryReadPointerPress(out Vector2 position)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }
    }
}
