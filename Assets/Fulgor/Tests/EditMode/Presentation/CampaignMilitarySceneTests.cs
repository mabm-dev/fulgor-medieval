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
    public sealed class CampaignMilitarySceneTests
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
        private void FundAndOpen()
        {
            for(int i=0;i<12;i++)campaign.CompleteTurn();
            Assert.That(towns.OpenCityAt(home),Is.True);City.SelectPanel(CityPanelKind.Armies);
        }
        [UnityTest]public IEnumerator ButtonsRecruitCompleteAndAssignWithSharedResourcesAndCivilWork()
        {
            FundAndOpen();var turn=campaign.State.Turn;var stock=campaign.State.Resources;
            Assert.That(City.PrimaryButton.interactable,Is.True);City.PrimaryButton.onClick.Invoke();
            Assert.That(campaign.Military.Pending("home-fort").completionTurn,Is.EqualTo(turn+2));
            Assert.That(City.PrimaryButton.interactable,Is.False);stock.TrySpend(RecruitmentCatalog.Get(TroopProfile.Spearmen).Cost,out var expected);
            Assert.That(campaign.State.Resources,Is.EqualTo(expected));Assert.That(campaign.TryConstruct("home-fort","well"),Is.True);
            City.SecondaryButton.onClick.Invoke();City.SecondaryButton.onClick.Invoke();
            Assert.That(campaign.Military.Garrison("home-fort").Formations.Single().Quantity,Is.EqualTo(20));Assert.That(campaign.CityState("home-fort").Has("well"),Is.True);
            City.SelectMilitaryMode(CityMilitaryMode.Assign);Assert.That(City.PrimaryButton.interactable,Is.True);City.PrimaryButton.onClick.Invoke();
            campaign.ArmyRegistry.TryGetArmy("own",out var own);Assert.That(own.Army.Formations.Count,Is.EqualTo(2));Assert.That(own.Movement.RemainingMovement,Is.Zero);
            Assert.That(campaign.Military.Garrison("home-fort").IsDissolved,Is.True);yield return null;
        }
        [UnityTest]public IEnumerator DepositLastFormationDissolvesArmyAndMusterCreatesSelectableMarker()
        {
            FundAndOpen();City.SelectMilitaryMode(CityMilitaryMode.Defend);Assert.That(City.PrimaryButton.interactable,Is.True);City.PrimaryButton.onClick.Invoke();
            Assert.That(campaign.ArmyRegistry.TryGetArmy("own",out _),Is.False);Assert.That(campaign.Military.Garrison("home-fort").Formations.Single().Id,Is.EqualTo("own-troops"));
            City.SelectMilitaryMode(CityMilitaryMode.Muster);Assert.That(City.PrimaryButton.interactable,Is.True);City.PrimaryButton.onClick.Invoke();
            Assert.That(campaign.SelectedArmyId,Is.EqualTo("muster/1"));Assert.That(campaign.Army.Position,Is.EqualTo(home));Assert.That(campaign.Army.RemainingMovement,Is.Zero);
            var layer=campaign.GetComponent<CampaignArmyMarkerLayer>();Assert.That(layer,Is.Not.Null);yield return null;
            Assert.That(City.BodyText,Does.Contain("muster/1"));
        }
        [UnityTest]public IEnumerator SaveV6RestoresPendingCompletedGarrisonNewArmyAndRejectsDuplicateDeploymentAtomically()
        {
            FundAndOpen();City.SelectMilitaryMode(CityMilitaryMode.Defend);City.PrimaryButton.onClick.Invoke();
            City.SelectMilitaryMode(CityMilitaryMode.Muster);City.PrimaryButton.onClick.Invoke();
            City.SelectMilitaryMode(CityMilitaryMode.Recruit);City.PrimaryButton.onClick.Invoke();City.CompleteTurn();City.CompleteTurn();
            City.ChangeMilitaryItem(1);City.PrimaryButton.onClick.Invoke();City.Close();campaign.SaveCampaign();
            string valid=PlayerPrefs.GetString(SaveKey);var saved=campaign.State;
            var data=JsonUtility.FromJson<CampaignSaveData>(valid);Assert.That(data.version,Is.EqualTo(9));Assert.That(data.military.recruits.Length,Is.EqualTo(2));
            campaign.CompleteTurn();campaign.LoadCampaign();yield return null;
            Assert.That(campaign.State.Resources,Is.EqualTo(saved.Resources));Assert.That(campaign.SelectedArmyId,Is.EqualTo("muster/1"));
            Assert.That(campaign.Military.Garrison("home-fort").Formations.Single().Id,Is.EqualTo("citytroop/1"));Assert.That(campaign.Military.Pending("home-fort"),Is.Not.Null);
            var state=campaign.State;var registry=campaign.ArmyRegistry;var military=campaign.Military;
            data.armyRegistry.armies.Single(a=>a.id=="muster/1").formations=new[]{new CampaignFormationData{id="citytroop/1",health=100,morale=70}};
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));campaign.LoadCampaign();
            Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(registry));Assert.That(campaign.Military,Is.SameAs(military));
        }
        private void SeedEnemyGuard()
        {
            for(int i=0;i<12;i++)campaign.CompleteTurn();campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            data.military.recruits=new[]{new RecruitmentRecord {serial=1,settlementId=TargetId,factionId="rival",profile=TroopProfile.Spearmen,startTurn=1,completionTurn=3,status=RecruitmentStatus.Completed}};
            data.military.garrisons.Single(g=>g.settlementId==TargetId).formations=new[]{new CampaignFormationData{id="citytroop/1",health=20,morale=70}};
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));campaign.LoadCampaign();
            Assert.That(campaign.Military.HasHostileGarrison(TargetId,"castilla"),Is.True);
        }
        [UnityTest]public IEnumerator EmptyFieldFortWithGarrisonRequiresBattleAndCancelsForeignRecruitmentOnConquest()
        {
            SeedEnemyGuard();selection.LocateCell(target);
            Assert.That(panel.MoveArmyButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Does.Contain("GUARNICIÓN"));
            Assert.That(campaign.AttackSelectedArmy(),Is.True);Assert.That(campaign.TryRecruit("home-fort",TroopProfile.Spearmen),Is.False);
            var encounter=Battle.ActiveCampaignEncounter;
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)Battle.AdvanceCampaignAutomaticStep();
            Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));Battle.ReturnToCampaign();yield return null;
            Assert.That(campaign.Military.Garrison(TargetId).IsDissolved,Is.True);campaign.SettlementState.TryGet(TargetId,out var town);Assert.That(town.OwnerFactionId,Is.EqualTo("castilla"));
            Assert.That(campaign.Army.Position,Is.EqualTo(target));Assert.That(campaign.ArmyRegistry.TryGetArmy("guard/"+TargetId,out _),Is.False);
            campaign.SaveCampaign();campaign.LoadCampaign();Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(encounter.Id),Is.True);
        }
        [UnityTest]public IEnumerator ForeignInspectionDisablesRecruitmentAndLegacyV5StartsWithEmptyGarrisons()
        {
            SeedEnemyGuard();Assert.That(towns.OpenCityAt(target),Is.True);City.SelectPanel(CityPanelKind.Armies);
            Assert.That(City.PrimaryButton.interactable,Is.False);Assert.That(City.BodyText,Does.Contain("Inspección"));City.Close();
            campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));data.version=5;data.military=null;
            data.context=CampaignSaveData.SettlementScenarioSignature(CampaignSaveData.ArmyScenarioSignature(CampaignSaveData.ScenarioSignature(map,selection.Routes,null),initial,"castilla"),campaign.SettlementState);
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));campaign.LoadCampaign();yield return null;
            Assert.That(campaign.Military.Garrison(TargetId).IsDissolved,Is.True);Assert.That(campaign.State.Turn,Is.EqualTo(data.turn));
        }
    }
}
