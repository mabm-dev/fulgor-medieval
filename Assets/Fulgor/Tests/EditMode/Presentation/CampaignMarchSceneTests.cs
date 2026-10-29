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
    public sealed class CampaignMarchSceneTests
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

        private HexCoordinates FarDestination(string armyId="a")=>map.Cells.Select(c=>c.Coordinates).First(target=>
            campaign.Military.TryPlanMarch(campaign.ArmyRegistry,armyId,target,out var route,out _)&&route.TotalCost>12);
        private void Issue(HexCoordinates target)
        {
            selection.LocateCell(target);Assert.That(panel.MoveArmyButton.interactable,Is.True);panel.MoveArmyButton.onClick.Invoke();
        }
        [UnityTest]public IEnumerator HudMovesPartiallyAndResumesToDestinationAcrossTurns()
        {
            var target=FarDestination();var stock=campaign.State.Resources;Issue(target);
            Assert.That(campaign.MarchFor("a"),Is.EqualTo(target));Assert.That(campaign.Army.Position,Is.Not.EqualTo(start));Assert.That(campaign.Army.Position,Is.Not.EqualTo(target));
            Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(panel.CancelMarchButton.interactable,Is.True);
            for(int i=0;i<100&&campaign.MarchFor("a").HasValue;i++)campaign.CompleteTurn();
            campaign.ArmyRegistry.TryGetArmy("a",out var arrived);Assert.That(arrived.Movement.Position,Is.EqualTo(target));Assert.That(campaign.MarchFor("a"),Is.Null);Assert.That(campaign.HasPendingBattle,Is.False);yield return null;
        }
        [UnityTest]public IEnumerator EachArmyKeepsOwnOrderAndCancelOnlyAffectsSelectedArmy()
        {
            var first=FarDestination();Issue(first);campaign.NextArmy();Assert.That(campaign.SelectedArmyId,Is.EqualTo("b"));
            var second=FarDestination("b");Issue(second);Assert.That(campaign.MarchOrders.Count,Is.EqualTo(2));
            panel.CancelMarchButton.onClick.Invoke();Assert.That(campaign.MarchFor("b"),Is.Null);Assert.That(campaign.MarchFor("a"),Is.EqualTo(first));
            campaign.NextArmy();Assert.That(campaign.SelectedArmyId,Is.EqualTo("a"));Assert.That(campaign.MarchDestination,Is.EqualTo(first));yield return null;
        }
        [UnityTest]public IEnumerator V7SavesOrdersWithoutExecutingThemOnLoadAndRejectsDamageAtomically()
        {
            var target=FarDestination();Issue(target);campaign.SaveCampaign();var before=campaign.Army;var stock=campaign.State;
            var json=PlayerPrefs.GetString(SaveKey);var data=JsonUtility.FromJson<CampaignSaveData>(json);Assert.That(data.version,Is.EqualTo(9));
            campaign.CompleteTurn();campaign.LoadCampaign();yield return null;
            Assert.That(campaign.Army.Position,Is.EqualTo(before.Position));Assert.That(campaign.Army.RemainingMovement,Is.EqualTo(before.RemainingMovement));
            Assert.That(campaign.State.Resources,Is.EqualTo(stock.Resources));Assert.That(campaign.MarchFor("a"),Is.EqualTo(target));
            var state=campaign.State;var registry=campaign.ArmyRegistry;var orders=campaign.MarchOrders;
            foreach(var kind in new[]{"unknown","foreign","duplicate","destination","missing"})
            {
                data=JsonUtility.FromJson<CampaignSaveData>(json);
                if(kind=="unknown")data.marchOrders[0].armyId="invented";
                if(kind=="foreign")data.marchOrders[0].armyId="enemy";
                if(kind=="duplicate")data.marchOrders=new[]{data.marchOrders[0],data.marchOrders[0]};
                if(kind=="destination")data.marchOrders[0].q=999;if(kind=="missing")data.marchOrders=null;
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));campaign.LoadCampaign();
                Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(registry));Assert.That(campaign.MarchOrders,Is.SameAs(orders));
            }
        }
        [UnityTest]public IEnumerator LegacyV6LoadsWithoutOrdersAndNewGameDropsThem()
        {
            var target=FarDestination();Issue(target);campaign.SaveCampaign();var data=JsonUtility.FromJson<CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            data.version=6;data.marchOrders=null;
            data.context=CampaignSaveData.MilitaryScenarioSignature(CampaignSaveData.SettlementScenarioSignature(
                CampaignSaveData.ArmyScenarioSignature(CampaignSaveData.ScenarioSignature(map,selection.Routes,null),initial,"castilla"),campaign.SettlementState));
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));campaign.LoadCampaign();Assert.That(campaign.MarchOrders.Count,Is.Zero);
            Issue(target);Assert.That(campaign.MarchOrders.Count,Is.EqualTo(1));campaign.NewGame();yield return null;yield return null;Assert.That(campaign.MarchOrders.Count,Is.Zero);
        }
    }
}
