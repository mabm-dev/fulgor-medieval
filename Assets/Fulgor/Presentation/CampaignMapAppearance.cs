using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Campaign terrain presentation. Logical cells/routes are retained; LOD only changes render meshes.</summary>
    public sealed class CampaignMapAppearance : MonoBehaviour
    {
        [SerializeField] private CampaignMapAppearanceSettings settings;
        private sealed class Land
        {
            public MeshFilter Filter;
            public Mesh Full,Medium,Distant;
            public int Level;
        }
        private readonly List<Land> land=new();
        private readonly List<Mesh> ownedMeshes=new();
        private readonly List<Material> ownedMaterials=new();
        private Transform generation;
        private Matrix4x4 view,projection;
        private int forcedLevel=-1;
        private BoardBuildMetrics baseMetrics;
        private double milliseconds;
        private long managedBytes;
        public bool Ready { get; private set; }
        public int TileCount { get; private set; }
        public int[] LevelCounts { get; }=new int[3];
        public ConnectedWaterLayout WaterLayout { get; private set; }
        public void ApplyTo(Transform root)
        {
            // Older review scenes retain their own independent art pipeline.
            if(!enabled||!gameObject.activeInHierarchy||settings==null||FindFirstObjectByType<TerrainMapArtReview>()!=null)return;
            settings.Validate();ReleaseGeneration();
            var watch=System.Diagnostics.Stopwatch.StartNew();long memory=GC.GetTotalMemory(false);
            generation=root;var tiles=root.GetComponentsInChildren<HexTileView>();
            var centers=tiles.ToDictionary(t=>t.Cell.Coordinates,t=>t.transform.position);
            var water=new HashSet<HexCoordinates>(tiles.Where(t=>t.Cell.Terrain==TerrainType.Water).Select(t=>t.Cell.Coordinates));
            WaterLayout=new ConnectedWaterLayout(centers,water);
            var materials=new Material[5];
            for(int i=0;i<5;i++) {
                var source=i==0?settings.WaterMaterial:settings.TerrainMaterial(i);
                materials[i]=new Material(source){name=source.name+" · campaña",enableInstancing=true};
                materials[i].SetColor("_BaseColor",CampaignVisualPalette.TerrainTint((TerrainType)i));ownedMaterials.Add(materials[i]);
            }
            var settlements=GetComponent<CampaignSettlementController>();var surfaces=new Dictionary<Mesh,TerrainGroundSurface>();
            foreach(var tile in tiles) {
                foreach(Transform child in tile.transform)
                    if(child.name.StartsWith("Transition ",StringComparison.Ordinal)||child.name.StartsWith("Junction ",StringComparison.Ordinal))child.gameObject.SetActive(false);
                int index=(int)tile.Cell.Terrain;Mesh mesh;
                var renderer=tile.GetComponent<MeshRenderer>();
                if(index==0) {
                    // Uniform campaign resolution retains conforming neighbor edges at a lower mesh budget.
                    tile.transform.localRotation=Quaternion.identity;mesh=WaterLayout.BuildMesh(tile.transform.position,"Agua de campaña "+tile.Cell.Coordinates.Key,6,9);
                    ownedMeshes.Add(mesh);renderer.shadowCastingMode=ShadowCastingMode.Off;
                } else {
                    bool cleared=settlements!=null&&settlements.HasSettlementAt(tile.Cell.Coordinates);
                    mesh=settings.Full(cleared?5:index);int levelIndex=cleared?4:index-1;
                    tile.transform.localRotation=Quaternion.Euler(0,60*((tile.Cell.Coordinates.Q*17+tile.Cell.Coordinates.R*31)%6),0);
                    land.Add(new Land{Filter=tile.GetComponent<MeshFilter>(),Full=mesh,Medium=settings.Medium(levelIndex),Distant=settings.Distant(levelIndex)});
                }
                tile.GetComponent<MeshFilter>().sharedMesh=mesh;tile.GetComponent<MeshCollider>().sharedMesh=mesh;renderer.sharedMaterial=materials[index];
                tile.Initialize(tile.Cell,tile.Owner,Color.white);
                var gold=tile.transform.Find("Oro");
                if(gold!=null) {
                    if(!surfaces.TryGetValue(mesh,out var surface)){surface=new TerrainGroundSurface(mesh);surfaces.Add(mesh,surface);}
                    if(surface.TrySample(tile.transform,tile.transform.position,out var point,out _,out _))gold.position=point+Vector3.up*.045f;
                }
            }
            Physics.SyncTransforms();TileCount=tiles.Length;Ready=true;
            var sun=FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
            if(sun!=null){sun.color=CampaignVisualPalette.SunColor;sun.intensity=CampaignVisualPalette.SunIntensity;sun.transform.rotation=Quaternion.Euler(42,-38,0);}
            baseMetrics=GetComponent<CampaignBoardDiagnostics>().Metrics;watch.Stop();
            milliseconds=baseMetrics.Milliseconds+watch.Elapsed.TotalMilliseconds;managedBytes=baseMetrics.ManagedBytes+Math.Max(0,GC.GetTotalMemory(false)-memory);
            RefreshNow(true);
        }
        public void ForceLevel(int level)
        {
            if(level< -1||level>2)throw new ArgumentOutOfRangeException(nameof(level));forcedLevel=level;RefreshNow(true);
        }
        public void RefreshNow(bool publish=false)
        {
            var camera=Camera.main;if(!Ready||camera==null||generation==null)return;
            Array.Clear(LevelCounts,0,3);bool changed=false;
            foreach(var tile in land) {
                float depth=Vector3.Dot(tile.Filter.transform.position-camera.transform.position,camera.transform.forward);
                float span=camera.orthographic?2*camera.orthographicSize:2*Mathf.Max(.01f,depth)*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
                float size=2/Mathf.Max(.01f,span);int level=forcedLevel;
                if(level<0){float medium=.15f*(tile.Level==0?.9f:1.1f),distant=.08f*(tile.Level==2?1.1f:.9f);level=size>=medium?0:size>=distant?1:2;}
                var mesh=level==0?tile.Full:level==1?tile.Medium:tile.Distant;
                if(tile.Filter.sharedMesh!=mesh){tile.Filter.sharedMesh=mesh;changed=true;}tile.Level=level;LevelCounts[level]++;
            }
            view=camera.worldToCameraMatrix;projection=camera.projectionMatrix;
            if(changed||publish)PublishMetrics();
        }
        private void PublishMetrics()
        {
            var renderers=generation.GetComponentsInChildren<MeshRenderer>();var filters=generation.GetComponentsInChildren<MeshFilter>();
            var metrics=new BoardBuildMetrics(milliseconds,managedBytes,TileCount,0,0,baseMetrics.GoldMarkers,
                filters.Select(f=>f.sharedMesh).Distinct().Count(),renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count());
            GetComponent<CampaignBoardDiagnostics>().Record(metrics);GetComponent<CampaignSelectionPanel>().SetDiagnostics(metrics);
        }
        private void LateUpdate()
        {
            if(!Ready||generation==null||!generation.gameObject.activeInHierarchy)return;
            var camera=Camera.main;if(camera!=null&&(view!=camera.worldToCameraMatrix||projection!=camera.projectionMatrix))RefreshNow();
        }
        public void ReleaseGeneration()
        {
            Ready=false;generation=null;land.Clear();TileCount=0;WaterLayout=null;Array.Clear(LevelCounts,0,3);
            foreach(var mesh in ownedMeshes)Release(mesh);foreach(var material in ownedMaterials)Release(material);ownedMeshes.Clear();ownedMaterials.Clear();
        }
        private static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>ReleaseGeneration();
    }
}
