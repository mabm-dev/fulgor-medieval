using System;
using System.Collections.Generic;
using System.Text;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public enum CityPanelKind { Overview, Buildings, Treasury, Armies, Orders }

    /// <summary>Modal city presentation over live campaign data; no independent economy or save state.</summary>
    public sealed partial class CityManagementView : MonoBehaviour
    {
        private const int PreviewLayer=30;
        private CampaignSettlementController owner;
        private CampaignSettlement settlement;
        private CampaignSettlementDefinition definition;
        private CampaignTurnController campaign;
        private CampaignSelectionPanel campaignPanel;
        private bool canManage;
        private Camera mainCamera,previewCamera;
        private int previousMask;
        private CameraClearFlags previousClear;
        private Color previousBackground;
        private Canvas[] oldHud;
        private bool[] oldHudEnabled;
        private GameObject stage,ui;
        private RenderTexture target;
        private RawImage viewport;
        private Text heading,body,realmSummary,citySubtitle;
        private readonly Text[] realmStock=new Text[5];
        private Button primary,secondary;
        private ScrollRect scroll;
        private RectTransform viewControls;
        private readonly List<Button> tabs=new();
        private readonly List<CityBuildingView> buildings=new();
        private Vector3 focus;
        private readonly List<Vector3> framingPoints=new();
        private Bounds townBounds;
        public float EffectiveDistance=>previewCamera!=null?Vector3.Distance(previewCamera.transform.position,focus):0;
        public const float MinimumDistance=8.5f,MaximumDistance=14.5f,DefaultDistance=10.5f;
        private float yaw=28,zoom=DefaultDistance;
        public float ZoomDistance=>zoom;
        public CityAmbientLife Life=>stage!=null?stage.GetComponent<CityAmbientLife>():null;
        private CityBuildingView selectedBuilding;
        private LineRenderer outline;
        private Material outlineMaterial;
        private CityDevelopment observedDevelopment;
        private int observedConstructionTurn;
        private int projectIndex;
        private int observedCityRevision;
        private CampaignTurnState observedTurn;
        private ArmyMovementState observedArmy;
        private CampaignArmyRegistry observedRegistry;
        private Fulgor.Core.Map.HexCoordinates? observedMarch;
        public bool IsOpen { get; private set; }
        public bool ClosedThisFrame=>Time.frameCount==closedFrame;
        private int closedFrame=-1;
        public CityPanelKind Panel { get; private set; }
        public Camera PreviewCamera=>previewCamera;
        public RawImage Viewport=>viewport;
        public string BodyText=>body!=null?body.text:"";
        public Button PrimaryButton=>primary;
        public Button SecondaryButton=>secondary;
        public int BuildingCount=>buildings.Count;

        public bool Open(CampaignSettlementController controller,CampaignSettlement state,CampaignSettlementDefinition data,bool owned)
        {
            if(data==null||data.CityPrefab==null||Camera.main==null||GetComponent<CampaignTurnController>()?.CanObserve(state.Position)==false)return false;
            if(IsOpen&&settlement.Id==state.Id)return true;
            Close();owner=controller;settlement=state;definition=data;canManage=owned;
            campaign=GetComponent<CampaignTurnController>();campaignPanel=GetComponent<CampaignSelectionPanel>();mainCamera=Camera.main;
            previousMask=mainCamera.cullingMask;previousClear=mainCamera.clearFlags;previousBackground=mainCamera.backgroundColor;
            oldHud=GetComponentsInChildren<Canvas>(true);oldHudEnabled=new bool[oldHud.Length];
            for(int i=0;i<oldHud.Length;i++){oldHudEnabled[i]=oldHud[i].enabled;oldHud[i].enabled=false;}
            IsOpen=true;
            try {
                mainCamera.cullingMask=1<<5;mainCamera.clearFlags=CameraClearFlags.SolidColor;mainCamera.backgroundColor=new Color(.035f,.052f,.06f);
                stage=Instantiate(data.CityPrefab);stage.name="Vista de ciudad · "+state.Name;stage.transform.position=new Vector3(10000,0,10000);
                foreach(var item in stage.GetComponentsInChildren<Transform>(true))item.gameObject.layer=PreviewLayer;
                observedDevelopment=campaign.CityState(state.Id);observedConstructionTurn=campaign.State.Turn;CityGrowthPresentation.Apply(stage,observedDevelopment,observedConstructionTurn);
                stage.GetComponent<CityVisualFinish>()?.Apply();
                buildings.AddRange(stage.GetComponentsInChildren<CityBuildingView>());projectIndex=0;
                foreach(var lod in stage.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                var lightObject=new GameObject("Luz de ciudad");lightObject.transform.SetParent(stage.transform,false);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=CampaignVisualPalette.SunIntensity;light.color=CampaignVisualPalette.SunColor;light.shadows=LightShadows.Soft;light.cullingMask=1<<PreviewLayer;lightObject.layer=PreviewLayer;light.transform.rotation=Quaternion.Euler(37,-48,0);light.shadowStrength=.80f;light.shadowBias=.035f;light.shadowNormalBias=.15f;
                var fillRoot=new GameObject("Luz de relleno");fillRoot.transform.SetParent(stage.transform,false);fillRoot.transform.localPosition=new Vector3(-3,5,-4);var fill=fillRoot.AddComponent<Light>();fill.type=LightType.Point;fill.intensity=1.1f;fill.range=17;fill.color=new Color(.80f,.86f,1);fill.cullingMask=1<<PreviewLayer;
                var life=stage.GetComponent<CityAmbientLife>();if(life!=null)life.Initialize();
                var cameraObject=new GameObject("Camara de ciudad");cameraObject.transform.SetParent(stage.transform,false);previewCamera=cameraObject.AddComponent<Camera>();
                previewCamera.orthographic=false;previewCamera.fieldOfView=42;previewCamera.cullingMask=1<<PreviewLayer;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=80;previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.11f,.15f,.15f);
                previewCamera.allowHDR=false;previewCamera.allowMSAA=true;
                target=new RenderTexture(1728,1200,24,RenderTextureFormat.ARGB32){name="Vista de ciudad",antiAliasing=4};target.Create();previewCamera.targetTexture=target;
                CollectFramingPoints();focus=TownFocus();yaw=28;zoom=DefaultDistance;UpdateCamera();BuildUI();SelectPanel(CityPanelKind.Overview);return true;
            }catch {Close();throw;}
        }

        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;closedFrame=Time.frameCount;
            if(mainCamera!=null){mainCamera.cullingMask=previousMask;mainCamera.clearFlags=previousClear;mainCamera.backgroundColor=previousBackground;}
            bool battle=GetComponent<BattleLabController>()?.IsOpen==true;
            if(oldHud!=null)for(int i=0;i<oldHud.Length;i++)if(oldHud[i]!=null)oldHud[i].enabled=!battle&&oldHudEnabled[i];
            if(previewCamera!=null){previewCamera.enabled=false;previewCamera.targetTexture=null;}
            if(viewport!=null)viewport.texture=null;
            if(stage!=null){stage.SetActive(false);Release(stage);}if(ui!=null){ui.SetActive(false);Release(ui);}
            if(target!=null){target.Release();Release(target);}Release(outlineMaterial);
            stage=null;ui=null;target=null;previewCamera=null;viewport=null;heading=null;body=null;realmSummary=null;citySubtitle=null;cityNotice=null;primary=null;secondary=null;scroll=null;viewControls=null;outline=null;outlineMaterial=null;selectedBuilding=null;
            militaryNav=null;militarySelection=null;militaryTabs.Clear();militaryItem=0;MilitaryMode=CityMilitaryMode.Recruit;
            buildings.Clear();framingPoints.Clear();tabs.Clear();oldHud=null;oldHudEnabled=null;
        }

        private void Update()
        {
            if(!IsOpen)return;
            if(owner==null||!owner.Ready||GetComponent<BattleLabController>()?.IsOpen==true){Close();return;}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){Close();return;}
            if(campaign.CityRevision!=observedCityRevision||campaign.State!=observedTurn||campaign.Army!=observedArmy||campaign.ArmyRegistry!=observedRegistry||campaign.MarchDestination!=observedMarch)Refresh();
            var mouse=Mouse.current;
            if(mouse==null||!Application.isFocused||viewport==null||!RectTransformUtility.RectangleContainsScreenPoint(viewport.rectTransform,mouse.position.ReadValue()))return;
            if(viewControls!=null&&RectTransformUtility.RectangleContainsScreenPoint(viewControls,mouse.position.ReadValue()))return;
            if(mouse.rightButton.isPressed)Rotate(mouse.delta.ReadValue().x*.25f);
            float scroll=mouse.scroll.ReadValue().y;if(Mathf.Abs(scroll)>.01f)ChangeZoom(-Mathf.Sign(scroll)*.25f);
            if(mouse.leftButton.wasPressedThisFrame&&RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport.rectTransform,mouse.position.ReadValue(),null,out var point)) {
                var rect=viewport.rectTransform.rect;var uv=new Vector2((point.x-rect.xMin)/rect.width,(point.y-rect.yMin)/rect.height);
                if(Physics.Raycast(previewCamera.ViewportPointToRay(uv),out var hit,80,1<<PreviewLayer)) {
                    var building=hit.collider.GetComponentInParent<CityBuildingView>();if(building!=null)FocusBuilding(building);
                }
            }
        }

        public void SelectPanel(CityPanelKind panel)
        {
            if(!IsOpen)return;Panel=panel;
            for(int i=0;i<tabs.Count;i++)tabs[i].targetGraphic.color=i==(int)panel?new Color(.42f,.32f,.16f):new Color(.11f,.15f,.16f);
            Refresh();
            if(scroll!=null)scroll.verticalNormalizedPosition=1;
        }
        private string ArmyText()
        {
            var text=new StringBuilder();bool present=false;
            if(campaign.ArmyRegistry!=null) {
                foreach(var army in campaign.ArmyRegistry.Armies)if(army.Movement.Position==settlement.Position) {
                    present=true;text.AppendLine(army.Army.Id);
                    foreach(var formation in army.Army.Formations)text.AppendLine(formation.Id+" · "+formation.Quantity+" efectivos");text.AppendLine();
                }
            }
            else if(campaign.Army!=null&&campaign.Army.Position==settlement.Position){present=true;text.AppendLine("Hueste del jugador presente.");}
            if(!present)text.AppendLine("No hay huestes en esta ciudad.");
            if(campaign.Army!=null)text.Append("\nHUESTE ACTIVA\nPosición ").Append(campaign.Army.Position.Key).Append("\nMovimiento ").Append(campaign.Army.RemainingMovement).Append('/').Append(campaign.Army.MovementPerTurn);
            return text.ToString();
        }
        private void Refresh()
        {
            if(!IsOpen||body==null||campaign.State==null)return;
            if(owner.Registry==null||!owner.Registry.TryGet(settlement.Id,out var current)){Close();return;}
            settlement=current;canManage=owner.CanManage(current.Id);
            SynchronizeTown();
            var state=campaign.State;var resources=state.Resources;
            realmSummary.text=$"Turno {state.Turn}";
            var stock=new[]{resources.Grain,resources.Wood,resources.Stone,resources.Gold,resources.Labor};
            for(int i=0;i<realmStock.Length;i++)if(realmStock[i]!=null)realmStock[i].text=stock[i].ToString("N0");
            RefreshCityNotice();
            citySubtitle.text=owner.OwnerName(settlement.OwnerFactionId)+"  ·  "+(canManage?"Ciudad propia":"Inspección");
            primary.onClick.RemoveAllListeners();secondary.onClick.RemoveAllListeners();primary.interactable=true;secondary.interactable=true;
            ConfigureMilitaryNavigation();
            switch(Panel) {
                case CityPanelKind.Overview:
                    heading.text="LA CIUDAD";body.text=CityOverviewText();
                    Action(primary,"VER FORTALEZA",()=>FocusKind(CityBuildingKind.Fortress));Action(secondary,"VER LA VILLA",ResetView);break;
                case CityPanelKind.Buildings:
                    RefreshBuildings();break;
                case CityPanelKind.Treasury:
                    heading.text="HACIENDA DEL REINO";body.text=TreasuryText();
                    Action(primary,"FIN DE TURNO",CompleteTurn);primary.interactable=canManage&&campaignPanel.EndTurnButton.interactable;Action(secondary,"VER ÓRDENES",()=>SelectPanel(CityPanelKind.Orders));break;
                case CityPanelKind.Armies:
                    RefreshMilitaryPanel();break;
                default:
                    heading.text="ÓRDENES";body.text="DESTINO\n"+settlement.Name+"\n\n"+ArmyText()+"\n\n"+(campaign.MarchDestination.HasValue?"Marcha pendiente a "+campaign.MarchDestination.Value.Key:"Sin orden de marcha.");
                    Action(primary,"MARCHAR A ESTA CIUDAD",()=>SendArmy());primary.interactable=canManage&&owner.MarchButton!=null&&owner.MarchButton.interactable;
                    Action(secondary,"CANCELAR MARCHA",()=>{campaign.CancelMarch();Refresh();});secondary.interactable=canManage&&!campaign.HasPendingBattle&&campaign.MarchDestination.HasValue;break;
            }
            observedCityRevision=campaign.CityRevision;observedTurn=campaign.State;observedArmy=campaign.Army;observedRegistry=campaign.ArmyRegistry;observedMarch=campaign.MarchDestination;
            Canvas.ForceUpdateCanvases();if(scroll!=null)body.rectTransform.sizeDelta=new Vector2(-16,Mathf.Max(scroll.viewport.rect.height,body.preferredHeight+12));
        }
        private void SynchronizeTown()
        {
            var development=campaign.CityState(settlement.Id);
            if(ReferenceEquals(development,observedDevelopment)&&(development.PendingId==null||observedConstructionTurn==campaign.State.Turn))return;
            CityGrowthPresentation.Apply(stage,development,campaign.State.Turn);stage.GetComponent<CityVisualFinish>()?.Apply();observedDevelopment=development;observedConstructionTurn=campaign.State.Turn;
            buildings.Clear();buildings.AddRange(stage.GetComponentsInChildren<CityBuildingView>());
            if(selectedBuilding!=null&&!selectedBuilding.gameObject.activeInHierarchy){selectedBuilding=null;if(outline!=null)outline.enabled=false;}
            CollectFramingPoints();focus=TownFocus();UpdateCamera();
        }
        private void RefreshBuildings()
        {
            var development=campaign.CityState(settlement.Id);
            heading.text="EDIFICIOS";var text=new StringBuilder();
            if(selectedBuilding!=null)text.AppendLine(selectedBuilding.DisplayName+"\n"+Description(selectedBuilding.Kind)+"\n"+BuildingEffectText(selectedBuilding.ConstructionId)+"\n");
            text.Append("VIVIENDAS: ").Append(development.Houses).AppendLine("\n");
            text.AppendLine("CONSTRUIDOS");foreach(var building in buildings)text.AppendLine(building.DisplayName);
            if(development.PendingId!=null) {
                var pending=CityProjects.Get(development.PendingId);
                int remaining=development.CompletionTurn-campaign.State.Turn;
                text.Append("\nEN CONSTRUCCIÓN\n").Append(pending.Name).Append("\nProgreso: ").Append(pending.Turns-remaining).Append('/').Append(pending.Turns)
                    .Append(" turnos\nFinaliza al llegar al turno ").Append(development.CompletionTurn).Append("\nCoste de obra ya abonado.\n\n").Append(ProjectEffectText(pending));
                Action(primary,"FIN DE TURNO",CompleteTurn);primary.interactable=canManage&&campaignPanel.EndTurnButton.interactable;
                Action(secondary,"VER FORTALEZA",()=>FocusKind(CityBuildingKind.Fortress));
            } else {
                var available=new List<CityProject>();foreach(var item in CityProjects.All)if(development.CanStart(item.Id))available.Add(item);
                if(available.Count==0) {
                    text.Append("\nTodas las obras de esta villa están terminadas.");Action(primary,"VER LA VILLA",ResetView);Action(secondary,"VER FORTALEZA",()=>FocusKind(CityBuildingKind.Fortress));
                } else {
                    projectIndex%=available.Count;var project=available[projectIndex];var cost=project.Cost;
                    text.Append("\nPRÓXIMA OBRA  ").Append(projectIndex+1).Append('/').Append(available.Count).Append("\n").Append(project.Name)
                        .Append("\nDuración: ").Append(project.Turns).Append(" turnos\nMadera ").Append(cost.Wood).Append(" · Piedra ").Append(cost.Stone)
                        .Append("\nOro ").Append(cost.Gold).Append(" · Mano de obra ").Append(cost.Labor);
                    text.Append("\n\n").Append(ProjectEffectText(project));
                    bool affordable=campaign.State.Resources.TrySpend(cost,out _);
                    if(!affordable)text.Append("\n\nFALTAN RECURSOS\n").Append(ResourceLines(campaign.State.Resources.MissingFor(cost))).Append("\nConsulta la previsión en Hacienda.");
                    Action(primary,"CONSTRUIR "+project.Name.ToUpperInvariant(),()=>Construct(project.Id));primary.interactable=canManage&&!campaign.HasPendingBattle&&!campaign.IsCampaignFinished&&affordable;
                    Action(secondary,"OTRA OBRA  ›",()=>{projectIndex++;Refresh();});secondary.interactable=available.Count>1;
                }
            }
            body.text=text.ToString();
        }
        public bool Construct(string projectId)
        {
            if(!IsOpen||!canManage||!campaign.TryConstruct(settlement.Id,projectId))return false;
            Refresh();return true;
        }
        public bool SendArmy()
        {
            if(!IsOpen||!canManage||owner.MarchButton==null||!owner.MarchButton.interactable)return false;
            var controller=owner;Close();return controller.MarchToSelection();
        }
        public void CompleteTurn(){if(IsOpen&&canManage&&campaignPanel.EndTurnButton.interactable){campaign.CompleteTurn();Refresh();}}
        private static string Description(CityBuildingKind kind)=>kind switch {
            CityBuildingKind.Fortress=>"El núcleo fortificado preside la villa.",CityBuildingKind.Market=>"El mercado ocupa la plaza, frente a la fortaleza.",
            CityBuildingKind.Warehouse=>"Un almacén junto al camino de acceso.",CityBuildingKind.Well=>"El pozo se encuentra en la plaza de la villa.",_=>"Vivienda de cal, piedra y madera, con cubierta de teja."};
        public void FocusKind(CityBuildingKind kind){foreach(var building in buildings)if(building.Kind==kind){FocusBuilding(building);break;}}
        private void FocusBuilding(CityBuildingView building)
        {
            selectedBuilding=building;focus=TownFocus();zoom=MinimumDistance;
            if(outline==null){var root=new GameObject("Edificio seleccionado");root.transform.SetParent(stage.transform);root.layer=PreviewLayer;outline=root.AddComponent<LineRenderer>();outline.loop=true;outline.useWorldSpace=true;outline.positionCount=4;outline.startWidth=outline.endWidth=.025f;outlineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));outlineMaterial.SetColor("_BaseColor",new Color(1,.72f,.18f));outline.sharedMaterial=outlineMaterial;}
            var b=building.Bounds;float y=b.min.y+.015f;outline.SetPositions(new[]{new Vector3(b.min.x-.05f,y,b.min.z-.05f),new Vector3(b.max.x+.05f,y,b.min.z-.05f),new Vector3(b.max.x+.05f,y,b.max.z+.05f),new Vector3(b.min.x-.05f,y,b.max.z+.05f)});outline.enabled=true;
            UpdateCamera();SelectPanel(CityPanelKind.Buildings);
        }
        public void ResetView(){if(!IsOpen)return;focus=TownFocus();zoom=DefaultDistance;yaw=28;if(outline!=null)outline.enabled=false;selectedBuilding=null;UpdateCamera();Refresh();}
        public void Rotate(float degrees){if(!IsOpen)return;yaw=Mathf.Repeat(yaw+degrees,360);UpdateCamera();}
        public void ChangeZoom(float delta){if(!IsOpen)return;zoom=Mathf.Clamp(zoom+delta,MinimumDistance,MaximumDistance);UpdateCamera();}
        private void CollectFramingPoints()
        {
            framingPoints.Clear();bool first=true;var ground=stage.transform.Find("Terreno de la villa");
            foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>()) {
                if(ground!=null&&(renderer.transform==ground||renderer.transform.IsChildOf(ground)))continue;
                var bounds=renderer.bounds;bounds.Expand(.12f);
                if(first){townBounds=bounds;first=false;}else townBounds.Encapsulate(bounds);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    framingPoints.Add(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
            }
        }
        private Vector3 TownFocus()=>new Vector3(townBounds.center.x,stage.transform.position.y+.55f,townBounds.center.z);
        private void UpdateCamera()
        {
            if(previewCamera==null)return;
            var offset=Quaternion.Euler(0,yaw,0)*new Vector3(0,Mathf.Sin(31*Mathf.Deg2Rad),-Mathf.Cos(31*Mathf.Deg2Rad));
            var rotation=Quaternion.LookRotation(-offset,Vector3.up);
            float distance=Mathf.Max(zoom,CityCameraFraming.RequiredDistance(framingPoints,focus,rotation,previewCamera.fieldOfView,previewCamera.aspect));
            previewCamera.transform.SetPositionAndRotation(focus+offset*distance,rotation);
        }

        private void BuildUI()
        {
            ui=new GameObject("City Management UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));ui.transform.SetParent(transform,false);
            var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            var backdrop=Stretch("Background",ui.transform,new Vector2(0,0),new Vector2(1,1),Vector2.zero,Vector2.zero);Image(backdrop,new Color(.035f,.052f,.06f));
            var imageRegion=Stretch("City viewport region",ui.transform,Vector2.zero,Vector2.one,new Vector2(238,146),new Vector2(-370,-116));
            var imageRect=Stretch("City viewport",imageRegion,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);viewport=imageRect.gameObject.AddComponent<RawImage>();viewport.texture=target;
            var fit=imageRect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1.44f;
            var header=Stretch("Header",ui.transform,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-108),Vector2.zero);Image(header,new Color(.07f,.095f,.105f));
            Text("Realm",header,new Vector2(28,-13),new Vector2(850,22),"CASTILLA  /  VISTA DE CIUDAD",14,new Color(.83f,.65f,.34f));
            var title=Text("City name",header,new Vector2(28,-39),new Vector2(1000,42),settlement.Name.ToUpperInvariant(),30,new Color(.95f,.9f,.79f));title.rectTransform.anchorMin=new Vector2(0,1);title.rectTransform.anchorMax=Vector2.one;title.rectTransform.offsetMin=new Vector2(28,-81);title.rectTransform.offsetMax=new Vector2(-340,-39);
            citySubtitle=Text("Owner",header,new Vector2(28,-82),new Vector2(920,20),"",14,new Color(.74f,.78f,.73f));
            var exit=Stretch("Return",header,Vector2.one,Vector2.one,new Vector2(-308,-77),new Vector2(-28,-31));CreateButton(exit,"VOLVER AL MAPA  ·  Esc",Close);
            var nav=Stretch("Navigation",ui.transform,new Vector2(0,0),new Vector2(0,1),new Vector2(20,100),new Vector2(216,-116));Image(nav,new Color(.075f,.10f,.11f));
            Text("Heading",nav,new Vector2(18,-22),new Vector2(168,30),"GESTIÓN",19,new Color(.83f,.65f,.34f));
            string[] labels={"Ciudad","Edificios","Hacienda","Huestes","Órdenes"};
            for(int i=0;i<labels.Length;i++){int index=i;var row=Fixed("Tab "+labels[i],nav,new Vector2(12,-78-i*66),new Vector2(172,52));tabs.Add(CreateButton(row,labels[i].ToUpperInvariant(),()=>SelectPanel((CityPanelKind)index)));}
            var content=Stretch("Management panel",ui.transform,new Vector2(1,0),new Vector2(1,1),new Vector2(-350,100),new Vector2(-20,-116));Image(content,new Color(.075f,.10f,.11f));
            heading=Text("Section",content,new Vector2(24,-26),new Vector2(282,70),"",24,new Color(.93f,.82f,.57f));
            var textRect=Stretch("Details scroll",content,new Vector2(0,0),new Vector2(1,1),new Vector2(24,160),new Vector2(-24,-115));Image(textRect,new Color(0,0,0,0));textRect.gameObject.AddComponent<RectMask2D>();scroll=textRect.gameObject.AddComponent<ScrollRect>();scroll.viewport=textRect;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var bodyRect=Stretch("Details",textRect,new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);bodyRect.pivot=new Vector2(.5f,1);bodyRect.sizeDelta=new Vector2(-16,0);body=bodyRect.gameObject.AddComponent<Text>();Style(body,18,new Color(.88f,.88f,.80f));scroll.content=bodyRect;
            var scrollbarRect=Stretch("Details scrollbar",textRect,new Vector2(1,0),Vector2.one,new Vector2(-7,0),Vector2.zero);Image(scrollbarRect,new Color(.12f,.15f,.15f));
            var scrollbar=scrollbarRect.gameObject.AddComponent<Scrollbar>();var handle=Stretch("Handle",scrollbarRect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);Image(handle,new Color(.65f,.54f,.32f));
            scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            var primaryRect=Stretch("Primary action",content,new Vector2(0,0),new Vector2(1,0),new Vector2(24,88),new Vector2(-24,136));primary=CreateButton(primaryRect,"",()=>{});
            var secondaryRect=Stretch("Secondary action",content,new Vector2(0,0),new Vector2(1,0),new Vector2(24,26),new Vector2(-24,74));secondary=CreateButton(secondaryRect,"",()=>{});
            var footer=Stretch("Resources",ui.transform,new Vector2(0,0),new Vector2(1,0),Vector2.zero,new Vector2(0,88));Image(footer,new Color(.07f,.095f,.105f));
            realmSummary=Text("Resources",footer,new Vector2(28,-12),new Vector2(1860,30),"",19,new Color(.93f,.82f,.57f));
            string[] names={"Grano","Madera","Piedra","Oro","Mano de obra"};
            for(int i=0;i<names.Length;i++)
            {
                var x=200+i*190;
                FulgorIcons.Create("Icon "+names[i],footer,FulgorIcons.Resources5[i],new Vector2(x,-6),40);
                realmStock[i]=Text("Stock "+names[i],footer,new Vector2(x+48,-6),new Vector2(136,24),"",20,new Color(.95f,.9f,.79f));
                Text("Name "+names[i],footer,new Vector2(x+48,-30),new Vector2(136,18),names[i].ToUpperInvariant(),12,new Color(.74f,.78f,.73f));
            }
            cityNotice=Text("City activity",footer,new Vector2(28,-49),new Vector2(1860,28),"",16,new Color(.80f,.84f,.76f));
            var controls=Stretch("View controls",ui.transform,Vector2.zero,new Vector2(1,0),new Vector2(238,100),new Vector2(-370,138));viewControls=controls;
            var layout=controls.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=true;
            CreateButton(Fixed("Rotate left",controls,new Vector2(0,0),new Vector2(140,38)),"GIRAR ‹",()=>Rotate(-15));
            CreateButton(Fixed("Zoom out",controls,new Vector2(156,0),new Vector2(72,38)),"−",()=>ChangeZoom(.35f));
            CreateButton(Fixed("Zoom in",controls,new Vector2(238,0),new Vector2(72,38)),"+",()=>ChangeZoom(-.35f));
            CreateButton(Fixed("Rotate right",controls,new Vector2(326,0),new Vector2(140,38)),"› GIRAR",()=>Rotate(15));
            CreateButton(Fixed("Reset view",controls,new Vector2(482,0),new Vector2(210,38)),"VISTA GENERAL",ResetView);
            float[] weights={2,1,1,2,3};for(int i=0;i<controls.childCount;i++){var element=controls.GetChild(i).gameObject.AddComponent<LayoutElement>();element.minWidth=0;element.preferredWidth=0;element.flexibleWidth=weights[i];}
            foreach(var item in ui.GetComponentsInChildren<Transform>(true))item.gameObject.layer=5;
            Canvas.ForceUpdateCanvases();
        }
        private static RectTransform Stretch(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax){var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var r=(RectTransform)root.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;}
        private static RectTransform Fixed(string name,Transform parent,Vector2 position,Vector2 size){var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        private static void Image(RectTransform rect,Color color){var image=rect.gameObject.AddComponent<Image>();image.color=color;}
        private static void Style(Text text,int size,Color color){text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
        private static Text Text(string name,Transform parent,Vector2 position,Vector2 size,string value,int fontSize,Color color){var rect=Fixed(name,parent,position,size);var text=rect.gameObject.AddComponent<Text>();Style(text,fontSize,color);text.text=value;return text;}
        private static Button CreateButton(RectTransform rect,string label,UnityEngine.Events.UnityAction action){Image(rect,new Color(.26f,.27f,.20f));var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();var colors=button.colors;colors.highlightedColor=new Color(1.15f,1.1f,.9f);colors.disabledColor=new Color(.38f,.38f,.38f);button.colors=colors;var caption=Text("Label",rect,Vector2.zero,rect.rect.size,label,16,new Color(.97f,.94f,.85f));caption.rectTransform.anchorMin=Vector2.zero;caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.offsetMin=caption.rectTransform.offsetMax=Vector2.zero;caption.alignment=TextAnchor.MiddleCenter;button.onClick.AddListener(action);return button;}
        private static void Action(Button button,string label,UnityEngine.Events.UnityAction action){button.GetComponentInChildren<Text>().text=label;button.onClick.AddListener(action);}
        private static void Release(UnityEngine.Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDisable()=>Close();
    }
}
