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
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class BattleGraphicalSceneTests
    {
        private CampaignTurnController campaign;
        private CampaignBoardSelection selection;
        private BattleLabController battle;
        private BattleGraphicalView View=>battle.GraphicalView;
        private HexCoordinates enemy;
        private string scene,save;
        private bool hadSave;
        private int cameraMask;
        [UnitySetUp]public IEnumerator Setup()
        {
            scene=EditorSceneManager.GetActiveScene().path;hadSave=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);save=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();selection=campaign.GetComponent<CampaignBoardSelection>();
            var map=ProceduralMapGenerator.Generate(24,16,12345);var graph=selection.Routes;
            var start=map.Cells.First(c=>graph.CanStandAt(c.Coordinates)&&c.Coordinates.Neighbors().Any(n=>graph.TryGetMovementCost(c.Coordinates,n,out _))).Coordinates;
            enemy=start.Neighbors().First(n=>graph.TryGetMovementCost(start,n,out _));
            CampaignArmyPlacement Army(string id,string faction,HexCoordinates at)=>new(new CampaignArmyState(id,faction,new[]{
                Form(id+"-a",TroopProfile.Spearmen),Form(id+"-b",TroopProfile.Crossbowmen),Form(id+"-c",TroopProfile.LightCavalry)}),at,6);
            var registry=new CampaignArmyRegistry(graph,new[]{Army("own","castilla",start),Army("enemy","rival",enemy)});
            campaign.Initialize(campaign.GetComponent<CampaignSelectionPanel>(),map,null,registry,"castilla");
            cameraMask=Camera.main.cullingMask;selection.LocateCell(enemy);Assert.That(campaign.AttackSelectedArmy(),Is.True);
            battle=campaign.GetComponent<BattleLabController>();yield return null;
        }
        private static CampaignFormationState Form(string id,TroopProfile profile)
        {var p=TroopCatalog.Get(profile);return new CampaignFormationState(id,p.Attributes,(long)p.DefaultQuantity*p.Attributes.HealthPerMember,70,p.Kind);}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,save);else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);PlayerPrefs.Save();
            if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);
        }
        private IEnumerator OwnTurn()
        {
            for(int i=0;i<50&&battle.ActiveCampaignEncounter.Roster.ActiveSide!=battle.PlayerBattleSide;i++)battle.AdvanceCampaignAutomaticStep();
            yield return null;Assert.That(battle.ActiveCampaignEncounter.Roster.ActiveSide,Is.EqualTo(battle.PlayerBattleSide));
        }
        [UnityTest]public IEnumerator RealEncounterBuildsOpaqueBattlefieldAndAllThreeTroopTypes()
        {
            Assert.That(View.IsOpen,Is.True);Assert.That(View.Stage.CellCount,Is.EqualTo(117));Assert.That(View.Stage.FormationCount,Is.EqualTo(6));
            Assert.That(battle.ActiveCampaignEncounter.Roster.FormationIds.Select(id=>battle.ActiveCampaignEncounter.Roster.GetFormation(id).Slot.Kind).Distinct().Count(),Is.EqualTo(3));
            Assert.That(View.Viewport.texture,Is.SameAs(View.Stage.Texture));Assert.That(View.Stage.Texture.IsCreated(),Is.True);Assert.That(Camera.main.cullingMask,Is.Zero);
            Assert.That(selection.IsInteractionSuspended,Is.True);
            foreach(int id in battle.ActiveCampaignEncounter.Roster.FormationIds)
            {Assert.That(View.Stage.IsFormationVisible(id),Is.True);Assert.That(View.Stage.VisibleFigurines(id),Is.EqualTo(battle.ActiveCampaignEncounter.Roster.GetFormation(id).Slot.Kind==FormationKind.Cavalry?4:9));}
            Assert.That(View.GetComponentsInChildren<UnityEngine.UI.InputField>().Length,Is.Zero,"Gameplay must not expose coordinate entry fields.");
            yield return null;
        }
        [UnityTest]public IEnumerator EveryHexPicksBackToItsActualCellAndCameraFitsAtDefaultZoom()
        {
            foreach(float aspect in new[]{1.2f,1.8f,2.4f})
            {
                View.Stage.SetZoom(1);View.Stage.FitCamera(aspect);
                foreach(var cell in View.Stage.Cells)
                {
                    var world=View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(cell));var point=View.Stage.Camera.WorldToViewportPoint(world);
                    Assert.That(point.x,Is.InRange(.015f,.985f));Assert.That(point.y,Is.InRange(.015f,.985f));
                    Assert.That(View.Stage.TryPick(point,out var picked),Is.True);Assert.That(picked,Is.EqualTo(cell));
                }
            }
            View.Stage.SetZoom(-10);Assert.That(View.Stage.Zoom,Is.EqualTo(.4f));View.Stage.SetZoom(20);Assert.That(View.Stage.Zoom,Is.EqualTo(1.12f));yield return null;
        }
        [UnityTest]public IEnumerator PreviewIsPureAndPointerSelectionConfirmsTheExactCoreRoute()
        {
            yield return OwnTurn();var encounter=battle.ActiveCampaignEncounter;int actor=encounter.Roster.ActiveFormationId.Value;
            var target=View.Stage.Cells.First(c=>encounter.Commands.TryPreviewMove(actor,c,out _,out _));
            encounter.Commands.TryGetPosition(actor,out var before);uint rng=encounter.Commands.RandomState;var stock=campaign.State;
            var screen=RectTransformUtility.WorldToScreenPoint(null,View.Viewport.rectTransform.TransformPoint(new Vector3(
                (View.Stage.Camera.WorldToViewportPoint(View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(target))).x-.5f)*View.Viewport.rectTransform.rect.width,
                (View.Stage.Camera.WorldToViewportPoint(View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(target))).y-.5f)*View.Viewport.rectTransform.rect.height,0)));
            // The viewport has a lower-left pivot; select through the supported UI pointer path.
            var viewport=View.Stage.Camera.WorldToViewportPoint(View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(target)));
            var rect=View.Viewport.rectTransform;screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+viewport.x*rect.rect.width,rect.rect.yMin+viewport.y*rect.rect.height,0)));
            View.SelectCell(target);
            Assert.That(View.ConfirmButton.interactable,Is.True);Assert.That(View.PreviewRoute.Last(),Is.EqualTo(target));
            encounter.Commands.TryGetPosition(actor,out var still);Assert.That(still,Is.EqualTo(before));Assert.That(encounter.Commands.RandomState,Is.EqualTo(rng));Assert.That(campaign.State,Is.SameAs(stock));
            var pointer=View.Viewport.GetComponent<BattlefieldPointer>();
            var click=new PointerEventData(EventSystem.current){position=screen,button=PointerEventData.InputButton.Left};
            pointer.OnPointerDown(click);pointer.OnBeginDrag(click);pointer.OnEndDrag(click);pointer.OnPointerClick(click);
            encounter.Commands.TryGetPosition(actor,out var afterDrag);Assert.That(afterDrag,Is.EqualTo(before),"Dragging must not issue a tactical order.");
            pointer.OnPointerDown(click);pointer.OnPointerClick(click);encounter.Commands.TryGetPosition(actor,out var moved);Assert.That(moved,Is.EqualTo(target));
            yield return null;Assert.That(View.Stage.DestinationOf(actor),Is.EqualTo(View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(target))));Assert.That(View.ConfirmButton.interactable,Is.False);
        }
        [UnityTest]public IEnumerator PanningAndZoomPreservePickingAndResetRestoresFraming()
        {
            var original=View.Stage.Camera.transform.localPosition;
            View.Stage.SetZoom(.5f);View.Stage.PanView(new Vector2(.2f,.1f));
            Assert.That(View.Stage.Camera.transform.localPosition,Is.Not.EqualTo(original));
            var cell=View.Stage.Cells.First();var world=View.Stage.transform.TransformPoint(BattleGraphicalStage.CellPosition(cell));
            var point=View.Stage.Camera.WorldToViewportPoint(world);
            Assert.That(View.Stage.TryPick(point,out var picked),Is.True);Assert.That(picked,Is.EqualTo(cell));
            View.Stage.FitCamera(1.8f);Assert.That(View.Stage.CameraPan,Is.Not.EqualTo(Vector2.zero));
            View.Stage.PanView(Vector2.one*10000);Assert.That(View.Stage.CameraPan.magnitude,Is.LessThan(100));
            View.Stage.ResetView();Assert.That(View.Stage.CameraPan,Is.EqualTo(Vector2.zero));Assert.That(View.Stage.Zoom,Is.EqualTo(1));
            Assert.That(View.Stage.Camera.transform.localPosition,Is.EqualTo(original));yield return null;
        }
        [UnityTest]public IEnumerator FriendlyOccupiedAndFarEnemyCannotBeAttackedThroughTheScreen()
        {
            yield return OwnTurn();var encounter=battle.ActiveCampaignEncounter;uint rng=encounter.Commands.RandomState;int actor=encounter.Roster.ActiveFormationId.Value;
            Assert.That(battle.TryCampaignOrder(null,out _),Is.False);
            encounter.Commands.TryGetPosition(actor,out var own);View.SelectCell(own);Assert.That(View.ConfirmButton.interactable,Is.False);View.ConfirmSelection();
            int foe=encounter.Roster.FormationIds.First(id=>encounter.Roster.GetFormation(id).Slot.Side!=battle.PlayerBattleSide);encounter.Commands.TryGetPosition(foe,out var far);
            View.SelectCell(far);Assert.That(View.ConfirmButton.interactable,Is.False);View.ConfirmSelection();
            Assert.That(encounter.Commands.RandomState,Is.EqualTo(rng));Assert.That(encounter.Roster.ActiveFormationId,Is.EqualTo(actor));yield return null;
        }
        [UnityTest]public IEnumerator ReachableEnemyAttackUpdatesRealHealthAndFinishesWithAResultPanel()
        {
            var encounter=battle.ActiveCampaignEncounter;int actor=-1,target=-1;
            for(int step=0;step<150&&encounter.Roster.Outcome==BattleOutcome.InProgress;step++)
            {
                int active=encounter.Roster.ActiveFormationId.Value;
                if(encounter.Roster.ActiveSide==battle.PlayerBattleSide)
                {
                    encounter.Commands.TryGetPosition(active,out var at);int range=encounter.Roster.GetFormation(active).Attributes.Range;
                    target=encounter.Roster.FormationIds.Where(id=>encounter.Roster.GetFormation(id).Slot.Side!=battle.PlayerBattleSide&&encounter.Commands.TryGetPosition(id,out var p)&&p.DistanceTo(at)<=range).DefaultIfEmpty(-1).First();
                    if(target>=0){actor=active;break;}
                }
                battle.AdvanceCampaignAutomaticStep();
            }
            Assert.That(actor,Is.GreaterThanOrEqualTo(0));yield return null;
            encounter.Commands.TryGetPosition(target,out var cell);long old=encounter.Roster.GetFormation(target).TotalHealth;View.SelectCell(cell);Assert.That(View.ConfirmButton.interactable,Is.True);View.ConfirmButton.onClick.Invoke();
            Assert.That(encounter.Roster.GetFormation(target).TotalHealth,Is.LessThanOrEqualTo(old));
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)battle.AdvanceCampaignAutomaticStep();yield return null;
            Assert.That(View.ResultVisible,Is.True);Assert.That(View.ConfirmButton.interactable||View.DefendButton.interactable||View.WaitButton.interactable,Is.False);
        }
        [UnityTest]public IEnumerator RetreatConfirmationPausesEnemyAndDoesNotWithdrawUntilConfirmed()
        {
            var encounter=battle.ActiveCampaignEncounter;var active=encounter.Roster.ActiveFormationId;View.WithdrawButton.onClick.Invoke();Assert.That(View.RetreatConfirmationVisible,Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(View.CancelRetreatButton.gameObject));
            View.ReturnButton.onClick.Invoke();Assert.That(View.IsOpen,Is.True);View.CancelRetreatButton.onClick.Invoke();Assert.That(View.RetreatConfirmationVisible,Is.False);Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.InProgress));View.WithdrawButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1);Assert.That(encounter.Roster.ActiveFormationId,Is.EqualTo(active));Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.InProgress));
            View.ConfirmRetreat();Assert.That(View.RetreatConfirmationVisible,Is.False);Assert.That(View.ResultVisible,Is.True);
            string id=encounter.Id;View.ReturnButton.onClick.Invoke();Assert.That(battle.IsOpen,Is.False);Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(id),Is.True);Assert.That(Camera.main.cullingMask,Is.EqualTo(cameraMask));
            Assert.That(battle.ClosedThisFrame,Is.True);yield return null;
        }
        [UnityTest]public IEnumerator LeaveResumeAndNewGameRestoreCameraAndDisposePreviewResources()
        {
            var encounter=battle.ActiveCampaignEncounter;var stage=View.Stage;var texture=stage.Texture;
            View.ReturnButton.onClick.Invoke();Assert.That(View.IsOpen,Is.False);Assert.That(Camera.main.cullingMask,Is.EqualTo(cameraMask));Assert.That(campaign.HasPendingBattle,Is.True);
            Assert.That(campaign.ResumeBattle(),Is.True);Assert.That(battle.ActiveCampaignEncounter,Is.SameAs(encounter));Assert.That(View.Stage,Is.Not.SameAs(stage));
            campaign.NewGame();Assert.That(View.IsOpen,Is.False);Assert.That(Camera.main.cullingMask,Is.EqualTo(cameraMask));yield return null;
            Assert.That(texture==null,Is.True);Assert.That(Object.FindObjectsByType<BattleGraphicalStage>(FindObjectsSortMode.None).Length,Is.Zero);
        }
    }
}
