using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignFogTests
    {
        private GeneratedMap map;private MapRouteGraph graph;private CampaignSettlementRegistry owners;private CampaignArmyRegistry armies;
        private static HexCoordinates At(int q,int r=1)=>new(q,r);
        private static readonly string[] Factions={"castilla","rival","other"};
        private CampaignArmyPlacement Army(string id,string faction,int q)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",TroopCatalog.Get(TroopProfile.Spearmen).Attributes,100,70)}),At(q),6);
        [SetUp]public void Setup()
        {
            map=new GeneratedMap(14,3,1,Enumerable.Range(0,3).SelectMany(r=>Enumerable.Range(0,14).Select(q=>new MapCell(At(q,r),TerrainType.Plain,false))).ToArray());graph=new MapRouteGraph(map);
            owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(0)),new CampaignSettlement("enemy","Rival","rival",At(13))});
            armies=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",3),Army("enemy","rival",12)});
        }
        private CampaignFogState Fresh()=>new CampaignFogState(map,Factions).Refresh(armies,owners,1);
        [Test]public void CitiesAndArmiesRevealOnlyRadiusTwoForTheirOwnFaction()
        {
            var fog=Fresh();Assert.That(fog.IsVisible("castilla",At(5)),Is.True);Assert.That(fog.Visibility("castilla",At(6)),Is.EqualTo(CampaignVisibility.Unknown));
            Assert.That(fog.IsVisible("rival",At(10)),Is.True);Assert.That(fog.Visibility("rival",At(3)),Is.EqualTo(CampaignVisibility.Unknown));
            Assert.That(fog.ExploredCount("other"),Is.Zero);Assert.That(fog.Intel("castilla","enemy"),Is.Null);Assert.That(fog.Intel("castilla","home").OwnerFactionId,Is.EqualTo("castilla"));
        }
        [Test]public void KnownFrontiersExpandAndPreviouslyVisitedGroundRemainsExplored()
        {
            var fog=Fresh();var military=new CampaignMilitaryState(armies,owners);
            var first=CampaignFogOrders.Advance("own",At(5),armies,military,owners,graph,fog,1);Assert.That(first.Arrived,Is.True);Assert.That(first.Travelled.TotalCost,Is.EqualTo(2));
            Assert.That(first.Fog.IsExplored("castilla",At(7)),Is.True);Assert.That(fog.IsExplored("castilla",At(7)),Is.False);
            var second=CampaignFogOrders.Advance("own",At(7),first.Armies,military,owners,graph,first.Fog,1);
            Assert.That(second.Fog.Visibility("castilla",At(3)),Is.EqualTo(CampaignVisibility.Explored));second.Armies.TryGetArmy("own",out var own);Assert.That(own.Movement.RemainingMovement,Is.EqualTo(2));
        }
        [Test]public void UnexploredTargetsCannotProduceOrdersOrRoutes()
        {
            var fog=Fresh();var military=new CampaignMilitaryState(armies,owners);
            Assert.That(CampaignFogOrders.TryPlan("own",At(8),armies,military,owners,graph,fog,out var path,out _),Is.False);Assert.That(path,Is.Null);
            Assert.That(new CampaignMarchOrders().TryKnownOrder("own",At(8),armies,military,owners,graph,fog,"castilla",out var orders),Is.False);Assert.That(orders.Count,Is.Zero);
        }
        [Test]public void HiddenOccupationDoesNotChangeThePreviewAndBecomesKnownBeforeMovementStops()
        {
            var hidden=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",3),Army("enemy","rival",7)});
            var memory=new CampaignFogState(map,Factions).LegacyKnowledge(owners,1).Refresh(hidden,owners,1);
            var emptyMemory=new CampaignFogState(map,Factions).LegacyKnowledge(owners,1).Refresh(armies,owners,1);
            var military=new CampaignMilitaryState(hidden,owners);
            Assert.That(CampaignFogOrders.TryPlan("own",At(7),hidden,military,owners,graph,memory,out var first,out _),Is.True);
            Assert.That(CampaignFogOrders.TryPlan("own",At(7),armies,new CampaignMilitaryState(armies,owners),owners,graph,emptyMemory,out var second,out _),Is.True);
            CollectionAssert.AreEqual(first.Cells,second.Cells);Assert.That(first.TotalCost,Is.EqualTo(second.TotalCost));
            var moved=CampaignFogOrders.Advance("own",At(7),hidden,military,owners,graph,memory,1);moved.Armies.TryGetArmy("own",out var own);
            Assert.That(own.Movement.Position,Is.EqualTo(At(5)));Assert.That(own.Movement.RemainingMovement,Is.EqualTo(4));Assert.That(moved.Fog.IsVisible("castilla",At(7)),Is.True);Assert.That(moved.Arrived,Is.False);Assert.That(moved.Armies.HasActiveEncounters,Is.False);
        }
        [Test]public void CityReportsDoNotUpdateUnseenEnemyOwnership()
        {
            var memory=new CampaignFogState(map,Factions).LegacyKnowledge(owners,1).Refresh(armies,owners,1);
            var changed=new CampaignSettlementRegistry(graph,new[]{owners.Settlements[0],new CampaignSettlement("enemy","Rival","other",At(13))});
            var next=memory.Refresh(armies,changed,2);Assert.That(next.Intel("castilla","enemy").OwnerFactionId,Is.EqualTo("rival"));Assert.That(next.Intel("castilla","enemy").LastSeenTurn,Is.EqualTo(1));
            Assert.That(next.Intel("rival","enemy").OwnerFactionId,Is.EqualTo("other"));Assert.That(next.Intel("rival","enemy").LastSeenTurn,Is.EqualTo(2));
            Assert.DoesNotThrow(()=>CampaignFogState.Restore(next.Capture(),map,Factions,armies,changed,2));
        }
        [Test]public void LossOfOwnCityIsReportedWithoutCreatingRemoteVision()
        {
            var fog=Fresh();var captured=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",At(0)),owners.Settlements[1]});
            var next=fog.Refresh(armies,captured,2);Assert.That(next.Visibility("castilla",At(0)),Is.EqualTo(CampaignVisibility.Explored));Assert.That(next.Intel("castilla","home").OwnerFactionId,Is.EqualTo("rival"));
            Assert.That(next.Intel("castilla","home").LastSeenTurn,Is.EqualTo(2));Assert.That(next.IsVisible("castilla",At(3)),Is.True);
        }
        [Test]public void RoundTripPreservesKnowledgeAndRecomputesVisibilityWithoutAdvancingAnything()
        {
            var fog=Fresh();var copy=CampaignFogState.Restore(fog.Capture(),map,Factions,armies,owners,1);
            foreach(string faction in Factions)foreach(var cell in map.Cells)Assert.That(copy.Visibility(faction,cell.Coordinates),Is.EqualTo(fog.Visibility(faction,cell.Coordinates)));
            Assert.That(copy.ExploredCount("castilla"),Is.EqualTo(fog.ExploredCount("castilla")));
        }
        [TestCase("missing")][TestCase("faction")][TestCase("count")][TestCase("outside")][TestCase("duplicate")][TestCase("source")]
        [TestCase("city")][TestCase("owner")][TestCase("future")][TestCase("current_owner")][TestCase("version")]
        public void CorruptExplorationIsRejected(string damage)
        {
            var data=Fresh().Capture();var own=data.factions.Single(f=>f.factionId=="castilla");
            if(damage=="missing")data.factions=null;
            if(damage=="faction")data.factions[1].factionId="castilla";
            if(damage=="count")own.exploredCount++;
            if(damage=="outside")own.explored[0].q=1000;
            if(damage=="duplicate"){own.explored[0].q=own.explored[1].q;own.explored[0].r=own.explored[1].r;}
            if(damage=="source"){own.explored=Array.Empty<FogCellData>();own.exploredCount=0;own.settlements=Array.Empty<FogSettlementData>();own.settlementCount=0;}
            if(damage=="city")own.settlements[0].settlementId="unknown";
            if(damage=="owner")own.settlements[0].ownerFactionId="unknown";
            if(damage=="future")own.settlements[0].lastSeenTurn=2;
            if(damage=="current_owner")own.settlements[0].ownerFactionId="rival";
            if(damage=="version")data.rulesVersion=2;
            Assert.Throws<ArgumentException>(()=>CampaignFogState.Restore(data,map,Factions,armies,owners,1));
        }
        [Test]public void LegacyCampaignRetainsItsPreviouslyOmniscientMapButHidesCurrentEnemies()
        {
            var payload=new CampaignSaveData{version=10,turn=8};var fog=payload.RestoreFog(true,map,Factions,armies,owners);
            Assert.That(fog.ExploredCount("castilla"),Is.EqualTo(map.Cells.Count));Assert.That(fog.Visibility("castilla",At(12)),Is.EqualTo(CampaignVisibility.Explored));Assert.That(fog.Intel("castilla","enemy").OwnerFactionId,Is.EqualTo("rival"));
            payload.version=11;payload.multipleArmies=true;Assert.Throws<ArgumentException>(()=>payload.RestoreFog(true,map,Factions,armies,owners));
        }
        [Test]public void SavedUnknownDestinationsAreRejectedEvenWhenTheRealMapHasARoute()
        {
            var military=new CampaignMilitaryState(armies,owners);var orders=new CampaignMarchOrders();Assert.That(orders.TryOrder("own",At(8),armies,military,"castilla",out orders,out _,out _),Is.True);
            Assert.Throws<ArgumentException>(()=>Fresh().ValidateOrders(orders,armies,"castilla",graph));
        }
        [Test]public void RivalDecisionsAreIndependentOfUnseenPlayerPositionsAndExploreKnownFrontiers()
        {
            var alternative=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",5),Army("enemy","rival",12)});
            var fog=Fresh();var otherFog=new CampaignFogState(map,Factions).Refresh(alternative,owners,1);
            var first=CampaignRivalPolicy.DecideWithFog("enemy","castilla",armies,new CampaignMilitaryState(armies,owners),owners,graph,fog);
            var second=CampaignRivalPolicy.DecideWithFog("enemy","castilla",alternative,new CampaignMilitaryState(alternative,owners),owners,graph,otherFog);
            Assert.That(first.Kind,Is.EqualTo(RivalDecisionKind.Move));Assert.That(second.Kind,Is.EqualTo(first.Kind));Assert.That(second.Target,Is.EqualTo(first.Target));Assert.That(fog.IsVisible("rival",first.Target),Is.True);
        }
    }
}
