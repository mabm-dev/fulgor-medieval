using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class WaterVisualClassifierTests
    {
        [Test]
        public void DistinguishesSeaLakeAndNarrowRiverFromTopology()
        {
            var cells = new[]
            {
                new MapCell(new HexCoordinates(0, 1), TerrainType.Water, false),
                new MapCell(new HexCoordinates(2, 2), TerrainType.Water, false),
                new MapCell(new HexCoordinates(3, 2), TerrainType.Water, false),
                new MapCell(new HexCoordinates(4, 2), TerrainType.Water, false),
                new MapCell(new HexCoordinates(3, 4), TerrainType.Water, false),
            };
            var result = WaterVisualClassifier.Classify(new GeneratedMap(6, 6, 1, cells));

            Assert.That(result[new HexCoordinates(0, 1)], Is.EqualTo(WaterVisualKind.Sea));
            Assert.That(result[new HexCoordinates(3, 2)], Is.EqualTo(WaterVisualKind.River));
            Assert.That(result[new HexCoordinates(2, 2)], Is.EqualTo(WaterVisualKind.Lake));
            Assert.That(result[new HexCoordinates(3, 4)], Is.EqualTo(WaterVisualKind.Lake));
        }

        [Test]
        public void IgnoresLandCells()
        {
            var cells = new[]
            {
                new MapCell(new HexCoordinates(1, 1), TerrainType.Plain, false),
            };
            Assert.That(WaterVisualClassifier.Classify(new GeneratedMap(3, 3, 1, cells)), Is.Empty);
        }
    }
}
