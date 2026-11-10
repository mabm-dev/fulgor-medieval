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
            for (int i = 0; i < 90 && Buildings(buildings) < count + 1; i++) yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(count + 1, Buildings(buildings), "Todos los edificios deberían haberse colocado.");
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

        [UnityTest]
        public IEnumerator TerritoryShadeHugsTheTerrainAndTheArmyStandsInFrontOfABuilding()
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
            var overlays = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < 180 && overlays.Count == 0; i++)
            {
                yield return null;
                foreach (var view in Object.FindObjectsByType<CampaignSettlementView>(FindObjectsSortMode.None))
                    foreach (Transform child in view.transform)
                        if (child.name == "Sombreado del territorio") overlays.Add(child);
            }
            Assert.IsNotEmpty(overlays, "El primer anillo de la fortaleza debe llevar sombreado.");

            // el sombreado es la malla de la propia casilla, puesta sobre ella y apenas levantada: sigue el relieve y no cruza los edificios
            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            foreach (var overlay in overlays)
            {
                var tile = tiles.OrderBy(t => Vector3.ProjectOnPlane(t.transform.position - overlay.position, Vector3.up).sqrMagnitude).First();
                Assert.Less(Vector3.ProjectOnPlane(tile.transform.position - overlay.position, Vector3.up).magnitude, .01f, "Debe estar sobre una casilla.");
                Assert.AreSame(tile.GetComponent<MeshFilter>().sharedMesh, overlay.GetComponent<MeshFilter>().sharedMesh, "Debe usar la malla de su casilla.");
                Assert.AreNotEqual(TerrainType.Mountain, tile.Cell.Terrain, "Las montañas no se tiñen.");
                Assert.That(overlay.position.y - tile.transform.position.y, Is.InRange(0f, .02f), "Solo flota unos milímetros sobre el suelo.");
            }

            // una hueste sobre una casilla con edificio se planta delante, hacia la cámara
            var camera = Camera.main;
            var toCamera = -Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            int checkedArmies = 0;
            foreach (var marker in Object.FindObjectsByType<CampaignArmyMarker>(FindObjectsSortMode.None))
            {
                if (!marker.IsVisible) continue;
                var coordinate = HexWorldLayout.FromWorld(board.transform.InverseTransformPoint(marker.VisualPosition));
                if (!buildings.HasBuildingAt(coordinate)) continue;
                var tile = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Single(t => t.Cell.Coordinates.Equals(coordinate));
                var offset = Vector3.ProjectOnPlane(marker.VisualPosition - tile.transform.position, Vector3.up);
                Assert.GreaterOrEqual(offset.magnitude, .6f, "La hueste no debe quedar dentro del edificio.");
                Assert.Greater(Vector3.Dot(offset.normalized, toCamera), .9f, "Debe quedar del lado de la cámara.");
                checkedArmies++;
            }
            Assert.Greater(checkedArmies, 0, "Alguna hueste inicial está sobre una casilla con edificio (la vivienda de prueba).");
        }

        [UnityTest]
        public IEnumerator BuildingsSitOnLevelGroundAndMarkTheGroundAround()
        {
            hadSave = PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);
            saved = PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, "");
            oldScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(ConquestScene);
            yield return new EnterPlayMode();
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            Assert.NotNull(buildings, "La escena de Conquista no tiene edificios de casilla.");
            for (int i = 0; i < 180 && buildings.Placed.Count == 0; i++) yield return null;
            Assert.IsNotEmpty(buildings.Placed);
            Assert.NotNull(buildings.GroundMap, "Con edificios se publica el mapa de huellas para el suelo pisado.");
            Assert.AreEqual(buildings.Placed.Count, buildings.GroundMap.Buildings);
            Assert.AreEqual(1f, Shader.GetGlobalFloat("_FulgorHuellasActivas"));

            var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).GroupBy(t => t.Cell.Coordinates).ToDictionary(g => g.Key, g => g.First());
            bool onHill = false;
            foreach (var building in buildings.Placed)
            {
                var footprint = building.GetComponent<CampaignBuildingFootprint>();
                Assert.NotNull(footprint, building.name + " lleva su huella.");
                Assert.IsTrue(buildings.TryGetCell(building, out var cell));
                var collider = tiles[cell].GetComponent<MeshCollider>();
                float baseY = building.transform.position.y + footprint.Bottom;
                var edge = Enumerable.Range(0, 16).Select(k => footprint.Outline(k * Mathf.PI / 8f))
                    .Select(p => Ground(collider, building.transform.TransformPoint(new Vector3(p.x, 0f, p.y)))).OrderBy(y => y).ToArray();
                Assert.AreEqual(CampaignTileBuildings.SeatDepthFor(footprint), edge[8] - baseY, 1e-3f, building.name + ": la base queda bajo el suelo de su borde, con la losa a ras.");
                Assert.That(CampaignTileBuildings.SeatDepthFor(footprint), Is.InRange(CampaignTileBuildings.SeatDepth, CampaignTileBuildings.SeatDepth + .025f));
                // el suelo apenas cambia bajo el borde (la llanura es plana y la colina se allana): la losa no deja rendijas
                Assert.Less(edge[12] - edge[4], .015f, building.name + ": suelo llano bajo el borde.");
                var centre = building.transform.position;
                Assert.Less(buildings.GroundMap.DistanceAt(new Vector2(centre.x, centre.z)), 0f, building.name + ": su huella está en el mapa.");
                var outside = building.transform.TransformPoint(new Vector3(footprint.Centre.x + footprint.HalfSize.x + .1f, 0f, footprint.Centre.y));
                float scale = CampaignTileBuildings.GroundBandScale(footprint);
                Assert.That(scale, Is.InRange(1f, 2f));
                Assert.AreEqual(.1f * scale, buildings.GroundMap.DistanceAt(new Vector2(outside.x, outside.z)), .03f * scale, building.name + ": a 10 cm de su pared.");
                onHill |= tiles[cell].Cell.Terrain == TerrainType.Hill;
            }
            Assert.IsTrue(onHill, "El patio de mesnadas de prueba está en una colina: comprueba el llano del relieve.");
        }

        // sin contar los yacimientos, que salen del mapa y no de la lista de edificios
        private static int Buildings(CampaignTileBuildings layer) => layer.Placed.Count(b => !b.name.StartsWith("Yacimiento"));

        private static float Ground(MeshCollider collider, Vector3 world)
        {
            var bounds = collider.bounds;
            Assert.IsTrue(collider.Raycast(new Ray(new Vector3(world.x, bounds.max.y + 1f, world.z), Vector3.down), out var hit, bounds.size.y + 2f), "Hay suelo bajo el borde.");
            return hit.point.y;
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
