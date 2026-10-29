using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed class FogMarchResult
    {
        internal FogMarchResult(CampaignArmyRegistry armies,CampaignFogState fog,MapPath travelled,bool arrived,CampaignArmyMoveFailure failure)
        {Armies=armies;Fog=fog;Travelled=travelled;Arrived=arrived;Failure=failure;}
        public CampaignArmyRegistry Armies {get;}
        public CampaignFogState Fog {get;}
        public MapPath Travelled {get;}
        public bool Arrived {get;}
        public CampaignArmyMoveFailure Failure {get;}
    }
    public static class CampaignFogOrders
    {
        public static bool TryPlan(string armyId,HexCoordinates target,CampaignArmyRegistry armies,CampaignMilitaryState military,
            CampaignSettlementRegistry owners,MapRouteGraph graph,CampaignFogState fog,out MapPath route,out CampaignArmyMoveFailure failure,bool avoidThreats=false)
        {
            route=null;failure=CampaignArmyMoveFailure.Unreachable;
            if(armies==null||!armies.TryGetArmy(armyId,out var army)){failure=CampaignArmyMoveFailure.UnknownArmy;return false;}
            if(armies.HasActiveEncounters){failure=CampaignArmyMoveFailure.ArmyInBattle;return false;}
            string faction=army.Army.FactionId;
            if(fog!=null&&!fog.IsExplored(faction,target))return false;
            var blocked=fog==null?new HashSet<HexCoordinates>():new HashSet<HexCoordinates>(fog.Cells.Where(c=>!fog.IsExplored(faction,c)));
            foreach(var unit in armies.Armies)
                if(unit.Army.Id!=armyId&&(unit.Army.FactionId==faction||fog==null||fog.IsVisible(faction,unit.Movement.Position)))blocked.Add(unit.Movement.Position);
            foreach(var city in owners.Settlements)
            {
                if(fog!=null&&!fog.IsVisible(faction,city.Position))continue;
                if(military.HasHostileGarrison(city.Id,faction))blocked.Add(city.Position);
                if(city.OwnerFactionId==faction&&military.Garrison(city.Id).Formations.Count+army.Army.Formations.Count+(military.Pending(city.Id)!=null?1:0)>6)blocked.Add(city.Position);
            }
            if(avoidThreats)
            {
                var danger=CampaignRivalRecovery.Danger(faction,armies,military,owners,fog);
                // Allow leaving a threatened origin; never permit entering another threatened cell.
                danger.Remove(army.Movement.Position);blocked.UnionWith(danger);
            }
            if(blocked.Contains(target)){failure=CampaignArmyMoveFailure.Unreachable;return false;}
            bool found=MapPathfinder.TryFindPath(graph,army.Movement.Position,target,out route,blocked,army.Movement.MovementPerTurn);
            failure=found?CampaignArmyMoveFailure.None:CampaignArmyMoveFailure.Unreachable;return found;
        }
        public static FogMarchResult Advance(string armyId,HexCoordinates target,CampaignArmyRegistry armies,CampaignMilitaryState military,
            CampaignSettlementRegistry owners,MapRouteGraph graph,CampaignFogState fog,int turn,HexCoordinates? objective=null,bool avoidThreats=false)
        {
            if(armies.HasActiveEncounters)throw new InvalidOperationException("Resolve battles before fog movement.");
            if(!armies.TryGetArmy(armyId,out var actor))throw new ArgumentException("Unknown army.");
            var moved=armies;var observed=fog?.Refresh(armies,owners,turn);var cells=new List<HexCoordinates>{actor.Movement.Position};long cost=0;
            var failure=CampaignArmyMoveFailure.None;
            // Every accepted prefix spends movement. Observation occurs before the next order is planned.
            while(moved.TryGetArmy(armyId,out actor)&&actor.Movement.Position!=target&&actor.Movement.RemainingMovement>0)
            {
                if(!TryPlan(armyId,target,moved,military,owners,graph,observed,out var path,out failure,avoidThreats)||path.Steps==0)break;
                var next=path.Cells[1];
                if(!graph.TryGetMovementCost(actor.Movement.Position,next,out int step)||step>actor.Movement.RemainingMovement)break;
                if(!military.TryMove(moved,armyId,next,out var advanced,out var travelled,out failure))break;
                if(travelled==null||travelled.Steps==0)break;
                moved=advanced;cost+=travelled.TotalCost;for(int i=1;i<travelled.Cells.Count;i++)cells.Add(travelled.Cells[i]);
                observed=observed?.Refresh(moved,owners,turn);
                // Occupation must be published by the campaign before another move is attempted.
                if(objective==next||owners.TryGet(next,out var city)&&city.OwnerFactionId!=actor.Army.FactionId)break;
            }
            moved.TryGetArmy(armyId,out actor);
            return new FogMarchResult(moved,observed,new MapPath(cells,cost),actor.Movement.Position==target,failure);
        }
    }
    public sealed partial class CampaignMarchOrders
    {
        internal CampaignMarchOrders RememberKnownOrder(string armyId,HexCoordinates target)
        {var next=new CampaignMarchOrders(this);next.orders[armyId]=target;return next;}
        public bool TryKnownOrder(string armyId,HexCoordinates target,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,
            MapRouteGraph graph,CampaignFogState fog,string faction,out CampaignMarchOrders next)
        {
            next=this;
            if(!armies.TryGetArmy(armyId,out var actor)||actor.Army.FactionId!=faction||actor.Movement.Position==target||
                !CampaignFogOrders.TryPlan(armyId,target,armies,military,owners,graph,fog,out _,out _))return false;
            next=RememberKnownOrder(armyId,target);return true;
        }
    }
    public static partial class CampaignRivalPolicy
    {
        public static RivalDecision DecideWithFog(string armyId,string player,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,
            MapRouteGraph graph,CampaignFogState fog)
        {
            if(armies==null||armies.HasActiveEncounters||!armies.TryGetArmy(armyId,out var army)||army.Army.FactionId==player||army.Movement.RemainingMovement<1)return default;
            string faction=army.Army.FactionId;
            var threats=new HashSet<HexCoordinates>(armies.Armies.Where(a=>a.Army.FactionId==player&&fog.IsVisible(faction,a.Movement.Position)).Select(a=>a.Movement.Position));
            foreach(var city in owners.Settlements)if(city.OwnerFactionId==player&&fog.IsVisible(faction,city.Position)&&!military.Garrison(city.Id).IsDissolved)threats.Add(city.Position);
            var adjacent=threats.Where(at=>at.DistanceTo(army.Movement.Position)==1).OrderBy(at=>Defense(armies,military,owners,at,player)).ThenBy(at=>at.Q).ThenBy(at=>at.R).ToArray();
            if(adjacent.Length>0&&Strength(army.Army)*1.25>=Defense(armies,military,owners,adjacent[0],player))return new RivalDecision(RivalDecisionKind.Attack,adjacent[0]);
            var goals=new List<(int Priority,long Cost,HexCoordinates At)>();
            void Consider(HexCoordinates at,int priority)
            {
                if(at==army.Movement.Position||!CampaignFogOrders.TryPlan(armyId,at,armies,military,owners,graph,fog,out var path,out _))return;
                goals.Add((priority,path.TotalCost,at));
            }
            foreach(var intel in fog.SettlementIntel(faction).Where(c=>c.OwnerFactionId==player))
            {
                owners.TryGet(intel.SettlementId,out var city);Consider(city.Position,0);
                foreach(var neighbor in city.Position.Neighbors())Consider(neighbor,1);
            }
            foreach(var threat in threats)foreach(var n in threat.Neighbors())Consider(n,2);
            // Expand from observed frontier cells; no query of hidden troops or unknown terrain costs.
            foreach(var cell in fog.Cells)
                if(fog.IsVisible(faction,cell)&&cell.Neighbors().Any(n=>fog.ContainsCell(n)&&!fog.IsExplored(faction,n)))Consider(cell,3);
            if(goals.Count==0)return default;
            var goal=goals.OrderBy(g=>g.Priority).ThenBy(g=>g.Priority==3?-g.Cost:g.Cost).ThenBy(g=>g.At.Q).ThenBy(g=>g.At.R).First();
            return new RivalDecision(RivalDecisionKind.Move,goal.At);
        }
    }
}
