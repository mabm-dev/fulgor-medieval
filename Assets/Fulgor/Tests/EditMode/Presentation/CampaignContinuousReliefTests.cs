using System.Collections;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fulgor.Presentation.Tests.Map
{
    /// <summary>Relieve continuo: toda la tierra de una sola función, y los árboles plantados sobre ella.</summary>
    public sealed class CampaignContinuousReliefTests
    {
        private const float Edge = CampaignMountainRelief.EdgeHeight;
        private static readonly HexCoordinates Mountain = new(5, 5), Hill = new(9, 5);
        private static readonly HexCoordinates[] Forest = { new(1, 1), new(2, 1), new(1, 2), new(2, 2) };
        private static readonly HexCoordinates Built = new(7, 2);

        // un tablero de 12 × 9 de llanura con una montaña, una colina y un bosque; la fila y la columna de fuera no se generan
        private static CampaignMountainRelief Land(int seed = 7)
        {
            var land = Enumerable.Range(0, 12).SelectMany(q => Enumerable.Range(0, 9).Select(r => new HexCoordinates(q, r))).ToArray();
            var low = land.Where(c => !c.Equals(Mountain) && !c.Equals(Hill)).ToArray();
            return new CampaignMountainRelief(new[] { Mountain }, seed, VisualScaleContract.HexOuterRadius, new[] { Hill }, new[] { Built }, low, Forest);
        }

        private static Vector2 Between(CampaignMountainRelief relief, HexCoordinates a, HexCoordinates b) => (relief.Centre(a) + relief.Centre(b)) * .5f;

        [Test]
        public void TheFootOfAMountainStartsBeforeItsTile()
        {
            var relief = Land();
            Assert.IsTrue(relief.IsContinuous);
            var neighbour = new HexCoordinates(Mountain.Q + 1, Mountain.R);
            Assert.IsTrue(relief.IsGenerated(neighbour)); Assert.IsFalse(relief.IsHighland(neighbour));
            // en la arista con la llanura ya se ha subido algo: no hay escalón que dibuje el hexágono
            float border = Enumerable.Range(0, 6).Average(k => relief.Height(Between(relief, Mountain, Mountain.Neighbors().ElementAt(k))));
            Assert.Greater(border, Edge + .03f);
            Assert.Greater(relief.Height(relief.Centre(Mountain)), border + .4f, "La cumbre sigue destacando.");
            Assert.Less(relief.Height(relief.Centre(new HexCoordinates(Mountain.Q + 3, Mountain.R))), Edge + .16f, "Lejos de la montaña, la llanura solo ondula.");
        }

        [Test]
        public void ThePlainRollsAndReturnsToTheEdgeHeightBesideWhatIsNotGenerated()
        {
            var relief = Land();
            var inner = new HexCoordinates(6, 6);
            var heights = Enumerable.Range(0, 40).Select(i => relief.Height(relief.Centre(inner) + new Vector2(Mathf.Cos(i), Mathf.Sin(i)) * (i / 40f))).ToArray();
            Assert.Greater(heights.Max() - heights.Min(), .004f, "La llanura no es un plano.");
            Assert.Less(heights.Max(), Edge + .2f);
            Assert.GreaterOrEqual(heights.Min(), Edge - 1e-4f);
            // el borde del mapa: la casilla (0, 4) tiene fuera a (-1, 4), que no se genera
            var outside = new HexCoordinates(-1, 4); var rim = new HexCoordinates(0, 4);
            Assert.IsFalse(relief.IsGenerated(outside));
            Assert.AreEqual(Edge, relief.Height(Between(relief, rim, outside)), 1e-5f);
            Assert.AreEqual(Edge, relief.Height(relief.Centre(outside)), 1e-6f);
        }

        [Test]
        public void NeighbouringTilesOfAnyKindMatchAlongTheirEdge()
        {
            var relief = Land();
            var plain = new HexCoordinates(Mountain.Q + 1, Mountain.R);
            var tiles = relief.BuildTiles(new[] { Mountain }, 32);
            foreach (var pair in relief.BuildTiles(new[] { plain }, 16)) tiles.Add(pair.Key, pair.Value);
            try
            {
                var a = relief.Centre(Mountain); var b = relief.Centre(plain);
                var middle = (a + b) * .5f; var along = new Vector2(-(b - a).y, (b - a).x).normalized;
                int matched = 0;
                foreach (var v in tiles[plain].Full.vertices)
                {
                    var p = new Vector2(v.x, v.z) + b;
                    if (Mathf.Abs(Vector2.Dot(p - middle, (b - a).normalized)) > 1e-4f || Mathf.Abs(Vector2.Dot(p - middle, along)) > .5f + 1e-4f) continue;
                    float surface = relief.Height(p);
                    if (Mathf.Abs(v.y - surface) > .03f) continue; // faldón
                    Assert.AreEqual(surface, v.y, 1e-4f);
                    Assert.IsTrue(tiles[Mountain].Full.vertices.Any(w => Vector2.Distance(new Vector2(w.x, w.z) + a, p) < 1e-4f && Mathf.Abs(w.y - v.y) < 1e-4f),
                        "El mismo punto, a la misma altura, en la montaña vecina.");
                    matched++;
                }
                Assert.AreEqual(17, matched, "La arista de la llanura (16 tramos) tiene 17 puntos, todos en la malla de la montaña.");
            }
            finally
            {
                foreach (var levels in tiles.Values) { Object.DestroyImmediate(levels.Full); Object.DestroyImmediate(levels.Medium); Object.DestroyImmediate(levels.Distant); }
            }
        }

        [Test]
        public void TheSurfaceKnowsForestRockAndABuiltTileIsLevel()
        {
            var relief = Land();
            Assert.Greater(relief.SurfaceAt(relief.Centre(Mountain)).r, .9f, "La cumbre es roca.");
            Assert.Less(relief.SurfaceAt(relief.Centre(new HexCoordinates(9, 2))).r, .05f, "Lejos de la montaña no hay grava.");
            Assert.Greater(relief.SurfaceAt((relief.Centre(Forest[0]) + relief.Centre(Forest[3])) * .5f).b, .9f, "Dentro del bosque.");
            Assert.Less(relief.SurfaceAt(relief.Centre(new HexCoordinates(8, 7))).b, .05f, "En la llanura.");
            Assert.IsTrue(relief.IsLevelled(Built), "Con relieve continuo también se allana una llanura con edificio.");
            float level = relief.Height(relief.Centre(Built));
            for (int k = 0; k < 12; k++)
                Assert.AreEqual(level, relief.Height(relief.Centre(Built) + new Vector2(Mathf.Cos(k), Mathf.Sin(k)) * CampaignMountainRelief.FlatRadius * .9f), 1e-5f);
        }

        [Test]
        public void TreesGrowWhereTheForestIsAndLeaveTheCentreClear()
        {
            var relief = Land();
            var forest = CampaignForest.Layout(relief, Forest[0], false);
            var again = CampaignForest.Layout(Land(), Forest[0], false);
            Assert.AreEqual(forest.Count, again.Count, "El mismo mapa da el mismo bosque.");
            Assert.IsTrue(forest.Zip(again, (a, b) => a.Position == b.Position && a.Kind == b.Kind).All(same => same));
            int trees = forest.Count(t => t.Kind != CampaignForest.Shrub);
            Assert.Greater(trees, 5, "Una casilla de bosque tiene árboles.");
            int plain = CampaignForest.Layout(relief, new HexCoordinates(8, 7), false).Count(t => t.Kind != CampaignForest.Shrub);
            Assert.Less(plain, trees / 2, "En la llanura, como mucho alguno suelto.");
            foreach (var tree in forest)
            {
                Assert.AreEqual(Forest[0], HexWorldLayout.FromWorld(new Vector3(tree.Position.x, 0f, tree.Position.y)), "Cada árbol, dentro de su casilla.");
                if (tree.Kind != CampaignForest.Shrub)
                    Assert.GreaterOrEqual((tree.Position - relief.Centre(Forest[0])).magnitude, CampaignForest.CentreClearing, "El centro queda libre para la hueste.");
            }
            foreach (var tree in CampaignForest.Layout(relief, Forest[1], true))
                Assert.GreaterOrEqual((tree.Position - relief.Centre(Forest[1])).magnitude, CampaignForest.BuiltClearing, "Un edificio deja su claro.");
            Assert.IsEmpty(CampaignForest.Layout(relief, Mountain, false).Where(t => relief.SurfaceAt(t.Position).r > .56f), "En la roca no crece nada.");
        }

        [UnityTest]
        public IEnumerator TheConquestMapIsOneSurfaceWithTreesThatFollowTheFog()
        {
            bool hadSave = PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);
            string saved = PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, ""), oldScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorConquest.unity");
            yield return new EnterPlayMode();
            try
            {
                CampaignMapAppearance appearance = null;
                for (int i = 0; i < 300 && (appearance = Object.FindFirstObjectByType<CampaignMapAppearance>())?.Ready != true; i++) yield return null;
                Assert.IsTrue(appearance != null && appearance.Ready);
                Assert.Greater(appearance.TreeCount, 300, "El mapa de Conquista tiene bosques.");
                yield return null; yield return null;
                var campaign = appearance.GetComponent<CampaignTurnController>();
                var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
                // toda la tierra sale del relieve: ninguna casilla de tierra conserva la pieza fija, que giraba de 60 en 60°
                foreach (var tile in tiles.Where(t => t.Cell.Terrain != TerrainType.Water))
                    Assert.AreEqual(Quaternion.identity, tile.transform.localRotation, tile.name);
                int hidden = 0, shown = 0;
                foreach (var tile in tiles)
                {
                    var trees = tile.transform.Find("Árboles")?.GetComponent<MeshRenderer>();
                    if (trees == null) continue;
                    Assert.IsNull(trees.GetComponent<Collider>(), "Los árboles no estorban al ratón.");
                    bool unknown = campaign.VisibilityAt(tile.Cell.Coordinates) == CampaignVisibility.Unknown;
                    Assert.AreEqual(unknown, trees.forceRenderingOff, tile.name + ": los árboles se ocultan con su casilla.");
                    if (unknown) hidden++; else shown++;
                }
                Assert.Greater(hidden, 0); Assert.Greater(shown, 0);
            }
            finally
            {
                if (hadSave) PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey, saved); else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);
            }
            yield return new ExitPlayMode();
            if (!string.IsNullOrEmpty(oldScene)) EditorSceneManager.OpenScene(oldScene);
        }
    }
}
