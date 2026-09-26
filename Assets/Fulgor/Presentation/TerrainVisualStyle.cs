using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public readonly struct TerrainVisualStyle
    {
        public TerrainVisualStyle(Color color, float height)
        {
            Color = color;
            Height = height;
        }

        public Color Color { get; }
        public float Height { get; }

        public static TerrainVisualStyle For(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Water:
                    return new TerrainVisualStyle(new Color(0.08f, 0.27f, 0.38f), 0.04f);
                case TerrainType.Plain:
                    return new TerrainVisualStyle(new Color(0.42f, 0.52f, 0.29f), 0.12f);
                case TerrainType.Forest:
                    return new TerrainVisualStyle(new Color(0.16f, 0.32f, 0.20f), 0.18f);
                case TerrainType.Hill:
                    return new TerrainVisualStyle(new Color(0.45f, 0.36f, 0.23f), 0.28f);
                case TerrainType.Mountain:
                    return new TerrainVisualStyle(new Color(0.34f, 0.34f, 0.32f), 0.42f);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(terrain));
            }
        }
    }
}
