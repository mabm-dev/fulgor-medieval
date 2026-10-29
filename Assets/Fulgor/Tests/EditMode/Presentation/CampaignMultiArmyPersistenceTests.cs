using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
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
    public sealed class CampaignMultiArmyPersistenceTests
    {
        private const string SaveKey = "Fulgor.Campaign.Prototype.Save.v1";
        private bool hadSave;
        private string previousSave, previousScene;
        private CampaignTurnController campaign;
        private CampaignBoardSelection selection;
        private CampaignSelectionPanel panel;
        private CampaignSettlementController settlements;
        private GeneratedMap map;
        private CampaignArmyRegistry initial;
        private HexCoordinates start, destination, ally, enemy;

        [UnitySetUp] public IEnumerator SetUp()
        {
            hadSave = PlayerPrefs.HasKey(SaveKey); previousSave = PlayerPrefs.GetString(SaveKey, "");
            previousScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");
            yield return new EnterPlayMode(); yield return null; yield return null;
            campaign = Object.FindFirstObjectByType<CampaignTurnController>();
            selection = campaign.GetComponent<CampaignBoardSelection>();
            panel = campaign.GetComponent<CampaignSelectionPanel>();
            settlements = campaign.GetComponent<CampaignSettlementController>();
            map = ProceduralMapGenerator.Generate(24, 16, 12345);
            var graph = selection.Routes;
            // Select an unoccupied affordable move, independent of terrain generation order.
            start = map.Cells.First(c => graph.CanStandAt(c.Coordinates) && c.Coordinates.Neighbors()
                .Count(n => graph.TryGetMovementCost(c.Coordinates, n, out _)) >= 2).Coordinates;
            destination = start.Neighbors().First(n => graph.TryGetMovementCost(start, n, out _));
            ally = start.Neighbors().First(n => n != destination && graph.TryGetMovementCost(start, n, out _));
            enemy = map.Cells.First(c => c.Coordinates != start && c.Coordinates != destination &&
                c.Coordinates != ally && graph.CanStandAt(c.Coordinates)).Coordinates;
            initial = new CampaignArmyRegistry(graph, new[] {
                Placement("a", "castilla", start), Placement("b", "castilla", ally),
                Placement("enemy", "otro", enemy) });
            campaign.Initialize(panel, map, null, initial, "castilla");
            yield return null;
        }

        private static CampaignArmyPlacement Placement(string id, string faction, HexCoordinates at)
        {
            var p = TroopCatalog.Get(TroopProfile.Spearmen);
            return new CampaignArmyPlacement(new CampaignArmyState(id, faction, new[] {
                new CampaignFormationState(id + "-lanzas", p.Attributes, 100, 70, p.Kind) }), at, 6);
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            if (hadSave) PlayerPrefs.SetString(SaveKey, previousSave); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene);
        }

        [UnityTest] public IEnumerator HudRoundTripRestoresSelectionSpentMovementCityEconomyAndWork()
        {
            Assert.That(panel.SaveButton.interactable && panel.LoadButton.interactable, Is.True);
            var town = settlements.Registry.Settlements.Single().Id;
            campaign.CompleteTurn(); Assert.That(campaign.TryConstruct(town, "market"), Is.True);
            campaign.CompleteTurn(); campaign.CompleteTurn();
            Assert.That(campaign.TryConstruct(town, "well"), Is.True);
            selection.LocateCell(destination); campaign.MoveToSelectedCell();
            campaign.ArmyRegistry.TryGetArmy("a", out var moved);
            Assert.That(moved.Movement.Position, Is.EqualTo(destination));
            Assert.That(moved.Movement.RemainingMovement, Is.LessThan(6));
            campaign.NextArmy(); Assert.That(campaign.SelectedArmyId, Is.EqualTo("b"));
            var savedState = campaign.State; int due = campaign.CityState(town).CompletionTurn;
            panel.SaveButton.onClick.Invoke();
            var payload = JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            Assert.That(payload.version, Is.EqualTo(9));
            Assert.That(payload.settlementOwners.Length, Is.EqualTo(1));
            Assert.That(payload.armyRegistry.armies.Length, Is.EqualTo(3));
            campaign.CompleteTurn(); campaign.CompleteTurn(); campaign.NextArmy();
            for (int repeat = 0; repeat < 2; repeat++)
            {
                panel.LoadButton.onClick.Invoke(); yield return null;
                Assert.That(campaign.State.Turn, Is.EqualTo(savedState.Turn));
                Assert.That(campaign.State.Resources, Is.EqualTo(savedState.Resources));
                Assert.That(campaign.State.IncomePerTurn, Is.EqualTo(savedState.IncomePerTurn));
                Assert.That(campaign.State.ExpensesPerTurn, Is.EqualTo(savedState.ExpensesPerTurn));
                Assert.That(campaign.SelectedArmyId, Is.EqualTo("b"));
                campaign.ArmyRegistry.TryGetArmy("a", out var restored);
                Assert.That(restored.Movement.Position, Is.EqualTo(destination));
                Assert.That(restored.Movement.RemainingMovement, Is.EqualTo(moved.Movement.RemainingMovement));
                Assert.That(campaign.CityState(town).Has("market"), Is.True);
                Assert.That(campaign.CityState(town).PendingId, Is.EqualTo("well"));
                Assert.That(campaign.CityState(town).CompletionTurn, Is.EqualTo(due));
                Assert.That(campaign.GetComponent<CampaignArmyMarkerLayer>()
                    .GetComponentsInChildren<CampaignArmyMarker>().Count(m => m.IsVisible), Is.EqualTo(3));
            }
            var forecast = campaign.State.ProjectedResources;
            campaign.CompleteTurn(); Assert.That(campaign.State.Resources, Is.EqualTo(forecast));
        }

        [UnityTest] public IEnumerator BadArmyBadCityAndLegacyModeDoNotReplaceAnyLiveState()
        {
            campaign.CompleteTurn(); campaign.SaveCampaign();
            var valid = PlayerPrefs.GetString(SaveKey);
            campaign.CompleteTurn();
            var state = campaign.State; var registry = campaign.ArmyRegistry; int revision = campaign.CityRevision;
            foreach (var kind in new[] { "army", "city", "legacy", "context", "victory" })
            {
                var data = JsonUtility.FromJson<CampaignSaveData>(valid);
                if (kind == "army") data.armyRegistry.armies[0].remainingMovement = 999;
                if (kind == "city") data.cities = new[] { new CityDevelopmentData {
                    settlementId = "unknown", completed = new[] { "market" } } };
                if (kind == "legacy") data.version = 3;
                if (kind == "context") data.context = "other";
                if (kind == "victory") data.missionWon = true;
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
                LogAssert.Expect(LogType.Warning, new Regex("No se pudo cargar:"));
                campaign.LoadCampaign();
                Assert.That(campaign.State, Is.SameAs(state));
                Assert.That(campaign.ArmyRegistry, Is.SameAs(registry));
                Assert.That(campaign.CityRevision, Is.EqualTo(revision));
            }
            yield return null;
        }

        [UnityTest] public IEnumerator VictorySurvivesWhenAnotherArmyWasSelectedAtSaveTime()
        {
            var mission = ReachDestinationMission.Create(map, selection.Routes, destination, start);
            campaign.Initialize(panel, map, mission, initial, "castilla");
            selection.LocateCell(destination); campaign.MoveToSelectedCell();
            Assert.That(campaign.Mission.IsWon, Is.True);
            campaign.NextArmy(); Assert.That(campaign.SelectedArmyId, Is.EqualTo("b"));
            campaign.SaveCampaign();
            campaign.Initialize(panel, map, mission, initial, "castilla");
            campaign.LoadCampaign(); yield return null;
            Assert.That(campaign.Mission.IsWon, Is.True);
            Assert.That(campaign.SelectedArmyId, Is.EqualTo("b"));
            var state = campaign.State; campaign.CompleteTurn(); Assert.That(campaign.State, Is.SameAs(state));
        }

        [UnityTest] public IEnumerator ActiveBattleCannotOverwriteSaveOrBeReplacedByLoad()
        {
            campaign.SaveCampaign(); var saved = PlayerPrefs.GetString(SaveKey);
            var fighting = new CampaignArmyRegistry(selection.Routes, new[] {
                Placement("a", "castilla", start), Placement("enemy", "otro", destination) })
                .BeginEncounter("battle", "a", "enemy", 42);
            // The tactical UI is not connected yet; inject the same registry it will publish.
            typeof(CampaignTurnController).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(campaign, fighting);
            campaign.SaveCampaign(); campaign.LoadCampaign();
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(saved));
            Assert.That(campaign.ArmyRegistry, Is.SameAs(fighting));
            Assert.That(panel.SaveButton.interactable || panel.LoadButton.interactable, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator EmptySurvivorRegistryCanBeSavedAndReloadedWithoutRespawningArmies()
        {
            var empty = new CampaignArmyRegistry(selection.Routes, new CampaignArmyPlacement[0]);
            typeof(CampaignTurnController).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(campaign, empty);
            selection.ClearSelection();
            Assert.That(campaign.Army, Is.Null); Assert.That(panel.SaveButton.interactable, Is.True);
            campaign.SaveCampaign(); campaign.Initialize(panel, map, null, initial, "castilla");
            campaign.LoadCampaign(); yield return null;
            Assert.That(campaign.ArmyRegistry.Armies, Is.Empty);
            Assert.That(campaign.Army, Is.Null);
            Assert.That(panel.LoadButton.interactable, Is.True);
            Assert.That(campaign.GetComponent<CampaignArmyMarkerLayer>()
                .GetComponentsInChildren<CampaignArmyMarker>().Count(m => m.IsVisible), Is.Zero);
        }
    }
}
