using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Fulgor.Core.Campaign;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignLaunchSceneTests
    {
        private bool hadSave;private string oldSave,oldScene;
        private CampaignLaunchMenu Menu=>Object.FindAnyObjectByType<CampaignLaunchMenu>();
        private CampaignTurnController Campaign=>Object.FindAnyObjectByType<CampaignTurnController>();
        private CampaignPlaySession Session=>Object.FindAnyObjectByType<CampaignPlaySession>();
        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);oldSave=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            oldScene=EditorSceneManager.GetActiveScene().path;PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);PlayerPrefs.Save();
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorStart.unity");yield return new EnterPlayMode();yield return null;yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,oldSave);else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);PlayerPrefs.Save();
            if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        private IEnumerator Ready(bool menu=false)
        {
            for(int i=0;i<300;i++)
            {
                if(!CampaignNavigation.IsTransitioning&&(menu?Menu?.ContinueButton!=null:Campaign?.State!=null&&Session?.MenuButton!=null))yield break;
                yield return null;
            }
            Assert.Fail("Scene transition did not finish.");
        }
        [UnityTest]public IEnumerator StartMenuOffersBothCampaignsAndConquestStartsWithoutSaving()
        {
            Assert.That(Menu.CampaignButtons.Count,Is.EqualTo(2));Assert.That(Menu.ContinueButton.interactable,Is.False);Assert.That(Menu.StartCampaign("castilla-conquest"),Is.True);
            yield return Ready();Assert.That(Menu,Is.Null);Assert.That(Campaign.ObjectiveKind,Is.EqualTo(CampaignObjectiveKind.Conquest));Assert.That(Campaign.Rivals.Enabled,Is.True);
            Assert.That(Campaign.State.Turn,Is.EqualTo(1));Assert.That(PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey),Is.False);
        }
        [UnityTest]public IEnumerator PauseBlocksMapAndResumeKeepsTheSameCampaign()
        {
            Menu.StartCampaign("castilla-mission");yield return Ready();var campaign=Campaign;var state=campaign.State;
            Session.MenuButton.onClick.Invoke();Assert.That(Session.IsOpen,Is.True);Assert.That(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended,Is.True);
            Session.ResumeButton.onClick.Invoke();Assert.That(Session.IsOpen,Is.False);Assert.That(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended,Is.False);Assert.That(campaign.State,Is.SameAs(state));
        }
        [UnityTest]public IEnumerator SaveReturnAndContinueRestoreValidatedSnapshotEvenIfSlotChangesDuringTransition()
        {
            Menu.StartCampaign("castilla-mission");yield return Ready();
            var campaign=Campaign;var selection=campaign.GetComponent<CampaignBoardSelection>();selection.LocateCell(campaign.Mission.Destination);campaign.MoveToSelectedCell();
            var position=campaign.Army.Position;var movement=campaign.Army.RemainingMovement;var destination=campaign.MarchDestination;
            Assert.That(destination.HasValue,Is.True);Session.Open();Assert.That(Session.SaveAndReturn(),Is.True);yield return Ready(true);
            Assert.That(Menu.ContinueButton.interactable,Is.True);PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,"{}");
            Menu.ContinueButton.onClick.Invoke();yield return Ready();
            Assert.That(Campaign.Army.Position,Is.EqualTo(position));Assert.That(Campaign.Army.RemainingMovement,Is.EqualTo(movement));Assert.That(Campaign.MarchDestination,Is.EqualTo(destination));
            Assert.That(PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey)=="{}",Is.True);
        }
        [UnityTest]public IEnumerator ReturningWithoutSavingPreservesThePreviousSlot()
        {
            Menu.StartCampaign("castilla-mission");yield return Ready();Assert.That(Campaign.TrySaveCampaign(),Is.True);string before=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey);
            Campaign.CompleteTurn();Campaign.CompleteTurn();Session.Open();Assert.That(Session.DiscardAndReturn(),Is.True);yield return Ready(true);
            Assert.That(PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey)==before,Is.True);Assert.That(Menu.ContinueButton.interactable,Is.True);
            Menu.ContinueCampaign();yield return Ready();Assert.That(Campaign.State.Turn,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator PendingBattleCannotBeDiscardedOrSavedThroughTheSessionMenu()
        {
            Menu.StartCampaign("castilla-conquest");yield return Ready();Campaign.CompleteTurn();Assert.That(Campaign.HasPendingBattle,Is.True);
            Assert.That(Session.Open(),Is.False);Assert.That(Session.DiscardAndReturn(),Is.False);Assert.That(Campaign.TrySaveCampaign(),Is.False);
            Assert.That(PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey),Is.False);
        }
        [UnityTest]public IEnumerator CorruptSaveDoesNotEnableContinueOrChangeScenes()
        {
            PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,"not-json");Menu.RefreshContinuation();Assert.That(Menu.ContinueButton.interactable,Is.False);Assert.That(Menu.ContinueCampaign(),Is.False);
            yield return null;Assert.That(Menu,Is.Not.Null);Assert.That(Campaign,Is.Null);
        }
        [UnityTest]public IEnumerator WrongModeSnapshotFailsAtomicallyAndCaptureDoesNotWriteToDisk()
        {
            var catalog=Menu.Catalog;string json=JsonUtility.ToJson(CampaignLaunchCatalogTests.InitialSnapshot(catalog.Find("castilla-conquest")));
            Menu.StartCampaign("castilla-mission");yield return Ready();var state=Campaign.State;var army=Campaign.Army;
            LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));Assert.That(Campaign.TryLoadCampaignSnapshot(json),Is.False);
            Assert.That(Campaign.State,Is.SameAs(state));Assert.That(Campaign.Army,Is.SameAs(army));Campaign.CaptureCampaignSnapshot();Assert.That(PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey),Is.False);
        }
        [UnityTest]public IEnumerator FinishedConquestCanReturnToStartAndContinueWithCommandsFrozen()
        {
            var data=CampaignLaunchCatalogTests.InitialSnapshot(Menu.Catalog.Find("castilla-conquest"));
            data.armyRegistry.armies=data.armyRegistry.armies.Where(a=>a.id.StartsWith("Rival")).ToArray();data.selectedArmyId=null;
            foreach(var owner in data.settlementOwners)owner.ownerFactionId="rival";
            PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey,JsonUtility.ToJson(data));Menu.RefreshContinuation();Assert.That(Menu.ContinueButton.interactable,Is.True);
            Menu.ContinueCampaign();yield return Ready();Assert.That(Campaign.Conclusion,Is.EqualTo(CampaignConclusion.Defeat));
            Assert.That(Campaign.GetComponent<CampaignSelectionPanel>().EndTurnButton.interactable,Is.False);Assert.That(Session.Open(),Is.True);Assert.That(Session.DiscardAndReturn(),Is.True);
            yield return Ready(true);Assert.That(Menu.ContinueButton.interactable,Is.True);
        }
    }
}
