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
        private KingdomPortraitGraphic armyHeroPortrait;
        private Text battalionLabel;
        private CampaignArmyState displayedArmy;
        private CampaignTroopStack[] displayedStacks = System.Array.Empty<CampaignTroopStack>();
        private int selectedFormation=-1;
        private bool cityMode;
        private float nextHudRefresh;
        private bool initialFocusDone;
        private int focusAfterFrame;
        private RectTransform campaignAlerts;
        private readonly Button[] alertButtons=new Button[3];
        private readonly System.Collections.Generic.Dictionary<string,Button> cityBanners=new();
        private readonly System.Collections.Generic.Dictionary<string,CampaignCityPresenceBadge> presenceBadges=new();
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
        // Distribución: barra fina arriba (reino y recursos), columna derecha (menú, minimapa, huestes, consejo, avisos, casilla y cierre de turno)
        // y abajo la hueste elegida. El mapa queda libre a la izquierda de la columna. Medidas sobre 1920 de ancho.
        internal const float ColumnWidth=320,ColumnMargin=10,TopBarHeight=58;
        private const float HuesteTrack=240;
        private void BuildCampaignHud(Transform canvas)
        {
            focusAfterFrame=Time.frameCount+3;
            // Width based scaling keeps the two bottom panels and all resource slots disjoint at 4:3 and ultrawide.
            hudCanvas.GetComponent<CanvasScaler>().matchWidthOrHeight=0;
            kingdomHeader=CampaignHudArt.KingdomBanner(canvas);
            SetKingdomHeaderCompact(true);

            resourceBar=CreateRect("Campaign resource bar",canvas,new Vector2(1326,TopBarHeight),new Vector2(-(ColumnWidth+ColumnMargin*2),-6));
            resourceBar.anchorMin=resourceBar.anchorMax=resourceBar.pivot=Vector2.one;
            CampaignHudArt.Surface(resourceBar);
            for(int i=0;i<5;i++)
            {
                CampaignHudArt.Resource(resourceBar,i,new Vector2(16+i*262,-9),40);
                resourceValues[i]=HudText("Stock "+i,resourceBar,new Vector2(64+i*262,-12),new Vector2(84,34),"—",25);
                resourceDeltas[i]=HudText("Balance "+i,resourceBar,new Vector2(142+i*262,-20),new Vector2(116,22),"—",14,false,Positive);
            }
            BuildColumn(canvas);

            panel=CreateRect("Map Information",canvas,new Vector2(ColumnWidth,304),new Vector2(-ColumnMargin,-564));
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one;
            CampaignHudArt.Surface(panel);
            HudText("Title",panel,new Vector2(18,-10),new Vector2(284,28),"ITINERARIO",19,true,CampaignMenuUI.Gold);
            details=HudText("Selection",panel,new Vector2(18,-48),new Vector2(284,46),"Selecciona un destino",16,false,CampaignMenuUI.Paper);
            pathPreview=HudText("Route Preview",panel,new Vector2(18,-104),new Vector2(284,44),"Clic derecho para ordenar la marcha.",14,false,new Color(.82f,.78f,.68f));
            armySummary=HudText("Army Summary",panel,new Vector2(18,-158),new Vector2(284,136),"",14,false,CampaignMenuUI.Paper);
            foreach(float y in new[]{42f,98f,152f})
            {
                var divider=CreateRect("Separador de itinerario",panel,new Vector2(284,1),new Vector2(18,-y));
                var line=divider.gameObject.AddComponent<Image>();line.color=new Color(.67f,.51f,.23f,.55f);line.raycastTarget=false;
            }
            diagnostics=HudText("Diagnostics",panel,Vector2.zero,Vector2.one,"",1);diagnostics.gameObject.SetActive(false);

            turnPanel=CreateRect("Turn Panel",canvas,new Vector2(1570,160),new Vector2(ColumnMargin,ColumnMargin));
            turnPanel.anchorMin=turnPanel.anchorMax=turnPanel.pivot=Vector2.zero;
            CampaignHudArt.Surface(turnPanel);
            var portraitRect=CreateRect("Retrato de hueste",turnPanel,new Vector2(108,140),new Vector2(12,-10));
            armyHeroPortrait=portraitRect.gameObject.AddComponent<KingdomPortraitGraphic>();armyHeroPortrait.raycastTarget=false;
            armyHeroPortrait.texture=Resources.Load<Texture2D>("KingdomSelection/hero-castilla");armyHeroPortrait.uvRect=new Rect(.33f,.12f,.32f,.84f);
            battalionLabel=HudText("Batallón sin héroe",turnPanel,new Vector2(12,-48),new Vector2(108,70),"BATALLÓN\nSIN HÉROE",15,true);
            huesteTitle=HudText("Hueste Title",turnPanel,new Vector2(132,-12),new Vector2(240,30),"Hueste de Castilla",20,true);
            huesteTotals=HudText("Efectivos",turnPanel,new Vector2(132,-48),new Vector2(240,26),"",16);
            huesteMovement=HudText("Movimiento",turnPanel,new Vector2(132,-78),new Vector2(240,26),"",16);
            movementTrack=CreateRect("Movement track",turnPanel,new Vector2(HuesteTrack,4),new Vector2(132,-108));
            movementTrack.gameObject.AddComponent<Image>().color=new Color(.27f,.22f,.13f);
            movementFill=CreateRect("Movement remaining",movementTrack,new Vector2(HuesteTrack,4),Vector2.zero);
            movementFill.gameObject.AddComponent<Image>().color=CampaignMenuUI.Gold;
            marchStatus=HudText("March Status",turnPanel,new Vector2(132,-120),new Vector2(240,30),"",13,false,new Color(.75f,.67f,.52f));
            for(int i=0;i<6;i++)
            {
                int slot=i;
                var card=CampaignHudArt.Button("Formación "+i,turnPanel,new Vector2(388+i*120,-10),new Vector2(112,140),"",()=>SelectFormation(slot));
                troopCards[i]=card;
                card.interactable=false;
                var art=CreateRect("Retrato",card.transform,new Vector2(96,84),new Vector2(8,-8));
                troopPortraits[i]=art.gameObject.AddComponent<KingdomPortraitGraphic>();troopPortraits[i].raycastTarget=false;
                troopNames[i]=HudText("Nombre",card.transform,new Vector2(6,-94),new Vector2(100,20),"Sin formación",11);
                troopNames[i].alignment=TextAnchor.MiddleCenter;
                troopCounts[i]=HudText("Efectivos",card.transform,new Vector2(6,-113),new Vector2(100,22),"—",17,false,CampaignMenuUI.Gold);
                troopCounts[i].alignment=TextAnchor.MiddleCenter;
                var rim=CampaignHudArt.Stretch("Selección",card.transform,4);rim.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
                troopSelection[i]=rim.gameObject;rim.gameObject.SetActive(false);
            }
            // órdenes en rejilla de 2 × 2, como el bloque de acciones de un héroe
            MoveArmyButton=CampaignHudArt.Button("Move Army",turnPanel,new Vector2(1114,-10),new Vector2(220,66),"MOVER",null);
            LocateArmyButton=CampaignHudArt.Button("Locate Army",turnPanel,new Vector2(1340,-10),new Vector2(220,66),"LOCALIZAR",null);
            NextArmyButton=CampaignHudArt.Button("Next Army",turnPanel,new Vector2(1114,-84),new Vector2(220,66),"OTRA HUESTE",null);
            CancelMarchButton=CampaignHudArt.Button("Cancel March",turnPanel,new Vector2(1340,-84),new Vector2(220,66),"CANCELAR",null);

            turnControls=CreateRect("Turn closing",canvas,new Vector2(ColumnWidth,190),new Vector2(-ColumnMargin,ColumnMargin));
            turnControls.anchorMin=turnControls.anchorMax=turnControls.pivot=new Vector2(1,0);CampaignHudArt.Surface(turnControls);
            turnSummary=HudText("Turn Summary",turnControls,new Vector2(18,-10),new Vector2(284,34),"Turno 1",22,true);
            orderNotice=HudText("Órdenes pendientes",turnControls,new Vector2(18,-46),new Vector2(284,24),"",14,false,CampaignMenuUI.Gold);
            LocateObjectiveButton=CampaignHudArt.Button("Locate Objective",turnControls,new Vector2(12,-74),new Vector2(296,32),"VER OBJETIVO",ToggleObjective);
            LocateObjectiveButton.GetComponentInChildren<Text>().fontSize=14;
            EndTurnButton=CampaignHudArt.Button("End Turn",turnControls,new Vector2(12,-114),new Vector2(296,66),"FINALIZAR TURNO",null);
            EndTurnButton.GetComponentInChildren<Text>().fontSize=21;
            EndTurnButton.transform.Find("Fondo").GetComponent<Image>().color=new Color(1f,.71f,.54f);
            missionSummary=HudText("Mission Summary",turnControls,Vector2.zero,Vector2.one,"",1);missionSummary.gameObject.SetActive(false);
            // Keep the established event API for save/load and diagnostics; these live exclusively in pause in a playable campaign.
            utilityPanel=CreateRect("Campaign tools",canvas,Vector2.one,Vector2.zero);
            SaveButton=Action("Save","GUARDAR",utilityPanel,Vector2.zero,Vector2.one);
            LoadButton=Action("Load","CARGAR",utilityPanel,Vector2.zero,Vector2.one);
            NewGameButton=Action("New Game","REINICIAR",utilityPanel,Vector2.zero,Vector2.one);
            BattleLabButton=Action("Battle Lab","LAB",utilityPanel,Vector2.zero,Vector2.one);
            saveStatus=HudText("Save status",utilityPanel,Vector2.zero,Vector2.one,"",1);utilityPanel.gameObject.SetActive(false);

            // consejo del reino: rejilla de 2 × 2 en la columna
            campaignNavigation=CreateRect("Campaign navigation",canvas,new Vector2(ColumnWidth,104),new Vector2(-ColumnMargin,-386));
            campaignNavigation.anchorMin=campaignNavigation.anchorMax=campaignNavigation.pivot=Vector2.one;
            CitiesButton=CampaignHudArt.Button("Ciudades",campaignNavigation,Vector2.zero,new Vector2(156,48),"CIUDADES",ToggleCities);
            CouncilButton=CampaignHudArt.Button("Diplomacia",campaignNavigation,new Vector2(164,0),new Vector2(156,48),"DIPLOMACIA",()=>GetComponent<CampaignDiplomacyView>()?.Open());
            CampaignHudArt.Button("Objetivos",campaignNavigation,new Vector2(0,-56),new Vector2(156,48),"OBJETIVOS",ToggleObjective);
            CampaignHudArt.Button("Investigación",campaignNavigation,new Vector2(164,-56),new Vector2(156,48),"CIENCIA",()=>{
                var view=GetComponent<CampaignResearchView>()??gameObject.AddComponent<CampaignResearchView>();view.Open();
            });
            foreach(var label in campaignNavigation.GetComponentsInChildren<Text>())label.fontSize=15;
            // avisos: obras en cola, huestes sin órdenes y propuestas recibidas, en fila bajo el consejo
            campaignAlerts=CreateRect("Avisos de campaña",canvas,new Vector2(ColumnWidth,62),new Vector2(-ColumnMargin,-496));
            campaignAlerts.anchorMin=campaignAlerts.anchorMax=campaignAlerts.pivot=Vector2.one;
            var actions=new UnityEngine.Events.UnityAction[]{ToggleCities,FocusUnorderedArmy,()=>GetComponent<CampaignDiplomacyView>()?.Open()};
            var symbols=new[]{FulgorIcon.Labor,FulgorIcon.Population,FulgorIcon.Gold};
            for(int i=0;i<3;i++)
            {
                var alert=CampaignHudArt.Button("Aviso "+i,campaignAlerts,new Vector2(i*108,0),new Vector2(104,62),"",actions[i]);
                FulgorIcons.Create("Símbolo",alert.transform,symbols[i],new Vector2(14,-16),30);
                var label=alert.GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(20,0);label.rectTransform.sizeDelta=new Vector2(44,30);label.fontSize=20;
                alertButtons[i]=alert;
            }
        }
        /// <summary>El estandarte del reino va compacto sobre el mapa y a su tamaño completo en la ciudad, que lo usa de cabecera.</summary>
        private void SetKingdomHeaderCompact(bool compact)
        {
            if(kingdomHeader==null)return;
            kingdomHeader.anchoredPosition=compact?new Vector2(4,-2):new Vector2(12,-8);
            kingdomHeader.sizeDelta=compact?new Vector2(236,84):new Vector2(490,175);
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
            UpdateCityBanners(campaign);RefreshColumn(campaign);
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
            campaignAlerts.gameObject.SetActive(!cityMode);
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
            foreach(var existing in presenceBadges.Values)if(existing!=null)existing.gameObject.SetActive(false);
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
                        ((RectTransform)banner.transform).anchoredPosition=local+new Vector2(0,campaign.CityPresence(city.Id).HasVisitor ? -105 : -45);
                }
                banner.gameObject.SetActive(show);
                banner.transform.Find("Fondo").GetComponent<Image>().color=city.OwnerFactionId==campaign.PlayerFactionId?Color.white:new Color(.8f,.68f,.55f);
                UpdatePresenceBadge(campaign,towns,city,view,camera);
            }
        }
        private void UpdatePresenceBadge(CampaignTurnController campaign,CampaignSettlementController towns,CampaignSettlement city,CampaignSettlementView view,Camera camera)
        {
            if(cityMode || !campaign.CanObserve(city.Position) || view==null || !view.gameObject.activeInHierarchy)return;
            var state=campaign.CityPresence(city.Id);
            if(!state.HasGarrison && !state.HasVisitor)return;
            if(!presenceBadges.TryGetValue(city.Id,out var badge) || badge==null)
            {
                var rect=CampaignMenuUI.Rect("Fuerzas sobre ciudad · "+city.Id,hudCanvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(262,60));
                badge=rect.gameObject.AddComponent<CampaignCityPresenceBadge>();badge.Build(towns,city.Position);
                rect.SetAsFirstSibling();presenceBadges[city.Id]=badge;
            }
            bool present=badge.SetPresence(state,city.OwnerFactionId,city.OwnerFactionId==campaign.PlayerFactionId);
            var screen=camera.WorldToScreenPoint(view.PresenceAnchor);
            bool show=present&&screen.z>0&&camera.pixelRect.Contains(screen);
            if(show&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudCanvas.transform,screen,null,out var local))
                badge.Rect.anchoredPosition=local+new Vector2(0,35);
            badge.gameObject.SetActive(show);
        }
        private bool CampaignExtrasContain(Vector2 point)=>Contains(campaignAlerts,point)||cityBanners.Values.Any(b=>Contains((RectTransform)b.transform,point))||presenceBadges.Values.Any(b=>b!=null&&Contains(b.Rect,point));

        private void RefreshCampaignArmy(ArmyMovementState movement)
        {
            var campaign=GetComponent<CampaignTurnController>();CampaignArmyOnMap active=null;
            campaign?.ArmyRegistry?.TryGetArmy(campaign.SelectedArmyId,out active);
            var commander=campaign?.Military?.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
            bool hasHero=active!=null && commander?.armyId==active.Army.Id;
            armyHeroPortrait.enabled=hasHero;battalionLabel.gameObject.SetActive(active!=null&&!hasHero);
            if(hasHero)armyHeroPortrait.texture=Resources.Load<Texture2D>("KingdomSelection/hero-"+campaign.PlayerFactionId);
            huesteTitle.text=active?.Army.Id??(movement==null?"Sin hueste":"Hueste de Castilla");
            huesteMovement.text=movement==null?"Sin movimiento":$"Movimiento  {movement.RemainingMovement} / {movement.MovementPerTurn}";
            long quantity=0,weightedMorale=0;
            if(active!=null)foreach(var formation in active.Army.Formations){quantity+=formation.Quantity;weightedMorale+=(long)formation.Morale*formation.Quantity;}
            huesteTotals.text=quantity>0?$"{quantity} efectivos · moral {weightedMorale/quantity}":"Sin tropas disponibles";
            movementFill.sizeDelta=new Vector2(HuesteTrack*(movement==null||movement.MovementPerTurn<=0?0:Mathf.Clamp01((float)movement.RemainingMovement/movement.MovementPerTurn)),4);
            if(ReferenceEquals(displayedArmy,active?.Army))return;
            string previousId=displayedArmy?.Id;displayedArmy=active?.Army;displayedStacks=CampaignTroopStack.Group(displayedArmy?.Formations).ToArray();
            if(previousId!=displayedArmy?.Id)selectedFormation=-1;
            for(int i=0;i<6;i++)
            {
                var stack=i<displayedStacks.Length?displayedStacks[i]:null;var formation=stack?.Representative;
                troopCards[i].interactable=formation!=null;
                troopNames[i].text=formation==null?"Sin formación":TroopName(formation);
                troopCounts[i].text=stack==null?"—":stack.Quantity.ToString();
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
                if(displayedArmy!=null&&selectedFormation<displayedStacks.Length)SelectFormation(selectedFormation);
                else CloseDrawer();
            }
            else if(formationDetails!=null)CloseDrawer();
        }
        private static string TroopName(CampaignFormationState f)=>f.Kind==FormationKind.Cavalry?"Jinetes ligeros":f.Kind==FormationKind.Ranged?"Ballesteros":f.Attributes.Defense>=7?"Hombres de armas":"Lanceros";
        public void SelectFormation(int index)
        {
            if(displayedArmy==null||index<0||index>=displayedStacks.Length)return;
            var stack=displayedStacks[index];var f=stack.Representative;selectedFormation=index;
            for(int i=0;i<6;i++)troopSelection[i].SetActive(i==index);
            OpenDrawer(TroopName(f));
            var a=f.Attributes;
            formationDetails=HudText("Atributos",campaignDrawer,new Vector2(28,-65),new Vector2(300,215),$"Efectivos      {stack.Quantity}\nMoral           {stack.Morale} / 100\nAtaque          {a.Attack}\nDefensa        {a.Defense}\nDaño             {a.MinimumDamage}–{a.MaximumDamage}\nAlcance        {a.Range}",19,false,LegacyUI.InkText);
            formationDetails.gameObject.SetActive(false);
            string[] labels={"Efectivos","Moral","Ataque","Defensa","Daño","Alcance"};
            string[] values={stack.Quantity.ToString(),stack.Morale+" / 100",a.Attack.ToString(),a.Defense.ToString(),a.MinimumDamage+"–"+a.MaximumDamage,a.Range.ToString()};
            for(int i=0;i<labels.Length;i++)
            {
                float y=-90-i*31;
                HudText(labels[i],campaignDrawer,new Vector2(40,y),new Vector2(170,28),labels[i],20,false,LegacyUI.InkText);
                var value=HudText("Valor "+i,campaignDrawer,new Vector2(210,y),new Vector2(105,28),values[i],20,false,LegacyUI.InkText);value.alignment=TextAnchor.UpperRight;
                var rule=CreateRect("Separador",campaignDrawer,new Vector2(275,1),new Vector2(40,y-29));var line=rule.gameObject.AddComponent<Image>();line.color=new Color(.24f,.13f,.05f,.25f);line.raycastTarget=false;
            }
            var joinTargets=GetComponent<CampaignTurnController>().ArmyJoinTargets();
            if(joinTargets.Length>0)
                CampaignHudArt.Button("Unir huestes",campaignDrawer,new Vector2(28,-300),new Vector2(150,40),"UNIR HUESTES",OpenArmyJoining);
        }
        private void OpenArmyJoining()
        {
            var campaign=GetComponent<CampaignTurnController>();
            var targets=campaign.ArmyJoinTargets();
            OpenDrawer("UNIR HUESTES");
            for(int i=0;i<targets.Length;i++)
            {
                if(!campaign.ArmyRegistry.TryGetArmy(targets[i],out var target))continue;
                var position=target.Movement.Position;
                CampaignHudArt.Button("Unir "+target.Army.Id,campaignDrawer,new Vector2(28,-70-i*36),new Vector2(304,32),
                    target.Army.Id+" · "+target.Army.Formations.Sum(f=>(long)f.Quantity)+" tropas",()=>{
                        if(campaign.TryJoinSelectedArmyAt(position))CloseDrawer();
                    });
            }
        }

        private void OpenDrawer(string title)
        {
            CloseDrawer();
            campaignDrawer=CreateRect("Campaign details",hudCanvas.transform,new Vector2(360,370),new Vector2(400,-92));
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

