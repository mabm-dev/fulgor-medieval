using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class MapInteractionTests
    {
        [TestCase(-1f, 5f)]
        [TestCase(5f, -1f)]
        [TestCase(101f, 5f)]
        [TestCase(5f, 101f)]
        public void PointerOutsideViewportNeverPans(float x, float y)
        {
            Assert.That(MapPointerPolicy.EdgeDirection(new Vector2(x, y), new Rect(0, 0, 100, 100), 14), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void EdgePanningUsesCameraViewportOffset()
        {
            var viewport = new Rect(100, 200, 300, 400);
            Assert.That(MapPointerPolicy.EdgeDirection(new Vector2(101, 201), viewport, 14), Is.EqualTo(new Vector2(-1, -1)));
            Assert.That(MapPointerPolicy.EdgeDirection(new Vector2(399, 599), viewport, 14), Is.EqualTo(Vector2.one));
            Assert.That(MapPointerPolicy.EdgeDirection(viewport.center, viewport, 14), Is.EqualTo(Vector2.zero));
            Assert.That(MapPointerPolicy.EdgeDirection(new Vector2(101, 201), viewport, 0), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void PanelHitTestConvertsBottomLeftToTopLeftCoordinates()
        {
            var panel = new Rect(24, 24, 230, 112);
            Assert.That(MapPointerPolicy.IsOverPanel(new Vector2(40, 740), 800, panel), Is.True);
            Assert.That(MapPointerPolicy.IsOverPanel(new Vector2(40, 60), 800, panel), Is.False);
            Assert.That(MapPointerPolicy.IsOverPanel(new Vector2(300, 740), 800, panel), Is.False);
        }

        [Test]
        public void SelectionRestoresPreviousColorAndClearRemovesSelection()
        {
            var root = new GameObject("Selection test");
            try
            {
                var owner = root.AddComponent<CampaignBoardSelection>();
                var first = CreateTile(root, owner, 0);
                var second = CreateTile(root, owner, 1);
                owner.Select(first);
                Assert.That(ReadColor(first), Is.Not.EqualTo(Color.green));
                owner.Select(second);
                Assert.That(ReadColor(first), Is.EqualTo(Color.green));
                Assert.That(owner.SelectedCell.Value.Coordinates.Q, Is.EqualTo(1));
                owner.ClearSelection();
                Assert.That(owner.SelectedCell.HasValue, Is.False);
                Assert.That(ReadColor(second), Is.EqualTo(Color.green));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator SelectionRejectsTilesOwnedByAnotherBoardAndClearsOnDisable()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("Selection owner test");
            var otherRoot = new GameObject("Other owner");
            try
            {
                var owner = root.AddComponent<CampaignBoardSelection>();
                var other = otherRoot.AddComponent<CampaignBoardSelection>();
                var tile = CreateTile(root, owner, 0);
                other.Select(tile);
                Assert.That(other.SelectedCell.HasValue, Is.False);
                owner.Select(tile);
                owner.enabled = false;
                Assert.That(owner.SelectedCell.HasValue, Is.False);
                Assert.That(ReadColor(tile), Is.EqualTo(Color.green));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(otherRoot); }
            yield return new ExitPlayMode();
        }

        private static HexTileView CreateTile(GameObject root, CampaignBoardSelection owner, int q)
        {
            var tileObject = new GameObject("Tile", typeof(MeshRenderer));
            tileObject.transform.SetParent(root.transform);
            var tile = tileObject.AddComponent<HexTileView>();
            tile.Initialize(new MapCell(new HexCoordinates(q, 0), TerrainType.Plain, false), owner, Color.green);
            return tile;
        }

        private static Color ReadColor(HexTileView tile)
        {
            var properties = new MaterialPropertyBlock();
            tile.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
            return properties.GetColor(Shader.PropertyToID("_BaseColor"));
        }
    }
}