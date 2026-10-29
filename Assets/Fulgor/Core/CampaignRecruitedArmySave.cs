using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignArmyRegistry
    {
        public static CampaignArmyRegistry RestoreRecruited(CampaignArmyRegistryData data,CampaignArmyRegistry initial,CampaignMilitaryState military)
        {
            if(initial==null||military==null||data?.armies==null||data.resolvedEncounters==null||initial.HasActiveEncounters)
                throw new ArgumentException("Missing recruited army state.");
            var history=ValidateHistory(data.resolvedEncounters);var origins=military.FormationOrigins();
            var restored=new CampaignArmyRegistry(initial.graph,Array.Empty<CampaignArmyPlacement>());restored.resolved.UnionWith(history);
            var occupied=new HashSet<HexCoordinates>();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var saved in data.armies)
            {
                if(saved?.id==null||restored.armies.ContainsKey(saved.id))throw new ArgumentException("Missing or duplicate army ID.");
                string faction;HexCoordinates spawn;int budget;
                if(initial.TryGetArmy(saved.id,out var original))
                {faction=original.Army.FactionId;spawn=original.Movement.Position;budget=original.Movement.MovementPerTurn;}
                else
                {
                    var origin=military.MusterOrigin(saved.id);
                    if(origin==null)throw new ArgumentException("Unknown army origin.");
                    faction=origin.factionId;spawn=military.Settlement(origin.settlementId).Position;budget=6;
                }
                var position=new HexCoordinates(saved.q,saved.r);
                if(!occupied.Add(position)||!MapPathfinder.TryFindPath(initial.graph,spawn,position,out _))throw new ArgumentException("Occupied or inaccessible army position.");
                var movement=ArmyMovementState.Restore(initial.graph,position,budget,saved.remainingMovement);
                if(saved.formations==null||saved.formations.Length<1||saved.formations.Length>6)throw new ArgumentException("Invalid field army roster.");
                var formations=new List<CampaignFormationState>();
                foreach(var dataFormation in saved.formations)
                {
                    var formation=CampaignMilitaryState.RestoreFormation(dataFormation,faction,origins);
                    if(!ids.Add(formation.Id))throw new ArgumentException("Formation occupies multiple armies.");formations.Add(formation);
                }
                var applied=ValidateHistory(saved.appliedEncounters);
                if(!applied.IsSubsetOf(history))throw new ArgumentException("Unresolved army history.");
                var army=CampaignArmyState.RestoreSurvivors(saved.id,faction,formations,applied);restored.armies.Add(saved.id,new CampaignArmyOnMap(army,movement));
            }
            military.ValidateDeployment(restored);return restored;
        }
    }
    public sealed partial class CampaignSaveData
    {
        public MilitarySaveData military;
        public void IncludeMilitary(CampaignMilitaryState state)
        {
            if(version!=5||state==null)throw new ArgumentException("Ownership must be included before military state.");
            military=state.Capture();context=MilitaryScenarioSignature(context);version=6;
        }
        public static string MilitaryScenarioSignature(string ownershipContext)
        {
            if(string.IsNullOrEmpty(ownershipContext))throw new ArgumentException("Missing ownership context.");
            string rules="military-v6:6-slots:6-movement:transfer-spends-movement:"+ownershipContext;
            foreach(var spec in RecruitmentCatalog.All)
            {
                var cost=spec.Cost;var a=spec.Create("signature").Attributes;
                rules+=FormattableString.Invariant($"|{(int)spec.Profile}:{spec.Quantity}:{spec.Turns}:{cost.Grain}:{cost.Wood}:{cost.Stone}:{cost.Gold}:{cost.Labor}:{a.Attack}:{a.Defense}:{a.MinimumDamage}:{a.MaximumDamage}:{a.HealthPerMember}:{a.Movement}:{a.Initiative}:{a.Range}:{a.Morale}:{a.Discipline}");
            }
            ulong hash=14695981039346656037UL;foreach(char c in rules)hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
