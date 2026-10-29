using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum CampaignVisibility { Unknown,Explored,Visible }
    [Serializable]public sealed class FogCellData {public int q,r;}
    [Serializable]public sealed class FogSettlementData {public string settlementId,ownerFactionId;public int lastSeenTurn;}
    [Serializable]public sealed class FogFactionData
    {public string factionId;public int exploredCount,settlementCount;public FogCellData[] explored;public FogSettlementData[] settlements;}
    [Serializable]public sealed class CampaignFogData {public int rulesVersion,factionCount;public FogFactionData[] factions;}
    public sealed class CampaignSettlementIntel
    {
        internal CampaignSettlementIntel(string id,string faction,int turn){SettlementId=id;OwnerFactionId=faction;LastSeenTurn=turn;}
        public string SettlementId {get;}
        public string OwnerFactionId {get;}
        public int LastSeenTurn {get;}
    }
    /// <summary>Immutable shared faction knowledge. Visibility is derived, exploration and city reports are remembered.</summary>
    public sealed class CampaignFogState
    {
        public const int VisionRadius=2;
        private sealed class Knowledge
        {
            internal HashSet<HexCoordinates> explored=new(),visible=new();
            internal SortedDictionary<string,CampaignSettlementIntel> cities=new(StringComparer.Ordinal);
            internal Knowledge Copy()=>new(){explored=new(explored),visible=new(visible),cities=new(cities,StringComparer.Ordinal)};
        }
        private readonly HexCoordinates[] cells;
        private readonly HashSet<HexCoordinates> valid;
        private readonly SortedDictionary<string,Knowledge> factions;
        public IReadOnlyList<HexCoordinates> Cells=>Array.AsReadOnly(cells);
        public IReadOnlyList<string> Factions=>Array.AsReadOnly(factions.Keys.ToArray());
        public bool ContainsCell(HexCoordinates cell)=>valid.Contains(cell);
        public CampaignFogState(GeneratedMap map,IEnumerable<string> factionIds)
        {
            if(map==null||factionIds==null)throw new ArgumentNullException("fog");
            cells=map.Cells.Select(c=>c.Coordinates).OrderBy(c=>c.R).ThenBy(c=>c.Q).ToArray();valid=new(cells);
            if(valid.Count!=cells.Length)throw new ArgumentException("Duplicate map cell.");
            factions=new(StringComparer.Ordinal);
            foreach(var id in factionIds)if(string.IsNullOrWhiteSpace(id)||!factions.TryAdd(id,new Knowledge()))throw new ArgumentException("Invalid or duplicate fog faction.");
            if(factions.Count==0)throw new ArgumentException("No fog faction.");
        }
        private CampaignFogState(CampaignFogState previous)
        {cells=previous.cells;valid=previous.valid;factions=new(StringComparer.Ordinal);foreach(var pair in previous.factions)factions.Add(pair.Key,pair.Value.Copy());}
        public CampaignVisibility Visibility(string faction,HexCoordinates at)
        {
            if(!factions.TryGetValue(faction,out var known)||!valid.Contains(at))return CampaignVisibility.Unknown;
            return known.visible.Contains(at)?CampaignVisibility.Visible:known.explored.Contains(at)?CampaignVisibility.Explored:CampaignVisibility.Unknown;
        }
        public bool IsVisible(string faction,HexCoordinates at)=>Visibility(faction,at)==CampaignVisibility.Visible;
        public bool IsExplored(string faction,HexCoordinates at)=>Visibility(faction,at)!=CampaignVisibility.Unknown;
        public int ExploredCount(string faction)=>factions.TryGetValue(faction,out var known)?known.explored.Count:0;
        public IReadOnlyList<CampaignSettlementIntel> SettlementIntel(string faction)=>factions.TryGetValue(faction,out var known)?Array.AsReadOnly(known.cities.Values.ToArray()):Array.Empty<CampaignSettlementIntel>();
        public CampaignSettlementIntel Intel(string faction,string cityId)=>factions.TryGetValue(faction,out var known)&&known.cities.TryGetValue(cityId,out var intel)?intel:null;
        public CampaignFogState Refresh(CampaignArmyRegistry armies,CampaignSettlementRegistry owners,int turn)
        {
            if(armies==null||owners==null||turn<1)throw new ArgumentException("Missing fog observation context.");
            var next=new CampaignFogState(this);
            foreach(var pair in next.factions)
            {
                var sources=armies.Armies.Where(a=>a.Army.FactionId==pair.Key).Select(a=>a.Movement.Position)
                    .Concat(owners.Settlements.Where(c=>c.OwnerFactionId==pair.Key).Select(c=>c.Position)).ToArray();
                var known=pair.Value;known.visible.Clear();
                foreach(var cell in cells)if(sources.Any(s=>s.DistanceTo(cell)<=VisionRadius))known.visible.Add(cell);
                known.explored.UnionWith(known.visible);
                foreach(var city in owners.Settlements)
                {
                    // A kingdom also receives notice when one of its own cities falls.
                    bool lostOwn=known.cities.TryGetValue(city.Id,out var previous)&&previous.OwnerFactionId==pair.Key&&city.OwnerFactionId!=pair.Key;
                    if(known.visible.Contains(city.Position)||lostOwn)
                        known.cities[city.Id]=new CampaignSettlementIntel(city.Id,city.OwnerFactionId,turn);
                }
            }
            return next;
        }
        public CampaignFogState LegacyKnowledge(CampaignSettlementRegistry owners,int turn)
        {
            if(owners==null||turn<1)throw new ArgumentException("Invalid legacy knowledge.");
            var next=new CampaignFogState(this);
            foreach(var known in next.factions.Values)
            {
                known.explored.UnionWith(cells);
                foreach(var city in owners.Settlements)known.cities[city.Id]=new CampaignSettlementIntel(city.Id,city.OwnerFactionId,turn);
            }
            return next;
        }
        public CampaignFogData Capture()=>new(){rulesVersion=1,factionCount=factions.Count,factions=factions.Select(p=>new FogFactionData{
            factionId=p.Key,exploredCount=p.Value.explored.Count,settlementCount=p.Value.cities.Count,
            explored=p.Value.explored.OrderBy(c=>c.R).ThenBy(c=>c.Q).Select(c=>new FogCellData{q=c.Q,r=c.R}).ToArray(),
            settlements=p.Value.cities.Values.Select(c=>new FogSettlementData{settlementId=c.SettlementId,ownerFactionId=c.OwnerFactionId,lastSeenTurn=c.LastSeenTurn}).ToArray()}).ToArray()};
        public static CampaignFogState Restore(CampaignFogData data,GeneratedMap map,IEnumerable<string> factionIds,CampaignArmyRegistry armies,CampaignSettlementRegistry owners,int turn)
        {
            if(data?.factions==null||data.rulesVersion!=1||data.factions.Length!=data.factionCount||turn<1)throw new ArgumentException("Missing or unsupported exploration state.");
            var saved=new CampaignFogState(map,factionIds);var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var faction in data.factions)
            {
                if(faction?.factionId==null||!saved.factions.TryGetValue(faction.factionId,out var known)||!seen.Add(faction.factionId)||
                    faction.explored==null||faction.explored.Length!=faction.exploredCount||faction.settlements==null||faction.settlements.Length!=faction.settlementCount)
                    throw new ArgumentException("Incomplete or duplicate exploration faction.");
                foreach(var cell in faction.explored)
                    if(cell==null||!saved.valid.Contains(new HexCoordinates(cell.q,cell.r))||!known.explored.Add(new HexCoordinates(cell.q,cell.r)))throw new ArgumentException("Invalid or duplicate explored cell.");
                foreach(var city in faction.settlements)
                {
                    if(city?.settlementId==null||!owners.TryGet(city.settlementId,out var actual)||!known.explored.Contains(actual.Position)||
                        !saved.factions.ContainsKey(city.ownerFactionId??"")||city.lastSeenTurn<1||city.lastSeenTurn>turn||!known.cities.TryAdd(city.settlementId,new CampaignSettlementIntel(city.settlementId,city.ownerFactionId,city.lastSeenTurn)))
                        throw new ArgumentException("Invalid remembered settlement.");
                }
            }
            if(seen.Count!=saved.factions.Count)throw new ArgumentException("Missing fog faction.");
            var current=saved.Refresh(armies,owners,turn);
            foreach(var pair in saved.factions)
            {
                var now=current.factions[pair.Key];
                if(!now.visible.IsSubsetOf(pair.Value.explored))throw new ArgumentException("Current sight is missing from exploration.");
                foreach(var city in now.cities.Values)
                    if(!pair.Value.cities.TryGetValue(city.SettlementId,out var old)||old.OwnerFactionId!=city.OwnerFactionId||old.LastSeenTurn!=city.LastSeenTurn)
                        throw new ArgumentException("Current city observation does not match the saved report.");
            }
            return current;
        }
        public void ValidateOrders(CampaignMarchOrders orders,CampaignArmyRegistry armies,string player,MapRouteGraph graph)
        {
            foreach(var id in orders.ArmyIds)
            {
                if(!armies.TryGetArmy(id,out var army)||army.Army.FactionId!=player||!IsExplored(player,orders.Destination(id).Value))throw new ArgumentException("March destination has not been explored.");
                var unknown=new HashSet<HexCoordinates>(cells.Where(c=>!IsExplored(player,c)));
                if(!MapPathfinder.TryFindPath(graph,army.Movement.Position,orders.Destination(id).Value,out _,unknown,army.Movement.MovementPerTurn))throw new ArgumentException("March crosses unknown terrain.");
            }
        }
    }
    public sealed partial class CampaignSaveData
    {
        public CampaignFogData exploration;
        public void IncludeFog(CampaignFogState fog)
        {
            if(fog==null)return;
            if(!multipleArmies||(version!=9&&version!=10))throw new ArgumentException("Fog requires a complete multiple-army snapshot.");
            exploration=fog.Capture();context=FogSignature(context);version=11;
        }
        public static string FogSignature(string context)
        {
            if(string.IsNullOrEmpty(context))throw new ArgumentException("Missing exploration identity.");
            ulong hash=14695981039346656037UL;
            foreach(char c in "fog-v11:radius2:shared-faction:known-routes:step-observation:city-reports:"+context)hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
        public CampaignFogState RestoreFog(bool enabled,GeneratedMap map,IEnumerable<string> factions,CampaignArmyRegistry armies,CampaignSettlementRegistry owners)
        {
            if(version==11)
            {
                if(!enabled||!multipleArmies)throw new ArgumentException("Exploration mode does not match.");
                return CampaignFogState.Restore(exploration,map,factions,armies,owners,turn);
            }
            if(version>11)throw new ArgumentException("Unsupported exploration version.");
            if(!enabled)return null;
            if(version!=9&&version!=10)throw new ArgumentException("Only compatible v9/v10 campaigns can adopt fog.");
            return new CampaignFogState(map,factions).LegacyKnowledge(owners,turn).Refresh(armies,owners,turn);
        }
    }
}
