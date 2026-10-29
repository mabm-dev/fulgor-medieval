using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private Dictionary<string,CityDevelopment> cities=new(StringComparer.Ordinal);
        private static readonly CityDevelopment InitialCity=new();
        private readonly Dictionary<string,string> cityCompletions=new(StringComparer.Ordinal);
        public CampaignResources TerritoryIncome { get; private set; }
        public CampaignResources CityIncomePerTurn=>SumCityEffects(cities,false);
        public CampaignResources CityUpkeepPerTurn=>SumCityEffects(cities,true);
        public int CityRevision { get; private set; }
        public string LastCompletedProject(string id)=>id!=null&&cityCompletions.TryGetValue(id,out var project)?project:null;
        public CityDevelopment CityState(string id)=>id!=null&&cities.TryGetValue(id,out var value)?value:InitialCity;
        public bool TryConstruct(string settlementId,string projectId)
        {
            var settlements=GetComponent<CampaignSettlementController>();
            if(State==null||IsCampaignFinished||HasPendingBattle||IsRivalPhaseActive||settlements==null||!settlements.CanManage(settlementId))return false;
            var current=CityState(settlementId);
            if(!current.TryStart(projectId,State.Turn,State.Resources,out var next,out var resources))return false;
            cities[settlementId]=next;State=new CampaignTurnState(State.Turn,resources,State.IncomePerTurn,State.ExpensesPerTurn);CityRevision++;
            panel?.SetTurnState(State);return true;
        }
        private void ResetCities(){cities.Clear();cityCompletions.Clear();CityRevision++;}
        private void AdvanceCities()
        {
            cityCompletions.Clear();
            foreach(var id in cities.Keys.ToArray()) {
                var next=cities[id].Advance(State.Turn);
                if(!ReferenceEquals(next,cities[id])){
                    cityCompletions[id]=cities[id].PendingId;cities[id]=next;CityRevision++;
                }
            }
            // The closing turn earns its previous income; newly completed projects
            // contribute to the next full turn. Always rebuild from territory income.
            State=WithCityEconomy(State,cities);
        }
        private CampaignResources SumCityEffects(Dictionary<string,CityDevelopment> developments,bool upkeep,CampaignSettlementRegistry owners=null)
        {
            owners??=SettlementState;
            var total=default(CampaignResources);
            foreach(var item in developments.OrderBy(p=>p.Key,StringComparer.Ordinal))
                if(owners!=null&&owners.TryGet(item.Key,out var city)&&city.OwnerFactionId==incomeFaction)
                    total=total.Add(upkeep?item.Value.UpkeepPerTurn:item.Value.IncomePerTurn);
            return total;
        }
        private CampaignTurnState WithCityEconomy(CampaignTurnState state,Dictionary<string,CityDevelopment> developments,CampaignSettlementRegistry owners=null,CampaignArmyRegistry armies=null,CampaignMilitaryState military=null)
        {
            owners??=SettlementState;
            var armyUpkeep=LogisticsEnabled?CampaignArmyLogistics.UpkeepForFaction(armies??registry,military??Military,owners,incomeFaction):default;
            return new CampaignTurnState(state.Turn,state.Resources,
                IncomeFrom(owners).Add(SumCityEffects(developments,false,owners)),SumCityEffects(developments,true,owners).Add(armyUpkeep));
        }
        private CityDevelopmentData[] CaptureCities()=>cities.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value.Capture(p.Key)).ToArray();
        private Dictionary<string,CityDevelopment> RestoreCities(CampaignSaveData save)
        {
            var known=GetComponent<CampaignSettlementController>()?.SettlementIds??Array.Empty<string>();
            return save.RestoreCities(known);
        }
    }
}
