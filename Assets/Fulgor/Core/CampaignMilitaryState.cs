using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignMilitaryState
    {
        private readonly CampaignArmyRegistry initialArmies;
        private readonly CampaignSettlementRegistry owners;
        private readonly Dictionary<string,CampaignArmyState> guards;
        private readonly List<RecruitmentRecord> recruits;
        private readonly List<MusterRecord> musters;

        public CampaignMilitaryState(CampaignArmyRegistry initial,CampaignSettlementRegistry settlements)
        {
            initialArmies=initial;owners=settlements??throw new ArgumentNullException(nameof(settlements));
            guards=new(StringComparer.Ordinal);recruits=new();musters=new();
            if(initial!=null)foreach(var army in initial.Armies)
            {
                if(army.Army.Id.StartsWith("guard/",StringComparison.Ordinal)||army.Army.Id.StartsWith("muster/",StringComparison.Ordinal))
                    throw new ArgumentException("Initial army uses a reserved military ID.");
                foreach(var formation in army.Army.Formations)if(formation.Id.StartsWith("citytroop/",StringComparison.Ordinal))
                    throw new ArgumentException("Initial formation uses a reserved recruitment ID.");
            }
            foreach(var city in settlements.Settlements)
                guards.Add(city.Id,new CampaignArmyState("guard/"+city.Id,city.OwnerFactionId,Array.Empty<CampaignFormationState>()));
            FormationOrigins();
        }
        private CampaignMilitaryState(CampaignMilitaryState previous,CampaignSettlementRegistry newOwners=null)
        {
            initialArmies=previous.initialArmies;owners=newOwners??previous.owners;
            guards=new(previous.guards,StringComparer.Ordinal);
            recruits=previous.recruits.Select(r=>r.Copy()).ToList();musters=previous.musters.Select(r=>r.Copy()).ToList();
        }
        public CampaignArmyState Garrison(string cityId)=>cityId!=null&&guards.TryGetValue(cityId,out var guard)?guard:null;
        public RecruitmentRecord Pending(string cityId)=>recruits.FirstOrDefault(r=>r.settlementId==cityId&&r.status==RecruitmentStatus.Pending)?.Copy();
        public bool HasHostileGarrison(string cityId,string faction)=>Garrison(cityId) is var guard&&guard!=null&&!guard.IsDissolved&&guard.FactionId!=faction;
        private int StationedCount(CampaignArmyRegistry armies,CampaignSettlement city)=>armies!=null&&armies.TryGetOccupant(city.Position,out var army)?army.Army.Formations.Count:0;

        public bool TryRecruit(string cityId,string faction,TroopProfile profile,int turn,CampaignResources resources,
            CampaignArmyRegistry armies,out CampaignMilitaryState next,out CampaignResources remaining)
        {
            next=this;remaining=resources;
            if(armies==null||armies.HasActiveEncounters||turn<1||cityId==null||!owners.TryGet(cityId,out var city)||city.OwnerFactionId!=faction||
                Pending(cityId)!=null||Garrison(cityId).Formations.Count+StationedCount(armies,city)>=6)return false;
            if(armies.TryGetOccupant(city.Position,out var stationed)&&stationed.Army.FactionId!=faction)return false;
            var spec=RecruitmentCatalog.Get(profile);
            if(!resources.TrySpend(spec.Cost,out var paid))return false;
            var record=new RecruitmentRecord {serial=checked(recruits.Count+1),settlementId=cityId,factionId=faction,profile=profile,
                startTurn=turn,completionTurn=checked(turn+spec.Turns),status=RecruitmentStatus.Pending};
            if(FormationOrigins().ContainsKey(record.FormationId))return false;
            next=new CampaignMilitaryState(this);next.recruits.Add(record);remaining=paid;return true;
        }

        public CampaignMilitaryState Advance(int turn,CampaignArmyRegistry armies)
        {
            if(!recruits.Any(r=>r.status==RecruitmentStatus.Pending&&r.completionTurn<=turn))return this;
            if(armies==null||armies.HasActiveEncounters)throw new InvalidOperationException("Finish battles before recruitment.");
            var next=new CampaignMilitaryState(this);
            foreach(var record in next.recruits)
            {
                if(record.status!=RecruitmentStatus.Pending||record.completionTurn>turn)continue;
                owners.TryGet(record.settlementId,out var city);
                if(city.OwnerFactionId!=record.factionId){record.status=RecruitmentStatus.Cancelled;continue;}
                var guard=next.guards[city.Id];
                if(guard.Formations.Count+StationedCount(armies,city)>=6)throw new InvalidOperationException("Reserved recruitment slot is occupied.");
                next.guards[city.Id]=guard.WithFormations(guard.Formations.Concat(new[]{RecruitmentCatalog.Get(record.profile).Create(record.FormationId)}));
                record.status=RecruitmentStatus.Completed;
            }
            return next;
        }

        public CampaignMilitaryState WithOwners(CampaignSettlementRegistry nextOwners)
        {
            var next=new CampaignMilitaryState(this,nextOwners);bool changed=false;
            foreach(var city in nextOwners.Settlements)
            {
                var guard=guards[city.Id];
                if(guard.FactionId==city.OwnerFactionId)continue;
                if(!guard.IsDissolved)throw new InvalidOperationException("A surviving garrison blocks conquest.");
                next.guards[city.Id]=new CampaignArmyState(guard.Id,city.OwnerFactionId,Array.Empty<CampaignFormationState>());changed=true;
                foreach(var record in next.recruits.Where(r=>r.settlementId==city.Id&&r.status==RecruitmentStatus.Pending))record.status=RecruitmentStatus.Cancelled;
            }
            return changed?next:this;
        }

        public bool TryMove(CampaignArmyRegistry armies,string armyId,HexCoordinates target,out CampaignArmyRegistry next,out MapPath path,out CampaignArmyMoveFailure failure)
        {
            next=armies;path=null;
            if(armies==null||!armies.TryGetArmy(armyId,out var army)){failure=CampaignArmyMoveFailure.UnknownArmy;return false;}
            if(armies.HasActiveEncounters){failure=CampaignArmyMoveFailure.ArmyInBattle;return false;}
            if(owners.TryGet(target,out var city))
            {
                if(HasHostileGarrison(city.Id,army.Army.FactionId)){failure=CampaignArmyMoveFailure.GuardedSettlement;return false;}
                if(city.OwnerFactionId==army.Army.FactionId&&Garrison(city.Id).Formations.Count+army.Army.Formations.Count+(Pending(city.Id)!=null?1:0)>6)
                {failure=CampaignArmyMoveFailure.GarrisonCapacity;return false;}
            }
            var blocked=new HashSet<HexCoordinates>();
            foreach(var settlement in owners.Settlements)if(HasHostileGarrison(settlement.Id,army.Army.FactionId))blocked.Add(settlement.Position);
            return armies.TryMove(armyId,target,out next,out path,out failure,blocked);
        }

        private bool CanTransfer(string cityId,string armyId,CampaignArmyRegistry armies,out CampaignSettlement city,out CampaignArmyOnMap army)
        {
            city=null;army=null;
            if(armies==null||armies.HasActiveEncounters||cityId==null||!owners.TryGet(cityId,out city)||!armies.TryGetArmy(armyId,out army)||
                army.Army.FactionId!=city.OwnerFactionId||army.Movement.Position.DistanceTo(city.Position)>1)return false;
            return !armies.TryGetOccupant(city.Position,out var stationed)||stationed.Army.FactionId==city.OwnerFactionId;
        }

        public bool TryAssignToArmy(string cityId,string armyId,string formationId,CampaignArmyRegistry armies,
            out CampaignMilitaryState next,out CampaignArmyRegistry nextArmies)
        {
            next=this;nextArmies=armies;
            if(!CanTransfer(cityId,armyId,armies,out var city,out var army)||army.Army.Formations.Count>=6)return false;
            if(owners.TryGet(army.Movement.Position,out var destination)&&destination.Id!=cityId&&
                guards[destination.Id].Formations.Count+army.Army.Formations.Count+1+(Pending(destination.Id)!=null?1:0)>6)return false;
            var guard=guards[cityId];var formation=guard.Formations.FirstOrDefault(f=>f.Id==formationId);if(formation==null)return false;
            next=new CampaignMilitaryState(this);next.guards[cityId]=guard.WithFormations(guard.Formations.Where(f=>f.Id!=formationId));
            nextArmies=armies.ReplaceFormations(armyId,army.Army.Formations.Concat(new[]{formation}));return true;
        }

        public bool TryDeposit(string cityId,string armyId,string formationId,CampaignArmyRegistry armies,
            out CampaignMilitaryState next,out CampaignArmyRegistry nextArmies)
        {
            next=this;nextArmies=armies;
            if(!CanTransfer(cityId,armyId,armies,out var city,out var army))return false;
            var guard=guards[cityId];var formation=army.Army.Formations.FirstOrDefault(f=>f.Id==formationId);if(formation==null)return false;
            int staying=StationedCount(armies,city)-(army.Movement.Position==city.Position?1:0);
            if(guard.Formations.Count+1+staying+(Pending(cityId)!=null?1:0)>6)return false;
            next=new CampaignMilitaryState(this);next.guards[cityId]=guard.WithFormations(guard.Formations.Concat(new[]{formation}));
            nextArmies=armies.ReplaceFormations(armyId,army.Army.Formations.Where(f=>f.Id!=formationId));return true;
        }

        public bool TryMuster(string cityId,string faction,string formationId,CampaignArmyRegistry armies,
            out CampaignMilitaryState next,out CampaignArmyRegistry nextArmies,out string newArmyId)
        {
            next=this;nextArmies=armies;newArmyId=null;
            if(armies==null||armies.HasActiveEncounters||cityId==null||!owners.TryGet(cityId,out var city)||city.OwnerFactionId!=faction||
                armies.TryGetOccupant(city.Position,out _))return false;
            var guard=guards[cityId];var formation=guard.Formations.FirstOrDefault(f=>f.Id==formationId);if(formation==null)return false;
            var record=new MusterRecord {serial=checked(musters.Count+1),settlementId=cityId,factionId=faction};
            if(armies.TryGetArmy(record.ArmyId,out _)||initialArmies?.TryGetArmy(record.ArmyId,out _)==true)return false;
            next=new CampaignMilitaryState(this);next.musters.Add(record);next.guards[cityId]=guard.WithFormations(guard.Formations.Where(f=>f.Id!=formationId));
            nextArmies=armies.AddMuster(new CampaignArmyState(record.ArmyId,faction,new[]{formation}),city.Position);newArmyId=record.ArmyId;return true;
        }

        public CampaignMilitaryState WithGarrisonResult(string cityId,CampaignArmyState survivors)
        {
            if(survivors==null||!guards.TryGetValue(cityId,out var old)||old.Id!=survivors.Id||old.FactionId!=survivors.FactionId)throw new ArgumentException("Invalid garrison result.");
            var next=new CampaignMilitaryState(this);next.guards[cityId]=survivors;return next;
        }
    }
}
