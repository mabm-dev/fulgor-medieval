using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed class CampaignRetreatResult
    {
        internal CampaignRetreatResult(string army,string faction,HexCoordinates from,HexCoordinates? to,long troops)
        {ArmyId=army;FactionId=faction;From=from;Destination=to;Troops=troops;}
        public string ArmyId {get;}
        public string FactionId {get;}
        public HexCoordinates From {get;}
        public HexCoordinates? Destination {get;}
        public long Troops {get;}
        public bool Surrendered=>!Destination.HasValue;
    }
    public sealed class CampaignStrategicResolution
    {
        private CampaignStrategicResolution(CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,
            string conquered,List<CampaignRetreatResult> retreats)
        {Armies=armies;Military=military;Owners=owners;ConqueredSettlementId=conquered;Retreats=retreats.AsReadOnly();}
        public CampaignArmyRegistry Armies {get;}
        public CampaignMilitaryState Military {get;}
        public CampaignSettlementRegistry Owners {get;}
        public string ConqueredSettlementId {get;}
        public IReadOnlyList<CampaignRetreatResult> Retreats {get;}

        public static HexCoordinates? RetreatDestination(CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners,
            HexCoordinates origin,string faction,int formations,int movementBudget)
        {
            var enemies=armies.Armies.Where(a=>a.Army.FactionId!=faction).Select(a=>a.Movement.Position).ToList();
            enemies.AddRange(owners.Settlements.Where(c=>military.HasHostileGarrison(c.Id,faction)).Select(c=>c.Position));
            var candidates=new List<HexCoordinates>();
            foreach(var at in origin.Neighbors())
            {
                if(!armies.RetreatStep(origin,at,out var cost)||cost>movementBudget||armies.TryGetOccupant(at,out _))continue;
                if(enemies.Any(enemy=>enemy.DistanceTo(at)<=1))continue;
                if(owners.TryGet(at,out var city)&&(city.OwnerFactionId!=faction||military.Garrison(city.Id).Formations.Count+formations+(military.Pending(city.Id)!=null?1:0)>6))continue;
                candidates.Add(at);
            }
            var friendly=owners.Settlements.Where(c=>c.OwnerFactionId==faction).Select(c=>c.Position).ToArray();
            return candidates.OrderByDescending(at=>enemies.Count==0?0:enemies.Min(e=>e.DistanceTo(at)))
                .ThenBy(at=>friendly.Length==0?0:friendly.Min(c=>c.DistanceTo(at))).ThenBy(at=>at.Q).ThenBy(at=>at.R)
                .Select(at=>(HexCoordinates?)at).FirstOrDefault();
        }

        public static CampaignStrategicResolution Resolve(string id,CampaignArmyRegistry armies,CampaignMilitaryState military,CampaignSettlementRegistry owners)
        {
            var retreats=new List<CampaignRetreatResult>();
            if(armies.HasResolvedEncounter(id))return new CampaignStrategicResolution(armies,military,owners,null,retreats);
            if(!armies.TryGetEncounter(id,out var encounter))throw new ArgumentException("Unknown strategic encounter.");
            var report=encounter.Battle.CreateReport();armies.TryGetArmy(encounter.AttackerId,out var originalAttacker);armies.TryGetArmy(encounter.DefenderId,out var originalDefender);
            CampaignArmyRegistry next;
            if(encounter.Garrison!=null)
            {
                next=armies.ResolveGarrisonEncounter(id,out var guard);military=military.WithGarrisonResult(encounter.Garrison.SettlementId,guard);
            }
            else next=armies.ResolveEncounter(id);
            if(report.Outcome==BattleOutcome.DefenderVictory&&next.TryGetArmy(encounter.AttackerId,out var attacker))
            {
                var destination=RetreatDestination(next,military,owners,originalAttacker.Movement.Position,attacker.Army.FactionId,attacker.Army.Formations.Count,attacker.Movement.MovementPerTurn);
                next=next.PlaceRetreatedArmy(attacker.Army,destination);
                retreats.Add(new CampaignRetreatResult(attacker.Army.Id,attacker.Army.FactionId,originalAttacker.Movement.Position,destination,attacker.Army.Formations.Sum(f=>(long)f.Quantity)));
            }
            if(report.Outcome==BattleOutcome.AttackerVictory)
            {
                next.TryGetArmy(encounter.DefenderId,out var defender);
                if(encounter.Garrison!=null)
                {
                    var guard=military.Garrison(encounter.Garrison.SettlementId);
                    var forms=(defender?.Army.Formations??Array.Empty<CampaignFormationState>()).Concat(guard.Formations).ToArray();
                    if(forms.Length>0)
                    {
                        int budget=defender?.Movement.MovementPerTurn??6;
                        var destination=RetreatDestination(next,military,owners,originalDefender.Movement.Position,guard.FactionId,forms.Length,budget);
                        military=military.EvacuateGarrison(encounter.Garrison.SettlementId,defender,next,destination,out next,out var escapedId);
                        retreats.Add(new CampaignRetreatResult(escapedId??encounter.DefenderId,guard.FactionId,originalDefender.Movement.Position,destination,forms.Sum(f=>(long)f.Quantity)));
                    }
                }
                else if(defender!=null)
                {
                    var destination=RetreatDestination(next,military,owners,originalDefender.Movement.Position,defender.Army.FactionId,defender.Army.Formations.Count,defender.Movement.MovementPerTurn);
                    next=next.PlaceRetreatedArmy(defender.Army,destination);
                    retreats.Add(new CampaignRetreatResult(defender.Army.Id,defender.Army.FactionId,originalDefender.Movement.Position,destination,defender.Army.Formations.Sum(f=>(long)f.Quantity)));
                }
                bool guardRemains=owners.TryGet(originalDefender.Movement.Position,out var fort)&&military.HasHostileGarrison(fort.Id,originalAttacker.Army.FactionId);
                if(!guardRemains)next=next.OccupyAfterStrategicVictory(encounter.AttackerId,originalDefender.Movement.Position);
            }
            string conquered=null;
            if(report.Outcome==BattleOutcome.AttackerVictory&&owners.TryGet(originalDefender.Movement.Position,out var city)&&
                owners.TryCapture(next,encounter.AttackerId,city.Id,out var captured,out _))
            {owners=captured;military=military.WithOwners(owners);conquered=city.Id;}
            military.ValidateDeployment(next);
            return new CampaignStrategicResolution(next,military,owners,conquered,retreats);
        }
    }
    public sealed partial class CampaignArmyRegistry
    {
        internal bool RetreatStep(HexCoordinates from,HexCoordinates to,out int cost)=>graph.TryGetMovementCost(from,to,out cost);
        internal CampaignArmyRegistry PlaceRetreatedArmy(CampaignArmyState army,HexCoordinates? at)
        {
            var next=new CampaignArmyRegistry(this);var previous=armies[army.Id];
            if(!at.HasValue)next.armies.Remove(army.Id);
            else next.armies[army.Id]=new CampaignArmyOnMap(army,ArmyMovementState.Restore(graph,at.Value,previous.Movement.MovementPerTurn,0));
            return next;
        }
        internal CampaignArmyRegistry OccupyAfterStrategicVictory(string id,HexCoordinates at)
        {
            if(TryGetOccupant(at,out _)||!TryGetArmy(id,out var attacker))return this;
            var next=new CampaignArmyRegistry(this);next.armies[id]=new CampaignArmyOnMap(attacker.Army,ArmyMovementState.Restore(graph,at,attacker.Movement.MovementPerTurn,0));return next;
        }
    }
    public sealed partial class CampaignMilitaryState
    {
        internal CampaignMilitaryState EvacuateGarrison(string cityId,CampaignArmyOnMap field,CampaignArmyRegistry armies,HexCoordinates? destination,
            out CampaignArmyRegistry moved,out string armyId)
        {
            var next=new CampaignMilitaryState(this);var guard=guards[cityId];
            var forms=(field?.Army.Formations??Array.Empty<CampaignFormationState>()).Concat(guard.Formations).ToArray();
            var history=new HashSet<string>(guard.CaptureEncounterHistory(),StringComparer.Ordinal);
            if(field!=null)history.UnionWith(field.Army.CaptureEncounterHistory());
            next.guards[cityId]=guard.WithFormations(Array.Empty<CampaignFormationState>());moved=armies;armyId=field?.Army.Id;
            if(field!=null)
                moved=armies.PlaceRetreatedArmy(CampaignArmyState.RestoreSurvivors(field.Army.Id,guard.FactionId,forms,history),destination);
            else if(destination.HasValue)
            {
                var record=new MusterRecord{serial=checked(musters.Count+1),settlementId=cityId,factionId=guard.FactionId};next.musters.Add(record);armyId=record.ArmyId;
                moved=armies.AddMuster(CampaignArmyState.RestoreSurvivors(armyId,guard.FactionId,forms,history),destination.Value);
            }
            return next;
        }
    }
}
