using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fulgor.Core.Randomness;

namespace Fulgor.Core.Map
{
    /// <summary>Recurso virgen de una casilla: el yacimiento sobre el que se construye la mina de oro o la cantera.</summary>
    public enum ResourceDepositKind { None, Gold, Stone }

    /// <summary>
    /// Yacimientos de oro y piedra del mapa. Es una capa aparte del generador, que es un port exacto del de la versión web y entra en la huella
    /// de las partidas guardadas: los yacimientos salen solo del mapa (su semilla y sus casillas), así que se rehacen igual al cargar sin
    /// guardarse. Van en llanura y colina (en la montaña ya no se extrae, en el agua no caben y la pieza de bosque lleva los árboles encima), sin dos
    /// yacimientos vecinos; el oro prefiere la colina. De cada recurso, el 7,8 % de esa tierra: 17 en un mapa de 24×16, parecido al oro útil de antes.
    /// </summary>
    public static class ResourceDepositLayout
    {
        public const double ShareOfLand = 0.078;
        // otra secuencia de la misma semilla: no consume la del generador
        private const long SeedSalt = 0x5ACA7E;
        private static readonly IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> Empty = new Dictionary<HexCoordinates, ResourceDepositKind>();
        private static readonly ConditionalWeakTable<GeneratedMap, IReadOnlyDictionary<HexCoordinates, ResourceDepositKind>> cache = new();

        /// <summary>Yacimientos del mapa por casilla.</summary>
        public static IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> For(GeneratedMap map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            return cache.GetValue(map, Build);
        }

        /// <summary>
        /// Fija yacimientos en casillas concretas de un mapa, encima de los de la semilla: lo usan los escenarios de prueba para tener un yacimiento
        /// de cada tipo en el territorio de una ciudad. Los de la semilla que caen en esas casillas o a su lado se quitan. Solo vale en llanura o
        /// colina. Se aplica a esa instancia del mapa y debe llamarse antes de que nadie lea sus yacimientos.
        /// </summary>
        public static void Pin(GeneratedMap map, IEnumerable<KeyValuePair<HexCoordinates, ResourceDepositKind>> pinned)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (pinned == null) return;
            var terrain = new Dictionary<HexCoordinates, TerrainType>();
            foreach (var cell in map.Cells) terrain[cell.Coordinates] = cell.Terrain;
            var fixedCells = new Dictionary<HexCoordinates, ResourceDepositKind>();
            foreach (var pin in pinned)
            {
                if (pin.Value == ResourceDepositKind.None) continue;
                if (!terrain.TryGetValue(pin.Key, out var ground) || !IsEligible(ground))
                    throw new ArgumentException($"El yacimiento fijado en {pin.Key.Key} no cae en llanura ni colina.", nameof(pinned));
                fixedCells[pin.Key] = pin.Value;
            }
            if (fixedCells.Count == 0) return;
            var result = new Dictionary<HexCoordinates, ResourceDepositKind>();
            foreach (var deposit in Build(map))
            {
                bool clash = fixedCells.ContainsKey(deposit.Key);
                foreach (var neighbour in deposit.Key.Neighbors()) clash |= fixedCells.ContainsKey(neighbour);
                if (!clash) result.Add(deposit.Key, deposit.Value);
            }
            foreach (var pin in fixedCells) result[pin.Key] = pin.Value;
            cache.Remove(map); cache.Add(map, result);
        }

        public static ResourceDepositKind At(GeneratedMap map, HexCoordinates cell) =>
            For(map).TryGetValue(cell, out var kind) ? kind : ResourceDepositKind.None;

        public static bool IsEligible(TerrainType terrain) =>
            terrain == TerrainType.Plain || terrain == TerrainType.Hill;

        /// <summary>Lo que da el yacimiento por sí solo cada turno, sin mina encima.</summary>
        public static Campaign.CampaignResources Yield(ResourceDepositKind kind) => kind switch
        {
            ResourceDepositKind.Gold => new Campaign.CampaignResources(0, 0, 0, 1, 0),
            ResourceDepositKind.Stone => new Campaign.CampaignResources(0, 0, 1, 0, 0),
            _ => default,
        };

        private static int Weight(ResourceDepositKind kind, TerrainType terrain) =>
            kind == ResourceDepositKind.Gold && terrain == TerrainType.Hill ? 3 : 1;

        private static IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> Build(GeneratedMap map)
        {
            var land = new List<MapCell>();
            foreach (var cell in map.Cells)
                if (IsEligible(cell.Terrain)) land.Add(cell);
            // sin mínimo: un mapa de prueba de pocas casillas se queda sin yacimientos
            int perKind = (int)Math.Round(land.Count * ShareOfLand, MidpointRounding.AwayFromZero);
            if (perKind == 0) return Empty;
            var random = new DeterministicRandom(map.Seed ^ SeedSalt);
            var result = new Dictionary<HexCoordinates, ResourceDepositKind>();
            var blocked = new HashSet<HexCoordinates>();
            // se alternan oro y piedra para que ninguno se quede con los mejores sitios
            for (int round = 0; round < perKind * 2; round++)
            {
                var kind = round % 2 == 0 ? ResourceDepositKind.Gold : ResourceDepositKind.Stone;
                int total = 0;
                foreach (var cell in land)
                    if (!blocked.Contains(cell.Coordinates)) total += Weight(kind, cell.Terrain);
                if (total == 0) break;
                int pick = random.NextInteger(0, total - 1);
                foreach (var cell in land)
                {
                    if (blocked.Contains(cell.Coordinates)) continue;
                    pick -= Weight(kind, cell.Terrain);
                    if (pick >= 0) continue;
                    result.Add(cell.Coordinates, kind);
                    blocked.Add(cell.Coordinates);
                    foreach (var neighbour in cell.Coordinates.Neighbors()) blocked.Add(neighbour);
                    break;
                }
            }
            return result;
        }
    }
}
