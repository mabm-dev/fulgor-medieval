using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Gestión inicial de Castilla sobre los hexágonos reales de campaña.</summary>
    public sealed class CampaignCityMapView : MonoBehaviour
    {
        private static readonly string[] Projects = { "house", "farm", "lumber" };
        private static readonly string[] Names = { "Vivienda", "Huerto", "Leñera" };
        private static readonly FulgorIcon[] Icons = { FulgorIcon.Population, FulgorIcon.Grain, FulgorIcon.Wood };
        private CampaignTurnController campaign;
        private CampaignSettlement city;
        private GameObject root;
        private RectTransform canvas, toolbar, legend, queue, heading, backArea, leftColumn, cityDetails, treasury;
        private readonly Text[] treasuryValues=new Text[5], treasuryIncome=new Text[5];
        private readonly Text[] workNames=new Text[6], workTurns=new Text[6], cityValues=new Text[4], cityProduction=new Text[5];
        private readonly Image[] workClocks=new Image[6];
        private Text title, description, queueText, legendTitle;
        private readonly Image[,] resourceIcons = new Image[4,5];
        private readonly Text[,] resourceValues = new Text[4,5];
        private readonly List<(RectTransform rect, HexTileView tile)> markers = new();
        private readonly List<GameObject> markerObjects = new();
        private readonly List<Sprite> ownedSprites = new();
        private readonly List<GameObject> selectionBorders = new();
        private readonly List<RectTransform> rings = new();
        private Texture2D ringTexture;
        private Sprite ringSprite;
        private int selected = 1, revision = -1, closedFrame = -1;
        public bool IsOpen => root != null && root.activeSelf;
        public bool ClosedThisFrame => closedFrame == Time.frameCount;
        public Button BackButton { get; private set; }

        public bool Open(CampaignSettlementController settlements, CampaignSettlement target)
        {
            campaign=GetComponent<CampaignTurnController>();
            if(settlements==null||target==null||campaign==null||!settlements.CanManage(target.Id))return false;
            Close(); city=target;
            try{Build();}catch{Close();throw;}
            GetComponent<CampaignSelectionPanel>()?.SetCityMapMode(true);settlements.SetCityMapMode(true);
            if(settlements.Views.TryGetValue(target.Position,out var view))Camera.main?.GetComponent<CampaignCameraController>()?.FocusOnWorld(view.Tile.transform.position);
            GetComponent<CampaignArmyMarker>()?.SetCityVisible(false);
            GetComponent<CampaignArmyMarkerLayer>()?.SetCityVisible(false);
            Refresh();Inspect(null);return true;
        }

        private void Build()
        {
            root=CampaignMenuUI.Canvas("Construcción en el mapa",transform,50);canvas=(RectTransform)root.transform;
            heading=CampaignMenuUI.Rect("Reino",canvas,new Vector2(0,1),new Vector2(20,-12),new Vector2(520,175));
            var crestTexture=Resources.Load<Texture2D>("CityMap/castilla");
            if(crestTexture!=null)
            {
                var crestRect=CampaignMenuUI.Rect("Estandarte",heading,new Vector2(0,.5f),new Vector2(0,0),new Vector2(510,175));
                var crest=crestRect.gameObject.AddComponent<Image>();crest.preserveAspect=true;crest.raycastTarget=false;
                crest.sprite=Sprite.Create(crestTexture,new Rect(0,0,crestTexture.width,crestTexture.height),new Vector2(.5f,.5f));ownedSprites.Add(crest.sprite);
            }
            else title=CampaignMenuUI.Text("Title",heading,new Vector2(.5f,.5f),Vector2.zero,new Vector2(315,70),"CASTILLA  ·  "+city.Name,25,CampaignMenuUI.Gold);
            treasury=CampaignMenuUI.Rect("Reservas del reino",canvas,new Vector2(.5f,1),new Vector2(90,-12),new Vector2(960,80));
            AddClip(treasury,12,55);
            var stockSurface=CampaignMenuUI.Rect("Fondo",treasury,new Vector2(.5f,.5f),Vector2.zero,treasury.sizeDelta);
            var treasuryBackground=stockSurface.gameObject.AddComponent<Image>();treasuryBackground.color=new Color(.10f,.07f,.045f,1f);treasuryBackground.raycastTarget=false;
            var resourceTexture=Resources.Load<Texture2D>("CityMap/resources_reference");
            if(resourceTexture!=null)
            {
                var border=CampaignMenuUI.Rect("Marco de recursos",treasury,new Vector2(.5f,.5f),Vector2.zero,treasury.sizeDelta);
                var image=border.gameObject.AddComponent<Image>();image.raycastTarget=false;
                image.sprite=Sprite.Create(resourceTexture,new Rect(resourceTexture.width*.066f,resourceTexture.height*.39f,resourceTexture.width*.934f,resourceTexture.height*.28f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(150,20,40,20));ownedSprites.Add(image.sprite);image.type=Image.Type.Sliced;image.fillCenter=false;image.pixelsPerUnitMultiplier=2;
            }
            for(int i=0;i<5;i++)
            {
                FulgorIcons.Create("Reserva icono",treasury,FulgorIcons.Resources5[i],new Vector2(78+i*169,-20),31);
                treasuryValues[i]=CampaignMenuUI.Text("Reserva "+i,treasury,new Vector2(0,1),new Vector2(119+i*169,-17),new Vector2(115,28),"",22,CampaignMenuUI.Paper);treasuryValues[i].alignment=TextAnchor.MiddleLeft;
                treasuryIncome[i]=CampaignMenuUI.Text("Ingreso "+i,treasury,new Vector2(0,1),new Vector2(119+i*169,-47),new Vector2(115,20),"",12,new Color(.67f,.75f,.56f));treasuryIncome[i].alignment=TextAnchor.MiddleLeft;
            }
            toolbar=CampaignMenuUI.Rect("Edificios",canvas,new Vector2(.5f,0),new Vector2(0,12),new Vector2(850,185));
            var frameTexture=Resources.Load<Texture2D>("CityMap/panel_3");
            if(frameTexture!=null)
            {
                var frame=toolbar.gameObject.AddComponent<Image>();frame.raycastTarget=false;
                frame.sprite=Sprite.Create(frameTexture,new Rect(frameTexture.width*.014f,frameTexture.height*.24f,frameTexture.width*.97f,frameTexture.height*.52f),new Vector2(.5f,.5f));ownedSprites.Add(frame.sprite);
            }
            else {CampaignMenuUI.Background(toolbar,new Color(.16f,.11f,.07f,.96f));LegacyUI.Panel(toolbar);}
            for(int i=0;i<Projects.Length;i++)
            {
                int index=i;
                // Slot bounds measured in the original 2172 × 724 artwork. The gaps are not uniform.
                var source=new Rect(new[]{267f,823f,1379f}[i],231f,528f,265f);
                var crop=new Rect(2172*.014f,724*.24f,2172*.97f,724*.52f);
                var size=new Vector2(source.width/crop.width*850,source.height/crop.height*185);
                var position=new Vector2((source.x-crop.x)/crop.width*850,(source.center.y-crop.center.y)/crop.height*185);
                var button=CampaignMenuUI.Button(Names[i],toolbar,new Vector2(0,.5f),position,size,"",()=>SelectProject(index));
                var background=button.GetComponent<Image>();background.sprite=null;background.color=new Color(0,0,0,.01f);
                button.transition=Selectable.Transition.ColorTint;
                var colors=button.colors;colors.normalColor=colors.selectedColor=new Color(1,1,1,.01f);colors.highlightedColor=new Color(1,1,1,.08f);colors.pressedColor=new Color(1,1,1,.15f);button.colors=colors;
                var oldOutline=button.GetComponent<Outline>();if(oldOutline!=null)oldOutline.enabled=false;
                var selectionRect=CampaignMenuUI.Rect("Selección",button.transform,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*2);
                selectionRect.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
                selectionRect.gameObject.SetActive(i==selected);selectionBorders.Add(selectionRect.gameObject);
                var texture=Resources.Load<Texture2D>("CityMap/"+Projects[i]+"_clean")??Resources.Load<Texture2D>("CityMap/"+Projects[i]);
                if(texture!=null)
                {
                    var imageRect=CampaignMenuUI.Rect("Imagen",button.transform,new Vector2(.5f,.5f),new Vector2(0,14),new Vector2(180,110));
                    var image=imageRect.gameObject.AddComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;
                    image.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));ownedSprites.Add(image.sprite);
                }
                var label=CampaignMenuUI.Text("Nombre",button.transform,new Vector2(.5f,0),new Vector2(0,16),new Vector2(170,26),Names[i],17,CampaignMenuUI.Paper);label.raycastTarget=false;
            }
            BackButton=CampaignMenuUI.Button("Volver al mapa",canvas,new Vector2(0,1),new Vector2(20,-200),new Vector2(185,44),"VOLVER AL MAPA",Close);
            backArea=(RectTransform)BackButton.transform;
            leftColumn=CampaignMenuUI.Rect("Columna de ciudad",canvas,new Vector2(0,1),new Vector2(20,-260),new Vector2(310,566));
            queue=Panel("Cola",new Vector2(0,1),Vector2.zero,new Vector2(310,260),false,leftColumn);
            CampaignMenuUI.Text("Title",queue,new Vector2(.5f,1),new Vector2(0,-17),new Vector2(274,28),"OBRAS DE LA CIUDAD",18,CampaignMenuUI.Gold);
            Rule(queue,44);
            queueText=CampaignMenuUI.Text("Cola texto",queue,new Vector2(0,1),new Vector2(22,-64),new Vector2(264,90),"",16,CampaignMenuUI.Paper);queueText.alignment=TextAnchor.UpperLeft;
            for(int i=0;i<6;i++)
            {
                float y=-54-i*29;
                workNames[i]=CampaignMenuUI.Text("Nombre de obra",queue,new Vector2(0,1),new Vector2(22,y),new Vector2(198,25),"",16,CampaignMenuUI.Paper);workNames[i].alignment=TextAnchor.MiddleLeft;
                workClocks[i]=FulgorIcons.Create("Plazo",queue,FulgorIcon.Turn,new Vector2(226,y-3),18);
                workTurns[i]=CampaignMenuUI.Text("Turnos de obra",queue,new Vector2(0,1),new Vector2(249,y),new Vector2(40,25),"",16,CampaignMenuUI.Gold);workTurns[i].alignment=TextAnchor.MiddleRight;
            }
            CampaignMenuUI.Text("Ayuda de cola",queue,new Vector2(.5f,0),new Vector2(0,14),new Vector2(274,22),"Plazo total · una obra activa",12,new Color(.74f,.65f,.48f));
            cityDetails=Panel("Datos de la ciudad",new Vector2(0,1),new Vector2(0,-276),new Vector2(310,290),false,leftColumn);
            var cityTitle=CampaignMenuUI.Text("Title",cityDetails,new Vector2(.5f,1),new Vector2(0,-17),new Vector2(274,28),city.Name,18,CampaignMenuUI.Gold);
            Rule(cityDetails,44);
            string[] cityLabels={"Población", "Territorio", "Suelo disponible", "Edificios terminados"};
            for(int i=0;i<4;i++)
            {
                float y=-55-i*29;
                var label=CampaignMenuUI.Text(cityLabels[i],cityDetails,new Vector2(0,1),new Vector2(22,y),new Vector2(158,25),cityLabels[i],15,new Color(.78f,.72f,.60f));label.alignment=TextAnchor.MiddleLeft;
                cityValues[i]=CampaignMenuUI.Text("Valor "+i,cityDetails,new Vector2(0,1),new Vector2(180,y),new Vector2(108,25),"",15,CampaignMenuUI.Paper);cityValues[i].alignment=TextAnchor.MiddleRight;
            }
            Rule(cityDetails,183);
            CampaignMenuUI.Text("Producción título",cityDetails,new Vector2(.5f,1),new Vector2(0,-193),new Vector2(274,25),"MEJORAS · APORTE POR TURNO",12,CampaignMenuUI.Gold);
            for(int i=0;i<5;i++)
            {
                FulgorIcons.Create("Producción recurso",cityDetails,FulgorIcons.Resources5[i],new Vector2(23+i*54,-228),25);
                cityProduction[i]=CampaignMenuUI.Text("Producción "+i,cityDetails,new Vector2(0,1),new Vector2(18+i*54,-257),new Vector2(36,22),"0",15,CampaignMenuUI.Paper);
            }
            legend=Panel("Beneficios",new Vector2(1,.5f),new Vector2(-20,0),new Vector2(400,400),true);
            legendTitle=CampaignMenuUI.Text("Detalle título",legend,new Vector2(.5f,1),new Vector2(0,-30),new Vector2(330,50),"",20,new Color(.19f,.13f,.07f));
            string[] rowNames={"Terreno · ya", "Edificio · nuevo", "Total / turno", "Coste inicial"};
            for(int row=0;row<4;row++)
            {
                float y=-100-row*48;
                var label=CampaignMenuUI.Text("Rendimiento",legend,new Vector2(0,1),new Vector2(32,y),new Vector2(115,28),rowNames[row],15,new Color(.2f,.13f,.06f));label.alignment=TextAnchor.MiddleLeft;
                for(int col=0;col<5;col++)
                {
                    resourceIcons[row,col]=FulgorIcons.Create("Recurso",legend,FulgorIcons.Resources5[col],new Vector2(158+col*40,y),24);
                    resourceValues[row,col]=CampaignMenuUI.Text("Cantidad",legend,new Vector2(0,1),new Vector2(183+col*40,y),new Vector2(25,25),"",16,new Color(.2f,.13f,.06f));
                }
            }
            description=CampaignMenuUI.Text("Estado",legend,new Vector2(.5f,0),new Vector2(0,28),new Vector2(330,76),"",15,new Color(.19f,.13f,.07f));description.alignment=TextAnchor.MiddleLeft;
        }
        private void SelectProject(int index)
        {
            selected=index;
            for(int i=0;i<selectionBorders.Count;i++)if(selectionBorders[i]!=null)selectionBorders[i].SetActive(i==index);
            Inspect(null);
        }

        private void Rule(RectTransform parent,float y)
        {
            var line=CampaignMenuUI.Rect("Separador",parent,new Vector2(.5f,1),new Vector2(0,-y),new Vector2(parent.sizeDelta.x-40,1));
            var image=line.gameObject.AddComponent<Image>();image.color=new Color(.62f,.46f,.23f,.55f);image.raycastTarget=false;
        }
        private static void AddClip(RectTransform panel,float corner,float leftSlant=0)
        {
            var shape=panel.gameObject.AddComponent<CityPanelClip>();shape.Corner=corner;shape.LeftSlant=leftSlant;shape.raycastTarget=false;
            panel.gameObject.AddComponent<Mask>().showMaskGraphic=false;
        }
        private RectTransform Panel(string name,Vector2 anchor,Vector2 position,Vector2 size,bool paper,Transform parent=null)
        {
            var rect=CampaignMenuUI.Rect(name,parent!=null?parent:canvas,anchor,position,size);
            AddClip(rect,18);
            var surface=CampaignMenuUI.Rect("Fondo",rect,new Vector2(.5f,.5f),Vector2.zero,paper?size-new Vector2(44,44):size);
            var background=surface.gameObject.AddComponent<Image>();background.color=Color.white;background.raycastTarget=false;
            var texture=Resources.Load<Texture2D>(paper?"CityMap/panel_reference":"CityMap/panel_3");
            if(texture!=null)
            {
                var region=paper?new Rect(texture.width*.71f,texture.height*.53f,texture.width*.13f,texture.height*.09f):new Rect(texture.width*267f/2172,texture.height*231f/724,texture.width*528f/2172,texture.height*265f/724);
                background.sprite=Sprite.Create(texture,region,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,paper?Vector4.zero:new Vector4(22,22,22,22));ownedSprites.Add(background.sprite);
                background.type=paper?Image.Type.Simple:Image.Type.Sliced;
                if(paper)
                {
                    var border=CampaignMenuUI.Rect("Marco de referencia",rect,new Vector2(.5f,.5f),Vector2.zero,size);
                    var image=border.gameObject.AddComponent<Image>();image.raycastTarget=false;
                    image.sprite=Sprite.Create(texture,new Rect(texture.width*.085f,texture.height*.125f,texture.width*.84f,texture.height*.77f),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(66,66,66,66));ownedSprites.Add(image.sprite);image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2.2f;image.fillCenter=false;
                }
            }
            else background.color=paper?new Color(.8f,.7f,.51f):new Color(.10f,.075f,.05f,.97f);
            return rect;
        }

        public bool BlocksPointer(Vector2 position)
        {
            if(!IsOpen)return false;
            return RectTransformUtility.RectangleContainsScreenPoint(toolbar,position)||RectTransformUtility.RectangleContainsScreenPoint(heading,position)||RectTransformUtility.RectangleContainsScreenPoint(backArea,position)||
                RectTransformUtility.RectangleContainsScreenPoint(legend,position)||RectTransformUtility.RectangleContainsScreenPoint(queue,position)||RectTransformUtility.RectangleContainsScreenPoint(cityDetails,position)||RectTransformUtility.RectangleContainsScreenPoint(treasury,position);
        }

        public void Inspect(HexTileView tile)
        {
            if(!IsOpen||description==null)return;
            if(tile==null){legendTitle.text=Names[selected];description.text="Señala una casilla del primer anillo.";for(int i=0;i<4;i++)SetRow(i,default);return;}
            bool occupied=campaign.IsCityTileOccupied(tile.Cell.Coordinates);
            bool valid=campaign.IsTileAssignedToCity(city.Id,tile.Cell.Coordinates)&&CityTileConstruction.CanPlace(city.Position,tile.Cell,Projects[selected],occupied);
            var passive=CityTileConstruction.Passive(tile.Cell);var active=valid?CityTileConstruction.Active(Projects[selected]):default;
            var cost=CityTileConstruction.Cost(Projects[selected]);
            string terrain=tile.Cell.Terrain switch {TerrainType.Plain=>"Llanura",TerrainType.Forest=>"Bosque",TerrainType.Hill=>"Colina",TerrainType.Mountain=>"Montaña",_=>"Agua"};
            legendTitle.text=Names[selected]+"  ·  "+tile.Cell.Coordinates.Key+"\n"+terrain;
            SetRow(0,passive);SetRow(1,active);SetRow(2,passive.Add(active));SetRow(3,cost);
            var development=campaign.TileCity(city.Id);var existing=development?.BuildingAt(tile.Cell.Coordinates);
            bool pending=false;if(development!=null)foreach(var order in development.Pending)if(order.q==tile.Cell.Coordinates.Q&&order.r==tile.Cell.Coordinates.R){pending=true;break;}
            bool affordable=campaign.State.Resources.TrySpend(cost,out _);
            description.text=(occupied?(pending?"CASILLA OCUPADA · clic derecho cancela la obra":existing!=null?"CASILLA OCUPADA · edificio terminado":"CASILLA OCUPADA · construcción existente"):!valid?"TERRENO NO VÁLIDO":!affordable?"FALTAN RECURSOS · finaliza turno para recibir ingresos":"TERRENO VÁLIDO · clic para construir")+"\n2 turnos"+(selected==0?" · capacidad de población pendiente":"");
            description.color=occupied?new Color(.45f,.28f,.04f):valid&&affordable?new Color(.14f,.34f,.12f):new Color(.55f,.1f,.08f);
        }
        private void SetRow(int row,CampaignResources values)
        {
            var amounts=new[]{values.Grain,values.Wood,values.Stone,values.Gold,values.Labor};
            int slot=0;
            for(int i=0;i<amounts.Length;i++)
            {
                if(amounts[i]<=0)continue;
                var image=resourceIcons[row,slot];image.sprite=FulgorIcons.Get(FulgorIcons.Resources5[i]);image.enabled=image.sprite!=null;
                resourceValues[row,slot].text=amounts[i].ToString();slot++;
            }
            if(slot==0){resourceValues[row,0].text="—";slot=1;resourceIcons[row,0].enabled=false;}
            for(int i=slot;i<amounts.Length;i++){resourceIcons[row,i].enabled=false;resourceValues[row,i].text="";}
        }
        public bool Place(HexTileView tile)
        {
            if(!IsOpen||tile==null)return false;
            bool placed=campaign.TryPlaceCityTile(city.Id,tile.Cell,Projects[selected]);
            Refresh();Inspect(tile);if(!placed&&description!=null)description.text="No se puede construir: comprueba recursos, terreno y ocupación.";
            return placed;
        }
        public void Cancel(HexTileView tile){if(IsOpen&&tile!=null){campaign.CancelCityTile(city.Id,tile.Cell.Coordinates);Refresh();Inspect(tile);}}

        private void Update()
        {
            if(!IsOpen)return;
            if(campaign==null||campaign.HasPendingBattle||!GetComponent<CampaignSettlementController>().CanManage(city.Id)){Close();return;}
            if(revision!=campaign.TileCityRevision)Refresh();
            var stock=campaign.State.Resources;var income=campaign.State.IncomePerTurn;var expense=campaign.State.ExpensesPerTurn;
            var amounts=new[]{stock.Grain,stock.Wood,stock.Stone,stock.Gold,stock.Labor};
            var gains=new[]{income.Grain-expense.Grain,income.Wood-expense.Wood,income.Stone-expense.Stone,income.Gold-expense.Gold,income.Labor-expense.Labor};
            for(int i=0;i<5;i++){treasuryValues[i].text=amounts[i].ToString();treasuryIncome[i].text=(gains[i]>=0?"+":"")+gains[i]+" al cierre";}
            float space=canvas.rect.height-260-toolbar.sizeDelta.y-32;
            leftColumn.localScale=Vector3.one*Mathf.Min(1f,Mathf.Max(.55f,space/566f));
            var camera=Camera.main;if(camera==null)return;
            foreach(var marker in markers)
            {
                if(marker.tile==null||marker.rect==null)continue;
                var screen=camera.WorldToScreenPoint(marker.tile.transform.position+Vector3.up*.35f);
                marker.rect.gameObject.SetActive(screen.z>0);
                if(screen.z>0&&RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out var local))marker.rect.anchoredPosition=local;
            }
            float pulse=.88f+.12f*Mathf.Sin(Time.unscaledTime*2f);
            foreach(var ring in rings)if(ring!=null)ring.localScale=Vector3.one*pulse;
        }

        private void Refresh()
        {
            if(!IsOpen)return;revision=campaign.TileCityRevision;
            foreach(var obj in markerObjects)if(obj!=null)Destroy(obj);markerObjects.Clear();markers.Clear();rings.Clear();
            var state=campaign.TileCity(city.Id);var pending=state?.Pending;
            queueText.text=pending==null||pending.Length==0?"Sin obras pendientes.\nElige un edificio y una casilla.":"";
            int elapsed=0;
            for(int i=0;i<workNames.Length;i++)
            {
                bool show=pending!=null&&i<pending.Length;
                workNames[i].gameObject.SetActive(show);workTurns[i].gameObject.SetActive(show);workClocks[i].gameObject.SetActive(show);
                if(!show)continue;
                elapsed+=pending[i].turnsRemaining;
                workNames[i].text=(i+1)+". "+Display(pending[i].project);workNames[i].color=i==0?CampaignMenuUI.Gold:CampaignMenuUI.Paper;
                workTurns[i].text=elapsed+" t";
            }
            int territory=1,available=0,finished=0;
            var buildings=state?.Capture(city.Id);
            if(buildings!=null)finished=buildings.completed.Length;
            foreach(var tile in GetComponentsInChildren<HexTileView>())
            {
                if(!campaign.IsTileAssignedToCity(city.Id,tile.Cell.Coordinates))continue;
                territory++;
                bool occupied=campaign.IsCityTileOccupied(tile.Cell.Coordinates);
                if(GetComponent<CampaignTileBuildings>()?.Occupies(tile.Cell.Coordinates)==true)finished++;
                if(!occupied)foreach(var project in Projects)if(CityTileConstruction.CanPlace(city.Position,tile.Cell,project,false)){available++;break;}
            }
            cityValues[0].text="Sin censo";cityValues[1].text=territory+" casillas";cityValues[2].text=available+" casillas";cityValues[3].text=finished.ToString();
            var income=state?.ActiveIncome??default;
            var amounts=new[]{income.Grain,income.Wood,income.Stone,income.Gold,income.Labor};
            for(int i=0;i<amounts.Length;i++)cityProduction[i].text="+"+amounts[i];
            EnsureRingSprite();
            foreach(var tile in GetComponentsInChildren<HexTileView>())
                if(tile.Cell.Coordinates.DistanceTo(city.Position)==1&&tile.Cell.Terrain!=TerrainType.Mountain&&tile.Cell.Terrain!=TerrainType.Water&&campaign.IsTileAssignedToCity(city.Id,tile.Cell.Coordinates)&&!campaign.IsCityTileOccupied(tile.Cell.Coordinates))AddRing(tile);
            if(state==null)return;
            var snapshot=state.Capture(city.Id);
            foreach(var built in snapshot.completed)AddMarker(built,0);
            for(int i=0;i<snapshot.pending.Length;i++)AddMarker(snapshot.pending[i],i+1);
        }
        private static string Display(string id){for(int i=0;i<Projects.Length;i++)if(Projects[i]==id)return Names[i];return id;}
        private void EnsureRingSprite()
        {
            if(ringSprite==null)
            {
                ringTexture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Aros de selección"};
                var pixels=new Color32[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float distance=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
                    byte alpha=(byte)(Mathf.Abs(distance-25)<1.1f||Mathf.Abs(distance-17)<1.1f?220:0);
                    pixels[y*64+x]=new Color32(255,224,155,alpha);
                }
                ringTexture.SetPixels32(pixels);ringTexture.Apply();
                ringSprite=Sprite.Create(ringTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
            }
        }
        private void AddRing(HexTileView tile)
        {
            EnsureRingSprite();
            var rect=CampaignMenuUI.Rect("Casilla disponible",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(48,48));
            var image=rect.gameObject.AddComponent<Image>();image.sprite=ringSprite;image.raycastTarget=false;
            markerObjects.Add(rect.gameObject);markers.Add((rect,tile));rings.Add(rect);
        }
        private void AddMarker(CityTileOrderData data,int number)
        {
            HexTileView tile=null;foreach(var candidate in GetComponentsInChildren<HexTileView>())if(candidate.Cell.Coordinates==new HexCoordinates(data.q,data.r)){tile=candidate;break;}
            if(tile==null)return;
            var rect=CampaignMenuUI.Rect("Obra "+data.q+","+data.r,canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(64,64));
            var image=rect.gameObject.AddComponent<Image>();image.raycastTarget=false;image.color=new Color(.14f,.09f,.05f,.94f);image.sprite=FulgorIcons.ChipFrame;image.type=Image.Type.Sliced;
            int index=System.Array.IndexOf(Projects,data.project);if(index<0)index=0;
            var icon=FulgorIcons.Create("Icono",rect,Icons[index],new Vector2(12,-12),40);icon.raycastTarget=false;
            if(number>0)
            {
                var badge=CampaignMenuUI.Rect("Número de cola",rect,new Vector2(1,1),new Vector2(5,5),new Vector2(28,28));
                var badgeImage=badge.gameObject.AddComponent<Image>();badgeImage.raycastTarget=false;badgeImage.sprite=ringSprite;
                CampaignMenuUI.Text("Número",badge,new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24),number.ToString(),17,CampaignMenuUI.Gold);
            }
            markerObjects.Add(rect.gameObject);markers.Add((rect,tile));
        }
        public void Close()
        {
            if(root==null)return;closedFrame=Time.frameCount;
            GetComponent<CampaignArmyMarker>()?.SetCityVisible(true);GetComponent<CampaignArmyMarkerLayer>()?.SetCityVisible(true);
            GetComponent<CampaignSelectionPanel>()?.SetCityMapMode(false);GetComponent<CampaignSettlementController>()?.SetCityMapMode(false);
            Destroy(root);root=null;canvas=null;backArea=null;BackButton=null;markers.Clear();markerObjects.Clear();
            foreach(var sprite in ownedSprites)if(sprite!=null)Destroy(sprite);ownedSprites.Clear();selectionBorders.Clear();city=null;
            if(ringSprite!=null)Destroy(ringSprite);if(ringTexture!=null)Destroy(ringTexture);ringSprite=null;ringTexture=null;rings.Clear();
        }
        private void OnDisable(){Close();}
    }
}
