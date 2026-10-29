using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    [Serializable]public sealed class RivalFactionData
    {public string factionId;public int grain,wood,stone,gold,labor;}
    [Serializable]public sealed class RivalCampaignData
    {public bool enabled;public int closedTurn;public RivalFactionData[] factions;}

    public sealed class CampaignRivalState
    {
        private readonly SortedDictionary<string,CampaignResources> stocks;
        public CampaignRivalState(bool enabled,IEnumerable<string> factions,int turn=1)
        {
            if(turn<1||factions==null)throw new ArgumentException("Invalid rival initial state.");
            Enabled=enabled;ClosedTurn=turn;stocks=new(StringComparer.Ordinal);
            if(enabled)foreach(var id in factions)
                if(string.IsNullOrWhiteSpace(id)||!stocks.TryAdd(id,default))throw new ArgumentException("Invalid or duplicate rival faction.");
        }
        private CampaignRivalState(CampaignRivalState previous,int turn)
        {Enabled=previous.Enabled;ClosedTurn=turn;stocks=new(previous.stocks,StringComparer.Ordinal);}
        public bool Enabled {get;}
        public int ClosedTurn {get;}
        public IReadOnlyList<string> Factions=>Array.AsReadOnly(stocks.Keys.ToArray());
        public CampaignResources Resources(string faction)=>stocks.TryGetValue(faction,out var stock)?stock:default;
        public CampaignRivalState WithResources(string faction,CampaignResources stock)
        {
            if(!stocks.ContainsKey(faction))throw new ArgumentException("Unknown rival faction.");
            var next=new CampaignRivalState(this,ClosedTurn);next.stocks[faction]=stock;return next;
        }
        public CampaignRivalState CloseTurn(int nextTurn,GeneratedMap map,CampaignSettlementRegistry owners,IReadOnlyDictionary<string,CityDevelopment> cities)
        {
            if(nextTurn!=checked(ClosedTurn+1))throw new InvalidOperationException("Close each rival turn exactly once.");
            var next=new CampaignRivalState(this,nextTurn);
            foreach(var faction in stocks.Keys)
            {
                var income=owners.Settlements.Count==0?default:CampaignIncomeCalculator.ForFaction(map,owners,faction);
                var upkeep=default(CampaignResources);
                foreach(var pair in cities.OrderBy(p=>p.Key,StringComparer.Ordinal))
                    if(owners.TryGet(pair.Key,out var city)&&city.OwnerFactionId==faction)
                    {income=income.Add(pair.Value.IncomePerTurn);upkeep=upkeep.Add(pair.Value.UpkeepPerTurn);}
                next.stocks[faction]=stocks[faction].Add(income).SubtractClamped(upkeep);
            }
            return next;
        }
        public RivalCampaignData Capture()=>new RivalCampaignData {enabled=Enabled,closedTurn=ClosedTurn,
            factions=stocks.Select(p=>new RivalFactionData{factionId=p.Key,grain=p.Value.Grain,wood=p.Value.Wood,stone=p.Value.Stone,gold=p.Value.Gold,labor=p.Value.Labor}).ToArray()};
        public static CampaignRivalState Restore(RivalCampaignData data,bool enabled,IEnumerable<string> factions,int turn)
        {
            if(data?.factions==null||data.enabled!=enabled||data.closedTurn!=turn)throw new ArgumentException("Missing or incompatible rival state.");
            var next=new CampaignRivalState(enabled,factions,turn);var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var saved in data.factions)
            {
                if(saved?.factionId==null||!next.stocks.ContainsKey(saved.factionId)||!seen.Add(saved.factionId))throw new ArgumentException("Unknown or duplicate rival treasury.");
                next.stocks[saved.factionId]=new CampaignResources(saved.grain,saved.wood,saved.stone,saved.gold,saved.labor);
            }
            if(seen.Count!=next.stocks.Count)throw new ArgumentException("Missing rival treasury.");return next;
        }
    }
    public sealed partial class CampaignSaveData
    {
        public RivalCampaignData rivals;
        public void IncludeRivals(CampaignRivalState state)
        {
            if(version!=7||state==null||state.ClosedTurn!=turn)throw new ArgumentException("Rival state does not match this turn.");
            rivals=state.Capture();context=RivalScenarioSignature(context,state.Enabled);version=8;
        }
        public static string RivalScenarioSignature(string marchContext,bool enabled)
        {
            if(string.IsNullOrEmpty(marchContext))throw new ArgumentException("Missing campaign identity.");
            ulong hash=14695981039346656037UL;
            foreach(char c in "rivals-v8:three-armies:paid-recruitment:player-opponent:one-action:"+enabled+":"+marchContext)
                hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
        public CampaignRivalState RestoreRivals(bool enabled,IEnumerable<string> factions)
        {
            if(version>=8)return CampaignRivalState.Restore(rivals,enabled,factions,turn);
            if(enabled)throw new ArgumentException("Active rival scenarios require a v8 save.");
            return new CampaignRivalState(false,factions,turn);
        }
    }
}
