using System.Collections;
using System.Linq;
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
    public sealed class CampaignSettlementSceneTests
    {
        [UnityTest]public IEnumerator ArtReviewReusesItsCastleAfterBoardRegeneration()
        {
            string previous=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity");yield return new EnterPlayMode();
            var board=Object.FindFirstObjectByType<CampaignBoardPreview>();var controller=board.GetComponent<CampaignSettlementController>();
            for(int i=0;i<30&&!controller.Ready;i++)yield return null;
            Assert.That(controller.Ready,Is.True);
            var castle=Object.FindFirstObjectByType<FortressMapArtReview>().PlacedFortress.gameObject;
            board.RegenerateBoard();yield return null;yield return null;
            Assert.That(controller.Ready,Is.True);Assert.That(controller.Views.Count,Is.EqualTo(1));
            var view=controller.Views.Values.Single();Assert.That(view.gameObject,Is.SameAs(castle));
            Assert.That(Object.FindObjectsByType<CampaignSettlementView>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(view.Tile.transform.IsChildOf(board.transform.Find("Generated Board")),Is.True);
            yield return new ExitPlayMode();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }

        [UnityTest]public IEnumerator CastleSupportsSelectionMarchPresenceAndBoardRegeneration()
        {
            string previous=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            var board=Object.FindFirstObjectByType<CampaignBoardPreview>();var controller=board.GetComponent<CampaignSettlementController>();
            Assert.That(controller.Ready,Is.True);Assert.That(controller.Registry.Settlements.Count,Is.EqualTo(1));
            var castle=controller.Registry.Settlements[0];var selection=board.GetComponent<CampaignBoardSelection>();var turn=board.GetComponent<CampaignTurnController>();
            var initial=turn.State;Assert.That(controller.LocateFirst(),Is.True);Assert.That(turn.State,Is.SameAs(initial));
            Assert.That(selection.SelectedCell.Value.Coordinates,Is.EqualTo(castle.Position));Assert.That(controller.Details,Does.Contain("Castilla"));
            Assert.That(controller.Views[castle.Position].Selected,Is.True);
            var view=controller.Views[castle.Position];Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Ray(view.transform.position+Vector3.up*3,Vector3.down),out var hit,6),Is.True);
            Assert.That(selection.ResolveHit(hit.collider),Is.SameAs(view.Tile));
            Canvas.ForceUpdateCanvases();var pointer=RectTransformUtility.WorldToScreenPoint(null,controller.LocateButton.transform.position);
            Assert.That(selection.BlocksPointer(pointer),Is.True);
            Assert.That(controller.MarchToSelection(),Is.True);
            for(int i=0;i<30&&turn.Army.Position!=castle.Position;i++){turn.CompleteTurn();yield return null;}
            yield return null;
            Assert.That(turn.Army.Position,Is.EqualTo(castle.Position));Assert.That(controller.MarchButton.interactable,Is.False);
            Assert.That(board.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Hueste del jugador presente")),Is.True);
            Assert.That(controller.Registry.Settlements[0].OwnerFactionId,Is.EqualTo("castilla"));
            selection.ClearSelection();Assert.That(view.Selected,Is.False);Assert.That(controller.MarchToSelection(),Is.False);
            var oldRoot=board.transform.Find("Generated Board");board.RegenerateBoard();yield return null;yield return null;
            Assert.That(controller.Ready,Is.True);Assert.That(board.transform.Find("Generated Board"),Is.Not.SameAs(oldRoot));
            Assert.That(controller.Views.Count,Is.EqualTo(1));Assert.That(board.GetComponentsInChildren<CampaignSettlementView>().Length,Is.EqualTo(1));
            yield return new ExitPlayMode();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }

        [UnityTest]public IEnumerator OtherOwnerCanBeInspectedWithoutOrderingAnAlliedMarch()
        {
            string previous=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            var controller=Object.FindFirstObjectByType<CampaignSettlementController>();controller.enabled=false;
            var config=new SerializedObject(controller);var property=config.FindProperty("settlements");
            var definition=Object.Instantiate((CampaignSettlementDefinition)property.GetArrayElementAtIndex(0).objectReferenceValue);
            var definitionData=new SerializedObject(definition);definitionData.FindProperty("ownerFactionId").stringValue="otra-faccion";definitionData.FindProperty("ownerDisplayName").stringValue="Otra facción";definitionData.ApplyModifiedPropertiesWithoutUndo();
            property.GetArrayElementAtIndex(0).objectReferenceValue=definition;config.ApplyModifiedPropertiesWithoutUndo();controller.enabled=true;yield return null;yield return null;
            var turn=controller.GetComponent<CampaignTurnController>();var army=turn.Army;
            Assert.That(controller.LocateFirst(),Is.True);Assert.That(controller.Details,Does.Contain("Otra facción"));
            Assert.That(controller.MarchButton.interactable,Is.False);Assert.That(controller.MarchToSelection(),Is.False);Assert.That(turn.Army,Is.SameAs(army));
            yield return new ExitPlayMode();Object.DestroyImmediate(definition);if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }
    }
}
