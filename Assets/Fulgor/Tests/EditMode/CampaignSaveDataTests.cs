using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignSaveDataTests
    {
        private static HexCoordinates At(int q) => new HexCoordinates(q, 0);
        private static GeneratedMap Map(long seed = 1) => new GeneratedMap(2, 1, seed, new[] {
            new MapCell(At(0), TerrainType.Plain, false), new MapCell(At(1), TerrainType.Forest, false) });
        private static CampaignSaveData Payload()
        {
            var graph = new MapRouteGraph(Map());
            var army = new ArmyMovementState(graph, At(0), 6);
            army.TryMove(At(1), out var moved, out _, out _);
            return CampaignSaveData.Capture("scenario", new CampaignTurnState(4,
                new CampaignResources(10, 9, 8, 7, 6), default), moved);
        }

        [Test]
        public void CaptureRestorePreservesPositionSpentMovementTurnAndResources()
        {
            var payload = Payload();
            var income = new CampaignResources(2, 0, 0, 0, 0);
            payload.Restore("scenario", new MapRouteGraph(Map()), At(0), income, 6, out var state, out var army);
            Assert.That(state.Turn, Is.EqualTo(4));
            Assert.That(state.Resources, Is.EqualTo(new CampaignResources(10, 9, 8, 7, 6)));
            Assert.That(state.CompleteTurn().Resources.Grain, Is.EqualTo(12));
            Assert.That(army.Position, Is.EqualTo(At(1)));
            Assert.That(army.RemainingMovement, Is.EqualTo(4));
        }

        [Test]
        public void WrongScenarioOrVersionIsRejected()
        {
            var payload = Payload(); var graph = new MapRouteGraph(Map());
            Assert.Throws<ArgumentException>(() => payload.Restore("other", graph, At(0), default, 6, out _, out _));
            payload.version = 0;
            Assert.Throws<ArgumentException>(() => payload.Restore("scenario", graph, At(0), default, 6, out _, out _));
        }

        [Test]
        public void InvalidResourcesTurnPositionAndMovementAreRejected()
        {
            var graph = new MapRouteGraph(Map());
            foreach (var change in new Action<CampaignSaveData>[] {
                p => p.grain = -1, p => p.turn = 0, p => p.q = 99,
                p => p.remainingMovement = -1, p => p.remainingMovement = 7, p => p.movementPerTurn = 99 })
            {
                var payload = Payload(); change(payload);
                Assert.Catch<ArgumentException>(() => payload.Restore("scenario", graph, At(0), default, 6, out _, out _));
            }
        }

        [Test]
        public void SignatureDistinguishesSeedRoutesAndObjective()
        {
            var map = Map(); var graph = new MapRouteGraph(map);
            var signature = CampaignSaveData.ScenarioSignature(map, graph, null);
            Assert.That(CampaignSaveData.ScenarioSignature(Map(), new MapRouteGraph(Map()), null), Is.EqualTo(signature));
            Assert.That(CampaignSaveData.ScenarioSignature(Map(2), graph, null), Is.Not.EqualTo(signature));
            var road = new MapRouteGraph(map, new[] { new MapRouteSegment(At(0), At(1), MapRouteKind.Road) });
            Assert.That(CampaignSaveData.ScenarioSignature(map, road, null), Is.Not.EqualTo(signature));
            Assert.That(CampaignSaveData.ScenarioSignature(map, graph, ReachDestinationMission.Create(map, graph)), Is.Not.EqualTo(signature));
        }

        [Test]
        public void LoadingAtDestinationRestoresVictoryFromInitialObjective()
        {
            var map = Map(); var graph = new MapRouteGraph(map);
            var mission = ReachDestinationMission.Create(map, graph);
            Payload().Restore("scenario", graph, At(0), default, 6, out _, out var army);
            Assert.That(mission.ObservePosition(army.Position).IsWon, Is.True);
            Assert.That(mission.IsWon, Is.False);
        }
    }
}
