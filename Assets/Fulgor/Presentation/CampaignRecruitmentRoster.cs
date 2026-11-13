using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignRecruitmentView
    {
        private readonly Button[,] rosterSlots = new Button[2, 6];
        private readonly Image[,] rosterPortraits = new Image[2, 6];
        private readonly Text[,] rosterCounts = new Text[2, 6];
        private readonly GameObject[,] rosterBorders = new GameObject[2, 6];
        private readonly RawImage[] heroPortraits = new RawImage[2];
        private readonly Text[] heroNames = new Text[2];
        private readonly GameObject[] heroBorders = new GameObject[2];
        private Text cityRosterTitle, visitingTitle, rosterNotice;
        private Button heroTransfer;
        private bool selectedHero, dragging, dragArmy, dragHero;
        private string dragId;
        private RectTransform dragGhost;
        private int lastDragFrame = -1;
        private string rosterMessage = "";

        private CampaignArmyOnMap VisitingArmy()
        {
            var army = ActiveArmy();
            return army != null && campaign.SettlementState.TryGet(cityId, out var city) &&
                army.Movement.Position.DistanceTo(city.Position) <= 1 ? army : null;
        }
        private CampaignTroopStack[] RowStacks(bool army) => CampaignTroopStack.Group(
            army ? VisitingArmy()?.Army.Formations : campaign.Military.Garrison(cityId)?.Formations).ToArray();

        private CampaignArmyOnMap[] NearbyArmies() => campaign.ArmyRegistry.Armies.Where(a => a.Army.FactionId == campaign.PlayerFactionId &&
            campaign.SettlementState.TryGet(cityId, out var city) && a.Movement.Position.DistanceTo(city.Position) <= 1).ToArray();

        private void ChooseVisitingArmy()
        {
            selectedHero = false; rosterMessage = "";
            if(campaign.SettlementState.TryGet(cityId,out var city) &&
                campaign.ArmyRegistry.TryGetOccupant(city.Position,out var stationed) && stationed.Army.FactionId==campaign.PlayerFactionId)
            {
                campaign.SelectArmyForRoster(stationed.Army.Id);
                return;
            }
            if (VisitingArmy() != null) return;
            var near = NearbyArmies().FirstOrDefault();
            if (near != null) campaign.SelectArmyForRoster(near.Army.Id);
        }

        public void FocusRoster(bool army,bool hero)
        {
            armySource=army;selectedFormation=null;selectedHero=false;
            if(hero)
            {
                var position=campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
                selectedHero=army?VisitingArmy()!=null&&position.armyId==campaign.SelectedArmyId:position.settlementId==cityId;
            }
            else selectedFormation=RowStacks(army).FirstOrDefault()?.Id;
            Refresh();
        }

        private void BuildRoster()
        {
            var roster = Panel("Guarnición", body, 0, 690, 1880, 350);
            cityRosterTitle = Label("Title", roster, 24, 16, 252, 76, "GUARNICIÓN\nDE LA CIUDAD", 22, true);
            visitingTitle = Label("Hueste visitante", roster, 24, 182, 252, 90, "", 21, true);
            CampaignHudArt.Button("Otra hueste visitante", roster, new Vector2(24, -278), new Vector2(245, 34), "OTRA HUESTE", () =>
            {
                var near = NearbyArmies(); if (near.Length == 0) return;
                int index = System.Array.FindIndex(near, a => a.Army.Id == campaign.SelectedArmyId);
                campaign.SelectArmyForRoster(near[(index + 1) % near.Length].Army.Id);
                selectedFormation = null; selectedHero = false; Refresh();
            });
            Label("Capacidad", roster, 24, 98, 245, 52, "6 formaciones de defensa.\nIncluye la leva en curso.", 17);
            for (int row = 0; row < 2; row++)
            {
                bool army = row == 1;
                int top = 20 + row * 160;
                var hero = CampaignHudArt.Button(army ? "Héroe visitante" : "Héroe en guarnición", roster,
                    new Vector2(295, -top), new Vector2(135, 136), "", () => SelectRosterHero(army));
                var portraitRect = CampaignMenuUI.Rect("Retrato de héroe", hero.transform, new Vector2(0, 1), new Vector2(8, -6), new Vector2(119, 91));
                heroPortraits[row] = portraitRect.gameObject.AddComponent<RawImage>(); heroPortraits[row].raycastTarget = false;
                heroPortraits[row].texture = Resources.Load<Texture2D>("KingdomSelection/hero-" + campaign.PlayerFactionId);
                heroPortraits[row].uvRect = new Rect(.25f, .38f, .43f, .58f);
                heroNames[row] = Label("Nombre de héroe", hero.transform, 7, 99, 121, 30, "", 16, true);
                heroNames[row].alignment = TextAnchor.MiddleCenter;
                heroBorders[row] = Selection(hero.transform);
                hero.gameObject.AddComponent<CampaignRosterSlot>().Initialize(this, army, -1, true);
                for (int i = 0; i < 6; i++)
                {
                    int index = i;
                    string name = army ? "Formación hueste " + i : "Formación " + i;
                    var slot = CampaignHudArt.Button(name, roster, new Vector2(448 + i * 174, -top), new Vector2(164, 136), "", () => SelectRosterSlot(army, index));
                    rosterSlots[row, i] = slot;
                    rosterPortraits[row, i] = Picture("Retrato de formación", slot.transform, 7, 7, 150, 91, null);
                    var countStrip = CampaignMenuUI.Rect("Fondo de efectivos", slot.transform, new Vector2(0, 1), new Vector2(7, -98), new Vector2(150, 30));
                    var countFill = countStrip.gameObject.AddComponent<Image>(); countFill.color = new Color(.08f, .06f, .035f, .96f); countFill.raycastTarget = false;
                    rosterCounts[row, i] = Label("Soldados", slot.transform, 8, 96, 148, 36, "", 25, true);
                    rosterCounts[row, i].alignment = TextAnchor.MiddleCenter;
                    rosterBorders[row, i] = Selection(slot.transform);
                    slot.gameObject.AddComponent<CampaignRosterSlot>().Initialize(this, army, i, false);
                }
            }
            TransferButton = CampaignHudArt.Button("Trasladar formación", roster, new Vector2(1512, -20), new Vector2(345, 44), "DESTINAR A HUESTE", Transfer);
            MusterButton = CampaignHudArt.Button("Crear hueste", roster, new Vector2(1512, -76), new Vector2(345, 44), "CREAR HUESTE", Muster);
            EndTurnButton = CampaignHudArt.Button("Finalizar turno", roster, new Vector2(1512, -132), new Vector2(345, 44), "FINALIZAR TURNO", () => { campaign.CompleteTurn(); Refresh(); });
            heroTransfer = CampaignHudArt.Button("Trasladar héroe", roster, new Vector2(1512, -208), new Vector2(345, 44), "TRASLADAR HÉROE", TransferHero);
            rosterNotice = Label("Ayuda de guarnición", roster, 24, 318, 1810, 24, "", 17);
        }

        public void SelectFormation(int index) => SelectRosterSlot(false, index);
        public void SelectRosterSlot(bool army, int index)
        {
            if (lastDragFrame == Time.frameCount) return;
            var stacks = RowStacks(army);
            if (!selectedHero && selectedFormation != null && army != armySource)
            { MoveStack(armySource, selectedFormation, army, index); return; }
            selectedHero = false; armySource = army;
            selectedFormation = index >= 0 && index < stacks.Length ? stacks[index].Id : null;
            rosterMessage = ""; Refresh();
        }

        public void SelectRosterHero(bool army)
        {
            if (lastDragFrame == Time.frameCount) return;
            if (selectedHero && armySource != army) { TransferHero(); return; }
            var hero = campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry);
            bool present = army ? VisitingArmy() != null && hero.armyId == campaign.SelectedArmyId : hero.settlementId == cityId;
            selectedHero = present; selectedFormation = null; armySource = army; rosterMessage = ""; Refresh();
        }

        public bool DropRosterSlot(bool fromArmy, int fromIndex, bool toArmy, int toIndex)
        {
            var stacks = RowStacks(fromArmy);
            return fromIndex >= 0 && fromIndex < stacks.Length && MoveStack(fromArmy, stacks[fromIndex].Id, toArmy, toIndex);
        }

        private bool MoveStack(bool sourceArmy, string sourceId, bool targetArmy, int targetIndex)
        {
            if (!IsOpen || campaign.IsRivalPhaseActive || campaign.HasPendingBattle || campaign.IsCampaignFinished) return false;
            var source = RowStacks(sourceArmy).FirstOrDefault(s => s.Contains(sourceId));
            var target = RowStacks(targetArmy).ElementAtOrDefault(targetIndex);
            if (source == null) return false;
            bool moved;
            if (sourceArmy == targetArmy)
                moved = campaign.TryReorderCityStack(cityId, sourceArmy, source.Id, target?.Id);
            else if (target != null && !source.IsSameType(target.Representative))
            { rosterMessage = "Elige una casilla vacía o tropas del mismo tipo."; Refresh(); return false; }
            else moved = sourceArmy ? campaign.TryLeaveGarrison(cityId, source.Id) : campaign.TryAssignGarrison(cityId, source.Id);
            rosterMessage = moved ? "Tropas trasladadas. Los efectivos del mismo tipo se suman en su ficha." : "Traslado no disponible. Comprueba la cercanía y las plazas de defensa.";
            if (moved) { armySource = targetArmy; selectedFormation = null; selectedHero = false; }
            Refresh(); return moved;
        }

        private void TransferHero()
        {
            if (!selectedHero) return;
            bool moved = campaign.TryTransferCityHero(cityId, armySource);
            rosterMessage = moved ? "Héroe trasladado." : "Para salir, el héroe necesita tropas en guarnición o una hueste propia próxima.";
            if (moved) armySource = !armySource;
            Refresh();
        }

        private void RefreshRoster(bool allowed)
        {
            var guard = RowStacks(false); var field = RowStacks(true); var army = VisitingArmy();
            var selected = (armySource ? field : guard).FirstOrDefault(g => g.Contains(selectedFormation));
            if (!selectedHero && selected == null)
            { selectedFormation = null; }
            cityRosterTitle.text = "GUARNICIÓN\n" + guard.Length + (guard.Length==1 ? " tipo de tropa" : " tipos de tropa");
            bool inside=army!=null&&campaign.SettlementState.TryGet(cityId,out var home)&&army.Movement.Position==home.Position;
            visitingTitle.text = army == null ? "SIN HUESTE VISITANTE\nArrastra aquí tropas o el héroe." : (inside?"HUESTE VISITANTE\n":"HUESTE PRÓXIMA\n") + army.Army.Id + " · " + field.Length + (field.Length==1 ? " tipo" : " tipos");
            var hero = campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry);
            var catalogue = Resources.Load<TextAsset>("KingdomSelection/catalog");
            var heroName = catalogue == null ? "Héroe" : JsonUtility.FromJson<KingdomChoices>(catalogue.text).entries.FirstOrDefault(e => e.id == campaign.PlayerFactionId)?.hero ?? "Héroe";
            for (int row = 0; row < 2; row++)
            {
                var stacks = row == 0 ? guard : field;
                bool heroHere = row == 0 ? hero.settlementId == cityId : army != null && hero.armyId == army.Army.Id;
                heroPortraits[row].enabled = heroHere;
                heroNames[row].text = heroHere ? heroName : "Sin héroe";
                heroBorders[row].SetActive(heroHere && selectedHero && armySource == (row == 1));
                for (int i = 0; i < 6; i++)
                {
                    var stack = stacks.ElementAtOrDefault(i);
                    rosterPortraits[row, i].enabled = stack != null;
                    if (stack != null) rosterPortraits[row, i].sprite = StackPortrait(Profile(stack.Representative));
                    rosterCounts[row, i].text = stack == null ? "—" : stack.Quantity.ToString();
                    rosterBorders[row, i].SetActive(!selectedHero && stack?.Contains(selectedFormation) == true && armySource == (row == 1));
                }
            }
            bool troopSelected = allowed && !selectedHero && selectedFormation != null;
            TransferButton.GetComponentInChildren<Text>().text = armySource ? "DEJAR EN GUARNICIÓN" : "DESTINAR A HUESTE";
            TransferButton.interactable = troopSelected && (armySource
                ? campaign.Military.TryTransferStack(cityId, campaign.SelectedArmyId, selectedFormation, campaign.ArmyRegistry, true, out _, out _)
                : campaign.Military.TrySendToVisitor(cityId, campaign.SelectedArmyId, selectedFormation, false, campaign.ArmyRegistry, out _, out _, out _));
            MusterButton.interactable = allowed && !selectedHero && !armySource && selectedFormation != null &&
                campaign.Military.TryMusterStack(cityId, campaign.PlayerFactionId, selectedFormation, campaign.ArmyRegistry, out _, out _, out _);
            heroTransfer.GetComponentInChildren<Text>().text = armySource ? "ACUARTELAR HÉROE" : army == null ? "SALIR CON LA GUARNICIÓN" : "ASIGNAR HÉROE A HUESTE";
            heroTransfer.interactable = allowed && selectedHero && (armySource
                ? army != null && campaign.Military.TryTransferHero(cityId, army.Army.Id, campaign.ArmyRegistry, true, out _, out _)
                : campaign.Military.TrySendToVisitor(cityId, campaign.SelectedArmyId, null, true, campaign.ArmyRegistry, out _, out _, out _));
            EndTurnButton.interactable = allowed;
            rosterNotice.text = string.IsNullOrEmpty(rosterMessage) ? "Guarnición arriba · Hueste visitante abajo. Selecciona y pulsa el destino, o arrastra las tropas y el héroe." : rosterMessage;
        }

        internal bool BeginRosterDrag(bool army, int index, bool hero, PointerEventData data)
        {
            if (campaign.IsRivalPhaseActive || campaign.HasPendingBattle || campaign.IsCampaignFinished) return false;
            Sprite sprite = null;
            if (hero)
            {
                var position=campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
                bool present=army ? VisitingArmy()!=null && position.armyId==campaign.SelectedArmyId : position.settlementId==cityId;
                if(!present)return false;
                selectedHero=true;armySource=army;selectedFormation=null;
            }
            else
            {
                var stack = RowStacks(army).ElementAtOrDefault(index); if (stack == null) return false;
                dragId = stack.Id; sprite = StackPortrait(Profile(stack.Representative));
            }
            dragArmy = army; dragHero = hero; dragging = true;
            dragGhost = CampaignMenuUI.Rect("Ficha arrastrada", body, new Vector2(.5f, .5f), Vector2.zero, new Vector2(130, 100));
            dragGhost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            if (hero)
            {
                var image = dragGhost.gameObject.AddComponent<RawImage>(); image.texture = heroPortraits[army ? 1 : 0].texture;
                image.uvRect = heroPortraits[army ? 1 : 0].uvRect; image.raycastTarget = false;
            }
            else { var image = dragGhost.gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; }
            MoveRosterDrag(data); return true;
        }
        internal void MoveRosterDrag(PointerEventData data)
        {
            if (dragGhost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(body, data.position, data.pressEventCamera, out var point)) dragGhost.anchoredPosition = point;
        }
        internal void AcceptRosterDrop(bool army, int index, bool hero)
        {
            if (!dragging || hero != dragHero) return;
            if (hero)
            {
                if (army != dragArmy) { armySource = dragArmy; selectedHero = true; TransferHero(); }
            }
            else MoveStack(dragArmy, dragId, army, index);
            EndRosterDrag();
        }
        internal void EndRosterDrag()
        {
            if (dragging) lastDragFrame = Time.frameCount;
            dragging = false;
            if (dragGhost != null) { dragGhost.gameObject.SetActive(false); Destroy(dragGhost.gameObject); dragGhost = null; }
        }
    }

    public sealed class CampaignRosterSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private CampaignRecruitmentView owner;
        private bool army, hero;
        private int index;
        internal void Initialize(CampaignRecruitmentView view, bool armyRow, int slot, bool heroSlot)
        { owner = view; army = armyRow; index = slot; hero = heroSlot; }
        public void OnBeginDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) owner.BeginRosterDrag(army, index, hero, data); }
        public void OnDrag(PointerEventData data) => owner.MoveRosterDrag(data);
        public void OnEndDrag(PointerEventData data) => owner.EndRosterDrag();
        public void OnDrop(PointerEventData data) => owner.AcceptRosterDrop(army, index, hero);
    }
}



