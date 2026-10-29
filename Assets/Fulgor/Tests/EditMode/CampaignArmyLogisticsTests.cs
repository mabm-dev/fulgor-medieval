using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignArmyLogisticsTests
    {
        private MapRouteGraph graph;private CampaignSettlementRegistry owners;private CampaignArmyRegistry initial;
        private static HexCoordinates At(int q)=>new(q,0);
        private static CampaignResources Rich=>new(1000,1000,1000,1000,1000);
        private static CampaignFormationState Troop(string id,long hp=100,int morale=70,TroopProfile profile=TroopProfile.Spearmen)
        {var p=TroopCatalog.Get(profile);return new CampaignFormationState(id,p.Attributes,hp,morale,p.Kind);}
        private static CampaignArmyPlacement Army(string id,string faction,int q,params CampaignFormationState[] f)=>new(new CampaignArmyState(id,faction,f),At(q),6);
        private CampaignMilitaryState Military=>new(initial,owners);
        private static Dictionary<string,CampaignResources> Funds(CampaignResources own)=>new(StringComparer.Ordinal){["castilla"]=own,["rival"]=Rich};
        [SetUp]public void Setup()
        {
            graph=new MapRouteGraph(new GeneratedMap(14,1,1,Enumerable.Range(0,14).Select(q=>new MapCell(At(q),TerrainType.Plain,false)).ToArray()));
            owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(0)),new CampaignSettlement("enemy","Rival","rival",At(13))});
            initial=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",1,Troop("a")),Army("enemy","rival",12,Troop("b"))});
        }
        private CampaignArmyRegistry Wounded(long health,int morale=40,int movement=6)
        {
            var data=initial.Capture();var own=data.armies.Single(a=>a.id=="own");own.formations[0].health=health;own.formations[0].morale=morale;own.remainingMovement=movement;
            return CampaignArmyRegistry.Restore(data,initial);
        }
        [TestCase(1,1,1)][TestCase(20,1,1)][TestCase(21,2,1)][TestCase(25,2,1)][TestCase(26,2,2)][TestCase(50,3,2)]
        public void FoodAndPayRoundUpByFormation(int quantity,int grain,int gold)
        {var cost=CampaignArmyLogistics.Upkeep(new CampaignArmyState("a","castilla",new[]{Troop("f",quantity*10L)}));Assert.That(cost.Grain,Is.EqualTo(grain));Assert.That(cost.Gold,Is.EqualTo(gold));}
        [Test]public void HorsesDoubleFoodAndNoOtherResourcesAreCharged()
        {
            var cost=CampaignArmyLogistics.Upkeep(new CampaignArmyState("a","castilla",new[]{Troop("horse",25*12L,70,TroopProfile.LightCavalry)}));
            Assert.That(cost,Is.EqualTo(new CampaignResources(4,0,0,1,0)));
        }
        [Test]public void PaidRestHealsWoundedSurvivorsAndMoraleWithoutReplacingCasualties()
        {
            var wounded=Wounded(83);var before=wounded.Capture();var result=CampaignArmyLogistics.CloseTurn(wounded,Military,owners,graph,Funds(Rich));
            result.Armies.TryGetArmy("own",out var own);var f=own.Army.Formations.Single();Assert.That(f.Health,Is.EqualTo(85));Assert.That(f.Quantity,Is.EqualTo(9));Assert.That(f.Morale,Is.EqualTo(45));
            Assert.That(result.Resources("castilla"),Is.EqualTo(new CampaignResources(999,1000,1000,999,1000)));Assert.That(own.Movement.RemainingMovement,Is.EqualTo(6));
            Assert.That(wounded.Capture().armies[1].formations[0].health,Is.EqualTo(before.armies[1].formations[0].health));
            var again=result;
            for(int i=0;i<20;i++)again=CampaignArmyLogistics.CloseTurn(again.Armies,again.Military,owners,graph,Funds(Rich));
            again.Armies.TryGetArmy("own",out own);Assert.That(own.Army.Formations.Single().Health,Is.EqualTo(90));Assert.That(own.Army.Formations.Single().Quantity,Is.EqualTo(9));Assert.That(own.Army.Formations.Single().Morale,Is.EqualTo(70));
        }
        [Test]public void MovingArmyPaysAndRefillsMovementButDoesNotRecover()
        {
            var result=CampaignArmyLogistics.CloseTurn(Wounded(83,40,5),Military,owners,graph,Funds(Rich));result.Armies.TryGetArmy("own",out var own);
            Assert.That(own.Army.Formations.Single().Health,Is.EqualTo(83));Assert.That(own.Army.Formations.Single().Morale,Is.EqualTo(40));Assert.That(own.Movement.RemainingMovement,Is.EqualTo(6));Assert.That(result.Reports.Single(r=>r.ArmyId=="own").Rested,Is.False);
        }
        [TestCase(0,100)][TestCase(100,0)][TestCase(0,0)]public void AnyUnpaidMilitaryRequirementPenalizesWithoutDebtOrDeaths(int food,int gold)
        {
            var result=CampaignArmyLogistics.CloseTurn(Wounded(83,4),Military,owners,graph,Funds(new CampaignResources(food,5,7,gold,9)));result.Armies.TryGetArmy("own",out var own);
            Assert.That(own.Army.Formations.Single().Health,Is.EqualTo(83));Assert.That(own.Army.Formations.Single().Morale,Is.Zero);Assert.That(own.Movement.RemainingMovement,Is.EqualTo(3));
            Assert.That(result.Resources("castilla"),Is.EqualTo(new CampaignResources(Math.Max(0,food-1),5,7,Math.Max(0,gold-1),9)));
            Assert.That(result.Reports.Single(r=>r.ArmyId=="own").Paid,Is.False);
        }
        [Test]public void SupplyRangeUsesLandCostsAndStopsAtHostileArmyOrSettlement()
        {
            var network=CampaignArmyLogistics.SupplyNetwork(graph,initial,owners,"castilla");Assert.That(network.Contains(At(6)),Is.True);Assert.That(network.Contains(At(7)),Is.False);
            var blocked=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",6,Troop("a")),Army("enemy","rival",3,Troop("b"))});
            network=CampaignArmyLogistics.SupplyNetwork(graph,blocked,owners,"castilla");Assert.That(network.Contains(At(2)),Is.True);Assert.That(network.Contains(At(3)),Is.False);Assert.That(network.Contains(At(6)),Is.False);
            var captured=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",At(0)),new CampaignSettlement("enemy","Rival","rival",At(13))});
            Assert.That(CampaignArmyLogistics.SupplyNetwork(graph,initial,captured,"castilla"),Is.Empty);
        }
        [Test]public void WaterBreaksSupplyAndExplicitRoadsExtendItThroughRoughTerrain()
        {
            var hills=new GeneratedMap(8,1,1,Enumerable.Range(0,8).Select(q=>new MapCell(At(q),TerrainType.Hill,false)).ToArray());
            var hillGraph=new MapRouteGraph(hills);var hillOwners=new CampaignSettlementRegistry(hillGraph,new[]{new CampaignSettlement("home","Casa","castilla",At(0))});
            Assert.That(CampaignArmyLogistics.SupplyNetwork(hillGraph,initial,hillOwners,"castilla").Contains(At(5)),Is.False);
            var roads=new MapRouteGraph(hills,Enumerable.Range(0,7).Select(q=>new MapRouteSegment(At(q),At(q+1),MapRouteKind.Road)));
            Assert.That(CampaignArmyLogistics.SupplyNetwork(roads,initial,hillOwners,"castilla").Contains(At(6)),Is.True);
            var water=new MapRouteGraph(new GeneratedMap(8,1,1,Enumerable.Range(0,8).Select(q=>new MapCell(At(q),q==3?TerrainType.Water:TerrainType.Plain,false)).ToArray()));
            Assert.That(CampaignArmyLogistics.SupplyNetwork(water,initial,hillOwners,"castilla").Contains(At(4)),Is.False);
        }
        [Test]public void SuppliedDistanceAndPaymentAreIndependentAndRivalsUseTheirOwnTreasury()
        {
            var distant=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",8,Troop("a",83,40)),Army("enemy","rival",12,Troop("b",83,40))});
            var mil=new CampaignMilitaryState(distant,owners);var result=CampaignArmyLogistics.CloseTurn(distant,mil,owners,graph,Funds(Rich));
            result.Armies.TryGetArmy("own",out var own);result.Armies.TryGetArmy("enemy",out var rival);
            Assert.That(own.Army.Formations.Single().Morale,Is.EqualTo(30));Assert.That(own.Movement.RemainingMovement,Is.EqualTo(3));
            Assert.That(rival.Army.Formations.Single().Morale,Is.EqualTo(45));Assert.That(result.Resources("rival").Gold,Is.EqualTo(999));
            Assert.That(result.Reports.Single(r=>r.ArmyId=="own").Supplied,Is.False);Assert.That(result.Reports.Single(r=>r.ArmyId=="own").Paid,Is.True);
        }
        [Test]public void GarrisonCostsSameAfterTransferAndRecoversInsideItsCity()
        {
            var source=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",0,Troop("a")),Army("enemy","rival",12,Troop("b"))});var mil=new CampaignMilitaryState(source,owners);
            var data=source.Capture();data.armies.Single(a=>a.id=="own").formations[0].health=83;data.armies.Single(a=>a.id=="own").formations[0].morale=40;var wounded=CampaignArmyRegistry.Restore(data,source);
            var before=CampaignArmyLogistics.UpkeepForFaction(wounded,mil,owners,"castilla");Assert.That(mil.TryDeposit("home","own","a",wounded,out var guarded,out var fields),Is.True);
            Assert.That(CampaignArmyLogistics.UpkeepForFaction(fields,guarded,owners,"castilla"),Is.EqualTo(before));
            var result=CampaignArmyLogistics.CloseTurn(fields,guarded,owners,graph,Funds(Rich));Assert.That(result.Military.Garrison("home").Formations.Single().Health,Is.EqualTo(85));
            Assert.That(result.Reports.Single(r=>r.IsGarrison).Paid,Is.True);result.Military.ValidateDeployment(result.Armies);
        }
        [Test]public void NewLevyIsBilledStartingOnItsFirstFullTurn()
        {
            var mil=Military;Assert.That(mil.TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,initial,out var pending,out _),Is.True);
            var close=CampaignArmyLogistics.CloseTurn(initial,pending,owners,graph,Funds(Rich));Assert.That(close.Reports.Count(r=>r.FactionId=="castilla"),Is.EqualTo(1));
            var completed=close.Military.Advance(3,close.Armies);Assert.That(CampaignArmyLogistics.UpkeepForFaction(close.Armies,completed,owners,"castilla").Gold,Is.EqualTo(2));
        }
        [Test]public void ActiveBattleRejectsAccountingBeforeAnyTreasuryOrSurvivorMutation()
        {
            var near=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",1,Troop("a")),Army("enemy","rival",2,Troop("b"))});var military=new CampaignMilitaryState(near,owners);
            Assert.That(near.TryBeginAttack("battle","own","enemy",1,out var fighting,out _),Is.True);var funds=Funds(Rich);
            Assert.Throws<InvalidOperationException>(()=>CampaignArmyLogistics.CloseTurn(fighting,military,owners,graph,funds));Assert.That(funds["castilla"],Is.EqualTo(Rich));
        }
        [Test]public void LogisticsV10IsExplicitRejectsOtherRulesAndAllowsOnlyV9Migration()
        {
            var data=new CampaignSaveData{version=9,context="prior",multipleArmies=true};data.IncludeLogistics(true);Assert.That(data.version,Is.EqualTo(10));Assert.That(data.logisticsVersion,Is.EqualTo(1));
            Assert.DoesNotThrow(()=>data.RestoreLogistics(true));Assert.Throws<ArgumentException>(()=>data.RestoreLogistics(false));data.logisticsVersion=2;Assert.Throws<ArgumentException>(()=>data.RestoreLogistics(true));
            var legacy=new CampaignSaveData{version=9,multipleArmies=true};Assert.DoesNotThrow(()=>legacy.RestoreLogistics(true));legacy.version=8;Assert.Throws<ArgumentException>(()=>legacy.RestoreLogistics(true));
        }
    }
}
