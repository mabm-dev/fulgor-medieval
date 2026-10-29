using System.Linq;
using Fulgor.Core.Map;
namespace Fulgor.Core.Campaign
{
    public static class CampaignRivalRecovery
    {
        internal static System.Collections.Generic.HashSet<HexCoordinates> Danger(string faction,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,CampaignFogState fog)
        {
            bool Seen(HexCoordinates at)=>fog==null||fog.IsVisible(faction,at);
            var threats=armies.Armies.Where(a=>a.Army.FactionId!=faction&&Seen(a.Movement.Position)).Select(a=>a.Movement.Position)
                .Concat(owners.Settlements.Where(c=>c.OwnerFactionId!=faction&&Seen(c.Position)&&military.HasHostileGarrison(c.Id,faction)).Select(c=>c.Position));
            var cells=new System.Collections.Generic.HashSet<HexCoordinates>();
            foreach(var enemy in threats){cells.Add(enemy);cells.UnionWith(enemy.Neighbors());}return cells;
        }
        // Rest for recoverable wounds or morale below 40; dead soldiers never count as wounds.
        public static bool TryDecide(string id,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,MapRouteGraph routes,CampaignFogState fog,bool paid,out RivalDecision decision)
        {
            decision=default;
            if(!paid||armies==null||armies.HasActiveEncounters||!armies.TryGetArmy(id,out var army)||army.Movement.RemainingMovement<1)return false;
            var origins=military.FormationOrigins();
            bool wounded=army.Army.Formations.Any(f=>origins.TryGetValue(f.Id,out var origin)&&f.Health<System.Math.Min(origin.Template.Health,(long)f.Quantity*f.Attributes.HealthPerMember));
            if(!wounded&&!army.Army.Formations.Any(f=>f.Morale<40))return false;
            var faction=army.Army.FactionId;
            var danger=Danger(faction,armies,military,owners,fog);
            bool Threatened(HexCoordinates at)=>danger.Contains(at);
            if(owners.TryGet(army.Movement.Position,out var current)&&current.OwnerFactionId==faction&&!Threatened(current.Position))return true;
            MapPath best=null;HexCoordinates target=default;
            foreach(var city in owners.Settlements.Where(c=>c.OwnerFactionId==faction).OrderBy(c=>c.Id,System.StringComparer.Ordinal))
            {
                if(Threatened(city.Position))continue;
                MapPath path;
                bool found=CampaignFogOrders.TryPlan(id,city.Position,armies,military,owners,routes,fog,out path,out _,true);
                if(found&&(best==null||path.TotalCost<best.TotalCost)){best=path;target=city.Position;}
            }
            if(best==null)return false;
            decision=new RivalDecision(RivalDecisionKind.Move,target);return true;
        }
    }
}
