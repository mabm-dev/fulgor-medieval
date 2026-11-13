using System;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private Dictionary<string, CityTileDevelopment> tileCities = new(StringComparer.Ordinal);
        public int TileCityRevision { get; private set; }
        public CityTileDevelopment TileCity(string id) => id != null && tileCities.TryGetValue(id, out var state) ? state : null;
        public bool IsCityTileOccupied(HexCoordinates tile)
        {
            if (SettlementState != null && SettlementState.TryGet(tile, out _)) return true;
            if (GetComponent<CampaignTileBuildings>()?.Occupies(tile) == true) return true;
            foreach (var entry in tileCities) if (entry.Value.IsOccupied(tile)) return true;
            return false;
        }
        public bool IsTileAssignedToCity(string id, HexCoordinates tile, int reach = 1)
        {
            return CityTileConstruction.IsAssigned(SettlementState,id,tile,reach);
        }

        /// <summary>Yacimiento de la casilla en el mapa de la campaña (ResourceDepositLayout); ninguno antes de empezar.</summary>
        public ResourceDepositKind DepositAt(HexCoordinates tile) => incomeMap != null ? ResourceDepositLayout.At(incomeMap, tile) : ResourceDepositKind.None;

        /// <summary>Obra terminada en una casilla, de cualquier ciudad (por ejemplo la mina sobre un yacimiento); null si no hay.</summary>
        public string CityTileBuildingAt(HexCoordinates tile)
        {
            foreach (var entry in tileCities)
            {
                var building = entry.Value.BuildingAt(tile);
                if (building != null) return building;
            }
            return null;
        }

        public bool IsProjectUnlocked(string project) => project != "simple_quarry" || Research.Has("tools");

        public bool TryPlaceCityTile(string settlementId, MapCell tile, string project)
        {
            var settlements = GetComponent<CampaignSettlementController>();
            if (State == null || IsCampaignFinished || HasPendingBattle || IsRivalPhaseActive || settlements == null ||
                !IsProjectUnlocked(project) || !settlements.CanManage(settlementId) || !SettlementState.TryGet(settlementId, out var city) ||
                VisibilityAt(tile.Coordinates)==CampaignVisibility.Unknown || IsCityTileOccupied(tile.Coordinates) || !IsTileAssignedToCity(settlementId,tile.Coordinates,CityTileConstruction.ReachOf(project)) || incomeMap == null) return false;
            bool actual = false;
            foreach (var cell in incomeMap.Cells) if (cell.Equals(tile)) { actual = true; break; }
            if (!actual) return false;
            foreach (var entry in tileCities) if (entry.Value.IsOccupied(tile.Coordinates)) return false;
            var deposit = DepositAt(tile.Coordinates);
            if (!CityTileConstruction.CanPlace(city.Position, tile, project, false, deposit)) return false;
            var cost = CityTileConstruction.Cost(project);
            if (!State.Resources.TrySpend(cost, out var remaining)) return false;
            if (!tileCities.TryGetValue(settlementId, out var development)) development = new CityTileDevelopment();
            if (!development.TryAdd(city.Position, tile, project, deposit)) return false;
            tileCities[settlementId] = development;
            State = new CampaignTurnState(State.Turn, remaining, State.IncomePerTurn, State.ExpensesPerTurn);
            TileCityRevision++; panel?.SetTurnState(State);
            return true;
        }

        // Cancelling an unfinished order returns its complete cost.
        public bool CancelCityTile(string settlementId, HexCoordinates position)
        {
            var settlements = GetComponent<CampaignSettlementController>();
            if (State == null || IsCampaignFinished || HasPendingBattle || IsRivalPhaseActive || settlements == null ||
                !settlements.CanManage(settlementId) || !tileCities.TryGetValue(settlementId, out var development)) return false;
            string project = null;
            foreach (var order in development.Pending) if (order.q == position.Q && order.r == position.R) { project = order.project; break; }
            if (project == null || !development.Cancel(position)) return false;
            State = new CampaignTurnState(State.Turn, State.Resources.Add(CityTileConstruction.Cost(project)), State.IncomePerTurn, State.ExpensesPerTurn);
            TileCityRevision++; panel?.SetTurnState(State);
            return true;
        }

        public bool UsesCityPopulation => LogisticsEnabled && GetComponent<CampaignPlaySession>() != null;

        public int LocalFood(string id)
        {
            int food = CityState(id).IncomePerTurn.Grain + (TileCity(id)?.Income(ResearchEnabled ? Research : null).Grain ?? 0) - CityState(id).UpkeepPerTurn.Grain;
            if (incomeMap != null && SettlementState.TryGet(id, out var city))
                foreach (var cell in incomeMap.Cells)
                    if (VisibilityAt(cell.Coordinates)!=CampaignVisibility.Unknown && (city.Position.Equals(cell.Coordinates) || IsTileAssignedToCity(id, cell.Coordinates)))
                        food += CityTileConstruction.Passive(cell, DepositAt(cell.Coordinates)).Grain;
            return food+(ResearchEnabled?Research.IncomePerCity.Grain:0);
        }

        private CampaignResources CivilUpkeep(CampaignSettlementRegistry owners, Dictionary<string, CityTileDevelopment> states)
        {
            int grain = 0;
            if (UsesCityPopulation && owners != null)
                foreach (var city in owners.Settlements)
                    if (city.OwnerFactionId == incomeFaction)
                        grain += CityTileDevelopment.FoodConsumption(states.TryGetValue(city.Id, out var town) ? town.Population : CityTileDevelopment.StartingPopulation);
            return new CampaignResources(grain, 0, 0, 0, 0);
        }

        private void ResetTileCities() { tileCities.Clear(); TileCityRevision++; }
        private void AdvanceTileCities()
        {
            if (UsesCityPopulation && SettlementState != null)
                foreach (var city in SettlementState.Settlements)
                    if (city.OwnerFactionId == incomeFaction)
                    {
                        if (!tileCities.TryGetValue(city.Id, out var town))
                            tileCities[city.Id] = town = new CityTileDevelopment();
                        town.Grow(LocalFood(city.Id), CityState(city.Id).Houses);
                    }
            foreach (var entry in tileCities)
                if (SettlementState.TryGet(entry.Key, out var owner) && owner.OwnerFactionId == incomeFaction) entry.Value.Advance();
            TileCityRevision++;
        }
        private CampaignResources TileCityIncome(CampaignSettlementRegistry owners, Dictionary<string, CityTileDevelopment> states, CampaignResearch research)
        {
            var result = default(CampaignResources);
            if (owners == null || states == null) return result;
            foreach (var entry in states)
                if (owners.TryGet(entry.Key, out var city) && city.OwnerFactionId == incomeFaction) result = result.Add(entry.Value.Income(ResearchEnabled ? research : null));
            return result;
        }
    }
}
