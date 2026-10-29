using System;
using System.Collections.Generic;

namespace Fulgor.Core.Map
{
    public sealed class MapPath
    {
        internal MapPath(List<HexCoordinates> cells, long totalCost)
        {
            Cells = Array.AsReadOnly(cells.ToArray());
            TotalCost = totalCost;
        }

        // Includes both endpoints; staying at the origin has zero steps and cost.
        public IReadOnlyList<HexCoordinates> Cells { get; }
        public int Steps => Cells.Count - 1;
        public long TotalCost { get; }
    }

    /// <summary>A* for the provisional land profile. Equal-cost choices are deterministic.</summary>
    public static class MapPathfinder
    {
        public static bool TryFindPath(MapRouteGraph graph, HexCoordinates from,
            HexCoordinates to, out MapPath path, ISet<HexCoordinates> blocked = null, int maximumStepCost = int.MaxValue)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if(maximumStepCost<1)throw new ArgumentOutOfRangeException(nameof(maximumStepCost));
            path = null;
            if (!graph.CanStandAt(from) || !graph.CanStandAt(to) ||
                blocked != null && (blocked.Contains(from) || blocked.Contains(to))) return false;

            var costs = new Dictionary<HexCoordinates, long> { [from] = 0 };
            var previous = new Dictionary<HexCoordinates, HexCoordinates>();
            var frontier = new SortedSet<FrontierNode>();
            long order = 0;
            frontier.Add(new FrontierNode(from, 0, from.DistanceTo(to), order++));
            while (frontier.Count > 0)
            {
                var current = frontier.Min;
                frontier.Remove(current);
                if (costs[current.Coordinates] != current.Cost) continue;
                if (current.Coordinates == to)
                {
                    var cells = new List<HexCoordinates> { to };
                    var cursor = to;
                    while (cursor != from)
                    {
                        cursor = previous[cursor];
                        cells.Add(cursor);
                    }
                    cells.Reverse();
                    path = new MapPath(cells, current.Cost);
                    return true;
                }

                foreach (var neighbor in current.Coordinates.Neighbors())
                {
                    if (blocked != null && blocked.Contains(neighbor)) continue;
                    if (!graph.TryGetMovementCost(current.Coordinates, neighbor, out var stepCost)||stepCost>maximumStepCost) continue;
                    var nextCost = current.Cost + stepCost;
                    if (costs.TryGetValue(neighbor, out var knownCost) && knownCost <= nextCost) continue;
                    costs[neighbor] = nextCost;
                    previous[neighbor] = current.Coordinates;
                    // Every allowed step costs at least one, so axial distance is admissible.
                    frontier.Add(new FrontierNode(neighbor, nextCost,
                        nextCost + neighbor.DistanceTo(to), order++));
                }
            }
            return false;
        }

        private readonly struct FrontierNode : IComparable<FrontierNode>
        {
            public FrontierNode(HexCoordinates coordinates, long cost, long estimate, long order)
            {
                Coordinates = coordinates;
                Cost = cost;
                Estimate = estimate;
                Order = order;
            }

            public HexCoordinates Coordinates { get; }
            public long Cost { get; }
            private long Estimate { get; }
            private long Order { get; }

            public int CompareTo(FrontierNode other)
            {
                var comparison = Estimate.CompareTo(other.Estimate);
                return comparison != 0 ? comparison : Order.CompareTo(other.Order);
            }
        }
    }
}
