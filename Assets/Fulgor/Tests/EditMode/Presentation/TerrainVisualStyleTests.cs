using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class TerrainVisualStyleTests
    {
        [Test]
        public void DefinesAStyleForEveryTerrain()
        {
            foreach (TerrainType terrain in System.Enum.GetValues(typeof(TerrainType)))
            {
                Assert.DoesNotThrow(() => TerrainVisualStyle.For(terrain));
            }
        }

        [Test]
        public void KeepsWaterBelowEveryLandTerrain()
        {
            var water = TerrainVisualStyle.For(TerrainType.Water);
            Assert.That(water.Height, Is.LessThan(TerrainVisualStyle.For(TerrainType.Plain).Height));
            Assert.That(water.Height, Is.LessThan(TerrainVisualStyle.For(TerrainType.Forest).Height));
            Assert.That(water.Height, Is.LessThan(TerrainVisualStyle.For(TerrainType.Hill).Height));
            Assert.That(water.Height, Is.LessThan(TerrainVisualStyle.For(TerrainType.Mountain).Height));
        }

        [Test]
        public void MakesMountainsHighest()
        {
            var mountain = TerrainVisualStyle.For(TerrainType.Mountain);
            Assert.That(mountain.Height, Is.GreaterThan(TerrainVisualStyle.For(TerrainType.Hill).Height));
            Assert.That(mountain.Height, Is.GreaterThan(TerrainVisualStyle.For(TerrainType.Forest).Height));
        }

        [Test]
        public void RejectsUnknownTerrain()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainVisualStyle.For((TerrainType)999));
        }
    }
}
