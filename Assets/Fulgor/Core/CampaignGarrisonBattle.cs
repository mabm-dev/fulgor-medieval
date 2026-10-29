using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed class GarrisonEncounterData
    {
        internal GarrisonEncounterData(string cityId,CampaignArmyState field,CampaignArmyState guard,HexCoordinates position)
        {SettlementId=cityId;FieldArmy=field;Garrison=guard;Position=position;}
        public string SettlementId {get;}
        public CampaignArmyState FieldArmy {get;}
        public CampaignArmyState Garrison {get;}
        public HexCoordinates Position {get;}
    }
    public sealed partial class CampaignArmyRegistry
    {
        internal CampaignArmyRegistry ReplaceFormations(string armyId,IEnumerable<CampaignFormationState> formations)
        {
            if(HasActiveEncounters)throw new InvalidOperationException("No transfers during battle.");
            var current=armies[armyId];var next=new CampaignArmyRegistry(this);next.UpdateSurvivors(current.Army.WithFormations(formations),ArmyMovementState.Restore(graph,current.Movement.Position,current.Movement.MovementPerTurn,0));return next;
        }
        internal CampaignArmyRegistry AddMuster(CampaignArmyState army,HexCoordinates position)
        {
            if(HasActiveEncounters||TryGetOccupant(position,out _)||armies.ContainsKey(army.Id))throw new InvalidOperationException("Muster position is occupied.");
            var next=new CampaignArmyRegistry(this);next.armies.Add(army.Id,new CampaignArmyOnMap(army,ArmyMovementState.Restore(graph,position,6,0)));return next;
        }
        public CampaignAttackFailure PreviewGarrisonAttack(string attackerId,CampaignArmyState guard,HexCoordinates position)
        {
            if(!TryGetArmy(attackerId,out var attacker)||guard==null||guard.IsDissolved)return CampaignAttackFailure.UnknownArmy;
            if(attacker.Army.FactionId==guard.FactionId)return CampaignAttackFailure.FriendlyArmy;
            if(HasActiveEncounters)return CampaignAttackFailure.ArmyInBattle;
            if(attacker.Movement.Position.DistanceTo(position)!=1)return CampaignAttackFailure.NotAdjacent;
            if(attacker.Movement.RemainingMovement<1)return CampaignAttackFailure.NoMovement;
            if(TryGetOccupant(position,out var field)&&field.Army.FactionId!=guard.FactionId)return CampaignAttackFailure.ArmyInBattle;
            return CampaignAttackFailure.None;
        }
        public bool TryBeginGarrisonAttack(string id,string attackerId,string cityId,CampaignArmyState guard,HexCoordinates position,long seed,
            out CampaignArmyRegistry next,out CampaignAttackFailure failure)
        {
            next=this;failure=PreviewGarrisonAttack(attackerId,guard,position);if(failure!=CampaignAttackFailure.None)return false;
            TryGetOccupant(position,out var field);
            var formations=(field?.Army.Formations??Array.Empty<CampaignFormationState>()).Concat(guard.Formations).ToArray();
            if(formations.Length>6)throw new ArgumentException("Fortress defense supports at most six formations.");
            var defenderId=field?.Army.Id??guard.Id;
            if(field==null&&armies.ContainsKey(defenderId))throw new ArgumentException("Reserved garrison ID collision.");
            var history=new HashSet<string>(guard.CaptureEncounterHistory(),StringComparer.Ordinal);
            if(field!=null)history.UnionWith(field.Army.CaptureEncounterHistory());
            var composite=CampaignArmyState.RestoreSurvivors(defenderId,guard.FactionId,formations,history);
            var prepared=new CampaignArmyRegistry(this);
            prepared.armies[defenderId]=new CampaignArmyOnMap(composite,field?.Movement??ArmyMovementState.Restore(graph,position,1,0));
            if(!prepared.TryBeginAttack(id,attackerId,defenderId,seed,out next,out failure))return false;
            var encounter=next.encounters[id];next.encounters[id]=new CampaignActiveEncounter(attackerId,defenderId,encounter.Battle,new GarrisonEncounterData(cityId,field?.Army,guard,position));
            return true;
        }
        public CampaignArmyRegistry ResolveGarrisonEncounter(string id,out CampaignArmyState guardSurvivors)
        {
            if(!TryGetEncounter(id,out var encounter)||encounter.Garrison==null)throw new ArgumentException("Not a garrison encounter.");
            var data=encounter.Garrison;var report=encounter.Battle.CreateReport();var resolved=ResolveEncounter(id);
            var next=new CampaignArmyRegistry(resolved);
            var survivors=next.TryGetArmy(encounter.DefenderId,out var defender)?defender.Army.Formations:Array.Empty<CampaignFormationState>();
            var guardIds=new HashSet<string>(data.Garrison.Formations.Select(f=>f.Id),StringComparer.Ordinal);
            var history=new HashSet<string>(defender?.Army.CaptureEncounterHistory()??data.Garrison.CaptureEncounterHistory(),StringComparer.Ordinal){id};
            guardSurvivors=CampaignArmyState.RestoreSurvivors(data.Garrison.Id,data.Garrison.FactionId,survivors.Where(f=>guardIds.Contains(f.Id)),history);
            if(data.FieldArmy==null)next.armies.Remove(encounter.DefenderId);
            else if(defender!=null)next.UpdateSurvivors(CampaignArmyState.RestoreSurvivors(data.FieldArmy.Id,data.FieldArmy.FactionId,survivors.Where(f=>!guardIds.Contains(f.Id)),history),defender.Movement);
            if(report.Outcome==BattleOutcome.AttackerVictory&&guardSurvivors.IsDissolved&&!next.TryGetOccupant(data.Position,out _)&&next.TryGetArmy(encounter.AttackerId,out var attacker))
                next.armies[attacker.Army.Id]=new CampaignArmyOnMap(attacker.Army,ArmyMovementState.Restore(graph,data.Position,attacker.Movement.MovementPerTurn,attacker.Movement.RemainingMovement));
            return next;
        }
    }
}
