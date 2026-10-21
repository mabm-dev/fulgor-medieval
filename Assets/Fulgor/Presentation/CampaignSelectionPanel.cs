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
        private Text details;
        private Text diagnostics;
        private Text turnSummary;

        public bool IsVisible => panel != null && panel.gameObject.activeInHierarchy;
        public RectTransform PanelRectTransform => panel;
        public Button EndTurnButton { get; private set; }

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
                new Vector2(360f, 230f), new Vector2(24f, -24f));
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.055f, 0.06f, 0.052f, 0.94f);
            background.raycastTarget = false;

            CreateText("Title", panel, new Vector2(18f, -14f), new Vector2(324f, 26f),
                "MAPA DE CAMPAÑA", 18, new Color(0.93f, 0.68f, 0.16f), FontStyle.Bold);
            details = CreateText("Selection", panel, new Vector2(18f, -48f),
                new Vector2(324f, 66f), "Selecciona una casilla para ver sus datos.",
                15, new Color(0.94f, 0.88f, 0.72f));
            CreateText("Legend", panel, new Vector2(18f, -118f), new Vector2(324f, 42f),
                "Agua  ·  Llanura  ·  Bosque  ·  Colina  ·  Montaña", 13,
                new Color(0.72f, 0.78f, 0.68f));
            CreateText("Controls", panel, new Vector2(18f, -158f), new Vector2(324f, 24f),
                "WASD/Flechas: mover   Rueda: zoom   Esc: limpiar", 12,
                new Color(0.68f, 0.70f, 0.64f));
            diagnostics = CreateText("Diagnostics", panel, new Vector2(18f, -190f),
                new Vector2(324f, 22f), "Construyendo tablero…", 11,
                new Color(0.50f, 0.57f, 0.48f));

            turnPanel = CreateRect("Turn Panel", canvasObject.transform,
                new Vector2(370f, 194f), new Vector2(-24f, -24f));
            turnPanel.anchorMin = Vector2.one;
            turnPanel.anchorMax = Vector2.one;
            turnPanel.pivot = Vector2.one;
            var turnBackground = turnPanel.gameObject.AddComponent<Image>();
            turnBackground.color = new Color(0.055f, 0.06f, 0.052f, 0.94f);
            turnBackground.raycastTarget = false;
            CreateText("Turn Title", turnPanel, new Vector2(18f, -14f),
                new Vector2(334f, 26f), "CAMPAÑA · PROTOTIPO", 18,
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

        public void SetDiagnostics(BoardBuildMetrics metrics)
        {
            if (diagnostics == null) return;
            diagnostics.text = $"{metrics.Tiles} casillas · {metrics.Renderers} renderers · " +
                $"{metrics.Milliseconds:F1} ms";
        }

        public void SetTurnState(CampaignTurnState state)
        {
            if (turnSummary == null || state == null) return;
            var resources = state.Resources;
            var income = state.IncomePerTurn;
            turnSummary.text = $"Turno {state.Turn}\n" +
                $"Grano {resources.Grain} · Madera {resources.Wood} · Piedra {resources.Stone}\n" +
                $"Oro {resources.Gold} · Mano de obra {resources.Labor}\n" +
                $"Ingreso provisional: +{income.Grain} grano, +{income.Gold} oro";
        }

        public bool ContainsScreenPoint(Vector2 screenPosition) =>
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
