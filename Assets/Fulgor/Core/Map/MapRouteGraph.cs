using System;
using System.Collections.Generic;

namespace Fulgor.Core.Map
{
    public enum MapRouteKind
    {
        Road,
        RiverBank,
    }

    public readonly struct MapRouteSegment
    {
        public MapRouteSegment(HexCoordinates from, HexCoordinates to, MapRouteKind kind)
        {
            From = from;
            To = to;
            Kind = kind;
        }

        public HexCoordinates From { get; }
        public HexCoordinates To { get; }
        public MapRouteKind Kind { get; }
    }

    /// <summary>
    /// Explicit scenario routes and provisional land movement rules.
    /// Adjacency alone does not imply a road or a river.
    /// </summary>
    public sealed class MapRouteGraph
    {
        private readonly Dictionary<HexCoordinates, MapCell> cells = new();
        private readonly Dictionary<(HexCoordinates, HexCoordinates), MapRouteKind> routes = new();

        public int RouteCount => routes.Count;

        public MapRouteGraph(GeneratedMap map, IEnumerable<MapRouteSegment> segments = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            foreach (var cell in map.Cells) cells.Add(cell.Coordinates, cell);
            if (segments == null) return;

            foreach (var segment in segments)
            {
                if (!cells.TryGetValue(segment.From, out var from) ||
                    !cells.TryGetValue(segment.To, out var to) ||
                    segment.From.DistanceTo(segment.To) != 1)
                    throw new ArgumentException($"Route {segment.From} -> {segment.To}: endpoints must be adjacent map cells.", nameof(segments));

                var fromWater = from.Terrain == TerrainType.Water;
                var toWater = to.Terrain == TerrainType.Water;
                switch (segment.Kind)
                {
                    case MapRouteKind.Road when !fromWater && !toWater:
                    case MapRouteKind.RiverBank when fromWater != toWater:
                        break;
                    default:
                        throw new ArgumentException($"Route {segment.From} -> {segment.To}: kind {segment.Kind} is incompatible with its terrain.", nameof(segments));
                }

                var key = Canonical(segment.From, segment.To);
                if (routes.TryGetValue(key, out var existing) && existing != segment.Kind)
                    throw new ArgumentException("Conflicting routes for the same edge.", nameof(segments));
                routes[key] = segment.Kind;
            }
        }

        public IEnumerable<KeyValuePair<(HexCoordinates From, HexCoordinates To), MapRouteKind>> Routes
        {
            get { foreach (var route in routes) yield return route; }
        }

        public bool TryGetRoute(HexCoordinates from, HexCoordinates to, out MapRouteKind kind) =>
            routes.TryGetValue(Canonical(from, to), out kind);

        public bool CanStandAt(HexCoordinates coordinates) =>
            cells.TryGetValue(coordinates, out var cell) && MovementCost(cell, false) != int.MaxValue;

        public bool TryGetMovementCost(HexCoordinates from, HexCoordinates to, out int cost)
        {
            cost = 0;
            if (!cells.TryGetValue(from, out var origin) ||
                !cells.TryGetValue(to, out var destination) ||
                from.DistanceTo(to) != 1 ||
                origin.Terrain == TerrainType.Water || destination.Terrain == TerrainType.Water)
                return false;

            var onRoad = TryGetRoute(from, to, out var kind) && kind == MapRouteKind.Road;
            cost = MovementCost(destination, onRoad);
            return cost != int.MaxValue;
        }

        // int.MaxValue means impassable for the provisional land movement profile.
        public static int MovementCost(MapCell cell, bool onRoute) => cell.Terrain switch
        {
            TerrainType.Plain => 1,
            TerrainType.Forest => onRoute ? 1 : 2,
            TerrainType.Hill => onRoute ? 1 : 2,
            TerrainType.Mountain => int.MaxValue, // las rocas no se atraviesan ni por camino
            _ => int.MaxValue,
        };

        private static (HexCoordinates, HexCoordinates) Canonical(HexCoordinates from, HexCoordinates to)
        {
            var comparison = from.Q.CompareTo(to.Q);
            if (comparison == 0) comparison = from.R.CompareTo(to.R);
            return comparison <= 0 ? (from, to) : (to, from);
        }
    }
}
