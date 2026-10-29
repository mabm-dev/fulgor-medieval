using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignMilitaryTests
    {
        private MapRouteGraph graph;private CampaignSettlementRegistry owners;private CampaignArmyRegistry initial;
        private static HexCoordinates At(int q)=>new(q,0);
        private static CampaignResources Rich=>new(1000,1000,1000,1000,1000);
        private static CampaignFormationState Troop(string id,long hp=100,int morale=43)=>new(id,TroopCatalog.Get(TroopProfile.Spearmen).Attributes,hp,morale);
        private static CampaignArmyState Army(string id,string faction,params CampaignFormationState[] forms)=>new(id,faction,forms);
        [SetUp]public void Setup()
        {
            graph=new MapRouteGraph(new GeneratedMap(5,1,1,Enumerable.Range(0,5).Select(i=>new MapCell(At(i),TerrainType.Plain,false)).ToArray()));
            owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(1)),new CampaignSettlement("enemy","Rival","rival",At(3))});
            initial=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("own","castilla",Troop("veterans",93,41)),At(1),6),
                new CampaignArmyPlacement(Army("rival","rival",Troop("enemy-troop")),At(4),6)});
        }
        private CampaignMilitaryState Empty()=>new(initial,owners);
        private CampaignMilitaryState Trained(string city="home",string faction="castilla")
        {
            Assert.That(Empty().TryRecruit(city,faction,TroopProfile.Spearmen,1,Rich,initial,out var pending,out _),Is.True);
            return pending.Advance(3,initial);
        }
        [TestCase(TroopProfile.Spearmen,20,2)]
        [TestCase(TroopProfile.MenAtArms,15,3)]
        [TestCase(TroopProfile.Crossbowmen,15,2)]
        [TestCase(TroopProfile.LightCavalry,10,3)]
        public void RecruitmentPaysOnceCompletesOnDueTurnAndDoesNotRepeat(TroopProfile profile,int quantity,int turns)
        {
            var empty=Empty();var spec=RecruitmentCatalog.Get(profile);
            Assert.That(empty.TryRecruit("home","castilla",profile,1,Rich,initial,out var pending,out var remaining),Is.True);
            Rich.TrySpend(spec.Cost,out var expected);Assert.That(remaining,Is.EqualTo(expected));Assert.That(empty.Pending("home"),Is.Null);
            Assert.That(pending.Advance(turns,initial),Is.SameAs(pending));var done=pending.Advance(1+turns,initial);
            Assert.That(done.Pending("home"),Is.Null);Assert.That(done.Garrison("home").Formations.Single().Quantity,Is.EqualTo(quantity));
            Assert.That(done.Advance(20,initial),Is.SameAs(done));Assert.That(pending.Garrison("home").IsDissolved,Is.True);
        }
        [Test]public void OrdersRejectInsufficientStockForeignCityAndDuplicateJobWithoutSpending()
        {
            var empty=Empty();Assert.That(empty.TryRecruit("home","castilla",TroopProfile.Spearmen,1,default,initial,out var next,out var stock),Is.False);
            Assert.That(next,Is.SameAs(empty));Assert.That(stock,Is.EqualTo(default(CampaignResources)));
            Assert.That(empty.TryRecruit("enemy","castilla",TroopProfile.Spearmen,1,Rich,initial,out _,out _),Is.False);
            empty.TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,initial,out var pending,out _);
            Assert.That(pending.TryRecruit("home","castilla",TroopProfile.Crossbowmen,1,Rich,initial,out next,out stock),Is.False);
            Assert.That(stock,Is.EqualTo(Rich));Assert.That(next,Is.SameAs(pending));
        }
        [Test]public void WholeFormationTransferPreservesWoundsMoraleAndSpendsMovement()
        {
            var empty=Empty();Assert.That(empty.TryDeposit("home","own","veterans",initial,out var guard,out var field),Is.True);
            Assert.That(field.TryGetArmy("own",out _),Is.False);var veteran=guard.Garrison("home").Formations.Single();
            Assert.That(veteran.Health,Is.EqualTo(93));Assert.That(veteran.Morale,Is.EqualTo(41));
            Assert.That(guard.TryMuster("home","castilla",veteran.Id,field,out var emptyAgain,out var mustered,out var id),Is.True);
            mustered.TryGetArmy(id,out var army);Assert.That(army.Movement.RemainingMovement,Is.Zero);Assert.That(army.Army.Formations.Single(),Is.SameAs(veteran));
            Assert.That(emptyAgain.Garrison("home").IsDissolved,Is.True);Assert.That(empty.Garrison("home").IsDissolved,Is.True);
        }
        [Test]public void AssigningToExistingArmyDoesNotRefillMovementOrHealAndRejectsDistantArmy()
        {
            var military=Trained();var troop=military.Garrison("home").Formations.Single();
            Assert.That(military.TryAssignToArmy("home","own",troop.Id,initial,out var next,out var field),Is.True);
            field.TryGetArmy("own",out var own);Assert.That(own.Movement.RemainingMovement,Is.Zero);Assert.That(own.Army.Formations.Count,Is.EqualTo(2));
            Assert.That(own.Army.Formations.First(f=>f.Id=="veterans").Health,Is.EqualTo(93));Assert.That(next.Garrison("home").IsDissolved,Is.True);
            initial.TryMove("own",At(0),out var distant,out _,out _);
            Assert.That(military.TryAssignToArmy("enemy","own",troop.Id,distant,out _,out _),Is.False);
        }
        [Test]public void PendingRecruitReservesLastSlotAndCompletedGarrisonBlocksOversizedArmy()
        {
            var six=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("six","castilla",Enumerable.Range(0,6).Select(i=>Troop("f"+i)).ToArray()),At(1),6)});
            Assert.That(new CampaignMilitaryState(six,owners).TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,six,out _,out _),Is.False);
            var five=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("five","castilla",Enumerable.Range(0,5).Select(i=>Troop("f"+i)).ToArray()),At(1),6),
                new CampaignArmyPlacement(Army("extra","castilla",Troop("extra")),At(0),6)});
            var m=new CampaignMilitaryState(five,owners);m.TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,five,out m,out _);
            Assert.That(m.TryDeposit("home","extra","extra",five,out _,out _),Is.False);
            Assert.That(m.Advance(3,five).Garrison("home").Formations.Count,Is.EqualTo(1));
        }
        [Test]public void HostileGarrisonBlocksDestinationAndRoutesThroughItsCell()
        {
            var guard=Trained("enemy","rival");
            Assert.That(guard.TryMove(initial,"own",At(3),out _,out _,out var reason),Is.False);Assert.That(reason,Is.EqualTo(CampaignArmyMoveFailure.GuardedSettlement));
            var noFieldRival=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("own","castilla",Troop("own")),At(1),20)});
            Assert.That(guard.TryMove(noFieldRival,"own",At(4),out _,out _,out reason),Is.False);Assert.That(reason,Is.EqualTo(CampaignArmyMoveFailure.Unreachable));
        }
        [Test]public void FriendlyGarrisonAndPendingLevyPreventSixFormationArmyFromEntering()
        {
            var initialSix=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("six","castilla",Enumerable.Range(0,6).Select(i=>Troop("f"+i)).ToArray()),At(0),6)});
            var empty=new CampaignMilitaryState(initialSix,owners);
            empty.TryRecruit("home","castilla",TroopProfile.Spearmen,1,Rich,initialSix,out var pending,out _);
            foreach(var state in new[]{pending,pending.Advance(3,initialSix)})
            {
                Assert.That(state.TryMove(initialSix,"six",At(1),out var next,out _,out var failure),Is.False);
                Assert.That(failure,Is.EqualTo(CampaignArmyMoveFailure.GarrisonCapacity));Assert.That(next,Is.SameAs(initialSix));
            }
        }
        [Test]public void AssigningAcrossAdjacentCitiesRespectsDestinationDefenseCapacity()
        {
            var cities=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("source","Origen","castilla",At(0)),new CampaignSettlement("destination","Destino","castilla",At(1))});
            var field=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("own","castilla",Enumerable.Range(0,5).Select(i=>Troop("f"+i)).ToArray()),At(1),6)});
            var m=new CampaignMilitaryState(field,cities);
            m.TryRecruit("source","castilla",TroopProfile.Spearmen,1,Rich,field,out m,out _);
            m.TryRecruit("destination","castilla",TroopProfile.Spearmen,1,Rich,field,out m,out _);m=m.Advance(3,field);
            Assert.That(m.TryAssignToArmy("source","own","citytroop/1",field,out var unchanged,out _),Is.False);Assert.That(unchanged,Is.SameAs(m));
            m.ValidateDeployment(field);
        }
        [Test]public void ConquestCancelsPendingTrainingWithoutConvertingLivingTroops()
        {
            var empty=Empty();empty.TryRecruit("enemy","rival",TroopProfile.Spearmen,1,Rich,initial,out var pending,out _);
            var occupying=new CampaignArmyRegistry(graph,new[]{new CampaignArmyPlacement(Army("victor","castilla",Troop("winner")),At(3),6)});
            owners.TryCapture(occupying,"victor","enemy",out var captured,out _);var cancelled=pending.WithOwners(captured);
            Assert.That(cancelled.Pending("enemy"),Is.Null);Assert.That(cancelled.Advance(10,occupying).Garrison("enemy").IsDissolved,Is.True);
            Assert.That(cancelled.Capture().recruits.Single().status,Is.EqualTo(RecruitmentStatus.Cancelled));
            Assert.Throws<InvalidOperationException>(()=>Trained("enemy","rival").WithOwners(captured));
        }
        [Test]public void RecruitmentAndTransfersAreBlockedDuringAnyBattle()
        {
            var m=Trained();initial.TryMove("own",At(3),out var adjacent,out _,out _);adjacent.TryBeginAttack("fight","own","rival",4,out var fighting,out _);
            Assert.That(m.TryRecruit("home","castilla",TroopProfile.Spearmen,3,Rich,fighting,out _,out _),Is.False);
            Assert.That(m.TryMuster("home","castilla","citytroop/1",fighting,out _,out _,out _),Is.False);
            Assert.That(m.TryDeposit("home","own","veterans",fighting,out _,out _),Is.False);
            Assert.That(m.TryMove(fighting,"own",At(2),out _,out _,out _),Is.False);
        }
        private (CampaignArmyRegistry,CampaignMilitaryState) PreparedGuardBattle(bool withField)
        {
            var m=Trained("enemy","rival");var strong=new FormationAttributes(100,100,20,30,20,4,10,1,70,70);
            var list=new System.Collections.Generic.List<CampaignArmyPlacement>{new(Army("own","castilla",new CampaignFormationState("strong",strong,1000,70)),At(2),6)};
            if(withField)list.Add(new CampaignArmyPlacement(Army("defender","rival",Troop("defender")),At(3),6));
            var field=new CampaignArmyRegistry(graph,list);
            Assert.That(field.TryBeginGarrisonAttack("siege","own","enemy",m.Garrison("enemy"),At(3),42,out var battle,out _),Is.True);
            return(battle,m);
        }
        [TestCase(false)][TestCase(true)]public void GuardAndStationedArmyDefendTogetherAndVictoryOccupiesOnlyAfterBothDestroyed(bool withField)
        {
            var(battle,m)=PreparedGuardBattle(withField);battle.TryGetEncounter("siege",out var encounter);
            Assert.That(encounter.Garrison.FieldArmy!=null,Is.EqualTo(withField));
            Assert.That(new BattleAutoController(encounter.Battle.Commands).Run(1000).Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));
            var next=battle.ResolveGarrisonEncounter("siege",out var guard);Assert.That(guard.IsDissolved,Is.True);
            Assert.That(next.Armies.Count,Is.EqualTo(1));next.TryGetArmy("own",out var own);Assert.That(own.Movement.Position,Is.EqualTo(At(3)));Assert.That(own.Movement.RemainingMovement,Is.Zero);
            Assert.That(next.HasResolvedEncounter("siege"),Is.True);Assert.That(m.WithGarrisonResult("enemy",guard).HasHostileGarrison("enemy","castilla"),Is.False);
        }
        [TestCase(false)][TestCase(true)]public void WithdrawalSplitsGuardAndFieldSurvivorsWithoutDuplicatingOrMovingThem(bool withField)
        {
            var(battle,m)=PreparedGuardBattle(withField);battle.TryGetEncounter("siege",out var encounter);encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Attacker);
            var next=battle.ResolveGarrisonEncounter("siege",out var guard);
            Assert.That(guard.Formations.Single().Id,Is.EqualTo("citytroop/1"));Assert.That(guard.HasAppliedEncounter("siege"),Is.True);
            next.TryGetArmy("own",out var own);Assert.That(own.Movement.Position,Is.EqualTo(At(2)));Assert.That(next.TryGetArmy("guard/enemy",out _),Is.False);
            if(withField){next.TryGetArmy("defender",out var field);Assert.That(field.Army.Formations.Single().Id,Is.EqualTo("defender"));}
        }
        private static CampaignMilitaryState Restore(CampaignMilitaryState m,CampaignArmyRegistry initial,CampaignSettlementRegistry owners,int turn)=>
            CampaignMilitaryState.Restore(m.Capture(),initial,owners,new[]{"castilla","rival"},turn);
        [Test]public void SaveRestoresRecruitsVeteransNewArmyAndPendingJobsWithoutResurrectingDissolvedArmies()
        {
            var m=Trained();m.TryDeposit("home","own","veterans",initial,out m,out var field);
            m.TryMuster("home","castilla","veterans",field,out m,out field,out var id);
            m.TryRecruit("enemy","rival",TroopProfile.Crossbowmen,3,Rich,field,out m,out _);
            var restored=Restore(m,initial,owners,3);var loaded=CampaignArmyRegistry.RestoreRecruited(field.Capture(),initial,restored);
            Assert.That(loaded.TryGetArmy("own",out _),Is.False);loaded.TryGetArmy(id,out var army);
            Assert.That(army.Army.Formations.Single().Health,Is.EqualTo(93));Assert.That(army.Army.Formations.Single().Morale,Is.EqualTo(41));Assert.That(army.Movement.RemainingMovement,Is.Zero);
            Assert.That(restored.Pending("enemy").completionTurn,Is.EqualTo(5));Assert.That(restored.Garrison("home").Formations.Single().Id,Is.EqualTo("citytroop/1"));
            var save=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(3,Rich,default),field,id,"castilla");save.IncludeOwnership("base",owners,owners);save.IncludeMilitary(m);
            Assert.That(save.version,Is.EqualTo(6));save.RestoreMultiple(save.context,initial,"castilla",default,out _,out _,restored);
        }
        [TestCase("duplicate")][TestCase("untrained")][TestCase("too-healthy")][TestCase("faction")][TestCase("unknown-army")]
        public void SaveRejectsInventedOrDuplicatedFormationsAndUnknownArmies(string damage)
        {
            var m=Trained();var field=initial.Capture();var own=field.armies.Single(a=>a.id=="own");
            if(damage=="duplicate")own.formations=new[]{new CampaignFormationData{id="citytroop/1",health=100,morale=50}};
            if(damage=="untrained")own.formations[0].id="citytroop/200";
            if(damage=="too-healthy")own.formations[0].health=94;
            if(damage=="faction")own.formations[0].id="enemy-troop";
            if(damage=="unknown-army")own.id="invented";
            Assert.Throws<ArgumentException>(()=>CampaignArmyRegistry.RestoreRecruited(field,initial,Restore(m,initial,owners,3)));
        }
        [TestCase("future")][TestCase("expired")][TestCase("duration")][TestCase("missing-guard")][TestCase("duplicate-guard")][TestCase("serial")]
        public void SaveRejectsInvalidRecruitmentLedgerAndGarrisons(string damage)
        {
            var data=Trained().Capture();
            if(damage=="future")data.recruits[0].completionTurn=4;
            if(damage=="expired")data.recruits[0].status=RecruitmentStatus.Pending;
            if(damage=="duration")data.recruits[0].startTurn=2;
            if(damage=="missing-guard")data.garrisons=Array.Empty<GarrisonData>();
            if(damage=="duplicate-guard")data.garrisons[1]=data.garrisons[0];
            if(damage=="serial")data.recruits[0].serial=2;
            Assert.Throws<ArgumentException>(()=>CampaignMilitaryState.Restore(data,initial,owners,new[]{"castilla","rival"},3));
        }
        [Test]public void SavedArraysAreDetachedAndLegacyVersionsStillRestoreOriginalArmies()
        {
            var trained=Trained();var captured=trained.Capture();captured.recruits[0].profile=TroopProfile.LightCavalry;captured.garrisons.Single(g=>g.settlementId=="home").formations[0].health=1;
            Assert.That(trained.Garrison("home").Formations.Single().Health,Is.EqualTo(200));
            Assert.That(CampaignArmyRegistry.Restore(initial.Capture(),initial).Armies.Count,Is.EqualTo(2));
            var signature=CampaignSaveData.MilitaryScenarioSignature("context");Assert.That(signature,Is.Not.EqualTo(CampaignSaveData.MilitaryScenarioSignature("other")));
        }
    }
}
