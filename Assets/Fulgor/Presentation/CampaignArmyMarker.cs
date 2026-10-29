using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Provisional army marker grounded on the terrain; movement is cosmetic, not campaign state.</summary>
    public sealed class CampaignArmyMarker : MonoBehaviour
    {
        private readonly Dictionary<HexCoordinates, HexTileView> tiles = new();
        private readonly Dictionary<Mesh,TerrainGroundSurface> surfaces = new();
        private readonly Dictionary<(Mesh,Quaternion,Vector3),Vector3> anchors = new();
        private readonly List<HexCoordinates> waypoints = new();
        private GameObject marker;
        private Transform markerBase;
        private Material material;
        private CampaignBoardSelection owner;
        private HexCoordinates? destination;
        private int waypoint;
        private Mesh lastGroundMesh;
        private Matrix4x4 lastGroundMatrix;
        private Vector2 lastGroundXZ;
        private static readonly HexCoordinates[] Directions={new(1,0),new(1,-1),new(0,-1),new(-1,0),new(-1,1),new(0,1)};
        private const float Speed = 2.5f, Clearance = .006f, BaseRadius = .325f;
        public bool IsMoving => waypoint < waypoints.Count;
        private bool fogVisible=true;
        public void SetFogVisible(bool value){fogVisible=value;if(marker!=null)foreach(var renderer in marker.GetComponentsInChildren<Renderer>(true)){renderer.forceRenderingOff=!value;renderer.shadowCastingMode=value?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;}}
        public bool IsVisible => marker != null && marker.activeSelf&&fogVisible;
        public Vector3 VisualPosition => marker != null ? marker.transform.position : transform.position;

        public void Initialize(CampaignBoardSelection selectionOwner)
        {
            owner=selectionOwner; tiles.Clear(); surfaces.Clear(); anchors.Clear(); waypoints.Clear(); waypoint=0; destination=null;lastGroundMesh=null;
            if (owner != null)
                foreach (var tile in owner.GetComponentsInChildren<HexTileView>())
                    if (tile.Owner == owner && tile.gameObject.activeInHierarchy) tiles[tile.Cell.Coordinates]=tile;
            if (marker == null) CreateMarker();
            marker.SetActive(false);
        }

        public void SetArmy(ArmyMovementState army,IReadOnlyList<HexCoordinates> route = null)
        {
            if (marker == null) return;
            if (army == null || !tiles.TryGetValue(army.Position,out var tile) || tile == null || !tile.gameObject.activeInHierarchy)
            { marker.SetActive(false); destination=null; waypoints.Clear(); waypoint=0; lastGroundMesh=null; return; }
            if (destination.HasValue && destination.Value == army.Position && marker.activeSelf) return;
            bool animate=destination.HasValue && marker.activeSelf && route != null && route.Count > 1 && route[0] == destination.Value;
            int end=-1;
            if (animate) {
                for(int i=1;i<route.Count;i++) {
                    if (!tiles.TryGetValue(route[i],out var step) || step == null || !step.gameObject.activeInHierarchy || route[i-1].DistanceTo(route[i]) != 1) break;
                    if (route[i] == army.Position) { end=i; break; }
                }
            }
            if (end > 0) {
                if (!IsMoving) { waypoints.Clear(); waypoint=0; }
                for(int i=1;i<=end;i++) waypoints.Add(route[i]);
            }
            else { waypoints.Clear(); waypoint=0; marker.transform.position=Anchor(tile); lastGroundMesh=null; }
            destination=army.Position; marker.SetActive(true); Ground();
        }

        private void Update()
        {
            if (owner == null || !owner.IsInteractionSuspended) AdvanceVisual(Time.deltaTime);
        }

        /// <summary>Advances the visual route only. Public for deterministic presentation checks.</summary>
        public void AdvanceVisual(float seconds)
        {
            if (!IsVisible || !destination.HasValue) return;
            float remaining=Mathf.Max(0,seconds)*Speed;
            while (IsMoving && remaining > 0) {
                if (!tiles.TryGetValue(waypoints[waypoint],out var tile) || tile == null || !tile.gameObject.activeInHierarchy)
                { SetArmy(null); return; }
                var current=marker.transform.position; var goal=Anchor(tile); goal.y=current.y;
                float length=Vector3.Distance(current,goal), travel=Mathf.Min(remaining,length);
                marker.transform.position=Vector3.MoveTowards(current,goal,travel); remaining-=travel;
                if (length <= travel+.00001f) waypoint++; else break;
            }
            if (!IsMoving && waypoints.Count>0) { waypoints.Clear(); waypoint=0; }
            Ground(); // Also observes terrain replacement after initial board generation and LOD changes.
        }

        private bool Sample(Vector3 position,out Vector3 point,out Vector3 normal)
        {
            point=position; normal=Vector3.up;
            if (owner == null) return false;
            // Use the board transform, not a physics ray: props and units cannot become ground.
            var local=owner.transform.InverseTransformPoint(position);
            var coordinate=HexWorldLayout.FromWorld(local);
            float weight=0; Vector3 sum=Vector3.zero,normalSum=Vector3.zero;
            if (SampleTile(coordinate,position,out point,out normal,out var gap)) {
                if (gap < .00001f) return true;
                float w=1/Mathf.Max(.00001f,gap);sum=point*w;normalSum=normal*w;weight=w;
            }
            foreach (var direction in Directions) {
                var adjacent=coordinate.Add(direction);
                if (!SampleTile(adjacent,position,out var p,out var n,out gap)) continue;
                if (gap < .00001f) {point=p;normal=n;return true;}
                float w=1/Mathf.Max(.00001f,gap);sum+=p*w;normalSum+=n*w;weight+=w;
            }
            if (weight<=0) return false;
            point=sum/weight;normal=normalSum.normalized;return true;
        }

        private bool SampleTile(HexCoordinates coordinate,Vector3 position,out Vector3 point,out Vector3 normal,out float gap)
        {
            point=default;normal=Vector3.up;gap=float.PositiveInfinity;
            if (!tiles.TryGetValue(coordinate,out var tile) || tile == null || !tile.gameObject.activeInHierarchy) return false;
            var collider=tile.GetComponent<MeshCollider>(); var mesh=collider != null ? collider.sharedMesh : null;
            if (mesh == null || !mesh.isReadable) return false;
            if (!surfaces.TryGetValue(mesh,out var surface)) { surface=new TerrainGroundSurface(mesh);surfaces.Add(mesh,surface); }
            return surface.TrySample(tile.transform,position,out point,out normal,out gap);
        }

        private void Ground()
        {
            var position=marker.transform.position;
            var coordinate=owner != null ? HexWorldLayout.FromWorld(owner.transform.InverseTransformPoint(position)) : default;
            tiles.TryGetValue(coordinate,out var tile);
            var currentMesh=tile != null ? tile.GetComponent<MeshCollider>()?.sharedMesh : null;
            if(!IsMoving && destination.HasValue && tile != null && tile.Cell.Coordinates==destination.Value && currentMesh!=lastGroundMesh) {
                position=Anchor(tile);marker.transform.position=position;
            }
            var matrix=tile != null ? tile.transform.localToWorldMatrix : Matrix4x4.identity;
            var xz=new Vector2(position.x,position.z);
            if (!IsMoving && currentMesh != null && currentMesh==lastGroundMesh && matrix==lastGroundMatrix && xz==lastGroundXZ) return;
            if (!Sample(position,out var ground,out var normal)) return;
            // Keep the flag upright; only its circular plinth follows the local slope.
            normal=Vector3.RotateTowards(Vector3.up,normal,35*Mathf.Deg2Rad,0);
            float support=ground.y;
            for(int i=0;i<8;i++) {
                float angle=i*Mathf.PI*.25f;var offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*BaseRadius;
                if (Sample(position+offset,out var p,out _)) {
                    float plane=-Vector3.Dot(normal,offset)/Mathf.Max(.1f,normal.y);
                    support=Mathf.Max(support,p.y-plane);
                }
            }
            position.y=support+Clearance;marker.transform.position=position;
            markerBase.rotation=Quaternion.FromToRotation(Vector3.up,normal);markerBase.position=position+normal*.06f;
            lastGroundMesh=currentMesh;lastGroundMatrix=matrix;lastGroundXZ=xz;
        }

        private Vector3 Anchor(HexTileView tile)
        {
            var mesh=tile.GetComponent<MeshCollider>()?.sharedMesh;
            if(mesh==null)return tile.transform.position;
            var key=(mesh,tile.transform.rotation,tile.transform.lossyScale);
            if(anchors.TryGetValue(key,out var cached))return tile.transform.TransformPoint(cached);
            var center=tile.transform.position;var chosen=center;
            float Score(Vector3 candidate) {
                if(!SampleTile(tile.Cell.Coordinates,candidate,out var ground,out var normal,out _))return float.PositiveInfinity;
                normal=Vector3.RotateTowards(Vector3.up,normal,35*Mathf.Deg2Rad,0);float support=ground.y;
                for(int i=0;i<8;i++) {
                    float angle=i*Mathf.PI*.25f;var offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*BaseRadius;
                    if(!SampleTile(tile.Cell.Coordinates,candidate+offset,out var p,out _,out var gap)||gap>.001f)return float.PositiveInfinity;
                    float plane=-Vector3.Dot(normal,offset)/Mathf.Max(.1f,normal.y);support=Mathf.Max(support,p.y-plane);
                }
                return support-ground.y+Vector3.Distance(candidate,center)*.02f+Vector3.Angle(Vector3.up,normal)*.0005f;
            }
            float score=Score(center);
            // Smooth ground retains the cell centre. On rough terrain search a small stable patch.
            if(score>.045f)
                for(int ring=1;ring<=2;ring++)for(int i=0;i<8;i++) {
                    float angle=i*Mathf.PI*.25f;var candidate=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(ring*.25f);
                    float value=Score(candidate);if(value<score){score=value;chosen=candidate;}
                }
            anchors[key]=tile.transform.InverseTransformPoint(chosen);return chosen;
        }

        public void SetColor(Color color) { if (material != null) material.color=color; }
        private void CreateMarker()
        {
            marker=new GameObject("Hueste provisional");marker.transform.SetParent(transform,false);
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material=new Material(shader) {name="Hueste provisional · azul"};material.color=new Color(.08f,.38f,.88f);
            markerBase=CreatePart("Base",PrimitiveType.Cylinder,new Vector3(0,.06f,0),new Vector3(.65f,.06f,.65f));
            CreatePart("Asta",PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.06f,.55f,.06f));
            CreatePart("Estandarte",PrimitiveType.Cube,new Vector3(.22f,1,0),new Vector3(.45f,.3f,.055f));
        }
        private Transform CreatePart(string label,PrimitiveType type,Vector3 position,Vector3 scale)
        {
            var part=GameObject.CreatePrimitive(type);part.name=label;part.layer=2;
            part.transform.SetParent(marker.transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
            var collider=part.GetComponent<Collider>();collider.enabled=false;Release(collider);
            part.GetComponent<Renderer>().sharedMaterial=material;return part.transform;
        }
        private static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value);else DestroyImmediate(value); }
        private void OnDestroy() { Release(material);Release(marker); }
    }
}
