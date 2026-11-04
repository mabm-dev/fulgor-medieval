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
using Object = UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignTileBuildingsFogTests
    {
        private const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        private bool hadSave;
        private string saved, oldScene;

        [TestCase(CampaignVisibility.Unknown, false)]
        [TestCase(CampaignVisibility.Explored, true)]
        [TestCase(CampaignVisibility.Visible, true)]
        public void ABuildingIsOnlyHiddenOnUnexploredTerrain(CampaignVisibility visibility, bool shown) =>
            Assert.AreEqual(shown, CampaignTileBuildings.IsShownOn(visibility));

        [UnityTest]
        public IEnumerator BuildingsOnUnexploredTerrainAreNotDrawn()
        {
            hadSave = PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);
            saved = PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, "");
            oldScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(ConquestScene);
            yield return new EnterPlayMode();
            yield return null;
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            Assert.NotNull(buildings, "La escena de Conquista no tiene edificios de casilla.");
            var board = buildings.GetComponent<CampaignBoardPreview>();
            var campaign = buildings.GetComponent<CampaignTurnController>();
            Assert.IsTrue(campaign.FogEnabled, "Conquista tiene niebla de guerra.");

            // un edificio más, en una casilla de llanura lejos de la fortaleza y de la hueste inicial, que empieza sin explorar
            var home = new HexCoordinates(13, 8);
            var far = ProceduralMapGenerator.Generate(24, 16, 12345).Cells
                .First(c => c.Terrain == TerrainType.Plain && c.Coordinates.DistanceTo(home) > 8).Coordinates;
            var data = new SerializedObject(buildings);
            var entries = data.FindProperty("entries");
            int count = entries.arraySize;
            var prefab = entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue;
            entries.arraySize = count + 1;
            var added = entries.GetArrayElementAtIndex(count);
            added.FindPropertyRelative("label").stringValue = "Lejano";
            added.FindPropertyRelative("cell").vector2IntValue = new Vector2Int(far.Q, far.R);
            added.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            data.ApplyModifiedPropertiesWithoutUndo();
            board.RegenerateBoard();
            for (int i = 0; i < 90 && buildings.Placed.Count < count + 1; i++) yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(count + 1, buildings.Placed.Count, "Todos los edificios deberían haberse colocado.");
            var distant = buildings.Placed.Single(b => b.name == "Lejano");
            Assert.AreEqual(CampaignVisibility.Unknown, campaign.VisibilityAt(far));
            Assert.IsTrue(distant.GetComponentsInChildren<Renderer>(true).All(r => r.forceRenderingOff), "Sobre terreno sin explorar no debe dibujarse.");
            var others = buildings.Placed.Where(b => b != distant).ToArray();
            foreach (var building in others)
            {
                Assert.IsTrue(buildings.TryGetCell(building, out var cell));
                bool expected = CampaignTileBuildings.IsShownOn(campaign.VisibilityAt(cell));
                Assert.AreEqual(expected, building.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff), building.name);
            }
            Assert.IsTrue(others.Any(b => b.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff)), "Los edificios sobre terreno visible tienen que dibujarse.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (hadSave) PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey, saved); else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);
            if (!string.IsNullOrEmpty(oldScene)) EditorSceneManager.OpenScene(oldScene);
        }
    }
}
