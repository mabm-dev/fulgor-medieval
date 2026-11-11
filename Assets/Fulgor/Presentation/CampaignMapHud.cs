using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignSelectionPanel
    {
        private RectTransform kingdomHeader,campaignNavigation,campaignDrawer;
        private Text huesteTitle,huesteMovement,huesteTotals,formationDetails,orderNotice;
        internal RectTransform KingdomHeader => kingdomHeader;
        private readonly Button[] troopCards=new Button[6];
        private readonly Text[] troopNames=new Text[6],troopCounts=new Text[6];
        private readonly RawImage[] troopPortraits=new RawImage[6];
        private readonly GameObject[] troopSelection=new GameObject[6];
        private CampaignArmyState displayedArmy;
        private int selectedFormation=-1;
        private bool cityMode;
        private float nextHudRefresh;
        private bool initialFocusDone;
        private int focusAfterFrame;
        private RectTransform campaignAlerts;
        private readonly Button[] alertButtons=new Button[3];
        private readonly System.Collections.Generic.Dictionary<string,Button> cityBanners=new();
        public bool HasCampaignHud=>kingdomHeader!=null;
        public bool HasCampaignDrawer=>campaignDrawer!=null&&campaignDrawer.gameObject.activeInHierarchy;
        public Button CitiesButton{get;private set;}
        public Button CouncilButton{get;private set;}
        public string FormationDetails=>formationDetails?.text;

        private Text HudText(string name,Transform parent,Vector2 position,Vector2 size,string value,int font=18,bool heading=false,Color? color=null)
        {
            var text=CreateText(name,parent,position,size,value,font,color??CampaignMenuUI.Paper);
            if(heading)text.font=LegacyUI.Heading??text.font;
            return text;
        }
        private void BuildCampaignHud(Transform canvas)
        {
            focusAfterFrame=Time.frameCount+3;
            // Width based scaling keeps the two bottom panels and all resource slots disjoint at 4:3 and ultrawide.
            hudCanvas.GetComponent<CanvasScaler>().matchWidthOrHeight=0;
            kingdomHeader=CampaignHudArt.KingdomBanner(canvas);

            resourceBar=CreateRect("Campaign resource bar",canvas,new Vector2(1030,86),new Vector2(-225,-12));
            resourceBar.anchorMin=resourceBar.anchorMax=resourceBar.pivot=Vector2.one;
            CampaignHudArt.Surface(resourceBar);
            for(int i=0;i<5;i++)
            {
                CampaignHudArt.Resource(resourceBar,i,new Vector2(25+i*198,-16),45);
                resourceValues[i]=HudText("Stock "+i,resourceBar,new Vector2(83+i*198,-15),new Vector2(106,30),"—",25);
                resourceDeltas[i]=HudText("Balance "+i,resourceBar,new Vector2(83+i*198,-50),new Vector2(110,22),"—",14,false,Positive);
            }
            panel=CreateRect("Map Information",canvas,new Vector2(340,370),new Vector2(-20,-155));
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;
            CampaignHudArt.Surface(panel,true);
            HudText("Title",panel,new Vector2(38,-35),new Vector2(264,44),"ITINERARIO",22,true,LegacyUI.InkText);
            details=HudText("Selection",panel,new Vector2(38,-95),new Vector2(264,58),"Selecciona un destino",18,false,LegacyUI.InkText);
            pathPreview=HudText("Route Preview",panel,new Vector2(38,-165),new Vector2(264,58),"Clic derecho para ordenar la marcha.",17,false,LegacyUI.InkText);
            armySummary=HudText("Army Summary",panel,new Vector2(38,-237),new Vector2(264,105),"",16,false,LegacyUI.InkText);
            diagnostics=HudText("Diagnostics",panel,Vector2.zero,Vector2.one,"",1);diagnostics.gameObject.SetActive(false);

            turnPanel=CreateRect("Turn Panel",canvas,new Vector2(1548,218),new Vector2(16,16));
            turnPanel.anchorMin=turnPanel.anchorMax=turnPanel.pivot=Vector2.zero;
            CampaignHudArt.Surface(turnPanel);
            var portraitRect=CreateRect("Retrato de hueste",turnPanel,new Vector2(145,182),new Vector2(20,-18));
            var portrait=portraitRect.gameObject.AddComponent<KingdomPortraitGraphic>();portrait.raycastTarget=false;
            portrait.texture=Resources.Load<Texture2D>("KingdomSelection/hero-castilla");portrait.uvRect=new Rect(.33f,.12f,.32f,.84f);
            huesteTitle=HudText("Hueste Title",turnPanel,new Vector2(180,-24),new Vector2(247,35),"Hueste de Castilla",21,true);
            huesteTotals=HudText("Efectivos",turnPanel,new Vector2(180,-70),new Vector2(247,35),"",17);
            huesteMovement=HudText("Movimiento",turnPanel,new Vector2(180,-113),new Vector2(247,27),"",17);
            movementTrack=CreateRect("Movement track",turnPanel,new Vector2(247,4),new Vector2(180,-148));
            movementTrack.gameObject.AddComponent<Image>().color=new Color(.27f,.22f,.13f);
            movementFill=CreateRect("Movement remaining",movementTrack,new Vector2(247,4),Vector2.zero);
            movementFill.gameObject.AddComponent<Image>().color=CampaignMenuUI.Gold;
            marchStatus=HudText("March Status",turnPanel,new Vector2(180,-169),new Vector2(247,34),"",13,false,new Color(.75f,.67f,.52f));
            for(int i=0;i<6;i++)
            {
                int slot=i;
                var card=CampaignHudArt.Button("Formación "+i,turnPanel,new Vector2(448+i*145,-19),new Vector2(134,180),"",()=>SelectFormation(slot));
                troopCards[i]=card;
                card.interactable=false;
                var art=CreateRect("Retrato",card.transform,new Vector2(116,118),new Vector2(9,-9));
                troopPortraits[i]=art.gameObject.AddComponent<KingdomPortraitGraphic>();troopPortraits[i].raycastTarget=false;
                troopNames[i]=HudText("Nombre",card.transform,new Vector2(10,-126),new Vector2(114,28),"Sin formación",12);
                troopNames[i].alignment=TextAnchor.MiddleCenter;
                troopCounts[i]=HudText("Efectivos",card.transform,new Vector2(10,-153),new Vector2(114,24),"—",19,false,CampaignMenuUI.Gold);
                troopCounts[i].alignment=TextAnchor.MiddleCenter;
                var rim=CampaignHudArt.Stretch("Selección",card.transform,4);rim.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
                troopSelection[i]=rim.gameObject;rim.gameObject.SetActive(false);
            }
            MoveArmyButton=CampaignHudArt.Button("Move Army",turnPanel,new Vector2(1330,-19),new Vector2(198,48),"MOVER",null);
            MoveArmyButton.GetComponentInChildren<Text>().fontSize=16;
            LocateArmyButton=CampaignHudArt.Button("Locate Army",turnPanel,new Vector2(1330,-75),new Vector2(198,36),"LOCALIZAR",null);
            NextArmyButton=CampaignHudArt.Button("Next Army",turnPanel,new Vector2(1330,-119),new Vector2(198,36),"OTRA HUESTE",null);
            CancelMarchButton=CampaignHudArt.Button("Cancel March",turnPanel,new Vector2(1330,-163),new Vector2(198,36),"CANCELAR",null);

            turnControls=CreateRect("Turn closing",canvas,new Vector2(320,218),new Vector2(-16,16));
            turnControls.anchorMin=turnControls.anchorMax=turnControls.pivot=new Vector2(1,0);CampaignHudArt.Surface(turnControls);
            turnSummary=HudText("Turn Summary",turnControls,new Vector2(24,-16),new Vector2(260,42),"Turno 1",23,true);
            orderNotice=HudText("Órdenes pendientes",turnControls,new Vector2(24,-62),new Vector2(272,26),"",15,false,CampaignMenuUI.Gold);
            LocateObjectiveButton=CampaignHudArt.Button("Locate Objective",turnControls,new Vector2(24,-90),new Vector2(272,34),"VER OBJETIVO",ToggleObjective);
            EndTurnButton=CampaignHudArt.Button("End Turn",turnControls,new Vector2(24,-138),new Vector2(272,60),"FINALIZAR TURNO",null);
            EndTurnButton.GetComponentInChildren<Text>().fontSize=20;
            EndTurnButton.transform.Find("Fondo").GetComponent<Image>().color=new Color(1f,.71f,.54f);
            missionSummary=HudText("Mission Summary",turnControls,Vector2.zero,Vector2.one,"",1);missionSummary.gameObject.SetActive(false);
            // Keep the established event API for save/load and diagnostics; these live exclusively in pause in a playable campaign.
            utilityPanel=CreateRect("Campaign tools",canvas,Vector2.one,Vector2.zero);
            SaveButton=Action("Save","GUARDAR",utilityPanel,Vector2.zero,Vector2.one);
            LoadButton=Action("Load","CARGAR",utilityPanel,Vector2.zero,Vector2.one);
            NewGameButton=Action("New Game","REINICIAR",utilityPanel,Vector2.zero,Vector2.one);
            BattleLabButton=Action("Battle Lab","LAB",utilityPanel,Vector2.zero,Vector2.one);
            saveStatus=HudText("Save status",utilityPanel,Vector2.zero,Vector2.one,"",1);utilityPanel.gameObject.SetActive(false);

            campaignNavigation=CreateRect("Campaign navigation",canvas,new Vector2(600,48),new Vector2(20,250));
            campaignNavigation.anchorMin=campaignNavigation.anchorMax=campaignNavigation.pivot=Vector2.zero;
            CitiesButton=CampaignHudArt.Button("Ciudades",campaignNavigation,Vector2.zero,new Vector2(186,48),"CIUDADES",ToggleCities);
            CouncilButton=CampaignHudArt.Button("Diplomacia",campaignNavigation,new Vector2(200,0),new Vector2(186,48),"DIPLOMACIA",()=>GetComponent<CampaignDiplomacyView>()?.Open());
            CampaignHudArt.Button("Objetivos",campaignNavigation,new Vector2(400,0),new Vector2(186,48),"OBJETIVOS",ToggleObjective);
            campaignAlerts=CreateRect("Avisos de campaña",canvas,new Vector2(78,258),new Vector2(20,-195));
            var actions=new UnityEngine.Events.UnityAction[]{ToggleCities,FocusUnorderedArmy,()=>GetComponent<CampaignDiplomacyView>()?.Open()};
            var symbols=new[]{FulgorIcon.Labor,FulgorIcon.Population,FulgorIcon.Gold};
            for(int i=0;i<3;i++)
            {
                var alert=CampaignHudArt.Button("Aviso "+i,campaignAlerts,new Vector2(0,-i*86),new Vector2(76,76),"",actions[i]);
                FulgorIcons.Create("Símbolo",alert.transform,symbols[i],new Vector2(22,-12),30);
                var label=alert.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(0,-18);label.rectTransform.sizeDelta=new Vector2(62,26);label.fontSize=17;
                alertButtons[i]=alert;
            }
        }

        private void LateUpdate()
        {
            if(!HasCampaignHud)return;
            var campaign=GetComponent<CampaignTurnController>();if(campaign?.State==null)return;
            if(!initialFocusDone&&Time.frameCount>=focusAfterFrame&&GetComponent<CampaignSettlementController>()?.Ready==true&&campaign.Army!=null)
            {
                var tile=GetComponentsInChildren<HexTileView>().FirstOrDefault(t=>t.Cell.Coordinates==campaign.Army.Position);
                if(tile!=null){Camera.main?.GetComponent<CampaignCameraController>()?.FocusOnWorld(tile.transform.position);initialFocusDone=true;}
            }
            if(campaignDrawer!=null)campaignDrawer.localScale=Vector3.one*Mathf.Min(1,Mathf.Max(.5f,(((RectTransform)hudCanvas.transform).rect.height-514)/370));
            UpdateCityBanners(campaign);
            // Update selection and drawers without rebuilding UI or loading portraits every frame.
            if(Time.unscaledTime<nextHudRefresh)return;nextHudRefresh=Time.unscaledTime+.15f;
            int pending=0;
            if(campaign.ArmyRegistry!=null)foreach(var army in campaign.ArmyRegistry.Armies)
                if(army.Army.FactionId==campaign.PlayerFactionId&&army.Movement.RemainingMovement>0&&!campaign.MarchFor(army.Army.Id).HasValue)pending++;
            orderNotice.text=pending==0?"Huestes con órdenes":pending+" huestes sin órdenes";
            CouncilButton.interactable=campaign.CanManageDiplomacy;
            int proposals=campaign.Diplomacy?.Proposals.Count(p=>p.receiver==campaign.PlayerFactionId&&p.response==0)??0;
            CouncilButton.GetComponentInChildren<Text>().text="DIPLOMACIA"+(proposals>0?" · "+proposals:"");
            CitiesButton.interactable=GetComponent<CampaignSettlementController>()?.Ready==true;
            int works=0;
            if(campaign.SettlementState!=null)foreach(var city in campaign.SettlementState.Settlements)
                if(city.OwnerFactionId==campaign.PlayerFactionId)works+=campaign.TileCity(city.Id)?.Pending.Length??0;
            int[] counts={works,pending,proposals};
            for(int i=0;i<3;i++){alertButtons[i].GetComponentInChildren<Text>().text=counts[i].ToString();alertButtons[i].interactable=counts[i]>0;}
            RefreshAlertsVisibility();
            RefreshCampaignArmy(campaign.Army);
        }

        private void RefreshAlertsVisibility()
        {
            if(campaignAlerts==null)return;
            var selection=GetComponent<CampaignBoardSelection>();var campaign=GetComponent<CampaignTurnController>();
            bool settlementSelected=selection?.SelectedCell!=null&&campaign?.SettlementState?.TryGet(selection.SelectedCell.Value.Coordinates,out _)==true;
            campaignAlerts.gameObject.SetActive(!cityMode&&campaignDrawer==null&&!settlementSelected);
        }

        private void FocusUnorderedArmy()
        {
            var campaign=GetComponent<CampaignTurnController>();if(campaign?.ArmyRegistry==null)return;
            var idle=campaign.ArmyRegistry.Armies.Where(a=>a.Army.FactionId==campaign.PlayerFactionId&&a.Movement.RemainingMovement>0&&!campaign.MarchFor(a.Army.Id).HasValue).ToArray();
            if(idle.Length==0)return;
            int index=System.Array.FindIndex(idle,a=>a.Army.Id==campaign.SelectedArmyId);
            GetComponent<CampaignBoardSelection>()?.LocateCell(idle[(index+1)%idle.Length].Movement.Position);
        }
        private void UpdateCityBanners(CampaignTurnController campaign)
        {
            var towns=GetComponent<CampaignSettlementController>();var camera=Camera.main;
            foreach(var existing in cityBanners.Values)if(existing!=null)existing.gameObject.SetActive(false);
            if(towns?.Ready!=true||camera==null)return;
            foreach(var city in towns.Registry.Settlements)
            {
                if(!cityBanners.TryGetValue(city.Id,out var banner))
                {
                    var position=city.Position;
                    banner=CampaignHudArt.Button("Ciudad en mapa · "+city.Id,hudCanvas.transform,Vector2.zero,new Vector2(210,38),city.Name,()=>GetComponent<CampaignBoardSelection>()?.LocateCell(position));
                    banner.GetComponentInChildren<Text>().fontSize=14;
                    var rect=(RectTransform)banner.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                    // Map labels stay beneath the anchored interface and never intercept its buttons.
                    rect.SetAsFirstSibling();cityBanners.Add(city.Id,banner);
                }
                towns.Views.TryGetValue(city.Position,out var view);
                bool show=!cityMode&&campaign.CanObserve(city.Position)&&view!=null&&view.Tile!=null&&view.Tile.gameObject.activeInHierarchy;
                if(show)
                {
                    var screen=camera.WorldToScreenPoint(view.Tile.transform.position);
                    show=screen.z>0&&camera.pixelRect.Contains(screen);
                    if(show&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudCanvas.transform,screen,null,out var local))
                        ((RectTransform)banner.transform).anchoredPosition=local+new Vector2(0,-45);
                }
                banner.gameObject.SetActive(show);
                banner.transform.Find("Fondo").GetComponent<Image>().color=city.OwnerFactionId==campaign.PlayerFactionId?Color.white:new Color(.8f,.68f,.55f);
            }
        }
        private bool CampaignExtrasContain(Vector2 point)=>Contains(campaignAlerts,point)||cityBanners.Values.Any(b=>Contains((RectTransform)b.transform,point));

        private void RefreshCampaignArmy(ArmyMovementState movement)
        {
            var campaign=GetComponent<CampaignTurnController>();CampaignArmyOnMap active=null;
            campaign?.ArmyRegistry?.TryGetArmy(campaign.SelectedArmyId,out active);
            huesteTitle.text=active?.Army.Id??(movement==null?"Sin hueste":"Hueste de Castilla");
            huesteMovement.text=movement==null?"Sin movimiento":$"Movimiento  {movement.RemainingMovement} / {movement.MovementPerTurn}";
            long quantity=0,weightedMorale=0;
            if(active!=null)foreach(var formation in active.Army.Formations){quantity+=formation.Quantity;weightedMorale+=(long)formation.Morale*formation.Quantity;}
            huesteTotals.text=quantity>0?$"{quantity} efectivos · moral {weightedMorale/quantity}":"Sin tropas disponibles";
            movementFill.sizeDelta=new Vector2(247*(movement==null||movement.MovementPerTurn<=0?0:Mathf.Clamp01((float)movement.RemainingMovement/movement.MovementPerTurn)),4);
            if(ReferenceEquals(displayedArmy,active?.Army))return;
            string previousId=displayedArmy?.Id;displayedArmy=active?.Army;
            if(previousId!=displayedArmy?.Id)selectedFormation=-1;
            for(int i=0;i<6;i++)
            {
                var formation=displayedArmy!=null&&i<displayedArmy.Formations.Count?displayedArmy.Formations[i]:null;
                troopCards[i].interactable=formation!=null;
                troopNames[i].text=formation==null?"Sin formación":TroopName(formation);
                troopCounts[i].text=formation==null?"—":formation.Quantity.ToString();
                troopPortraits[i].enabled=formation!=null;
                if(formation!=null)
                {
                    string art=formation.Kind==FormationKind.Cavalry?"cavalry":formation.Kind==FormationKind.Ranged?"crossbowmen":formation.Attributes.Defense>=7?"men-at-arms":"spearmen";
                    troopPortraits[i].texture=Resources.Load<Texture2D>("CampaignHud/"+art);
                    troopPortraits[i].uvRect=formation.Kind==FormationKind.Cavalry?new Rect(.12f,.15f,.76f,.75f):new Rect(.23f,.47f,.53f,.46f);
                }
                troopSelection[i].SetActive(formation!=null&&i==selectedFormation);
            }
            if(selectedFormation>=0)
            {
                if(displayedArmy!=null&&selectedFormation<displayedArmy.Formations.Count)SelectFormation(selectedFormation);
                else CloseDrawer();
            }
            else if(formationDetails!=null)CloseDrawer();
        }
        private static string TroopName(CampaignFormationState f)=>f.Kind==FormationKind.Cavalry?"Jinetes ligeros":f.Kind==FormationKind.Ranged?"Ballesteros":f.Attributes.Defense>=7?"Hombres de armas":"Lanceros";
        public void SelectFormation(int index)
        {
            if(displayedArmy==null||index<0||index>=displayedArmy.Formations.Count)return;
            var f=displayedArmy.Formations[index];selectedFormation=index;
            for(int i=0;i<6;i++)troopSelection[i].SetActive(i==index);
            OpenDrawer(TroopName(f));
            var a=f.Attributes;
            formationDetails=HudText("Atributos",campaignDrawer,new Vector2(28,-65),new Vector2(300,215),$"Efectivos      {f.Quantity}\nMoral           {f.Morale} / 100\nAtaque          {a.Attack}\nDefensa        {a.Defense}\nDaño             {a.MinimumDamage}–{a.MaximumDamage}\nAlcance        {a.Range}",19,false,LegacyUI.InkText);
            formationDetails.gameObject.SetActive(false);
            string[] labels={"Efectivos","Moral","Ataque","Defensa","Daño","Alcance"};
            string[] values={f.Quantity.ToString(),f.Morale+" / 100",a.Attack.ToString(),a.Defense.ToString(),a.MinimumDamage+"–"+a.MaximumDamage,a.Range.ToString()};
            for(int i=0;i<labels.Length;i++)
            {
                float y=-90-i*31;
                HudText(labels[i],campaignDrawer,new Vector2(40,y),new Vector2(170,28),labels[i],20,false,LegacyUI.InkText);
                var value=HudText("Valor "+i,campaignDrawer,new Vector2(210,y),new Vector2(105,28),values[i],20,false,LegacyUI.InkText);value.alignment=TextAnchor.UpperRight;
                var rule=CreateRect("Separador",campaignDrawer,new Vector2(275,1),new Vector2(40,y-29));var line=rule.gameObject.AddComponent<Image>();line.color=new Color(.24f,.13f,.05f,.25f);line.raycastTarget=false;
            }
        }
        private void OpenDrawer(string title)
        {
            CloseDrawer();
            campaignDrawer=CreateRect("Campaign details",hudCanvas.transform,new Vector2(360,370),new Vector2(20,-200));
            CampaignHudArt.Surface(campaignDrawer,true);
            RefreshAlertsVisibility();
            HudText("Title",campaignDrawer,new Vector2(40,-34),new Vector2(275,46),title,20,true,LegacyUI.InkText);
            CampaignHudArt.Button("Cerrar detalle",campaignDrawer,new Vector2(190,-300),new Vector2(125,40),"VOLVER",CloseDrawer).GetComponentInChildren<Text>().fontSize=14;
        }
        private void CloseDrawer(){if(campaignDrawer!=null){campaignDrawer.gameObject.SetActive(false);Destroy(campaignDrawer.gameObject);}campaignDrawer=null;formationDetails=null;}
        private void ToggleObjective()
        {
            if(campaignDrawer!=null&&campaignDrawer.name=="Objetivo de campaña"){CloseDrawer();return;}
            selectedFormation=-1;foreach(var rim in troopSelection)rim.SetActive(false);
            OpenDrawer("Objetivo de campaña");campaignDrawer.name="Objetivo de campaña";
            HudText("Objetivo",campaignDrawer,new Vector2(40,-95),new Vector2(275,185),missionSummary.text,20,false,LegacyUI.InkText);
        }
        private void ToggleCities()
        {
            if(campaignDrawer!=null&&campaignDrawer.name=="Ciudades del reino"){CloseDrawer();return;}
            selectedFormation=-1;foreach(var rim in troopSelection)rim.SetActive(false);
            OpenDrawer("Ciudades del reino");campaignDrawer.name="Ciudades del reino";
            var towns=GetComponent<CampaignSettlementController>();var campaign=GetComponent<CampaignTurnController>();
            if(towns?.Registry==null)return;
            var owned=towns.Registry.Settlements.Where(s=>s.OwnerFactionId==campaign.PlayerFactionId).ToArray();
            // Scrollable rather than truncating the list when the kingdom expands.
            var viewport=CreateRect("Lista de ciudades",campaignDrawer,new Vector2(284,195),new Vector2(38,-90));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            var content=CreateRect("Ciudades",viewport,new Vector2(284,Mathf.Max(195,owned.Length*54)),Vector2.zero);scroll.content=content;
            for(int i=0;i<owned.Length;i++)
            {
                var city=owned[i];
                CampaignHudArt.Button(city.Id,content,new Vector2(0,-i*54),new Vector2(280,46),city.Name,()=>{CloseDrawer();if(!towns.OpenCityMapAt(city.Position))towns.OpenCityAt(city.Position);}).GetComponentInChildren<Text>().fontSize=14;
            }
            if(owned.Length==0)HudText("Sin ciudades",content,Vector2.zero,new Vector2(290,90),"Aún no controlas ninguna ciudad.",18,false,LegacyUI.InkText);
        }
    }
}
