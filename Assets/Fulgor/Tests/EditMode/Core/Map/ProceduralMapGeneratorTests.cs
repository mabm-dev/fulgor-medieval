using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Map
{
    public sealed class ProceduralMapGeneratorTests
    {
        [Test]
        public void GeneratesExpectedBoardDimensionsAndOrder()
        {
            var map = ProceduralMapGenerator.Generate(24, 16, 12345);

            Assert.That(map.Width, Is.EqualTo(24));
            Assert.That(map.Height, Is.EqualTo(16));
            Assert.That(map.Seed, Is.EqualTo(12345));
            Assert.That(map.Cells.Count, Is.EqualTo(384));
            Assert.That(map.Cells[0].Coordinates, Is.EqualTo(new HexCoordinates(0, 0)));
            Assert.That(map.Cells[383].Coordinates, Is.EqualTo(new HexCoordinates(23, 15)));
        }

        [Test]
        public void GeneratesUniqueCoordinates()
        {
            var map = ProceduralMapGenerator.Generate(24, 16, 12345);
            Assert.That(
                new HashSet<HexCoordinates>(map.Cells.Select(cell => cell.Coordinates)),
                Has.Count.EqualTo(384));
        }

        [Test]
        public void RepeatsSameMapForSameSeed()
        {
            var first = ProceduralMapGenerator.Generate(24, 16, 12345);
            var second = ProceduralMapGenerator.Generate(24, 16, 12345);
            CollectionAssert.AreEqual(first.Cells, second.Cells);
        }

        [Test]
        public void ProducesDifferentMapForDifferentSeed()
        {
            var first = ProceduralMapGenerator.Generate(24, 16, 12345);
            var second = ProceduralMapGenerator.Generate(24, 16, 54321);
            Assert.That(first.Cells.SequenceEqual(second.Cells), Is.False);
        }

        [Test]
        public void MatchesCompleteTypeScriptFixtureForSeed12345()
        {
            var map = ProceduralMapGenerator.Generate(24, 16, 12345);
            Assert.That(CalculateFixtureSignature(map.Cells), Is.EqualTo(0x49AD3DF7u));
        }

        [Test]
        public void KeepsWaterDensityNearTenPercent()
        {
            var map = ProceduralMapGenerator.Generate(24, 16, 12345);
            var waterCount = map.Cells.Count(cell => cell.Terrain == TerrainType.Water);
            Assert.That(waterCount, Is.EqualTo(36));
            Assert.That((double)waterCount / map.Cells.Count, Is.InRange(0.05d, 0.15d));
        }

        [Test]
        public void GrowsAtLeastOneConnectedWaterMassLargerThanFiveCells()
        {
            foreach (var seed in new long[] { 1, 2, 3, 4, 5, 100, 999 })
            {
                var map = ProceduralMapGenerator.Generate(24, 16, seed);
                Assert.That(LargestWaterComponent(map.Cells), Is.GreaterThan(5));
            }
        }

        [Test]
        public void PlacesGoldOnlyOnHillsOrMountains()
        {
            var map = ProceduralMapGenerator.Generate(24, 16, 12345);
            foreach (var cell in map.Cells.Where(cell => cell.HasGold))
            {
                Assert.That(cell.Terrain, Is.EqualTo(TerrainType.Hill).Or.EqualTo(TerrainType.Mountain));
            }
        }

        [Test]
        public void RejectsNonPositiveDimensions()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ProceduralMapGenerator.Generate(0, 16, 12345));
            Assert.Throws<ArgumentOutOfRangeException>(() => ProceduralMapGenerator.Generate(24, 0, 12345));
        }

        private static uint CalculateFixtureSignature(IReadOnlyList<MapCell> cells)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var cell in cells)
                {
                    var value = (uint)cell.Terrain | (cell.HasGold ? 8u : 0u);
                    hash ^= value;
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static int LargestWaterComponent(IReadOnlyList<MapCell> cells)
        {
            var remaining = new HashSet<HexCoordinates>(
                cells.Where(cell => cell.Terrain == TerrainType.Water).Select(cell => cell.Coordinates));
            var largest = 0;

            while (remaining.Count > 0)
            {
                var start = remaining.First();
                var pending = new Stack<HexCoordinates>();
                pending.Push(start);
                remaining.Remove(start);
                var size = 0;

                while (pending.Count > 0)
                {
                    var current = pending.Pop();
                    size++;
                    foreach (var neighbor in current.Neighbors())
                    {
                        if (remaining.Remove(neighbor))
                        {
                            pending.Push(neighbor);
                        }
                    }
                }

                largest = Math.Max(largest, size);
            }

            return largest;
        }
    }
}
