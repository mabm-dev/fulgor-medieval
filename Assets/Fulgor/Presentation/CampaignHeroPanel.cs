using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Panel de héroes en el mapa, a la izquierda sobre la barra de la hueste: héroe elegido, órdenes de unir, separar y contratar, y los
    /// cautivos con su rescate o canje. Incluye la ventana con la que el jugador decide el destino de un héroe vencido.
    /// Tiene su propio lienzo para no alterar la disposición del HUD.
    /// </summary>
    public sealed class CampaignHeroPanel : MonoBehaviour
    {
        private const int CaptiveRows = 4;
        private const float Width = 338, BaseHeight = 214, RowHeight = 40;
        private CampaignTurnController campaign;
        private GameObject root;
        private RectTransform box, surface, captiveArea;
        private Text summary, status;
        private Button attach, detach, recruit;
        private readonly Text[] captiveNames = new Text[CaptiveRows];
        private readonly Button[] captiveActions = new Button[CaptiveRows];
        private readonly string[] captiveIds = new string[CaptiveRows];
        private readonly string[] exchangeIds = new string[CaptiveRows];
        private GameObject fateDialog;
        private Text fateText;
        private string fateHeroId;
        private string captiveSignature;

        /// <summary>Botón de la fila de un cautivo (rescatar, canjear o pedir rescate), o null si la fila está vacía.</summary>
        public Button CaptiveAction(int row) => row >= 0 && row < CaptiveRows && captiveIds[row] != null ? captiveActions[row] : null;
        public string CaptiveHeroId(int row) => row >= 0 && row < CaptiveRows ? captiveIds[row] : null;
        public string StatusText => status != null ? status.text : "";

        public void Initialize(CampaignTurnController owner)
        {
            campaign = owner;
            if (root != null) return;
            root = CampaignMenuUI.Canvas("Héroes", transform, 24);
            root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0; // como el HUD de campaña
            box = CampaignMenuUI.Rect("Héroe", root.transform, new Vector2(0, 0), new Vector2(10, 180), new Vector2(Width, BaseHeight));
            box.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // recoge el puntero; la madera va en la superficie
            surface = CampaignHudArt.Stretch("Superficie", box);
            CampaignHudArt.Surface(surface);
            var title = CampaignMenuUI.Text("Title", box, new Vector2(0, 1), new Vector2(18, -10), new Vector2(300, 26), "HÉROES", 19, CampaignMenuUI.Gold);
            title.alignment = TextAnchor.MiddleLeft;
            summary = CampaignMenuUI.Text("Resumen del héroe", box, new Vector2(0, 1), new Vector2(18, -40), new Vector2(302, 96), "", 13, CampaignMenuUI.Paper);
            summary.alignment = TextAnchor.UpperLeft;
            attach = Action("Unir", "UNIR", new Vector2(14, -140), () => Run(campaign.TryAttachAtSelection));
            detach = Action("Separar", "SEPARAR", new Vector2(118, -140), () => Run(campaign.TryDetachToSelection));
            recruit = Action("Contratar", "CONTRATAR", new Vector2(222, -140), () => Run(campaign.TryRecruitHeroAtSelection));
            status = CampaignMenuUI.Text("Estado del héroe", box, new Vector2(0, 1), new Vector2(18, -182), new Vector2(302, 26), "", 12, CampaignMenuUI.Gold);
            status.alignment = TextAnchor.MiddleLeft;

            // cautivos: una fila por héroe, con la acción que le corresponde; el panel crece hacia arriba
            captiveArea = CampaignMenuUI.Rect("Cautivos", box, new Vector2(0, 1), new Vector2(0, -BaseHeight + 4), new Vector2(Width, CaptiveRows * RowHeight + 30));
            var heading = CampaignMenuUI.Text("Title", captiveArea, new Vector2(0, 1), new Vector2(18, -2), new Vector2(300, 24), "CAUTIVOS", 15, CampaignMenuUI.Gold);
            heading.alignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < CaptiveRows; i++)
            {
                int row = i;
                captiveNames[i] = CampaignMenuUI.Text("Cautivo " + i, captiveArea, new Vector2(0, 1), new Vector2(18, -28 - i * RowHeight), new Vector2(170, 34), "", 13, CampaignMenuUI.Paper);
                captiveNames[i].alignment = TextAnchor.MiddleLeft;
                captiveActions[i] = CampaignHudArt.Button("Acción de cautivo " + i, captiveArea, new Vector2(192, -28 - i * RowHeight), new Vector2(132, 34), "", () => ActOnCaptive(row));
                captiveActions[i].GetComponentInChildren<Text>().fontSize = 12;
            }
            captiveArea.gameObject.SetActive(false);

            fateDialog = CampaignMenuUI.Full("Destino del héroe", root.transform, new Color(0, 0, 0, .65f)).gameObject;
            var dialog = CampaignMenuUI.Rect("Decisión", fateDialog.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 320));
            CampaignHudArt.Surface(dialog);
            CampaignMenuUI.Text("Title", dialog, new Vector2(.5f, .5f), new Vector2(0, 110), new Vector2(640, 44), "HÉROE VENCIDO", 26, CampaignMenuUI.Gold);
            fateText = CampaignMenuUI.Text("Texto de la decisión", dialog, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(640, 110), "", 20, CampaignMenuUI.Paper);
            var choices = CampaignMenuUI.Rect("Opciones", dialog, new Vector2(.5f, .5f), new Vector2(0, -100), new Vector2(560, 56));
            CampaignHudArt.Button("Apresar", choices, Vector2.zero, new Vector2(270, 56), "APRESAR", () => Decide(HeroFate.Capture));
            CampaignHudArt.Button("Ejecutar", choices, new Vector2(290, 0), new Vector2(270, 56), "EJECUTAR", () => Decide(HeroFate.Execute));
            fateDialog.SetActive(false);
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 5;
        }

        public bool BlocksPointer(Vector2 screenPosition) =>
            root != null && root.activeInHierarchy && root.GetComponent<Canvas>().enabled &&
            (fateDialog.activeSelf || box.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(box, screenPosition));

        private void Update()
        {
            if (root == null || campaign == null) return;
            bool visible = campaign.Heroes != null && campaign.isActiveAndEnabled;
            var hud = campaign.GetComponent<CampaignSelectionPanel>();
            var canvas = root.GetComponent<Canvas>();
            var city = campaign.GetComponent<CityManagementView>();
            var battle = campaign.GetComponent<BattleLabController>();
            canvas.enabled = visible && (hud == null || hud.IsVisible) && (city == null || !city.IsOpen) && (battle == null || !battle.IsOpen);
            if (!canvas.enabled) return;
            summary.text = campaign.HeroSummary();
            bool hero = campaign.SelectedArmyHasHero;
            attach.interactable = true; detach.interactable = hero; recruit.interactable = true;
            RefreshCaptives();
            var decision = campaign.PlayerHeroDecisions.FirstOrDefault();
            fateHeroId = decision?.HeroId;
            fateDialog.SetActive(decision != null);
            if (decision != null)
                fateText.text = $"{HeroCatalog.Find(decision.HeroId)?.Name} ha perdido su hueste y está a tu merced.\n" +
                    "Apresarlo permite pedir rescate o intercambiarlo; ejecutarlo lo elimina para siempre.";
        }

        /// <summary>Vuelve a leer los cautivos y ajusta las filas y la altura del panel.</summary>
        public void RefreshCaptives()
        {
            var own = campaign.OwnCaptives; var held = campaign.HeldCaptives;
            string signature = string.Join("|", own.Select(h => h.HeroId)) + "#" + string.Join("|", held.Select(h => h.HeroId));
            if (signature == captiveSignature) return;
            captiveSignature = signature;
            var rows = new List<(CampaignHero hero, string exchange)>();
            var free = held.ToList();
            // un héroe propio se canjea por uno ajeno si el reino que lo retiene es el del cautivo que guardas; si no, se rescata con oro
            foreach (var hero in own)
            {
                var match = free.FirstOrDefault(h => campaign.Heroes.CanExchange(hero.HeroId, h.HeroId));
                if (match != null) free.Remove(match);
                rows.Add((hero, match?.HeroId));
            }
            foreach (var hero in free) rows.Add((hero, null));
            int count = Mathf.Min(rows.Count, CaptiveRows);
            for (int i = 0; i < CaptiveRows; i++)
            {
                bool used = i < count;
                captiveNames[i].gameObject.SetActive(used); captiveActions[i].gameObject.SetActive(used);
                captiveIds[i] = used ? rows[i].hero.HeroId : null; exchangeIds[i] = used ? rows[i].exchange : null;
                if (!used) continue;
                var hero = rows[i].hero; bool mine = hero.FactionId == campaign.PlayerFactionId; int gold = CampaignHeroRoster.RansomGold(hero);
                captiveNames[i].text = hero.Definition.Name + "\n" + (mine ? "Preso de " + FactionName(hero.CaptorFactionId) : "En tu poder · " + FactionName(hero.FactionId));
                captiveActions[i].GetComponentInChildren<Text>().text = mine ? (rows[i].exchange != null ? "CANJEAR" : "RESCATAR · " + gold) : "PEDIR · " + gold;
            }
            captiveArea.gameObject.SetActive(count > 0);
            box.sizeDelta = new Vector2(Width, BaseHeight + (count > 0 ? 30 + count * RowHeight : 0));
            // el contenido cuelga de arriba: al crecer el panel, las filas de cautivos quedan debajo de las órdenes
        }

        private void ActOnCaptive(int row)
        {
            string id = captiveIds[row]; if (id == null || campaign == null) return;
            if (!campaign.Heroes.TryGet(id, out var hero)) return;
            string message;
            if (hero.FactionId != campaign.PlayerFactionId) campaign.TryDemandRansom(id, out message);
            else if (exchangeIds[row] != null) campaign.TryExchangeCaptives(id, exchangeIds[row], out message);
            else campaign.TryPayRansom(id, out message);
            status.text = message; captiveSignature = null; RefreshCaptives();
        }

        private static string FactionName(string factionId) =>
            string.IsNullOrEmpty(factionId) ? "otro reino" : char.ToUpperInvariant(factionId[0]) + factionId.Substring(1);

        private void Run(TryOrder order)
        {
            order(out var message);
            status.text = message;
        }

        private delegate bool TryOrder(out string message);

        private void Decide(HeroFate fate)
        {
            if (fateHeroId != null) campaign.ResolveHeroFate(fateHeroId, fate);
        }

        private Button Action(string name, string caption, Vector2 position, UnityEngine.Events.UnityAction click)
        {
            var button = CampaignHudArt.Button(name, box, position, new Vector2(100, 36), caption, click);
            button.GetComponentInChildren<Text>().fontSize = 12;
            return button;
        }
    }
}
