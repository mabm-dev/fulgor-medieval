using System.Collections;
using System.Linq;
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
    public sealed class CampaignBattleSceneTests
    {
        private const string SaveKey="Fulgor.Campaign.Prototype.Save.v1";
        private bool hadSave;private string oldSave,oldScene;
        private CampaignTurnController campaign;private CampaignBoardSelection selection;
        private CampaignSelectionPanel panel;private CampaignArmyRegistry initial;
        private HexCoordinates start,enemy,ally;
        private BattleLabController View=>campaign.GetComponent<BattleLabController>();

        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(SaveKey);oldSave=PlayerPrefs.GetString(SaveKey,"");
            oldScene=EditorSceneManager.GetActiveScene().path;EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");
            yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();selection=campaign.GetComponent<CampaignBoardSelection>();panel=campaign.GetComponent<CampaignSelectionPanel>();
            var map=ProceduralMapGenerator.Generate(24,16,12345);var graph=selection.Routes;
            start=map.Cells.First(c=>graph.CanStandAt(c.Coordinates)&&c.Coordinates.Neighbors().Count(n=>graph.TryGetMovementCost(c.Coordinates,n,out _))>=2).Coordinates;
            enemy=start.Neighbors().First(n=>graph.TryGetMovementCost(start,n,out _));
            ally=start.Neighbors().First(n=>n!=enemy&&graph.TryGetMovementCost(start,n,out _));
            initial=new CampaignArmyRegistry(graph,new[]{Placement("a","castilla",start,173),Placement("b","rival",enemy,110),Placement("c","castilla",ally,100)});
            campaign.Initialize(panel,map,null,initial,"castilla");selection.ClearSelection();yield return null;
        }
        private static CampaignArmyPlacement Placement(string id,string faction,HexCoordinates at,long health)
        {
            var p=TroopCatalog.Get(TroopProfile.Spearmen);
            return new CampaignArmyPlacement(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troop",p.Attributes,health,67)}),at,6);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(SaveKey,oldSave);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
            if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        private void Attack()
        {
            selection.LocateCell(enemy);Assert.That(panel.MoveArmyButton.interactable,Is.True);
            Assert.That(panel.MoveArmyButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Does.Contain("ATACAR"));
            panel.MoveArmyButton.onClick.Invoke();Assert.That(View.IsOpen,Is.True);
        }

        [UnityTest]public IEnumerator EnemySelectionStartsRealBattleAndBlocksStrategicChanges()
        {
            campaign.CompleteTurn();var state=campaign.State;campaign.SaveCampaign();var saved=PlayerPrefs.GetString(SaveKey);
            Attack();Assert.That(View.ActiveCampaignEncounter.Roster.GetFormation(0).TotalHealth,Is.EqualTo(173));
            Assert.That(View.ActiveCampaignEncounter.Roster.GetFormation(0).Morale,Is.EqualTo(67));
            Assert.That(View.ActiveCampaignEncounter.Roster.FormationIds.Count,Is.EqualTo(2));
            Assert.That(campaign.Army.RemainingMovement,Is.Zero);Assert.That(selection.IsInteractionSuspended,Is.True);
            campaign.CompleteTurn();Assert.That(campaign.State,Is.SameAs(state));
            campaign.SaveCampaign();campaign.LoadCampaign();Assert.That(PlayerPrefs.GetString(SaveKey),Is.EqualTo(saved));
            Assert.That(panel.SaveButton.interactable||panel.LoadButton.interactable||panel.EndTurnButton.interactable,Is.False);
            var settlements=campaign.GetComponent<CampaignSettlementController>();
            Assert.That(campaign.TryConstruct(settlements.Registry.Settlements.Single().Id,"market"),Is.False);
            var registry=campaign.ArmyRegistry;campaign.MoveToSelectedCell();Assert.That(campaign.ArmyRegistry,Is.SameAs(registry));
            yield return null;
        }

        [UnityTest]public IEnumerator ReturningMidBattleAndResumingPreservesTacticalState()
        {
            Attack();var encounter=View.ActiveCampaignEncounter;
            Assert.That(View.TryCampaignOrder(new BattleRecordedOrder{kind=BattleRecordedOrderKind.Defend,actorId=encounter.Roster.ActiveFormationId.Value},out _),Is.True);
            var active=encounter.Roster.ActiveFormationId;var random=encounter.Commands.RandomState;
            Assert.That(View.ReturnToCampaign(),Is.True);Assert.That(View.IsOpen,Is.False);
            Assert.That(campaign.HasPendingBattle,Is.True);Assert.That(panel.MoveArmyButton.interactable,Is.True);
            Assert.That(panel.MoveArmyButton.GetComponentInChildren<UnityEngine.UI.Text>().text,Does.Contain("VOLVER"));
            campaign.MoveToSelectedCell();Assert.That(View.IsOpen,Is.True);
            Assert.That(View.ActiveCampaignEncounter,Is.SameAs(encounter));Assert.That(encounter.Roster.ActiveFormationId,Is.EqualTo(active));Assert.That(encounter.Commands.RandomState,Is.EqualTo(random));
            Assert.That(View.TryCampaignOrder(new BattleRecordedOrder{kind=BattleRecordedOrderKind.Defend,actorId=active.Value},out _),Is.False);
            Assert.That(View.AdvanceCampaignAutomaticStep(),Is.True);yield return null;
        }

        [UnityTest]public IEnumerator RetreatAppliesOnceRestoresHudAndPersistsHistoryAndWounds()
        {
            Attack();string id=campaign.ActiveEncounterId;
            Assert.That(View.WithdrawPlayerArmy(),Is.True);Assert.That(View.EncounterReport,Does.Contain("retiradas"));
            Assert.That(View.ReturnToCampaign(),Is.True);Assert.That(View.IsOpen,Is.False);Assert.That(campaign.HasPendingBattle,Is.False);
            Assert.That(selection.IsInteractionSuspended,Is.False);Assert.That(panel.SaveButton.interactable&&panel.EndTurnButton.interactable,Is.True);
            campaign.ArmyRegistry.TryGetArmy("a",out var own);Assert.That(own.Army.Formations[0].Health,Is.EqualTo(173));Assert.That(own.Movement.RemainingMovement,Is.Zero);
            var state=campaign.ArmyRegistry;Assert.That(campaign.ApplyEncounterResult(id),Is.True);Assert.That(campaign.ArmyRegistry,Is.SameAs(state));
            campaign.SaveCampaign();campaign.CompleteTurn();campaign.LoadCampaign();yield return null;
            Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(id),Is.True);campaign.ArmyRegistry.TryGetArmy("a",out own);
            Assert.That(own.Army.Formations[0].Health,Is.EqualTo(173));Assert.That(own.Army.HasAppliedEncounter(id),Is.True);Assert.That(own.Movement.RemainingMovement,Is.Zero);
        }

        [UnityTest]public IEnumerator AutomaticBattleReturnsExactSurvivorsAndUpdatesMarkers()
        {
            Attack();var encounter=View.ActiveCampaignEncounter;
            for(int step=0;step<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;step++)Assert.That(View.AdvanceCampaignAutomaticStep(),Is.True);
            Assert.That(encounter.Roster.Outcome,Is.Not.EqualTo(BattleOutcome.InProgress));
            var report=encounter.CreateReport();Assert.That(View.ReturnToCampaign(),Is.True);yield return null;
            Assert.That(campaign.LastBattleReport.Attacker.Casualties,Is.EqualTo(report.Attacker.Casualties));
            Assert.That(campaign.LastBattleReport.Defender.Casualties,Is.EqualTo(report.Defender.Casualties));
            foreach(var pair in new[]{("a",report.Formations[0]),("b",report.Formations[1])})
            {
                bool exists=campaign.ArmyRegistry.TryGetArmy(pair.Item1,out var survivor);
                Assert.That(exists,Is.EqualTo(!pair.Item2.IsDestroyed));
                if(exists){Assert.That(survivor.Army.Formations[0].Health,Is.EqualTo(pair.Item2.TotalHealth));Assert.That(survivor.Army.Formations[0].Morale,Is.EqualTo(pair.Item2.Morale));}
            }
            Assert.That(campaign.GetComponent<CampaignArmyMarkerLayer>().GetComponentsInChildren<CampaignArmyMarker>().Count(m=>m.IsVisible),Is.EqualTo(campaign.ArmyRegistry.Armies.Count));
            campaign.SaveCampaign();campaign.LoadCampaign();Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(encounter.Id),Is.True);
        }

        [UnityTest]public IEnumerator NewGameClosesPendingBattleAndRestoresNormalCampaign()
        {
            Attack();campaign.NewGame();yield return null;yield return null;
            Assert.That(View.IsOpen,Is.False);Assert.That(View.ActiveCampaignEncounter,Is.Null);Assert.That(campaign.ActiveEncounterId,Is.Null);Assert.That(campaign.State.Turn,Is.EqualTo(1));
            Assert.That(selection.IsInteractionSuspended,Is.False);
        }

        [UnityTest]public IEnumerator LabStillUsesItsIndependentFixtureAndDoesNotAlterCampaign()
        {
            var registry=campaign.ArmyRegistry;var state=campaign.State;
            campaign.OpenBattleLab();Assert.That(View.IsOpen,Is.True);Assert.That(View.ActiveCampaignEncounter,Is.Null);
            Assert.That(View.ReturnToCampaign(),Is.True);Assert.That(campaign.ArmyRegistry,Is.SameAs(registry));Assert.That(campaign.State,Is.SameAs(state));
            Attack();Assert.That(View.WithdrawPlayerArmy(),Is.True);Assert.That(View.ReturnToCampaign(),Is.True);
            campaign.OpenBattleLab();
            var automatic=(BattleAutoController)typeof(BattleLabController).GetField("automatic",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(View);
            Assert.That(automatic.ChooseOrder().HasValue,Is.True,"The independent laboratory must not keep the finished campaign AI.");
            View.ReturnToCampaign();
            yield return null;
        }
    }
}
