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
    public sealed class CampaignConquestSceneTests
    {
        private const string SaveKey="Fulgor.Campaign.Prototype.Save.v1",TargetId="rival-fort";
        private bool hadSave;private string oldSave,oldScene;
        private CampaignTurnController campaign;private CampaignSettlementController towns;
        private CampaignBoardSelection selection;private CampaignSelectionPanel panel;
        private CampaignSettlementDefinition homeDefinition,rivalDefinition;
        private GeneratedMap map;private CampaignArmyRegistry initial;
        private HexCoordinates home,target,ally;
        private CityManagementView City=>campaign.GetComponent<CityManagementView>();
        private BattleLabController Battle=>campaign.GetComponent<BattleLabController>();

        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(SaveKey);oldSave=PlayerPrefs.GetString(SaveKey,"");oldScene=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();towns=campaign.GetComponent<CampaignSettlementController>();
            selection=campaign.GetComponent<CampaignBoardSelection>();panel=campaign.GetComponent<CampaignSelectionPanel>();map=ProceduralMapGenerator.Generate(24,16,12345);
            home=towns.Registry.Settlements.Single().Position;
            target=home.Neighbors().First(n=>selection.Routes.CanStandAt(n));
            ally=home.Neighbors().First(n=>n!=target&&selection.Routes.CanStandAt(n));
            towns.enabled=false;var config=new SerializedObject(towns);var definitions=config.FindProperty("settlements");
            homeDefinition=Object.Instantiate((CampaignSettlementDefinition)definitions.GetArrayElementAtIndex(0).objectReferenceValue);
            rivalDefinition=Object.Instantiate(homeDefinition);
            Configure(homeDefinition,"home-fort","Castilla","castilla","Castilla",home);
            Configure(rivalDefinition,TargetId,"Villa rival","rival","Señorío rival",target);
            definitions.arraySize=2;definitions.GetArrayElementAtIndex(0).objectReferenceValue=homeDefinition;definitions.GetArrayElementAtIndex(1).objectReferenceValue=rivalDefinition;
            config.ApplyModifiedPropertiesWithoutUndo();towns.enabled=true;yield return null;yield return null;
            Reinitialize(false);yield return null;
        }
        private static void Configure(CampaignSettlementDefinition definition,string id,string name,string faction,string owner,HexCoordinates at)
        {
            var s=new SerializedObject(definition);s.FindProperty("id").stringValue=id;s.FindProperty("displayName").stringValue=name;
            s.FindProperty("ownerFactionId").stringValue=faction;s.FindProperty("ownerDisplayName").stringValue=owner;
            s.FindProperty("position").vector2IntValue=new Vector2Int(at.Q,at.R);s.ApplyModifiedPropertiesWithoutUndo();
        }
        private static CampaignArmyPlacement Placement(string id,string faction,HexCoordinates at,long health)
        {
            var p=TroopCatalog.Get(TroopProfile.Spearmen);
            return new CampaignArmyPlacement(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",p.Attributes,health,70)}),at,20);
        }
        private void Reinitialize(bool defended)
        {
            var list=new System.Collections.Generic.List<CampaignArmyPlacement>{Placement("own","castilla",home,500),Placement("own-2","castilla",ally,100)};
            if(defended)list.Add(Placement("enemy","rival",target,20));
            initial=new CampaignArmyRegistry(selection.Routes,list);campaign.Initialize(panel,map,null,initial,"castilla");selection.ClearSelection();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(SaveKey,oldSave);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
            if(homeDefinition!=null)Object.DestroyImmediate(homeDefinition);if(rivalDefinition!=null)Object.DestroyImmediate(rivalDefinition);
            if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        private void Occupy(){selection.LocateCell(target);Assert.That(panel.MoveArmyButton.interactable,Is.True);panel.MoveArmyButton.onClick.Invoke();}
        private string Owner(){campaign.SettlementState.TryGet(TargetId,out var city);return city.OwnerFactionId;}
        private void SeedEnemyCity()
        {
            campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            data.cities=new[]{new CityDevelopmentData {settlementId=TargetId,completed=new[]{"market"},pendingId="well",completionTurn=campaign.State.Turn+1}};
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));campaign.LoadCampaign();
        }

        [UnityTest]public IEnumerator EmptyFortChangesControlManagementAndTerritoryIncomeOnEntry()
        {
            var before=campaign.TerritoryIncome;var stock=campaign.State.Resources;
            selection.LocateCell(target);Assert.That(panel.MoveArmyButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Does.Contain("OCUPAR"));
            panel.MoveArmyButton.onClick.Invoke();yield return null;
            Assert.That(Owner(),Is.EqualTo("castilla"));Assert.That(towns.CanManage(TargetId),Is.True);Assert.That(rivalDefinition.Create().OwnerFactionId,Is.EqualTo("rival"));
            Assert.That(campaign.TerritoryIncome,Is.EqualTo(CampaignIncomeCalculator.FromMap(map)));Assert.That(campaign.TerritoryIncome,Is.Not.EqualTo(before));
            Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(campaign.LastConqueredSettlementId,Is.EqualTo(TargetId));
            Assert.That(towns.OpenCityAt(target),Is.True);City.SelectPanel(CityPanelKind.Overview);Assert.That(City.BodyText,Does.Contain("Control de Castilla"));
            City.SelectPanel(CityPanelKind.Treasury);Assert.That(City.PrimaryButton.interactable,Is.True);
            var forecast=campaign.State.ProjectedResources;City.CompleteTurn();Assert.That(campaign.State.Resources,Is.EqualTo(forecast));
            Assert.That(City.Construct("market"),Is.True);
        }

        [UnityTest]public IEnumerator VictoryOverDefendingArmyOccupiesAndConquersOnce()
        {
            Reinitialize(true);selection.LocateCell(target);Assert.That(campaign.AttackSelectedArmy(),Is.True);
            var encounter=Battle.ActiveCampaignEncounter;
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)Battle.AdvanceCampaignAutomaticStep();
            Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));Assert.That(Battle.ReturnToCampaign(),Is.True);yield return null;
            Assert.That(Owner(),Is.EqualTo("castilla"));Assert.That(campaign.Army.Position,Is.EqualTo(target));Assert.That(campaign.Army.RemainingMovement,Is.Zero);
            Assert.That(campaign.ArmyRegistry.TryGetArmy("enemy",out _),Is.False);Assert.That(campaign.HasPendingBattle,Is.False);
            var state=campaign.State;Assert.That(campaign.ApplyEncounterResult(encounter.Id),Is.True);Assert.That(campaign.State,Is.SameAs(state));
            Assert.That(campaign.TerritoryIncome,Is.EqualTo(CampaignIncomeCalculator.FromMap(map)));
        }

        [UnityTest]public IEnumerator RetreatDoesNotChangeOwnerOrIncome()
        {
            Reinitialize(true);var income=campaign.State.IncomePerTurn;selection.LocateCell(target);campaign.AttackSelectedArmy();
            Assert.That(Battle.WithdrawPlayerArmy(),Is.True);Assert.That(Battle.ReturnToCampaign(),Is.True);
            Assert.That(Owner(),Is.EqualTo("rival"));Assert.That(towns.CanManage(TargetId),Is.False);Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(income));yield return null;
        }

        [UnityTest]public IEnumerator SaveV6KeepsConquestBuildingsPendingWorkAndDoesNotDuplicateIncome()
        {
            SeedEnemyCity();Assert.That(campaign.CityIncomePerTurn.Gold,Is.Zero);Occupy();
            Assert.That(campaign.CityIncomePerTurn.Gold,Is.EqualTo(8));Assert.That(campaign.CityState(TargetId).PendingId,Is.EqualTo("well"));
            var state=campaign.State;campaign.SaveCampaign();var payload=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(payload.version,Is.EqualTo(9));Assert.That(payload.settlementOwners.Length,Is.EqualTo(2));
            campaign.CompleteTurn();Assert.That(campaign.CityState(TargetId).Has("well"),Is.True);
            for(int load=0;load<2;load++)
            {
                campaign.LoadCampaign();yield return null;Assert.That(Owner(),Is.EqualTo("castilla"));
                Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(state.IncomePerTurn));Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(state.ExpensesPerTurn));
                Assert.That(campaign.State.Resources,Is.EqualTo(state.Resources));Assert.That(campaign.CityState(TargetId).PendingId,Is.EqualTo("well"));
            }
        }

        [UnityTest]public IEnumerator LoadingAnEarlierOwnerUpdatesAnOpenCityToInspection()
        {
            SeedEnemyCity();campaign.SaveCampaign();string earlier=PlayerPrefs.GetString(SaveKey);Occupy();
            towns.OpenCityAt(target);City.SelectPanel(CityPanelKind.Treasury);Assert.That(City.PrimaryButton.interactable,Is.True);
            PlayerPrefs.SetString(SaveKey,earlier);campaign.LoadCampaign();yield return null;
            Assert.That(Owner(),Is.EqualTo("rival"));Assert.That(City.PrimaryButton.interactable,Is.False);Assert.That(City.Construct("well"),Is.False);
            Assert.That(campaign.CityIncomePerTurn.Gold,Is.Zero);City.SelectPanel(CityPanelKind.Overview);Assert.That(City.BodyText,Does.Contain("Señorío rival"));
        }

        [UnityTest]public IEnumerator DamagedOwnersDoNotReplaceArmiesResourcesOrOwnership()
        {
            Occupy();campaign.SaveCampaign();string valid=PlayerPrefs.GetString(SaveKey);var state=campaign.State;var owners=campaign.SettlementState;var armies=campaign.ArmyRegistry;
            foreach(var damage in new[]{"missing","duplicate","faction","context","mode"})
            {
                var data=JsonUtility.FromJson<CampaignSaveData>(valid);
                if(damage=="missing")data.settlementOwners=new SettlementOwnershipData[0];
                if(damage=="duplicate")data.settlementOwners[1]=data.settlementOwners[0];
                if(damage=="faction")data.settlementOwners[0].ownerFactionId="invented";
                if(damage=="context")data.context="other";if(damage=="mode")data.multipleArmies=false;
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));campaign.LoadCampaign();
                Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.SettlementState,Is.SameAs(owners));Assert.That(campaign.ArmyRegistry,Is.SameAs(armies));
            }
            yield return null;
        }

        [UnityTest]public IEnumerator LegacyV4UsesInitialOwnersAndNewGameAlsoRestoresThem()
        {
            Occupy();campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            data.version=4;data.settlementOwners=null;
            data.context=CampaignSaveData.ArmyScenarioSignature(CampaignSaveData.ScenarioSignature(map,selection.Routes,null),initial,"castilla");
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));campaign.LoadCampaign();yield return null;
            Assert.That(Owner(),Is.EqualTo("rival"));selection.LocateCell(target);Assert.That(campaign.CaptureSelectedSettlement(),Is.True);
            campaign.NewGame();yield return null;yield return null;
            Assert.That(Owner(),Is.EqualTo("rival"));Assert.That(campaign.State.Turn,Is.EqualTo(1));Assert.That(towns.CanManage(TargetId),Is.False);
        }
    }
}
