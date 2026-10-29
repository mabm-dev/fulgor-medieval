using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignSelectionPanel : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform turnPanel;
        private RectTransform resourceBar;
        private RectTransform cameraControls;
        private Text details;
        private Text diagnostics;
        private Text turnSummary;
        private Text pathPreview;
        private Text armySummary;
        private Text missionSummary;
        private Text saveStatus;

        public bool IsVisible => panel != null && panel.gameObject.activeInHierarchy;
        public RectTransform PanelRectTransform => panel;
        public Button EndTurnButton { get; private set; }
        public Button MoveArmyButton { get; private set; }
        public Button SaveButton { get; private set; }
        public Button LoadButton { get; private set; }
        public Button LocateArmyButton { get; private set; }
        public Button LocateObjectiveButton { get; private set; }
        public Button NewGameButton { get; private set; }
        public Button CancelMarchButton { get; private set; }
        public Button BattleLabButton { get; private set; }
        public Button NextArmyButton { get; private set; }
        private Text marchStatus;

        private void Awake()
        {
            var canvasObject = new GameObject("Campaign HUD", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            panel = CreateRect("Map Information", canvasObject.transform,
                new Vector2(360f, 306f), new Vector2(24f, -24f));
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.06f, 0.052f, 0.94f);
            background.raycastTarget = false;

            CreateText("Title", panel, new Vector2(18f, -14f), new Vector2(324f, 26f),
                "MAPA DE CAMPAÑA", 18, new Color(0.93f, 0.68f, 0.16f), FontStyle.Bold);
            details = CreateText("Selection", panel, new Vector2(18f, -48f),
                new Vector2(324f, 66f), "Selecciona una casilla para ver sus datos.",
                15, new Color(0.94f, 0.88f, 0.72f));
            pathPreview = CreateText("Route Preview", panel, new Vector2(18f, -116f),
                new Vector2(324f, 60f), "Selecciona origen y usa Mayús + clic en destino.",
                13, new Color(0.93f, 0.78f, 0.42f));
            CreateText("Legend", panel, new Vector2(18f, -180f), new Vector2(324f, 32f),
                "Agua  ·  Llanura  ·  Bosque  ·  Colina  ·  Montaña", 13,
                new Color(0.72f, 0.78f, 0.68f));
            CreateText("Controls", panel, new Vector2(18f, -222f), new Vector2(324f, 40f),
                "WASD/Flechas: cámara   Rueda: zoom\nClic derecho: marchar · Esc: limpiar", 12,
                new Color(0.68f, 0.70f, 0.64f));
            diagnostics = CreateText("Diagnostics", panel, new Vector2(18f, -274f),
                new Vector2(324f, 22f), "Construyendo tablero…", 11,
                new Color(0.50f, 0.57f, 0.48f));

            turnPanel = CreateRect("Turn Panel", canvasObject.transform,
                new Vector2(370f, 660f), new Vector2(-24f, -24f));
            turnPanel.anchorMin = Vector2.one;
            turnPanel.anchorMax = Vector2.one;
            turnPanel.pivot = Vector2.one;
            var turnBackground = turnPanel.gameObject.AddComponent<Image>();
            turnBackground.color = new Color(0.055f, 0.06f, 0.052f, 0.94f);
            turnBackground.raycastTarget = false;
            CreateText("Turn Title", turnPanel, new Vector2(18f, -14f),
                new Vector2(334f, 26f), "ÓRDENES DE LA HUESTE", 18,
                new Color(0.93f, 0.68f, 0.16f), FontStyle.Bold);
            turnSummary = CreateText("Turn Summary", turnPanel, new Vector2(18f, -45f),
                new Vector2(334f, 88f), "Turno 1", 14,
                new Color(0.94f, 0.88f, 0.72f));
            var buttonRect = CreateRect("End Turn", turnPanel,
                new Vector2(334f, 40f), new Vector2(18f, -142f));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.45f, 0.31f, 0.12f);
            EndTurnButton = buttonRect.gameObject.AddComponent<Button>();
            EndTurnButton.targetGraphic = buttonImage;
            var buttonText = CreateText("Label", buttonRect, Vector2.zero,
                buttonRect.sizeDelta, "FIN DE TURNO", 16, Color.white, FontStyle.Bold);
            buttonText.alignment = TextAnchor.MiddleCenter;

            armySummary = CreateText("Army Summary", turnPanel, new Vector2(18f, -190f),
                new Vector2(334f, 94f), "Preparando hueste…", 14,
                new Color(0.94f, 0.88f, 0.72f));
            var moveRect = CreateRect("Move Army", turnPanel,
                new Vector2(334f, 40f), new Vector2(18f, -296f));
            var moveImage = moveRect.gameObject.AddComponent<Image>();
            moveImage.color = new Color(0.25f, 0.36f, 0.18f);
            MoveArmyButton = moveRect.gameObject.AddComponent<Button>();
            MoveArmyButton.targetGraphic = moveImage;
            MoveArmyButton.interactable = false;
            var moveLabel = CreateText("Label", moveRect, Vector2.zero,
                moveRect.sizeDelta, "MARCHAR A LA CASILLA SELECCIONADA", 13, Color.white, FontStyle.Bold);
            moveLabel.alignment = TextAnchor.MiddleCenter;

            missionSummary = CreateText("Mission Summary", turnPanel, new Vector2(18f, -350f),
                new Vector2(334f, 50f), "", 14, new Color(0.93f, 0.78f, 0.42f));

            SaveButton = CreateSaveButton("Save", "GUARDAR", turnPanel, new Vector2(18f, -406f));
            LoadButton = CreateSaveButton("Load", "CARGAR", turnPanel, new Vector2(190f, -406f));
            saveStatus = CreateText("Save Status", turnPanel, new Vector2(18f, -448f),
                new Vector2(334f, 32f), "", 11, new Color(0.8f, 0.8f, 0.7f));

            LocateArmyButton = CreateSaveButton("Locate Army", "VER HUESTE", turnPanel, new Vector2(18f, -488f));
            LocateObjectiveButton = CreateSaveButton("Locate Objective", "VER OBJETIVO", turnPanel, new Vector2(190f, -488f));
            NewGameButton = CreateSaveButton("New Game", "NUEVA PARTIDA", turnPanel, new Vector2(18f, -530f));

            CancelMarchButton = CreateSaveButton("Cancel March", "CANCELAR MARCHA", turnPanel, new Vector2(190f, -530f));
            marchStatus = CreateText("March Status", turnPanel, new Vector2(18f, -572f),
                new Vector2(334f, 34f), "Sin orden de marcha.", 12, new Color(0.93f, 0.78f, 0.42f));

            BattleLabButton = CreateSaveButton("Battle Lab", "LAB. COMBATE", turnPanel, new Vector2(18f, -612f));

            NextArmyButton = CreateSaveButton("Next Army", "SIGUIENTE HUESTE", turnPanel, new Vector2(190f, -612f));
            NextArmyButton.interactable = false;

            resourceBar=CampaignMenuUI.Rect("Campaign resource bar",canvasObject.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(1920,88));
            resourceBar.anchorMin=new Vector2(0,1);resourceBar.anchorMax=new Vector2(1,1);resourceBar.sizeDelta=new Vector2(0,88);
            CampaignMenuUI.Background(resourceBar,LegacyUI.Ink);LegacyUI.Panel(resourceBar);
            CampaignMenuUI.Text("Title",resourceBar,new Vector2(0,.5f),new Vector2(24,0),new Vector2(300,60),"FULGOR MEDIEVAL",23,LegacyUI.Gold);
            turnSummary.transform.SetParent(resourceBar,false);var summaryRect=turnSummary.rectTransform;
            summaryRect.anchorMin=summaryRect.anchorMax=summaryRect.pivot=new Vector2(.5f,.5f);summaryRect.anchoredPosition=Vector2.zero;summaryRect.sizeDelta=new Vector2(1100,76);turnSummary.alignment=TextAnchor.MiddleCenter;turnSummary.fontSize=19;
            panel.anchoredPosition=new Vector2(24,-112);turnPanel.anchoredPosition=new Vector2(-24,-112);turnPanel.sizeDelta=new Vector2(370,556);
            foreach(RectTransform child in turnPanel)if(child.anchoredPosition.y<=-142)child.anchoredPosition+=new Vector2(0,104);
            LegacyUI.Skin(panel);LegacyUI.Skin(turnPanel);
            CampaignCameraController CameraControls()=>Camera.main?.GetComponent<CampaignCameraController>();
            cameraControls=MapCameraControls.Create(canvasObject.transform,new Vector2(.5f,0),new Vector2(0,20),d=>CameraControls()?.PanView(d),d=>CameraControls()?.ZoomView(d),()=>CameraControls()?.ResetView(),()=>CameraControls()?.CanNavigate==true);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("Campaign Event System", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                eventSystem.transform.SetParent(transform, false);
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        public void SetCell(MapCell? cell)
        {
            if (details == null) return;
            details.text = cell.HasValue
                ? $"{TerrainName(cell.Value.Terrain)}  ·  {cell.Value.Coordinates.Key}\n" +
                  $"Oro: {(cell.Value.HasGold ? "Sí" : "No")}"
                : "Selecciona una casilla para ver sus datos.";
        }

        public void SetFogMode(bool enabled){if(diagnostics!=null)diagnostics.gameObject.SetActive(!enabled);}

        public void SetCellKnowledge(MapCell cell,Fulgor.Core.Campaign.CampaignVisibility visibility)
        {if(details==null)return;if(visibility==Fulgor.Core.Campaign.CampaignVisibility.Unknown){details.text="Territorio sin explorar · "+cell.Coordinates.Key;return;}SetCell(cell);if(visibility==Fulgor.Core.Campaign.CampaignVisibility.Explored)details.text+="\nExplorado · sin observación actual";}

        public void SetDiagnostics(BoardBuildMetrics metrics)
        {
            if (diagnostics == null) return;
            diagnostics.text = $"{metrics.Tiles} casillas · {metrics.Renderers} renderers · " +
                $"{metrics.Milliseconds:F1} ms";
        }

        public void SetPathPreview(HexCoordinates from, HexCoordinates to, MapPath path)
        {
            if (pathPreview == null) return;
            pathPreview.text = $"Itinerario {from.Key} → {to.Key}\n" +
                (path == null ? "Sin itinerario terrestre disponible." :
                    $"{path.Steps} pasos · Coste {path.TotalCost}");
        }

        public void ClearPathPreview()
        {
            if (pathPreview != null)
                pathPreview.text = "Selecciona origen y usa Mayús + clic en destino.";
        }

        public void SetTurnState(CampaignTurnState state)
        {
            if (turnSummary == null || state == null) return;
            var resources = state.Resources;
            var projected = state.ProjectedResources;
            turnSummary.text = $"Turno {state.Turn}   ·   " +
                $"Grano {resources.Grain} · Madera {resources.Wood} · Piedra {resources.Stone}   ·   " +
                $"Oro {resources.Gold} · Mano de obra {resources.Labor}\n" +
                $"Próximo cierre: {projected.Grain-resources.Grain:+0;-0;0} grano, {projected.Gold-resources.Gold:+0;-0;0} oro";
        }

        public void SetMultipleArmyMode(bool enabled)
        {
            if (NextArmyButton != null) NextArmyButton.interactable = enabled;
            if (MoveArmyButton != null) MoveArmyButton.GetComponentInChildren<Text>().text = enabled ?
                "MOVER HUESTE SELECCIONADA" : "MARCHAR A LA CASILLA SELECCIONADA";
        }

        public void SetNextArmyEnabled(bool enabled)
        {
            if (NextArmyButton != null) NextArmyButton.interactable = enabled;
        }

        public void SetArmyActionLabel(string label)
        {
            if (MoveArmyButton != null) MoveArmyButton.GetComponentInChildren<Text>().text = label;
        }

        public void SetHudVisible(bool visible)
        {
            foreach (var canvas in GetComponentsInChildren<Canvas>(true)) canvas.enabled = visible;
        }

        public void SetMarchOrder(HexCoordinates? destination)
        {
            if (marchStatus != null) marchStatus.text = destination.HasValue ?
                $"Marcha pendiente: {destination.Value.Key}" : "Sin orden de marcha.";
            if (CancelMarchButton != null) CancelMarchButton.interactable = destination.HasValue;
        }

        public void SetMissionControls(bool hasArmy, bool hasObjective, bool canRestart)
        {
            if (LocateArmyButton != null) LocateArmyButton.interactable = hasArmy;
            if (LocateObjectiveButton != null) LocateObjectiveButton.interactable = hasObjective;
            if (NewGameButton != null) NewGameButton.interactable = canRestart;
        }

        public void SetSaveStatus(string message, bool enabled)
        {
            if (saveStatus != null) saveStatus.text = message;
            SetSavingEnabled(enabled);
        }

        public void SetSavingEnabled(bool enabled)
        {
            if (SaveButton != null) SaveButton.interactable = enabled;
            if (LoadButton != null) LoadButton.interactable = enabled;
        }

        private static Button CreateSaveButton(string name, string label, Transform parent, Vector2 position)
        {
            var rect = CreateRect(name, parent, new Vector2(162f, 34f), position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.27f, 0.30f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = CreateText("Label", rect, Vector2.zero, rect.sizeDelta, label, 13, Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public void SetMission(ReachDestinationMission mission)
        {
            if (missionSummary != null)
                missionSummary.text = mission == null ? "Sin objetivo: exploración libre." :
                    mission.IsWon ? "VICTORIA · Destino alcanzado" : $"Objetivo: lleva la hueste a {mission.Destination.Key}";
            if (EndTurnButton != null) EndTurnButton.interactable = mission?.IsWon != true;
        }

        public void SetCampaignConclusion(CampaignConclusion conclusion,CampaignObjectiveKind objective)
        {
            if(missionSummary!=null)
            {
                if(conclusion==CampaignConclusion.Victory)missionSummary.text=objective==CampaignObjectiveKind.Conquest?"VICTORIA · Has vencido a las facciones rivales.":"VICTORIA · Destino alcanzado";
                else if(conclusion==CampaignConclusion.Defeat)missionSummary.text="DERROTA · No quedan ciudades ni huestes propias.";
                else if(objective==CampaignObjectiveKind.Conquest)missionSummary.text="Objetivo: conquistar las ciudades y vencer las huestes rivales.";
                missionSummary.color=conclusion==CampaignConclusion.Victory?new Color(.45f,.9f,.6f):conclusion==CampaignConclusion.Defeat?new Color(1,.4f,.35f):new Color(.93f,.82f,.57f);
            }
            if(EndTurnButton!=null&&conclusion!=CampaignConclusion.InProgress)EndTurnButton.interactable=false;
        }

        public void SetArmyState(ArmyMovementState army, string orderStatus, bool canMove)
        {
            if (armySummary != null)
                armySummary.text = army == null ? "No hay tierra disponible para una hueste." :
                    $"Hueste · {army.Position.Key}\nMovimiento {army.RemainingMovement}/{army.MovementPerTurn}\n{orderStatus}";
            if (MoveArmyButton != null) MoveArmyButton.interactable = canMove;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition) =>
            (cameraControls != null && RectTransformUtility.RectangleContainsScreenPoint(cameraControls, screenPosition)) ||
            (resourceBar != null && RectTransformUtility.RectangleContainsScreenPoint(resourceBar, screenPosition)) ||
            (panel != null && RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition)) ||
            (turnPanel != null && RectTransformUtility.RectangleContainsScreenPoint(turnPanel, screenPosition));

        private static RectTransform CreateRect(string name, Transform parent,
            Vector2 size, Vector2 position)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = (RectTransform)item.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, Vector2 position,
            Vector2 size, string value, int fontSize, Color color,
            FontStyle style = FontStyle.Normal)
        {
            var rect = CreateRect(name, parent, size, position);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static string TerrainName(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water: return "Agua";
                case TerrainType.Plain: return "Llanura";
                case TerrainType.Forest: return "Bosque";
                case TerrainType.Hill: return "Colina";
                case TerrainType.Mountain: return "Montaña";
                default: return terrain.ToString();
            }
        }
    }
}
