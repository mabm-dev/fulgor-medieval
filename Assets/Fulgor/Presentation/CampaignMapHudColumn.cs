using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Columna derecha del mapa de campaña: minimapa y huestes del reino, encima del consejo, los avisos y el cierre de turno.</summary>
    public sealed partial class CampaignSelectionPanel
    {
        private const int ArmySlots=5;
        private RectTransform minimapPanel,armyStrip;
        private CampaignMinimap minimap;
        private readonly Button[] armyButtons=new Button[ArmySlots];
        private readonly Text[] armyLabels=new Text[ArmySlots],armyCounts=new Text[ArmySlots];
        private readonly RawImage[] armyPortraits=new RawImage[ArmySlots];
        private readonly GameObject[] armyRims=new GameObject[ArmySlots];
        private readonly string[] armyIds=new string[ArmySlots];
        private Text armyOverflow;
        private float nextColumnRefresh;
        internal CampaignMinimap Minimap=>minimap;
        internal RectTransform ArmyStrip=>armyStrip;
        /// <summary>Hueste que ocupa cada hueco de la columna (null si está vacío).</summary>
        public IReadOnlyList<string> ColumnArmyIds=>armyIds;
        public Button ColumnArmyButton(int index)=>index>=0&&index<ArmySlots?armyButtons[index]:null;

        private void BuildColumn(Transform canvas)
        {
            minimapPanel=CreateRect("Minimapa",canvas,new Vector2(ColumnWidth,206),new Vector2(-ColumnMargin,-64));
            minimapPanel.anchorMin=minimapPanel.anchorMax=minimapPanel.pivot=Vector2.one;
            CampaignHudArt.Surface(minimapPanel);
            var view=CreateRect("Vista del minimapa",minimapPanel,new Vector2(ColumnWidth-20,186),new Vector2(10,-10));
            minimap=view.gameObject.AddComponent<CampaignMinimap>();minimap.Bind(GetComponent<CampaignTurnController>());

            armyStrip=CreateRect("Huestes del reino",canvas,new Vector2(ColumnWidth,104),new Vector2(-ColumnMargin,-276));
            armyStrip.anchorMin=armyStrip.anchorMax=armyStrip.pivot=Vector2.one;
            CampaignHudArt.Surface(armyStrip);
            for(int i=0;i<ArmySlots;i++)
            {
                int slot=i;
                var button=CampaignHudArt.Button("Hueste "+i,armyStrip,new Vector2(8+i*61,-8),new Vector2(58,88),"",()=>SelectColumnArmy(slot));
                var art=CreateRect("Retrato",button.transform,new Vector2(46,46),new Vector2(6,-6));
                armyPortraits[i]=art.gameObject.AddComponent<KingdomPortraitGraphic>();armyPortraits[i].raycastTarget=false;
                armyLabels[i]=HudText("Sin héroe",button.transform,new Vector2(6,-14),new Vector2(46,30),"",10,true);armyLabels[i].alignment=TextAnchor.MiddleCenter;
                armyCounts[i]=HudText("Tropas",button.transform,new Vector2(4,-58),new Vector2(50,22),"",15,false,CampaignMenuUI.Gold);armyCounts[i].alignment=TextAnchor.MiddleCenter;
                var rim=CampaignHudArt.Stretch("Selección",button.transform,3);rim.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;rim.gameObject.SetActive(false);
                armyButtons[i]=button;armyRims[i]=rim.gameObject;button.gameObject.SetActive(false);
            }
            armyOverflow=HudText("Más huestes",armyStrip,new Vector2(8,-36),new Vector2(304,30),"Sin huestes en el mapa",14,false,new Color(.75f,.67f,.52f));
            armyOverflow.alignment=TextAnchor.MiddleCenter;
        }

        private void SelectColumnArmy(int slot)
        {
            var campaign=GetComponent<CampaignTurnController>();string id=armyIds[slot];
            if(campaign?.ArmyRegistry==null||id==null||!campaign.ArmyRegistry.TryGetArmy(id,out var army))return;
            var position=army.Movement.Position;
            if(campaign.SelectArmyForRoster(id))
            {
                var tile=GetComponentsInChildren<HexTileView>().FirstOrDefault(t=>t.Cell.Coordinates==position);
                if(tile!=null)Camera.main?.GetComponent<CampaignCameraController>()?.FocusOnWorld(tile.transform.position);
            }
            RefreshColumn(campaign,true);
        }

        private void RefreshColumn(CampaignTurnController campaign,bool force=false)
        {
            if(armyStrip==null||!force&&Time.unscaledTime<nextColumnRefresh)return;nextColumnRefresh=Time.unscaledTime+.3f;
            bool show=!cityMode;
            minimapPanel.gameObject.SetActive(show);armyStrip.gameObject.SetActive(show);
            if(!show)return;
            var own=campaign.ArmyRegistry==null?new List<CampaignArmyOnMap>():
                campaign.ArmyRegistry.Armies.Where(a=>a.Army.FactionId==campaign.PlayerFactionId&&!a.Army.IsDissolved).ToList();
            for(int i=0;i<ArmySlots;i++)
            {
                var army=i<own.Count?own[i]:null;armyIds[i]=army?.Army.Id;
                armyButtons[i].gameObject.SetActive(army!=null);
                if(army==null)continue;
                bool hero=army.Army.HeroId!=null;long troops=army.Army.Formations.Sum(f=>(long)f.Quantity);
                armyPortraits[i].enabled=hero;armyLabels[i].gameObject.SetActive(!hero);
                if(hero)
                {
                    var texture=Resources.Load<Texture2D>("KingdomSelection/hero-"+campaign.PlayerFactionId);
                    if(armyPortraits[i].texture!=texture){armyPortraits[i].texture=texture;armyPortraits[i].uvRect=new Rect(.36f,.52f,.28f,.40f);}
                }
                else armyLabels[i].text="HUESTE";
                armyCounts[i].text=troops>0?troops.ToString():"—";
                armyRims[i].SetActive(army.Army.Id==campaign.SelectedArmyId);
                // una hueste con movimiento y sin marcha ordenada se señala en oro; con órdenes, en papel
                bool idle=army.Movement.RemainingMovement>0&&!campaign.MarchFor(army.Army.Id).HasValue;
                armyCounts[i].color=idle?CampaignMenuUI.Gold:CampaignMenuUI.Paper;
            }
            armyOverflow.gameObject.SetActive(own.Count==0||own.Count>ArmySlots);
            if(own.Count>ArmySlots)
            {
                armyOverflow.rectTransform.anchoredPosition=new Vector2(8,-84);armyOverflow.rectTransform.sizeDelta=new Vector2(304,18);armyOverflow.fontSize=11;
                armyOverflow.text="+"+(own.Count-ArmySlots)+" más · OTRA HUESTE";
            }
            else if(own.Count==0)armyOverflow.text="Sin huestes en el mapa";
        }

        private bool ColumnContains(Vector2 point)=>Contains(minimapPanel,point)||Contains(armyStrip,point);
    }

    /// <summary>
    /// Minimapa de la campaña: una textura pequeña con el terreno conocido, los asentamientos y las huestes a la vista. Respeta la niebla
    /// (lo desconocido queda negro y lo explorado, apagado) y un clic lleva la cámara a ese punto.
    /// </summary>
    public sealed class CampaignMinimap:MonoBehaviour,IPointerDownHandler,IDragHandler
    {
        private const int Width=300,Height=186;
        private CampaignTurnController campaign;
        private RawImage image;private Texture2D texture;private Color32[] pixels;
        private RectTransform frame;
        private Vector2 min,max;private float cellRadius;
        private float nextRefresh;
        private readonly List<(HexCoordinates cell,TerrainType terrain,Vector3 world)> cells=new();
        public int KnownCells{get;private set;}
        public Texture2D Texture=>texture;

        private static readonly Color32 Unknown=new(12,13,14,255);
        private static Color32 TerrainColor(TerrainType terrain)=>terrain switch
        {
            TerrainType.Water=>new Color32(46,86,122,255),
            TerrainType.Plain=>new Color32(121,142,74,255),
            TerrainType.Forest=>new Color32(58,94,52,255),
            TerrainType.Hill=>new Color32(146,132,88,255),
            TerrainType.Mountain=>new Color32(150,146,140,255),
            _=>new Color32(90,90,90,255)
        };

        internal void Bind(CampaignTurnController owner)
        {
            campaign=owner;
            image=gameObject.AddComponent<RawImage>();image.raycastTarget=true;
            texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Minimapa de campaña",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            pixels=new Color32[Width*Height];image.texture=texture;
            frame=CampaignMenuUI.Rect("Encuadre de cámara",transform,new Vector2(0,0),Vector2.zero,new Vector2(40,26));
            var outline=frame.gameObject.AddComponent<CitySelectionFrame>();outline.raycastTarget=false;
        }

        private void OnDestroy(){if(texture!=null)Destroy(texture);}

        private void LateUpdate()
        {
            if(campaign==null)campaign=GetComponentInParent<CampaignTurnController>(); // el panel se construye antes de que exista el controlador
            if(campaign==null||Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.5f;
            Redraw();
        }

        /// <summary>Vuelve a leer las casillas del tablero (tras generarlo o cargar partida) y repinta.</summary>
        public void Redraw()
        {
            if(campaign==null)campaign=GetComponentInParent<CampaignTurnController>();
            if(campaign==null||texture==null)return;
            var tiles=campaign.GetComponentsInChildren<HexTileView>();
            if(tiles.Length!=cells.Count||tiles.Length>0&&cells.Count>0&&tiles[0].transform.position!=cells[0].world)
            {
                cells.Clear();foreach(var tile in tiles)cells.Add((tile.Cell.Coordinates,tile.Cell.Terrain,tile.transform.position));
                if(cells.Count>0)
                {
                    min=new Vector2(cells.Min(c=>c.world.x),cells.Min(c=>c.world.z));max=new Vector2(cells.Max(c=>c.world.x),cells.Max(c=>c.world.z));
                    var pad=Vector2.one*VisualScaleContract.HexOuterRadius;min-=pad;max+=pad;
                    cellRadius=VisualScaleContract.HexOuterRadius*Mathf.Min(Width/(max.x-min.x),Height/(max.y-min.y));
                }
            }
            for(int i=0;i<pixels.Length;i++)pixels[i]=Unknown;
            int known=0;
            foreach(var cell in cells)
            {
                var visibility=campaign.VisibilityAt(cell.cell);if(visibility==CampaignVisibility.Unknown)continue;
                known++;var color=TerrainColor(cell.terrain);
                if(visibility==CampaignVisibility.Explored)color=Color32.Lerp(color,Unknown,.45f);
                Disc(ToPixel(cell.world),cellRadius*.98f,color);
            }
            KnownCells=known;
            if(campaign.SettlementState!=null)foreach(var city in campaign.SettlementState.Settlements)
            {
                if(campaign.VisibilityAt(city.Position)==CampaignVisibility.Unknown||!TryWorld(city.Position,out var world))continue;
                bool own=city.OwnerFactionId==campaign.PlayerFactionId;
                Square(ToPixel(world),4,new Color32(20,16,10,255));Square(ToPixel(world),3,own?new Color32(236,196,96,255):new Color32(196,70,56,255));
            }
            if(campaign.ArmyRegistry!=null)foreach(var army in campaign.ArmyRegistry.Armies)
            {
                if(army.Army.IsDissolved||!campaign.CanObserve(army.Movement.Position)||!TryWorld(army.Movement.Position,out var world))continue;
                bool own=army.Army.FactionId==campaign.PlayerFactionId;bool selected=army.Army.Id==campaign.SelectedArmyId;
                Disc(ToPixel(world),selected?3.6f:2.6f,new Color32(16,12,8,255));
                Disc(ToPixel(world),selected?2.6f:1.7f,own?new Color32(250,244,226,255):new Color32(226,82,62,255));
            }
            texture.SetPixels32(pixels);texture.Apply(false);
            PlaceFrame();
        }

        private bool TryWorld(HexCoordinates position,out Vector3 world)
        {
            foreach(var cell in cells)if(cell.cell==position){world=cell.world;return true;}
            world=default;return false;
        }
        private Vector2 ToPixel(Vector3 world)=>new((world.x-min.x)/(max.x-min.x)*(Width-1),(world.z-min.y)/(max.y-min.y)*(Height-1));
        private void Disc(Vector2 centre,float radius,Color32 color)
        {
            int x0=Mathf.Max(0,Mathf.FloorToInt(centre.x-radius)),x1=Mathf.Min(Width-1,Mathf.CeilToInt(centre.x+radius));
            int y0=Mathf.Max(0,Mathf.FloorToInt(centre.y-radius)),y1=Mathf.Min(Height-1,Mathf.CeilToInt(centre.y+radius));
            float limit=radius*radius;
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                if((x-centre.x)*(x-centre.x)+(y-centre.y)*(y-centre.y)<=limit)pixels[y*Width+x]=color;
        }
        private void Square(Vector2 centre,int half,Color32 color)
        {
            int cx=Mathf.RoundToInt(centre.x),cy=Mathf.RoundToInt(centre.y);
            for(int y=Mathf.Max(0,cy-half);y<=Mathf.Min(Height-1,cy+half);y++)for(int x=Mathf.Max(0,cx-half);x<=Mathf.Min(Width-1,cx+half);x++)pixels[y*Width+x]=color;
        }

        // el encuadre marca dónde mira la cámara: el punto del suelo en el centro de la pantalla
        private void PlaceFrame()
        {
            var camera=Camera.main;if(camera==null||cells.Count==0||frame==null){if(frame!=null)frame.gameObject.SetActive(false);return;}
            var ray=camera.ViewportPointToRay(new Vector3(.5f,.5f));
            if(Mathf.Abs(ray.direction.y)<1e-4f){frame.gameObject.SetActive(false);return;}
            var ground=ray.origin+ray.direction*(-ray.origin.y/ray.direction.y);
            var rect=(RectTransform)transform;var pixel=ToPixel(ground);
            frame.gameObject.SetActive(true);
            frame.anchoredPosition=new Vector2(Mathf.Clamp(pixel.x/(Width-1),0,1)*rect.rect.width,Mathf.Clamp(pixel.y/(Height-1),0,1)*rect.rect.height)-frame.sizeDelta*.5f;
        }

        public void OnPointerDown(PointerEventData data)=>Focus(data);
        public void OnDrag(PointerEventData data)=>Focus(data);
        private void Focus(PointerEventData data)
        {
            if(cells.Count==0||!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,data.position,data.pressEventCamera,out var local))return;
            var rect=((RectTransform)transform).rect;
            FocusOn(new Vector2(Mathf.InverseLerp(rect.xMin,rect.xMax,local.x),Mathf.InverseLerp(rect.yMin,rect.yMax,local.y)));
        }
        /// <summary>Lleva la cámara al punto del mapa que corresponde a una posición del minimapa (0..1 en cada eje).</summary>
        public void FocusOn(Vector2 normalized)
        {
            if(cells.Count==0)return;
            var world=new Vector3(Mathf.Lerp(min.x,max.x,normalized.x),0,Mathf.Lerp(min.y,max.y,normalized.y));
            Camera.main?.GetComponent<CampaignCameraController>()?.FocusOnWorld(world);
            nextRefresh=0;
        }
    }
}
