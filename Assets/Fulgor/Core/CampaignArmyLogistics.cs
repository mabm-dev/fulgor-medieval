using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed class ArmyLogisticsReport
    {
        internal ArmyLogisticsReport(string id,string faction,bool garrison,bool supplied,bool paid,bool rested,
            CampaignResources upkeep,long healing,int moraleChange,int movement)
        {ArmyId=id;FactionId=faction;IsGarrison=garrison;Supplied=supplied;Paid=paid;Rested=rested;Upkeep=upkeep;Healing=healing;MoraleChange=moraleChange;NextMovement=movement;}
        public string ArmyId {get;}
        public string FactionId {get;}
        public bool IsGarrison {get;}
        public bool Supplied {get;}
        public bool Paid {get;}
        public bool Rested {get;}
        public CampaignResources Upkeep {get;}
        public long Healing {get;}
        public int MoraleChange {get;}
        public int NextMovement {get;}
    }
    public sealed class CampaignLogisticsResult
    {
        private readonly Dictionary<string,CampaignResources> stocks;
        internal CampaignLogisticsResult(CampaignArmyRegistry armies,CampaignMilitaryState military,
            Dictionary<string,CampaignResources> resources,List<ArmyLogisticsReport> reports)
        {Armies=armies;Military=military;stocks=resources;Reports=reports.AsReadOnly();}
        public CampaignArmyRegistry Armies {get;}
        public CampaignMilitaryState Military {get;}
        public IReadOnlyList<ArmyLogisticsReport> Reports {get;}
        public CampaignResources Resources(string faction)=>stocks.TryGetValue(faction,out var stock)?stock:throw new ArgumentException("Unmanaged faction.");
    }
    /// <summary>Pure turn-boundary accounting, supply and recovery. Never creates soldiers or advances an encounter.</summary>
    public static class CampaignArmyLogistics
    {
        public const int SupplyRange=6,RecoveryHealth=2,RecoveryMorale=5,UnsuppliedMoraleLoss=10;
        public static CampaignResources Upkeep(CampaignArmyState army)
        {
            int grain=0,gold=0;if(army==null)return default;
            foreach(var f in army.Formations)
            {
                int food=checked((int)(((long)f.Quantity+19)/20));
                grain=checked(grain+food*(f.Kind==Fulgor.Core.Combat.FormationKind.Cavalry?2:1));
                gold=checked(gold+(int)(((long)f.Quantity+24)/25));
            }
            return new CampaignResources(grain,0,0,gold,0);
        }
        public static CampaignResources UpkeepForFaction(CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,string faction)
        {
            var cost=default(CampaignResources);if(armies==null||military==null||owners==null)return cost;
            foreach(var army in armies.Armies)if(army.Army.FactionId==faction)cost=cost.Add(Upkeep(army.Army));
            foreach(var city in owners.Settlements)if(city.OwnerFactionId==faction)cost=cost.Add(Upkeep(military.Garrison(city.Id)));
            return cost;
        }
        public static HashSet<HexCoordinates> SupplyNetwork(MapRouteGraph graph,CampaignArmyRegistry armies,CampaignSettlementRegistry owners,string faction)
        {
            if(graph==null||armies==null||owners==null||string.IsNullOrWhiteSpace(faction))throw new ArgumentException("Missing supply context.");
            var blocked=new HashSet<HexCoordinates>(armies.Armies.Where(a=>a.Army.FactionId!=faction).Select(a=>a.Movement.Position));
            foreach(var city in owners.Settlements)if(city.OwnerFactionId!=faction)blocked.Add(city.Position);
            var costs=new Dictionary<HexCoordinates,int>();var frontier=new SortedSet<(int Cost,int Q,int R)>();
            foreach(var city in owners.Settlements)
                if(city.OwnerFactionId==faction&&!blocked.Contains(city.Position))
                {costs[city.Position]=0;frontier.Add((0,city.Position.Q,city.Position.R));}
            while(frontier.Count>0)
            {
                var item=frontier.Min;frontier.Remove(item);var at=new HexCoordinates(item.Q,item.R);
                if(costs[at]!=item.Cost)continue;
                foreach(var n in at.Neighbors())
                {
                    if(blocked.Contains(n)||!graph.TryGetMovementCost(at,n,out var step)||step>SupplyRange-item.Cost)continue;
                    int cost=item.Cost+step;if(costs.TryGetValue(n,out var old)&&old<=cost)continue;
                    costs[n]=cost;frontier.Add((cost,n.Q,n.R));
                }
            }
            return new HashSet<HexCoordinates>(costs.Keys);
        }
        public static CampaignLogisticsResult CloseTurn(CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,
            MapRouteGraph graph,IReadOnlyDictionary<string,CampaignResources> available)
        {
            if(armies==null||military==null||owners==null||graph==null||available==null)throw new ArgumentNullException("logistics");
            if(armies.HasActiveEncounters)throw new InvalidOperationException("Resolve battles before logistics.");
            military.ValidateLogisticsOwners(owners);
            military.ValidateDeployment(armies);
            var origins=military.FormationOrigins();var nextStocks=new Dictionary<string,CampaignResources>(StringComparer.Ordinal);
            var fieldChanges=new Dictionary<string,(CampaignArmyState Army,int Movement)>(StringComparer.Ordinal);
            var guardChanges=new Dictionary<string,CampaignArmyState>(StringComparer.Ordinal);var reports=new List<ArmyLogisticsReport>();
            foreach(var account in available.OrderBy(p=>p.Key,StringComparer.Ordinal))
            {
                if(string.IsNullOrWhiteSpace(account.Key))throw new ArgumentException("Invalid treasury.");
                var cost=UpkeepForFaction(armies,military,owners,account.Key);bool paid=account.Value.TrySpend(cost,out _);
                nextStocks.Add(account.Key,account.Value.SubtractClamped(cost));
                var supply=SupplyNetwork(graph,armies,owners,account.Key);
                CampaignArmyState Recover(CampaignArmyState army,bool garrison,bool supplied,bool rested,int budget)
                {
                    bool supported=paid&&supplied;var formations=new List<CampaignFormationState>();long healing=0;int morale=0;
                    foreach(var f in army.Formations)
                    {
                        var origin=origins[f.Id].Template;long health=f.Health;int spirit=f.Morale;
                        if(!supported)spirit=Math.Max(0,spirit-UnsuppliedMoraleLoss);
                        else if(rested)
                        {
                            long cap=Math.Min(origin.Health,(long)f.Quantity*f.Attributes.HealthPerMember);
                            health=Math.Min(cap,checked(f.Health+RecoveryHealth));
                            spirit=Math.Max(spirit,Math.Min(Math.Max(origin.Morale,f.Attributes.Morale),spirit+RecoveryMorale));
                        }
                        healing+=health-f.Health;morale+=spirit-f.Morale;
                        formations.Add(new CampaignFormationState(f.Id,f.Attributes,health,spirit,f.Kind));
                    }
                    int movement=garrison?0:supported?budget:Math.Max(1,budget/2);
                    if(!army.IsDissolved)reports.Add(new ArmyLogisticsReport(army.Id,army.FactionId,garrison,supplied,paid,rested,Upkeep(army),healing,morale,movement));
                    return army.WithFormations(formations);
                }
                foreach(var field in armies.Armies.Where(a=>a.Army.FactionId==account.Key))
                {
                    bool supplied=supply.Contains(field.Movement.Position),rested=field.Movement.RemainingMovement==field.Movement.MovementPerTurn;
                    var army=Recover(field.Army,false,supplied,rested,field.Movement.MovementPerTurn);
                    fieldChanges.Add(army.Id,(army,paid&&supplied?field.Movement.MovementPerTurn:Math.Max(1,field.Movement.MovementPerTurn/2)));
                }
                foreach(var city in owners.Settlements.Where(c=>c.OwnerFactionId==account.Key).OrderBy(c=>c.Id,StringComparer.Ordinal))
                {
                    var guard=military.Garrison(city.Id);guardChanges.Add(city.Id,Recover(guard,true,supply.Contains(city.Position),true,0));
                }
            }
            return new CampaignLogisticsResult(armies.BeginTurn().ApplyLogistics(fieldChanges),military.ApplyLogistics(guardChanges),nextStocks,reports);
        }
    }
    public sealed partial class CampaignArmyRegistry
    {
        internal CampaignArmyRegistry ApplyLogistics(Dictionary<string,(CampaignArmyState Army,int Movement)> changes)
        {
            var next=new CampaignArmyRegistry(this);
            foreach(var pair in changes)
            {
                var old=armies[pair.Key];
                next.armies[pair.Key]=new CampaignArmyOnMap(pair.Value.Army,ArmyMovementState.Restore(graph,old.Movement.Position,old.Movement.MovementPerTurn,pair.Value.Movement));
            }
            return next;
        }
    }
    public sealed partial class CampaignMilitaryState
    {
        internal void ValidateLogisticsOwners(CampaignSettlementRegistry expected)
        {
            if(expected.Settlements.Count!=owners.Settlements.Count)throw new ArgumentException("Logistics ownership does not match the military state.");
            foreach(var city in expected.Settlements)
                if(!owners.TryGet(city.Id,out var actual)||actual.Position!=city.Position||actual.OwnerFactionId!=city.OwnerFactionId)
                    throw new ArgumentException("Logistics ownership does not match the military state.");
        }
        internal CampaignMilitaryState ApplyLogistics(Dictionary<string,CampaignArmyState> changes)
        {var next=new CampaignMilitaryState(this);foreach(var pair in changes)next.guards[pair.Key]=pair.Value;return next;}
    }
    public sealed partial class CampaignSaveData
    {
        public int logisticsVersion;
        public void IncludeLogistics(bool enabled)
        {
            if(version!=9)throw new ArgumentException("Logistics requires campaign v9 data.");
            if(!enabled)return;if(!multipleArmies)throw new ArgumentException("Logistics requires multiple armies.");
            logisticsVersion=1;context=LogisticsSignature(context);version=10;
        }
        public static string LogisticsSignature(string context)
        {
            if(string.IsNullOrEmpty(context))throw new ArgumentException("Missing campaign identity.");
            ulong hash=14695981039346656037UL;
            foreach(char c in "logistics-v10:range6:grain20:cavalry2:gold25:heal2:morale5:shortage10:half-move:"+context)hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
        public void RestoreLogistics(bool enabled)
        {
            if(enabled&&!multipleArmies)throw new ArgumentException("Military logistics requires multiple-army mode.");
            if(version==11){if(logisticsVersion!=(enabled?1:0))throw new ArgumentException("Incompatible logistics rules.");return;}
            if(version==10){if(!enabled||!multipleArmies||logisticsVersion!=1)throw new ArgumentException("Incompatible logistics rules.");return;}
            if(version>10||logisticsVersion!=0||enabled&&version!=9)throw new ArgumentException("Unsupported logistics migration.");
            // Compatible v9 snapshots adopt upkeep on their next closing turn; no past turn is replayed.
        }
    }
}
