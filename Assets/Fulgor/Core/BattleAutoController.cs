using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Combat
{
    public enum BattleAutoOrderKind { Attack, Move, Defend }

    public readonly struct BattleAutoOrder
    {
        public BattleAutoOrder(BattleAutoOrderKind kind, int actorId, int? targetId = null, HexCoordinates? destination = null)
        { Kind = kind; ActorId = actorId; TargetId = targetId; Destination = destination; }
        public BattleAutoOrderKind Kind { get; }
        public int ActorId { get; }
        public int? TargetId { get; }
        public HexCoordinates? Destination { get; }
    }

    public readonly struct BattleAutoStep
    {
        public BattleAutoStep(int round, BattleAutoOrder order, BattleAttackResult? attack, MapPath path)
        { Round = round; Order = order; Attack = attack; Path = path; }
        public int Round { get; }
        public BattleAutoOrder Order { get; }
        public BattleAttackResult? Attack { get; }
        public MapPath Path { get; }
    }

    public sealed class BattleAutoRun
    {
        internal BattleAutoRun(List<BattleAutoStep> steps, BattleOutcome outcome)
        { Steps = steps.AsReadOnly(); Outcome = outcome; }
        public IReadOnlyList<BattleAutoStep> Steps { get; }
        public BattleOutcome Outcome { get; }
        public bool LimitReached => Outcome == BattleOutcome.InProgress;
    }

    /// <summary>Provisional deterministic decisions, executed through the normal command resolver.</summary>
    public sealed class BattleAutoController
    {
        private readonly BattleAttackResolver commands;
        private readonly BattleRoster roster;

        public BattleAutoController(BattleAttackResolver commands)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            roster = commands.Roster;
        }

        // Pure decision: previews do not draw random numbers or consume activations.
        public BattleAutoOrder? ChooseOrder()
        {
            if (!roster.ActiveFormationId.HasValue) return null;
            var actorId = roster.ActiveFormationId.Value;
            var actor = roster.GetFormation(actorId);
            if (!commands.TryGetPosition(actorId, out var origin))
                throw new InvalidOperationException("Active formation has no live position.");
            var enemies = new List<int>();
            foreach (var id in roster.FormationIds)
                if (roster.GetFormation(id).Slot.Side != actor.Slot.Side && commands.TryGetPosition(id, out _)) enemies.Add(id);
            enemies.Sort((a, b) =>
            {
                commands.TryGetPosition(a, out var pa); commands.TryGetPosition(b, out var pb);
                int compare = origin.DistanceTo(pa).CompareTo(origin.DistanceTo(pb));
                return compare == 0 ? a.CompareTo(b) : compare;
            });
            foreach (var enemy in enemies)
            {
                commands.TryGetPosition(enemy, out var position);
                if (origin.DistanceTo(position) <= actor.Attributes.Range)
                    return new BattleAutoOrder(BattleAutoOrderKind.Attack, actorId, enemy);
            }
            foreach (var enemy in enemies)
            {
                commands.TryGetPosition(enemy, out var target);
                MapPath best = null;
                // Seek a free firing/melee position by actual route cost, not
                // geometric greed; this can take detours around occupied cells.
                foreach (var cell in commands.BattlefieldCells)
                {
                    if (cell.DistanceTo(target) > actor.Attributes.Range || commands.TryGetOccupant(cell, out _)) continue;
                    commands.TryPreviewMove(actorId, cell, out var path, out _);
                    if (path != null && (best == null || path.TotalCost < best.TotalCost)) best = path;
                }
                if (best == null) continue;
                long cost = 0; var destination = origin;
                for (int i = 1; i < best.Cells.Count; i++)
                {
                    cost += commands.MovementCostAt(best.Cells[i]);
                    if (cost > actor.Attributes.Movement) break;
                    destination = best.Cells[i];
                }
                if (destination != origin)
                    return new BattleAutoOrder(BattleAutoOrderKind.Move, actorId, destination: destination);
            }
            return new BattleAutoOrder(BattleAutoOrderKind.Defend, actorId);
        }

        public bool TryStep(out BattleAutoStep step)
        {
            step = default;
            var choice = ChooseOrder();
            if (!choice.HasValue) return false;
            var order = choice.Value; int round = roster.Round;
            BattleAttackResult? attack = null; MapPath path = null; bool applied;
            if (order.Kind == BattleAutoOrderKind.Attack)
            {
                applied = commands.TryAttack(order.ActorId, order.TargetId.Value, out var result, out _);
                attack = result;
            }
            else if (order.Kind == BattleAutoOrderKind.Move)
                applied = commands.TryMove(order.ActorId, order.Destination.Value, out path, out _);
            else applied = roster.TryDefend();
            if (!applied) throw new InvalidOperationException("Automatic decision was rejected by the shared rules.");
            step = new BattleAutoStep(round, order, attack, path);
            return true;
        }

        // Presentation should prefer TryStep between frames. This bounded runner
        // is for future batch resolution and reports unfinished battles honestly.
        public BattleAutoRun Run(int maximumActivations = 1000)
        {
            if (maximumActivations < 1 || maximumActivations > 10000)
                throw new ArgumentOutOfRangeException(nameof(maximumActivations));
            var steps = new List<BattleAutoStep>();
            while (steps.Count < maximumActivations && TryStep(out var step)) steps.Add(step);
            return new BattleAutoRun(steps, roster.Outcome);
        }
    }
}
