using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignMarchTests
    {
        private MapRouteGraph graph;private CampaignArmyRegistry initial;private CampaignSettlementRegistry owners;private CampaignMilitaryState military;
        private static HexCoordinates At(int q,int r=0)=>new(q,r);
        private static CampaignArmyPlacement Place(string id,string faction,int q,int r=0,int budget=2)=>new(new CampaignArmyState(id,faction,
            new[]{new CampaignFormationState(id+"-troop",TroopCatalog.Get(TroopProfile.Spearmen).Attributes,93,41)}),At(q,r),budget);
        private static CampaignResources Rich=>new(1000,1000,1000,1000,1000);
        [SetUp]public void Setup()
        {
            graph=new MapRouteGraph(new GeneratedMap(9,1,1,Enumerable.Range(0,9).Select(i=>new MapCell(At(i),TerrainType.Plain,false)).ToArray()));
            initial=new CampaignArmyRegistry(graph,new[]{Place("a","castilla",0),Place("enemy","rival",8,budget:20)});
            owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(0))});military=new CampaignMilitaryState(initial,owners);
        }
        private CampaignMarchOrders Order(HexCoordinates target,CampaignArmyRegistry registry=null)
        {
            var empty=new CampaignMarchOrders();Assert.That(empty.TryOrder("a",target,registry??initial,military,"castilla",out var next,out _,out _),Is.True);return next;
        }
        [Test]public void MarchAdvancesWholeAffordableStepsAcrossTurnsAndClearsOnlyAtArrival()
        {
            var empty=new CampaignMarchOrders();var orders=Order(At(6));var registry=initial;
            for(int q=2;q<=6;q+=2)
            {
                orders=orders.AdvanceArmy("a",registry,military,"castilla",out registry,out var step);
                registry.TryGetArmy("a",out var army);Assert.That(army.Movement.Position,Is.EqualTo(At(q)));Assert.That(army.Movement.RemainingMovement,Is.Zero);
                Assert.That(step.Travelled.TotalCost,Is.EqualTo(2));Assert.That(step.Travelled.Steps,Is.EqualTo(2));
                Assert.That(orders.Count,Is.EqualTo(q==6?0:1));registry=registry.BeginTurn();
            }
            Assert.That(empty.Count,Is.Zero);initial.TryGetArmy("a",out var original);Assert.That(original.Movement.Position,Is.EqualTo(At(0)));
        }
        [Test]public void ExhaustedArmyCanQueueOrderWithoutMovingOrRefilling()
        {
            initial.TryMove("a",At(2),out var exhausted,out _,out _);var orders=Order(At(6),exhausted);
            var same=orders.AdvanceArmy("a",exhausted,military,"castilla",out var next,out var step);
            Assert.That(next,Is.SameAs(exhausted));Assert.That(same,Is.SameAs(orders));Assert.That(step.Status,Is.EqualTo(MarchStepStatus.WaitingForMovement));
            next.TryGetArmy("a",out var army);Assert.That(army.Movement.RemainingMovement,Is.Zero);
        }
        [Test]public void MovingBlockerPausesSavedOrderAndClearingItResumesWithoutBattle()
        {
            var orders=Order(At(6));initial.TryMove("enemy",At(5),out var blocked,out _,out _);
            var paused=orders.AdvanceArmy("a",blocked,military,"castilla",out var next,out var step);
            Assert.That(step.Status,Is.EqualTo(MarchStepStatus.Blocked));Assert.That(next,Is.SameAs(blocked));Assert.That(paused.Destination("a"),Is.EqualTo(At(6)));Assert.That(next.HasActiveEncounters,Is.False);
            var loaded=CampaignMarchOrders.Restore(paused.Capture(),blocked,"castilla");blocked.TryMove("enemy",At(8),out var cleared,out _,out _);
            loaded.AdvanceArmy("a",cleared,military,"castilla",out next,out step);next.TryGetArmy("a",out var army);
            Assert.That(army.Movement.Position,Is.EqualTo(At(2)));Assert.That(step.Status,Is.EqualTo(MarchStepStatus.Moving));
        }
        [Test]public void OccupiedDestinationPausesAndNeverCreatesAnAttack()
        {
            var orders=Order(At(6));initial.TryMove("enemy",At(6),out var blocked,out _,out _);
            orders.AdvanceArmy("a",blocked,military,"castilla",out var next,out var result);
            Assert.That(result.Failure,Is.EqualTo(CampaignArmyMoveFailure.Occupied));Assert.That(next.HasActiveEncounters,Is.False);
        }
        [Test]public void TwoMarchesUseStablePriorityAndCannotShareDestination()
        {
            var map=new GeneratedMap(6,2,1,Enumerable.Range(0,2).SelectMany(r=>Enumerable.Range(0,6).Select(q=>new MapCell(At(q,r),TerrainType.Plain,false))).ToArray());
            var routes=new MapRouteGraph(map);var registry=new CampaignArmyRegistry(routes,new[]{Place("b","castilla",0,1,6),Place("a","castilla",0,0,6)});
            var m=new CampaignMilitaryState(registry,new CampaignSettlementRegistry(routes,Array.Empty<CampaignSettlement>()));var orders=new CampaignMarchOrders();
            orders.TryOrder("b",At(4),registry,m,"castilla",out orders,out _,out _);orders.TryOrder("a",At(4),registry,m,"castilla",out orders,out _,out _);
            CollectionAssert.AreEqual(new[]{"a","b"},orders.ArmyIds);
            foreach(var id in orders.ArmyIds)orders=orders.AdvanceArmy(id,registry,m,"castilla",out registry,out _);
            registry.TryGetArmy("a",out var a);registry.TryGetArmy("b",out var b);Assert.That(a.Movement.Position,Is.EqualTo(At(4)));Assert.That(b.Movement.Position,Is.EqualTo(At(0,1)));
            Assert.That(orders.Destination("a"),Is.Null);Assert.That(orders.Destination("b"),Is.EqualTo(At(4)));
        }
        [Test]public void ImpossibleTerrainStepsAreExcludedAndAlternateAffordableRouteIsUsed()
        {
            var map=new GeneratedMap(3,2,1,new[]{new MapCell(At(0),TerrainType.Plain,false),new MapCell(At(1),TerrainType.Mountain,false),new MapCell(At(2),TerrainType.Plain,false),
                new MapCell(At(0,1),TerrainType.Plain,false),new MapCell(At(1,1),TerrainType.Plain,false),new MapCell(At(2,1),TerrainType.Plain,false)});
            var routes=new MapRouteGraph(map);var registry=new CampaignArmyRegistry(routes,new[]{Place("a","castilla",0,budget:1)});
            var m=new CampaignMilitaryState(registry,new CampaignSettlementRegistry(routes,Array.Empty<CampaignSettlement>()));
            Assert.That(m.TryPlanMarch(registry,"a",At(2),out var path,out _),Is.True);Assert.That(path.Cells.Contains(At(1)),Is.False);
            Assert.That(path.TotalCost,Is.GreaterThan(1));
            Assert.That(m.TryPlanMarch(registry,"a",At(1),out _,out _),Is.False);
        }
        [Test]public void ObjectiveAndUnownedFortStopThePrefixEvenWithMovementToContinue()
        {
            var fast=new CampaignArmyRegistry(graph,new[]{Place("a","castilla",0,budget:6)});var orders=Order(At(6),fast);
            orders.AdvanceArmy("a",fast,military,"castilla",out var atGoal,out _,At(3));atGoal.TryGetArmy("a",out var army);Assert.That(army.Movement.Position,Is.EqualTo(At(3)));
            var forts=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("rival","Fortaleza","rival",At(3))});var m=new CampaignMilitaryState(fast,forts);
            orders=orders.AdvanceArmy("a",fast,m,"castilla",out var atFort,out _);atFort.TryGetArmy("a",out army);Assert.That(army.Movement.Position,Is.EqualTo(At(3)));
            Assert.That(orders.Destination("a"),Is.EqualTo(At(6)));forts.TryCapture(atFort,"a","rival",out var captured,out _);m=m.WithOwners(captured);
            orders=orders.AdvanceArmy("a",atFort.BeginTurn(),m,"castilla",out var arrived,out _);arrived.TryGetArmy("a",out army);Assert.That(army.Movement.Position,Is.EqualTo(At(6)));Assert.That(orders.Count,Is.Zero);
        }
        [Test]public void DefendedFortCannotBeOrderedOrTraversed()
        {
            var forts=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("rival","Fortaleza","rival",At(3))});var m=new CampaignMilitaryState(initial,forts);
            m.TryRecruit("rival","rival",TroopProfile.Spearmen,1,Rich,initial,out m,out _);m=m.Advance(3,initial);
            Assert.That(new CampaignMarchOrders().TryOrder("a",At(3),initial,m,"castilla",out _,out _,out var reason),Is.False);Assert.That(reason,Is.EqualTo(CampaignArmyMoveFailure.GuardedSettlement));
            Assert.That(m.TryPlanMarch(initial,"a",At(6),out _,out _),Is.False);
        }
        [Test]public void ActiveBattleFreezesEveryMarchAndOrdersCanBeCancelledIndividually()
        {
            var orders=Order(At(6));initial.TryMove("enemy",At(1),out var adjacent,out _,out _);adjacent.TryBeginAttack("fight","a","enemy",4,out var fighting,out _);
            Assert.Throws<InvalidOperationException>(()=>orders.AdvanceArmy("a",fighting,military,"castilla",out _,out _));
            Assert.That(orders.TryOrder("a",At(4),fighting,military,"castilla",out var unchanged,out _,out _),Is.False);Assert.That(unchanged,Is.SameAs(orders));
            Assert.That(orders.Cancel("missing"),Is.SameAs(orders));Assert.That(orders.Cancel("a").Count,Is.Zero);Assert.That(orders.Count,Is.EqualTo(1));
        }
        [TestCase("unknown")][TestCase("foreign")][TestCase("duplicate")][TestCase("water")][TestCase("arrived")]
        public void SavedOrdersRejectUnknownForeignDuplicateOrInvalidDestinations(string kind)
        {
            var data=Order(At(6)).Capture();if(kind=="unknown")data[0].armyId="invented";if(kind=="foreign")data[0].armyId="enemy";
            if(kind=="duplicate")data=new[]{data[0],data[0]};if(kind=="water")data[0].q=99;if(kind=="arrived")data[0].q=0;
            Assert.Throws<ArgumentException>(()=>CampaignMarchOrders.Restore(data,initial,"castilla"));
        }
        [Test]public void SaveV7RoundTripKeepsIndependentDestinationAndSpentMovement()
        {
            var orders=Order(At(6));orders=orders.AdvanceArmy("a",initial,military,"castilla",out var moved,out _);
            var save=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(1,Rich,default),moved,"a","castilla");
            save.IncludeOwnership("base",owners,owners);save.IncludeMilitary(military);save.IncludeMarches(orders);
            save.RestoreMultiple(save.context,initial,"castilla",default,out _,out var loaded,military);
            var restored=save.RestoreMarches(loaded,"castilla");loaded.TryGetArmy("a",out var army);
            Assert.That(save.version,Is.EqualTo(7));Assert.That(army.Movement.RemainingMovement,Is.Zero);Assert.That(restored.Destination("a"),Is.EqualTo(At(6)));
            save.marchOrders[0].q=5;Assert.That(restored.Destination("a"),Is.EqualTo(At(6)));
        }
        [Test]public void LegacyV6HasNoFieldOrdersAndSimpleV7KeepsItsExistingSingleMarch()
        {
            var legacy=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(1,Rich,default),initial,"a","castilla");legacy.IncludeOwnership("base",owners,owners);legacy.IncludeMilitary(military);
            Assert.That(legacy.RestoreMarches(initial,"castilla").Count,Is.Zero);
            var simple=CampaignSaveData.Capture("base",new CampaignTurnState(1,Rich,default),new ArmyMovementState(graph,At(0),6),At(6));
            simple.IncludeOwnership("base",owners,owners);simple.IncludeMilitary(new CampaignMilitaryState(null,owners));simple.IncludeMarches(new CampaignMarchOrders());
            simple.Restore(simple.context,graph,At(0),default,6,out _,out var army);Assert.That(simple.RestoreMarchDestination(graph,army),Is.EqualTo(At(6)));
            simple.marchOrders=null;Assert.Throws<ArgumentException>(()=>simple.Restore(simple.context,graph,At(0),default,6,out _,out _));
        }
    }
}
