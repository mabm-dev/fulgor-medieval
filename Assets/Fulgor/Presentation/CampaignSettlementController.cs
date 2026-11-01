using System;
using System.Linq;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    [RequireComponent(typeof(CampaignBoardPreview))]
    public sealed class CampaignSettlementController : MonoBehaviour
    {
        [SerializeField] private CampaignSettlementDefinition[] settlements=Array.Empty<CampaignSettlementDefinition>();
        [SerializeField] private string playerFactionId="castilla";
        [SerializeField] private bool reuseArtReviewFortress;
        private readonly Dictionary<HexCoordinates,CampaignSettlementView> views=new();
        private readonly List<GameObject> ownedVisuals=new();
        private Dictionary<HexCoordinates,HexTileView> boardTiles;
        private CampaignBoardSelection selection;
        private CampaignTurnController campaign;
        private CampaignSelectionPanel campaignPanel;
        private Transform generation;
        private Canvas canvas;
        private RectTransform panel;
        private Text details,status;
        private ArmyMovementState observedArmy;
        private CampaignArmyRegistry observedRegistry;
        private CampaignTurnState observedTurn;
        private bool observedCanMove;
        public bool Ready { get; private set; }
        public CampaignSettlementRegistry Registry { get; private set; }
        public IReadOnlyDictionary<HexCoordinates,CampaignSettlementView> Views=>views;
        public Button LocateButton { get; private set; }
        public Button MarchButton { get; private set; }
        public Button VisitButton { get; private set; }
        public string Details=>details!=null?details.text:"";
        public string ConfiguredPlayerFaction=>playerFactionId;
        private string EffectivePlayerFaction=>campaign?.PlayerFactionId??playerFactionId;
        public CampaignSettlementRegistry CreateInitialRegistry(MapRouteGraph graph)
        {
            var cities=new List<CampaignSettlement>();
            foreach(var definition in settlements)
            {
                if(definition==null)throw new ArgumentException("Missing settlement definition.");
                cities.Add(definition.Create());
            }
            return new CampaignSettlementRegistry(graph,cities);
        }
        public string OwnerName(string faction)
        {
            foreach(var definition in settlements)
                if(definition!=null&&definition.Create().OwnerFactionId==faction)return definition.OwnerDisplayName;
            return faction==EffectivePlayerFaction?"Tu reino":faction;
        }
        public void PublishOwnership(CampaignSettlementRegistry owners)
        {
            if(!Ready)return;
            Registry=owners;
            foreach(var pair in views)
                if(Registry.TryGet(pair.Key,out var city)){bool own=city.OwnerFactionId==EffectivePlayerFaction;pair.Value.SetOwner(own);pair.Value.SetTerritory(own?boardTiles:null,TerritoryColor);}
            Refresh();
        }

        public IEnumerable<string> SettlementIds { get {foreach(var data in settlements)if(data!=null)yield return data.Create().Id;} }
        public bool HasSettlementAt(HexCoordinates position)
        {
            foreach(var data in settlements)if(data!=null&&data.Create().Position==position)return true;
            return false;
        }
        private Color TerritoryColor{get{var design=KingdomDesignCatalog.Find(EffectivePlayerFaction);return design!=null&&ColorUtility.TryParseHtmlString("#"+design.PrimaryColor,out var color)?color:new Color(.15f,.48f,.92f);}}
        public bool CanManage(string id)=>Ready&&id!=null&&Registry.TryGet(id,out var city)&&city.OwnerFactionId==EffectivePlayerFaction;

        private void LateUpdate()
        {
            var root=transform.Find("Generated Board");
            if(root==null||!root.gameObject.activeInHierarchy){if(Ready)Clear();return;}
            if(root!=generation) {
                if(reuseArtReviewFortress) {
                    var art=FindFirstObjectByType<FortressMapArtReview>();
                    var terrain=FindFirstObjectByType<TerrainMapArtReview>();var water=FindFirstObjectByType<WaterMapArtReview>();
                    if(art==null||!art.PlacementReady||terrain!=null&&!terrain.Ready||water!=null&&!water.Ready)return;
                }
                try{Initialize(root);}catch(Exception e){Clear();Debug.LogException(e,this);enabled=false;}
            }
            if(!Ready)return;
            bool canMove=campaignPanel.MoveArmyButton.interactable;
            if(observedArmy!=campaign.Army||observedRegistry!=campaign.ArmyRegistry||observedTurn!=campaign.State||observedCanMove!=canMove)Refresh();
        }

        private void Initialize(Transform root)
        {
            var owner=GetComponent<CampaignBoardSelection>();
            if(owner==null||owner.Routes==null)throw new InvalidOperationException("El asentamiento necesita un tablero preparado.");
            var logical=new List<CampaignSettlement>();
            foreach(var definition in settlements) {
                if(definition==null||definition.Prefab==null||string.IsNullOrWhiteSpace(definition.OwnerDisplayName))throw new ArgumentException("Asentamiento sin datos o prefab.");
                logical.Add(definition.Create());
            }
            var registry=new CampaignSettlementRegistry(owner.Routes,logical);
            var tileLookup=new Dictionary<HexCoordinates,HexTileView>();
            foreach(var tile in root.GetComponentsInChildren<HexTileView>())tileLookup.Add(tile.Cell.Coordinates,tile);
            Clear();boardTiles=tileLookup;selection=owner;campaign=GetComponent<CampaignTurnController>();campaignPanel=GetComponent<CampaignSelectionPanel>();
            Registry=registry;generation=root;
            for(int i=0;i<logical.Count;i++) {
                var settlement=logical[i];var tile=tileLookup[settlement.Position];GameObject visual=null;
                if(reuseArtReviewFortress) {
                    var art=FindFirstObjectByType<FortressMapArtReview>();
                    if(art.ReviewTile!=tile&&art.ReviewTile!=null&&art.ReviewTile.gameObject.activeInHierarchy)
                        throw new InvalidOperationException("La muestra de fortaleza no coincide con el asentamiento.");
                    visual=art.PlacedFortress.gameObject;
                    if(art.ReviewTile!=tile) {
                        var source=new TerrainGroundSurface(tile.GetComponent<MeshCollider>().sharedMesh);
                        if(!source.TrySample(tile.transform,tile.transform.position,out var ground,out _,out _))throw new InvalidOperationException("No hay suelo para la fortaleza.");
                        visual.transform.position=ground+Vector3.up*.008f;
                    }
                }
                if(visual==null) {
                    visual=Instantiate(settlements[i].Prefab,transform);ownedVisuals.Add(visual);visual.name=settlement.Name;
                    visual.transform.position=tile.transform.position;visual.transform.rotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
                    var collider=tile.GetComponent<MeshCollider>();var surface=new TerrainGroundSurface(collider.sharedMesh);
                    if(!surface.TrySample(tile.transform,tile.transform.position,out var ground,out _,out _))throw new InvalidOperationException("No hay suelo para el asentamiento.");
                    var position=ground+Vector3.up*.008f;visual.transform.position=position;
                }
                var finish=visual.GetComponent<CityVisualFinish>();if(finish==null)finish=visual.AddComponent<CityVisualFinish>();finish.Apply();
                var view=visual.AddComponent<CampaignSettlementView>();view.Initialize(tile);views.Add(settlement.Position,view);
            }
            if(logical.Count>0)CreatePanel();
            selection.SelectionChanged+=Refresh;Ready=true;
            campaign.SynchronizeSettlementDefinitions(registry);
            PublishOwnership(campaign.SettlementState);Refresh();
        }

        public bool OpenCityAt(HexCoordinates position)
        {
            if(!Ready||campaign?.CanObserve(position)==false||GetComponent<BattleLabController>()?.IsOpen==true||!Registry.TryGet(position,out var settlement))return false;
            CampaignSettlementDefinition definition=null;
            foreach(var item in settlements)if(item.Create().Id==settlement.Id){definition=item;break;}
            if(definition==null||definition.CityPrefab==null)return false;
            selection.Select(views[position].Tile);
            var city=GetComponent<CityManagementView>();if(city==null)city=gameObject.AddComponent<CityManagementView>();
            return city.Open(this,settlement,definition,settlement.OwnerFactionId==EffectivePlayerFaction);
        }

        public bool LocateFirst()
        {
            if(!Ready||selection.IsInteractionSuspended||Registry.Settlements.Count==0)return false;
            if(campaign.FogEnabled&&!Registry.Settlements.Any(c=>c.OwnerFactionId==EffectivePlayerFaction||campaign.Fog?.Intel(EffectivePlayerFaction,c.Id)!=null))return false;
            var target=Registry.Settlements[0];
            foreach(var item in Registry.Settlements)if(item.OwnerFactionId==EffectivePlayerFaction){target=item;break;}
            if(campaign.FogEnabled&&target.OwnerFactionId!=EffectivePlayerFaction)target=Registry.Settlements.First(c=>campaign.Fog.Intel(EffectivePlayerFaction,c.Id)!=null);
            return selection.LocateCell(target.Position);
        }
        public bool MarchToSelection()
        {
            if(Ready&&!selection.IsInteractionSuspended&&selection.SelectedCell.HasValue&&campaign.FogEnabled&&!campaign.CanObserve(selection.SelectedCell.Value.Coordinates))
            {
                if(!campaignPanel.MoveArmyButton.interactable)return false;
                var before=campaign.Army?.Position;campaign.MoveToSelectedCell();Refresh();return campaign.Army?.Position!=before||campaign.MarchDestination==selection.SelectedCell.Value.Coordinates;
            }
            if(!Ready||selection.IsInteractionSuspended||!selection.SelectedCell.HasValue||
                !Registry.TryGet(selection.SelectedCell.Value.Coordinates,out var settlement)||
                settlement.OwnerFactionId!=EffectivePlayerFaction&&campaign.ArmyRegistry==null||
                !campaignPanel.MoveArmyButton.interactable)return false;
            var previous=campaign.Army?.Position;campaign.MoveToSelectedCell();Refresh();
            return campaign.Army?.Position!=previous||campaign.MarchDestination==settlement.Position;
        }

        private void Refresh()
        {
            if(!Ready)return;
            CampaignSettlement selected=null;
            if(selection.SelectedCell.HasValue)Registry.TryGet(selection.SelectedCell.Value.Coordinates,out selected);
            foreach(var pair in views)pair.Value.SetSelected(selected!=null&&pair.Key==selected.Position);
            if(details==null)return;
            if(selection.SelectedCell.HasValue&&!campaign.CanObserve(selection.SelectedCell.Value.Coordinates))
            {
                var intel=selected!=null?campaign.Fog?.Intel(EffectivePlayerFaction,selected.Id):null;
                details.text=intel==null?"Sin información de asentamientos.":selected.Name+"\nÚltimo control conocido: "+OwnerName(intel.OwnerFactionId)+"\nNoticia del turno "+intel.LastSeenTurn;
                status.text="Presencia y defensas desconocidas.";VisitButton.interactable=false;MarchButton.interactable=campaignPanel.MoveArmyButton.interactable;
                MarchButton.GetComponentInChildren<Text>().text="MARCHAR A CASILLA";
                observedArmy=campaign.Army;observedRegistry=campaign.ArmyRegistry;observedTurn=campaign.State;observedCanMove=campaignPanel.MoveArmyButton.interactable;return;
            }
            if(selected==null) {
                details.text="Selecciona una fortaleza para consultar su control y las huestes presentes.";
                status.text="Localizar centra la cámara en tu fortaleza.";MarchButton.interactable=false;
            }
            else {
                string owner=OwnerName(selected.OwnerFactionId);
                string presence="Sin huestes en esta casilla.";
                if(campaign.ArmyRegistry!=null) {
                    foreach(var army in campaign.ArmyRegistry.Armies)if(army.Movement.Position==selected.Position){presence="Hueste presente: "+army.Army.Id;break;}
                }
                else if(campaign.Army!=null&&campaign.Army.Position==selected.Position)presence="Hueste del jugador presente.";
                details.text=selected.Name+"\nControl: "+owner+"\n"+selected.Position.Key+" · "+(selected.OwnerFactionId==EffectivePlayerFaction?"Fortaleza propia":"Otra facción");
                status.text=presence;
                bool hostile=selected.OwnerFactionId!=EffectivePlayerFaction;
                MarchButton.GetComponentInChildren<Text>().text=hostile?"OCUPAR / ATACAR":"MARCHAR AQUÍ";
                MarchButton.interactable=campaignPanel.MoveArmyButton.interactable&&(!hostile||campaign.ArmyRegistry!=null);
            }
            VisitButton.interactable=selected!=null;
            observedArmy=campaign.Army;observedRegistry=campaign.ArmyRegistry;observedTurn=campaign.State;observedCanMove=campaignPanel.MoveArmyButton.interactable;
        }

        public bool BlocksPointer(Vector2 screenPosition)=>GetComponent<CityManagementView>()?.IsOpen==true||Ready&&canvas!=null&&canvas.enabled&&panel!=null&&panel.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(panel,screenPosition);
        private void CreatePanel()
        {
            var root=new GameObject("Settlement HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=21;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            panel=Rect("Settlement information",root.transform,new Vector2(324,288),new Vector2(20,-370));
            var background=panel.gameObject.AddComponent<Image>();background.color=LegacyUI.Ink;background.raycastTarget=true;
            Label("Title",panel,new Vector2(18,-16),new Vector2(288,26),"ASENTAMIENTO",18,LegacyUI.Gold);
            details=Label("Details",panel,new Vector2(18,-54),new Vector2(288,76),"",16,LegacyUI.Paper);
            status=Label("Presence",panel,new Vector2(18,-136),new Vector2(288,42),"",14,new Color(.68f,.73f,.72f));
            LocateButton=Button("Locate","TU FORTALEZA",new Vector2(18,-188));LocateButton.onClick.AddListener(()=>LocateFirst());
            MarchButton=Button("March","MARCHAR AQUÍ",new Vector2(168,-188));MarchButton.onClick.AddListener(()=>MarchToSelection());
            VisitButton=Button("Open city","GESTIONAR CIUDAD",new Vector2(18,-236));VisitButton.GetComponent<RectTransform>().sizeDelta=new Vector2(288,36);
            VisitButton.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(288,36);
            LegacyUI.Skin(panel);
            VisitButton.onClick.AddListener(()=>{if(selection.SelectedCell.HasValue)OpenCityAt(selection.SelectedCell.Value.Coordinates);});
        }
        private Button Button(string name,string caption,Vector2 position)
        {
            var rect=Rect(name,panel,new Vector2(138,36),position);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.25f,.36f,.18f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var text=Label("Label",rect,Vector2.zero,rect.sizeDelta,caption,13,Color.white);text.alignment=TextAnchor.MiddleCenter;return button;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var rect=(RectTransform)root.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        private static Text Label(string name,Transform parent,Vector2 position,Vector2 size,string text,int fontSize,Color color)
        {
            var rect=Rect(name,parent,size,position);var label=rect.gameObject.AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=fontSize;label.color=color;label.raycastTarget=false;return label;
        }
        private static void Release(UnityEngine.Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void Clear()
        {
            GetComponent<CityManagementView>()?.Close();
            Ready=false;if(selection!=null)selection.SelectionChanged-=Refresh;
            foreach(var view in views.Values){if(view!=null){view.SetSelected(false);Release(view);}}
            foreach(var visual in ownedVisuals){if(visual!=null){visual.SetActive(false);Release(visual);}}
            views.Clear();ownedVisuals.Clear();if(canvas!=null){canvas.gameObject.SetActive(false);Release(canvas.gameObject);}
            canvas=null;panel=null;details=null;status=null;LocateButton=null;MarchButton=null;VisitButton=null;Registry=null;generation=null;
        }
        private void OnDisable()=>Clear();
    }
}
