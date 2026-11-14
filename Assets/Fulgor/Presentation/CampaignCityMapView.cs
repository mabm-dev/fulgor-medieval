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
        private static readonly string[] Projects = { "house", "farm", "lumber", "market_stall", "militia_camp", "knowledge_house", "simple_quarry" };
        private static readonly string[] Names = { "Vivienda", "Huerto", "Leñera", "Puesto de intercambio", "Campo de milicias", "Casa de conocimiento", "Cantera sencilla" };
        private static readonly FulgorIcon[] Icons = { FulgorIcon.Population, FulgorIcon.Grain, FulgorIcon.Wood };
        private CampaignTurnController campaign;
        private CampaignSettlement city;
        private GameObject root;
        private RectTransform canvas, toolbar, legend, queue, heading, backArea, recruitArea, leftColumn, cityDetails, treasury, markerLayer;
        private readonly Text[] treasuryValues=new Text[5], treasuryIncome=new Text[5];
        private readonly Text[] workNames=new Text[6], workTurns=new Text[6], cityValues=new Text[7], cityProduction=new Text[5];
        private readonly Image[] workClocks=new Image[6];
        private Text description, queueText, legendTitle, legendTerrain;
        private Image legendIcon;
        private readonly Graphic[,] resourceIcons = new Graphic[4,5];
        private readonly Text[,] resourceValues = new Text[4,5];
        private readonly List<(RectTransform rect, HexTileView tile)> markers = new();
        private readonly List<GameObject> markerObjects = new();
        private readonly List<Sprite> ownedSprites = new();
        private readonly GameObject[] selectionBorders = new GameObject[Projects.Length];
        private readonly List<RectTransform> rings = new();
        private Texture2D ringTexture;
        private Sprite ringSprite, turnBadgeSprite;
        private Texture2D turnBadgeTexture;
        private readonly Sprite[] buildingIcons = new Sprite[Projects.Length];
        // minas sobre yacimiento: no ocupan hueco en la barra; una casilla con yacimiento ofrece directamente la suya
        private readonly Dictionary<string, Sprite> mineIcons = new();
        private static string MineName(string project) => project == "goldmine" ? "Mina de oro" : project == "quarry" ? "Cantera" : null;
        private string ProjectFor(HexTileView tile) =>
            (tile != null ? CityTileConstruction.MineFor(campaign.DepositAt(tile.Cell.Coordinates)) : null) ?? Projects[selected];
        private bool InReach(HexTileView tile, string project) =>
            campaign.IsTileAssignedToCity(city.Id, tile.Cell.Coordinates, CityTileConstruction.ReachOf(project));
        private Sprite IconFor(string project)
        {
            int index = System.Array.IndexOf(Projects, project);
            if (index >= 0) return buildingIcons[index];
            if (mineIcons.TryGetValue(project, out var sprite)) return sprite;
            var texture = Resources.Load<Texture2D>("CityMap/" + project);
            sprite = texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f)) : null;
            if (sprite != null) ownedSprites.Add(sprite);
            mineIcons[project] = sprite;
            return sprite;
        }
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
            // los círculos de casilla libre y los marcadores de obra van en la primera capa: los paneles se dibujan siempre encima
            markerLayer=CampaignHudArt.Stretch("Marcadores sobre el mapa",canvas);markerLayer.SetAsFirstSibling();
            heading=GetComponent<CampaignSelectionPanel>()?.KingdomHeader;
            if(heading==null)heading=CampaignHudArt.KingdomBanner(canvas);
            treasury=CampaignMenuUI.Rect("Reservas del reino",canvas,new Vector2(.5f,1),new Vector2(90,-12),new Vector2(960,80));
            var resourceTexture=Resources.Load<Texture2D>("CityMap/resources_reference");
            if(resourceTexture!=null)
            {
                // Keep the engraved ends at the artwork's aspect ratio. The middle samples
                // only bare wood and rails, leaving the five live counters their own space.
                TreasurySlice("Remate izquierdo",resourceTexture,140,378,0,90);
                TreasurySlice("Madera y ribetes",resourceTexture,1910,2040,90,820);
                TreasurySlice("Remate derecho",resourceTexture,2040,2170,910,50);
            }
            for(int i=0;i<5;i++)
            {
                float x=96+i*165;
                if(i>0)
                {
                    var divider=CampaignMenuUI.Rect("Separador "+i,treasury,new Vector2(0,1),new Vector2(x-10,-17),new Vector2(1,45));
                    var line=divider.gameObject.AddComponent<Image>();
                    line.color=new Color(.69f,.48f,.26f,.56f);line.raycastTarget=false;
                }
                CampaignHudArt.Resource(treasury,i,new Vector2(x,-18),42);
                treasuryValues[i]=CampaignMenuUI.Text("Reserva "+i,treasury,new Vector2(0,1),new Vector2(x+48,-14),new Vector2(105,28),"",22,CampaignMenuUI.Paper);treasuryValues[i].alignment=TextAnchor.MiddleLeft;
                treasuryIncome[i]=CampaignMenuUI.Text("Ingreso "+i,treasury,new Vector2(0,1),new Vector2(x+48,-43),new Vector2(105,22),"",15,new Color(.86f,.91f,.76f));treasuryIncome[i].alignment=TextAnchor.MiddleLeft;
            }
            // La ciudad comparte la distribución del mapa: la barra superior del reino sigue a la vista (las reservas propias se ocultan),
            // la ficha de la ciudad y su cola de obras van en la columna derecha, la casilla señalada a la izquierda y los edificios abajo.
            if(GetComponent<CampaignSelectionPanel>()?.HasCampaignHud==true){treasury.gameObject.SetActive(false);treasury.sizeDelta=Vector2.zero;}
            toolbar=CampaignMenuUI.Rect("Edificios",canvas,new Vector2(.5f,0),new Vector2(-165,12),new Vector2(850,185));
            CampaignMenuUI.Background(toolbar,new Color(.16f,.11f,.07f,.96f));
            LegacyUI.Panel(toolbar);
            if(!campaign.IsProjectUnlocked(Projects[selected]))selected=1;
            float step=820f/System.Array.FindAll(Projects,campaign.IsProjectUnlocked).Length;
            int column=0;
            for(int i=0;i<Projects.Length;i++)
            {
                if(!campaign.IsProjectUnlocked(Projects[i]))continue;
                int index=i;
                var size=new Vector2(step-8,140);
                var position=new Vector2(15+step*column++,0);
                var button=CampaignMenuUI.Button(Names[i],toolbar,new Vector2(0,.5f),position,size,"",()=>SelectProject(index));
                var background=button.GetComponent<Image>();background.sprite=null;background.color=new Color(0,0,0,.01f);
                {
                    var frame=CampaignHudArt.Stretch("Marco de obra",button.transform);
                    frame.SetAsFirstSibling();
                    CampaignHudArt.Surface(frame);
                }
                CampaignHudArt.AddButtonWash(button,(RectTransform)button.transform,5);
                var oldOutline=button.GetComponent<Outline>();if(oldOutline!=null)oldOutline.enabled=false;
                var selectionRect=CampaignMenuUI.Rect("Selección",button.transform,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*2);
                selectionRect.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
                selectionRect.gameObject.SetActive(i==selected);selectionBorders[i]=selectionRect.gameObject;
                var texture=Resources.Load<Texture2D>("CityMap/"+Projects[i]+"_clean")??Resources.Load<Texture2D>("CityMap/"+Projects[i]);
                if(texture!=null)
                {
                    var imageRect=CampaignMenuUI.Rect("Imagen",button.transform,new Vector2(.5f,.5f),new Vector2(0,14),new Vector2(size.x-10,110));
                    var image=imageRect.gameObject.AddComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;
                    image.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));ownedSprites.Add(image.sprite);
                    buildingIcons[i] = image.sprite;
                }
                if(texture==null&&Projects[i]=="simple_quarry") // sin ilustración, el icono de piedra
                {
                    var stone=CampaignMenuUI.Rect("Piedra",button.transform,new Vector2(.5f,.5f),new Vector2(0,14),new Vector2(85,70)).gameObject.AddComponent<Image>();
                    stone.sprite=CampaignHudArt.Slice("stone-project","CityMap/resources_reference",new Rect(1178f/2172,322f/724,129f/2172,109f/724));
                    stone.preserveAspect=true;stone.raycastTarget=false;buildingIcons[i]=stone.sprite;
                }
                var label=CampaignMenuUI.Text("Nombre",button.transform,new Vector2(.5f,0),new Vector2(0,8),new Vector2(size.x-8,40),Names[i],16,CampaignMenuUI.Paper);label.raycastTarget=false;
            }
            var cityActions=CampaignMenuUI.Rect("Acciones de ciudad",canvas,new Vector2(1,1),new Vector2(-15,-68),new Vector2(310,46));
            BackButton=CampaignHudArt.Button("Volver al mapa",cityActions,Vector2.zero,new Vector2(170,46),"VOLVER AL MAPA",Close);
            BackButton.GetComponentInChildren<Text>().fontSize=14;
            backArea=(RectTransform)BackButton.transform;
            recruitArea=(RectTransform)CampaignHudArt.Button("Reclutar en ciudad",cityActions,new Vector2(178,0),new Vector2(132,46),"RECLUTAR",()=>{
                var location=city.Position;
                var settlements=GetComponent<CampaignSettlementController>();
                Close();
                settlements.OpenRecruitmentAt(location);
            }).transform;
            leftColumn=CampaignMenuUI.Rect("Columna de ciudad",canvas,new Vector2(1,1),new Vector2(-15,-124),new Vector2(310,653));
            queue=Panel("Cola",new Vector2(0,1),new Vector2(0,-385),new Vector2(310,260),false,leftColumn);
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
            CampaignMenuUI.Text("Ayuda de cola",queue,new Vector2(.5f,0),new Vector2(0,14),new Vector2(274,22),"Plazo total · una obra activa",15,new Color(.74f,.65f,.48f));
            cityDetails=Panel("Datos de la ciudad",new Vector2(0,1),Vector2.zero,new Vector2(310,377),false,leftColumn);
            var cityTitle=CampaignMenuUI.Text("Title",cityDetails,new Vector2(.5f,1),new Vector2(0,-17),new Vector2(274,28),city.Name,18,CampaignMenuUI.Gold);
            Rule(cityDetails,44);
            string[] cityLabels={"Población", "Territorio", "Suelo disponible", "Edificios terminados", "Grano local / turno", "Consumo civil", "Crecimiento"};
            for(int i=0;i<cityLabels.Length;i++)
            {
                float y=-55-i*29;
                var label=CampaignMenuUI.Text(cityLabels[i],cityDetails,new Vector2(0,1),new Vector2(22,y),new Vector2(158,25),cityLabels[i],15,new Color(.78f,.72f,.60f));label.alignment=TextAnchor.MiddleLeft;
                cityValues[i]=CampaignMenuUI.Text("Valor "+i,cityDetails,new Vector2(0,1),new Vector2(180,y),new Vector2(108,25),"",15,CampaignMenuUI.Paper);cityValues[i].alignment=TextAnchor.MiddleRight;
            }
            Rule(cityDetails,270);
            CampaignMenuUI.Text("Producción título",cityDetails,new Vector2(.5f,1),new Vector2(0,-280),new Vector2(274,25),"MEJORAS · APORTE POR TURNO",15,CampaignMenuUI.Gold);
            for(int i=0;i<5;i++)
            {
                CampaignHudArt.Resource(cityDetails,i,new Vector2(20+i*54,-312),31);
                cityProduction[i]=CampaignMenuUI.Text("Producción "+i,cityDetails,new Vector2(0,1),new Vector2(18+i*54,-344),new Vector2(36,22),"0",15,CampaignMenuUI.Paper);
            }
            legend=Panel("Beneficios",new Vector2(0,1),new Vector2(10,-92),new Vector2(460,410),true);
            LegendBand("Cabecera",27,60);
            var emblem=CampaignMenuUI.Rect("Edificio seleccionado",legend,new Vector2(0,1),new Vector2(38,-30),new Vector2(54,54));
            legendIcon=emblem.gameObject.AddComponent<Image>();
            legendIcon.preserveAspect=true;
            legendIcon.raycastTarget=false;
            legendTitle=CampaignMenuUI.Text("Detalle título",legend,new Vector2(0,1),new Vector2(104,-30),new Vector2(312,30),"",23,CampaignMenuUI.Paper);
            legendTitle.alignment=TextAnchor.MiddleLeft;
            legendTitle.font=LegacyUI.Heading??legendTitle.font;
            legendTerrain=CampaignMenuUI.Text("Terreno seleccionado",legend,new Vector2(0,1),new Vector2(104,-60),new Vector2(312,23),"",16,CampaignMenuUI.Gold);
            legendTerrain.alignment=TextAnchor.MiddleLeft;
            string[] rowNames={"Terreno", "Edificio", "Total / turno", "Coste inicial"};
            for(int row=0;row<rowNames.Length;row++)
            {
                float y=-92-row*48;
                var strip=CampaignMenuUI.Rect("Banda "+row,legend,new Vector2(0,1),new Vector2(26,y),new Vector2(408,42));
                var surface=strip.gameObject.AddComponent<Image>();
                surface.color=row==2 ? new Color(.23f,.12f,.07f) : new Color(.16f,.12f,.10f,.94f);
                surface.raycastTarget=false;
                var label=CampaignMenuUI.Text("Rendimiento",legend,new Vector2(0,1),new Vector2(38,y-6),new Vector2(115,30),rowNames[row],19,row==2?CampaignMenuUI.Gold:CampaignMenuUI.Paper);
                label.alignment=TextAnchor.MiddleLeft;
                for(int i=0;i<5;i++)
                {
                    resourceIcons[row,i]=CampaignHudArt.Resource(legend,i,new Vector2(151,y-4),34);
                    resourceValues[row,i]=CampaignMenuUI.Text("Cantidad",legend,new Vector2(0,1),new Vector2(185+i*55,y-5),new Vector2(26,32),"",18,Color.white);
                    resourceValues[row,i].alignment=TextAnchor.MiddleLeft;
                }
            }
            Rule(legend,287);
            LegendBand("Aviso de obra",295,88);
            description=CampaignMenuUI.Text("Estado",legend,new Vector2(.5f,0),new Vector2(0,28),new Vector2(370,78),"",18,new Color(.19f,.13f,.07f));description.alignment=TextAnchor.MiddleLeft;
        }
        private void LegendBand(string name,float top,float height)
        {
            var rect=CampaignMenuUI.Rect(name,legend,new Vector2(0,1),new Vector2(26,-top),new Vector2(408,height));
            var surface=rect.gameObject.AddComponent<Image>();
            surface.raycastTarget=false;
            surface.color=new Color(.18f,.115f,.075f,.98f);
        }
        private void SelectProject(int index)
        {
            selected=index;
            for(int i=0;i<selectionBorders.Length;i++)if(selectionBorders[i]!=null)selectionBorders[i].SetActive(i==index);
            Inspect(null);
        }

        private void Rule(RectTransform parent,float y)
        {
            var line=CampaignMenuUI.Rect("Separador",parent,new Vector2(.5f,1),new Vector2(0,-y),new Vector2(parent.sizeDelta.x-40,1));
            var image=line.gameObject.AddComponent<Image>();image.color=new Color(.62f,.46f,.23f,.55f);image.raycastTarget=false;
        }
        private void TreasurySlice(string name,Texture2D texture,int sourceLeft,int sourceRight,float left,float width)
        {
            var rect=CampaignMenuUI.Rect(name,treasury,new Vector2(0,1),new Vector2(left,0),new Vector2(width,80));
            var art=rect.gameObject.AddComponent<RawImage>();
            art.texture=texture;
            // Normalized against the source PNG, independently of Unity's imported resolution.
            art.uvRect=new Rect(sourceLeft/2170f,(725-445)/725f,(sourceRight-sourceLeft)/2170f,212/725f);
            art.raycastTarget=false;
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
            return RectTransformUtility.RectangleContainsScreenPoint(toolbar,position)||RectTransformUtility.RectangleContainsScreenPoint(heading,position)||RectTransformUtility.RectangleContainsScreenPoint(backArea,position)||RectTransformUtility.RectangleContainsScreenPoint(recruitArea,position)||
                RectTransformUtility.RectangleContainsScreenPoint(legend,position)||RectTransformUtility.RectangleContainsScreenPoint(queue,position)||RectTransformUtility.RectangleContainsScreenPoint(cityDetails,position)||RectTransformUtility.RectangleContainsScreenPoint(treasury,position);
        }

        public void Inspect(HexTileView tile)
        {
            if(!IsOpen||description==null)return;
            legendTitle.text=Names[selected];
            legendIcon.sprite=buildingIcons[selected];
            bool unknown=tile!=null && campaign.VisibilityAt(tile.Cell.Coordinates)==CampaignVisibility.Unknown;
            if(tile==null||unknown){legendTerrain.text="";description.color=CampaignMenuUI.Paper;description.text=unknown?"Territorio sin explorar.":"Señala una casilla del territorio de la ciudad.";for(int i=0;i<4;i++)SetRow(i,default);return;}
            bool occupied=campaign.IsCityTileOccupied(tile.Cell.Coordinates);
            var deposit=campaign.DepositAt(tile.Cell.Coordinates);var project=ProjectFor(tile);
            if(MineName(project)!=null){legendTitle.text=MineName(project);legendIcon.sprite=IconFor(project);}
            bool valid=InReach(tile,project)&&campaign.IsProjectUnlocked(project)&&CityTileConstruction.CanPlace(city.Position,tile.Cell,project,occupied,deposit);
            var passive=CityTileConstruction.Passive(tile.Cell,deposit);var active=valid?CityTileConstruction.Active(project,campaign.ResearchEnabled ? campaign.Research : null):default;
            var cost=CityTileConstruction.Cost(project);
            string terrain=tile.Cell.Terrain switch {TerrainType.Plain=>"Llanura",TerrainType.Forest=>"Bosque",TerrainType.Hill=>"Colina",TerrainType.Mountain=>"Montaña",_=>"Agua"};
            if(deposit!=ResourceDepositKind.None)terrain+=deposit==ResourceDepositKind.Gold?" · yacimiento de oro":" · yacimiento de piedra";
            legendTerrain.text=terrain+"  ·  "+tile.Cell.Coordinates.Key;
            SetRow(0,passive);SetRow(1,active);SetRow(2,passive.Add(active));SetRow(3,cost);
            var development=campaign.TileCity(city.Id);var existing=development?.BuildingAt(tile.Cell.Coordinates);
            int wait=0;
            bool pending=false;
            if(development!=null)
                foreach(var order in development.Pending)
                {
                    wait+=order.turnsRemaining;
                    if(order.q!=tile.Cell.Coordinates.Q||order.r!=tile.Cell.Coordinates.R)continue;
                    pending=true;
                    break;
                }
            bool affordable=campaign.State.Resources.TrySpend(cost,out _);
            if(occupied)
            {
                description.color=new Color(1f,.82f,.49f);
                description.text=pending ? $"Obra en cola · {wait} {(wait==1?"turno restante":"turnos restantes")}\nClic derecho para cancelar."
                    : "Casilla ocupada\n"+(existing==null?"Ya hay una construcción aquí.":Display(existing));
            }
            else if(!valid)
            {
                description.color=new Color(1f,.68f,.60f);
                description.text=!InReach(tile,project)
                    ? "Fuera del territorio de esta ciudad."
                    : "Terreno no válido\n"+(MineName(project)!=null?"El yacimiento no admite esta obra.":project switch {
                        "house" => "La vivienda necesita llanura o colina.",
                        "lumber" => "La leñera necesita bosque.",
                        "simple_quarry" => campaign.IsProjectUnlocked(project)?"La cantera sencilla necesita colina.":"La cantera sencilla requiere Herramientas de hierro.",
                        _ => "Esta construcción necesita llanura."
                    });
            }
            else
            {
                description.color=affordable?new Color(.76f,.93f,.67f):new Color(1f,.70f,.56f);
                description.text=affordable?"Terreno válido · obra de 2 turnos\nClic izquierdo para construir.":"Faltan recursos\nConsulta el coste inicial.";
                if(project=="knowledge_house")
                    description.text+="\n+1 sabiduría por turno. Máximo una bonificación por ciudad.";
                if(project=="militia_camp")description.text+="\nLanceros: −4 de mano de obra por leva.";
                if(affordable && project=="house")description.text+="  +10 plazas";
                if(MineName(project)!=null)description.text=(affordable?"Yacimiento · obra de 2 turnos\nClic izquierdo para construir ":"Faltan recursos para ")+MineName(project).ToLowerInvariant()+".";
            }
        }
        private void SetRow(int row,CampaignResources values)
        {
            var amounts=new[]{values.Grain,values.Wood,values.Stone,values.Gold,values.Labor};
            int slot=0;
            for(int i=0;i<amounts.Length;i++)
            {
                var icon=resourceIcons[row,i];
                icon.enabled=amounts[i]>0;
                if(!icon.enabled)continue;
                var position=icon.rectTransform.anchoredPosition;
                icon.rectTransform.anchoredPosition=new Vector2(151+slot*55,position.y);
                resourceValues[row,slot].text=amounts[i].ToString();
                slot++;
            }
            if(slot==0)resourceValues[row,slot++].text="—";
            for(int i=slot;i<amounts.Length;i++)resourceValues[row,i].text="";

        }
        public bool Place(HexTileView tile)
        {
            if(!IsOpen||tile==null)return false;
            bool placed=campaign.TryPlaceCityTile(city.Id,tile.Cell,ProjectFor(tile));
            Refresh();Inspect(tile);
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
            leftColumn.localScale=Vector3.one*Mathf.Min(1f,Mathf.Max(.55f,space/653f));
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
                if(campaign.VisibilityAt(tile.Cell.Coordinates)==CampaignVisibility.Unknown)continue;
                var deposit=campaign.DepositAt(tile.Cell.Coordinates);var mine=CityTileConstruction.MineFor(deposit);
                // el territorio es el primer anillo; en un yacimiento del territorio solo cabe su mina
                bool ring=campaign.IsTileAssignedToCity(city.Id,tile.Cell.Coordinates);
                if(!ring&&(mine==null||!InReach(tile,mine)))continue;
                if(ring)territory++;
                bool occupied=campaign.IsCityTileOccupied(tile.Cell.Coordinates);
                if(ring&&GetComponent<CampaignTileBuildings>()?.Occupies(tile.Cell.Coordinates)==true)finished++;
                if(!occupied&&mine!=null){if(CityTileConstruction.CanPlace(city.Position,tile.Cell,mine,false,deposit))available++;}
                else if(!occupied)foreach(var project in Projects)if(campaign.IsProjectUnlocked(project)&&CityTileConstruction.CanPlace(city.Position,tile.Cell,project,false)){available++;break;}
            }
            var town = state ?? new CityTileDevelopment();
            cityValues[0].text = town.Population + " / " + town.HousingCapacity(campaign.CityState(city.Id).Houses);
            cityValues[4].text = campaign.LocalFood(city.Id).ToString();
            int consumption = CityTileDevelopment.FoodConsumption(town.Population);
            cityValues[5].text = consumption.ToString();
            cityValues[6].text = town.Population >= town.HousingCapacity(campaign.CityState(city.Id).Houses)
                ? "Falta vivienda" : campaign.LocalFood(city.Id) <= consumption ? "Falta excedente" : "+1 hab./turno";
            cityValues[1].text=territory+" casillas";cityValues[2].text=available+" casillas";cityValues[3].text=finished.ToString();
            var income=(state?.Income(campaign.ResearchEnabled ? campaign.Research : null)??default).Add(campaign.ResearchEnabled?campaign.Research.IncomePerCity:default);
            var amounts=new[]{income.Grain,income.Wood,income.Stone,income.Gold,income.Labor};
            for(int i=0;i<amounts.Length;i++)cityProduction[i].text="+"+amounts[i];
            EnsureRingSprite();
            foreach(var tile in GetComponentsInChildren<HexTileView>())
            {
                if(campaign.VisibilityAt(tile.Cell.Coordinates)==CampaignVisibility.Unknown||campaign.IsCityTileOccupied(tile.Cell.Coordinates))continue;
                var mine=CityTileConstruction.MineFor(campaign.DepositAt(tile.Cell.Coordinates));
                if(mine!=null?InReach(tile,mine):tile.Cell.Terrain!=TerrainType.Mountain&&tile.Cell.Terrain!=TerrainType.Water&&campaign.IsTileAssignedToCity(city.Id,tile.Cell.Coordinates))AddRing(tile);
            }
            if(state==null)return;
            var snapshot=state.Capture(city.Id);
            int remaining = 0;
            foreach (var work in snapshot.pending)
            {
                remaining += work.turnsRemaining;
                if(campaign.VisibilityAt(new HexCoordinates(work.q,work.r))!=CampaignVisibility.Unknown) AddMarker(work, remaining);
            }
        }
        private static string Display(string id){for(int i=0;i<Projects.Length;i++)if(Projects[i]==id)return Names[i];return MineName(id)??id;}
        private void EnsureRingSprite()
        {
            if(ringSprite==null)
            {
                ringTexture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Aros de selección"};
                var pixels=new Color32[64*64];
                var badgePixels=new Color32[pixels.Length];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float distance=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
                    badgePixels[y*64+x] = distance < 28 ? new Color32(49,30,17,250) :
                        distance <= 30 ? new Color32(226,180,92,255) : new Color32(0,0,0,0);
                    byte alpha=(byte)(Mathf.Abs(distance-25)<1.1f||Mathf.Abs(distance-17)<1.1f?220:0);
                    pixels[y*64+x]=new Color32(255,224,155,alpha);
                }
                turnBadgeTexture = new Texture2D(64,64,TextureFormat.RGBA32,false);
                turnBadgeTexture.SetPixels32(badgePixels);
                turnBadgeTexture.Apply();
                turnBadgeSprite = Sprite.Create(turnBadgeTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
                ringTexture.SetPixels32(pixels);ringTexture.Apply();
                ringSprite=Sprite.Create(ringTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
            }
        }
        private void AddRing(HexTileView tile)
        {
            EnsureRingSprite();
            var rect=CampaignMenuUI.Rect("Casilla disponible",markerLayer,new Vector2(.5f,.5f),Vector2.zero,new Vector2(48,48));
            var image=rect.gameObject.AddComponent<Image>();image.sprite=ringSprite;image.raycastTarget=false;
            markerObjects.Add(rect.gameObject);markers.Add((rect,tile));rings.Add(rect);
        }
        private void AddMarker(CityTileOrderData data,int number)
        {
            HexTileView tile=null;foreach(var candidate in GetComponentsInChildren<HexTileView>())if(candidate.Cell.Coordinates==new HexCoordinates(data.q,data.r)){tile=candidate;break;}
            if(tile==null)return;
            var rect=CampaignMenuUI.Rect("Obra "+data.q+","+data.r,markerLayer,new Vector2(.5f,.5f),Vector2.zero,new Vector2(64,64));
            var icon = rect.gameObject.AddComponent<Image>();
            icon.sprite = IconFor(data.project);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var badge=CampaignMenuUI.Rect("Turnos restantes",rect,new Vector2(1,1),new Vector2(8,12),new Vector2(29,29));
            var background=badge.gameObject.AddComponent<Image>();
            background.sprite=turnBadgeSprite;
            background.raycastTarget=false;
            var numberText=CampaignMenuUI.Text("Número",badge,new Vector2(.5f,.5f),Vector2.zero,new Vector2(25,25),number.ToString(),17,CampaignMenuUI.Gold);
            numberText.raycastTarget=false;
            markerObjects.Add(rect.gameObject);markers.Add((rect,tile));
        }
        public void Close()
        {
            if(root==null)return;closedFrame=Time.frameCount;
            GetComponent<CampaignArmyMarker>()?.SetCityVisible(true);GetComponent<CampaignArmyMarkerLayer>()?.SetCityVisible(true);
            GetComponent<CampaignSelectionPanel>()?.SetCityMapMode(false);GetComponent<CampaignSettlementController>()?.SetCityMapMode(false);
            Destroy(root);root=null;canvas=null;backArea=null;BackButton=null;markers.Clear();markerObjects.Clear();
            foreach(var sprite in ownedSprites)if(sprite!=null)Destroy(sprite);ownedSprites.Clear();System.Array.Clear(selectionBorders,0,selectionBorders.Length);city=null;
            if(turnBadgeSprite!=null)Destroy(turnBadgeSprite);if(turnBadgeTexture!=null)Destroy(turnBadgeTexture);
            if(ringSprite!=null)Destroy(ringSprite);if(ringTexture!=null)Destroy(ringTexture);ringSprite=null;ringTexture=null;rings.Clear();
        }
        private void OnDisable(){Close();}
    }
}
