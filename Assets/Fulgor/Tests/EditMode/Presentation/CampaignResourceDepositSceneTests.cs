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
    public sealed class CampaignResourceDepositSceneTests
    {
        private const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        // yacimientos que el escenario de prueba fija en el territorio de la fortaleza (PlayableConquest.fixedDeposits)
        private static readonly HexCoordinates RingGold = new(12, 8), RingStone = new(14, 8);
        private bool hadSave;
        private string saved, oldScene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave = PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);
            saved = PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, "");
            oldScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(ConquestScene);
            yield return new EnterPlayMode();
            for (int i = 0; i < 300 && Object.FindAnyObjectByType<CampaignSettlementController>()?.Ready != true; i++) yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<CampaignSettlementController>().Ready, "La campaña no se ha iniciado.");
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            for (int i = 0; i < 180 && buildings.Placed.Count == 0; i++) yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (hadSave) PlayerPrefs.SetString(CampaignTurnController.LocalSaveKey, saved); else PlayerPrefs.DeleteKey(CampaignTurnController.LocalSaveKey);
            if (!string.IsNullOrEmpty(oldScene)) EditorSceneManager.OpenScene(oldScene);
        }

        private static GameObject[] Deposits(CampaignTileBuildings buildings) => buildings.Placed.Where(b => b != null && b.name.StartsWith("Yacimiento")).ToArray();

        [UnityTest]
        public IEnumerator EveryFreeDepositHasItsModelAndNoYellowMarkerRemains()
        {
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            var campaign = buildings.GetComponent<CampaignTurnController>();
            var settlements = buildings.GetComponent<CampaignSettlementController>();
            var deposits = ResourceDepositLayout.For(buildings.GetComponent<CampaignBoardPreview>().CurrentMap);
            var models = Deposits(buildings);
            int covered = deposits.Keys.Count(c => settlements.HasSettlementAt(c) || buildings.Occupies(c));
            Assert.AreEqual(deposits.Count - covered, models.Length, "Un modelo por yacimiento libre.");
            foreach (var model in models)
            {
                Assert.IsTrue(buildings.TryGetCell(model, out var cell));
                Assert.IsTrue(deposits.TryGetValue(cell, out var kind), model.name);
                StringAssert.StartsWith(kind == ResourceDepositKind.Gold ? "Yacimiento de oro" : "Yacimiento de piedra", model.name);
                Assert.IsFalse(campaign.IsCityTileOccupied(cell), model.name + ": sobre el yacimiento se puede construir su mina.");
                Assert.AreEqual(CampaignTileBuildings.IsShownOn(campaign.VisibilityAt(cell)), model.GetComponentsInChildren<Renderer>(true).All(r => !r.forceRenderingOff), model.name);
            }
            Assert.IsFalse(Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Any(t => t.transform.Find("Oro") != null), "Los modelos sustituyen a la pieza amarilla.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ACityMinesADepositInItsTerritoryAndTheMineReplacesTheRock()
        {
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            var campaign = buildings.GetComponent<CampaignTurnController>();
            var settlements = buildings.GetComponent<CampaignSettlementController>();
            var own = settlements.Registry.Settlements.First(s => s.OwnerFactionId == campaign.PlayerFactionId);
            Assert.AreEqual(1, own.Position.DistanceTo(RingGold), "El oro de prueba del escenario está en el territorio de la fortaleza.");
            Assert.AreEqual(ResourceDepositKind.Gold, campaign.DepositAt(RingGold));
            Assert.AreEqual(ResourceDepositKind.Stone, campaign.DepositAt(RingStone), "El escenario de prueba fija un yacimiento de cada tipo en el territorio.");
            Assert.AreEqual(1, own.Position.DistanceTo(RingStone));

            var save = campaign.CaptureCampaignSnapshot(); save.wood = 200; save.stone = 200; save.labor = 200;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(save)));
            yield return null; yield return null;
            var before = JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            var tile = campaign.GetComponentsInChildren<HexTileView>().Single(t => t.Cell.Coordinates.Equals(RingGold));
            var rock = Deposits(buildings).Single(b => buildings.TryGetCell(b, out var cell) && cell.Equals(RingGold));
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id, tile.Cell, "farm"), "El yacimiento queda reservado para su mina.");
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id, tile.Cell, "quarry"), "En un yacimiento de oro no cabe una cantera.");
            int stone = campaign.State.Resources.Stone;
            Assert.IsTrue(campaign.TryPlaceCityTile(own.Id, tile.Cell, "goldmine"));
            Assert.AreEqual(stone - CityTileConstruction.Cost("goldmine").Stone, campaign.State.Resources.Stone);
            yield return null;
            Assert.IsTrue(rock.activeSelf, "Mientras dura la obra sigue viéndose la roca.");

            for (int turn = 0; turn < 2; turn++)
            {
                campaign.CompleteTurn();
                for (int i = 0; i < 300 && campaign.IsRivalPhaseActive; i++) yield return null;
            }
            yield return null;
            Assert.AreEqual("goldmine", campaign.CityTileBuildingAt(RingGold));
            Assert.AreEqual(CityTileConstruction.Active("goldmine").Gold, campaign.TileCity(own.Id).ActiveIncome.Gold);
            Assert.IsFalse(rock.activeSelf, "La mina sustituye a la roca.");
            var mine = buildings.Placed.Where(b => b != null && b.activeSelf && buildings.TryGetCell(b, out var cell) && cell.Equals(RingGold)).ToArray();
            Assert.AreEqual(1, mine.Length, "Un solo modelo a la vista en la casilla.");
            Assert.AreEqual("MinaOro", mine[0].name);
            Assert.AreEqual(buildings.Placed.Count(b => b != null && b.activeSelf), buildings.GroundMap.Buildings, "La roca ya no marca el suelo.");

            // una partida con la mina se carga entera (está en el territorio de la ciudad) y al volver a una sin ella reaparece la roca
            var withMine = JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(withMine));
            yield return null; yield return null;
            Assert.AreEqual("goldmine", campaign.CityTileBuildingAt(RingGold));
            Assert.AreEqual("MinaOro", buildings.Placed.Single(b => b != null && b.activeSelf && buildings.TryGetCell(b, out var cell) && cell.Equals(RingGold)).name);
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(before));
            yield return null; yield return null;
            Assert.IsNull(campaign.CityTileBuildingAt(RingGold));
            Assert.AreEqual(1, Deposits(buildings).Count(b => b.activeSelf && buildings.TryGetCell(b, out var cell) && cell.Equals(RingGold)), "Vuelve la roca.");
        }
        [UnityTest]
        public IEnumerator ADepositOutsideTheCityTerritoryCannotBeMined()
        {
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            var campaign = buildings.GetComponent<CampaignTurnController>();
            var settlements = buildings.GetComponent<CampaignSettlementController>();
            var own = settlements.Registry.Settlements.First(s => s.OwnerFactionId == campaign.PlayerFactionId);
            // el yacimiento visible más cercano fuera del territorio (a dos casillas o más)
            var far = campaign.GetComponentsInChildren<HexTileView>().Select(t => t.Cell.Coordinates)
                .Where(c => own.Position.DistanceTo(c) >= 2 && campaign.DepositAt(c) != ResourceDepositKind.None && campaign.VisibilityAt(c) != CampaignVisibility.Unknown)
                .OrderBy(c => own.Position.DistanceTo(c)).First();
            var mine = CityTileConstruction.MineFor(campaign.DepositAt(far));

            var save = campaign.CaptureCampaignSnapshot(); save.wood = 200; save.stone = 200; save.labor = 200;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(save)));
            yield return null; yield return null;
            var tile = campaign.GetComponentsInChildren<HexTileView>().Single(t => t.Cell.Coordinates.Equals(far));
            var rock = Deposits(buildings).Single(b => buildings.TryGetCell(b, out var cell) && cell.Equals(far));
            // fuera del territorio de la ciudad (primer anillo) no se puede minar; hace falta otra ciudad más cerca
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id, tile.Cell, mine), "Fuera del territorio no hay mina.");
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id, tile.Cell, "farm"), "El yacimiento queda reservado para su mina.");
            Assert.IsNull(campaign.CityTileBuildingAt(far));
            Assert.IsTrue(rock.activeSelf, "La roca sigue a la vista.");

            // en la vista de la ciudad no se marca como casilla disponible
            Assert.IsTrue(settlements.OpenCityMapAt(own.Position));
            yield return null;
            var camera = Camera.main; var anchor = camera.WorldToScreenPoint(tile.transform.position);
            foreach (var ring in Object.FindObjectsByType<RectTransform>().Where(r => r.name == "Casilla disponible" && r.gameObject.activeInHierarchy))
                Assert.Greater(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, ring.position), anchor), 30f, "Ningún círculo sobre un yacimiento fuera del territorio.");
            campaign.GetComponent<CampaignCityMapView>().Close();
        }
    }
}
