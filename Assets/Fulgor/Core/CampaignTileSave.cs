using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignSaveData
    {
        public int tileBaseVersion;
        public CityTileCampaignSnapshot tileCities;

        public void IncludeTileCities(IReadOnlyDictionary<string, CityTileDevelopment> states)
        {
            if (states == null) throw new ArgumentNullException(nameof(states));
            if (states.Count == 0) return;
            if (version < 3 || version > 11) throw new ArgumentException("Versión de campaña incompatible.");
            tileCities = CityTileCampaignSnapshot.Capture(states);
            tileBaseVersion = version;
            version = 12;
        }

        // Normalize only a newly deserialized object, before the existing validation pipeline.
        public Dictionary<string, CityTileDevelopment> RestoreTileCities(CampaignSettlementRegistry settlements, GeneratedMap map)
        {
            if (settlements == null || map == null) throw new ArgumentNullException();
            if (version != 12)
            {
                if (tileBaseVersion != 0 || tileCities != null && (tileCities.schemaVersion != 0 || tileCities.cities != null && tileCities.cities.Length != 0))
                    throw new ArgumentException("Bloque de construcciones inesperado.");
                return new Dictionary<string, CityTileDevelopment>(StringComparer.Ordinal);
            }
            if (tileBaseVersion < 3 || tileBaseVersion > 11 || tileCities == null) throw new ArgumentException("Guardado de construcciones inválido.");
            var centers = new Dictionary<string, HexCoordinates>(StringComparer.Ordinal);
            foreach (var city in settlements.Settlements) centers.Add(city.Id, city.Position);
            var cells = new Dictionary<HexCoordinates, MapCell>();
            foreach (var cell in map.Cells) cells.Add(cell.Coordinates, cell);
            var restored = tileCities.Restore(centers, cells);
            foreach (var city in restored)
            {
                foreach (var item in city.Value.Capture(city.Key).completed)
                {
                    var position = new HexCoordinates(item.q,item.r);
                    if (settlements.TryGet(position, out _) || !CityTileConstruction.IsAssigned(settlements,city.Key,position)) throw new ArgumentException("Construcción fuera del territorio.");
                }
                foreach (var item in city.Value.Pending)
                {
                    var position = new HexCoordinates(item.q,item.r);
                    if (settlements.TryGet(position, out _) || !CityTileConstruction.IsAssigned(settlements,city.Key,position)) throw new ArgumentException("Obra fuera del territorio.");
                }
            }
            version = tileBaseVersion;
            tileBaseVersion = 0;
            tileCities = null;
            return restored;
        }
    }
}
