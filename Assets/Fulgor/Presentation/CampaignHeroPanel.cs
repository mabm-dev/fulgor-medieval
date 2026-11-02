using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Panel del héroe en el mapa, abajo a la derecha, y ventana con la que el jugador decide el
    /// destino de un héroe vencido. Tiene su propio lienzo para no alterar la disposición del HUD.
    /// </summary>
    public sealed class CampaignHeroPanel : MonoBehaviour
    {
        private CampaignTurnController campaign;
        private GameObject root;
        private RectTransform box;
        private Text summary, status;
        private Button attach, detach, recruit;
        private GameObject fateDialog;
        private Text fateText;
        private string fateHeroId;

        public void Initialize(CampaignTurnController owner)
        {
            campaign = owner;
            if (root != null) return;
            root = CampaignMenuUI.Canvas("Héroes", transform, 24);
            box = CampaignMenuUI.Rect("Héroe", root.transform, new Vector2(1, 0), new Vector2(-20, 236), new Vector2(338, 232));
            CampaignMenuUI.Background(box, LegacyUI.Ink);
            box.GetComponent<Image>().raycastTarget = true;
            LegacyUI.Parchment(box);
            var title = CampaignMenuUI.Text("Title", box, new Vector2(0, 1), new Vector2(18, -14), new Vector2(300, 24), "HÉROE", 18, LegacyUI.Gold);
            title.alignment = TextAnchor.MiddleLeft;
            summary = CampaignMenuUI.Text("Resumen del héroe", box, new Vector2(0, 1), new Vector2(18, -42), new Vector2(302, 112), "", 13, LegacyUI.Paper);
            summary.alignment = TextAnchor.UpperLeft;
            attach = Action("UNIR", new Vector2(18, -160), () => Run(campaign.TryAttachAtSelection));
            detach = Action("SEPARAR", new Vector2(122, -160), () => Run(campaign.TryDetachToSelection));
            recruit = Action("CONTRATAR", new Vector2(226, -160), () => Run(campaign.TryRecruitHeroAtSelection));
            status = CampaignMenuUI.Text("Estado del héroe", box, new Vector2(0, 1), new Vector2(18, -198), new Vector2(302, 26), "", 12, LegacyUI.Paper);
            status.alignment = TextAnchor.MiddleLeft;

            fateDialog = CampaignMenuUI.Full("Destino del héroe", root.transform, new Color(0, 0, 0, .65f)).gameObject;
            var dialog = CampaignMenuUI.Rect("Decisión", fateDialog.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(720, 320));
            CampaignMenuUI.Background(dialog, LegacyUI.Ink);
            LegacyUI.Parchment(dialog);
            CampaignMenuUI.Text("Title", dialog, new Vector2(.5f, .5f), new Vector2(0, 110), new Vector2(640, 44), "HÉROE VENCIDO", 26, LegacyUI.Gold);
            fateText = CampaignMenuUI.Text("Texto de la decisión", dialog, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(640, 110), "", 20, LegacyUI.Paper);
            CampaignMenuUI.Button("Apresar", dialog, new Vector2(.5f, .5f), new Vector2(-150, -100), new Vector2(260, 56), "APRESAR", () => Decide(HeroFate.Capture));
            CampaignMenuUI.Button("Ejecutar", dialog, new Vector2(.5f, .5f), new Vector2(150, -100), new Vector2(260, 56), "EJECUTAR", () => Decide(HeroFate.Execute));
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
            var decision = campaign.PlayerHeroDecisions.FirstOrDefault();
            fateHeroId = decision?.HeroId;
            fateDialog.SetActive(decision != null);
            if (decision != null)
                fateText.text = $"{HeroCatalog.Find(decision.HeroId)?.Name} ha perdido su hueste y está a tu merced.\n" +
                    "Apresarlo permite pedir rescate o intercambiarlo; ejecutarlo lo elimina para siempre.";
        }

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

        private Button Action(string caption, Vector2 position, UnityEngine.Events.UnityAction click)
        {
            var button = CampaignMenuUI.Button(caption, box, new Vector2(0, 1), position, new Vector2(96, 34), caption, click);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = 13;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }
    }
}
