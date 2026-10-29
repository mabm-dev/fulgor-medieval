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
    public sealed class CampaignConclusionSceneTests
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
        private void Conquest(bool trapped=false,bool enemyAttacks=true)
        {
            var list=new System.Collections.Generic.List<CampaignArmyPlacement>{Placement("own","castilla",home,enemyAttacks?100:600),Placement("enemy","rival",target,enemyAttacks?600:100)};
            var origin=enemyAttacks?home:target;var opponent=enemyAttacks?target:home;string faction=enemyAttacks?"rival":"castilla";
            if(trapped)foreach(var at in origin.Neighbors().Where(at=>at!=opponent&&selection.Routes.CanStandAt(at)))list.Add(Placement("support-"+list.Count,faction,at,100));
            initial=new CampaignArmyRegistry(selection.Routes,list);campaign.Initialize(panel,map,null,initial,"castilla",enemyAttacks,CampaignObjectiveKind.Conquest);selection.ClearSelection();
        }
        [UnityTest]public IEnumerator DefenderRetreatVacatesFortButSurvivingArmyPreventsPrematureDefeat()
        {
            Conquest();campaign.CompleteTurn();Assert.That(campaign.RetreatPreview(),Does.Contain("Retirada prevista"));var stock=campaign.State.Resources;
            Battle.WithdrawPlayerArmy();Assert.That(Battle.ReturnToCampaign(),Is.True);yield return null;
            var retreat=campaign.LastRetreats.Single();Assert.That(retreat.Surrendered,Is.False);Assert.That(retreat.From,Is.EqualTo(home));
            campaign.ArmyRegistry.TryGetArmy("own",out var escaped);Assert.That(escaped.Movement.Position,Is.EqualTo(retreat.Destination));Assert.That(escaped.Movement.Position,Is.Not.EqualTo(home));
            Assert.That(escaped.Army.Formations.Single().Health,Is.EqualTo(100));Assert.That(escaped.Movement.RemainingMovement,Is.Zero);
            campaign.SettlementState.TryGet("home-fort",out var city);Assert.That(city.OwnerFactionId,Is.EqualTo("rival"));Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.InProgress));
            Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(campaign.IsRivalPhaseActive,Is.False);
            campaign.SaveCampaign();campaign.LoadCampaign();Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.InProgress));Assert.That(campaign.Army.Position,Is.EqualTo(escaped.Movement.Position));
        }
        [UnityTest]public IEnumerator TrappedRetreatSurrendersAndDefeatFreezesCommandsButAllowsSaveLoadAndNewGame()
        {
            Conquest(true);campaign.CompleteTurn();Assert.That(campaign.RetreatPreview(),Does.Contain("se rendirán"));Battle.WithdrawPlayerArmy();Battle.ReturnToCampaign();yield return null;
            Assert.That(campaign.LastRetreats.Single().Surrendered,Is.True);Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Defeat));Assert.That(campaign.IsRivalPhaseActive,Is.False);
            Assert.That(panel.EndTurnButton.interactable,Is.False);Assert.That(panel.MoveArmyButton.interactable,Is.False);
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.text.Contains("DERROTA")),Is.True);
            var state=campaign.State;campaign.CompleteTurn();Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.TryRecruit("home-fort",TroopProfile.Spearmen),Is.False);
            Assert.That(panel.SaveButton.interactable&&panel.LoadButton.interactable&&panel.NewGameButton.interactable,Is.True);
            campaign.SaveCampaign();campaign.LoadCampaign();Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Defeat));Assert.That(campaign.State.Turn,Is.EqualTo(2));
            campaign.NewGame();yield return null;yield return null;Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.InProgress));Assert.That(campaign.State.Turn,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator GarrisonEscapesAsSelectableArmyAndKeepsItsOriginAcrossV9Load()
        {
            Conquest();Assert.That(campaign.TryLeaveGarrison("home-fort","own-troops"),Is.True);campaign.CompleteTurn();
            var encounter=Battle.ActiveCampaignEncounter;Assert.That(Battle.PlayerBattleSide,Is.EqualTo(BattleSide.Defender));Battle.WithdrawPlayerArmy();Battle.ReturnToCampaign();yield return null;
            Assert.That(campaign.SelectedArmyId,Is.EqualTo("muster/1"));Assert.That(campaign.Military.Garrison("home-fort").IsDissolved,Is.True);Assert.That(campaign.Army.RemainingMovement,Is.Zero);
            Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.InProgress));Assert.That(campaign.ArmyRegistry.TryGetArmy("guard/home-fort",out _),Is.False);
            var at=campaign.Army.Position;campaign.SaveCampaign();var payload=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));Assert.That(payload.version,Is.EqualTo(9));
            Assert.That(payload.objectiveKind,Is.EqualTo(CampaignObjectiveKind.Conquest));campaign.LoadCampaign();Assert.That(campaign.SelectedArmyId,Is.EqualTo("muster/1"));Assert.That(campaign.Army.Position,Is.EqualTo(at));
            Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(encounter.Id),Is.True);Assert.That(campaign.ApplyEncounterResult(encounter.Id),Is.True);Assert.That(campaign.Military.Capture().musters.Length,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator LastOppositionEliminatedProducesVictoryAndPersistsWithoutLegacyMissionFlag()
        {
            Conquest(true,false);selection.LocateCell(target);Assert.That(campaign.AttackSelectedArmy(),Is.True);var encounter=Battle.ActiveCampaignEncounter;
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)Battle.AdvanceCampaignAutomaticStep();
            Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));Battle.ReturnToCampaign();yield return null;
            Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Victory));Assert.That(campaign.Mission,Is.Null);Assert.That(panel.EndTurnButton.interactable,Is.False);
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.text.Contains("VICTORIA")),Is.True);
            var state=campaign.State;campaign.CompleteTurn();Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.TryConstruct(TargetId,"well"),Is.False);
            campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));Assert.That(data.missionWon,Is.False);campaign.LoadCampaign();Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Victory));
        }
        [UnityTest]public IEnumerator InvalidRulesAndTerminalMarchDoNotReplaceTheCurrentCampaign()
        {
            Conquest(true,false);selection.LocateCell(target);campaign.AttackSelectedArmy();var encounter=Battle.ActiveCampaignEncounter;
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)Battle.AdvanceCampaignAutomaticStep();Battle.ReturnToCampaign();campaign.SaveCampaign();
            string valid=PlayerPrefs.GetString(SaveKey);var state=campaign.State;var armies=campaign.ArmyRegistry;var rivals=campaign.Rivals;
            foreach(var damage in new[]{"objective","unknown","legacy","march"})
            {
                var data=JsonUtility.FromJson<CampaignSaveData>(valid);if(damage=="objective")data.objectiveKind=CampaignObjectiveKind.ReachDestination;
                if(damage=="unknown")data.objectiveKind=(CampaignObjectiveKind)99;if(damage=="legacy")data.version=8;
                if(damage=="march")
                {
                    var to=map.Cells.First(c=>c.Coordinates!=campaign.Army.Position&&selection.Routes.CanStandAt(c.Coordinates)&&Fulgor.Core.Map.MapPathfinder.TryFindPath(selection.Routes,campaign.Army.Position,c.Coordinates,out _)).Coordinates;
                    data.marchOrders=new[]{new CampaignMarchData{armyId=campaign.SelectedArmyId,q=to.Q,r=to.R}};data.marchOrderCount=1;
                }
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));campaign.LoadCampaign();
                Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(armies));Assert.That(campaign.Rivals,Is.SameAs(rivals));Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Victory));
            }
            yield return null;
        }
        [UnityTest]public IEnumerator LoadingPreVictoryMissionRestoresItsMarchInsteadOfClearingItWithTheOldResult()
        {
            var forms=new CampaignArmyState("own","castilla",new[]{new CampaignFormationState("own-troops",TroopCatalog.Get(TroopProfile.Spearmen).Attributes,100,70)});
            initial=new CampaignArmyRegistry(selection.Routes,new[]{new CampaignArmyPlacement(forms,home,2)});
            var goal=map.Cells.First(c=>MapPathfinder.TryFindPath(selection.Routes,home,c.Coordinates,out var path,maximumStepCost:2)&&path.TotalCost>6).Coordinates;
            var mission=ReachDestinationMission.Create(map,selection.Routes,goal,home);campaign.Initialize(panel,map,mission,initial,"castilla");
            selection.LocateCell(goal);campaign.MoveToSelectedCell();Assert.That(campaign.MarchFor("own"),Is.EqualTo(goal));campaign.SaveCampaign();
            for(int i=0;i<100&&!campaign.IsCampaignFinished;i++)campaign.CompleteTurn();Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.Victory));
            campaign.LoadCampaign();yield return null;Assert.That(campaign.Conclusion,Is.EqualTo(CampaignConclusion.InProgress));Assert.That(campaign.MarchFor("own"),Is.EqualTo(goal));Assert.That(panel.EndTurnButton.interactable,Is.True);
        }
    }
}
