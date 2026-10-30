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
        private RectTransform turnControls, utilityPanel, movementTrack, movementFill;
        private Canvas hudCanvas;
        private readonly Text[] resourceValues = new Text[5];
        private readonly Text[] resourceDeltas = new Text[5];
        private Text populationValue;
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
            var canvasObject = CampaignMenuUI.Canvas("Campaign HUD", transform, 20);
            hudCanvas = canvasObject.GetComponent<Canvas>();

            resourceBar = CreateRect("Campaign resource bar", canvasObject.transform,
                new Vector2(0, 104), Vector2.zero);
            resourceBar.anchorMax = Vector2.one;
            Surface(resourceBar, false);
            CreateText("Title", resourceBar, new Vector2(24, -18), new Vector2(250, 32),
                "FULGOR MEDIEVAL", 23, LegacyUI.Gold, FontStyle.Bold);
            CreateText("Campaign label", resourceBar, new Vector2(24, -57), new Vector2(250, 24),
                "MAPA DE CAMPAÑA", 14, Muted);
            var resources = CreateRect("Treasury", resourceBar, new Vector2(6 * 166 - 6, 84), Vector2.zero);
            resources.anchorMin = resources.anchorMax = resources.pivot = new Vector2(.5f, .5f);
            string[] names = { "GRANO", "MADERA", "PIEDRA", "ORO", "MANO DE OBRA" };
            for (int i = 0; i <= names.Length; i++)
            {
                var population = i == names.Length;
                var slot = CreateRect(population ? "Population" : "Resource " + i, resources,
                    new Vector2(160, 84), new Vector2(i * 166, 0));
                FulgorIcons.Create("Icon", slot, population ? FulgorIcon.Population : FulgorIcons.Resources5[i],
                    new Vector2(4, -20), 44);
                CreateText("Resource name", slot, new Vector2(54, -4), new Vector2(106, 20),
                    population ? "POBLACIÓN" : names[i], 13, Muted);
                var value = CreateText("Stock", slot, new Vector2(54, -25), new Vector2(106, 30), "—", 24, LegacyUI.Paper, FontStyle.Bold);
                var delta = CreateText("Next closing", slot, new Vector2(54, -57), new Vector2(106, 22),
                    population ? "sin censo" : "— al cierre", 14, Muted);
                if (population) { populationValue = value; continue; }
                resourceValues[i] = value;
                resourceDeltas[i] = delta;
            }

            panel = CreateRect("Map Information", canvasObject.transform, new Vector2(324, 228), new Vector2(20, -124));
            Surface(panel);
            CreateText("Title", panel, new Vector2(18, -16), new Vector2(288, 26), "TERRITORIO", 18, LegacyUI.Gold, FontStyle.Bold);
            details = CreateText("Selection", panel, new Vector2(18, -54), new Vector2(288, 72),
                "Selecciona una casilla del mapa.", 17, LegacyUI.Paper);
            Divider(panel, 135, 288);
            pathPreview = CreateText("Route Preview", panel, new Vector2(18, -148), new Vector2(288, 52),
                "Mayús + clic: comparar una ruta.\nClic derecho: ordenar la marcha.", 14, Muted);
            diagnostics = CreateText("Diagnostics", panel, new Vector2(18, -204), new Vector2(288, 18), "", 11, Muted);

            turnPanel = CreateRect("Turn Panel", canvasObject.transform, new Vector2(338, 424), new Vector2(-20, -124));
            turnPanel.anchorMin = turnPanel.anchorMax = turnPanel.pivot = Vector2.one;
            Surface(turnPanel);
            CreateText("Turn Title", turnPanel, new Vector2(18, -16), new Vector2(302, 26), "HUESTE ACTIVA", 18, LegacyUI.Gold, FontStyle.Bold);
            armySummary = CreateText("Army Summary", turnPanel, new Vector2(18, -54), new Vector2(302, 188), "Preparando hueste…", 16, LegacyUI.Paper);
            movementTrack = CreateRect("Movement track", turnPanel, new Vector2(302, 5), new Vector2(18, -246));
            var track = movementTrack.gameObject.AddComponent<Image>(); track.color = new Color(.16f, .19f, .20f); track.raycastTarget = false;
            movementFill = CreateRect("Movement remaining", movementTrack, new Vector2(302, 5), Vector2.zero);
            var fill = movementFill.gameObject.AddComponent<Image>(); fill.color = LegacyUI.Gold; fill.raycastTarget = false;
            MoveArmyButton = Action("Move Army", "ELEGIR DESTINO", turnPanel, new Vector2(18, -265), new Vector2(302, 46), true);
            MoveArmyButton.interactable = false;
            LocateArmyButton = Action("Locate Army", "LOCALIZAR", turnPanel, new Vector2(18, -323), new Vector2(145, 38));
            NextArmyButton = Action("Next Army", "OTRA HUESTE", turnPanel, new Vector2(175, -323), new Vector2(145, 38));
            NextArmyButton.interactable = false;
            marchStatus = CreateText("March Status", turnPanel, new Vector2(18, -375), new Vector2(164, 38), "Sin marcha pendiente", 13, Muted);
            CancelMarchButton = Action("Cancel March", "CANCELAR", turnPanel, new Vector2(194, -375), new Vector2(126, 32));

            turnControls = CreateRect("Turn closing", canvasObject.transform, new Vector2(338, 204), new Vector2(-20, 20));
            turnControls.anchorMin = turnControls.anchorMax = turnControls.pivot = new Vector2(1, 0);
            Surface(turnControls);
            turnSummary = CreateText("Turn Summary", turnControls, new Vector2(18, -14), new Vector2(140, 28), "Turno 1", 22, LegacyUI.Gold, FontStyle.Bold);
            LocateObjectiveButton = Action("Locate Objective", "VER OBJETIVO", turnControls, new Vector2(175, -12), new Vector2(145, 32));
            missionSummary = CreateText("Mission Summary", turnControls, new Vector2(18, -56), new Vector2(302, 70), "", 16, LegacyUI.Paper);
            EndTurnButton = Action("End Turn", "FINALIZAR TURNO", turnControls, new Vector2(18, -142), new Vector2(302, 46), true);

            utilityPanel = CreateRect("Campaign tools", canvasObject.transform, new Vector2(324, 158), new Vector2(20, 20));
            utilityPanel.anchorMin = utilityPanel.anchorMax = utilityPanel.pivot = Vector2.zero;
            Surface(utilityPanel);
            SaveButton = Action("Save", "GUARDAR", utilityPanel, new Vector2(16, -14), new Vector2(140, 36));
            LoadButton = Action("Load", "CARGAR", utilityPanel, new Vector2(168, -14), new Vector2(140, 36));
            saveStatus = CreateText("Save Status", utilityPanel, new Vector2(16, -60), new Vector2(292, 32), "", 13, Muted);
            NewGameButton = Action("New Game", "REINICIAR", utilityPanel, new Vector2(16, -108), new Vector2(140, 34));
            BattleLabButton = Action("Battle Lab", "LAB. COMBATE", utilityPanel, new Vector2(168, -108), new Vector2(140, 34));

            CampaignCameraController CameraControls() => Camera.main?.GetComponent<CampaignCameraController>();
            cameraControls = MapCameraControls.Create(canvasObject.transform, new Vector2(.5f, 0), new Vector2(0, 20),
                d => CameraControls()?.PanView(d), d => CameraControls()?.ZoomView(d), () => CameraControls()?.ResetView(), () => CameraControls()?.CanNavigate == true);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Campaign Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private static readonly Color Muted = new(.68f, .73f, .72f);
        private static readonly Color Positive = new(.57f, .80f, .64f);
        private static readonly Color Negative = new(.98f, .59f, .46f);

        private static void Surface(RectTransform rect, bool parchment = true)
        {
            rect.gameObject.AddComponent<Image>().raycastTarget = true;
            // Barra de recursos en madera; paneles de información en pergamino con tinta.
            if (parchment) LegacyUI.Parchment(rect);
            else LegacyUI.Panel(rect);
            Divider(rect, 0, rect.sizeDelta.x > 0 ? rect.sizeDelta.x - 36 : 0);
        }

        private static void Divider(RectTransform parent, float y, float width)
        {
            if (width <= 0) return;
            var line = CreateRect("Rule", parent, new Vector2(width, 1), new Vector2(18, -y));
            var image = line.gameObject.AddComponent<Image>(); image.color = new Color(.59f, .47f, .25f, .55f); image.raycastTarget = false;
        }

        private static Button Action(string name, string caption, Transform parent, Vector2 position, Vector2 size, bool primary = false)
        {
            var rect = CreateRect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = CreateText("Label", rect, new Vector2(8, -4), size - new Vector2(16, 8), caption, 14, LegacyUI.Paper, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            LegacyUI.Button(button);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 15;
            // Explicit commands retain their captions when disabled; colour is secondary to the order explanation.
            var colors = button.colors;
            colors.normalColor = primary ? new Color(.34f, .27f, .13f) : new Color(.10f, .13f, .14f);
            colors.highlightedColor = colors.selectedColor = new Color(.42f, .34f, .18f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }


        public void SetCell(MapCell? cell)
        {
            if (details == null) return;
            details.text = cell.HasValue
                ? $"{TerrainName(cell.Value.Terrain)}  ·  {cell.Value.Coordinates.Key}\n" +
                  (cell.Value.HasGold ? "Veta de oro presente" : "Sin veta de oro")
                : "Selecciona una casilla del mapa.\nLas ciudades abren su gestión con un clic.";
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
                pathPreview.text = "Mayús + clic: comparar una ruta.\nClic derecho: ordenar la marcha.";
        }

        public void SetTurnState(CampaignTurnState state)
        {
            if (turnSummary == null || state == null) return;
            var resources = state.Resources;
            var projected = state.ProjectedResources;
            turnSummary.text = $"Turno {state.Turn}";
            var stock = new[] { resources.Grain, resources.Wood, resources.Stone, resources.Gold, resources.Labor };
            var next = new[] { projected.Grain, projected.Wood, projected.Stone, projected.Gold, projected.Labor };
            for (int i = 0; i < stock.Length; i++)
            {
                var delta = next[i] - stock[i];
                resourceValues[i].text = stock[i].ToString("N0");
                resourceDeltas[i].text = $"{delta:+0;-0;0} al cierre";
                resourceDeltas[i].color = delta < 0 ? Negative : delta > 0 ? Positive : Muted;
            }
        }

        /// <summary>Las reglas aún no llevan censo: null deja un guion.</summary>
        public void SetPopulation(int? population)
        {
            if (populationValue != null) populationValue.text = population?.ToString("N0") ?? "—";
        }

        public void SetMultipleArmyMode(bool enabled)
        {
            if (NextArmyButton != null) NextArmyButton.interactable = enabled;
            if (MoveArmyButton != null) MoveArmyButton.GetComponentInChildren<Text>().text = enabled ?
                "MOVER HUESTE" : "MARCHAR AL DESTINO";
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
                $"Marcha a {destination.Value.Key}" : "Sin marcha pendiente";
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
                missionSummary.color=conclusion==CampaignConclusion.Victory?new Color(.10f,.40f,.16f):conclusion==CampaignConclusion.Defeat?new Color(.55f,.10f,.06f):LegacyUI.InkText;
            }
            if(EndTurnButton!=null&&conclusion!=CampaignConclusion.InProgress)EndTurnButton.interactable=false;
        }

        public void SetArmyState(ArmyMovementState army, string orderStatus, bool canMove)
        {
            if (armySummary != null)
                armySummary.text = army == null ? (string.IsNullOrEmpty(orderStatus) ? "Sin hueste disponible." : orderStatus) :
                    $"Posición {army.Position.Key} · Movimiento {army.RemainingMovement}/{army.MovementPerTurn}\n\n{orderStatus}";
            if (movementFill != null)
                movementFill.sizeDelta = new Vector2(302 * (army == null || army.MovementPerTurn <= 0 ? 0 : Mathf.Clamp01((float)army.RemainingMovement / army.MovementPerTurn)), 5);
            if (MoveArmyButton != null) MoveArmyButton.interactable = canMove;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition) => hudCanvas != null && hudCanvas.enabled &&
            (Contains(cameraControls, screenPosition) || Contains(resourceBar, screenPosition) ||
             Contains(panel, screenPosition) || Contains(turnPanel, screenPosition) ||
             Contains(turnControls, screenPosition) || Contains(utilityPanel, screenPosition));

        private static bool Contains(RectTransform rect, Vector2 point) =>
            rect != null && rect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rect, point);

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
