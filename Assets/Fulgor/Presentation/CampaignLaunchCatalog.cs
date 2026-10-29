using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    [Serializable]public sealed class CampaignLaunchEntry
    {
        [SerializeField]private string id,title,description,sceneName;
        [SerializeField]private CampaignScenarioDefinition scenario;
        [SerializeField]private CampaignSettlementDefinition[] settlements=Array.Empty<CampaignSettlementDefinition>();
        public CampaignLaunchEntry(string id,string title,string description,string sceneName,CampaignScenarioDefinition scenario,CampaignSettlementDefinition[] settlements)
        {this.id=id;this.title=title;this.description=description;this.sceneName=sceneName;this.scenario=scenario;this.settlements=settlements?.ToArray();Validate();}
        public string Id=>id;
        public string Title=>title;
        public string Description=>description;
        public string SceneName=>sceneName;
        public CampaignScenarioDefinition Scenario=>scenario;
        public IReadOnlyList<CampaignSettlementDefinition> Settlements=>Array.AsReadOnly(settlements);
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(sceneName)||sceneName.IndexOfAny(new[]{'/','\\',':'})>=0||scenario==null||settlements==null||settlements.Any(s=>s==null))
                throw new ArgumentException("Incomplete campaign launch entry.");
        }
        public bool TryValidateSnapshot(string json,out CampaignSaveData payload)
        {
            payload=null;
            try
            {
                Validate();if(string.IsNullOrWhiteSpace(json)||json.Length>4000000)return false;
                var data=JsonUtility.FromJson<CampaignSaveData>(json);if(data==null||data.version<1||data.version>11)return false;
                var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);var routes=scenario.CreateRouteGraph(map);
                var initial=scenario.CreateArmyRegistry(routes);var mission=scenario.CreateMission(map,routes,initial);
                var towns=new CampaignSettlementRegistry(routes,settlements.Select(s=>s.Create()));string faction=scenario.PlayerFactionId;
                var factions=new HashSet<string>(towns.Settlements.Select(c=>c.OwnerFactionId),StringComparer.Ordinal){faction};
                if(initial!=null)foreach(var army in initial.Armies)factions.Add(army.Army.FactionId);
                string context=CampaignSaveData.ScenarioSignature(map,routes,mission);
                if(initial!=null)context=CampaignSaveData.ArmyScenarioSignature(context,initial,faction);
                if(data.version>=5)context=CampaignSaveData.SettlementScenarioSignature(context,towns);
                if(data.version>=6)context=CampaignSaveData.MilitaryScenarioSignature(context);
                if(data.version>=7)context=CampaignSaveData.MarchScenarioSignature(context);
                if(data.version>=8)context=CampaignSaveData.RivalScenarioSignature(context,scenario.EnableStrategicRivals);
                if(data.version>=9)context=CampaignSaveData.CampaignRulesSignature(context,scenario.ObjectiveKind);
                if(data.version==10||data.version>=11&&data.logisticsVersion==1)context=CampaignSaveData.LogisticsSignature(context);
                if(data.version>=11)context=CampaignSaveData.FogSignature(context);
                data.RestoreCampaignRules(scenario.ObjectiveKind);data.RestoreLogistics(scenario.EnableMilitaryLogistics);
                var owners=data.RestoreSettlementOwners(towns,factions);
                data.RestoreRivals(scenario.EnableStrategicRivals,factions.Where(f=>f!=faction));
                data.RestoreDiplomacy(faction,factions);
                var military=data.version>=6?CampaignMilitaryState.Restore(data.military,initial,owners,factions,data.turn):new CampaignMilitaryState(initial,owners);
                var cities=data.RestoreCities(towns.Settlements.Select(c=>c.Id));
                var income=CampaignIncomeCalculator.ForFaction(map,owners,faction);
                foreach(var city in cities)if(owners.TryGet(city.Key,out var town)&&town.OwnerFactionId==faction)income=income.Add(city.Value.IncomePerTurn);
                if(initial!=null)
                {
                    data.RestoreMultiple(context,initial,faction,income,out _,out var restored,military);military.ValidateDeployment(restored);
                    var orders=data.RestoreMarches(restored,faction);
                    var fog=data.RestoreFog(scenario.EnableFogOfWar,map,factions,restored,owners);fog?.ValidateOrders(orders,restored,faction,routes);
                    var restoredMission=mission;
                    if(data.missionWon)
                    {
                        foreach(var army in restored.Armies)if(army.Army.FactionId==faction)restoredMission=restoredMission?.ObservePosition(army.Movement.Position);
                        if(restoredMission?.IsWon!=true)throw new ArgumentException("Invalid saved mission victory.");
                    }
                    var result=CampaignConclusionRules.Evaluate(scenario.ObjectiveKind,restored,owners,faction,restoredMission?.IsWon==true);
                    if(result!=CampaignConclusion.InProgress&&orders.Count!=0)throw new ArgumentException("Finished campaign has pending orders.");
                }
                else
                {
                    var spawn=map.Cells.First(c=>routes.CanStandAt(c.Coordinates)).Coordinates;
                    data.Restore(context,routes,spawn,income,6,out _,out var army);military.ValidateDeployment(null);
                    if(mission?.ObservePosition(army.Position).IsWon==true&&data.RestoreMarchDestination(routes,army).HasValue)throw new ArgumentException("Finished mission has a pending march.");
                }
                payload=data;return true;
            }
            catch(Exception){return false;}
        }
    }
    [CreateAssetMenu(fileName="CampaignLaunchCatalog",menuName="Fulgor/Campaign Launch Catalog")]
    public sealed class CampaignLaunchCatalog : ScriptableObject
    {
        [SerializeField]private CampaignLaunchEntry[] entries=Array.Empty<CampaignLaunchEntry>();
        [SerializeField]private string menuSceneName="FulgorStart";
        public IReadOnlyList<CampaignLaunchEntry> Entries=>Array.AsReadOnly(entries);
        public string MenuSceneName=>menuSceneName;
        public void Configure(CampaignLaunchEntry[] options)
        {
            if(options==null||options.Length==0)throw new ArgumentException("Campaign catalog is empty.");
            var ids=new HashSet<string>(StringComparer.Ordinal);var scenes=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in options)
            {
                if(entry==null)throw new ArgumentException("Missing launch entry.");entry.Validate();
                if(!ids.Add(entry.Id)||!scenes.Add(entry.SceneName)||entry.SceneName==menuSceneName)throw new ArgumentException("Duplicate campaign identity or scene.");
            }
            entries=options.ToArray();
        }
        public CampaignLaunchEntry Find(string id)=>entries.FirstOrDefault(e=>e.Id==id);
        public bool TryFindSnapshot(string json,out CampaignLaunchEntry entry,out CampaignSaveData payload)
        {
            entry=null;payload=null;
            foreach(var candidate in entries)
            {
                if(candidate==null||!candidate.TryValidateSnapshot(json,out var decoded))continue;
                if(entry!=null){entry=null;payload=null;return false;} // Ambiguous catalogs cannot select a different game silently.
                entry=candidate;payload=decoded;
            }
            return entry!=null;
        }
    }
}
