using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Maps a castle hit back to its logical tile and shows its selected footprint.</summary>
    public sealed class CampaignSettlementView : MonoBehaviour
    {
        private LineRenderer outline;
        private Material material;
        private BoxCollider pickCollider;
        public HexTileView Tile { get; private set; }
        public Vector3 PresenceAnchor=>pickCollider!=null?new Vector3(pickCollider.bounds.center.x,pickCollider.bounds.max.y+.15f,pickCollider.bounds.center.z):transform.position;
        /// <summary>The entrance faces local south in the fortress assets, independent of the camera.</summary>
        public Vector3 VisitorAnchor
        {
            get
            {
                var outward = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.back), Vector3.up).normalized;
                var bounds = pickCollider != null ? pickCollider.bounds : new Bounds(transform.position, Vector3.one);
                float extent = Mathf.Abs(outward.x) * bounds.extents.x + Mathf.Abs(outward.z) * bounds.extents.z;
                return new Vector3(bounds.center.x, Tile.transform.position.y, bounds.center.z) + outward * (extent + .28f);
            }
        }        public bool Selected=>outline!=null&&outline.enabled;
        public void Initialize(HexTileView tile)
        {
            Tile=tile;Tile.UseSelectionOutline(true);
            var bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())bounds.Encapsulate(renderer.bounds);
            pickCollider=gameObject.AddComponent<BoxCollider>();
            pickCollider.center=transform.InverseTransformPoint(bounds.center);
            pickCollider.size=new Vector3(bounds.size.x/transform.lossyScale.x,bounds.size.y/transform.lossyScale.y,bounds.size.z/transform.lossyScale.z);
            var root=new GameObject("Asentamiento seleccionado");root.transform.SetParent(transform,false);
            outline=root.AddComponent<LineRenderer>();outline.useWorldSpace=true;outline.loop=true;outline.positionCount=48;
            outline.startWidth=outline.endWidth=.018f;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(1,.72f,.12f));outline.sharedMaterial=material;
            outline.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;outline.receiveShadows=false;
            for(int i=0;i<48;i++) {
                float angle=i*Mathf.PI*2/48;outline.SetPosition(i,new Vector3(tile.transform.position.x+Mathf.Cos(angle)*.71f,transform.position.y+.008f,tile.transform.position.z+Mathf.Sin(angle)*.71f));
            }
            outline.enabled=false;
        }
        public void SetSelected(bool selected){if(outline!=null)outline.enabled=selected;}
        private static readonly HexCoordinates[] Directions={new(1,0),new(1,-1),new(0,-1),new(-1,0),new(-1,1),new(0,1)};
        private static readonly float[] DirectionDegrees={0,-60,-120,180,120,60};
        private readonly System.Collections.Generic.List<LineRenderer> territory=new();
        private Material territoryMaterial;
        /// <summary>Recuadra la casilla de la fortaleza y su primer anillo con el color del reino; null lo oculta.</summary>
        public void SetTerritory(System.Collections.Generic.IReadOnlyDictionary<HexCoordinates,HexTileView> tiles,Color color)
        {
            foreach(var line in territory)if(line!=null)Release(line.gameObject);
            territory.Clear();
            foreach(var overlay in shades)if(overlay!=null)Release(overlay);shades.Clear();followed.Clear();
            if(tiles==null||Tile==null)return;
            var center=Tile.Cell.Coordinates;var set=new System.Collections.Generic.HashSet<HexCoordinates>{center};
            foreach(var direction in Directions)if(tiles.ContainsKey(center.Add(direction)))set.Add(center.Add(direction));
            float radius=VisualScaleContract.HexOuterRadius;
            var next=new System.Collections.Generic.Dictionary<Vector2Int,(Vector3 from,Vector3 to)>();
            foreach(var coordinate in set)
            {
                var middle=tiles[coordinate].transform.position;
                for(int k=0;k<6;k++)
                {
                    if(set.Contains(coordinate.Add(Directions[k])))continue;
                    var from=middle+Corner(DirectionDegrees[k]-30,radius);var to=middle+Corner(DirectionDegrees[k]+30,radius);
                    next[Key(from)]=(from,to);
                }
            }
            if(territoryMaterial==null){territoryMaterial=new Material(Resources.Load<Shader>("CityMap/TerritoryBoundary"));}
            territoryMaterial.SetColor("_BaseColor",color);
            var colliders=new System.Collections.Generic.List<MeshCollider>();
            foreach(var coordinate in set)
            {
                var collider=tiles[coordinate].GetComponent<MeshCollider>();if(collider!=null)colliders.Add(collider);
                foreach(var direction in Directions)if(tiles.TryGetValue(coordinate.Add(direction),out var around)&&around.GetComponent<MeshCollider>()!=null)colliders.Add(around.GetComponent<MeshCollider>());
            }
            while(next.Count>0)
            {
                var points=new System.Collections.Generic.List<Vector3>();
                Vector2Int start=default;foreach(var pair in next){start=pair.Key;break;}
                var key=start;
                while(next.TryGetValue(key,out var edge))
                {
                    next.Remove(key);
                    points.Add(OnGround(colliders,edge.from));
                    key=Key(edge.to);
                }
                var root=new GameObject("Territorio del reino");root.transform.SetParent(transform,false);
                var line=root.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=points.Count;line.SetPositions(points.ToArray());
                line.startWidth=line.endWidth=.035f;line.numCornerVertices=0;line.sharedMaterial=territoryMaterial;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                territory.Add(line);
            }
            Shade(set,tiles,color);
        }
        private readonly System.Collections.Generic.List<GameObject> shades=new();
        private readonly System.Collections.Generic.List<(MeshFilter source,MeshFilter copy)> followed=new();
        private Material shadeMaterial;
        /// <summary>Altura, en vertical, a la que el sombreado flota sobre el suelo: lo justo para no solaparse con él en la profundidad.</summary>
        private const float ShadeLift=.006f;
        /// <summary>Sombreado suave del color del reino sobre el terreno del primer anillo, sin tocar los materiales del suelo. Copia la malla de cada
        /// casilla en lugar de usar una lámina aparte: sigue el relieve sin asomar, no cruza las paredes de los edificios y deja las montañas sin teñir.</summary>
        private void Shade(System.Collections.Generic.HashSet<HexCoordinates> set,System.Collections.Generic.IReadOnlyDictionary<HexCoordinates,HexTileView> tiles,Color color)
        {
            var template=Resources.Load<Material>("Hueste/M_Territorio");
            if(template==null)return;
            if(shadeMaterial==null)shadeMaterial=new Material(template);
            color.a=.22f;shadeMaterial.SetColor("_BaseColor",color); // un velo: con .30 el rojo tapaba el suelo
            foreach(var coordinate in set)
            {
                if(!tiles.TryGetValue(coordinate,out var tile)||tile==null||tile.Cell.Terrain==TerrainType.Mountain)continue;
                var source=tile.GetComponent<MeshFilter>();
                if(source==null||source.sharedMesh==null)continue;
                var overlay=new GameObject("Sombreado del territorio"){layer=2};
                overlay.transform.SetParent(transform,false);
                Follow(overlay.transform,tile.transform);
                var copy=overlay.AddComponent<MeshFilter>();copy.sharedMesh=source.sharedMesh;
                var renderer=overlay.AddComponent<MeshRenderer>();renderer.sharedMaterial=shadeMaterial;
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                shades.Add(overlay);followed.Add((source,copy));
            }
        }
        /// <summary>Pone el sombreado sobre su casilla: misma posición (apenas levantado), giro y escala que ella. Cuelga de la fortaleza y no del tablero para no contar entre sus renderers.</summary>
        private void Follow(Transform overlay,Transform tile)
        {
            overlay.SetPositionAndRotation(tile.position+Vector3.up*ShadeLift,tile.rotation);
            var parent=transform.lossyScale;var wanted=tile.lossyScale;
            overlay.localScale=new Vector3(wanted.x/Mathf.Max(parent.x,1e-6f),wanted.y/Mathf.Max(parent.y,1e-6f),wanted.z/Mathf.Max(parent.z,1e-6f));
        }
        /// <summary>El sombreado acompaña a su casilla; si el terreno cambia de malla (nivel de detalle), cambia con él.</summary>
        private void LateUpdate()
        {
            for(int i=0;i<followed.Count;i++)
            {
                var (source,copy)=followed[i];
                if(source==null||copy==null)continue;
                if(copy.sharedMesh!=source.sharedMesh)copy.sharedMesh=source.sharedMesh;
                Follow(copy.transform,source.transform);
                Darken(copy,source.GetComponent<HexTileView>());
            }
        }
        /// <summary>
        /// En una casilla explorada y no visible el suelo se oscurece (×0,42, CampaignFogMap/HexTileView): el sombreado del territorio también,
        /// o el rojo queda intenso sobre un suelo casi negro.
        /// </summary>
        private void Darken(MeshFilter copy,HexTileView tile)
        {
            bool dark=tile!=null&&tile.FogVisibility!=Fulgor.Core.Campaign.CampaignVisibility.Visible;
            var renderer=copy.GetComponent<MeshRenderer>();
            if(renderer==null||shadeMaterial==null||renderer.HasPropertyBlock()==dark)return;
            if(!dark){renderer.SetPropertyBlock(null);return;}
            shadeBlock??=new MaterialPropertyBlock();
            var color=shadeMaterial.GetColor("_BaseColor");shadeBlock.SetColor("_BaseColor",new Color(color.r*.42f,color.g*.42f,color.b*.42f,color.a));
            renderer.SetPropertyBlock(shadeBlock);
        }
        private MaterialPropertyBlock shadeBlock;
        private static Vector3 Corner(float degrees,float radius){float a=degrees*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
        private static Vector2Int Key(Vector3 point)=>new(Mathf.RoundToInt(point.x*20),Mathf.RoundToInt(point.z*20));
        private static Vector3 OnGround(System.Collections.Generic.List<MeshCollider> colliders,Vector3 point,float lift=.16f)
        {
            float height=float.NegativeInfinity;var ray=new Ray(point+Vector3.up*20,Vector3.down);
            foreach(var collider in colliders)if(collider.Raycast(ray,out var hit,60)&&hit.point.y>height)height=hit.point.y;
            point.y=(float.IsNegativeInfinity(height)?point.y:height)+lift;return point;
        }
        private static void Release(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDestroy(){if(Tile!=null)Tile.UseSelectionOutline(false);Release(material);Release(territoryMaterial);Release(shadeMaterial);foreach(var overlay in shades)if(overlay!=null)Release(overlay);foreach(var line in territory)if(line!=null)Release(line.gameObject);if(outline!=null)Release(outline.gameObject);Release(pickCollider);}
    }
}

