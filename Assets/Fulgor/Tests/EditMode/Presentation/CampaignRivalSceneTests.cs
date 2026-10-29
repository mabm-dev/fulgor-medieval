using System.Collections;
using System.Linq;
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
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignRivalSceneTests
    {
        private const string SaveKey="Fulgor.Campaign.Prototype.Save.v1",TargetId="rival-fort";
        private bool hadSave;private string oldSave,oldScene;
        private CampaignTurnController campaign;private CampaignSettlementController towns;
        private CampaignBoardSelection selection;private CampaignSelectionPanel panel;
        private CampaignSettlementDefinition homeDefinition,rivalDefinition;
        private GeneratedMap map;private CampaignArmyRegistry initial;
        private HexCoordinates home,target,ally;
        private CityManagementView City=>campaign.GetComponent<CityManagementView>();
        private BattleLabController Battle=>campaign.GetComponent<BattleLabController>();

        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(SaveKey);oldSave=PlayerPrefs.GetString(SaveKey,"");oldScene=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            campaign=Object.FindAnyObjectByType<CampaignTurnController>();towns=campaign.GetComponent<CampaignSettlementController>();
            selection=campaign.GetComponent<CampaignBoardSelection>();panel=campaign.GetComponent<CampaignSelectionPanel>();map=ProceduralMapGenerator.Generate(24,16,12345);
            home=towns.Registry.Settlements.Single().Position;
            target=home.Neighbors().First(n=>selection.Routes.CanStandAt(n));
            ally=home.Neighbors().First(n=>n!=target&&selection.Routes.CanStandAt(n));
            towns.enabled=false;var config=new SerializedObject(towns);var definitions=config.FindProperty("settlements");
            homeDefinition=Object.Instantiate((CampaignSettlementDefinition)definitions.GetArrayElementAtIndex(0).objectReferenceValue);
            rivalDefinition=Object.Instantiate(homeDefinition);
            Configure(homeDefinition,"home-fort","Castilla","castilla","Castilla",home);
            Configure(rivalDefinition,TargetId,"Villa rival","rival","Señorío rival",target);
            definitions.arraySize=2;definitions.GetArrayElementAtIndex(0).objectReferenceValue=homeDefinition;definitions.GetArrayElementAtIndex(1).objectReferenceValue=rivalDefinition;
            config.ApplyModifiedPropertiesWithoutUndo();towns.enabled=true;yield return null;yield return null;
            Reinitialize(false);yield return null;
        }
        private static void Configure(CampaignSettlementDefinition definition,string id,string name,string faction,string owner,HexCoordinates at)
        {
            var s=new SerializedObject(definition);s.FindProperty("id").stringValue=id;s.FindProperty("displayName").stringValue=name;
            s.FindProperty("ownerFactionId").stringValue=faction;s.FindProperty("ownerDisplayName").stringValue=owner;
            s.FindProperty("position").vector2IntValue=new Vector2Int(at.Q,at.R);s.ApplyModifiedPropertiesWithoutUndo();
        }
        private static CampaignArmyPlacement Placement(string id,string faction,HexCoordinates at,long health)
        {
            var p=TroopCatalog.Get(TroopProfile.Spearmen);
            return new CampaignArmyPlacement(new CampaignArmyState(id,faction,new[]{new CampaignFormationState(id+"-troops",p.Attributes,health,70)}),at,20);
        }
        private void Reinitialize(bool defended)
        {
            var list=new System.Collections.Generic.List<CampaignArmyPlacement>{Placement("own","castilla",home,500),Placement("own-2","castilla",ally,100)};
            if(defended)list.Add(Placement("enemy","rival",target,20));
            initial=new CampaignArmyRegistry(selection.Routes,list);campaign.Initialize(panel,map,null,initial,"castilla");selection.ClearSelection();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(hadSave)PlayerPrefs.SetString(SaveKey,oldSave);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
            if(homeDefinition!=null)Object.DestroyImmediate(homeDefinition);if(rivalDefinition!=null)Object.DestroyImmediate(rivalDefinition);
            if(!string.IsNullOrEmpty(oldScene))EditorSceneManager.OpenScene(oldScene);
        }
        private void Enable(long enemyHealth=600,long ownHealth=100,bool hasEnemy=true,HexCoordinates? reserve=null)
        {
            var placements=new System.Collections.Generic.List<CampaignArmyPlacement>{Placement("own","castilla",home,ownHealth)};
            if(hasEnemy)placements.Add(Placement("a-enemy","rival",target,enemyHealth));
            if(reserve.HasValue)placements.Add(Placement("z-enemy","rival",reserve.Value,100));
            initial=new CampaignArmyRegistry(selection.Routes,placements);campaign.Initialize(panel,map,null,initial,"castilla",true);selection.ClearSelection();
        }
        [UnityTest]public IEnumerator RivalAttackClosesCityAndPlayerControlsDefenderWithoutRepeatingTurn()
        {
            Enable(200,200);Assert.That(towns.OpenCityAt(home),Is.True);City.CompleteTurn();
            Assert.That(City.IsOpen,Is.False);Assert.That(campaign.HasPendingBattle,Is.True);Assert.That(campaign.IsRivalPhaseActive,Is.True);
            Assert.That(Battle.PlayerBattleSide,Is.EqualTo(BattleSide.Defender));Assert.That(Battle.IsOpen,Is.True);Assert.That(panel.SaveButton.interactable,Is.False);
            var state=campaign.State;var treasury=campaign.Rivals.Resources("rival");campaign.CompleteTurn();Assert.That(campaign.State,Is.SameAs(state));
            Assert.That(campaign.TryRecruit("home-fort",TroopProfile.Spearmen),Is.False);Assert.That(campaign.TryConstruct("home-fort","well"),Is.False);
            var encounter=Battle.ActiveCampaignEncounter;
            if(encounter.Roster.ActiveSide==BattleSide.Attacker)
                Assert.That(Battle.TryCampaignOrder(new BattleRecordedOrder{kind=BattleRecordedOrderKind.Defend,actorId=encounter.Roster.ActiveFormationId.Value},out _),Is.False);
            for(int i=0;i<100&&encounter.Roster.Outcome==BattleOutcome.InProgress&&encounter.Roster.ActiveSide!=BattleSide.Defender;i++)Battle.AdvanceCampaignAutomaticStep();
            Assert.That(encounter.Roster.ActiveSide,Is.EqualTo(BattleSide.Defender));
            Assert.That(Battle.TryCampaignOrder(new BattleRecordedOrder{kind=BattleRecordedOrderKind.Defend,actorId=encounter.Roster.ActiveFormationId.Value},out _),Is.True);
            Battle.WithdrawPlayerArmy();Assert.That(Battle.ReturnToCampaign(),Is.True);yield return null;
            Assert.That(campaign.State.Turn,Is.EqualTo(2));Assert.That(campaign.Rivals.Resources("rival"),Is.EqualTo(treasury));Assert.That(campaign.IsRivalPhaseActive,Is.False);
            campaign.SettlementState.TryGet("home-fort",out var city);Assert.That(city.OwnerFactionId,Is.EqualTo("castilla"));Assert.That(panel.EndTurnButton.interactable,Is.True);
        }
        [UnityTest]public IEnumerator WinningRivalOccupiesPlayerFortAndChangesIncomeWithoutTakingStock()
        {
            Enable(5000,100);campaign.CompleteTurn();var stock=campaign.State.Resources;var encounter=Battle.ActiveCampaignEncounter;
            for(int i=0;i<1000&&encounter.Roster.Outcome==BattleOutcome.InProgress;i++)Battle.AdvanceCampaignAutomaticStep();
            Assert.That(encounter.Roster.Outcome,Is.EqualTo(BattleOutcome.AttackerVictory));Assert.That(Battle.ReturnToCampaign(),Is.True);yield return null;
            campaign.SettlementState.TryGet("home-fort",out var city);Assert.That(city.OwnerFactionId,Is.EqualTo("rival"));Assert.That(towns.CanManage("home-fort"),Is.False);
            Assert.That(campaign.State.Resources,Is.EqualTo(stock));Assert.That(campaign.TerritoryIncome,Is.EqualTo(default(CampaignResources)));
            campaign.ArmyRegistry.TryGetArmy("a-enemy",out var enemy);Assert.That(enemy.Movement.Position,Is.EqualTo(home));Assert.That(enemy.Movement.RemainingMovement,Is.Zero);
            Assert.That(campaign.ArmyRegistry.TryGetArmy("own",out _),Is.False);campaign.SaveCampaign();var before=campaign.Rivals.Resources("rival");campaign.LoadCampaign();
            Assert.That(campaign.Rivals.Resources("rival"),Is.EqualTo(before));Assert.That(campaign.ArmyRegistry.HasResolvedEncounter(encounter.Id),Is.True);Assert.That(campaign.State.Turn,Is.EqualTo(2));
        }
        [UnityTest]public IEnumerator GarrisonOnlyDefenseUsesPlayerSideAndSurvivingRetreatBlocksOccupation()
        {
            Enable();Assert.That(campaign.TryLeaveGarrison("home-fort","own-troops"),Is.True);campaign.CompleteTurn();
            Assert.That(Battle.PlayerBattleSide,Is.EqualTo(BattleSide.Defender));Assert.That(Battle.WithdrawPlayerArmy(),Is.True);Battle.ReturnToCampaign();yield return null;
            Assert.That(campaign.Military.Garrison("home-fort").Formations.Single().Id,Is.EqualTo("own-troops"));
            Assert.That(campaign.ArmyRegistry.TryGetArmy("guard/home-fort",out _),Is.False);campaign.SettlementState.TryGet("home-fort",out var city);Assert.That(city.OwnerFactionId,Is.EqualTo("castilla"));
        }
        [UnityTest]public IEnumerator RivalRecruitmentPaysOrdinaryCostsWaitsAndMustersWithoutFreeMovement()
        {
            map=new GeneratedMap(map.Width,map.Height,map.Seed,map.Cells.Select(c=>new MapCell(c.Coordinates,c.Terrain,true)).ToArray());Enable(ownHealth:100000,hasEnemy:false);
            int previousRecords=0;bool raised=false;
            for(int i=0;i<40&&!raised;i++)
            {
                var expected=campaign.Rivals.Resources("rival").Add(CampaignIncomeCalculator.ForFaction(map,campaign.SettlementState,"rival"));campaign.CompleteTurn();
                Assert.That(campaign.HasPendingBattle,Is.False);var records=campaign.Military.Capture().recruits;
                foreach(var record in records.Skip(previousRecords))
                {
                    Assert.That(record.factionId,Is.EqualTo("rival"));Assert.That(record.startTurn,Is.EqualTo(campaign.State.Turn));Assert.That(record.completionTurn,Is.GreaterThan(campaign.State.Turn));
                    Assert.That(expected.TrySpend(RecruitmentCatalog.Get(record.profile).Cost,out expected),Is.True);
                }
                Assert.That(campaign.Rivals.Resources("rival"),Is.EqualTo(expected));previousRecords=records.Length;
                var army=campaign.ArmyRegistry.Armies.FirstOrDefault(a=>a.Army.FactionId=="rival");
                if(army!=null){raised=true;Assert.That(army.Army.Id,Does.StartWith("muster/"));Assert.That(army.Movement.RemainingMovement,Is.Zero);Assert.That(army.Movement.Position,Is.EqualTo(target));}
            }
            Assert.That(raised,Is.True);Assert.That(previousRecords,Is.GreaterThan(0));yield return null;
        }
        [UnityTest]public IEnumerator PhaseResumesWithNextArmyAfterDefenseAndDoesNotMoveItTwice()
        {
            var far=map.Cells.First(c=>selection.Routes.CanStandAt(c.Coordinates)&&c.Coordinates.DistanceTo(home)>4).Coordinates;Enable(reserve:far);campaign.CompleteTurn();
            campaign.ArmyRegistry.TryGetArmy("z-enemy",out var waiting);Assert.That(waiting.Movement.Position,Is.EqualTo(far));var stock=campaign.Rivals.Resources("rival");
            Battle.WithdrawPlayerArmy();Battle.ReturnToCampaign();campaign.ArmyRegistry.TryGetArmy("z-enemy",out var moved);
            Assert.That(moved.Movement.Position,Is.Not.EqualTo(far));Assert.That(campaign.State.Turn,Is.EqualTo(2));Assert.That(campaign.Rivals.Resources("rival"),Is.EqualTo(stock));Assert.That(campaign.IsRivalPhaseActive,Is.False);
            Battle.ReturnToCampaign();campaign.ArmyRegistry.TryGetArmy("z-enemy",out var again);Assert.That(again.Movement.Position,Is.EqualTo(moved.Movement.Position));yield return null;
        }
        [UnityTest]public IEnumerator V8RestoresRivalTreasuryAndRejectsCorruptionBeforePublishing()
        {
            Enable(100,100000);campaign.CompleteTurn();Assert.That(campaign.HasPendingBattle,Is.False);campaign.SaveCampaign();string json=PlayerPrefs.GetString(SaveKey);
            var data=JsonUtility.FromJson<CampaignSaveData>(json);Assert.That(data.version,Is.EqualTo(9));var stock=campaign.Rivals.Resources("rival");
            campaign.LoadCampaign();Assert.That(campaign.Rivals.Resources("rival"),Is.EqualTo(stock));Assert.That(campaign.State.Turn,Is.EqualTo(2));
            var state=campaign.State;var armies=campaign.ArmyRegistry;var rival=campaign.Rivals;
            foreach(var damage in new[]{"turn","mode","missing","duplicate","negative"})
            {
                data=JsonUtility.FromJson<CampaignSaveData>(json);if(damage=="turn")data.rivals.closedTurn++;if(damage=="mode")data.rivals.enabled=false;
                if(damage=="missing")data.rivals.factions=null;if(damage=="duplicate")data.rivals.factions=new[]{data.rivals.factions[0],data.rivals.factions[0]};if(damage=="negative")data.rivals.factions[0].gold=-1;
                PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar:"));campaign.LoadCampaign();
                Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.ArmyRegistry,Is.SameAs(armies));Assert.That(campaign.Rivals,Is.SameAs(rival));
            }
            yield return null;
        }
    }
}
