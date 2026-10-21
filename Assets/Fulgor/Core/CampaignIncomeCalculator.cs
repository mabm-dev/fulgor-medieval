using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>Rendimiento provisional del mapa hasta que exista territorio asignado.</summary>
    public static class CampaignIncomeCalculator
    {
        public static CampaignResources FromMap(GeneratedMap map)
        {
            if (map == null) throw new System.ArgumentNullException(nameof(map));
            var grain = 0; var wood = 0; var stone = 0; var gold = 0; var labor = 0;
            foreach (var cell in map.Cells)
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
    }
}
