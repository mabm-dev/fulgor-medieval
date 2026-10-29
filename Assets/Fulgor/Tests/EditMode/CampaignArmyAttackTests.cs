using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignArmyAttackTests
    {
        private static HexCoordinates At(int q) => new(q, 0);
        private static CampaignArmyRegistry Registry(int enemyPosition = 1, string enemyFaction = "rival")
        {
            var graph = new MapRouteGraph(new GeneratedMap(4, 1, 3, new[] {
                new MapCell(At(0),TerrainType.Plain,false),new MapCell(At(1),TerrainType.Plain,false),
                new MapCell(At(2),TerrainType.Plain,false),new MapCell(At(3),TerrainType.Plain,false) }));
            var p = TroopCatalog.Get(TroopProfile.Spearmen);
            var a = new CampaignArmyState("a", "castilla", new[] { new CampaignFormationState("a-1",p.Attributes,173,67) });
            var b = new CampaignArmyState("b", enemyFaction, new[] { new CampaignFormationState("b-1",p.Attributes,110,70) });
            return new CampaignArmyRegistry(graph,new[] { new CampaignArmyPlacement(a,At(0),6),new CampaignArmyPlacement(b,At(enemyPosition),6) });
        }

        [Test] public void PreviewDoesNotSpendOrCreateBattleAndAttackConsumesRemainingMovement()
        {
            var registry=Registry();
            Assert.That(registry.PreviewAttack("a","b"),Is.EqualTo(CampaignAttackFailure.None));
            Assert.That(registry.HasActiveEncounters,Is.False);
            registry.TryGetArmy("a",out var before);Assert.That(before.Movement.RemainingMovement,Is.EqualTo(6));
            Assert.That(registry.TryBeginAttack("fight","a","b",42,out var next,out _),Is.True);
            next.TryGetArmy("a",out var attacker);next.TryGetArmy("b",out var defender);
            Assert.That(attacker.Movement.RemainingMovement,Is.Zero);
            Assert.That(defender.Movement.RemainingMovement,Is.EqualTo(6));
            Assert.That(registry.HasActiveEncounters,Is.False);Assert.That(before.Movement.RemainingMovement,Is.EqualTo(6));
            Assert.That(next.IsLocked("a")&&next.IsLocked("b"),Is.True);
            next.TryGetEncounter("fight",out var encounter);
            Assert.That(encounter.Battle.Roster.GetFormation(0).TotalHealth,Is.EqualTo(173));
            Assert.That(encounter.Battle.Roster.GetFormation(0).Morale,Is.EqualTo(67));
        }

        [TestCase("unknown",CampaignAttackFailure.UnknownArmy)]
        [TestCase("friendly",CampaignAttackFailure.FriendlyArmy)]
        [TestCase("distant",CampaignAttackFailure.NotAdjacent)]
        [TestCase("busy",CampaignAttackFailure.ArmyInBattle)]
        public void InvalidAttacksLeaveRegistryUnchanged(string kind,CampaignAttackFailure expected)
        {
            var registry=Registry(kind=="distant"?3:1,kind=="friendly"?"castilla":"rival");
            if(kind=="busy")registry=registry.BeginEncounter("old","a","b",42);
            Assert.That(registry.TryBeginAttack("new",kind=="unknown"?"missing":"a","b",42,out var next,out var reason),Is.False);
            Assert.That(next,Is.SameAs(registry));Assert.That(reason,Is.EqualTo(expected));
        }

        [Test] public void RetreatPreservesWoundsAndCannotAttackAgainUntilNextTurn()
        {
            var initial=Registry();initial.TryBeginAttack("fight","a","b",42,out var fighting,out _);
            fighting.TryGetEncounter("fight",out var encounter);
            Assert.That(encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Attacker),Is.True);
            var after=fighting.ResolveEncounter("fight");after.TryGetArmy("a",out var survivor);
            Assert.That(survivor.Army.Formations[0].Health,Is.EqualTo(173));
            Assert.That(survivor.Movement.Position,Is.EqualTo(At(0)));
            Assert.That(after.PreviewAttack("a","b"),Is.EqualTo(CampaignAttackFailure.NoMovement));
            Assert.That(after.BeginTurn().PreviewAttack("a","b"),Is.EqualTo(CampaignAttackFailure.None));
            Assert.That(after.ResolveEncounter("fight"),Is.SameAs(after));
        }

        [Test] public void AutomaticBattleIsRepeatableAndResultsSurviveRegistrySave()
        {
            var initial=Registry();BattleReport previous=null;
            for(int run=0;run<2;run++)
            {
                initial.TryBeginAttack("fight","a","b",42,out var fighting,out _);
                fighting.TryGetEncounter("fight",out var encounter);
                var result=new BattleAutoController(encounter.Battle.Commands).Run(1000);
                Assert.That(result.LimitReached,Is.False);
                var report=encounter.Battle.CreateReport();
                var ended=fighting.ResolveEncounter("fight");
                var loaded=CampaignArmyRegistry.Restore(ended.Capture(),initial);
                Assert.That(loaded.HasResolvedEncounter("fight"),Is.True);
                foreach(var army in loaded.Armies)
                {
                    var side=army.Army.Id=="a"?BattleSide.Attacker:BattleSide.Defender;
                    var formation=report.Formations[side==BattleSide.Attacker?0:1];
                    Assert.That(army.Army.Formations[0].Health,Is.EqualTo(formation.TotalHealth));
                    Assert.That(army.Army.Formations[0].Morale,Is.EqualTo(formation.Morale));
                }
                if(previous!=null){Assert.That(report.Outcome,Is.EqualTo(previous.Outcome));Assert.That(report.Attacker.RemainingHealth,Is.EqualTo(previous.Attacker.RemainingHealth));Assert.That(report.Defender.RemainingHealth,Is.EqualTo(previous.Defender.RemainingHealth));}
                previous=report;
            }
        }

        [Test] public void DuplicateEncounterCannotSpendMovementAgain()
        {
            var registry=Registry().BeginEncounter("fight","a","b",42);
            registry.TryGetEncounter("fight",out var encounter);encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Attacker);
            registry=registry.ResolveEncounter("fight");
            Assert.Throws<ArgumentException>(()=>registry.TryBeginAttack("fight","a","b",42,out _,out _));
            registry.TryGetArmy("a",out var unchanged);Assert.That(unchanged.Movement.RemainingMovement,Is.EqualTo(6));
        }

        [Test] public void SharedOrderExecutorRejectsWrongActorWithoutChangingRandomness()
        {
            var battle=BattlePrototype.Create(42);var random=battle.Commands.RandomState;var active=battle.Roster.ActiveFormationId;
            Assert.That(BattleOrderExecutor.TryExecute(battle.Commands,new BattleRecordedOrder {kind=BattleRecordedOrderKind.Defend,actorId=999},out _),Is.False);
            Assert.That(battle.Commands.RandomState,Is.EqualTo(random));Assert.That(battle.Roster.ActiveFormationId,Is.EqualTo(active));
        }
    }
}
