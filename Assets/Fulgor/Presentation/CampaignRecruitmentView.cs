using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Recruitment and deployment over the live campaign military state.</summary>
    public sealed partial class CampaignRecruitmentView : MonoBehaviour
    {
        private CampaignTurnController campaign;
        private string cityId, selectedFormation;
        private GameObject root;
        private RectTransform body;
        private int selectedProfile, observedRevision = -1;
        private CampaignTurnState observedState;
        private CampaignMilitaryState observedMilitary;
        private CampaignArmyRegistry observedArmies;
        private bool armySource, observedRivalPhase;
        private readonly Button[] choices = new Button[4];
        private readonly GameObject[] borders = new GameObject[4];
        private readonly Image[] portraits = new Image[4];
        private readonly Text[] locks = new Text[4], reserves = new Text[5], costs = new Text[5];
        private Text unitName, category, attributes, quantity, duration, discount, reason, pendingName, pendingTime;
        private Image illustration, pendingPortrait;
        private RectTransform progress;

        public Button RecruitButton { get; private set; }
        public Button TransferButton { get; private set; }
        public Button MusterButton { get; private set; }
        public Button EndTurnButton { get; private set; }
        public Button BackButton { get; private set; }
        public bool IsOpen => root != null;
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        private int closedFrame = -1;

        public bool Open(string settlementId)
        {
            campaign = GetComponent<CampaignTurnController>();
            var towns = GetComponent<CampaignSettlementController>();
            if (campaign?.State == null || towns == null || !towns.CanManage(settlementId) || campaign.Military == null ||
                campaign.ArmyRegistry == null || campaign.HasPendingBattle || campaign.IsRivalPhaseActive || campaign.IsCampaignFinished) return false;
            Close();
            GetComponent<CampaignCityMapView>()?.Close();
            GetComponent<CityManagementView>()?.Close();
            cityId = settlementId;
            selectedProfile = 0;
            armySource = false;
            selectedFormation = null;
            try { ChooseVisitingArmy(); Build(); Refresh(); }
            catch { Close(); throw; }
            return true;
        }

        public void Close()
        {
            if (root == null) return;
            EndRosterDrag();
            root.SetActive(false);
            Destroy(root);
            root = null;
            closedFrame = Time.frameCount;
        }

        private void OnDisable() => Close();
        private void Update()
        {
            if (!IsOpen) return;
            if (!campaign.SettlementState.TryGet(cityId, out var city) || city.OwnerFactionId != campaign.PlayerFactionId || campaign.HasPendingBattle)
            { Close(); return; }
            if (observedState != campaign.State || observedRevision != campaign.CityRevision || observedMilitary != campaign.Military ||
                observedArmies != campaign.ArmyRegistry || observedRivalPhase != campaign.IsRivalPhaseActive) Refresh();
        }

        private RectTransform Panel(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = CampaignMenuUI.Rect(name, parent, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            CampaignHudArt.Surface(rect);
            return rect;
        }

        private Text Label(string name, Transform parent, float x, float y, float width, float height, string value, int size = 20, bool gold = false)
        {
            var label = CampaignMenuUI.Text(name, parent, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height), value,
                size, gold ? CampaignMenuUI.Gold : CampaignMenuUI.Paper);
            label.alignment = TextAnchor.MiddleLeft;
            return label;
        }

        private static Sprite Preview(int index) => CampaignHudArt.Slice("recruit-preview-" + index, "Recruitment/castilla",
            new Rect(index % 2 * .5f, index < 2 ? .5f : 0, .5f, .5f));

        internal static Sprite Portrait(int index) => CampaignHudArt.Slice("recruit-portrait-" + index, "Recruitment/castilla",
            new Rect((index % 2 * 768 + 160) / 1536f, (index < 2 ? 1024 - 330 : 512 - 330) / 1024f, 450 / 1536f, 310 / 1024f));

        // A close crop keeps faces and class equipment readable in the small roster slots.
        internal static Sprite StackPortrait(int index) => CampaignHudArt.Slice("recruit-stack-" + index, "Recruitment/castilla",
            new Rect((index % 2 * 768 + (index == 3 ? 200 : 255)) / 1536f,
                (index < 2 ? 1024 - 265 : 512 - 265) / 1024f,
                (index == 3 ? 370 : 285) / 1536f, 245 / 1024f));
        private Image Picture(string name, Transform parent, float x, float y, float width, float height, Sprite sprite)
        {
            var rect = CampaignMenuUI.Rect(name, parent, new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        private GameObject Selection(Transform parent)
        {
            var rect = CampaignHudArt.Stretch("Selección", parent, 7);
            rect.gameObject.AddComponent<CitySelectionFrame>().raycastTarget = false;
            return rect.gameObject;
        }

        private void Build()
        {
            root = CampaignMenuUI.Canvas("Reclutamiento", transform, 170);
            // Fit the entire composition for narrow and ultrawide displays.
            root.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var backdrop = CampaignMenuUI.Full("Fondo de reclutamiento", root.transform, new Color(.045f, .032f, .023f, 1));
            body = CampaignMenuUI.Rect("Cuartel", backdrop, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1880, 1040));
            var header = Panel("Cabecera", body, 0, 0, 1880, 108);
            var banner = CampaignHudArt.KingdomBanner(header);
            banner.sizeDelta = new Vector2(350, 124);
            Label("Title", header, 395, 10, 510, 42, "RECLUTAMIENTO", 30, true);
            Label("Ciudad", header, 395, 56, 500, 32, campaign.SettlementState.Settlements.First(c => c.Id == cityId).Name, 21);
            var stock = CampaignMenuUI.Rect("Recursos", header, new Vector2(0, 1), new Vector2(925, -12), new Vector2(680, 82));
            for (int i = 0; i < reserves.Length; i++)
            {
                CampaignHudArt.Resource(stock, i, new Vector2(i * 135 + 4, -22), 38);
                reserves[i] = Label("Reserva " + i, stock, i * 135 + 48, 21, 82, 38, "", 23);
            }
            BackButton = CampaignHudArt.Button("Volver al mapa", header, new Vector2(1635, -28), new Vector2(228, 52), "VOLVER AL MAPA", Close);

            var catalogue = Panel("Tropas disponibles", body, 0, 124, 535, 550);
            Label("Title", catalogue, 24, 8, 487, 44, "TROPAS DISPONIBLES", 25, true);
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                var button = CampaignHudArt.Button("Elegir tropa " + i, catalogue, new Vector2(18 + i % 2 * 254, -54 - i / 2 * 238),
                    new Vector2(245, 214), "", () => SelectTroop(index));
                choices[i] = button;
                portraits[i] = Picture("Retrato", button.transform, 10, 10, 225, 132, Portrait(i));
                Label("Nombre", button.transform, 14, 144, 217, 30, Name((TroopProfile)i), 19).alignment = TextAnchor.MiddleCenter;
                locks[i] = Label("Requisito", button.transform, 12, 177, 221, 31, "", 13, true);
                locks[i].alignment = TextAnchor.MiddleCenter;
                borders[i] = Selection(button.transform);
            }
            Label("Edad", catalogue, 24, 516, 487, 26, CampaignTechnologies.AgeNames[(int)campaign.Research.Age], 20, true);

            var preview = Panel("Vista de tropa", body, 550, 124, 695, 550);
            unitName = Label("Title", preview, 20, 8, 655, 44, "", 28, true);
            unitName.alignment = TextAnchor.MiddleCenter;
            category = Label("Tipo", preview, 20, 48, 655, 28, "", 18);
            category.alignment = TextAnchor.MiddleCenter;
            illustration = Picture("Ilustración de tropa", preview, 14, 82, 667, 400, null);
            var stats = Panel("Atributos", preview, 14, 486, 667, 50);
            attributes = Label("Valores", stats, 20, 5, 627, 40, "", 21);
            attributes.alignment = TextAnchor.MiddleCenter;

            var order = Panel("Formar una leva", body, 1260, 124, 620, 350);
            Label("Title", order, 24, 9, 572, 42, "FORMAR UNA LEVA", 27, true);
            quantity = Label("Efectivos", order, 24, 54, 290, 38, "", 30);
            duration = Label("Duración", order, 330, 54, 264, 38, "", 26);
            var price = Panel("Coste", order, 18, 103, 584, 76);
            for (int i = 0; i < costs.Length; i++)
            {
                CampaignHudArt.Resource(price, i, new Vector2(10 + i * 114, -20), 35);
                costs[i] = Label("Coste " + i, price, 48 + i * 114, 17, 62, 42, "", 24);
            }
            discount = Label("Beneficio del campo", order, 24, 185, 572, 28, "", 19, true);
            RecruitButton = CampaignHudArt.Button("Reclutar leva", order, new Vector2(24, -220), new Vector2(572, 52), "RECLUTAR", Recruit);
            reason = Label("Disponibilidad", order, 24, 280, 572, 56, "", 18);

            var training = Panel("En formación", body, 1260, 488, 620, 186);
            Label("Title", training, 24, 8, 572, 38, "EN FORMACIÓN", 25, true);
            pendingPortrait = Picture("Leva en curso", training, 20, 49, 100, 72, null);
            pendingName = Label("Leva", training, 136, 48, 455, 30, "", 21);
            pendingTime = Label("Restante", training, 136, 80, 455, 28, "", 20, true);
            var track = CampaignMenuUI.Rect("Esfuerzo pendiente", training, new Vector2(0, 1), new Vector2(136, -121), new Vector2(455, 10));
            CampaignMenuUI.Background(track, new Color(.08f, .06f, .04f));
            progress = CampaignMenuUI.Rect("Progreso de leva", track, new Vector2(0, 1), Vector2.zero, new Vector2(0, 10));
            CampaignMenuUI.Background(progress, CampaignMenuUI.Gold);
            Label("Destino", training, 24, 145, 572, 24, "Al completarse, se incorporan a la guarnición.", 18);

            var accent = CampaignHudArt.Stretch("Paño de reclutamiento", RecruitButton.transform, 7);
            CampaignMenuUI.Background(accent, new Color(.34f,.035f,.055f,.68f));
            accent.GetComponent<Image>().raycastTarget = false;
            accent.SetSiblingIndex(1);
            BuildRoster();
        }

        private string Name(TroopProfile profile) => campaign.PlayerFactionId == "castilla" ? profile switch
        {
            TroopProfile.MenAtArms => "Hombres de armas",
            TroopProfile.LightCavalry => "Jinetes de leva",
            TroopProfile.Crossbowmen => "Ballesteros de leva",
            _ => RecruitmentCatalog.Get(profile).Name
        } : RecruitmentCatalog.Get(profile).Name;

        private string Requirement(TroopProfile profile) => campaign.CanRecruitProfile(profile) ? "Disponible" :
            profile == TroopProfile.Crossbowmen ? "Oficio de ballestería" : "Organización de la frontera";

        public void SelectTroop(int index)
        {
            if (index < 0 || index >= choices.Length || !IsOpen) return;
            selectedProfile = index; Refresh();
        }

        private CampaignArmyOnMap ActiveArmy()
        {
            campaign.ArmyRegistry.TryGetArmy(campaign.SelectedArmyId, out var army);
            return army;
        }

        internal static int Profile(CampaignFormationState formation)
        {
            foreach (var spec in RecruitmentCatalog.All)
            {
                var a = TroopCatalog.Get(spec.Profile).Attributes;
                if (a.Attack == formation.Attributes.Attack && a.Defense == formation.Attributes.Defense && a.Range == formation.Attributes.Range)
                    return (int)spec.Profile;
            }
            return formation.Kind == FormationKind.Cavalry ? 3 : formation.Kind == FormationKind.Ranged ? 2 : 0;
        }

        private static int[] Amounts(CampaignResources resources) => new[] { resources.Grain, resources.Wood, resources.Stone, resources.Gold, resources.Labor };

        private void Refresh()
        {
            if (!IsOpen) return;
            observedRevision = campaign.CityRevision; observedState = campaign.State;
            observedMilitary = campaign.Military; observedArmies = campaign.ArmyRegistry; observedRivalPhase = campaign.IsRivalPhaseActive;
            var spec = RecruitmentCatalog.All[selectedProfile];
            var troop = TroopCatalog.Get(spec.Profile);
            bool camp = campaign.TileCity(cityId)?.HasMilitiaCamp == true;
            var cost = spec.CostInCity(camp);
            var stock = Amounts(campaign.State.Resources); var price = Amounts(cost);
            for (int i = 0; i < reserves.Length; i++)
            {
                reserves[i].text = stock[i].ToString("N0"); costs[i].text = price[i].ToString();
                costs[i].color = stock[i] >= price[i] ? CampaignMenuUI.Paper : new Color(1f, .49f, .42f);
            }
            for (int i = 0; i < choices.Length; i++)
            {
                bool unlocked = campaign.CanRecruitProfile((TroopProfile)i);
                locks[i].text = unlocked ? "Disponible" : "Bloqueada · " + Requirement((TroopProfile)i);
                portraits[i].color = unlocked ? Color.white : new Color(.48f, .45f, .42f);
                borders[i].SetActive(i == selectedProfile);
            }
            unitName.text = Name(spec.Profile).ToUpperInvariant();
            category.text = troop.Kind == FormationKind.Cavalry ? "CABALLERÍA · MOVILIDAD" : troop.Kind == FormationKind.Ranged ? "INFANTERÍA · ALCANCE LARGO" : "INFANTERÍA · ALCANCE CORTO";
            illustration.sprite = Preview(selectedProfile);
            attributes.text = $"ATAQUE  {troop.Attributes.Attack}     DEFENSA  {troop.Attributes.Defense}     MOVIMIENTO  {troop.Attributes.Movement}";
            quantity.text = spec.Quantity + " SOLDADOS"; duration.text = spec.Turns + " TURNOS";
            discount.text = camp && spec.Profile == TroopProfile.Spearmen ? "Campo de milicias: −4 de mano de obra" : "";
            var job = campaign.Military.Pending(cityId);
            bool allowed = !campaign.HasPendingBattle && !campaign.IsRivalPhaseActive && !campaign.IsCampaignFinished;
            bool unlockedProfile = campaign.CanRecruitProfile(spec.Profile);
            bool affordable = campaign.State.Resources.TrySpend(cost, out _);
            RecruitButton.GetComponentInChildren<Text>().text = "RECLUTAR · " + spec.Quantity + " SOLDADOS";
            RecruitButton.interactable = allowed && unlockedProfile && campaign.Military.TryRecruit(cityId, campaign.PlayerFactionId, spec.Profile,
                campaign.State.Turn, campaign.State.Resources, campaign.ArmyRegistry, out _, out _, camp);
            reason.text = !allowed ? "Espera a que termine el turno." : !unlockedProfile ? "Requiere investigar " + Requirement(spec.Profile) + "." :
                job != null ? "Ya hay una leva en formación en esta ciudad." : !affordable ? "Recursos insuficientes. Revisa el coste en rojo." :
                !RecruitButton.interactable ? "No hay plaza libre o hay una hueste rival en la ciudad." : "Leva disponible · se paga una vez al comenzar.";
            reason.color = RecruitButton.interactable ? new Color(.73f, .87f, .65f) : CampaignMenuUI.Gold;
            pendingName.text = job == null ? "Sin leva en curso" : Name(job.profile);
            int remaining = job == null ? 0 : Math.Max(0, job.completionTurn - campaign.State.Turn);
            pendingTime.text = job == null ? "Elige una tropa para reclutar." : RecruitmentCatalog.Get(job.profile).Quantity + " soldados · " + remaining + " turnos restantes";
            pendingPortrait.enabled = job != null;
            if (job != null) pendingPortrait.sprite = Portrait((int)job.profile);
            progress.sizeDelta = new Vector2(job == null ? 0 : 455 * Mathf.Clamp01(1 - remaining / (float)(job.completionTurn - job.startTurn)), 10);
            RefreshRoster(allowed);
        }

        private void Recruit() { campaign.TryRecruit(cityId, (TroopProfile)selectedProfile); Refresh(); }
        private void Transfer()
        {
            if (armySource) campaign.TryLeaveGarrison(cityId, selectedFormation);
            else campaign.TryAssignGarrison(cityId, selectedFormation);
            Refresh();
        }
        private void Muster() { campaign.TryMuster(cityId, selectedFormation); Refresh(); }
    }
}

