using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignConquestTests
    {
        private static HexCoordinates At(int q)=>new(q,0);
        private static GeneratedMap Map()=>new(4,1,8,new[]{new MapCell(At(0),TerrainType.Plain,false),
            new MapCell(At(1),TerrainType.Forest,false),new MapCell(At(2),TerrainType.Hill,true),new MapCell(At(3),TerrainType.Hill,false)});
        private static CampaignSettlementRegistry Cities(bool reverse=false)
        {
            var home=new CampaignSettlement("a","Casa","castilla",At(0));var enemy=new CampaignSettlement("b","Villa rival","rival",At(3));
            return new CampaignSettlementRegistry(new MapRouteGraph(Map()),reverse?new[]{enemy,home}:new[]{home,enemy});
        }
        private static CampaignArmyState Army(string id,string faction,bool strong=false)
        {
            var a=strong?new FormationAttributes(100,100,20,30,20,4,10,1,70,70):TroopCatalog.Get(TroopProfile.Spearmen).Attributes;
            return new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",a,strong?1000:50,67)});
        }
        private static CampaignArmyRegistry Armies(int q=0)=>new(new MapRouteGraph(Map()),new[]{new CampaignArmyPlacement(Army("own","castilla"),At(q),10)});

        [Test]public void DistrictIncomeFollowsOwnerAndPreservesWholeMapTotals()
        {
            var cities=Cities();var own=CampaignIncomeCalculator.ForFaction(Map(),cities,"castilla");var enemy=CampaignIncomeCalculator.ForFaction(Map(),cities,"rival");
            Assert.That(own,Is.EqualTo(new CampaignResources(1,1,0,0,2)));
            Assert.That(enemy,Is.EqualTo(new CampaignResources(0,0,2,1,2)));
            Assert.That(own.Add(enemy),Is.EqualTo(CampaignIncomeCalculator.FromMap(Map())));
            Assert.That(CampaignIncomeCalculator.ForFaction(Map(),Cities(true),"castilla"),Is.EqualTo(own));
        }

        [Test]public void EnteringEmptySettlementTransfersOwnerAndIncomeWithoutMutatingOriginal()
        {
            var cities=Cities();var armies=Armies(3);
            Assert.That(cities.TryCapture(armies,"own","b",out var next,out _),Is.True);
            Assert.That(cities.Settlements[1].OwnerFactionId,Is.EqualTo("rival"));
            next.TryGet("b",out var captured);Assert.That(captured.OwnerFactionId,Is.EqualTo("castilla"));
            Assert.That(CampaignIncomeCalculator.ForFaction(Map(),next,"castilla"),Is.EqualTo(CampaignIncomeCalculator.FromMap(Map())));
            Assert.That(next.TryCapture(armies,"own","b",out var repeated,out var reason),Is.False);
            Assert.That(repeated,Is.SameAs(next));Assert.That(reason,Is.EqualTo(SettlementCaptureFailure.AlreadyOwned));
        }

        [TestCase("missing-city",SettlementCaptureFailure.UnknownSettlement)]
        [TestCase("missing-army",SettlementCaptureFailure.UnknownArmy)]
        [TestCase("outside",SettlementCaptureFailure.NotInside)]
        [TestCase("own-city",SettlementCaptureFailure.AlreadyOwned)]
        public void CaptureRejectsInvalidOrdersWithoutMutation(string kind,SettlementCaptureFailure reason)
        {
            var cities=Cities();var armies=Armies();
            Assert.That(cities.TryCapture(armies,kind=="missing-army"?"missing":"own",kind=="missing-city"?"missing":kind=="own-city"?"a":"b",out var next,out var actual),Is.False);
            Assert.That(next,Is.SameAs(cities));Assert.That(actual,Is.EqualTo(reason));
        }

        [Test]public void VictoryOccupiesVacatedFortAndDoesNotRestoreSpentMovement()
        {
            var graph=new MapRouteGraph(Map());var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(Army("own","castilla",true),At(2),10),new CampaignArmyPlacement(Army("enemy","rival"),At(3),10)});
            initial.TryBeginAttack("battle","own","enemy",42,out var fighting,out _);fighting.TryGetEncounter("battle",out var encounter);
            Assert.That(new BattleAutoController(encounter.Battle.Commands).Run(1000).Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));
            var next=fighting.ResolveAndOccupyVacatedCell("battle",out var occupied);Assert.That(occupied,Is.EqualTo(At(3)));
            next.TryGetArmy("own",out var army);Assert.That(army.Movement.RemainingMovement,Is.Zero);
            Assert.That(Cities().TryCapture(next,"own","b",out var captured,out _),Is.True);
            Assert.That(CampaignIncomeCalculator.ForFaction(Map(),captured,"rival"),Is.EqualTo(default(CampaignResources)));
            Assert.That(next.ResolveAndOccupyVacatedCell("battle",out var again),Is.SameAs(next));Assert.That(again,Is.Null);
        }

        [Test]public void WithdrawnLivingDefenderStillBlocksOccupation()
        {
            var graph=new MapRouteGraph(Map());var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(Army("own","castilla"),At(2),10),new CampaignArmyPlacement(Army("enemy","rival"),At(3),10)});
            initial.TryBeginAttack("battle","own","enemy",42,out var fighting,out _);fighting.TryGetEncounter("battle",out var encounter);
            encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Defender);
            Assert.That(Cities().PreviewCapture(fighting,"enemy","b"),Is.EqualTo(SettlementCaptureFailure.BattlePending));
            var resolved=fighting.ResolveAndOccupyVacatedCell("battle",out var occupied);Assert.That(occupied,Is.Null);
            Assert.That(resolved.TryGetArmy("enemy",out var alive),Is.True);Assert.That(alive.Movement.Position,Is.EqualTo(At(3)));
            Assert.That(Cities().PreviewCapture(resolved,"own","b"),Is.EqualTo(SettlementCaptureFailure.NotInside));
        }

        [Test]public void SaveV5RestoresOwnershipAndRejectsWrongSettlementConfiguration()
        {
            var initial=Cities();initial.TryCapture(Armies(3),"own","b",out var captured,out _);
            var save=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(4,default,default),Armies(3),"own","castilla");
            save.IncludeOwnership("base",initial,captured);
            Assert.That(save.version,Is.EqualTo(5));
            save.RestoreMultiple(CampaignSaveData.SettlementScenarioSignature("base",initial),Armies(),"castilla",default,out _,out _);
            var loaded=save.RestoreSettlementOwners(initial,new[]{"castilla","rival"});loaded.TryGet("b",out var city);
            Assert.That(city.OwnerFactionId,Is.EqualTo("castilla"));
            var different=new CampaignSettlementRegistry(new MapRouteGraph(Map()),new[]{new CampaignSettlement("a","Casa","castilla",At(0))});
            Assert.Throws<ArgumentException>(()=>save.RestoreMultiple(CampaignSaveData.SettlementScenarioSignature("base",different),Armies(),"castilla",default,out _,out _));
            Assert.That(CampaignSaveData.SettlementScenarioSignature("base",initial),Is.EqualTo(CampaignSaveData.SettlementScenarioSignature("base",Cities(true))));
        }

        [TestCase("missing")][TestCase("duplicate")][TestCase("unknown")][TestCase("faction")][TestCase("blank")][TestCase("null")]
        public void DamagedOwnershipIsRejected(string kind)
        {
            var cities=Cities();var owners=cities.CaptureOwnership();
            switch(kind){case "missing":owners=new[]{owners[0]};break;case "duplicate":owners[1]=owners[0];break;
                case "unknown":owners[1].settlementId="missing";break;case "faction":owners[1].ownerFactionId="invented";break;
                case "blank":owners[1].ownerFactionId=" ";break;case "null":owners=null;break;}
            Assert.Throws<ArgumentException>(()=>cities.RestoreOwnership(owners,new[]{"castilla","rival"}));
            cities.TryGet("b",out var unchanged);Assert.That(unchanged.OwnerFactionId,Is.EqualTo("rival"));
        }

        [Test]public void LegacySavesRestoreScenarioOwnersRatherThanCurrentConquests()
        {
            var cities=Cities();var old=CampaignSaveData.CaptureMultiple("base",new CampaignTurnState(2,default,default),Armies(),"own","castilla");
            Assert.That(old.RestoreSettlementOwners(cities,new[]{"castilla","rival"}),Is.SameAs(cities));
        }
    }
}
