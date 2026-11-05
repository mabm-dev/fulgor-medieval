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
        public bool IsTileAssignedToCity(string id, HexCoordinates tile)
        {
            return CityTileConstruction.IsAssigned(SettlementState,id,tile);
        }

        public bool TryPlaceCityTile(string settlementId, MapCell tile, string project)
        {
            var settlements = GetComponent<CampaignSettlementController>();
            if (State == null || IsCampaignFinished || HasPendingBattle || IsRivalPhaseActive || settlements == null ||
                !settlements.CanManage(settlementId) || !SettlementState.TryGet(settlementId, out var city) ||
                IsCityTileOccupied(tile.Coordinates) || !IsTileAssignedToCity(settlementId,tile.Coordinates) || incomeMap == null) return false;
            bool actual = false;
            foreach (var cell in incomeMap.Cells) if (cell.Equals(tile)) { actual = true; break; }
            if (!actual) return false;
            foreach (var entry in tileCities) if (entry.Value.IsOccupied(tile.Coordinates)) return false;
            if (!CityTileConstruction.CanPlace(city.Position, tile, project, false)) return false;
            var cost = CityTileConstruction.Cost(project);
            if (!State.Resources.TrySpend(cost, out var remaining)) return false;
            if (!tileCities.TryGetValue(settlementId, out var development)) development = new CityTileDevelopment();
            if (!development.TryAdd(city.Position, tile, project)) return false;
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

        private void ResetTileCities() { tileCities.Clear(); TileCityRevision++; }
        private void AdvanceTileCities()
        {
            foreach (var entry in tileCities)
                if (SettlementState.TryGet(entry.Key, out var owner) && owner.OwnerFactionId == incomeFaction) entry.Value.Advance();
            TileCityRevision++;
        }
        private CampaignResources TileCityIncome(CampaignSettlementRegistry owners, Dictionary<string, CityTileDevelopment> states)
        {
            var result = default(CampaignResources);
            if (owners == null || states == null) return result;
            foreach (var entry in states)
                if (owners.TryGet(entry.Key, out var city) && city.OwnerFactionId == incomeFaction) result = result.Add(entry.Value.ActiveIncome);
            return result;
        }
    }
}
