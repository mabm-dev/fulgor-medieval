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
        private bool pointerDown,pointerDragged;private Vector2 pointerStart;
        private HexTileView hoveredView;
        private CampaignSelectionPanel panel;
        public MapRouteGraph Routes { get; private set; }
        public MapPath PreviewedPath { get; private set; }

        public bool IsInteractionSuspended => GetComponent<CampaignDiplomacyView>()?.IsOpen == true || GetComponent<BattleLabController>()?.IsOpen == true || GetComponent<CityManagementView>()?.IsOpen == true || GetComponent<CampaignPlaySession>()?.IsOpen == true;

        public MapCell? SelectedCell => selection.SelectedCell;
        public event System.Action SelectionChanged;

        public void Initialize(CampaignSelectionPanel selectionPanel)
        {
            ResetBoard();
            panel = selectionPanel;
            panel?.SetCell(null);
        }

        public void Initialize(CampaignSelectionPanel selectionPanel, GeneratedMap map)
        {
            if (map == null) throw new System.ArgumentNullException(nameof(map));
            InitializeWithRoutes(selectionPanel, new MapRouteGraph(map));
        }

        public void InitializeWithRoutes(CampaignSelectionPanel selectionPanel, MapRouteGraph routes)
        {
            if (routes == null) throw new System.ArgumentNullException(nameof(routes));
            Initialize(selectionPanel);
            Routes = routes;
        }

        public bool CanTraverse(HexCoordinates from, HexCoordinates to) =>
            Routes != null && Routes.TryGetMovementCost(from, to, out _);

        public void ResetBoard()
        {
            SetHover(null);
            ClearSelection();
            Routes = null;
        }

        public void Select(HexTileView tile)
        {
            if (tile == null || tile.Owner != this) return;
            ClearPathPreview();
            if (selectedView != null && selectedView != tile) selectedView.SetSelected(false);
            selectedView = tile;
            selectedView.SetSelected(true);
            selection.Select(tile.Cell);
            panel?.SetCell(selection.SelectedCell);
            RefreshKnowledgeText();
            SelectionChanged?.Invoke();
        }

        public bool LocateCell(HexCoordinates coordinates)
        {
            foreach (var tile in GetComponentsInChildren<HexTileView>())
            {
                if (tile.Owner != this || tile.Cell.Coordinates != coordinates ||
                    !tile.gameObject.activeInHierarchy) continue;
                Select(tile);
                var camera = Camera.main;
                if (camera != null)
                    camera.GetComponent<CampaignCameraController>()?.FocusOnWorld(tile.transform.position);
                return true;
            }
            return false;
        }

        public void ClearSelection()
        {
            ClearPathPreview();
            if (selectedView != null) selectedView.SetSelected(false);
            selectedView = null;
            selection.Clear();
            panel?.SetCell(null);
            SelectionChanged?.Invoke();
        }

        public bool PreviewPath(HexTileView destination)
        {
            ClearPathPreview();
            if (!selection.SelectedCell.HasValue || Routes == null || destination == null ||
                destination.Owner != this || !destination.gameObject.activeInHierarchy) return false;

            var origin = selection.SelectedCell.Value.Coordinates;
            var target = destination.Cell.Coordinates;
            var fog=GetComponent<CampaignTurnController>();
            if(fog?.FogEnabled==true){var known=fog.PreviewKnowledgePath(origin,target,out var knownPath);PreviewedPath=knownPath;panel?.SetPathPreview(origin,target,knownPath);return known;}
            var found = MapPathfinder.TryFindPath(Routes, origin, target, out var path);
            PreviewedPath = path;
            panel?.SetPathPreview(origin, target, path);
            return found;
        }

        public void RefreshKnowledgeText(){if(panel!=null&&selection.SelectedCell.HasValue)panel.SetCellKnowledge(selection.SelectedCell.Value,GetComponent<CampaignTurnController>()?.VisibilityAt(selection.SelectedCell.Value.Coordinates)??Fulgor.Core.Campaign.CampaignVisibility.Visible);}

        private void ClearPathPreview()
        {
            PreviewedPath = null;
            panel?.ClearPathPreview();
        }

        public bool BlocksPointer(Vector2 screenPosition)
        {
            return isActiveAndEnabled && ((panel != null && panel.IsVisible &&
                panel.ContainsScreenPoint(screenPosition)) ||
                GetComponent<CampaignSettlementController>()?.BlocksPointer(screenPosition) == true);
        }

        public HexTileView ResolveHit(Collider collider)
        {
            if (collider == null) return null;
            var candidate = collider.GetComponentInParent<HexTileView>() ??
                collider.GetComponentInParent<CampaignSettlementView>()?.Tile;
            return candidate != null && candidate.Owner == this ? candidate : null;
        }

        public bool ActivateTile(HexTileView tile)
        {
            if(IsInteractionSuspended||tile==null||tile.Owner!=this||!tile.gameObject.activeInHierarchy)return false;
            Select(tile);GetComponent<CampaignSettlementController>()?.OpenCityAt(tile.Cell.Coordinates);return true;
        }

        private void Update()
        {
            if(GetComponent<BattleLabController>()?.ClosedThisFrame==true||GetComponent<CityManagementView>()?.ClosedThisFrame==true||GetComponent<CampaignPlaySession>()?.ClosedThisFrame==true){SetHover(null);return;}
            if (!Application.isFocused || IsInteractionSuspended) { pointerDown=false;SetHover(null); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ClearSelection();
            var mouse = Mouse.current;
            var camera = Camera.main;
            if (mouse == null || camera == null) { SetHover(null); return; }
            var pointer = mouse.position.ReadValue();
            if(pointerDown&&(pointer-pointerStart).sqrMagnitude>=64)pointerDragged=true;
            bool click=mouse.leftButton.wasReleasedThisFrame&&pointerDown&&!pointerDragged;
            if(mouse.leftButton.wasReleasedThisFrame)pointerDown=false;
            if (!camera.pixelRect.Contains(pointer) || BlocksPointer(pointer))
            {
                SetHover(null);
                return;
            }
            HexTileView tile = null;
            if (Physics.Raycast(camera.ScreenPointToRay(pointer), out var hit,
                camera.farClipPlane, camera.cullingMask, QueryTriggerInteraction.Ignore))
            {
                tile = ResolveHit(hit.collider);
            }
            SetHover(tile);
            if(mouse.leftButton.wasPressedThisFrame){pointerDown=true;pointerDragged=false;pointerStart=pointer;}
            if(mouse.rightButton.wasPressedThisFrame&&tile!=null)
            {
                Select(tile);
                if(panel?.MoveArmyButton.interactable==true)GetComponent<CampaignTurnController>()?.MoveToSelectedCell();
            }
            if (click)
            {
                if (tile != null && Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
                    PreviewPath(tile);
                else if (tile != null) ActivateTile(tile);
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
