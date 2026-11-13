using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>Reglas provisionales de colocación; no modifica la economía ni el guardado existente.</summary>
    public static class CityTileConstruction
    {
        /// <summary>La mina y la cantera llegan a los yacimientos de su ciudad hasta esta distancia; el resto de obras, solo al primer anillo.</summary>
        public const int MineReach = 3;
        public static int ReachOf(string project) => project == "goldmine" || project == "quarry" ? MineReach : 1;

        public static bool IsAssigned(CampaignSettlementRegistry settlements, string id, HexCoordinates tile, int reach = 1)
        {
            if (settlements == null) return false;
            CampaignSettlement nearest = null; var distance = int.MaxValue;
            foreach (var city in settlements.Settlements)
            {
                var next = city.Position.DistanceTo(tile);
                if (next < distance || next == distance && string.CompareOrdinal(city.Id, nearest?.Id) < 0) { distance = next; nearest = city; }
            }
            return nearest != null && nearest.Id == id && distance >= 1 && distance <= reach;
        }
        /// <summary>Obra que encaja en un yacimiento: la mina de oro o la cantera; null sin yacimiento.</summary>
        public static string MineFor(ResourceDepositKind deposit) => deposit switch {
            ResourceDepositKind.Gold => "goldmine",
            ResourceDepositKind.Stone => "quarry",
            _ => null
        };

        public static bool CanPlace(HexCoordinates city, MapCell cell, string project, bool occupied, ResourceDepositKind deposit = ResourceDepositKind.None)
        {
            if (occupied) return false;
            var distance = city.DistanceTo(cell.Coordinates);
            if (distance < 1 || distance > ReachOf(project)) return false;
            return project switch {
                // la mina de oro y la cantera solo sobre su yacimiento; un yacimiento queda reservado para su mina
                "goldmine" => deposit == ResourceDepositKind.Gold && ResourceDepositLayout.IsEligible(cell.Terrain),
                "quarry" => deposit == ResourceDepositKind.Stone && ResourceDepositLayout.IsEligible(cell.Terrain),
                _ when deposit != ResourceDepositKind.None => false,
                "house" => cell.Terrain == TerrainType.Plain || cell.Terrain == TerrainType.Hill,
                "farm" or "market_stall" or "militia_camp" or "knowledge_house" => cell.Terrain == TerrainType.Plain,
                "simple_quarry" => cell.Terrain == TerrainType.Hill,
                "lumber" => cell.Terrain == TerrainType.Forest,
                _ => false
            };
        }

        public static CampaignResources Passive(TerrainType terrain) => terrain switch {
            TerrainType.Plain => new CampaignResources(1, 0, 0, 0, 0),
            TerrainType.Forest => new CampaignResources(0, 1, 0, 0, 0),
            TerrainType.Hill => new CampaignResources(0, 0, 1, 0, 0),
            _ => default
        };

        // La marca de oro del generador (MapCell.HasGold) ya no produce: el oro sale de los yacimientos (ResourceDepositLayout).
        public static CampaignResources Passive(MapCell cell, ResourceDepositKind deposit = ResourceDepositKind.None)
        {
            if(cell.Terrain==TerrainType.Mountain)return default;
            var terrain = Passive(cell.Terrain);
            return terrain.Add(new CampaignResources(0, 0, 0, 0, cell.Terrain == TerrainType.Water ? 0 : 1)).Add(ResourceDepositLayout.Yield(deposit));
        }

        public static CampaignResources Active(string project, CampaignResearch research = null) => project switch {
            "market_stall" => new CampaignResources(0, 0, 0, research?.Has("routes") == true ? 3 : 2, 0),
            "farm" => new CampaignResources(research?.Has("seed_selection") == true ? 4 : 3, 0, 0, 0, 0),
            "simple_quarry" => new CampaignResources(0, 0, 3, 0, 0),
            "lumber" => new CampaignResources(0, 3, 0, 0, 0),
            // sobre el +1 del yacimiento: con la mina, +3 de oro o de piedra por turno
            "goldmine" => new CampaignResources(0, 0, 0, 2, 0),
            "quarry" => new CampaignResources(0, 0, 2, 0, 0),
            // La población todavía no se simula; no se inventa un ingreso de mano de obra.
            _ => default
        };

        public static CampaignResources Cost(string project) => project switch {
            "militia_camp" or "knowledge_house" => new CampaignResources(0, 20, 0, 0, 10),
            "house" => new CampaignResources(0, 10, 0, 0, 5),
            "farm" => new CampaignResources(0, 15, 0, 0, 10),
            "market_stall" or "simple_quarry" => new CampaignResources(0, 15, 0, 0, 8),
            "lumber" => new CampaignResources(0, 5, 0, 0, 6),
            "goldmine" => new CampaignResources(0, 20, 10, 0, 10),
            "quarry" => new CampaignResources(0, 15, 0, 0, 8),
            _ => throw new ArgumentException("Edificio desconocido.", nameof(project))
        };
    }

    [Serializable]
    public sealed class CityTileOrderData
    {
        public int q, r, turnsRemaining;
        public string project;
    }

    /// <summary>Una cola por ciudad; solo la primera obra avanza al cerrar turno.</summary>
    public sealed class CityTileQueue
    {
        private readonly List<CityTileOrderData> orders = new();
        public int Count => orders.Count;

        public bool Contains(HexCoordinates tile) => orders.Exists(o => o.q == tile.Q && o.r == tile.R);

        internal static ResourceDepositKind DepositAt(IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> deposits, HexCoordinates tile) =>
            deposits != null && deposits.TryGetValue(tile, out var kind) ? kind : ResourceDepositKind.None;

        public bool TryAdd(HexCoordinates city, MapCell tile, string project, bool alreadyBuilt, ResourceDepositKind deposit = ResourceDepositKind.None)
        {
            if (!CityTileConstruction.CanPlace(city, tile, project, alreadyBuilt || Contains(tile.Coordinates), deposit)) return false;
            orders.Add(new CityTileOrderData { q = tile.Coordinates.Q, r = tile.Coordinates.R, project = project, turnsRemaining = 2 });
            return true;
        }

        public bool Cancel(HexCoordinates tile)
        {
            var index = orders.FindIndex(o => o.q == tile.Q && o.r == tile.R);
            if (index < 0) return false;
            orders.RemoveAt(index);
            return true;
        }

        // Returns a detached snapshot; callers cannot mutate the live queue through it.
        public CityTileOrderData[] Capture()
        {
            var result = new CityTileOrderData[orders.Count];
            for (var i = 0; i < orders.Count; i++)
                result[i] = new CityTileOrderData { q = orders[i].q, r = orders[i].r, project = orders[i].project, turnsRemaining = orders[i].turnsRemaining };
            return result;
        }

        public CityTileOrderData Advance()
        {
            if (orders.Count == 0) return null;
            var first = orders[0];
            first.turnsRemaining--;
            if (first.turnsRemaining > 0) return null;
            orders.RemoveAt(0);
            return first;
        }

        public static CityTileQueue Restore(HexCoordinates city, CityTileOrderData[] data, IReadOnlyDictionary<HexCoordinates, MapCell> map,
            IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> deposits = null)
        {
            if (data == null || map == null) throw new ArgumentNullException();
            var restored = new CityTileQueue();
            for (var i = 0; i < data.Length; i++)
            {
                var item = data[i];
                if (item == null || item.turnsRemaining < 1 || item.turnsRemaining > 2 || (i > 0 && item.turnsRemaining != 2))
                    throw new ArgumentException("Obra inválida.");
                var position = new HexCoordinates(item.q, item.r);
                if (!map.TryGetValue(position, out var cell) || !restored.TryAdd(city, cell, item.project, false, DepositAt(deposits, position)))
                    throw new ArgumentException("Casilla o edificio inválido.");
                restored.orders[i].turnsRemaining = item.turnsRemaining;
            }
            return restored;
        }
    }

    [Serializable]
    public sealed class CityTileDevelopmentData
    {
        public string settlementId;
        public int population;
        public CityTileOrderData[] pending;
        public CityTileOrderData[] completed;
    }

    /// <summary>Obras terminadas y pendientes de una ciudad sobre casillas reales.</summary>
    public sealed class CityTileDevelopment
    {
        public const int StartingPopulation = 10;
        public int Population { get; private set; } = StartingPopulation;
        public int HousingCapacity(int houses)
        {
            foreach (var building in completed.Values)
                if (building == "house") houses++;
            return houses * 10;
        }
        public static int FoodConsumption(int population) => (population + 9) / 10;
        public void Grow(int food, int houses)
        {
            if (Population < HousingCapacity(houses) && food > FoodConsumption(Population))
                Population++;
        }

        private CityTileQueue queue = new();
        private readonly Dictionary<HexCoordinates, string> completed = new();
        public CityTileOrderData[] Pending => queue.Capture();
        public bool IsOccupied(HexCoordinates position) => completed.ContainsKey(position) || queue.Contains(position);
        public bool HasKnowledgeHouse => completed.ContainsValue("knowledge_house");
        public bool HasMilitiaCamp => completed.ContainsValue("militia_camp");
        public string BuildingAt(HexCoordinates position) => completed.TryGetValue(position, out var project) ? project : null;

        public bool TryAdd(HexCoordinates city, MapCell tile, string project, ResourceDepositKind deposit = ResourceDepositKind.None) =>
            queue.TryAdd(city, tile, project, completed.ContainsKey(tile.Coordinates), deposit);

        public bool Cancel(HexCoordinates position) => queue.Cancel(position);

        public void Advance()
        {
            var finished = queue.Advance();
            if (finished != null) completed.Add(new HexCoordinates(finished.q, finished.r), finished.project);
        }

        public CampaignResources ActiveIncome => Income(null);

        public CampaignResources Income(CampaignResearch research)
        {
            var result = default(CampaignResources);
            foreach (var project in completed.Values)
                result = result.Add(CityTileConstruction.Active(project, research));
            return result;
        }

        public CityTileDevelopmentData Capture(string settlementId)
        {
            if (string.IsNullOrWhiteSpace(settlementId)) throw new ArgumentException("Ciudad inválida.");
            var built = new List<CityTileOrderData>();
            foreach (var item in completed)
                built.Add(new CityTileOrderData { q = item.Key.Q, r = item.Key.R, project = item.Value, turnsRemaining = 0 });
            built.Sort((a, b) => a.q == b.q ? a.r.CompareTo(b.r) : a.q.CompareTo(b.q));
            return new CityTileDevelopmentData { population = Population, settlementId = settlementId, pending = queue.Capture(), completed = built.ToArray() };
        }

        public static CityTileDevelopment Restore(HexCoordinates city, CityTileDevelopmentData data, IReadOnlyDictionary<HexCoordinates, MapCell> map,
            IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> deposits = null)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.settlementId) || data.pending == null || data.completed == null || map == null)
                throw new ArgumentException("Estado de ciudad inválido.");
            if (data.population < 0 || data.population > 10000) throw new ArgumentException("Censo fuera de rango.");
            var result = new CityTileDevelopment { Population = data.population == 0 ? StartingPopulation : data.population };
            foreach (var item in data.completed)
            {
                if (item == null || item.turnsRemaining != 0) throw new ArgumentException("Edificio inválido.");
                var position = new HexCoordinates(item.q, item.r);
                if (!map.TryGetValue(position, out var cell) ||
                    !CityTileConstruction.CanPlace(city, cell, item.project, result.IsOccupied(position), CityTileQueue.DepositAt(deposits, position)))
                    throw new ArgumentException("Casilla o edificio inválido.");
                result.completed.Add(position, item.project);
            }
            result.queue = CityTileQueue.Restore(city, data.pending, map, deposits);
            foreach (var item in result.queue.Capture())
                if (result.completed.ContainsKey(new HexCoordinates(item.q, item.r))) throw new ArgumentException("Casilla duplicada.");
            return result;
        }
    }
}
