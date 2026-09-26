using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class MapSelectionTests
    {
        [Test]
        public void StartsWithoutSelection()
        {
            Assert.That(new MapSelection().SelectedCell.HasValue, Is.False);
        }

        [Test]
        public void SelectsAndReplacesCell()
        {
            var selection = new MapSelection();
            var first = new MapCell(new HexCoordinates(2, 3), TerrainType.Forest, false);
            var second = new MapCell(new HexCoordinates(4, 5), TerrainType.Hill, true);

            selection.Select(first);
            Assert.That(selection.SelectedCell.Value, Is.EqualTo(first));
            selection.Select(second);
            Assert.That(selection.SelectedCell.Value, Is.EqualTo(second));
        }

        [Test]
        public void NotifiesOnlyWhenSelectionChanges()
        {
            var selection = new MapSelection();
            var cell = new MapCell(new HexCoordinates(2, 3), TerrainType.Forest, false);
            var notifications = 0;
            selection.Changed += _ => notifications++;

            selection.Select(cell);
            selection.Select(cell);
            selection.Clear();
            selection.Clear();

            Assert.That(notifications, Is.EqualTo(2));
        }
    }
}
