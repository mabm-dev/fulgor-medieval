using System;
using System.Collections.Generic;
using System.Globalization;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    // Public fields are the versioned serialization contract, not live state.
    [Serializable]
    public sealed partial class CampaignSaveData
    {
        public int version;
        public string context;
        public int turn, grain, wood, stone, gold, labor;
        public int q, r, remainingMovement, movementPerTurn;
        public bool hasMarchOrder;
        public int marchQ, marchR;
        public CityDevelopmentData[] cities;

        public static CampaignSaveData Capture(string context, CampaignTurnState state, ArmyMovementState army, HexCoordinates? marchDestination = null, CityDevelopmentData[] cities = null)
        {
            if (string.IsNullOrEmpty(context)) throw new ArgumentException("Missing scenario signature.", nameof(context));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (army == null) throw new ArgumentNullException(nameof(army));
            return new CampaignSaveData { version = 3, cities = cities ?? Array.Empty<CityDevelopmentData>(), context = context, turn = state.Turn,
                grain = state.Resources.Grain, wood = state.Resources.Wood, stone = state.Resources.Stone,
                gold = state.Resources.Gold, labor = state.Resources.Labor, q = army.Position.Q, r = army.Position.R,
                remainingMovement = army.RemainingMovement, movementPerTurn = army.MovementPerTurn,
                hasMarchOrder = marchDestination.HasValue, marchQ = marchDestination?.Q ?? 0,
                marchR = marchDestination?.R ?? 0 };
        }

        public void Restore(string expectedContext, MapRouteGraph graph, HexCoordinates spawn,
            CampaignResources income, int expectedMovementPerTurn,
            out CampaignTurnState state, out ArmyMovementState army)
        {
            // Validate and construct everything before exposing either restored state.
            state = null; army = null;
            if ((version != 1 && version != 2 && version != 3 && version != 5 && version != 6 && version != 7 && version != 8 && version != 9) || multipleArmies)
                throw new ArgumentException("Unsupported save version or army mode.");
            if (string.IsNullOrEmpty(expectedContext) || context != expectedContext)
                throw new ArgumentException("Save belongs to a different scenario.");
            if(version>=7&&(marchOrders==null||marchOrders.Length!=0||marchOrderCount!=0))throw new ArgumentException("Simple save contains field army march orders.");
            if (movementPerTurn != expectedMovementPerTurn)
                throw new ArgumentException("Movement rules do not match.");
            var position = new HexCoordinates(q, r);
            if (!MapPathfinder.TryFindPath(graph, spawn, position, out _))
                throw new ArgumentException("Saved army is outside its accessible land component.");
            var restoredState = new CampaignTurnState(turn, new CampaignResources(grain, wood, stone, gold, labor), income);
            var restoredArmy = ArmyMovementState.Restore(graph, position, movementPerTurn, remainingMovement);
            RestoreMarchDestination(graph, restoredArmy);
            state = restoredState; army = restoredArmy;
        }

        public HexCoordinates? RestoreMarchDestination(MapRouteGraph graph, ArmyMovementState army)
        {
            if (version < 2 || !hasMarchOrder) return null;
            var destination = new HexCoordinates(marchQ, marchR);
            if (destination == army.Position || !MapPathfinder.TryFindPath(graph, army.Position, destination, out _))
                throw new ArgumentException("Invalid pending march destination.");
            return destination;
        }

        public Dictionary<string,CityDevelopment> RestoreCities(IEnumerable<string> settlementIds)
        {
            var restored=new Dictionary<string,CityDevelopment>(StringComparer.Ordinal);
            if(version<3)return restored;
            if((version!=3&&version!=4&&version!=5&&version!=6&&version!=7&&version!=8&&version!=9&&version!=10&&version!=11)||cities==null)throw new ArgumentException("Missing city state.");
            var known=new HashSet<string>(settlementIds,StringComparer.Ordinal);
            foreach(var data in cities) {
                var state=CityDevelopment.Restore(data,turn);
                if(!known.Contains(data.settlementId)||restored.ContainsKey(data.settlementId))
                    throw new ArgumentException("Unknown or duplicate settlement.");
                restored.Add(data.settlementId,state);
            }
            return restored;
        }

        public static string ScenarioSignature(GeneratedMap map, MapRouteGraph graph, ReachDestinationMission mission)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var parts = new List<string>();
            foreach (var cell in map.Cells)
                parts.Add($"C:{cell.Coordinates.Q}:{cell.Coordinates.R}:{(int)cell.Terrain}:{cell.HasGold}");
            foreach (var route in graph.Routes)
                parts.Add($"R:{route.Key.From.Q}:{route.Key.From.R}:{route.Key.To.Q}:{route.Key.To.R}:{(int)route.Value}");
            parts.Sort(StringComparer.Ordinal);
            parts.Add($"M:{map.Width}:{map.Height}:{map.Seed}");
            parts.Add(mission == null ? "O:none" :
                $"O:{mission.Spawn.Q}:{mission.Spawn.R}:{mission.Destination.Q}:{mission.Destination.R}");
            // FNV-1a over a deterministic textual representation; this is an identity
            // guard against incompatible scenarios, not a security checksum.
            ulong hash = 14695981039346656037UL;
            foreach (var part in parts)
                foreach (var c in part + "\n") hash = unchecked((hash ^ c) * 1099511628211UL);
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }
    }
}
