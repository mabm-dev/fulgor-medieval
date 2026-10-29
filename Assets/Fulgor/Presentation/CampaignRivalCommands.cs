using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private string[] rivalActors;
        private int rivalActorIndex,rivalMoves,rivalRecruitments;
        private bool rivalsConfigured;
        public CampaignRivalState Rivals {get;private set;}
        public bool IsRivalPhaseActive=>rivalActors!=null;
        public string LastRivalSummary {get;private set;}
        private IEnumerable<string> RivalFactions()=>KnownFactions().Where(id=>id!=incomeFaction).OrderBy(id=>id,StringComparer.Ordinal);
        private void InitializeRivals(bool enabled)
        {
            rivalsConfigured=enabled&&registry!=null;rivalActors=null;rivalActorIndex=0;LastRivalSummary="";
            Rivals=new CampaignRivalState(rivalsConfigured,RivalFactions());
        }
        private void BeginRivalPhase()
        {
            if(!Rivals.Enabled||IsCampaignFinished)return;
            rivalMoves=0;rivalRecruitments=0;bool created=false;
            foreach(var faction in Rivals.Factions)
            {
                foreach(var city in SettlementState.Settlements.Where(c=>c.OwnerFactionId==faction).OrderBy(c=>c.Id,StringComparer.Ordinal))
                {
                    var guard=Military.Garrison(city.Id);
                    if(!guard.IsDissolved)
                    {
                        var formation=guard.Formations[0];
                        if(registry.TryGetOccupant(city.Position,out var field)&&field.Army.FactionId==faction)
                        {
                            if(Military.TryAssignToArmy(city.Id,field.Army.Id,formation.Id,registry,out var next,out var armies))
                            {Military=next;registry=armies;}
                        }
                        else
                        {
                            int count=registry.Armies.Count(a=>a.Army.FactionId==faction);
                            if(count<CampaignRivalPolicy.MaximumFieldArmies&&(guard.Formations.Count>1||count==0)&&
                                Military.TryMuster(city.Id,faction,formation.Id,registry,out var next,out var armies,out _))
                            {Military=next;registry=armies;created=true;}
                        }
                    }
                    // Rotate the preferred profile, then try other affordable ordinary batches.
                    for(int n=0;n<RecruitmentCatalog.All.Count;n++)
                    {
                        var spec=RecruitmentCatalog.All[(State.Turn+n)%RecruitmentCatalog.All.Count];
                        if(LogisticsEnabled&&!CampaignRivalReserve.CanRecruit(Rivals.Resources(faction),spec,registry,Military,SettlementState,faction))continue;
                        if(!Military.TryRecruit(city.Id,faction,spec.Profile,State.Turn,Rivals.Resources(faction),registry,out var next,out var stock))continue;
                        Military=next;Rivals=Rivals.WithResources(faction,stock);rivalRecruitments++;break;
                    }
                }
            }
            CityRevision++;if(created)armyMarkers?.Initialize(selection,registry,playerFaction);else armyMarkers?.SetRegistry(registry);
            rivalActors=registry.Armies.Where(a=>Rivals.Factions.Contains(a.Army.FactionId)).Select(a=>a.Army.Id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
            rivalActorIndex=0;ContinueRivalPhase();
        }
        private void ContinueRivalPhase()
        {
            if(!IsRivalPhaseActive||HasPendingBattle)return;
            while(IsRivalPhaseActive&&rivalActorIndex<rivalActors.Length)
            {
                // Consume the actor before opening a battle; returning continues with the next actor.
                var id=rivalActors[rivalActorIndex++];
                if(!registry.TryGetArmy(id,out var actor))continue;
                RefreshFog();
                var decision=FogEnabled?CampaignRivalPolicy.DecideWithFog(id,playerFaction,registry,Military,SettlementState,activeRoutes,Fog):CampaignRivalPolicy.Decide(id,playerFaction,registry,Military,SettlementState);
                var report=LastLogisticsReport?.Reports.FirstOrDefault(r=>r.ArmyId==id&&!r.IsGarrison);
                bool recovering=false;
                if(LogisticsEnabled&&report!=null&&CampaignRivalRecovery.TryDecide(id,registry,Military,SettlementState,activeRoutes,FogEnabled?Fog:null,report.Paid,out var recovery)){decision=recovery;recovering=true;}
                if(!recovering&&!Diplomacy.CanAttack(actor.Army.FactionId,playerFaction))continue;
                if(!DiplomacyAllowsTarget(actor.Army.FactionId,decision.Target))continue;
                if(decision.Kind==RivalDecisionKind.Attack&&BeginRivalAttack(id,decision.Target))return;
                if(decision.Kind!=RivalDecisionKind.Move)continue;
                if(FogEnabled||recovering){if(AdvanceFogRival(id,decision.Target,recovering))rivalMoves++;continue;}
                var orders=new CampaignMarchOrders();
                if(!orders.TryOrder(id,decision.Target,registry,Military,actor.Army.FactionId,out orders,out _,out _))continue;
                orders.AdvanceArmy(id,registry,Military,actor.Army.FactionId,out var moved,out var result);
                registry=moved;
                if(result?.Travelled?.Steps>0)
                {rivalMoves++;armyMarkers?.SetRegistry(registry,id,result.Travelled.Cells);CaptureForArmy(id);}
            }
            if(IsCampaignFinished){rivalActors=null;RefreshArmy();return;}
            rivalActors=null;CityRevision++;
            LastRivalSummary=(FogEnabled?"Turno rival completado":$"Turno rival completado · {rivalMoves} marchas · {rivalRecruitments} levas")+LogisticsClosingText;
            panel?.SetSaveStatus(LastRivalSummary,true);RefreshArmy();
        }
        private bool BeginRivalAttack(string attackerId,HexCoordinates target)
        {
            registry.TryGetArmy(attackerId,out var attacker);
            if(attacker==null||!DiplomacyAllowsTarget(attacker.Army.FactionId,target))return false;
            bool guarded=SettlementState.TryGet(target,out var city)&&Military.HasHostileGarrison(city.Id,attacker.Army.FactionId);
            registry.TryGetOccupant(target,out var field);
            if(!guarded&&(field==null||field.Army.FactionId!=playerFaction))return false;
            string id;int suffix=1;
            do{id=$"turn-{State.Turn}-rival-{suffix++}";}while(registry.HasResolvedEncounter(id)||registry.TryGetEncounter(id,out _));
            uint seed=2166136261;foreach(char c in saveContext+":"+id+":"+attackerId)seed=unchecked((seed^c)*16777619);
            CampaignArmyRegistry next;
            bool started=guarded?registry.TryBeginGarrisonAttack(id,attackerId,city.Id,Military.Garrison(city.Id),target,seed,out next,out _):
                registry.TryBeginAttack(id,attackerId,field.Army.Id,seed,out next,out _);
            if(!started)return false;
            registry=next;ActiveEncounterId=id;LastBattleReport=null;
            if(field!=null&&field.Army.FactionId==playerFaction)marchOrders=marchOrders.Cancel(field.Army.Id);
            GetComponent<CityManagementView>()?.Close();RefreshArmy();ResumeBattle();return true;
        }
    }
}
