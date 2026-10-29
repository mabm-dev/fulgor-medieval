using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum RivalDecisionKind { Wait, Move, Attack }
    public readonly struct RivalDecision
    {
        internal RivalDecision(RivalDecisionKind kind,HexCoordinates target){Kind=kind;Target=target;}
        public RivalDecisionKind Kind {get;}
        public HexCoordinates Target {get;}
    }
    /// <summary>Deterministic basic opponent; uses current visible campaign state and ordinary routes.</summary>
    public static partial class CampaignRivalPolicy
    {
        public const int MaximumFieldArmies=3;
        private static double Strength(CampaignArmyState army)=>army==null?0:army.Formations.Sum(f=>
            f.Health/(double)f.Attributes.HealthPerMember*(f.Attributes.Attack+f.Attributes.Defense+(f.Attributes.MinimumDamage+f.Attributes.MaximumDamage)/2.0));
        private static double Defense(CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,HexCoordinates at,string player)
        {
            double strength=armies.TryGetOccupant(at,out var field)&&field.Army.FactionId==player?Strength(field.Army):0;
            if(owners.TryGet(at,out var city)&&city.OwnerFactionId==player)strength+=Strength(military.Garrison(city.Id));return strength;
        }
        public static RivalDecision Decide(string armyId,string player,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners)
        {
            if(armies==null||armies.HasActiveEncounters||!armies.TryGetArmy(armyId,out var army)||army.Army.FactionId==player||army.Movement.RemainingMovement<1)return default;
            var threats=new HashSet<HexCoordinates>(armies.Armies.Where(a=>a.Army.FactionId==player).Select(a=>a.Movement.Position));
            foreach(var city in owners.Settlements)if(city.OwnerFactionId==player&&!military.Garrison(city.Id).IsDissolved)threats.Add(city.Position);
            var adjacent=threats.Where(at=>at.DistanceTo(army.Movement.Position)==1)
                .Select(at=>new {Position=at,Strength=Defense(armies,military,owners,at,player)})
                .OrderBy(t=>t.Strength).ThenBy(t=>t.Position.Q).ThenBy(t=>t.Position.R).FirstOrDefault();
            double own=Strength(army.Army);
            if(adjacent!=null&&own*1.25>=adjacent.Strength)return new RivalDecision(RivalDecisionKind.Attack,adjacent.Position);

            var goals=new List<(int Priority,long Cost,HexCoordinates At)>();
            void Consider(HexCoordinates target,int priority)
            {
                if(target==army.Movement.Position||!military.TryPlanMarch(armies,armyId,target,out var path,out _))return;
                goals.Add((priority,path.TotalCost,target));
            }
            foreach(var city in owners.Settlements.Where(c=>c.OwnerFactionId==player))
            {
                bool defended=threats.Contains(city.Position);
                if(!defended)Consider(city.Position,0);
                else if(army.Movement.Position.DistanceTo(city.Position)>1)
                    foreach(var neighbor in city.Position.Neighbors())Consider(neighbor,1);
            }
            foreach(var threat in threats)
                if(army.Movement.Position.DistanceTo(threat)>1)foreach(var neighbor in threat.Neighbors())Consider(neighbor,2);
            if(goals.Count==0)return default;
            var goal=goals.OrderBy(g=>g.Priority).ThenBy(g=>g.Cost).ThenBy(g=>g.At.Q).ThenBy(g=>g.At.R).First();
            return new RivalDecision(RivalDecisionKind.Move,goal.At);
        }
    }
}
