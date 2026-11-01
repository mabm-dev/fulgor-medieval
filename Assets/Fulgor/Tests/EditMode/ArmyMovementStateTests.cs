using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class ArmyMovementStateTests
    {
        private static HexCoordinates At(int q) => new HexCoordinates(q, 0);

        private static MapRouteGraph Line(params TerrainType[] terrain)
        {
            var cells = new MapCell[terrain.Length];
            for (var i = 0; i < terrain.Length; i++) cells[i] = new MapCell(At(i), terrain[i], false);
            return new MapRouteGraph(new GeneratedMap(terrain.Length, 1, 1, cells));
        }

        [Test]
        public void MovementChargesAllTerrainAndPreservesPreviousState()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Forest,
                TerrainType.Hill), At(0), 6);
            Assert.That(initial.TryMove(At(2), out var next, out var path, out var failure), Is.True);
            Assert.That(path.TotalCost, Is.EqualTo(4));
            Assert.That(path.Steps, Is.EqualTo(2));
            Assert.That(next.Position, Is.EqualTo(At(2)));
            Assert.That(next.RemainingMovement, Is.EqualTo(2));
            Assert.That(failure, Is.EqualTo(ArmyMoveFailure.None));
            Assert.That(initial.Position, Is.EqualTo(At(0)));
            Assert.That(initial.RemainingMovement, Is.EqualTo(6));
        }

        [Test]
        public void InsufficientBudgetRejectsWholeMoveAndReturnsPreview()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Forest,
                TerrainType.Hill), At(0), 3);
            Assert.That(initial.TryMove(At(2), out var next, out var path, out var failure), Is.False);
            Assert.That(next, Is.SameAs(initial));
            Assert.That(path.TotalCost, Is.EqualTo(4));
            Assert.That(failure, Is.EqualTo(ArmyMoveFailure.InsufficientMovement));
        }

        [Test]
        public void RepeatedMovesCannotSpendMoreThanTheTurnBudget()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Plain,
                TerrainType.Hill), At(0), 2);
            initial.TryMove(At(1), out var moved, out _, out _);
            Assert.That(moved.TryMove(At(2), out var rejected, out _, out var failure), Is.False);
            Assert.That(rejected, Is.SameAs(moved));
            Assert.That(rejected.RemainingMovement, Is.EqualTo(1));
            Assert.That(failure, Is.EqualTo(ArmyMoveFailure.InsufficientMovement));
        }

        [Test]
        public void ExactBudgetAndNewTurnWorkWithoutMovingTheArmy()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Hill), At(0), 2);
            initial.TryMove(At(1), out var moved, out _, out _);
            Assert.That(moved.RemainingMovement, Is.Zero);
            var reset = moved.BeginTurn();
            Assert.That(reset.Position, Is.EqualTo(At(1)));
            Assert.That(reset.RemainingMovement, Is.EqualTo(2));
            Assert.That(moved.RemainingMovement, Is.Zero);
            Assert.That(reset.BeginTurn().RemainingMovement, Is.EqualTo(2));
        }

        [Test]
        public void StayingStillIsFreeEvenWithNoMovementLeft()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Plain), At(0), 1);
            initial.TryMove(At(1), out var moved, out _, out _);
            Assert.That(moved.TryMove(At(1), out var next, out var path, out var failure), Is.True);
            Assert.That(next, Is.SameAs(moved));
            Assert.That(path.TotalCost, Is.Zero);
            Assert.That(failure, Is.EqualTo(ArmyMoveFailure.None));
        }

        [Test]
        public void WaterBarrierAndOutOfMapAreRejected()
        {
            var initial = new ArmyMovementState(Line(TerrainType.Plain, TerrainType.Water,
                TerrainType.Plain), At(0), 10);
            foreach (var target in new[] { At(1), At(2), At(99) })
            {
                Assert.That(initial.TryMove(target, out var next, out var path, out var failure), Is.False);
                Assert.That(next, Is.SameAs(initial));
                Assert.That(path, Is.Null);
                Assert.That(failure, Is.EqualTo(ArmyMoveFailure.Unreachable));
            }
        }

        [Test]
        public void MountainsCannotBeCrossedEvenAlongARoad()
        {
            var map = new GeneratedMap(2, 1, 1, new[] {
                new MapCell(At(0), TerrainType.Plain, false),
                new MapCell(At(1), TerrainType.Mountain, false) });
            var graph = new MapRouteGraph(map, new[] { new MapRouteSegment(At(0), At(1), MapRouteKind.Road) });
            Assert.That(graph.CanStandAt(At(1)), Is.False);
            Assert.That(graph.TryGetMovementCost(At(0), At(1), out _), Is.False);
        }

        [Test]
        public void RoadsReduceTheActualChargedCost()
        {
            var map = new GeneratedMap(2, 1, 1, new[] {
                new MapCell(At(0), TerrainType.Plain, false),
                new MapCell(At(1), TerrainType.Hill, false) });
            var graph = new MapRouteGraph(map, new[] { new MapRouteSegment(At(0), At(1), MapRouteKind.Road) });
            var initial = new ArmyMovementState(graph, At(0), 1);
            Assert.That(initial.TryMove(At(1), out var next, out var path, out _), Is.True);
            Assert.That(path.TotalCost, Is.EqualTo(1));
            Assert.That(next.RemainingMovement, Is.Zero);
        }

        [Test]
        public void ChoosesAffordableDetourInsteadOfExpensiveShortRoute()
        {
            var map = new GeneratedMap(3, 2, 1, new[] {
                new MapCell(At(0), TerrainType.Plain, false),
                new MapCell(At(1), TerrainType.Mountain, false),
                new MapCell(At(2), TerrainType.Plain, false),
                new MapCell(new HexCoordinates(0, 1), TerrainType.Plain, false),
                new MapCell(new HexCoordinates(1, 1), TerrainType.Plain, false) });
            var initial = new ArmyMovementState(new MapRouteGraph(map), At(0), 3);
            Assert.That(initial.TryMove(At(2), out var next, out var path, out _), Is.True);
            Assert.That(path.TotalCost, Is.EqualTo(3));
            Assert.That(path.Steps, Is.EqualTo(3));
            Assert.That(path.Cells, Has.No.Member(At(1)));
            Assert.That(next.RemainingMovement, Is.Zero);
        }

        [Test]
        public void InvalidSpawnOrBudgetIsRejected()
        {
            var graph = Line(TerrainType.Plain, TerrainType.Water);
            Assert.Throws<ArgumentNullException>(() => new ArmyMovementState(null, At(0), 2));
            Assert.Throws<ArgumentException>(() => new ArmyMovementState(graph, At(1), 2));
            Assert.Throws<ArgumentException>(() => new ArmyMovementState(graph, At(5), 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmyMovementState(graph, At(0), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArmyMovementState(graph, At(0), -1));
        }
    }
}
