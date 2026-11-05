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
        private LineRenderer control;
        private Material controlMaterial;
        public HexTileView Tile { get; private set; }
        public bool Selected=>outline!=null&&outline.enabled;
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
        public void SetOwner(bool own)
        {
            if(control==null)
            {
                var root=new GameObject("Control de la fortaleza");root.transform.SetParent(transform,false);
                control=root.AddComponent<LineRenderer>();control.useWorldSpace=true;control.loop=true;control.positionCount=48;
                control.startWidth=control.endWidth=.012f;
                controlMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));control.sharedMaterial=controlMaterial;
                control.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;control.receiveShadows=false;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;control.SetPosition(i,new Vector3(Tile.transform.position.x+Mathf.Cos(a)*.66f,transform.position.y+.012f,Tile.transform.position.z+Mathf.Sin(a)*.66f));}
            }
            controlMaterial.SetColor("_BaseColor",own?new Color(.15f,.48f,.92f):new Color(.82f,.23f,.17f));
        }
        private static readonly HexCoordinates[] Directions={new(1,0),new(1,-1),new(0,-1),new(-1,0),new(-1,1),new(0,1)};
        private static readonly float[] DirectionDegrees={0,-60,-120,180,120,60};
        private readonly System.Collections.Generic.List<LineRenderer> territory=new();
        private Material territoryMaterial;
        /// <summary>Recuadra la casilla de la fortaleza y su primer anillo con el color del reino; null lo oculta.</summary>
        public void SetTerritory(System.Collections.Generic.IReadOnlyDictionary<HexCoordinates,HexTileView> tiles,Color color)
        {
            foreach(var line in territory)if(line!=null)Release(line.gameObject);
            territory.Clear();
            ClearTerrainTint();
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
            TintTerrain(set,tiles,color);
        }
        private readonly System.Collections.Generic.List<HexTileView> tintedTiles=new();
        private void ClearTerrainTint()
        {
            foreach(var tile in tintedTiles)if(tile!=null)tile.SetTerritoryTint(Color.white);
            tintedTiles.Clear();
        }
        // Tint only the terrain renderer: buildings retain their original materials.
        private void TintTerrain(System.Collections.Generic.HashSet<HexCoordinates> set,System.Collections.Generic.IReadOnlyDictionary<HexCoordinates,HexTileView> tiles,Color color)
        {
            var tint=Color.Lerp(Color.white,color,.42f);tint.a=1f;
            foreach(var coordinate in set)
                if(tiles.TryGetValue(coordinate,out var tile)&&tile.Cell.Terrain!=TerrainType.Mountain){tile.SetTerritoryTint(tint);tintedTiles.Add(tile);}
        }
        private static Vector3 Corner(float degrees,float radius){float a=degrees*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
        private static Vector2Int Key(Vector3 point)=>new(Mathf.RoundToInt(point.x*20),Mathf.RoundToInt(point.z*20));
        private static Vector3 OnGround(System.Collections.Generic.List<MeshCollider> colliders,Vector3 point,float lift=.012f)
        {
            float height=float.NegativeInfinity;var ray=new Ray(point+Vector3.up*20,Vector3.down);
            foreach(var collider in colliders)if(collider.Raycast(ray,out var hit,60)&&hit.point.y>height)height=hit.point.y;
            point.y=(float.IsNegativeInfinity(height)?point.y:height)+lift;return point;
        }
        private static void Release(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDestroy(){if(Tile!=null)Tile.UseSelectionOutline(false);Release(material);Release(controlMaterial);Release(territoryMaterial);ClearTerrainTint();foreach(var line in territory)if(line!=null)Release(line.gameObject);if(outline!=null)Release(outline.gameObject);if(control!=null)Release(control.gameObject);Release(pickCollider);}
    }
}
