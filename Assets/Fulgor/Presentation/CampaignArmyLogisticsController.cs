using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public bool LogisticsEnabled {get;private set;}
        public CampaignLogisticsResult LastLogisticsReport {get;private set;}
        public CampaignResources ArmyUpkeepPerTurn=>LogisticsEnabled?CampaignArmyLogistics.UpkeepForFaction(registry,Military,SettlementState,incomeFaction):default;
        private string LogisticsClosingText
        {
            get
            {
                if(LastLogisticsReport==null)return "";
                var own=LastLogisticsReport.Reports.Where(r=>r.FactionId==playerFaction).ToArray();
                int unpaid=own.Count(r=>!r.Paid),isolated=own.Count(r=>!r.Supplied);
                long healed=own.Sum(r=>r.Healing);
                return $" · Logística: {unpaid} sin pago, {isolated} sin suministro, {healed} PV recuperados";
            }
        }
        private CampaignLogisticsResult CloseMilitaryLogistics(CampaignRivalState closingRivals)
        {
            var funds=new Dictionary<string,CampaignResources>(System.StringComparer.Ordinal)
            {[playerFaction]=State.Resources.Add(State.IncomePerTurn).SubtractClamped(CityUpkeepPerTurn)};
            foreach(var faction in closingRivals.Factions)funds.Add(faction,closingRivals.Resources(faction));
            return CampaignArmyLogistics.CloseTurn(registry,Military,SettlementState,activeRoutes,funds);
        }
        private string ArmyLogisticsText(CampaignArmyOnMap army)
        {
            if(!LogisticsEnabled)return "";
            bool supplied=CampaignArmyLogistics.SupplyNetwork(activeRoutes,registry,SettlementState,army.Army.FactionId).Contains(army.Movement.Position);
            var cost=CampaignArmyLogistics.Upkeep(army.Army);
            var last=LastLogisticsReport?.Reports.FirstOrDefault(r=>r.ArmyId==army.Army.Id);
            return $"\n{(supplied?"Abastecida":"Sin suministro")} · Mantenimiento {cost.Grain} grano / {cost.Gold} oro"+
                (last!=null?last.Paid?" · Pagado":" · Pago incompleto":"");
        }
    }
}
