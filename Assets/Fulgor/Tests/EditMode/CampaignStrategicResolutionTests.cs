using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignStrategicResolutionTests
    {
        private MapRouteGraph routes;private CampaignSettlementRegistry owners;private CampaignArmyRegistry initial;
        private CampaignMilitaryState military;
        private static HexCoordinates At(int q,int r=1)=>new(q,r);
        private static CampaignResources Rich=>new(1000,1000,1000,1000,1000);
        private static CampaignArmyPlacement Place(string id,string faction,HexCoordinates at,long health=93,int morale=41,int count=1)=>new(
            new CampaignArmyState(id,faction,Enumerable.Range(0,count).Select(i=>new CampaignFormationState(id+"-troop-"+i,TroopCatalog.Get(TroopProfile.Spearmen).Attributes,health,morale))),at,6);
        [SetUp]public void Setup()
        {
            routes=new MapRouteGraph(new GeneratedMap(5,3,1,Enumerable.Range(0,3).SelectMany(r=>Enumerable.Range(0,5).Select(q=>new MapCell(At(q,r),TerrainType.Plain,false))).ToArray()));
            owners=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("home","Casa","castilla",At(0)),new CampaignSettlement("fort","Fortaleza","rival",At(2))});
            initial=new CampaignArmyRegistry(routes,new[]{Place("attacker","castilla",At(1)),Place("defender","rival",At(2))});military=new CampaignMilitaryState(initial,owners);
        }
        private static CampaignArmyRegistry Withdraw(CampaignArmyRegistry armies,BattleSide side)
        {
            armies.TryBeginAttack("fight","attacker","defender",4,out var fighting,out _);fighting.TryGetEncounter("fight",out var encounter);Assert.That(encounter.Battle.Roster.TryWithdrawArmy(side),Is.True);return fighting;
        }
        [Test]public void WithdrawnDefenderLeavesItsCellAndPreservesExactWoundsMoraleAndHistory()
        {
            var fighting=Withdraw(initial,BattleSide.Defender);var result=CampaignStrategicResolution.Resolve("fight",fighting,military,owners);
            var retreat=result.Retreats.Single();Assert.That(retreat.Surrendered,Is.False);Assert.That(retreat.From,Is.EqualTo(At(2)));Assert.That(retreat.Destination.Value.DistanceTo(At(2)),Is.EqualTo(1));
            result.Armies.TryGetArmy("defender",out var defender);Assert.That(defender.Army.Formations.Single().Health,Is.EqualTo(93));Assert.That(defender.Army.Formations.Single().Morale,Is.EqualTo(41));
            Assert.That(defender.Movement.RemainingMovement,Is.Zero);Assert.That(defender.Army.HasAppliedEncounter("fight"),Is.True);
            result.Armies.TryGetArmy("attacker",out var attacker);Assert.That(attacker.Movement.Position,Is.EqualTo(At(2)));Assert.That(attacker.Movement.RemainingMovement,Is.Zero);
            result.Owners.TryGet("fort",out var fort);Assert.That(fort.OwnerFactionId,Is.EqualTo("castilla"));Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,result.Armies,result.Owners,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
            var again=CampaignStrategicResolution.Resolve("fight",result.Armies,result.Military,result.Owners);Assert.That(again.Armies,Is.SameAs(result.Armies));Assert.That(again.Retreats.Count,Is.Zero);
        }
        [Test]public void AttackerRetreatMovesAwayWithoutAdvancingTheDefender()
        {
            var result=CampaignStrategicResolution.Resolve("fight",Withdraw(initial,BattleSide.Attacker),military,owners);
            result.Armies.TryGetArmy("attacker",out var attacker);result.Armies.TryGetArmy("defender",out var defender);
            Assert.That(attacker.Movement.Position,Is.EqualTo(At(0)));Assert.That(attacker.Movement.RemainingMovement,Is.Zero);Assert.That(attacker.Army.Formations.Single().Health,Is.EqualTo(93));
            Assert.That(defender.Movement.Position,Is.EqualTo(At(2)));Assert.That(result.ConqueredSettlementId,Is.Null);
        }
        [Test]public void SurroundedSurvivorsSurrenderWithoutBeingReportedAsTacticalDeaths()
        {
            var placements=new[]{Place("attacker","castilla",At(1)),Place("defender","rival",At(2))}.Concat(
                At(2).Neighbors().Where(at=>at!=At(1)).Select((at,i)=>Place("block-"+i,"castilla",at))).ToArray();
            var armies=new CampaignArmyRegistry(routes,placements);var m=new CampaignMilitaryState(armies,owners);var fighting=Withdraw(armies,BattleSide.Defender);
            fighting.TryGetEncounter("fight",out var encounter);Assert.That(encounter.Battle.CreateReport().Defender.Casualties,Is.Zero);
            var result=CampaignStrategicResolution.Resolve("fight",fighting,m,owners);Assert.That(result.Retreats.Single().Surrendered,Is.True);Assert.That(result.Retreats.Single().Troops,Is.EqualTo(10));
            Assert.That(result.Armies.TryGetArmy("defender",out _),Is.False);Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,result.Armies,result.Owners,"castilla"),Is.EqualTo(CampaignConclusion.Victory));
        }
        private CampaignMilitaryState Train(CampaignArmyRegistry armies)
        {
            var m=new CampaignMilitaryState(armies,owners);Assert.That(m.TryRecruit("fort","rival",TroopProfile.Spearmen,1,Rich,armies,out m,out _),Is.True);return m.Advance(3,armies);
        }
        private CampaignArmyRegistry GuardBattle(CampaignArmyRegistry armies,CampaignMilitaryState m)
        {
            armies.TryBeginGarrisonAttack("siege","attacker","fort",m.Garrison("fort"),At(2),4,out var fighting,out _);
            fighting.TryGetEncounter("siege",out var encounter);encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Defender);return fighting;
        }
        [Test]public void GarrisonEscapesAsNewArmyWithProvenanceAndPendingLevyIsCancelledOnConquest()
        {
            var armies=new CampaignArmyRegistry(routes,new[]{Place("attacker","castilla",At(1))});var m=Train(armies);
            m.TryRecruit("fort","rival",TroopProfile.Crossbowmen,3,Rich,armies,out m,out _);
            var result=CampaignStrategicResolution.Resolve("siege",GuardBattle(armies,m),m,owners);
            Assert.That(result.Military.Garrison("fort").IsDissolved,Is.True);Assert.That(result.Military.Pending("fort"),Is.Null);
            result.Armies.TryGetArmy("muster/1",out var escaped);Assert.That(escaped,Is.Not.Null);Assert.That(escaped.Army.FactionId,Is.EqualTo("rival"));Assert.That(escaped.Army.Formations.Single().Health,Is.EqualTo(200));
            Assert.That(escaped.Movement.RemainingMovement,Is.Zero);Assert.That(escaped.Army.HasAppliedEncounter("siege"),Is.True);Assert.That(result.Military.Capture().musters.Length,Is.EqualTo(1));
            var restoredMilitary=CampaignMilitaryState.Restore(result.Military.Capture(),armies,result.Owners,new[]{"castilla","rival"},3);
            var restored=CampaignArmyRegistry.RestoreRecruited(result.Armies.Capture(),armies,restoredMilitary);restored.TryGetArmy("muster/1",out var loaded);
            Assert.That(loaded.Movement.Position,Is.EqualTo(escaped.Movement.Position));Assert.That(loaded.Movement.RemainingMovement,Is.Zero);
            Assert.That(restoredMilitary.Capture().recruits[1].status,Is.EqualTo(RecruitmentStatus.Cancelled));
            var again=CampaignStrategicResolution.Resolve("siege",result.Armies,result.Military,result.Owners);Assert.That(again.Military.Capture().musters.Length,Is.EqualTo(1));
        }
        [Test]public void FieldArmyAndGarrisonEscapeTogetherWithoutDuplicatingFormations()
        {
            var m=Train(initial);var result=CampaignStrategicResolution.Resolve("siege",GuardBattle(initial,m),m,owners);
            result.Armies.TryGetArmy("defender",out var escaped);Assert.That(escaped.Army.Formations.Count,Is.EqualTo(2));Assert.That(escaped.Army.Formations.Sum(f=>f.Health),Is.EqualTo(293));
            Assert.That(escaped.Army.Formations.Select(f=>f.Id).Distinct().Count(),Is.EqualTo(2));Assert.That(result.Military.Garrison("fort").IsDissolved,Is.True);Assert.That(result.Military.Capture().musters.Length,Is.Zero);
            Assert.That(escaped.Movement.Position,Is.EqualTo(result.Retreats.Single().Destination));result.Military.ValidateDeployment(result.Armies);
        }
        [Test]public void EscapeRejectsEnemyCityAndFullFriendlyGarrison()
        {
            var enemyCity=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("enemy","Ajena","castilla",At(3))});var m=new CampaignMilitaryState(initial,enemyCity);
            Assert.That(CampaignStrategicResolution.RetreatDestination(initial,m,enemyCity,At(2),"rival",1,6),Is.Not.EqualTo(At(3)));
            var refuge=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("refuge","Refugio","rival",At(3))});m=new CampaignMilitaryState(initial,refuge);
            m.TryRecruit("refuge","rival",TroopProfile.Spearmen,1,Rich,initial,out m,out _);m=m.Advance(3,initial);
            Assert.That(CampaignStrategicResolution.RetreatDestination(initial,m,refuge,At(2),"rival",6,6),Is.Not.EqualTo(At(3)));
        }
        [Test]public void OutcomeAllowsRecoveryWithCityOrArmyAndOnlyEndsAfterAllOppositionIsGone()
        {
            var noArmies=new CampaignArmyRegistry(routes,Array.Empty<CampaignArmyPlacement>());
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,noArmies,owners,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
            var rivalCities=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("fort","Rival","rival",At(2))});
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,initial,rivalCities,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,noArmies,rivalCities,"castilla"),Is.EqualTo(CampaignConclusion.Defeat));
            var ownCities=new CampaignSettlementRegistry(routes,new[]{new CampaignSettlement("home","Casa","castilla",At(0))});
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,noArmies,ownCities,"castilla"),Is.EqualTo(CampaignConclusion.Victory));
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,initial,ownCities,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
        }
        [Test]public void LegacyObjectiveDoesNotGainConquestDefeatRulesAndPendingBattleIsNotFinal()
        {
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.ReachDestination,null,null,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.ReachDestination,null,null,"castilla",true),Is.EqualTo(CampaignConclusion.Victory));
            initial.TryBeginAttack("fight","attacker","defender",4,out var fighting,out _);
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,fighting,owners,"castilla"),Is.EqualTo(CampaignConclusion.InProgress));
        }
        [Test]public void V9RulesRejectWrongObjectiveAndLegacyConquestWhileKeepingLegacyMissionSaves()
        {
            var old=new CampaignSaveData{version=8};old.RestoreCampaignRules(CampaignObjectiveKind.ReachDestination);
            Assert.Throws<ArgumentException>(()=>old.RestoreCampaignRules(CampaignObjectiveKind.Conquest));
            var save=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(1,Rich,default),initial,"attacker","castilla");
            save.IncludeOwnership("base",owners,owners);save.IncludeMilitary(military);save.IncludeMarches(new CampaignMarchOrders());save.IncludeRivals(new CampaignRivalState(false,new[]{"rival"}));save.IncludeCampaignRules(CampaignObjectiveKind.Conquest);
            Assert.That(save.version,Is.EqualTo(9));save.RestoreCampaignRules(CampaignObjectiveKind.Conquest);
            Assert.Throws<ArgumentException>(()=>save.RestoreCampaignRules(CampaignObjectiveKind.ReachDestination));
            Assert.That(CampaignSaveData.CampaignRulesSignature("base",CampaignObjectiveKind.Conquest),Is.Not.EqualTo(CampaignSaveData.CampaignRulesSignature("base",CampaignObjectiveKind.ReachDestination)));
        }
    }
}
