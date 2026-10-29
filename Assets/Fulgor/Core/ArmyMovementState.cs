using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum ArmyMoveFailure
    {
        None,
        Unreachable,
        InsufficientMovement,
    }

    /// <summary>
    /// Immutable movement state for one land army. Uses the scenario's terrain
    /// and roads; occupancy, combat and naval movement are not implemented here.
    /// </summary>
    public sealed class ArmyMovementState
    {
        private readonly MapRouteGraph graph;

        public ArmyMovementState(MapRouteGraph graph, HexCoordinates position, int movementPerTurn)
            : this(graph, position, movementPerTurn, movementPerTurn)
        {
        }

        private ArmyMovementState(MapRouteGraph graph, HexCoordinates position,
            int movementPerTurn, int remainingMovement)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (movementPerTurn < 1) throw new ArgumentOutOfRangeException(nameof(movementPerTurn));
            if (!graph.CanStandAt(position))
                throw new ArgumentException("Army must start on a passable land cell.", nameof(position));
            Position = position;
            MovementPerTurn = movementPerTurn;
            RemainingMovement = remainingMovement;
        }

        public static ArmyMovementState Restore(MapRouteGraph graph, HexCoordinates position,
            int movementPerTurn, int remainingMovement)
        {
            if (remainingMovement < 0 || remainingMovement > movementPerTurn)
                throw new ArgumentOutOfRangeException(nameof(remainingMovement));
            return new ArmyMovementState(graph, position, movementPerTurn, remainingMovement);
        }

        public HexCoordinates Position { get; }
        public int MovementPerTurn { get; }
        public int RemainingMovement { get; }

        /// <summary>
        /// Moves the entire route or rejects it without changing state. A route
        /// exceeding the budget is returned for preview, but is never executed.
        /// Callers must replace their state with next only on success.
        /// </summary>
        public bool TryMove(HexCoordinates destination, out ArmyMovementState next,
            out MapPath path, out ArmyMoveFailure failure, ISet<HexCoordinates> blocked = null)
        {
            next = this;
            if (!MapPathfinder.TryFindPath(graph, Position, destination, out path, blocked))
            {
                failure = ArmyMoveFailure.Unreachable;
                return false;
            }
            if (path.TotalCost > RemainingMovement)
            {
                failure = ArmyMoveFailure.InsufficientMovement;
                return false;
            }

            failure = ArmyMoveFailure.None;
            if (destination != Position)
                next = new ArmyMovementState(graph, destination, MovementPerTurn,
                    RemainingMovement - checked((int)path.TotalCost));
            return true;
        }

        /// <summary>Advances a shortest route by affordable whole steps only.
        /// Returns false only when no route exists; an exhausted army may make zero steps.</summary>
        public bool TryAdvance(HexCoordinates destination, out ArmyMovementState next, out MapPath path)
        {
            next = this;
            if (!MapPathfinder.TryFindPath(graph, Position, destination, out path)) return false;
            int spent = 0;
            var reached = Position;
            for (int i = 1; i < path.Cells.Count; i++)
            {
                if (!graph.TryGetMovementCost(reached, path.Cells[i], out var cost) ||
                    cost > RemainingMovement - spent) break;
                spent += cost;
                reached = path.Cells[i];
            }
            if (reached != Position)
                next = new ArmyMovementState(graph, reached, MovementPerTurn, RemainingMovement - spent);
            return true;
        }

        // The campaign owner calls this once at its turn boundary, never per move.
        public ArmyMovementState BeginTurn() =>
            new ArmyMovementState(graph, Position, MovementPerTurn, MovementPerTurn);
    }
}
