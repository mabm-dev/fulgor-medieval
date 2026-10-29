using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignRivalTests
    {
        private GeneratedMap map;private MapRouteGraph routes;private CampaignSettlementRegistry owners;private CampaignArmyRegistry initial;private CampaignMilitaryState military;
        private static HexCoordinates At(int q,int r=0)=>new(q,r);
        private static CampaignResources Rich=>new(1000,1000,1000,1000,1000);
        private static CampaignArmyPlacement Place(string id,string faction,int q,int quantity=10,int movement=2)=>new(
            new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troop",TroopCatalog.Get(TroopProfile.Spearmen).Attributes,quantity*10,70)}),At(q),movement);
        [SetUp]public void Setup()
        {
            map=new GeneratedMap(8,2,1,Enumerable.Range(0,2).SelectMany(r=>Enumerable.Range(0,8).Select(q=>new MapCell(At(q,r),TerrainType.Plain,true))).ToArray());
            routes=new MapRouteGraph(map);owners=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("home","Casa","castilla",At(0)),new CampaignSettlement("rival","Rival","rival",At(7))});
            initial=new CampaignArmyRegistry(routes,new[]{Place("own","castilla",0),Place("enemy","rival",6)});military=new CampaignMilitaryState(initial,owners);
        }
        [Test]public void OpponentStartsEmptyAndEarnsOnlyItsTerritoryOncePerClosingTurn()
        {
            var state=new CampaignRivalState(true,new[]{"rival"});Assert.That(state.Resources("rival"),Is.EqualTo(default(CampaignResources)));
            var next=state.CloseTurn(2,map,owners,new Dictionary<string,CityDevelopment>());
            Assert.That(next.Resources("rival"),Is.EqualTo(CampaignIncomeCalculator.ForFaction(map,owners,"rival")));Assert.That(state.Resources("rival"),Is.EqualTo(default(CampaignResources)));
            Assert.Throws<InvalidOperationException>(()=>next.CloseTurn(2,map,owners,new Dictionary<string,CityDevelopment>()));
            Assert.Throws<InvalidOperationException>(()=>next.CloseTurn(4,map,owners,new Dictionary<string,CityDevelopment>()));
        }
        [Test]public void OpponentPaysSameCompletedCityUpkeepAndHasNoFallbackIncomeWithoutCities()
        {
            var city=CityDevelopment.Restore(new CityDevelopmentData{settlementId="rival",completed=new[]{"market"}},1);
            var state=new CampaignRivalState(true,new[]{"rival"});var next=state.CloseTurn(2,map,owners,new Dictionary<string,CityDevelopment>{{"rival",city}});
            var expected=CampaignIncomeCalculator.ForFaction(map,owners,"rival").Add(city.IncomePerTurn).SubtractClamped(city.UpkeepPerTurn);
            Assert.That(next.Resources("rival"),Is.EqualTo(expected));
            var none=new CampaignSettlementRegistry(routes,Array.Empty<CampaignSettlement>());
            Assert.That(state.CloseTurn(2,map,none,new Dictionary<string,CityDevelopment>()).Resources("rival"),Is.EqualTo(default(CampaignResources)));
        }
        [Test]public void PaidRecruitmentDoesNotTouchPlayerMoneyAndNeverCreatesInstantTroops()
        {
            var treasury=new CampaignRivalState(true,new[]{"rival"}).WithResources("rival",Rich);
            Assert.That(military.TryRecruit("rival","rival",TroopProfile.Spearmen,1,treasury.Resources("rival"),initial,out var pending,out var paid),Is.True);
            treasury=treasury.WithResources("rival",paid);Rich.TrySpend(RecruitmentCatalog.Get(TroopProfile.Spearmen).Cost,out var expected);
            Assert.That(treasury.Resources("rival"),Is.EqualTo(expected));Assert.That(pending.Garrison("rival").IsDissolved,Is.True);
            Assert.That(pending.Advance(2,initial).Garrison("rival").IsDissolved,Is.True);Assert.That(pending.Advance(3,initial).Garrison("rival").Formations.Single().Quantity,Is.EqualTo(20));
        }
        [Test]public void ConquestChangesFutureRivalIncomeWithoutAnImmediateResourceGrant()
        {
            var state=new CampaignRivalState(true,new[]{"rival"}).CloseTurn(2,map,owners,new Dictionary<string,CityDevelopment>());
            var invading=new CampaignArmyRegistry(routes,new[]{Place("enemy","rival",0)});owners.TryCapture(invading,"enemy","home",out var captured,out _);
            var next=state.CloseTurn(3,map,captured,new Dictionary<string,CityDevelopment>());
            Assert.That(next.Resources("rival"),Is.EqualTo(state.Resources("rival").Add(CampaignIncomeCalculator.FromMap(map))));
        }
        [Test]public void DisabledRivalsRemainEmptyAndLegacySavesOnlyLoadWithRivalsDisabled()
        {
            var state=new CampaignRivalState(false,new[]{"rival"}).CloseTurn(2,map,owners,new Dictionary<string,CityDevelopment>());
            Assert.That(state.Factions.Count,Is.Zero);var old=new CampaignSaveData{version=7,turn=4};Assert.That(old.RestoreRivals(false,new[]{"rival"}).ClosedTurn,Is.EqualTo(4));
            Assert.Throws<ArgumentException>(()=>old.RestoreRivals(true,new[]{"rival"}));
        }
        [TestCase("missing")][TestCase("duplicate")][TestCase("unknown")][TestCase("turn")][TestCase("mode")][TestCase("negative")]
        public void SavedTreasuriesRejectMissingForeignDuplicateOrInvalidState(string kind)
        {
            var data=new CampaignRivalState(true,new[]{"rival"}).WithResources("rival",Rich).Capture();
            if(kind=="missing")data.factions=Array.Empty<RivalFactionData>();if(kind=="duplicate")data.factions=new[]{data.factions[0],data.factions[0]};
            if(kind=="unknown")data.factions[0].factionId="invented";if(kind=="turn")data.closedTurn=2;if(kind=="mode")data.enabled=false;if(kind=="negative")data.factions[0].gold=-1;
            Assert.Catch<ArgumentException>(()=>CampaignRivalState.Restore(data,true,new[]{"rival"},1));
        }
        [Test]public void V8RoundTripKeepsBudgetAndAIModeInScenarioIdentity()
        {
            var state=new CampaignRivalState(true,new[]{"rival"}).WithResources("rival",Rich);var save=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(1,default,default),initial,"own","castilla");
            save.IncludeOwnership("base",owners,owners);save.IncludeMilitary(military);save.IncludeMarches(new CampaignMarchOrders());save.IncludeRivals(state);
            Assert.That(save.version,Is.EqualTo(8));save.RestoreMultiple(save.context,initial,"castilla",default,out _,out _,military);
            var loaded=save.RestoreRivals(true,new[]{"rival"});Assert.That(loaded.Resources("rival"),Is.EqualTo(Rich));save.rivals.factions[0].gold=1;
            Assert.That(loaded.Resources("rival").Gold,Is.EqualTo(1000));Assert.That(CampaignSaveData.RivalScenarioSignature("base",true),Is.Not.EqualTo(CampaignSaveData.RivalScenarioSignature("base",false)));
        }
        [Test]public void AdjacentRivalAttacksAnAcceptableTargetButHoldsAgainstOverwhelmingDefense()
        {
            var strong=new CampaignArmyRegistry(routes,new[]{Place("own","castilla",0,1),Place("enemy","rival",1,30)});
            var m=new CampaignMilitaryState(strong,owners);Assert.That(CampaignRivalPolicy.Decide("enemy","castilla",strong,m,owners).Kind,Is.EqualTo(RivalDecisionKind.Attack));
            var weak=new CampaignArmyRegistry(routes,new[]{Place("own","castilla",0,100),Place("enemy","rival",1,1)});
            Assert.That(CampaignRivalPolicy.Decide("enemy","castilla",weak,new CampaignMilitaryState(weak,owners),owners).Kind,Is.EqualTo(RivalDecisionKind.Wait));
        }
        [Test]public void PolicyIncludesGarrisonStrengthAndNeverActsForPlayerOrExhaustedArmy()
        {
            var armies=new CampaignArmyRegistry(routes,new[]{Place("own","castilla",0,1),Place("enemy","rival",1,10)});var m=new CampaignMilitaryState(armies,owners);
            m.TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,armies,out m,out _);m=m.Advance(3,armies);
            Assert.That(CampaignRivalPolicy.Decide("enemy","castilla",armies,m,owners).Kind,Is.EqualTo(RivalDecisionKind.Wait));
            Assert.That(CampaignRivalPolicy.Decide("own","castilla",armies,m,owners).Kind,Is.EqualTo(RivalDecisionKind.Wait));
            initial.TryMove("enemy",At(4),out var exhausted,out _,out _);Assert.That(CampaignRivalPolicy.Decide("enemy","castilla",exhausted,military,owners).Kind,Is.EqualTo(RivalDecisionKind.Wait));
        }
        [Test]public void PolicyPlansAnOrdinaryRouteToEmptyFortAndIsDeterministic()
        {
            var armies=new CampaignArmyRegistry(routes,new[]{Place("own","castilla",1,100),Place("enemy","rival",6,10)});var m=new CampaignMilitaryState(armies,owners);
            var one=CampaignRivalPolicy.Decide("enemy","castilla",armies,m,owners);var two=CampaignRivalPolicy.Decide("enemy","castilla",armies,m,owners);
            Assert.That(one.Kind,Is.EqualTo(RivalDecisionKind.Move));Assert.That(one.Target,Is.EqualTo(At(0)));Assert.That(two.Target,Is.EqualTo(one.Target));
            var book=new CampaignMarchOrders();book.TryOrder("enemy",one.Target,armies,m,"rival",out book,out _,out _);book.AdvanceArmy("enemy",armies,m,"rival",out var moved,out var step);
            moved.TryGetArmy("enemy",out var enemy);Assert.That(step.Travelled.TotalCost,Is.LessThanOrEqualTo(2));Assert.That(enemy.Movement.Position,Is.Not.EqualTo(one.Target));Assert.That(moved.HasActiveEncounters,Is.False);
        }
    }
}
