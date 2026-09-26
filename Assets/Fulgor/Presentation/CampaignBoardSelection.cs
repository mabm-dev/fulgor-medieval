using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fulgor.Presentation.Map
{
    /// <summary>Adapta el puntero al estado de selección y a su panel.</summary>
    public sealed class CampaignBoardSelection : MonoBehaviour
    {
        private readonly MapSelection selection = new();
        private HexTileView selectedView;
        private HexTileView hoveredView;
        private CampaignSelectionPanel panel;

        public MapCell? SelectedCell => selection.SelectedCell;

        public void Initialize(CampaignSelectionPanel selectionPanel)
        {
            panel = selectionPanel;
            panel?.SetCell(selection.SelectedCell);
        }

        public void Select(HexTileView tile)
        {
            if (tile == null || tile.Owner != this) return;
            if (selectedView != null && selectedView != tile) selectedView.SetSelected(false);
            selectedView = tile;
            selectedView.SetSelected(true);
            selection.Select(tile.Cell);
            panel?.SetCell(selection.SelectedCell);
        }

        public void ClearSelection()
        {
            if (selectedView != null) selectedView.SetSelected(false);
            selectedView = null;
            selection.Clear();
            panel?.SetCell(null);
        }

        public bool BlocksPointer(Vector2 screenPosition)
        {
            return isActiveAndEnabled && panel != null && panel.IsVisible &&
                panel.ContainsScreenPoint(screenPosition);
        }

        private void Update()
        {
            if (!Application.isFocused) { SetHover(null); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ClearSelection();
            var mouse = Mouse.current;
            var camera = Camera.main;
            if (mouse == null || camera == null) { SetHover(null); return; }
            var pointer = mouse.position.ReadValue();
            if (!camera.pixelRect.Contains(pointer) || BlocksPointer(pointer))
            {
                SetHover(null);
                return;
            }
            HexTileView tile = null;
            if (Physics.Raycast(camera.ScreenPointToRay(pointer), out var hit,
                camera.farClipPlane, camera.cullingMask, QueryTriggerInteraction.Ignore))
            {
                var candidate = hit.collider.GetComponentInParent<HexTileView>();
                if (candidate != null && candidate.Owner == this) tile = candidate;
            }
            SetHover(tile);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (tile != null) Select(tile);
                else ClearSelection();
            }
        }

        private void SetHover(HexTileView tile)
        {
            if (hoveredView == tile) return;
            if (hoveredView != null) hoveredView.SetHovered(false);
            hoveredView = tile;
            if (hoveredView != null) hoveredView.SetHovered(true);
        }

        private void OnDisable()
        {
            SetHover(null);
            ClearSelection();
        }
    }
}