using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignArmyJoiningTests
    {
        private static HexCoordinates At(int q) => new(q, 0);
        private static CampaignFormationState Troop(string id, TroopProfile profile, long health, int morale) =>
            new(id, TroopCatalog.Get(profile).Attributes, health, morale, TroopCatalog.Get(profile).Kind);
        private static MapRouteGraph Graph() => new(new GeneratedMap(5, 1, 1,
            Enumerable.Range(0, 5).Select(q => new MapCell(At(q), TerrainType.Plain, false)).ToArray()));

        [Test]
        public void JoiningKeepsWoundedSoldiersGroupsTypesAndSurvivesRestore()
        {
            var graph = Graph();
            var first = Troop("first", TroopProfile.Spearmen, 91, 40);
            var second = Troop("second", TroopProfile.Spearmen, 61, 80);
            var ranged = Troop("bows", TroopProfile.Crossbowmen, 60, 70);
            var initial = new CampaignArmyRegistry(graph, new[] {
                new CampaignArmyPlacement(new CampaignArmyState("source", "castilla", new[]{first}), At(0), 8),
                new CampaignArmyPlacement(new CampaignArmyState("target", "castilla", new[]{second,ranged}), At(1), 6)
            });
            var owners = new CampaignSettlementRegistry(graph, new[]{new CampaignSettlement("home", "Casa", "castilla", At(1))});
            var military = new CampaignMilitaryState(initial, owners);
            Assert.IsTrue(military.TryJoinArmies("source", "target", initial, out var joined));
            Assert.AreEqual(2, initial.Armies.Count);
            Assert.IsFalse(joined.TryGetArmy("source", out _));
            Assert.IsFalse(joined.TryGetOccupant(At(0), out _));
            Assert.IsTrue(joined.TryGetArmy("target", out var target));
            Assert.AreEqual(0, target.Movement.RemainingMovement);
            Assert.AreEqual(6, target.Movement.MovementPerTurn);
            var stacks = CampaignTroopStack.Group(target.Army.Formations);
            Assert.AreEqual(2, stacks.Count);
            var spears = stacks.Single(g => g.Contains("first"));
            Assert.AreEqual(first.Quantity + second.Quantity, spears.Quantity);
            Assert.AreEqual(152, spears.Health);
            Assert.AreEqual(2, spears.Members.Count);
            Assert.IsFalse(military.TryJoinArmies("source", "target", joined, out var retry));
            Assert.AreSame(joined, retry);
            var savedMilitary = CampaignMilitaryState.Restore(military.Capture(), initial, owners, new[]{"castilla"}, 1);
            var restored = CampaignArmyRegistry.RestoreRecruited(joined.Capture(), initial, savedMilitary);
            Assert.IsTrue(restored.TryGetArmy("target", out var loaded));
            Assert.AreEqual(3, loaded.Army.Formations.Count);
            Assert.AreEqual(91, loaded.Army.Formations.Single(f => f.Id == "first").Health);
            Assert.AreEqual(40, loaded.Army.Formations.Single(f => f.Id == "first").Morale);
        }

        [Test]
        public void HeroCanStayInCityAndReturnAfterJsonSave()
        {
            var graph=Graph();
            var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(new CampaignArmyState("source","castilla",new[]{Troop("spear",TroopProfile.Spearmen,91,40)}),At(0),6)});
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(1))});
            var military=new CampaignMilitaryState(initial,owners);
            Assert.AreEqual("source",military.HeroPosition("castilla",initial).armyId);
            Assert.IsTrue(military.TryTransferHero("home","source",initial,true,out var stationed,out var roster));
            Assert.AreEqual("home",stationed.HeroPosition("castilla",roster).settlementId);
            Assert.AreEqual(0,roster.Armies[0].Movement.RemainingMovement);
            var json=UnityEngine.JsonUtility.ToJson(stationed.Capture());
            var data=UnityEngine.JsonUtility.FromJson<MilitarySaveData>(json);
            var restored=CampaignMilitaryState.Restore(data,initial,owners,new[]{"castilla"},1);
            Assert.AreEqual("home",restored.HeroPosition("castilla",roster).settlementId);
            Assert.IsTrue(restored.TryTransferHero("home","source",roster,false,out var visiting,out _));
            Assert.AreEqual("source",visiting.HeroPosition("castilla",roster).armyId);
            Assert.IsNull(visiting.HeroPosition("castilla",roster).settlementId);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void DepositedHostCanReturnToVisitorWithoutAnotherArmy(bool withHero)
        {
            var graph = Graph();
            var initial = new CampaignArmyRegistry(graph, new[] {
                new CampaignArmyPlacement(new CampaignArmyState("source", "castilla", new[] {
                    Troop("spear", TroopProfile.Spearmen, 91, 40),
                    Troop("bow", TroopProfile.Crossbowmen, 60, 70) }), At(1), 6) });
            var owners = new CampaignSettlementRegistry(graph, new[] { new CampaignSettlement("home", "Casa", "castilla", At(1)) });
            var military = new CampaignMilitaryState(initial, owners);
            Assert.IsTrue(military.TryTransferStack("home", "source", "spear", initial, true, out var one, out var roster));
            Assert.IsTrue(one.TryTransferStack("home", "source", "bow", roster, true, out var deposited, out var empty));
            deposited = deposited.WithDepositedArmyHero("source", "home", roster);
            Assert.IsFalse(empty.TryGetArmy("source", out _));
            Assert.IsTrue(deposited.TrySendToVisitor("home", null, "spear", withHero, empty, out var returned, out var visitors, out var id));
            Assert.IsTrue(visitors.TryGetArmy(id, out var visitor));
            Assert.AreEqual(At(1), visitor.Movement.Position);
            Assert.AreEqual(0, visitor.Movement.RemainingMovement);
            Assert.AreEqual(withHero ? 2 : 1, visitor.Army.Formations.Count);
            Assert.AreEqual(91, visitor.Army.Formations.Single(f => f.Id == "spear").Health);
            Assert.AreEqual(withHero ? id : null, returned.HeroPosition("castilla", visitors).armyId);
            Assert.AreEqual(withHero ? 0 : 1, returned.Garrison("home").Formations.Count);
            Assert.AreEqual(2, visitors.Armies.Sum(a => a.Army.Formations.Count) + returned.Garrison("home").Formations.Count);
        }
        [TestCase("other", 1)]
        [TestCase("castilla", 3)]
        public void ForeignOrDistantArmyCannotJoin(string faction, int destination)
        {
            var graph=Graph();
            var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(new CampaignArmyState("source","castilla",new[]{Troop("a",TroopProfile.Spearmen,100,70)}),At(0),6),
                new CampaignArmyPlacement(new CampaignArmyState("target",faction,new[]{Troop("b",TroopProfile.Spearmen,100,70)}),At(destination),6)});
            Assert.IsFalse(initial.TryJoin("source","target",out var rejected));
            Assert.AreSame(initial,rejected);
        }

        [Test]
        public void FullArmyRejectsTheWholeJoin()
        {
            var graph=Graph();
            var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(new CampaignArmyState("source","castilla",new[]{Troop("extra",TroopProfile.Spearmen,100,70)}),At(0),6),
                new CampaignArmyPlacement(new CampaignArmyState("target","castilla",Enumerable.Range(0,6).Select(i=>Troop("troop"+i,TroopProfile.Spearmen,100,70))),At(1),6)});
            Assert.IsFalse(initial.TryJoin("source","target",out var rejected));
            Assert.AreSame(initial,rejected);
        }

        [Test]
        public void StackTransferRejectsInsufficientCapacityWithoutMovingAnyMember()
        {
            var graph=Graph();
            var initial=new CampaignArmyRegistry(graph,new[]{
                new CampaignArmyPlacement(new CampaignArmyState("source","castilla",Enumerable.Range(0,5).Select(i=>Troop("troop"+i,TroopProfile.Spearmen,91,70))),At(0),6),
                new CampaignArmyPlacement(new CampaignArmyState("target","castilla",Enumerable.Range(0,2).Select(i=>Troop("target"+i,TroopProfile.MenAtArms,100,70))),At(2),6)});
            var owners=new CampaignSettlementRegistry(graph,new[]{new CampaignSettlement("home","Casa","castilla",At(1))});
            var military=new CampaignMilitaryState(initial,owners);
            var registry=initial;
            foreach(var formation in initial.Armies.Single(a=>a.Army.Id=="source").Army.Formations)
            {
                Assert.IsTrue(military.TryDeposit("home","source",formation.Id,registry,out var next,out var moved));
                military=next;registry=moved;
            }
            Assert.IsFalse(military.TryTransferStack("home","target","troop0",registry,false,out var rejected,out var unchanged));
            Assert.AreSame(military,rejected);
            Assert.AreSame(registry,unchanged);
            Assert.AreEqual(5,military.Garrison("home").Formations.Count);
        }
    }
}

