using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    [Serializable]public sealed class CampaignMarchData
    {
        public string armyId;public int q,r;
    }
    public enum MarchStepStatus { Moving, Arrived, WaitingForMovement, Blocked, Removed }
    public sealed class MarchStepResult
    {
        internal MarchStepResult(string id,MarchStepStatus status,MapPath travelled,CampaignArmyMoveFailure failure)
        {ArmyId=id;Status=status;Travelled=travelled;Failure=failure;}
        public string ArmyId {get;}
        public MarchStepStatus Status {get;}
        public MapPath Travelled {get;}
        public CampaignArmyMoveFailure Failure {get;}
    }
    /// <summary>Immutable, per-army destinations. Routes are recomputed against live occupancy.</summary>
    public sealed partial class CampaignMarchOrders
    {
        private readonly SortedDictionary<string,HexCoordinates> orders;
        public CampaignMarchOrders(){orders=new(StringComparer.Ordinal);}
        private CampaignMarchOrders(CampaignMarchOrders previous){orders=new(previous.orders,StringComparer.Ordinal);}
        public int Count=>orders.Count;
        public IReadOnlyList<string> ArmyIds=>Array.AsReadOnly(orders.Keys.ToArray());
        public HexCoordinates? Destination(string armyId)=>armyId!=null&&orders.TryGetValue(armyId,out var target)?target:null;
        public CampaignMarchOrders Cancel(string armyId)
        {
            if(armyId==null||!orders.ContainsKey(armyId))return this;
            var next=new CampaignMarchOrders(this);next.orders.Remove(armyId);return next;
        }
        public CampaignMarchOrders Prune(CampaignArmyRegistry armies,string faction)
        {
            var next=this;
            foreach(var id in orders.Keys)if(armies==null||!armies.TryGetArmy(id,out var army)||army.Army.FactionId!=faction)next=next.Cancel(id);
            return next;
        }
        public bool TryOrder(string armyId,HexCoordinates destination,CampaignArmyRegistry armies,CampaignMilitaryState military,string faction,
            out CampaignMarchOrders next,out MapPath route,out CampaignArmyMoveFailure failure)
        {
            next=this;route=null;
            if(armies==null||!armies.TryGetArmy(armyId,out var army)||army.Army.FactionId!=faction)
            {failure=CampaignArmyMoveFailure.UnknownArmy;return false;}
            if(!military.TryPlanMarch(armies,armyId,destination,out route,out failure)||destination==army.Movement.Position)return false;
            next=new CampaignMarchOrders(this);next.orders[armyId]=destination;return true;
        }
        public CampaignMarchOrders AdvanceArmy(string armyId,CampaignArmyRegistry armies,CampaignMilitaryState military,string faction,
            out CampaignArmyRegistry moved,out MarchStepResult result,HexCoordinates? objective=null)
        {
            moved=armies;result=null;
            if(!Destination(armyId).HasValue)return this;
            if(armies.HasActiveEncounters)throw new InvalidOperationException("Resolve battles before advancing marches.");
            if(!armies.TryGetArmy(armyId,out var army)||army.Army.FactionId!=faction)
            {result=new MarchStepResult(armyId,MarchStepStatus.Removed,null,CampaignArmyMoveFailure.UnknownArmy);return Cancel(armyId);}
            var destination=orders[armyId];
            if(army.Movement.Position==destination)
            {result=new MarchStepResult(armyId,MarchStepStatus.Arrived,null,CampaignArmyMoveFailure.None);return Cancel(armyId);}
            if(!military.TryPlanMarch(armies,armyId,destination,out var plan,out var failure))
            {result=new MarchStepResult(armyId,MarchStepStatus.Blocked,null,failure);return this;}
            var prefix=new List<HexCoordinates>{army.Movement.Position};long cost=0;
            for(int i=1;i<plan.Cells.Count;i++)
            {
                var at=plan.Cells[i];armies.MarchStepCost(prefix[prefix.Count-1],at,out var step);
                if(step>army.Movement.RemainingMovement-cost)break;
                cost+=step;prefix.Add(at);
                // Entering an unowned fort stops here so the campaign can publish its conquest.
                if(objective==at||military.StopsMarchAt(at,faction))break;
            }
            var travelled=new MapPath(prefix,cost);
            moved=armies.MoveMarchPrefix(armyId,travelled);
            bool arrived=prefix[prefix.Count-1]==destination;
            result=new MarchStepResult(armyId,arrived?MarchStepStatus.Arrived:prefix.Count==1?MarchStepStatus.WaitingForMovement:MarchStepStatus.Moving,travelled,CampaignArmyMoveFailure.None);
            return arrived?Cancel(armyId):this;
        }
        public CampaignMarchData[] Capture()=>orders.Select(p=>new CampaignMarchData{armyId=p.Key,q=p.Value.Q,r=p.Value.R}).ToArray();
        public static CampaignMarchOrders Restore(CampaignMarchData[] data,CampaignArmyRegistry armies,string faction)
        {
            if(data==null||armies==null)throw new ArgumentException("Missing march orders.");
            var next=new CampaignMarchOrders();
            foreach(var saved in data)
            {
                if(saved?.armyId==null||next.orders.ContainsKey(saved.armyId)||!armies.TryGetArmy(saved.armyId,out var army)||army.Army.FactionId!=faction)
                    throw new ArgumentException("Unknown, foreign or duplicate march army.");
                var target=new HexCoordinates(saved.q,saved.r);
                // A blocked destination is a valid paused order. Validate terrain, not temporary occupants.
                if(target==army.Movement.Position||!armies.HasMarchTerrainRoute(saved.armyId,target))throw new ArgumentException("Invalid march destination.");
                next.orders.Add(saved.armyId,target);
            }
            return next;
        }
    }
    public sealed partial class CampaignArmyRegistry
    {
        internal bool MarchStepCost(HexCoordinates from,HexCoordinates to,out int cost)=>graph.TryGetMovementCost(from,to,out cost);
        internal bool HasMarchTerrainRoute(string armyId,HexCoordinates target)=>TryGetArmy(armyId,out var army)&&
            MapPathfinder.TryFindPath(graph,army.Movement.Position,target,out _,maximumStepCost:army.Movement.MovementPerTurn);
        internal bool TryPlanMarch(string id,HexCoordinates target,ISet<HexCoordinates> extraBlocked,out MapPath path,out CampaignArmyMoveFailure failure)
        {
            path=null;
            if(!TryGetArmy(id,out var army)){failure=CampaignArmyMoveFailure.UnknownArmy;return false;}
            if(HasActiveEncounters){failure=CampaignArmyMoveFailure.ArmyInBattle;return false;}
            if(TryGetOccupant(target,out var other)&&other.Army.Id!=id){failure=CampaignArmyMoveFailure.Occupied;return false;}
            var blocked=new HashSet<HexCoordinates>(extraBlocked);
            foreach(var unit in Armies)if(unit.Army.Id!=id)blocked.Add(unit.Movement.Position);
            bool found=MapPathfinder.TryFindPath(graph,army.Movement.Position,target,out path,blocked,army.Movement.MovementPerTurn);
            failure=found?CampaignArmyMoveFailure.None:CampaignArmyMoveFailure.Unreachable;return found;
        }
        internal CampaignArmyRegistry MoveMarchPrefix(string id,MapPath path)
        {
            var army=armies[id];if(path.Steps==0)return this;
            var next=new CampaignArmyRegistry(this);
            next.armies[id]=new CampaignArmyOnMap(army.Army,ArmyMovementState.Restore(graph,path.Cells[path.Cells.Count-1],army.Movement.MovementPerTurn,
                checked(army.Movement.RemainingMovement-(int)path.TotalCost)));return next;
        }
    }
    public sealed partial class CampaignMilitaryState
    {
        internal bool StopsMarchAt(HexCoordinates position,string faction)=>owners.TryGet(position,out var city)&&city.OwnerFactionId!=faction;
        public bool TryPlanMarch(CampaignArmyRegistry armies,string armyId,HexCoordinates target,out MapPath path,out CampaignArmyMoveFailure failure)
        {
            path=null;
            if(armies==null||!armies.TryGetArmy(armyId,out var army)){failure=CampaignArmyMoveFailure.UnknownArmy;return false;}
            var blocked=new HashSet<HexCoordinates>();
            foreach(var city in owners.Settlements)
                if(HasHostileGarrison(city.Id,army.Army.FactionId)||city.OwnerFactionId==army.Army.FactionId&&
                    Garrison(city.Id).Formations.Count+army.Army.Formations.Count+(Pending(city.Id)!=null?1:0)>6)blocked.Add(city.Position);
            if(blocked.Contains(target))
            {failure=owners.TryGet(target,out var city)&&HasHostileGarrison(city.Id,army.Army.FactionId)?CampaignArmyMoveFailure.GuardedSettlement:CampaignArmyMoveFailure.GarrisonCapacity;return false;}
            return armies.TryPlanMarch(armyId,target,blocked,out path,out failure);
        }
    }
}
