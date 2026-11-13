using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public CampaignMilitaryState Military {get;private set;}
        private CampaignMilitaryState RestoreMilitary(CampaignSaveData data,CampaignSettlementRegistry owners)=>data.version>=6?
            CampaignMilitaryState.Restore(data.military,initialArmyRegistry,owners,KnownFactions(),data.turn):new CampaignMilitaryState(initialArmyRegistry,owners);
        private bool CanOrderMilitary(string cityId)=>State!=null&&registry!=null&&Military!=null&&!HasPendingBattle&&!IsRivalPhaseActive&&!IsCampaignFinished&&
            cityId!=null&&SettlementState.TryGet(cityId,out var city)&&city.OwnerFactionId==playerFaction;
        public bool CanRecruitProfile(TroopProfile profile)
        {
            if(!ResearchEnabled || playerFaction!="castilla")return true;
            return profile switch
            {
                TroopProfile.LightCavalry => Research.Has("frontier"),
                TroopProfile.Crossbowmen => Research.Has("crossbow_craft"),
                _ => true
            };
        }
        public bool TryRecruit(string cityId,TroopProfile profile)
        {
            if(!CanOrderMilitary(cityId)||!CanRecruitProfile(profile)||!Military.TryRecruit(cityId,playerFaction,profile,State.Turn,State.Resources,registry,out var next,out var paid,TileCity(cityId)?.HasMilitiaCamp==true))return false;
            Military=next;State=new CampaignTurnState(State.Turn,paid,State.IncomePerTurn,State.ExpensesPerTurn);CityRevision++;
            panel?.SetTurnState(State);RefreshArmy();return true;
        }
        public bool TryAssignGarrison(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TrySendToVisitor(cityId,selectedArmyId,formationId,false,registry,out var next,out var nextRegistry,out var visitorId))return false;
            marchOrders=marchOrders.Cancel(selectedArmyId);selectedArmyId=visitorId;PublishMilitary(next,nextRegistry);return true;
        }
        public bool TryLeaveGarrison(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryTransferStack(cityId,selectedArmyId,formationId,registry,true,out var next,out var nextRegistry))return false;
            if(!nextRegistry.TryGetArmy(selectedArmyId,out _))next=next.WithDepositedArmyHero(selectedArmyId,cityId,registry);
            marchOrders=marchOrders.Cancel(selectedArmyId);PublishMilitary(next,nextRegistry);return true;
        }
        public bool TryMuster(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryMusterStack(cityId,playerFaction,formationId,registry,out var next,out var nextRegistry,out var id))return false;
            selectedArmyId=id;PublishMilitary(next,nextRegistry);return true;
        }
        private void PublishMilitary(CampaignMilitaryState next,CampaignArmyRegistry nextRegistry)
        {
            Military=next;registry=nextRegistry;CityRevision++;
            if(!registry.TryGetArmy(selectedArmyId,out _))SelectFirstPlayerArmy();
            armyMarkers?.Initialize(selection,registry,playerFaction);RefreshArmy();
        }
    }
}


