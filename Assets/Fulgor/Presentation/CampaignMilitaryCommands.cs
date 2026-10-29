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
        public bool TryRecruit(string cityId,TroopProfile profile)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryRecruit(cityId,playerFaction,profile,State.Turn,State.Resources,registry,out var next,out var paid))return false;
            Military=next;State=new CampaignTurnState(State.Turn,paid,State.IncomePerTurn,State.ExpensesPerTurn);CityRevision++;
            panel?.SetTurnState(State);RefreshArmy();return true;
        }
        public bool TryAssignGarrison(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryAssignToArmy(cityId,selectedArmyId,formationId,registry,out var next,out var nextRegistry))return false;
            marchOrders=marchOrders.Cancel(selectedArmyId);PublishMilitary(next,nextRegistry);return true;
        }
        public bool TryLeaveGarrison(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryDeposit(cityId,selectedArmyId,formationId,registry,out var next,out var nextRegistry))return false;
            marchOrders=marchOrders.Cancel(selectedArmyId);PublishMilitary(next,nextRegistry);return true;
        }
        public bool TryMuster(string cityId,string formationId)
        {
            if(!CanOrderMilitary(cityId)||!Military.TryMuster(cityId,playerFaction,formationId,registry,out var next,out var nextRegistry,out var id))return false;
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
