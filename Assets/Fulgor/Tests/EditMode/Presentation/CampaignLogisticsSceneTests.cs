using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignLogisticsSceneTests
    {
        private CampaignTurnController campaign;private CampaignBoardSelection selection;private CampaignSelectionPanel panel;
        private CampaignArmyRegistry initial;private GeneratedMap map;private HexCoordinates home,enemy;
        private bool hadSave;private string saved,oldScene;
        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);saved=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");oldScene=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();selection=campaign.GetComponent<CampaignBoardSelection>();panel=campaign.GetComponent<CampaignSelectionPanel>();
            map=ProceduralMapGenerator.Generate(24,16,12345);home=campaign.SettlementState.Settlements.Single().Position;
            enemy=map.Cells.First(c=>selection.Routes.CanStandAt(c.Coordinates)&&c.Coordinates.DistanceTo(home)>12).Coordinates;
            Configure(true);yield return null;
        }
        private void Configure(bool logistics,bool near=false)
        {
            var p=TroopCatalog.Get(TroopProfile.Spearmen);
            CampaignArmyPlacement Army(string id,string faction,HexCoordinates at)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",p.Attributes,100000,70)}),at,6);
            var at=near?home.Neighbors().First(n=>selection.Routes.CanStandAt(n)):enemy;
            initial=new CampaignArmyRegistry(selection.Routes,new[]{Army("own","castilla",home),Army("enemy","rival",at)});
            campaign.Initialize(panel,map,null,initial,"castilla",true,CampaignObjectiveKind.Conquest,logistics);selection.ClearSelection();
        }
        private CampaignSaveData Snapshot()=>campaign.CaptureCampaignSnapshot();
        private void SetSurvivors(bool rich,int movement=6)
        {
            var data=Snapshot();data.grain=data.gold=rich?1000000:0;
            var own=data.armyRegistry.armies.Single(a=>a.id=="own");own.formations[0].health=99993;own.formations[0].morale=40;own.remainingMovement=movement;
            Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)),Is.True);
        }
        private CampaignArmyOnMap Own(){campaign.ArmyRegistry.TryGetArmy("own",out var army);return army;}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,saved);else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);PlayerPrefs.Save();
            if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        [UnityTest]public IEnumerator PaidTurnUsesThePublishedForecastAndRecoversOnlyExistingSurvivors()
        {
            SetSurvivors(true);var expected=campaign.State.ProjectedResources;var f=Own().Army.Formations.Single();int quantity=f.Quantity;
            Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(campaign.CityUpkeepPerTurn.Add(campaign.ArmyUpkeepPerTurn)));
            campaign.CompleteTurn();Assert.That(campaign.State.Resources,Is.EqualTo(expected));Assert.That(campaign.State.Turn,Is.EqualTo(2));
            f=Own().Army.Formations.Single();Assert.That(f.Health,Is.EqualTo(99995));Assert.That(f.Quantity,Is.EqualTo(quantity));Assert.That(f.Morale,Is.EqualTo(45));
            Assert.That(campaign.LastLogisticsReport.Reports.Single(r=>r.ArmyId=="own").Paid,Is.True);yield return null;
        }
        [UnityTest]public IEnumerator ShortageAffectsPlayerAndRivalAndPersistsTheReducedMovement()
        {
            SetSurvivors(false);campaign.CompleteTurn();Assert.That(campaign.IsRivalPhaseActive,Is.False);Assert.That(campaign.HasPendingBattle,Is.False);
            Assert.That(Own().Movement.RemainingMovement,Is.EqualTo(3));Assert.That(Own().Army.Formations.Single().Morale,Is.EqualTo(30));Assert.That(Own().Army.Formations.Single().Health,Is.EqualTo(99993));
            Assert.That(campaign.LastLogisticsReport.Reports.Single(r=>r.ArmyId=="enemy").Paid,Is.False);
            var data=Snapshot();Assert.That(data.version,Is.EqualTo(10));Assert.That(data.logisticsVersion,Is.EqualTo(1));var stock=campaign.State.Resources;
            Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)),Is.True);Assert.That(Own().Movement.RemainingMovement,Is.EqualTo(3));Assert.That(Own().Army.Formations.Single().Morale,Is.EqualTo(30));Assert.That(campaign.State.Resources,Is.EqualTo(stock));yield return null;
        }
        [UnityTest]public IEnumerator MovementPreventsRecoveryAndLoadingDoesNotChargeOrHealAgain()
        {
            SetSurvivors(true,5);campaign.CompleteTurn();Assert.That(Own().Army.Formations.Single().Health,Is.EqualTo(99993));Assert.That(Own().Army.Formations.Single().Morale,Is.EqualTo(40));
            var json=JsonUtility.ToJson(Snapshot());var stock=campaign.State.Resources;
            Assert.That(campaign.TryLoadCampaignSnapshot(json),Is.True);Assert.That(campaign.TryLoadCampaignSnapshot(json),Is.True);
            Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(campaign.State.Turn,Is.EqualTo(2));Assert.That(Own().Army.Formations.Single().Health,Is.EqualTo(99993));Assert.That(campaign.LastLogisticsReport,Is.Null);yield return null;
        }
        [UnityTest]public IEnumerator V9MigrationPreservesCurrentStateAndStartsNewRulesAtNextClosing()
        {
            Configure(false);var old=Snapshot();Assert.That(old.version,Is.EqualTo(9));old.grain=old.gold=1000000;
            old.armyRegistry.armies.Single(a=>a.id=="own").formations[0].health=99993;old.armyRegistry.armies.Single(a=>a.id=="own").formations[0].morale=40;
            Configure(true);Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(old)),Is.True);
            Assert.That(campaign.State.Resources.Grain,Is.EqualTo(1000000));Assert.That(campaign.State.Turn,Is.EqualTo(1));Assert.That(Own().Army.Formations.Single().Health,Is.EqualTo(99993));Assert.That(Snapshot().version,Is.EqualTo(10));
            campaign.CompleteTurn();Assert.That(Own().Army.Formations.Single().Health,Is.EqualTo(99995));yield return null;
        }
        [UnityTest]public IEnumerator IncompatibleRulesRejectLoadAtomically()
        {
            SetSurvivors(true);var data=Snapshot();var state=campaign.State;var armies=campaign.ArmyRegistry;var military=campaign.Military;
            data.logisticsVersion=42;LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));
            Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)),Is.False);Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(armies));Assert.That(campaign.Military,Is.SameAs(military));yield return null;
        }
        [UnityTest]public IEnumerator PendingBattleBlocksTheEntireClosingWithoutChargingAnything()
        {
            Configure(true,true);var target=home.Neighbors().First(n=>campaign.ArmyRegistry.TryGetOccupant(n,out _));selection.LocateCell(target);Assert.That(campaign.AttackSelectedArmy(),Is.True);
            var state=campaign.State;int rivalTurn=campaign.Rivals.ClosedTurn;campaign.CompleteTurn();Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.Rivals.ClosedTurn,Is.EqualTo(rivalTurn));Assert.That(campaign.LastLogisticsReport,Is.Null);yield return null;
        }
        [UnityTest]public IEnumerator PlayableConquestEnablesLogisticsAndCatalogAcceptsNewAndLegacySnapshots()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CampaignLaunchCatalogTests.CatalogPath);var entry=catalog.Find("castilla-conquest");Assert.That(entry.Scenario.EnableMilitaryLogistics,Is.True);
            var fresh=CampaignLaunchCatalogTests.InitialSnapshot(entry,10);Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(fresh),out _),Is.True);
            var old=CampaignLaunchCatalogTests.InitialSnapshot(entry,9);Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(old),out _),Is.True);
            fresh.logisticsVersion=2;Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(fresh),out _),Is.False);
            yield return new ExitPlayMode();EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorConquest.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();Assert.That(campaign.LogisticsEnabled,Is.True);Assert.That(campaign.CaptureCampaignSnapshot().version,Is.EqualTo(campaign.FogEnabled?11:10));campaign.NewGame();yield return null;Assert.That(campaign.LogisticsEnabled,Is.True);
        }
    }
}
