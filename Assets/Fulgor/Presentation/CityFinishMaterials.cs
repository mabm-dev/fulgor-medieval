using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Authored, baked Substance surfaces used by the compact city.</summary>
    public sealed class CityFinishMaterials : ScriptableObject
    {
        public Material Limestone, Plaster, Oak, Terracotta, Ground;
        public Texture2D HouseLimeOcclusion, HouseTallOcclusion, MarketOcclusion;
        public Texture2D OcclusionFor(string model)
        {
            if(model.Contains("CasaCal"))return HouseLimeOcclusion;
            if(model.Contains("CasaAlta"))return HouseTallOcclusion;
            if(model.Contains("Mercado"))return MarketOcclusion;
            return null;
        }
        public Material SurfaceFor(string name)
        {
            if(name.Contains("Madera"))return Oak;
            if(name.Contains("Teja"))return Terracotta;
            if(name.Contains("Piedra")||name.Contains("Camino"))return Limestone;
            if(name.Contains("Cal"))return Plaster;
            return null;
        }
    }
}
