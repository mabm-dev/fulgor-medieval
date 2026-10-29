using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>Prototype objective: reach a different cell in the spawn's land component.</summary>
    public sealed class ReachDestinationMission
    {
        private ReachDestinationMission(HexCoordinates spawn, HexCoordinates destination, bool won = false)
        { Spawn = spawn; Destination = destination; IsWon = won; }

        public HexCoordinates Spawn { get; }
        public HexCoordinates Destination { get; }
        public bool IsWon { get; }

        public ReachDestinationMission ObservePosition(HexCoordinates position) =>
            IsWon || position != Destination ? this : new ReachDestinationMission(Spawn, Destination, true);

        public static ReachDestinationMission Create(GeneratedMap map, MapRouteGraph graph,
            HexCoordinates? destination = null, HexCoordinates? spawnOverride = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            HexCoordinates? spawn = spawnOverride;
            if (spawn.HasValue && !graph.CanStandAt(spawn.Value)) throw new ArgumentException("Invalid mission spawn.");
            if (!spawn.HasValue)
                foreach (var cell in map.Cells)
                    if (graph.CanStandAt(cell.Coordinates)) { spawn = cell.Coordinates; break; }
            if (!spawn.HasValue)
            {
                if (destination.HasValue) throw new ArgumentException("No land spawn for the configured objective.");
                return null;
            }
            var visited = new HashSet<HexCoordinates> { spawn.Value };
            var queue = new Queue<HexCoordinates>(); queue.Enqueue(spawn.Value);
            var farthest = spawn.Value;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell.DistanceTo(spawn.Value) > farthest.DistanceTo(spawn.Value)) farthest = cell;
                foreach (var neighbor in cell.Neighbors())
                    if (!visited.Contains(neighbor) && graph.TryGetMovementCost(cell, neighbor, out _))
                    { visited.Add(neighbor); queue.Enqueue(neighbor); }
            }
            var target = destination ?? farthest;
            if (target == spawn.Value || !visited.Contains(target))
            {
                if (destination.HasValue)
                    throw new ArgumentException("Objective must be reachable land different from the army spawn.");
                return null;
            }
            return new ReachDestinationMission(spawn.Value, target);
        }
    }
}
