using System;
using System.Collections.Generic;

namespace Fulgor.Core.Map
{
    public readonly struct MapCell : IEquatable<MapCell>
    {
        public MapCell(HexCoordinates coordinates, TerrainType terrain, bool hasGold)
        {
            Coordinates = coordinates;
            Terrain = terrain;
            HasGold = hasGold;
        }

        public HexCoordinates Coordinates { get; }
        public TerrainType Terrain { get; }
        public bool HasGold { get; }

        public bool Equals(MapCell other) =>
            Coordinates.Equals(other.Coordinates) && Terrain == other.Terrain && HasGold == other.HasGold;

        public override bool Equals(object obj) => obj is MapCell other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Coordinates, Terrain, HasGold);
    }

    public sealed class GeneratedMap
    {
        private readonly MapCell[] cells;

        public GeneratedMap(int width, int height, long seed, MapCell[] cells)
        {
            Width = width;
            Height = height;
            Seed = seed;
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }

        public int Width { get; }
        public int Height { get; }
        public long Seed { get; }
        public IReadOnlyList<MapCell> Cells => cells;
    }
}
