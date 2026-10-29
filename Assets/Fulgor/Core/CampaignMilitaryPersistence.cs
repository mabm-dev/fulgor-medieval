using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignMilitaryState
    {
        internal Dictionary<string,FormationOrigin> FormationOrigins()
        {
            var origins=new Dictionary<string,FormationOrigin>(StringComparer.Ordinal);
            if(initialArmies!=null)foreach(var army in initialArmies.Armies)
                foreach(var formation in army.Army.Formations)
                    if(!origins.TryAdd(formation.Id,new FormationOrigin(army.Army.FactionId,formation)))
                        throw new ArgumentException("Formation IDs must be unique across the scenario.");
            foreach(var recruit in recruits)
            {
                if(origins.ContainsKey(recruit.FormationId))throw new ArgumentException("Reserved recruit ID collision.");
                if(recruit.status==RecruitmentStatus.Completed)
                    origins.Add(recruit.FormationId,new FormationOrigin(recruit.factionId,RecruitmentCatalog.Get(recruit.profile).Create(recruit.FormationId)));
            }
            return origins;
        }
        internal MusterRecord MusterOrigin(string id)=>musters.FirstOrDefault(m=>m.ArmyId==id)?.Copy();
        internal CampaignSettlement Settlement(string id)=>owners.TryGet(id,out var city)?city:null;
        internal static CampaignFormationState RestoreFormation(CampaignFormationData data,string faction,Dictionary<string,FormationOrigin> origins)
        {
            if(data==null||data.id==null||!origins.TryGetValue(data.id,out var origin)||origin.FactionId!=faction||data.health>origin.Template.Health)
                throw new ArgumentException("Unknown formation, faction mismatch or excessive strength.");
            var template=origin.Template;
            return new CampaignFormationState(data.id,template.Attributes,data.health,data.morale,template.Kind);
        }
        private static HashSet<string> History(string[] entries)
        {
            if(entries==null)throw new ArgumentException("Missing garrison history.");
            var result=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in entries)if(string.IsNullOrWhiteSpace(id)||!result.Add(id))throw new ArgumentException("Invalid garrison history.");
            return result;
        }
        public MilitarySaveData Capture()=>new MilitarySaveData {
            recruits=recruits.Select(r=>r.Copy()).ToArray(),musters=musters.Select(m=>m.Copy()).ToArray(),
            garrisons=guards.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new GarrisonData {
                settlementId=p.Key,appliedEncounters=p.Value.CaptureEncounterHistory(),
                formations=p.Value.Formations.Select(f=>new CampaignFormationData{id=f.Id,health=f.Health,morale=f.Morale}).ToArray()}).ToArray()};

        public static CampaignMilitaryState Restore(MilitarySaveData data,CampaignArmyRegistry initial,CampaignSettlementRegistry settlements,
            IEnumerable<string> knownFactions,int turn)
        {
            if(data?.recruits==null||data.musters==null||data.garrisons==null||turn<1)throw new ArgumentException("Missing military state.");
            var next=new CampaignMilitaryState(initial,settlements);var factions=new HashSet<string>(knownFactions,StringComparer.Ordinal);
            var pending=new HashSet<string>(StringComparer.Ordinal);
            foreach(var record in data.recruits)
            {
                if(record==null||record.serial!=next.recruits.Count+1||record.settlementId==null||!settlements.TryGet(record.settlementId,out var city)||
                    record.factionId==null||!factions.Contains(record.factionId)||record.startTurn<1||record.startTurn>turn||
                    !Enum.IsDefined(typeof(RecruitmentStatus),record.status))throw new ArgumentException("Invalid recruitment record.");
                var spec=RecruitmentCatalog.Get(record.profile);
                if(record.completionTurn!=checked(record.startTurn+spec.Turns))throw new ArgumentException("Invalid recruitment duration.");
                if(record.status==RecruitmentStatus.Pending&&(record.completionTurn<=turn||record.factionId!=city.OwnerFactionId||!pending.Add(city.Id)))
                    throw new ArgumentException("Expired or duplicate pending recruitment.");
                if(record.status==RecruitmentStatus.Completed&&record.completionTurn>turn)throw new ArgumentException("Recruitment completed before its turn.");
                next.recruits.Add(record.Copy());
            }
            foreach(var record in data.musters)
            {
                if(record==null||record.serial!=next.musters.Count+1||record.settlementId==null||!settlements.TryGet(record.settlementId,out _)||
                    record.factionId==null||!factions.Contains(record.factionId)||initial?.TryGetArmy(record.ArmyId,out _)==true)
                    throw new ArgumentException("Invalid mustered army origin.");
                next.musters.Add(record.Copy());
            }
            if(initial==null&&(next.recruits.Count!=0||next.musters.Count!=0))throw new ArgumentException("Military orders require multiple-army mode.");
            var origins=next.FormationOrigins();var ids=new HashSet<string>(StringComparer.Ordinal);var formations=new HashSet<string>(StringComparer.Ordinal);
            foreach(var saved in data.garrisons)
            {
                if(saved?.settlementId==null||!settlements.TryGet(saved.settlementId,out var city)||!ids.Add(city.Id)||saved.formations==null)
                    throw new ArgumentException("Unknown or duplicate garrison.");
                var survivors=new List<CampaignFormationState>();
                foreach(var savedFormation in saved.formations)
                {
                    var formation=RestoreFormation(savedFormation,city.OwnerFactionId,origins);
                    if(!formations.Add(formation.Id))throw new ArgumentException("Formation occupies multiple garrisons.");
                    survivors.Add(formation);
                }
                next.guards[city.Id]=CampaignArmyState.RestoreSurvivors("guard/"+city.Id,city.OwnerFactionId,survivors,History(saved.appliedEncounters));
            }
            if(ids.Count!=settlements.Settlements.Count)throw new ArgumentException("Missing settlement garrison.");
            return next;
        }
        public void ValidateDeployment(CampaignArmyRegistry armies)
        {
            var ids=new HashSet<string>(StringComparer.Ordinal);var origins=FormationOrigins();
            foreach(var guard in guards.Values)foreach(var formation in guard.Formations)
            {
                RestoreFormation(new CampaignFormationData{id=formation.Id,health=formation.Health,morale=formation.Morale},guard.FactionId,origins);
                if(!ids.Add(formation.Id))throw new ArgumentException("Duplicate deployed formation.");
            }
            if(armies!=null)foreach(var army in armies.Armies)foreach(var formation in army.Army.Formations)
            {
                RestoreFormation(new CampaignFormationData{id=formation.Id,health=formation.Health,morale=formation.Morale},army.Army.FactionId,origins);
                if(!ids.Add(formation.Id))throw new ArgumentException("Formation occupies both field army and garrison.");
            }
            foreach(var city in owners.Settlements)
            {
                var guard=guards[city.Id];
                if(guard.Formations.Count+StationedCount(armies,city)+(Pending(city.Id)!=null?1:0)>6)
                    throw new ArgumentException("Fortress defense capacity exceeded.");
                if(armies!=null&&armies.TryGetOccupant(city.Position,out var field)&&field.Army.FactionId!=city.OwnerFactionId&&(!guard.IsDissolved||Pending(city.Id)!=null))
                    throw new ArgumentException("Hostile army occupies an active garrison.");
                foreach(var id in guard.CaptureEncounterHistory())if(armies==null||!armies.HasResolvedEncounter(id))
                    throw new ArgumentException("Garrison history contains an unresolved encounter.");
            }
        }
    }
}
