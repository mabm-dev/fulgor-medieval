using System.Collections;
using System.Linq;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignSceneSmokeTests
    {
        [Test]
        public void MetricsDeriveRendererAndColliderCounts()
        {
            var metrics = new BoardBuildMetrics(12.5, 2048, 384, 120, 80, 10, 40, 10);
            Assert.That(metrics.Renderers, Is.EqualTo(594));
            Assert.That(metrics.Colliders, Is.EqualTo(384));
        }

        [UnityTest]
        public IEnumerator CampaignSceneBuildsMeasuredBoardAndAdaptivePanel()
        {
            var previousScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");
            yield return new EnterPlayMode();
            yield return null;

            var board = Object.FindAnyObjectByType<CampaignBoardPreview>();
            var report = board.GetComponent<CampaignBoardDiagnostics>();
            var panel = board.GetComponent<CampaignSelectionPanel>();
            Assert.That(report.Completed, Is.True);
            Assert.That(report.Metrics.Tiles, Is.EqualTo(384));
            Assert.That(report.Metrics.TransitionInstances, Is.GreaterThan(0));
            Assert.That(report.Metrics.JunctionInstances, Is.GreaterThan(0));
            Assert.That(report.Metrics.Milliseconds, Is.GreaterThan(0d));
            Assert.That(board.GetComponentsInChildren<MeshCollider>().Length,
                Is.EqualTo(report.Metrics.Colliders));
            Assert.That(board.GetComponentsInChildren<MeshRenderer>().Length,
                Is.EqualTo(report.Metrics.Renderers));
            Assert.That(board.GetComponentsInChildren<MeshFilter>()
                    .Select(item => item.sharedMesh).Distinct().Count(),
                Is.EqualTo(report.Metrics.UniqueMeshes));

            Canvas.ForceUpdateCanvases();
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(panel.PanelRectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(panel.PanelRectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(panel.PanelRectTransform.sizeDelta.x, Is.GreaterThanOrEqualTo(320f));
            var tile = board.GetComponentInChildren<HexTileView>();
            board.GetComponent<CampaignBoardSelection>().Select(tile);
            Assert.That(panel.GetComponentsInChildren<Text>()
                .Any(text => text.text.Contains(tile.Cell.Coordinates.Key)), Is.True);

            yield return new ExitPlayMode();
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene);
        }
    }
}
