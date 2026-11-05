using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Fulgor.Tests.EditMode
{
    public sealed class CityTileConstructionTests
    {
        private static readonly HexCoordinates City = new(4, 4);

        [Test]
        public void PlacementRequiresAnUnoccupiedFirstRingAndMatchingTerrain()
        {
            var plain = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var forest = new MapCell(new HexCoordinates(4, 5), TerrainType.Forest, false);
            Assert.IsTrue(CityTileConstruction.CanPlace(City, plain, "farm", false));
            Assert.IsFalse(CityTileConstruction.CanPlace(City, plain, "farm", true));
            Assert.IsFalse(CityTileConstruction.CanPlace(City, plain, "lumber", false));
            Assert.IsTrue(CityTileConstruction.CanPlace(City, forest, "lumber", false));
            Assert.IsFalse(CityTileConstruction.CanPlace(City, new MapCell(City, TerrainType.Plain, false), "house", false));
            Assert.IsFalse(CityTileConstruction.CanPlace(City, new MapCell(new HexCoordinates(6, 4), TerrainType.Plain, false), "house", false));
        }

        [Test]
        public void PreviewSeparatesPassiveYieldFromBuildingYield()
        {
            Assert.AreEqual(1, CityTileConstruction.Passive(TerrainType.Plain).Grain);
            Assert.AreEqual(3, CityTileConstruction.Active("farm").Grain);
            Assert.AreEqual(1, CityTileConstruction.Passive(TerrainType.Forest).Wood);
            Assert.AreEqual(3, CityTileConstruction.Active("lumber").Wood);
        }

        [Test]
        public void MountainsHaveNoYieldEvenWithALegacyGoldFlag()
        {
            var cell=new MapCell(new HexCoordinates(5,4),TerrainType.Mountain,true);
            Assert.AreEqual(default(CampaignResources),CityTileConstruction.Passive(cell));
            var map=new GeneratedMap(1,1,1,new[]{cell});
            Assert.AreEqual(default(CampaignResources),CampaignIncomeCalculator.FromMap(map));
            Assert.IsFalse(CityTileConstruction.CanPlace(City,cell,"house",false));
        }

        [Test]
        public void QueueAdvancesInOrderAndRejectsDuplicateTiles()
        {
            var queue = new CityTileQueue();
            var a = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var b = new MapCell(new HexCoordinates(4, 5), TerrainType.Forest, false);
            Assert.IsTrue(queue.TryAdd(City, a, "farm", false));
            Assert.IsFalse(queue.TryAdd(City, a, "house", false));
            Assert.IsTrue(queue.TryAdd(City, b, "lumber", false));
            Assert.IsNull(queue.Advance());
            Assert.AreEqual(1, queue.Capture()[0].turnsRemaining);
            Assert.AreEqual(2, queue.Capture()[1].turnsRemaining);
            Assert.AreEqual("farm", queue.Advance().project);
            Assert.AreEqual(1, queue.Count);
            Assert.IsTrue(queue.Cancel(b.Coordinates));
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void QueueSnapshotDoesNotMutateLiveStateAndRestoreRejectsDuplicates()
        {
            var queue = new CityTileQueue();
            var cell = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var map = new Dictionary<HexCoordinates, MapCell> { [cell.Coordinates] = cell };
            Assert.IsTrue(queue.TryAdd(City, cell, "farm", false));
            var data = queue.Capture();
            data[0].turnsRemaining = 1;
            Assert.AreEqual(2, queue.Capture()[0].turnsRemaining);
            Assert.AreEqual(1, CityTileQueue.Restore(City, data, map).Capture()[0].turnsRemaining);
            Assert.Throws<ArgumentException>(() => CityTileQueue.Restore(City, new[] { data[0], data[0] }, map));
        }

        [Test]
        public void CompletedBuildingsSurviveRestoreAndOnlyThenProduceIncome()
        {
            var city = new CityTileDevelopment();
            var cell = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var map = new Dictionary<HexCoordinates, MapCell> { [cell.Coordinates] = cell };
            Assert.IsTrue(city.TryAdd(City, cell, "farm"));
            Assert.AreEqual(0, city.ActiveIncome.Grain);
            city.Advance(); city.Advance();
            Assert.AreEqual(3, city.ActiveIncome.Grain);
            Assert.IsFalse(city.TryAdd(City, cell, "house"));
            var restored = CityTileDevelopment.Restore(City, city.Capture("castilla"), map);
            Assert.AreEqual("farm", restored.BuildingAt(cell.Coordinates));
            Assert.AreEqual(3, restored.ActiveIncome.Grain);
        }

        [Test]
        public void RestoreRejectsCompletedAndPendingOnSameTile()
        {
            var cell = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var map = new Dictionary<HexCoordinates, MapCell> { [cell.Coordinates] = cell };
            var data = new CityTileDevelopmentData {
                settlementId = "castilla",
                completed = new[] { new CityTileOrderData { q = 5, r = 4, project = "farm", turnsRemaining = 0 } },
                pending = new[] { new CityTileOrderData { q = 5, r = 4, project = "house", turnsRemaining = 2 } }
            };
            Assert.Throws<ArgumentException>(() => CityTileDevelopment.Restore(City, data, map));
        }

        [Test]
        public void CampaignSnapshotRoundTripsAndRejectsOverlappingCities()
        {
            var tile = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var map = new Dictionary<HexCoordinates, MapCell> { [tile.Coordinates] = tile };
            var centers = new Dictionary<string, HexCoordinates> { ["castilla"] = City, ["vecina"] = new HexCoordinates(6, 4) };
            var state = new CityTileDevelopment();
            Assert.IsTrue(state.TryAdd(City, tile, "farm"));
            var snapshot = CityTileCampaignSnapshot.Capture(new Dictionary<string, CityTileDevelopment> { ["castilla"] = state });
            Assert.AreEqual(1, snapshot.Restore(centers, map)["castilla"].Pending.Length);
            var other = new CityTileDevelopment();
            Assert.IsTrue(other.TryAdd(centers["vecina"], tile, "house"));
            var invalid = CityTileCampaignSnapshot.Capture(new Dictionary<string, CityTileDevelopment> { ["castilla"] = state, ["vecina"] = other });
            Assert.Throws<ArgumentException>(() => invalid.Restore(centers, map));
            snapshot.schemaVersion = 99;
            Assert.Throws<ArgumentException>(() => snapshot.Restore(centers, map));
        }

        [Test]
        public void VersionedSavePreservesOldVersionsAndRejectsUnexpectedBlocks()
        {
            var cell = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var center = new MapCell(City, TerrainType.Plain, false);
            var map = new GeneratedMap(2, 1, 1, new[] { center, cell });
            var settlements = new CampaignSettlementRegistry(new MapRouteGraph(map), new[] { new CampaignSettlement("castilla", "Castilla", "castilla", City) });
            var state = new CityTileDevelopment();
            Assert.IsTrue(state.TryAdd(City, cell, "farm"));
            var save = new CampaignSaveData { version = 9 };
            save.IncludeTileCities(new Dictionary<string, CityTileDevelopment> { ["castilla"] = state });
            Assert.AreEqual(12, save.version);
            Assert.AreEqual(1, save.RestoreTileCities(settlements, map)["castilla"].Pending.Length);
            Assert.AreEqual(9, save.version);
            Assert.AreEqual(0, new CampaignSaveData { version = 9 }.RestoreTileCities(settlements, map).Count);
            Assert.Throws<ArgumentException>(() => new CampaignSaveData { version = 9, tileBaseVersion = 9 }.RestoreTileCities(settlements, map));
        }

        [Test]
        public void ContestedTilesUseTheSameDeterministicCityAsTheIncomeCalculator()
        {
            var left = new MapCell(new HexCoordinates(4, 4), TerrainType.Plain, false);
            var shared = new MapCell(new HexCoordinates(5, 4), TerrainType.Plain, false);
            var right = new MapCell(new HexCoordinates(6, 4), TerrainType.Plain, false);
            var map = new GeneratedMap(3, 1, 1, new[] { left, shared, right });
            var cities = new CampaignSettlementRegistry(new MapRouteGraph(map), new[] {
                new CampaignSettlement("a", "A", "castilla", left.Coordinates),
                new CampaignSettlement("b", "B", "rival", right.Coordinates) });
            Assert.IsTrue(CityTileConstruction.IsAssigned(cities,"a",shared.Coordinates));
            Assert.IsFalse(CityTileConstruction.IsAssigned(cities,"b",shared.Coordinates));
        }
    }
}
