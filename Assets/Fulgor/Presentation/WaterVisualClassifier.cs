using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public enum WaterVisualKind { Sea, Lake, River }

    /// <summary>Presentation-only reading of water topology; gameplay remains unchanged.</summary>
    public static class WaterVisualClassifier
    {
        private static readonly HexCoordinates[] Directions =
        {
            new HexCoordinates(1, 0), new HexCoordinates(1, -1),
            new HexCoordinates(0, -1), new HexCoordinates(-1, 0),
            new HexCoordinates(-1, 1), new HexCoordinates(0, 1),
        };

        public static IReadOnlyDictionary<HexCoordinates, WaterVisualKind> Classify(GeneratedMap map)
        {
            var water = new HashSet<HexCoordinates>();
            foreach (var cell in map.Cells)
                if (cell.Terrain == TerrainType.Water) water.Add(cell.Coordinates);

            var result = new Dictionary<HexCoordinates, WaterVisualKind>(water.Count);
            var pending = new HashSet<HexCoordinates>(water);
            while (pending.Count > 0)
            {
                using var enumerator = pending.GetEnumerator();
                enumerator.MoveNext();
                var start = enumerator.Current;
                var queue = new Queue<HexCoordinates>();
                var region = new List<HexCoordinates>();
                var touchesBoundary = false;
                queue.Enqueue(start);
                pending.Remove(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    region.Add(current);
                    touchesBoundary |= current.Q == 0 || current.R == 0 ||
                        current.Q == map.Width - 1 || current.R == map.Height - 1;
                    foreach (var direction in Directions)
                    {
                        var neighbor = current.Add(direction);
                        if (pending.Remove(neighbor)) queue.Enqueue(neighbor);
                    }
                }

                var regionKind = touchesBoundary ? WaterVisualKind.Sea : WaterVisualKind.Lake;
                foreach (var coordinate in region)
                    result.Add(coordinate, IsNarrowChannel(coordinate, water)
                        ? WaterVisualKind.River : regionKind);
            }
            return result;
        }

        private static bool IsNarrowChannel(HexCoordinates coordinate, ISet<HexCoordinates> water)
        {
            var connected = new List<int>(6);
            for (var index = 0; index < Directions.Length; index++)
                if (water.Contains(coordinate.Add(Directions[index]))) connected.Add(index);
            return connected.Count == 2 &&
                (connected[0] + 3) % Directions.Length == connected[1];
        }
    }
}
