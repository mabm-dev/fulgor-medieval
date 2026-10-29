using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class ReachDestinationMissionTests
    {
        private static HexCoordinates At(int q) => new HexCoordinates(q, 0);
        private static GeneratedMap Map(params TerrainType[] terrain)
        {
            var cells = new MapCell[terrain.Length];
            for (int i = 0; i < terrain.Length; i++) cells[i] = new MapCell(At(i), terrain[i], false);
            return new GeneratedMap(terrain.Length, 1, 1, cells);
        }

        [Test]
        public void AutomaticDestinationIsDeterministicAndDoesNotCrossWater()
        {
            var map = Map(TerrainType.Plain, TerrainType.Forest, TerrainType.Water, TerrainType.Plain);
            var graph = new MapRouteGraph(map);
            var mission = ReachDestinationMission.Create(map, graph);
            Assert.That(mission.Spawn, Is.EqualTo(At(0)));
            Assert.That(mission.Destination, Is.EqualTo(At(1)));
            Assert.That(ReachDestinationMission.Create(map, graph).Destination, Is.EqualTo(mission.Destination));
        }

        [Test]
        public void InvalidExplicitDestinationsAreRejected()
        {
            var map = Map(TerrainType.Plain, TerrainType.Plain, TerrainType.Water, TerrainType.Plain);
            var graph = new MapRouteGraph(map);
            foreach (var target in new[] { At(0), At(2), At(3), At(99) })
                Assert.Throws<ArgumentException>(() => ReachDestinationMission.Create(map, graph, target));
            Assert.That(ReachDestinationMission.Create(map, graph, At(1)).Destination, Is.EqualTo(At(1)));
        }

        [Test]
        public void VictoryOnlyOnArrivalAndPreviousStateIsPreserved()
        {
            var map = Map(TerrainType.Plain, TerrainType.Plain, TerrainType.Plain);
            var mission = ReachDestinationMission.Create(map, new MapRouteGraph(map));
            Assert.That(mission.ObservePosition(At(1)).IsWon, Is.False);
            var won = mission.ObservePosition(At(2));
            Assert.That(won.IsWon, Is.True);
            Assert.That(mission.IsWon, Is.False);
            Assert.That(won.ObservePosition(At(0)).IsWon, Is.True);
        }

        [Test]
        public void EmptyWaterOrIsolatedSpawnDoNotCreateImpossibleAutomaticObjectives()
        {
            foreach (var map in new[] { Map(), Map(TerrainType.Water), Map(TerrainType.Plain),
                Map(TerrainType.Plain, TerrainType.Water, TerrainType.Plain) })
                Assert.That(ReachDestinationMission.Create(map, new MapRouteGraph(map)), Is.Null);
        }
    }
}
