using System;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public string ActiveEncounterId { get; private set; }
        public BattleReport LastBattleReport { get; private set; }
        public bool HasPendingBattle => registry?.HasActiveEncounters == true;

        public bool AttackSelectedArmy()
        {
            if (HasPendingBattle) return ResumeBattle();
            if (IsRivalPhaseActive || registry == null || IsCampaignFinished || selection?.SelectedCell == null ||
                !registry.TryGetArmy(selectedArmyId, out var own) || own.Army.FactionId != playerFaction ||
                own.Army.IsDissolved) return false;
            var target=selection.SelectedCell.Value.Coordinates;
            RefreshFog();if(!CanObserve(target)||!DiplomacyAllowsTarget(playerFaction,target))return false;
            bool guarded=SettlementState.TryGet(target,out var defended)&&Military.HasHostileGarrison(defended.Id,playerFaction);
            registry.TryGetOccupant(target,out var enemy);
            var guard=guarded?Military.Garrison(defended.Id):null;
            if(guarded?registry.PreviewGarrisonAttack(selectedArmyId,guard,target)!=CampaignAttackFailure.None:
                enemy==null||registry.PreviewAttack(selectedArmyId,enemy.Army.Id)!=CampaignAttackFailure.None)return false;
            if (GetComponent<CityManagementView>()?.IsOpen == true || GetComponent<BattleLabController>()?.IsOpen == true) return false;
            int suffix = 1;
            string id;
            do { id = $"turn-{State.Turn}-battle-{suffix++}"; }
            while (registry.HasResolvedEncounter(id) || registry.TryGetEncounter(id, out _));
            // Stable seed, independent of process-randomized string hashes.
            uint seed = 2166136261;
            foreach (char c in saveContext + ":" + id + ":" + selectedArmyId + ":" + (guarded?guard.Id:enemy.Army.Id))
                seed = unchecked((seed ^ c) * 16777619);
            CampaignArmyRegistry next;
            bool begun=guarded?registry.TryBeginGarrisonAttack(id,selectedArmyId,defended.Id,guard,target,seed,out next,out _):
                registry.TryBeginAttack(id,selectedArmyId,enemy.Army.Id,seed,out next,out _);
            if(!begun)return false;
            marchOrders=marchOrders.Cancel(selectedArmyId);
            registry = next; ActiveEncounterId = id; LastBattleReport = null;
            RefreshArmy();
            return ResumeBattle();
        }

        public bool ResumeBattle()
        {
            if (registry == null || !registry.TryGetEncounter(ActiveEncounterId, out var encounter) ||
                GetComponent<CityManagementView>()?.IsOpen == true) return false;
            var view = GetComponent<BattleLabController>();
            if (view == null) view = gameObject.AddComponent<BattleLabController>();
            view.OpenEncounter(this, panel, encounter); return true;
        }

        public bool ApplyEncounterResult(string id)
        {
            if (registry == null || string.IsNullOrEmpty(id)) return false;
            if (registry.HasResolvedEncounter(id)) return true;
            if (id != ActiveEncounterId) return false;
            if (!registry.TryGetEncounter(id, out var encounter) || encounter.Battle.Roster.Outcome == BattleOutcome.InProgress) return false;
            var report = encounter.Battle.CreateReport();
            registry.TryGetArmy(encounter.DefenderId,out var defender);
            bool atFort=SettlementState.TryGet(defender.Movement.Position,out var fort);
            CampaignArmyRegistry next;var military=Military;var owners=SettlementState;string conquered=null;
            registry.TryGetArmy(encounter.AttackerId,out var attacker);
            if(ObjectiveKind==CampaignObjectiveKind.Conquest)
            {
                var resolution=CampaignStrategicResolution.Resolve(id,registry,Military,SettlementState);
                next=resolution.Armies;military=resolution.Military;owners=resolution.Owners;conquered=resolution.ConqueredSettlementId;LastRetreats=resolution.Retreats;
            }
            else
            {
                LastRetreats=Array.Empty<CampaignRetreatResult>();
                if(encounter.Garrison!=null)
                {next=registry.ResolveGarrisonEncounter(id,out var survivors);military=military.WithGarrisonResult(encounter.Garrison.SettlementId,survivors);}
                else next=atFort?registry.ResolveAndOccupyVacatedCell(id,out _):registry.ResolveEncounter(id);
                if(atFort&&!military.HasHostileGarrison(fort.Id,attacker.Army.FactionId)&&report.Outcome==BattleOutcome.AttackerVictory&&
                    owners.TryCapture(next,encounter.AttackerId,fort.Id,out var capturedOwners,out _))
                {owners=capturedOwners;conquered=fort.Id;}
            }
            var economy=WithCityEconomy(State,cities,owners);
            registry = next;Military=military;CityRevision++; LastBattleReport = report; ActiveEncounterId = null;
            if(conquered!=null)CommitOwnership(owners,economy,conquered);
            marchOrders=marchOrders.Prune(registry,playerFaction);
            foreach(var retreat in LastRetreats)marchOrders=marchOrders.Cancel(retreat.ArmyId);
            UpdateConclusion();
            if(ObjectiveKind==CampaignObjectiveKind.Conquest)armyMarkers?.Initialize(selection,registry,playerFaction);
            RefreshArmy();
            bool attacking=attacker.Army.FactionId==playerFaction;
            var message=conquered!=null?$"{(attacking?"Fortaleza conquistada":"Fortaleza perdida")} · {fort.Name} · Rentas al próximo cierre":
                $"Batalla resuelta · bajas propias {(attacking?report.Attacker.Casualties:report.Defender.Casualties)}, rivales {(attacking?report.Defender.Casualties:report.Attacker.Casualties)}";
            if(LastRetreats.Count>0)message+=" · "+RetreatSummary();
            panel.SetSaveStatus(message,true);
            return true;
        }

        public void BattleViewClosed() {ContinueRivalPhase();RefreshArmy();}

        private void ResetCampaignBattle()
        {
            GetComponent<BattleLabController>()?.ResetEncounterView();
            ActiveEncounterId = null; LastBattleReport = null;
        }

        private static string AttackFailureText(CampaignAttackFailure failure) => failure switch
        {
            CampaignAttackFailure.NotAdjacent => "Acércate a una casilla adyacente para atacar.",
            CampaignAttackFailure.NoMovement => "Sin movimiento: espera al siguiente turno para atacar.",
            CampaignAttackFailure.ArmyInBattle => "Hueste bloqueada en un combate.",
            CampaignAttackFailure.FriendlyArmy => "Hueste de tu facción.",
            _ => "Ataque no disponible.",
        };
    }
}
