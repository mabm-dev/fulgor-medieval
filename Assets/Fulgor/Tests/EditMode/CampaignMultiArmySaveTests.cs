using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignMultiArmySaveTests
    {
        private static HexCoordinates At(int q, int r = 0) => new HexCoordinates(q, r);
        private static MapRouteGraph Graph()
        {
            var cells = new List<MapCell>();
            for (int r = 0; r < 2; r++) for (int q = 0; q < 6; q++)
                cells.Add(new MapCell(At(q, r), TerrainType.Plain, false));
            return new MapRouteGraph(new GeneratedMap(6, 2, 9, cells.ToArray()));
        }
        private static CampaignArmyState Army(string id, string faction, int quantity = 20)
        {
            var profile = TroopCatalog.Get(TroopProfile.Spearmen);
            return new CampaignArmyState(id, faction, new[] {
                new CampaignFormationState(id + "-lanzas", profile.Attributes,
                    (long)quantity * profile.Attributes.HealthPerMember, 70, profile.Kind) });
        }
        private static CampaignArmyRegistry Initial(bool reverse = false, int strength = 20, int movement = 6)
        {
            var placements = new[] {
                new CampaignArmyPlacement(Army("a", "castilla", strength), At(0), movement),
                new CampaignArmyPlacement(Army("b", "castilla"), At(0, 1), 6),
                new CampaignArmyPlacement(Army("enemy", "otro"), At(4), 6) };
            return new CampaignArmyRegistry(Graph(), reverse ? placements.Reverse() : placements);
        }
        private static CampaignSaveData Payload()
        {
            var registry = Initial();
            Assert.That(registry.TryMove("a", At(2), out registry, out _, out _), Is.True);
            return CampaignSaveData.CaptureMultiple("scenario", new CampaignTurnState(4,
                new CampaignResources(30, 40, 50, 60, 70), default), registry, "b", "castilla",
                new[] { new CityDevelopmentData { settlementId = "villa", completed = new[] { "market" },
                    pendingId = "well", completionTurn = 5 } });
        }

        [Test] public void RoundTripPreservesEveryArmyMovementResourcesAndPendingCity()
        {
            var data = Payload();
            data.RestoreMultiple("scenario", Initial(), "castilla", new CampaignResources(1, 2, 3, 4, 5), out var state, out var registry);
            Assert.That(registry.Armies.Count, Is.EqualTo(3));
            Assert.That(registry.TryGetArmy("a", out var army), Is.True);
            Assert.That(army.Movement.Position, Is.EqualTo(At(2)));
            Assert.That(army.Movement.RemainingMovement, Is.EqualTo(4));
            Assert.That(army.Army.Formations[0].Health, Is.EqualTo(200));
            Assert.That(data.selectedArmyId, Is.EqualTo("b"));
            Assert.That(state.Turn, Is.EqualTo(4));
            Assert.That(state.Resources, Is.EqualTo(new CampaignResources(30, 40, 50, 60, 70)));
            Assert.That(state.CompleteTurn().Resources.Gold, Is.EqualTo(64));
            var city = data.RestoreCities(new[] { "villa" })["villa"];
            Assert.That(city.Has("market"), Is.True);
            Assert.That(city.PendingId, Is.EqualTo("well"));
            Assert.That(city.CompletionTurn, Is.EqualTo(5));
        }

        [Test] public void WoundsMoraleAndEliminatedArmiesAreNotResetOnLoad()
        {
            var data = Payload();
            data.armyRegistry.armies[0].formations[0].health = 73;
            data.armyRegistry.armies[0].formations[0].morale = 31;
            data.armyRegistry.armies = data.armyRegistry.armies.Where(a => a.id != "enemy").ToArray();
            data.RestoreMultiple("scenario", Initial(), "castilla", default, out _, out var restored);
            restored.TryGetArmy("a", out var survivor);
            Assert.That(survivor.Army.Formations[0].Quantity, Is.EqualTo(8));
            Assert.That(survivor.Army.Formations[0].Health, Is.EqualTo(73));
            Assert.That(survivor.Army.Formations[0].Morale, Is.EqualTo(31));
            Assert.That(restored.TryGetArmy("enemy", out _), Is.False);
            Assert.That(restored.BeginTurn().TryGetArmy("a", out var refreshed), Is.True);
            Assert.That(refreshed.Movement.RemainingMovement, Is.EqualTo(6));
            Assert.That(refreshed.Army.Formations[0].Health, Is.EqualTo(73));
        }

        [Test] public void LastArmyLostStillAllowsSavingAndLoadingCampaign()
        {
            var empty = new CampaignArmyRegistry(Graph(), Array.Empty<CampaignArmyPlacement>());
            var data = CampaignSaveData.CaptureMultiple("scenario", new CampaignTurnState(5, default, default), empty, null, "castilla");
            data.RestoreMultiple("scenario", Initial(), "castilla", default, out var state, out var restored);
            Assert.That(restored.Armies, Is.Empty);
            Assert.That(state.Turn, Is.EqualTo(5));
        }

        [Test] public void LiveEncounterCannotBeSavedAndResolvedHistorySurvivesLoading()
        {
            var initial = Initial();
            initial.TryMove("a", At(3), out var moved, out _, out _);
            var fighting = moved.BeginEncounter("battle-1", "a", "enemy", 42);
            Assert.That(fighting.HasActiveEncounters, Is.True);
            Assert.Throws<InvalidOperationException>(() => fighting.Capture());
            Assert.That(fighting.TryGetEncounter("battle-1", out var encounter), Is.True);
            Assert.That(encounter.Battle.Roster.TryWithdrawArmy(BattleSide.Attacker), Is.True);
            var ended = fighting.ResolveEncounter("battle-1");
            var restored = CampaignArmyRegistry.Restore(ended.Capture(), initial);
            Assert.That(restored.HasActiveEncounters, Is.False);
            Assert.That(restored.HasResolvedEncounter("battle-1"), Is.True);
            foreach (var id in new[] { "a", "enemy" })
            {
                restored.TryGetArmy(id, out var army);
                Assert.That(army.Army.HasAppliedEncounter("battle-1"), Is.True);
            }
            Assert.That(restored.ResolveEncounter("battle-1"), Is.SameAs(restored));
            Assert.Throws<ArgumentException>(() => restored.BeginEncounter("battle-1", "a", "enemy", 42));
        }

        [TestCase("unknownArmy")][TestCase("duplicateArmy")][TestCase("occupied")]
        [TestCase("offMap")][TestCase("negativeMovement")][TestCase("excessMovement")]
        [TestCase("unknownFormation")][TestCase("duplicateFormation")][TestCase("noFormation")]
        [TestCase("deadFormation")][TestCase("excessHealth")][TestCase("invalidMorale")]
        [TestCase("unknownHistory")][TestCase("duplicateHistory")][TestCase("missingRegistry")]
        [TestCase("enemySelection")][TestCase("missingSelection")][TestCase("pendingMarch")]
        [TestCase("negativeResources")][TestCase("invalidTurn")]
        public void CorruptPayloadIsRejectedWithoutPublishingPartialState(string damage)
        {
            var data = Payload(); var a = data.armyRegistry.armies[0];
            switch (damage)
            {
                case "unknownArmy": a.id = "intruder"; break;
                case "duplicateArmy": data.armyRegistry.armies[1] = a; break;
                case "occupied": a.q = 0; a.r = 1; break;
                case "offMap": a.q = 99; break;
                case "negativeMovement": a.remainingMovement = -1; break;
                case "excessMovement": a.remainingMovement = 7; break;
                case "unknownFormation": a.formations[0].id = "wrong"; break;
                case "duplicateFormation": a.formations = new[] { a.formations[0], a.formations[0] }; break;
                case "noFormation": a.formations = Array.Empty<CampaignFormationData>(); break;
                case "deadFormation": a.formations[0].health = 0; break;
                case "excessHealth": a.formations[0].health = 201; break;
                case "invalidMorale": a.formations[0].morale = 101; break;
                case "unknownHistory": a.appliedEncounters = new[] { "unknown" }; break;
                case "duplicateHistory": data.armyRegistry.resolvedEncounters = new[] { "x", "x" }; break;
                case "missingRegistry": data.armyRegistry = null; break;
                case "enemySelection": data.selectedArmyId = "enemy"; break;
                case "missingSelection": data.selectedArmyId = null; break;
                case "pendingMarch": data.hasMarchOrder = true; break;
                case "negativeResources": data.gold = -1; break;
                case "invalidTurn": data.turn = 0; break;
            }
            CampaignTurnState state = null; CampaignArmyRegistry registry = null;
            Assert.Catch<ArgumentException>(() => data.RestoreMultiple("scenario", Initial(), "castilla", default, out state, out registry));
            Assert.That(state, Is.Null); Assert.That(registry, Is.Null);
        }

        [Test] public void ModeVersionAndDeploymentMismatchAreRejected()
        {
            var data = Payload();
            Assert.Throws<ArgumentException>(() => data.RestoreMultiple("different", Initial(), "castilla", default, out _, out _));
            Assert.Throws<ArgumentException>(() => data.Restore("scenario", Graph(), At(0), default, 6, out _, out _));
            data.version = 3;
            Assert.Throws<ArgumentException>(() => data.RestoreMultiple("scenario", Initial(), "castilla", default, out _, out _));
        }

        [Test] public void SignatureIncludesFactionDeploymentStrengthAndMovementButNotOrdering()
        {
            var signature = CampaignSaveData.ArmyScenarioSignature("map", Initial(), "castilla");
            Assert.That(CampaignSaveData.ArmyScenarioSignature("map", Initial(true), "castilla"), Is.EqualTo(signature));
            Assert.That(CampaignSaveData.ArmyScenarioSignature("map", Initial(), "otro"), Is.Not.EqualTo(signature));
            Assert.That(CampaignSaveData.ArmyScenarioSignature("map", Initial(strength: 21), "castilla"), Is.Not.EqualTo(signature));
            Assert.That(CampaignSaveData.ArmyScenarioSignature("map", Initial(movement: 5), "castilla"), Is.Not.EqualTo(signature));
            Assert.That(CampaignSaveData.ArmyScenarioSignature("another-map", Initial(), "castilla"), Is.Not.EqualTo(signature));
        }

        [Test] public void CaptureIsDetachedFromLiveRegistry()
        {
            var initial = Initial(); var data = initial.Capture();
            data.armies[0].formations[0].health = 1; data.armies[0].q = 3;
            initial.TryGetArmy("a", out var unchanged);
            Assert.That(unchanged.Army.Formations[0].Health, Is.EqualTo(200));
            Assert.That(unchanged.Movement.Position, Is.EqualTo(At(0)));
        }
    }
}
