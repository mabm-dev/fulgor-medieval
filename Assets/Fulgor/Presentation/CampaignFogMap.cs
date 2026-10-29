using System.Collections.Generic;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Visibility adapter only. Unknown terrain uses a uniform cover and retains its selection collider.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class CampaignFogMap : MonoBehaviour
    {
        private sealed class Surface
        {
            internal Renderer renderer;
            internal MaterialPropertyBlock original=new();
            internal CampaignVisibility previous=CampaignVisibility.Visible;
            internal bool forced;
            internal ShadowCastingMode shadows;
        }
        private sealed class Tile
        {
            internal HexTileView view;internal GameObject cover;internal readonly Dictionary<int,Surface> surfaces=new();
        }
        private readonly Dictionary<int,Tile> tiles=new();
        private readonly Dictionary<int,Surface> cities=new();
        private readonly Dictionary<int,Surface> auxiliary=new();
        private readonly Dictionary<Collider,bool> cityColliders=new();
        private Mesh mesh;
        private Material material;
        private MaterialPropertyBlock faded;
        private static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),ColorId=Shader.PropertyToID("_Color");
        public int UnknownCount {get;private set;}
        public int ExploredCount {get;private set;}
        public int VisibleCount {get;private set;}
        private void LateUpdate(){ApplyNow();}
        public void ApplyNow()
        {
            var campaign=GetComponent<CampaignTurnController>();
            if(campaign?.FogEnabled!=true||campaign.Fog==null){if(tiles.Count>0)ResetFog();return;}
            campaign.RefreshFog();UnknownCount=ExploredCount=VisibleCount=0;
            foreach(var view in GetComponentsInChildren<HexTileView>())
            {
                if(!view.gameObject.activeInHierarchy)continue;int key=view.GetEntityId().GetHashCode();
                if(!tiles.TryGetValue(key,out var tile))
                {
                    EnsureResources();tile=new Tile{view=view};
                    tile.cover=new GameObject("Territorio sin explorar",typeof(MeshFilter),typeof(MeshRenderer));
                    tile.cover.transform.SetParent(view.transform,false);tile.cover.transform.localPosition=Vector3.up*.04f;
                    tile.cover.GetComponent<MeshFilter>().sharedMesh=mesh;tile.cover.GetComponent<MeshRenderer>().sharedMaterial=material;
                    tile.cover.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;tile.cover.GetComponent<MeshRenderer>().receiveShadows=false;
                    tiles.Add(key,tile);
                }
                var state=campaign.VisibilityAt(view.Cell.Coordinates);
                if(state==CampaignVisibility.Unknown)UnknownCount++;else if(state==CampaignVisibility.Explored)ExploredCount++;else VisibleCount++;
                tile.cover.SetActive(state==CampaignVisibility.Unknown);view.SetFogVisibility(state);
                foreach(var renderer in view.GetComponentsInChildren<Renderer>(true))
                {
                    if(renderer.gameObject==tile.cover)continue;
                    int id=renderer.GetEntityId().GetHashCode();
                    if(!tile.surfaces.TryGetValue(id,out var surface)){surface=Remember(renderer);tile.surfaces.Add(id,surface);}
                    Apply(surface,state,renderer.gameObject==view.gameObject);
                }
            }
            var settlements=GetComponent<CampaignSettlementController>();
            if(settlements?.Ready==true)foreach(var pair in settlements.Views)
            {
                bool seen=campaign.CanObserve(pair.Key);
                foreach(var renderer in pair.Value.GetComponentsInChildren<Renderer>(true))
                {int key=renderer.GetEntityId().GetHashCode();if(!cities.TryGetValue(key,out var surface)){surface=Remember(renderer);cities.Add(key,surface);}renderer.forceRenderingOff=!seen||surface.forced;renderer.shadowCastingMode=seen?surface.shadows:ShadowCastingMode.Off;}
                foreach(var collider in pair.Value.GetComponentsInChildren<Collider>(true))
                {if(!cityColliders.ContainsKey(collider))cityColliders.Add(collider,collider.enabled);collider.enabled=seen&&cityColliders[collider];}
            }
            // The legacy review's combined coast mesh is outside individual tiles and would reveal the full shoreline.
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
                if(renderer.gameObject.name=="Orillas de revision")
                {int key=renderer.GetEntityId().GetHashCode();if(!auxiliary.ContainsKey(key))auxiliary.Add(key,Remember(renderer));renderer.forceRenderingOff=true;renderer.shadowCastingMode=ShadowCastingMode.Off;}
        }
        private static Surface Remember(Renderer renderer)
        {var surface=new Surface{renderer=renderer,forced=renderer.forceRenderingOff,shadows=renderer.shadowCastingMode};renderer.GetPropertyBlock(surface.original);return surface;}
        private void Apply(Surface surface,CampaignVisibility state,bool root)
        {
            var renderer=surface.renderer;if(renderer==null)return;
            renderer.forceRenderingOff=surface.forced||state==CampaignVisibility.Unknown;
            renderer.shadowCastingMode=state==CampaignVisibility.Unknown?ShadowCastingMode.Off:surface.shadows;
            if(!root)
            {
                if(state==CampaignVisibility.Explored)
                {
                    faded??=new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(faded);var mat=renderer.sharedMaterial;
                    Color baseColor=mat!=null&&mat.HasProperty(BaseColor)?mat.GetColor(BaseColor):mat!=null&&mat.HasProperty(ColorId)?mat.GetColor(ColorId):Color.white;
                    faded.SetColor(BaseColor,baseColor*.42f);faded.SetColor(ColorId,baseColor*.42f);renderer.SetPropertyBlock(faded);
                }
                else if(surface.previous==CampaignVisibility.Explored)renderer.SetPropertyBlock(surface.original.isEmpty?null:surface.original);
            }
            surface.previous=state;
        }
        private void EnsureResources()
        {
            if(material==null){var shader=Resources.Load<Shader>("BattleSurface");material=new Material(shader){name="Territorio desconocido"};material.color=new Color(.055f,.075f,.085f);}
            if(mesh!=null)return;
            var vertices=new List<Vector3>{Vector3.zero};var indices=new List<int>();
            for(int i=0;i<6;i++){float a=(30+i*60)*Mathf.Deg2Rad;vertices.Add(new Vector3(Mathf.Cos(a)*.99f,0,Mathf.Sin(a)*.99f));}
            for(int i=0;i<6;i++){indices.Add(0);indices.Add((i+1)%6+1);indices.Add(i+1);}
            mesh=new Mesh{name="Cobertura uniforme"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        public void ResetFog()
        {
            foreach(var tile in tiles.Values)
            {
                foreach(var surface in tile.surfaces.Values)Restore(surface);
                if(tile.view!=null)tile.view.SetFogVisibility(CampaignVisibility.Visible);
                if(tile.cover!=null){tile.cover.SetActive(false);Destroy(tile.cover);}
            }
            foreach(var surface in cities.Values)Restore(surface);foreach(var surface in auxiliary.Values)Restore(surface);
            foreach(var pair in cityColliders)if(pair.Key!=null)pair.Key.enabled=pair.Value;cityColliders.Clear();
            tiles.Clear();cities.Clear();auxiliary.Clear();UnknownCount=ExploredCount=VisibleCount=0;
        }
        private static void Restore(Surface surface)
        {if(surface.renderer==null)return;surface.renderer.forceRenderingOff=surface.forced;surface.renderer.shadowCastingMode=surface.shadows;surface.renderer.SetPropertyBlock(surface.original.isEmpty?null:surface.original);}
        private void OnDisable(){ResetFog();}
        private void OnDestroy(){ResetFog();if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
