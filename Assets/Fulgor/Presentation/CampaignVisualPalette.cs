using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public static class CampaignVisualPalette
    {
        public static readonly Color FortressTint=new(.96f,.935f,.88f);
        public static readonly Color SunColor=new(1,.945f,.86f);
        public const float SunIntensity=1.35f;
        public static Color TerrainTint(TerrainType terrain)=>terrain switch {
            TerrainType.Water=>new Color(.84f,.93f,.94f),
            TerrainType.Plain=>new Color(.96f,.97f,.91f),
            TerrainType.Forest=>new Color(.95f,.97f,.88f),
            TerrainType.Hill=>new Color(.99f,.96f,.89f),
            _=>new Color(.98f,.97f,.93f)};
    }
}
