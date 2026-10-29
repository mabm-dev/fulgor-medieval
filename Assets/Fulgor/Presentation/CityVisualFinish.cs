using System.Collections.Generic;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Local material treatment for the city preview, preserving source assets and geometry.</summary>
    public sealed class CityVisualFinish : MonoBehaviour
    {
        [SerializeField] private Shader surfaceShader;
        [SerializeField] private CityFinishMaterials authoredMaterials;
        private readonly Vector4[] streetPaths=new Vector4[16];
        private readonly Dictionary<Material,Material> replacements=new();
        private readonly HashSet<Material> owned=new();
        private readonly Vector4[] footprints=new Vector4[16];
        private Material ground;
        public int MaterialCount=>owned.Count;
        public void Apply()
        {
            if(surfaceShader==null)surfaceShader=Shader.Find("Fulgor/City/Surface");
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true)) {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++) {
                    var source=materials[i];if(source==null||owned.Contains(source))continue;
                    if(!source.name.StartsWith("M_Villa_",System.StringComparison.Ordinal)&&!source.name.StartsWith("M_Ambiente_",System.StringComparison.Ordinal)&&!source.name.StartsWith("M_Castilla",System.StringComparison.Ordinal)&&!source.name.StartsWith("M_Patio",System.StringComparison.Ordinal))continue;
                    if(!replacements.TryGetValue(source,out var replacement)) {
                        replacement=Make(source);replacements.Add(source,replacement);owned.Add(replacement);
                    }
                    materials[i]=replacement;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
                if(authoredMaterials!=null) {
                    var occlusion=authoredMaterials.OcclusionFor(renderer.name);
                    if(occlusion!=null) {
                        var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
                        properties.SetTexture("_HouseAO",occlusion);properties.SetFloat("_UseHouseAO",1);properties.SetFloat("_UseVertexTint",1);
                        renderer.SetPropertyBlock(properties);
                    }
                }
            }
            UpdateGround();
        }
        private Material Make(Material source)
        {
            string name=source.name;
            if(authoredMaterials!=null) {
                if(name=="M_Ambiente_Suelo"&&authoredMaterials.Ground!=null) {
                    ground=new Material(authoredMaterials.Ground){name=name+" · ciudad",enableInstancing=true};return ground;
                }
                var surface=authoredMaterials.SurfaceFor(name);
                if(surface!=null) {
                    var authored=new Material(surface){name=name+" · ciudad",enableInstancing=true};
                    authored.SetVector("_CityOrigin",transform.position);return authored;
                }
                if(name.StartsWith("M_Castilla",System.StringComparison.Ordinal)||name.StartsWith("M_Patio",System.StringComparison.Ordinal)) {
                    var fortress=new Material(source){name=name+" · ciudad",enableInstancing=true};
                    fortress.shader=surfaceShader;fortress.shaderKeywords=System.Array.Empty<string>();
                    var stone=authoredMaterials.Limestone;
                    foreach(string property in new[]{"_DetailMap","_DetailNormal","_RoughnessMap"})fortress.SetTexture(property,stone.GetTexture(property));
                    fortress.SetFloat("_UseTexture",1);fortress.SetFloat("_UseAtlas",1);fortress.SetFloat("_DetailScale",6);
                    fortress.SetFloat("_NormalStrength",.12f);fortress.SetFloat("_BumpScale",.6f);
                    fortress.SetColor("_BaseColor",CampaignVisualPalette.FortressTint);fortress.SetVector("_CityOrigin",transform.position);return fortress;
                }
            }
            var material=new Material(source){name=name+" · ciudad",enableInstancing=true};
            if(name=="M_Ambiente_Suelo") {
                ground=material;material.SetColor("_BaseColor",new Color(.22f,.255f,.115f));material.SetColor("_SoilColor",new Color(.36f,.30f,.215f));return material;
            }
            if(name.StartsWith("M_Castilla",System.StringComparison.Ordinal)||name.StartsWith("M_Patio",System.StringComparison.Ordinal)) {
                // Preserve the fortress atlas, its heraldry and its baked normal/roughness maps.
                material.SetColor("_BaseColor",CampaignVisualPalette.FortressTint);return material;
            }
            if(name.Contains("Follaje")) {
                var color=source.GetColor("_BaseColor");material.SetColor("_BaseColor",color*new Color(.88f,.90f,.80f));return material;
            }
            if(name.Contains("Hierro")||name.Contains("Oscuro")||name.Contains("Frutas")||name.Contains("Hortalizas")||name.Contains("Grano"))return material;
            if(surfaceShader==null)return material;
            var tint=source.GetColor("_BaseColor");float kind=0;
            if(name.Contains("Madera")){kind=2;tint=new Color(.23f,.145f,.075f);}
            else if(name.Contains("Teja")){kind=3;tint=new Color(.39f,.17f,.085f);}
            else if(name.Contains("Piedra")||name.Contains("Camino")){kind=1;tint=new Color(.42f,.40f,.33f);}
            else if(name.Contains("Cal"))tint=new Color(.68f,.635f,.52f);
            else if(name.Contains("Tela")||name.Contains("Lino"))kind=4;
            material.shader=surfaceShader;material.shaderKeywords=System.Array.Empty<string>();
            material.SetColor("_BaseColor",tint);material.SetFloat("_SurfaceKind",kind);material.SetFloat("_Smoothness",.06f);
            material.SetVector("_CityOrigin",transform.position);return material;
        }
        private void UpdateGround()
        {
            if(ground==null)return;int count=0,pathCount=0;bool market=false;
            foreach(var building in GetComponentsInChildren<CityBuildingView>()) {
                if(count==footprints.Length)break;
                var bounds=building.Bounds;var center=bounds.center-transform.position;
                footprints[count++]=new Vector4(center.x,center.z,bounds.extents.x,bounds.extents.z);
                market|=building.Kind==CityBuildingKind.Market;
                if(building.Kind!=CityBuildingKind.Fortress&&pathCount<streetPaths.Length) {
                    float doorX=center.x-Mathf.Sign(center.x)*bounds.extents.x;
                    streetPaths[pathCount++]=new Vector4(doorX,center.z,0,Mathf.Clamp(center.z,-3.8f,3.4f));
                }
            }
            ground.SetInt("_BuildingCount",count);ground.SetVectorArray("_BuildingFootprints",footprints);
            ground.SetInt("_PathCount",pathCount);ground.SetVectorArray("_StreetPaths",streetPaths);ground.SetFloat("_MarketBuilt",market?1:0);
        }
        private void OnDestroy()
        {
            foreach(var material in owned){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            owned.Clear();replacements.Clear();
        }
    }
}
