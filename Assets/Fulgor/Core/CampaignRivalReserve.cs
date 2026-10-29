namespace Fulgor.Core.Campaign
{
    public static class CampaignRivalReserve
    {
        public const int ReserveTurns=2;
        public static bool CanAfford(CampaignResources stock,CampaignResources recruitment,CampaignResources upkeep)
        {
            if(!stock.TrySpend(recruitment,out var remaining))return false;
            return (long)remaining.Grain>=(long)upkeep.Grain*ReserveTurns&&(long)remaining.Gold>=(long)upkeep.Gold*ReserveTurns;
        }
        public static bool CanRecruit(CampaignResources stock,RecruitmentSpec spec,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,string faction)
        {
            var upkeep=CampaignArmyLogistics.UpkeepForFaction(armies,military,owners,faction);
            CampaignResources Batch(RecruitmentSpec batch)=>CampaignArmyLogistics.Upkeep(new CampaignArmyState("forecast",faction,new[]{batch.Create("forecast-troops")}));
            foreach(var city in owners.Settlements)
            {
                if(city.OwnerFactionId!=faction)continue;
                var pending=military.Pending(city.Id);
                if(pending!=null)upkeep=upkeep.Add(Batch(RecruitmentCatalog.Get(pending.profile)));
            }
            return CanAfford(stock,spec.Cost,upkeep.Add(Batch(spec)));
        }
    }
}
