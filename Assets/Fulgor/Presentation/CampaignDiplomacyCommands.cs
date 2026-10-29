using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public CampaignDiplomacy Diplomacy {get;private set;}
        public bool CanManageDiplomacy=>State!=null&&registry!=null&&!HasPendingBattle&&!IsRivalPhaseActive&&!IsCampaignFinished;
        private void InitializeDiplomacy()
        {
            Diplomacy=new CampaignDiplomacy(incomeFaction,KnownFactions());
            if(GetComponent<CampaignDiplomacyView>()==null)gameObject.AddComponent<CampaignDiplomacyView>();
        }
        public bool SetDiplomaticRelation(string faction,DiplomaticRelation relation,DiplomaticIntention intention)
        {
            if(!CanManageDiplomacy||!Diplomacy.Factions.Contains(faction))return false;
            Diplomacy.SetRelation(faction,relation,intention);RefreshArmy();return true;
        }
        public bool ProposeDiplomacy(string faction,DiplomaticRelation type)
        {
            if(!CanManageDiplomacy||!Rivals.Factions.Contains(faction))return false;
            bool result=Diplomacy.Propose(faction,type,type==DiplomaticRelation.Trade?new CampaignResources(0,0,0,2,0):default,type==DiplomaticRelation.Trade?new CampaignResources(0,2,0,0,0):default);
            return result;
        }
        public bool RespondDiplomacy(string id,bool accept)=>CanManageDiplomacy&&Diplomacy.Respond(id,accept);
        private void CloseDiplomacy()
        {
            var stock=State.Resources;var rivals=Rivals;
            Diplomacy.CloseTurn(ref stock,ref rivals);Rivals=rivals;
            State=new CampaignTurnState(State.Turn,stock,State.IncomePerTurn,State.ExpensesPerTurn);
        }
        private bool DiplomacyAllowsTarget(string actor,HexCoordinates target)
        {
            if(Diplomacy==null)return true;
            if(SettlementState.TryGet(target,out var city)&&city.OwnerFactionId!=actor&&!Diplomacy.CanAttack(actor,city.OwnerFactionId))return false;
            if(registry!=null&&registry.TryGetOccupant(target,out var other)&&other.Army.FactionId!=actor&&!Diplomacy.CanAttack(actor,other.Army.FactionId))return false;
            return true;
        }
    }
}
