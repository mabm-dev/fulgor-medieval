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
        /// <summary>Tierra del suelo pisado alrededor de un edificio y de su base, según el terreno de la casilla (sRGB).</summary>
        public static Color TrampledGround(TerrainType terrain)=>terrain switch {
            TerrainType.Forest=>new Color(.38f,.30f,.21f),   // tierra oscura con hojarasca
            TerrainType.Mountain=>new Color(.60f,.57f,.52f), // grava
            _=>new Color(.56f,.47f,.34f)};                    // tierra pisada, polvorienta
        /// <summary>Cuánta hierba sobrevive en lo pisado y trepa por la base del edificio: en la montaña, ninguna.</summary>
        public static float GroundTufts(TerrainType terrain)=>terrain switch {TerrainType.Mountain=>0f,TerrainType.Forest=>.8f,_=>.7f};
        /// <summary>Luz de esa hierba respecto a la hierba común: bajo los árboles, más oscura.</summary>
        public static float GroundTuftLight(TerrainType terrain)=>terrain==TerrainType.Forest?.7f:1f;
    }
}
