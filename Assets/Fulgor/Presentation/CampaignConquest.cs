using System;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private CampaignSettlementRegistry initialSettlements;
        private GeneratedMap incomeMap;
        private string incomeFaction;
        public CampaignSettlementRegistry SettlementState { get; private set; }
        public string PlayerFactionId=>incomeFaction;
        public string LastConqueredSettlementId { get; private set; }
        public int OwnershipRevision { get; private set; }

        private void InitializeConquest(GeneratedMap map,MapRouteGraph graph,string faction)
        {
            var controller=GetComponent<CampaignSettlementController>();
            incomeMap=map;incomeFaction=string.IsNullOrWhiteSpace(faction)?controller?.ConfiguredPlayerFaction??"castilla":faction;
            initialSettlements=controller?.CreateInitialRegistry(graph)??new CampaignSettlementRegistry(graph,Array.Empty<CampaignSettlement>());
            SettlementState=initialSettlements;LastConqueredSettlementId=null;OwnershipRevision++;
            TerritoryIncome=IncomeFrom(SettlementState);State=WithCityEconomy(State,cities,SettlementState);
            controller?.PublishOwnership(SettlementState);
        }

        public void SynchronizeSettlementDefinitions(CampaignSettlementRegistry definitions)
        {
            if(definitions==null)throw new ArgumentNullException(nameof(definitions));
            if(initialSettlements==null||CampaignSaveData.SettlementScenarioSignature("definitions",initialSettlements)!=
                CampaignSaveData.SettlementScenarioSignature("definitions",definitions))
            {
                initialSettlements=definitions;
                Military=new CampaignMilitaryState(initialArmyRegistry,definitions);
                CommitOwnership(definitions,WithCityEconomy(State,cities,definitions),null);
            }
        }

        private CampaignResources IncomeFrom(CampaignSettlementRegistry owners)=>CampaignIncomeCalculator.ForFaction(incomeMap,owners,incomeFaction);
        private string ExpectedSaveContext(CampaignSaveData data)
        {
            var owned=data.version>=5?CampaignSaveData.SettlementScenarioSignature(saveContext,initialSettlements):saveContext;
            var military=data.version>=6?CampaignSaveData.MilitaryScenarioSignature(owned):owned;
            var march=data.version>=7?CampaignSaveData.MarchScenarioSignature(military):military;
            var rival=data.version>=8?CampaignSaveData.RivalScenarioSignature(march,rivalsConfigured):march;
            var rules=data.version>=9?CampaignSaveData.CampaignRulesSignature(rival,ObjectiveKind):rival;
            var logistics=data.version==10||data.version>=11&&data.logisticsVersion==1?CampaignSaveData.LogisticsSignature(rules):rules;
            return data.version>=11?CampaignSaveData.FogSignature(logistics):logistics;
        }

        private IEnumerable<string> KnownFactions()
        {
            var ids=new HashSet<string>(StringComparer.Ordinal){incomeFaction};
            foreach(var city in initialSettlements.Settlements)ids.Add(city.OwnerFactionId);
            if(initialArmyRegistry!=null)foreach(var army in initialArmyRegistry.Armies)ids.Add(army.Army.FactionId);
            return ids;
        }
        private CampaignSettlementRegistry RestoreOwners(CampaignSaveData data)=>data.RestoreSettlementOwners(initialSettlements,KnownFactions());
        private void IncludeOwnership(CampaignSaveData data)=>data.IncludeOwnership(saveContext,initialSettlements,SettlementState);

        private void CommitOwnership(CampaignSettlementRegistry owners,CampaignTurnState state,string conquered)
        {
            Military=Military?.WithOwners(owners);
            SettlementState=owners;TerritoryIncome=IncomeFrom(owners);State=state;
            LastConqueredSettlementId=conquered;OwnershipRevision++;CityRevision++;
            GetComponent<CampaignSettlementController>()?.PublishOwnership(owners);
            panel?.SetTurnState(State);
        }

        private bool CaptureAtArmyPosition(string armyId)=>!IsRivalPhaseActive&&registry!=null&&registry.TryGetArmy(armyId,out var own)&&own.Army.FactionId==incomeFaction&&CaptureForArmy(armyId);
        private bool CaptureForArmy(string armyId)
        {
            if(registry==null||HasPendingBattle||!registry.TryGetArmy(armyId,out var army)||
                !DiplomacyAllowsTarget(army.Army.FactionId,army.Movement.Position)||!SettlementState.TryGet(army.Movement.Position,out var city)||Military.HasHostileGarrison(city.Id,army.Army.FactionId)||
                !SettlementState.TryCapture(registry,armyId,city.Id,out var owners,out _))return false;
            var state=WithCityEconomy(State,cities,owners);
            CommitOwnership(owners,state,city.Id);UpdateConclusion();
            panel?.SetSaveStatus($"{(army.Army.FactionId==incomeFaction?"Fortaleza conquistada":"Fortaleza perdida")} · {city.Name} · Rentas al próximo cierre",true);
            return true;
        }

        public bool CaptureSelectedSettlement()
        {
            if(IsCampaignFinished||selection?.SelectedCell==null||Army==null||selection.SelectedCell.Value.Coordinates!=Army.Position)return false;
            bool captured=CaptureAtArmyPosition(selectedArmyId);RefreshArmy();return captured;
        }
    }
}
