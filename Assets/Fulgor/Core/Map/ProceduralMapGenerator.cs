using System;
using System.Collections.Generic;
using Fulgor.Core.Randomness;

namespace Fulgor.Core.Map
{
    /// <summary>Port directo de generateMap.ts; conserva orden y consumo del PRNG.</summary>
    public static class ProceduralMapGenerator
    {
        public const int DefaultWidth = 24;
        public const int DefaultHeight = 16;

        private const double WaterDensity = 0.1d;
        private const int WaterMassSize = 12;

        public static GeneratedMap Generate(int width, int height, long seed)
        {
            ValidateDimension(width, nameof(width));
            ValidateDimension(height, nameof(height));

            var random = new DeterministicRandom(seed);
            var water = GenerateWaterMask(width, height, random);
            var cells = new MapCell[width * height];
            var index = 0;

            for (var r = 0; r < height; r++)
            {
                for (var q = 0; q < width; q++)
                {
                    var coordinates = new HexCoordinates(q, r);
                    var terrain = water.Contains(coordinates)
                        ? TerrainType.Water
                        : SelectLandTerrain(random);
                    cells[index++] = new MapCell(coordinates, terrain, SelectGoldVein(terrain, random));
                }
            }

            return new GeneratedMap(width, height, seed, cells);
        }

        private static HashSet<HexCoordinates> GenerateWaterMask(
            int width,
            int height,
            DeterministicRandom random)
        {
            var totalTarget = RoundLikeJavaScript(width * height * WaterDensity);
            var massCount = Math.Max(1, RoundLikeJavaScript((double)totalTarget / WaterMassSize));
            var water = new HashSet<HexCoordinates>();

            for (var mass = 0; mass < massCount && water.Count < totalTarget; mass++)
            {
                var seed = new HexCoordinates(
                    random.NextInteger(0, width - 1),
                    random.NextInteger(0, height - 1));
                var targetSize = Math.Min(WaterMassSize, totalTarget - water.Count);
                GrowWaterMass(seed, targetSize, width, height, water, random);
            }

            return water;
        }

        private static void GrowWaterMass(
            HexCoordinates seed,
            int targetSize,
            int width,
            int height,
            ISet<HexCoordinates> water,
            DeterministicRandom random)
        {
            var current = seed;
            var placed = 0;
            var attempts = 0;
            var attemptLimit = targetSize * 20;

            while (placed < targetSize && attempts < attemptLimit)
            {
                if (water.Add(current))
                {
                    placed++;
                }

                var validNeighbors = new List<HexCoordinates>(6);
                foreach (var neighbor in current.Neighbors())
                {
                    if (IsInsideMap(neighbor, width, height))
                    {
                        validNeighbors.Add(neighbor);
                    }
                }

                if (validNeighbors.Count == 0)
                {
                    break;
                }

                current = validNeighbors[random.NextInteger(0, validNeighbors.Count - 1)];
                attempts++;
            }
        }

        private static TerrainType SelectLandTerrain(DeterministicRandom random)
        {
            var roll = random.NextInteger(0, 99);
            if (roll < 44) return TerrainType.Plain;
            if (roll < 69) return TerrainType.Forest;
            if (roll < 89) return TerrainType.Hill;
            return TerrainType.Mountain;
        }

        private static bool SelectGoldVein(TerrainType terrain, DeterministicRandom random)
        {
            if (terrain != TerrainType.Hill && terrain != TerrainType.Mountain)
            {
                return false;
            }

            return random.NextInteger(0, 99) < 25;
        }

        private static bool IsInsideMap(HexCoordinates coordinates, int width, int height) =>
            coordinates.Q >= 0 && coordinates.Q < width &&
            coordinates.R >= 0 && coordinates.R < height;

        private static int RoundLikeJavaScript(double value) =>
            (int)Math.Floor(value + 0.5d);

        private static void ValidateDimension(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "La dimensión debe ser un entero positivo.");
            }
        }
    }
}
