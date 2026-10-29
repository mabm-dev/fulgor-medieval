using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;
namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignRivalRecoveryTests
    {
        [TestCase(20,0,true,true,RivalDecisionKind.Wait)]
        [TestCase(20,2,true,true,RivalDecisionKind.Move)]
        [TestCase(40,2,true,false,RivalDecisionKind.Wait)]
        [TestCase(20,2,false,false,RivalDecisionKind.Wait)]
        public void LowMoraleReturnsToOwnCityAndRestsOnlyWhenPaid(int morale,int position,bool paid,bool expected,RivalDecisionKind kind)
        {
            var map=new GeneratedMap(6,1,1,Enumerable.Range(0,6).Select(q=>new MapCell(new HexCoordinates(q,0),TerrainType.Plain,false)).ToArray());var graph=new MapRouteGraph(map);
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",new HexCoordinates(0,0))});
            var army=new CampaignArmyState("army","rival",new[]{new CampaignFormationState("troops",TroopCatalog.Get(TroopProfile.Spearmen).Attributes,100,morale)});
            var armies=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(army,new HexCoordinates(position,0),6)});var military=new CampaignMilitaryState(armies,owners);
            var fog=new CampaignFogState(map,new[]{"rival"}).Refresh(armies,owners,1);
            Assert.That(CampaignRivalRecovery.TryDecide("army",armies,military,owners,graph,fog,paid,out var result),Is.EqualTo(expected));
            Assert.That(result.Kind,Is.EqualTo(kind));if(kind==RivalDecisionKind.Move)Assert.That(result.Target,Is.EqualTo(new HexCoordinates(0,0)));
        }

        [TestCase(0,0,false)]
        [TestCase(0,1,true)]
        [TestCase(0,10,false)]
        [TestCase(0,11,true)]
        [TestCase(1,1,false)]
        [TestCase(1,2,true)]
        public void RecoveryCountsOnlyWoundsOfSurvivingSoldiers(int initialLoss,int currentLoss,bool expected)
        {
            var map=new GeneratedMap(3,1,1,Enumerable.Range(0,3).Select(q=>new MapCell(new HexCoordinates(q,0),TerrainType.Plain,false)).ToArray());var graph=new MapRouteGraph(map);
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",new HexCoordinates(0,0))});
            var attributes=TroopCatalog.Get(TroopProfile.Spearmen).Attributes;
            // Loss of ten tenths is exactly one dead member, with no remaining wound.
            long full=attributes.HealthPerMember*10L;
            long Loss(int tenths)=>tenths==10?attributes.HealthPerMember:tenths==11?attributes.HealthPerMember+1:tenths;
            CampaignArmyRegistry Registry(long health)=>new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(new CampaignArmyState("army","rival",new[]{new CampaignFormationState("troops",attributes,health,70)}),new HexCoordinates(0,0),6)});
            var initial=Registry(full-Loss(initialLoss));var military=new CampaignMilitaryState(initial,owners);var current=Registry(full-Loss(currentLoss));
            var fog=new CampaignFogState(map,new[]{"rival"}).Refresh(current,owners,1);
            Assert.That(CampaignRivalRecovery.TryDecide("army",current,military,owners,graph,fog,true,out var decision),Is.EqualTo(expected));
            Assert.That(decision.Kind,Is.EqualTo(RivalDecisionKind.Wait));
            if(expected)
            {
                var funds=new System.Collections.Generic.Dictionary<string,CampaignResources>{{"rival",new CampaignResources(100,0,0,100,0)}};
                var recovered=CampaignArmyLogistics.CloseTurn(current,military,owners,graph,funds);
                current.TryGetArmy("army",out var before);recovered.Armies.TryGetArmy("army",out var after);
                Assert.That(after.Army.Formations[0].Quantity,Is.EqualTo(before.Army.Formations[0].Quantity));
                Assert.That(after.Army.Formations[0].Health,Is.GreaterThan(before.Army.Formations[0].Health));
                Assert.That(CampaignRivalRecovery.TryDecide("army",recovered.Armies,recovered.Military,owners,graph,fog,true,out _),Is.False);
            }
        }

        [TestCase(true,false,false,false,false)]
        [TestCase(true,true,false,true,true)]
        [TestCase(false,false,false,true,false)]
        [TestCase(true,true,true,false,false)]
        public void RecoveryAvoidsObservedEnemiesAtRefuge(bool hostile,bool alternative,bool secondThreat,bool expected,bool moves)
        {
            var map=new GeneratedMap(7,3,1,Enumerable.Range(0,3).SelectMany(r=>Enumerable.Range(0,7).Select(q=>new MapCell(new HexCoordinates(q,r),TerrainType.Plain,false))).ToArray());var graph=new MapRouteGraph(map);
            var cities=new System.Collections.Generic.List<CampaignSettlement>{new CampaignSettlement("home","Casa","rival",new HexCoordinates(0,1))};
            if(alternative)cities.Add(new CampaignSettlement("refuge","Refugio","rival",new HexCoordinates(4,1)));
            var owners=new CampaignSettlementRegistry(graph,cities);var attributes=TroopCatalog.Get(TroopProfile.Spearmen).Attributes;
            CampaignArmyPlacement Unit(string id,string faction,int q,int r,int morale)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",attributes,100,morale)}),new HexCoordinates(q,r),6);
            var units=new System.Collections.Generic.List<CampaignArmyPlacement>{Unit("own","rival",0,1,20),Unit("near",hostile?"player":"rival",0,0,70)};
            if(secondThreat)units.Add(Unit("other","player",4,0,70));
            var armies=new CampaignArmyRegistry(graph,units);var military=new CampaignMilitaryState(armies,owners);
            var fog=new CampaignFogState(map,new[]{"rival","player"}).Refresh(armies,owners,1);
            Assert.That(CampaignRivalRecovery.TryDecide("own",armies,military,owners,graph,fog,true,out var decision),Is.EqualTo(expected));
            Assert.That(decision.Kind,Is.EqualTo(moves?RivalDecisionKind.Move:RivalDecisionKind.Wait));
            if(moves)Assert.That(decision.Target,Is.EqualTo(new HexCoordinates(4,1)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RecoveryPlansAndExecutesDetourAroundObservedEnemy(bool fogEnabled)
        {
            var map=new GeneratedMap(7,5,1,Enumerable.Range(0,5).SelectMany(r=>Enumerable.Range(0,7).Select(q=>new MapCell(new HexCoordinates(q,r),TerrainType.Plain,false))).ToArray());var graph=new MapRouteGraph(map);
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",new HexCoordinates(5,2))});
            var attrs=TroopCatalog.Get(TroopProfile.Spearmen).Attributes;
            CampaignArmyPlacement Unit(string id,string faction,int q,int r)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",attrs,100,20)}),new HexCoordinates(q,r),20);
            var armies=new CampaignArmyRegistry(graph,new[]{Unit("own","rival",0,2),Unit("enemy","player",2,2)});var military=new CampaignMilitaryState(armies,owners);
            var fog=fogEnabled?new CampaignFogState(map,new[]{"rival","player"}).LegacyKnowledge(owners,1).Refresh(armies,owners,1):null;
            Assert.That(CampaignFogOrders.TryPlan("own",new HexCoordinates(5,2),armies,military,owners,graph,fog,out var path,out _,true),Is.True);
            Assert.That(path.Cells.Skip(1).All(c=>c.DistanceTo(new HexCoordinates(2,2))>1),Is.True);
            var moved=CampaignFogOrders.Advance("own",new HexCoordinates(5,2),armies,military,owners,graph,fog,1,null,true);
            Assert.That(moved.Arrived,Is.True);Assert.That(moved.Travelled.Cells.Skip(1).All(c=>c.DistanceTo(new HexCoordinates(2,2))>1),Is.True);
            moved.Armies.TryGetArmy("own",out var after);Assert.That(after.Movement.RemainingMovement,Is.EqualTo(20-moved.Travelled.TotalCost));
        }

        [Test]public void HiddenEnemyDoesNotAlterForecastButIsAvoidedWhenDiscovered()
        {
            var map=new GeneratedMap(10,5,1,Enumerable.Range(0,5).SelectMany(r=>Enumerable.Range(0,10).Select(q=>new MapCell(new HexCoordinates(q,r),TerrainType.Plain,false))).ToArray());var graph=new MapRouteGraph(map);
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","rival",new HexCoordinates(8,2))});var attrs=TroopCatalog.Get(TroopProfile.Spearmen).Attributes;
            CampaignArmyPlacement Unit(string id,string faction,int q)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",attrs,100,20)}),new HexCoordinates(q,2),20);
            var own=Unit("own","rival",0);var enemy=Unit("enemy","player",4);
            var empty=new CampaignArmyRegistry(graph,new[]{own});var occupied=new CampaignArmyRegistry(graph,new[]{own,enemy});
            var military=new CampaignMilitaryState(occupied,owners);var emptyMilitary=new CampaignMilitaryState(empty,owners);
            var fog=new CampaignFogState(map,new[]{"rival","player"}).LegacyKnowledge(owners,1).Refresh(occupied,owners,1);
            Assert.That(fog.IsVisible("rival",new HexCoordinates(4,2)),Is.False);
            Assert.That(CampaignFogOrders.TryPlan("own",new HexCoordinates(8,2),empty,emptyMilitary,owners,graph,fog,out var a,out _,true),Is.True);
            Assert.That(CampaignFogOrders.TryPlan("own",new HexCoordinates(8,2),occupied,military,owners,graph,fog,out var b,out _,true),Is.True);
            Assert.That(b.Cells,Is.EqualTo(a.Cells));Assert.That(b.TotalCost,Is.EqualTo(a.TotalCost));
            var moved=CampaignFogOrders.Advance("own",new HexCoordinates(8,2),occupied,military,owners,graph,fog,1,null,true);
            Assert.That(moved.Arrived,Is.True);Assert.That(moved.Travelled.Cells.All(c=>c.DistanceTo(new HexCoordinates(4,2))>1),Is.True);
        }
    }
}
