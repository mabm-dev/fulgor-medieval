using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignLaunchCatalogTests
    {
        internal const string CatalogPath="Assets/Fulgor/Scenarios/CampaignLaunchCatalog.asset";
        private CampaignLaunchCatalog Catalog=>AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CatalogPath);
        internal static CampaignSaveData InitialSnapshot(CampaignLaunchEntry entry,int version=9)
        {
            var scenario=entry.Scenario;var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);var routes=scenario.CreateRouteGraph(map);
            var armies=scenario.CreateArmyRegistry(routes);var mission=scenario.CreateMission(map,routes,armies);
            var owners=new CampaignSettlementRegistry(routes,entry.Settlements.Select(s=>s.Create()));
            string context=CampaignSaveData.ScenarioSignature(map,routes,mission);var state=new CampaignTurnState(1,default,default);
            CampaignSaveData data;
            if(armies==null)data=CampaignSaveData.Capture(context,state,new ArmyMovementState(routes,map.Cells.First(c=>routes.CanStandAt(c.Coordinates)).Coordinates,6));
            else
            {
                context=CampaignSaveData.ArmyScenarioSignature(context,armies,scenario.PlayerFactionId);
                data=CampaignSaveData.CaptureMultiple(context,state,armies,armies.Armies.First(a=>a.Army.FactionId==scenario.PlayerFactionId).Army.Id,scenario.PlayerFactionId);
            }
            if(version>=5)data.IncludeOwnership(context,owners,owners);
            if(version>=6)data.IncludeMilitary(new CampaignMilitaryState(armies,owners));
            if(version>=7)data.IncludeMarches(new CampaignMarchOrders());
            if(version>=8)
            {
                var factions=owners.Settlements.Select(c=>c.OwnerFactionId).Concat(armies?.Armies.Select(a=>a.Army.FactionId)??Array.Empty<string>()).Where(f=>f!=scenario.PlayerFactionId).Distinct();
                data.IncludeRivals(new CampaignRivalState(scenario.EnableStrategicRivals,factions));
            }
            if(version>=9)data.IncludeCampaignRules(scenario.ObjectiveKind);
            if(version>=10)data.IncludeLogistics(scenario.EnableMilitaryLogistics);
            if(version>=11)data.IncludeFog(new CampaignFogState(map,owners.Settlements.Select(c=>c.OwnerFactionId).Concat(armies.Armies.Select(a=>a.Army.FactionId)).Append(scenario.PlayerFactionId).Distinct()).Refresh(armies,owners,1));
            data.version=version;return data;
        }
        [Test]public void BothCurrentCampaignsAreRecognizedWithoutSceneNamesInSaveData()
        {
            Assert.That(Catalog.Entries.Count,Is.EqualTo(2));
            foreach(var entry in Catalog.Entries)
            {
                var json=JsonUtility.ToJson(InitialSnapshot(entry));Assert.That(Catalog.TryFindSnapshot(json,out var matched,out var data),Is.True);
                Assert.That(matched.Id,Is.EqualTo(entry.Id));Assert.That(data.version,Is.EqualTo(9));Assert.That(json,Does.Not.Contain(entry.SceneName));
            }
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        public void CompatibleLegacyMissionSaveIsAccepted(int version)
        {
            var entry=Catalog.Find("castilla-mission");Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(InitialSnapshot(entry,version)),out var data),Is.True);Assert.That(data.version,Is.EqualTo(version));
        }
        [Test]public void OldConquestAndFutureVersionsAreRejectedBeforeNavigation()
        {
            var entry=Catalog.Find("castilla-conquest");var data=InitialSnapshot(entry,8);Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(data),out _),Is.False);
            data=InitialSnapshot(entry);data.version=999;Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(data),out _),Is.False);
        }
        [TestCase("context")][TestCase("owner")][TestCase("formation")][TestCase("army")][TestCase("faction")][TestCase("military")]
        [TestCase("rival")][TestCase("turn")][TestCase("march")][TestCase("city")][TestCase("objective")][TestCase("resources")]
        public void CorruptSaveCannotEnableContinuation(string damage)
        {
            var data=InitialSnapshot(Catalog.Find("castilla-conquest"));
            if(damage=="context")data.context="other";
            if(damage=="owner")data.settlementOwners[0].settlementId="unknown";
            if(damage=="formation")data.armyRegistry.armies[0].formations[0].health++;
            if(damage=="army")data.armyRegistry.armies[0].id="unknown";
            if(damage=="faction")data.armyRegistry.armies[0].formations[0].id=data.armyRegistry.armies.Last().formations[0].id;
            if(damage=="military")data.military=null;
            if(damage=="rival")data.rivals.closedTurn++;
            if(damage=="turn")data.turn=0;
            if(damage=="march"){data.marchOrders=new[]{new CampaignMarchData{armyId="unknown",q=0,r=0}};data.marchOrderCount=1;}
            if(damage=="city")data.cities=new[]{new CityDevelopmentData{settlementId=data.settlementOwners[0].settlementId,completed=new[]{"unknown"}}};
            if(damage=="objective")data.objectiveKind=CampaignObjectiveKind.ReachDestination;
            if(damage=="resources")data.gold=-1;
            Assert.That(Catalog.TryFindSnapshot(JsonUtility.ToJson(data),out var matched,out _),Is.False);Assert.That(matched,Is.Null);
        }
        [TestCase("")][TestCase("{}")] [TestCase("not-json")]
        public void InvalidJsonIsRejectedWithoutThrowing(string json)=>Assert.That(Catalog.TryFindSnapshot(json,out _,out _),Is.False);
        [Test]public void DuplicateIdsOrScenesAreRejectedAndAnAmbiguousCatalogIsNotChosen()
        {
            var entry=Catalog.Entries[0];var copy=ScriptableObject.CreateInstance<CampaignLaunchCatalog>();
            try
            {
                Assert.Throws<ArgumentException>(()=>copy.Configure(new[]{entry,entry}));
                var other=new CampaignLaunchEntry("other",entry.Title,entry.Description,"OtherScene",entry.Scenario,entry.Settlements.ToArray());
                copy.Configure(new[]{entry,other});Assert.That(copy.TryFindSnapshot(JsonUtility.ToJson(InitialSnapshot(entry)),out _,out _),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
    }
}
