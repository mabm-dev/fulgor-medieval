using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public CampaignResearch Research { get; private set; }=new();
        public bool ResearchEnabled=>GetComponent<CampaignPlaySession>()!=null;
        public int ResearchCityCount=>SettlementState?.Settlements.Count(c=>c.OwnerFactionId==incomeFaction)??0;
        public int LargestResearchCity=>SettlementState?.Settlements.Where(c=>c.OwnerFactionId==incomeFaction)
            .Select(c=>TileCity(c.Id)?.Population??CityTileDevelopment.StartingPopulation).DefaultIfEmpty(0).Max()??0;
        public int ResearchKnowledgeCities=>SettlementState?.Settlements.Count(c=>c.OwnerFactionId==incomeFaction && TileCity(c.Id)?.HasKnowledgeHouse==true)??0;
        public int WisdomPerTurn=>Research.WisdomPerTurn(ResearchCityCount)+ResearchKnowledgeCities;
        public string ResearchBlockReason(string id)
        {
            if(!ResearchEnabled||State==null||IsCampaignFinished||HasPendingBattle||IsRivalPhaseActive)return "Resuelve el turno actual antes de investigar.";
            if(id=="crossbow_craft" && playerFaction!="castilla")return "Investigación de Castilla.";
            var reason=Research.BlockReason(id,LargestResearchCity,ResearchCityCount);
            if(reason!=null)return reason;
            return State.Resources.TrySpend(CampaignTechnologies.Get(id).Cost,out _)?null:"Recursos insuficientes.";
        }
        public bool TryStartResearch(string id)
        {
            if(ResearchBlockReason(id)!=null)return false;
            var cost=CampaignTechnologies.Get(id).Cost;
            if(!State.Resources.TrySpend(cost,out var remaining)||!Research.TryStart(id,LargestResearchCity,ResearchCityCount))return false;
            State=new CampaignTurnState(State.Turn,remaining,State.IncomePerTurn,State.ExpensesPerTurn);
            CityRevision++;panel?.SetTurnState(State);return true;
        }
        private void AdvanceResearch(int wisdom)
        {
            if(ResearchEnabled&&Research.Advance(wisdom))CityRevision++;
        }
        private CampaignResources ResearchIncome(CampaignSettlementRegistry owners,CampaignResearch research)
        {
            var total=default(CampaignResources);
            if(!ResearchEnabled||owners==null)return total;
            foreach(var city in owners.Settlements)
                if(city.OwnerFactionId==incomeFaction)total=total.Add(research.IncomePerCity);
            return total;
        }
    }
}
