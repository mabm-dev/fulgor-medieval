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
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignFogSceneTests
    {
        private CampaignTurnController campaign;private CampaignBoardSelection selection;private CampaignSelectionPanel panel;private CampaignSettlementController towns;
        private CampaignSettlementDefinition enemyDefinition;private GeneratedMap map;private CampaignArmyRegistry initial;
        private HexCoordinates home,enemy;private bool hadSave;private string saved,oldScene;
        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);saved=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");oldScene=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();selection=campaign.GetComponent<CampaignBoardSelection>();panel=campaign.GetComponent<CampaignSelectionPanel>();towns=campaign.GetComponent<CampaignSettlementController>();
            map=ProceduralMapGenerator.Generate(24,16,12345);home=campaign.SettlementState.Settlements.Single().Position;
            enemy=map.Cells.First(c=>selection.Routes.CanStandAt(c.Coordinates)&&c.Coordinates.DistanceTo(home)>12&&MapPathfinder.TryFindPath(selection.Routes,home,c.Coordinates,out _)).Coordinates;
            towns.enabled=false;var data=new SerializedObject(towns);var defs=data.FindProperty("settlements");enemyDefinition=Object.Instantiate((CampaignSettlementDefinition)defs.GetArrayElementAtIndex(0).objectReferenceValue);
            var config=new SerializedObject(enemyDefinition);config.FindProperty("id").stringValue="secret-fort";config.FindProperty("displayName").stringValue="Fortaleza secreta";config.FindProperty("ownerFactionId").stringValue="rival";config.FindProperty("ownerDisplayName").stringValue="Señorío rival";config.FindProperty("position").vector2IntValue=new Vector2Int(enemy.Q,enemy.R);config.ApplyModifiedPropertiesWithoutUndo();
            defs.arraySize=2;defs.GetArrayElementAtIndex(1).objectReferenceValue=enemyDefinition;data.ApplyModifiedPropertiesWithoutUndo();towns.enabled=true;yield return null;yield return null;
            Configure(true);yield return null;campaign.GetComponent<CampaignFogMap>().ApplyNow();
        }
        private void Configure(bool fog,bool enemyNear=false,bool rivals=false)
        {
            var p=TroopCatalog.Get(TroopProfile.Spearmen);
            CampaignArmyPlacement Army(string id,string faction,HexCoordinates at)=>new(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",p.Attributes,100,70)}),at,6);
            var at=enemyNear?home.Neighbors().First(n=>selection.Routes.CanStandAt(n)):enemy;
            initial=new CampaignArmyRegistry(selection.Routes,new[]{Army("own","castilla",home),Army("secret-enemy","rival",at)});
            campaign.Initialize(panel,map,null,initial,"castilla",rivals,CampaignObjectiveKind.Conquest,true,fog);selection.ClearSelection();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,saved);else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);PlayerPrefs.Save();
            if(enemyDefinition!=null)Object.DestroyImmediate(enemyDefinition);if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        private CampaignArmyMarker EnemyMarker()=>campaign.GetComponentsInChildren<CampaignArmyMarker>().Single(m=>m.gameObject.name=="Hueste secret-enemy");
        private HexTileView Tile(HexCoordinates at)=>campaign.GetComponentsInChildren<HexTileView>().Single(t=>t.Cell.Coordinates==at);
        [UnityTest]public IEnumerator UnknownTerrainUnitsCitiesAndShadowsAreHiddenWhileSelectionStillWorks()
        {
            var fog=campaign.GetComponent<CampaignFogMap>();fog.ApplyNow();Assert.That(fog.UnknownCount,Is.GreaterThan(0));Assert.That(campaign.CanObserve(home),Is.True);Assert.That(campaign.CanObserve(enemy),Is.False);
            var tile=Tile(enemy);Assert.That(tile.GetComponent<Renderer>().forceRenderingOff,Is.True);Assert.That(tile.GetComponent<Collider>().enabled,Is.True);Assert.That(tile.transform.Find("Territorio sin explorar").gameObject.activeSelf,Is.True);
            Assert.That(EnemyMarker().IsVisible,Is.False);Assert.That(towns.Views[enemy].GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff),Is.True);Assert.That(towns.Views[enemy].GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True);
            Assert.That(selection.LocateCell(enemy),Is.True);Assert.That(panel.MoveArmyButton.interactable,Is.False);Assert.That(towns.Details,Does.Not.Contain("Fortaleza secreta"));yield return null;
        }
        [UnityTest]public IEnumerator UnknownEnemyAndUnknownEmptyCellHaveTheSameDisclosureAndNoRemoteActions()
        {
            selection.LocateCell(enemy);string hidden=towns.Details;
            Assert.That(campaign.AttackSelectedArmy(),Is.False);Assert.That(campaign.HasPendingBattle,Is.False);Assert.That(towns.OpenCityAt(enemy),Is.False);Assert.That(towns.VisitButton.interactable,Is.False);
            var city=campaign.gameObject.AddComponent<CityManagementView>();Assert.That(city.Open(towns,campaign.SettlementState.Settlements.Single(c=>c.Id=="secret-fort"),enemyDefinition,false),Is.False);
            var empty=map.Cells.First(c=>!campaign.Fog.IsExplored("castilla",c.Coordinates)&&!campaign.SettlementState.TryGet(c.Coordinates,out _)).Coordinates;
            selection.LocateCell(empty);Assert.That(towns.Details,Is.EqualTo(hidden));Assert.That(panel.MoveArmyButton.interactable,Is.False);
            var before=campaign.Army.Position;campaign.MoveToSelectedCell();Assert.That(campaign.Army.Position,Is.EqualTo(before));
            var text=string.Join("\n",panel.GetComponentsInChildren<Text>().Select(t=>t.text));Assert.That(text,Does.Not.Contain("Oro: Sí"));Assert.That(text,Does.Not.Contain("secret-enemy"));yield return null;
        }
        [UnityTest]public IEnumerator MarchingToAVisibleFrontierDiscoversNewLandWithoutRefillingMovement()
        {
            int before=campaign.Fog.ExploredCount("castilla");var destination=map.Cells.Where(c=>campaign.Fog.IsVisible("castilla",c.Coordinates)&&c.Coordinates.DistanceTo(home)==2)
                .Select(c=>c.Coordinates).First(at=>CampaignFogOrders.TryPlan("own",at,campaign.ArmyRegistry,campaign.Military,campaign.SettlementState,selection.Routes,campaign.Fog,out var p,out _)&&p.TotalCost<=6);
            CampaignFogOrders.TryPlan("own",destination,campaign.ArmyRegistry,campaign.Military,campaign.SettlementState,selection.Routes,campaign.Fog,out var route,out _);
            selection.LocateCell(destination);Assert.That(panel.MoveArmyButton.interactable,Is.True);panel.MoveArmyButton.onClick.Invoke();yield return null;
            Assert.That(campaign.Army.Position,Is.EqualTo(destination));Assert.That(campaign.Army.RemainingMovement,Is.EqualTo(6-route.TotalCost));Assert.That(campaign.Fog.ExploredCount("castilla"),Is.GreaterThan(before));
            var snapshot=campaign.CaptureCampaignSnapshot();Assert.That(snapshot.version,Is.EqualTo(11));Assert.That(snapshot.exploration.rulesVersion,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator SaveRestorePreservesExplorationResourcesMovementAndNoTurnIsReplayed()
        {
            var json=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());int explored=campaign.Fog.ExploredCount("castilla");var stock=campaign.State.Resources;int movement=campaign.Army.RemainingMovement;
            Assert.That(campaign.TryLoadCampaignSnapshot(json),Is.True);Assert.That(campaign.TryLoadCampaignSnapshot(json),Is.True);
            Assert.That(campaign.Fog.ExploredCount("castilla"),Is.EqualTo(explored));Assert.That(campaign.Army.RemainingMovement,Is.EqualTo(movement));Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(campaign.State.Turn,Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(campaign.CaptureCampaignSnapshot()),Is.EqualTo(json));
            var status=panel.GetComponentsInChildren<Text>(true).Single(t=>t.gameObject.name=="Save Status");
            Assert.That(status.text,Is.EqualTo("Partida cargada · turno 1"));Assert.That(campaign.TrySaveCampaign(),Is.True);
            Assert.That(status.text,Is.EqualTo("Partida guardada · turno 1"));yield return null;
        }
        [UnityTest]public IEnumerator CorruptExplorationLoadLeavesTheEntireLiveCampaignUntouched()
        {
            var data=campaign.CaptureCampaignSnapshot();var state=campaign.State;var armies=campaign.ArmyRegistry;var knowledge=campaign.Fog;
            data.exploration.factions.Single(f=>f.factionId=="castilla").explored[0].q=999;
            LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)),Is.False);
            Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(armies));Assert.That(campaign.Fog,Is.SameAs(knowledge));yield return null;
        }
        [UnityTest]public IEnumerator V10MigrationRemembersThePreviousFullMapAndStillHidesRemoteEnemies()
        {
            Configure(false);var payload=campaign.CaptureCampaignSnapshot();Assert.That(payload.version,Is.EqualTo(10));Configure(true);
            Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(payload)),Is.True);Assert.That(campaign.Fog.ExploredCount("castilla"),Is.EqualTo(map.Cells.Count));Assert.That(campaign.VisibilityAt(enemy),Is.EqualTo(CampaignVisibility.Explored));
            campaign.GetComponent<CampaignFogMap>().ApplyNow();Assert.That(EnemyMarker().IsVisible,Is.False);selection.LocateCell(enemy);Assert.That(towns.Details,Does.Contain("Último control conocido"));Assert.That(towns.VisitButton.interactable,Is.False);yield return null;
        }
        [UnityTest]public IEnumerator LosingSightHidesTheEnemyAndDoesNotAnimateItsHiddenRoute()
        {
            Configure(true,true);campaign.GetComponent<CampaignFogMap>().ApplyNow();Assert.That(EnemyMarker().IsVisible,Is.True);
            var data=campaign.CaptureCampaignSnapshot();var unit=data.armyRegistry.armies.Single(a=>a.id=="secret-enemy");unit.q=enemy.Q;unit.r=enemy.R;
            var moved=CampaignArmyRegistry.Restore(data.armyRegistry,initial);data.exploration=campaign.Fog.Refresh(moved,campaign.SettlementState,campaign.State.Turn).Capture();
            Assert.That(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)),Is.True);campaign.GetComponent<CampaignFogMap>().ApplyNow();Assert.That(EnemyMarker().IsVisible,Is.False);Assert.That(EnemyMarker().IsMoving,Is.False);yield return null;
        }
        [UnityTest]public IEnumerator DisablingFogRestoresExistingRenderersAndReleasesTheCovers()
        {
            var tile=Tile(enemy);Assert.That(tile.GetComponent<Renderer>().forceRenderingOff,Is.True);Configure(false);yield return null;
            Assert.That(campaign.Fog,Is.Null);Assert.That(EnemyMarker().IsVisible,Is.True);Assert.That(tile.GetComponent<Renderer>().forceRenderingOff,Is.False);Assert.That(tile.transform.Find("Territorio sin explorar"),Is.Null);
        }
        [UnityTest]public IEnumerator CatalogAcceptsV11AndLegacyV10AndRejectsDamagedKnowledge()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CampaignLaunchCatalogTests.CatalogPath);var entry=catalog.Find("castilla-conquest");Assert.That(entry.Scenario.EnableFogOfWar,Is.True);
            var fresh=CampaignLaunchCatalogTests.InitialSnapshot(entry,11);Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(fresh),out _),Is.True);fresh.exploration.rulesVersion=999;Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(fresh),out _),Is.False);
            var legacy=CampaignLaunchCatalogTests.InitialSnapshot(entry,10);Assert.That(entry.TryValidateSnapshot(JsonUtility.ToJson(legacy),out _),Is.True);yield return null;
        }
        [UnityTest]public IEnumerator RivalClosingDoesNotDiscloseHiddenRecruitmentOrMarchCounts()
        {
            Configure(true,false,true);campaign.CompleteTurn();yield return null;
            Assert.That(campaign.HasPendingBattle,Is.False);Assert.That(campaign.IsRivalPhaseActive,Is.False);
            Assert.That(campaign.LastRivalSummary,Does.StartWith("Turno rival completado"));
            Assert.That(campaign.LastRivalSummary,Does.Not.Contain(" marchas"));Assert.That(campaign.LastRivalSummary,Does.Not.Contain(" levas"));
        }
    }
}
