using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Combat
{
    public enum BattleMoveFailure
    {
        None, BattleFinished, NotActiveFormation, AlreadyThere, InvalidDestination,
        Occupied, Unreachable, InsufficientMovement,
    }

    // Uses the same positions and terrain as attacks, so movement cannot leave
    // the attack resolver looking at obsolete deployment coordinates.
    public sealed partial class BattleAttackResolver
    {
        public bool TryGetPosition(int formationId, out HexCoordinates position)
        {
            position = default;
            return positions.TryGetValue(formationId, out position) && roster.GetFormation(formationId).IsInBattle;
        }

        public bool TryGetOccupant(HexCoordinates cell, out int formationId)
        {
            foreach (var pair in positions)
                if (pair.Value == cell && roster.GetFormation(pair.Key).IsInBattle)
                { formationId = pair.Key; return true; }
            formationId = default;
            return false;
        }

        /// <summary>Pure preview. Returns the full route even when its cost exceeds movement.</summary>
        public bool TryPreviewMove(int formationId, HexCoordinates destination,
            out MapPath path, out BattleMoveFailure failure)
        {
            path = null;
            if (roster.Outcome != BattleOutcome.InProgress)
            { failure = BattleMoveFailure.BattleFinished; return false; }
            if (roster.ActiveFormationId != formationId)
            { failure = BattleMoveFailure.NotActiveFormation; return false; }
            var origin = positions[formationId];
            if (destination == origin)
            { failure = BattleMoveFailure.AlreadyThere; return false; }
            if (!terrain.ContainsKey(destination))
            { failure = BattleMoveFailure.InvalidDestination; return false; }
            if (TryGetOccupant(destination, out _))
            { failure = BattleMoveFailure.Occupied; return false; }

            var blocked = new HashSet<HexCoordinates>();
            foreach (var pair in positions)
                if (pair.Key != formationId && roster.GetFormation(pair.Key).IsInBattle) blocked.Add(pair.Value);
            var costs = new Dictionary<HexCoordinates, long> { [origin] = 0 };
            var previous = new Dictionary<HexCoordinates, HexCoordinates>();
            var frontier = new SortedSet<(long Cost, long Order, int Q, int R)>();
            long order = 0;
            frontier.Add((0, order++, origin.Q, origin.R));
            while (frontier.Count > 0)
            {
                var node = frontier.Min; frontier.Remove(node);
                var current = new HexCoordinates(node.Q, node.R);
                if (costs[current] != node.Cost) continue;
                if (current == destination)
                {
                    var cells = new List<HexCoordinates> { destination };
                    while (current != origin) { current = previous[current]; cells.Add(current); }
                    cells.Reverse();
                    path = new MapPath(cells, node.Cost);
                    failure = node.Cost > roster.GetFormation(formationId).Attributes.Movement ?
                        BattleMoveFailure.InsufficientMovement : BattleMoveFailure.None;
                    return failure == BattleMoveFailure.None;
                }
                foreach (var neighbor in current.Neighbors())
                {
                    if (blocked.Contains(neighbor) || !terrain.TryGetValue(neighbor, out var tile)) continue;
                    var cost = node.Cost + MovementCostFor(tile);
                    if (costs.TryGetValue(neighbor, out var known) && known <= cost) continue;
                    costs[neighbor] = cost; previous[neighbor] = new HexCoordinates(node.Q, node.R);
                    frontier.Add((cost, order++, neighbor.Q, neighbor.R));
                }
            }
            failure = BattleMoveFailure.Unreachable;
            return false;
        }

        /// <summary>A legal tactical move consumes the entire activation, as in the reference rules.</summary>
        public bool TryMove(int formationId, HexCoordinates destination, out MapPath path, out BattleMoveFailure failure)
        {
            if (!TryPreviewMove(formationId, destination, out path, out failure)) return false;
            positions[formationId] = destination;
            SynchronizeVisualOrder();
            roster.CompleteActivation();
            return true;
        }

        private void SynchronizeVisualOrder()
        {
            int minQ = int.MaxValue, minR = int.MaxValue;
            foreach (var cell in terrain.Keys) { minQ = Math.Min(minQ, cell.Q); minR = Math.Min(minR, cell.R); }
            foreach (var pair in positions)
                if (roster.GetFormation(pair.Key).IsInBattle)
                    roster.UpdateVisualPosition(pair.Key, checked(pair.Value.R - minR), checked(pair.Value.Q - minQ));
        }

        public static int MovementCostFor(TacticalTerrain tile) => tile switch
        {
            TacticalTerrain.Clear => 1,
            TacticalTerrain.Woodland => 2,
            TacticalTerrain.Rough => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(tile)),
        };
    }
}
