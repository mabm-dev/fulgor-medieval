using Fulgor.Core.Map;
using System;
using System.Collections.Generic;

namespace Fulgor.Core.Campaign
{
    /// <summary>Provisional terrain yields and deterministic settlement districts.</summary>
    public static class CampaignIncomeCalculator
    {
        public static CampaignResources FromMap(GeneratedMap map)
        {
            if (map == null) throw new System.ArgumentNullException(nameof(map));
            return FromCells(map.Cells);
        }

        private static CampaignResources FromCells(IEnumerable<MapCell> cells)
        {
            var grain = 0; var wood = 0; var stone = 0; var gold = 0; var labor = 0;
            foreach (var cell in cells)
            {
                switch (cell.Terrain)
                {
                    case TerrainType.Plain: grain++; break;
                    case TerrainType.Forest: wood++; break;
                    case TerrainType.Hill: stone++; break;
                    case TerrainType.Mountain: stone += 2; break;
                }
                if (cell.Terrain != TerrainType.Water) labor++;
                if (cell.HasGold) gold++;
            }
            return new CampaignResources(grain, wood, stone, gold, labor);
        }

        public static CampaignResources ForFaction(GeneratedMap map,CampaignSettlementRegistry settlements,string faction)
        {
            if(map==null)throw new ArgumentNullException(nameof(map));
            if(string.IsNullOrWhiteSpace(faction))throw new ArgumentException("Missing income faction.");
            if(settlements==null||settlements.Settlements.Count==0)return FromMap(map);
            var controlled=new List<MapCell>();
            foreach(var cell in map.Cells)
            {
                CampaignSettlement nearest=null;int distance=int.MaxValue;
                foreach(var city in settlements.Settlements)
                {
                    int candidate=cell.Coordinates.DistanceTo(city.Position);
                    if(candidate<distance||candidate==distance&&StringComparer.Ordinal.Compare(city.Id,nearest?.Id)<0)
                    {nearest=city;distance=candidate;}
                }
                if(nearest.OwnerFactionId==faction)controlled.Add(cell);
            }
            return FromCells(controlled);
        }
    }
}
