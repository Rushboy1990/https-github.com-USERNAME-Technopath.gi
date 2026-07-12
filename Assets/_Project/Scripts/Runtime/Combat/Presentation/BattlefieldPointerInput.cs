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
            UpdateHover();
            if (!TryReadPointerPress(out var screenPosition))
                return;

            var worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            var hit = Physics2D.OverlapPoint(worldPosition);
            if (hit != null && hit.TryGetComponent<GridCellView>(out var cell))
                presenter.Select(cell);
            else
                presenter.ClearInspection();
        }

        private void UpdateHover()
        {
            if (Mouse.current == null) return;
            var screenPosition = Mouse.current.position.ReadValue();
            var worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            var hit = Physics2D.OverlapPoint(worldPosition);
            presenter.Hover(hit != null && hit.TryGetComponent<GridCellView>(out var cell) ? cell : null, screenPosition);
        }

        private static bool TryReadPointerPress(out Vector2 position)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }
    }
}
