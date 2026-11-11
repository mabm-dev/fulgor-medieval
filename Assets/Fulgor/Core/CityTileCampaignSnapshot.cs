using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>Bloque de guardado independiente para el futuro guardado versionado de campaña.</summary>
    [Serializable]
    public sealed class CityTileCampaignSnapshot
    {
        public int schemaVersion;
        public CityTileDevelopmentData[] cities;

        public static CityTileCampaignSnapshot Capture(IReadOnlyDictionary<string, CityTileDevelopment> states)
        {
            if (states == null) throw new ArgumentNullException(nameof(states));
            var keys = new List<string>(states.Keys);
            keys.Sort(StringComparer.Ordinal);
            var data = new CityTileDevelopmentData[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                if (states[keys[i]] == null) throw new ArgumentException("Ciudad sin estado.");
                data[i] = states[keys[i]].Capture(keys[i]);
            }
            return new CityTileCampaignSnapshot { schemaVersion = 1, cities = data };
        }

        public Dictionary<string, CityTileDevelopment> Restore(
            IReadOnlyDictionary<string, HexCoordinates> centers,
            IReadOnlyDictionary<HexCoordinates, MapCell> map,
            IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> deposits = null)
        {
            if (centers == null || map == null) throw new ArgumentNullException();
            if (schemaVersion != 1 || cities == null) throw new ArgumentException("Bloque de construcciones incompatible.");
            var restored = new Dictionary<string, CityTileDevelopment>(StringComparer.Ordinal);
            var occupied = new HashSet<HexCoordinates>();
            foreach (var data in cities)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.settlementId) ||
                    !centers.TryGetValue(data.settlementId, out var center) || restored.ContainsKey(data.settlementId))
                    throw new ArgumentException("Ciudad desconocida o repetida.");
                var state = CityTileDevelopment.Restore(center, data, map, deposits);
                foreach (var item in data.completed)
                    if (!occupied.Add(new HexCoordinates(item.q, item.r))) throw new ArgumentException("Casilla asignada a dos ciudades.");
                foreach (var item in data.pending)
                    if (!occupied.Add(new HexCoordinates(item.q, item.r))) throw new ArgumentException("Casilla asignada a dos ciudades.");
                restored.Add(data.settlementId, state);
            }
            return restored;
        }
    }
}
