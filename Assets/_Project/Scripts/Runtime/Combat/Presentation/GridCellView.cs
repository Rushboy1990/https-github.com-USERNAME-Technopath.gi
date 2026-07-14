using Technopath.Combat.Board;
using UnityEngine;

namespace Technopath.Combat.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GridCellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Color normalColor = new(0.16f, 0.2f, 0.24f, 1f);
        [SerializeField] private Color selectedColor = new(0.28f, 0.78f, 0.65f, 1f);
        [SerializeField] private Color validNeighborColor = new(0.82f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color threatenedColor = new(0.72f, 0.22f, 0.25f, 1f);

        public BoardSide Side { get; private set; }
        public GridPosition Position { get; private set; }

        public void Initialize(BoardSide side, GridPosition position)
        {
            Side = side;
            Position = position;
            ShowNormal();
        }

        public void ShowNormal() => SetColor(normalColor);
        public void ShowSelected() => SetColor(selectedColor);
        public void ShowValidNeighbor() => SetColor(validNeighborColor);
        public void ShowThreatened() => SetColor(threatenedColor);

        private void SetColor(Color color)
        {
            if (background != null)
                background.color = color;
        }

        private void Reset() => background = GetComponent<SpriteRenderer>();
    }
}
