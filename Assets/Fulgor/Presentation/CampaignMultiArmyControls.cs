using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private CampaignArmyRegistry registry;
        private CampaignArmyRegistry initialArmyRegistry;
        private string playerFaction;
        private string selectedArmyId;
        private CampaignArmyMarkerLayer armyMarkers;
        public CampaignArmyRegistry ArmyRegistry => registry;
        public string SelectedArmyId => selectedArmyId;

        private void InitializeMultipleArmies(CampaignArmyRegistry initialRegistry, string faction)
        {
            registry = initialRegistry; initialArmyRegistry = initialRegistry;
            playerFaction = faction; selectedArmyId = null;
            if (armyMarkers == null) armyMarkers = GetComponent<CampaignArmyMarkerLayer>();
            if (registry == null)
            {
                if (armyMarkers != null) armyMarkers.Clear();
                panel.SetMultipleArmyMode(false);
                return;
            }
            armyMarker.SetArmy(null);
            saveContext = CampaignSaveData.ArmyScenarioSignature(saveContext, initialRegistry, playerFaction);
            if (armyMarkers == null) armyMarkers = gameObject.AddComponent<CampaignArmyMarkerLayer>();
            armyMarkers.Initialize(selection, registry, playerFaction);
            SelectFirstPlayerArmy();
            panel.SetMultipleArmyMode(true);
            panel.SetSaveStatus("Guardado local · todas las huestes y ciudades", !registry.HasActiveEncounters);
        }

        private void SelectFirstPlayerArmy()
        {
            selectedArmyId = null; Army = null;
            foreach (var unit in registry.Armies)
                if (unit.Army.FactionId == playerFaction)
                { selectedArmyId = unit.Army.Id; Army = unit.Movement; break; }
        }

        public void NextArmy()
        {
            if (registry == null || HasPendingBattle || IsRivalPhaseActive) return;
            var own = new System.Collections.Generic.List<CampaignArmyOnMap>();
            foreach (var unit in registry.Armies) if (unit.Army.FactionId == playerFaction) own.Add(unit);
            if (own.Count == 0) return;
            int index = own.FindIndex(unit => unit.Army.Id == selectedArmyId);
            var next = own[(index + 1) % own.Count];
            selectedArmyId = next.Army.Id; Army = next.Movement;
            if (selection != null) selection.LocateCell(Army.Position);
            RefreshArmy();
        }

        private void RefreshMultipleArmies()
        {
            RefreshFog();
            if(LogisticsEnabled){State=WithCityEconomy(State,cities);panel?.SetTurnState(State);}
            if (selection != null && selection.SelectedCell.HasValue &&
                registry.TryGetOccupant(selection.SelectedCell.Value.Coordinates, out var occupant) &&
                occupant.Army.FactionId == playerFaction) selectedArmyId = occupant.Army.Id;
            if (!registry.TryGetArmy(selectedArmyId, out var active) || active.Army.FactionId != playerFaction)
            {
                SelectFirstPlayerArmy(); registry.TryGetArmy(selectedArmyId, out active);
            }
            Army = active?.Movement;
            armyMarker?.SetArmy(null);
            armyMarkers?.SetRegistry(registry);
            UpdateConclusion();panel.SetMission(Mission);panel.SetCampaignConclusion(Conclusion,ObjectiveKind);
            MarchDestination=marchOrders.Destination(selectedArmyId);
            panel.SetMarchOrder(MarchDestination);
            panel.SetMissionControls(Army != null, Mission != null,
                Application.isPlaying && GetComponent<CampaignBoardPreview>() != null);
            panel.SetNextArmyEnabled(Army != null);
            panel.SetSavingEnabled(!registry.HasActiveEncounters);
            panel.SetArmyActionLabel("MOVER HUESTE SELECCIONADA");
            if(IsRivalPhaseActive&&!HasPendingBattle)
            {panel.EndTurnButton.interactable=false;panel.CancelMarchButton.interactable=false;panel.SetNextArmyEnabled(false);panel.SetArmyState(Army,"Finalizando el turno rival.",false);return;}
            if (HasPendingBattle)
            {
                panel.EndTurnButton.interactable = false;
                panel.SetNextArmyEnabled(false);panel.CancelMarchButton.interactable=false;
                bool canResume = registry.TryGetEncounter(ActiveEncounterId, out _);
                panel.SetArmyActionLabel("VOLVER A LA BATALLA");
                panel.SetArmyState(Army, "Batalla pendiente: resuélvela antes de continuar la campaña.", canResume);
                return;
            }
            if (active == null) { panel.SetArmyState(null, "No quedan huestes propias.", false); return; }
            long troops = 0; foreach (var formation in active.Army.Formations) troops += formation.Quantity;
            string info = $"{active.Army.Id} · {troops} tropas"+ActiveMarchText()+ArmyLogisticsText(active);
            bool canMove = false;
            if (IsCampaignFinished) info += " · Misión completada.";
            else if (selection != null && selection.SelectedCell.HasValue)
            {
                var target = selection.SelectedCell.Value.Coordinates;
                if(FogEnabled&&!CanObserve(target)){RefreshUnseenTarget(target,info);return;}
                if(!DiplomacyAllowsTarget(playerFaction,target)){panel.SetArmyState(Army,info+"\nEl acuerdo diplomático impide atacar u ocupar. Abre DIPLOMACIA.",false);return;}
                if(SettlementState.TryGet(target,out var defended)&&Military.HasHostileGarrison(defended.Id,playerFaction))
                {
                    var guardFailure=registry.PreviewGarrisonAttack(selectedArmyId,Military.Garrison(defended.Id),target);
                    panel.SetArmyActionLabel("ATACAR GUARNICIÓN");
                    info+=" · "+defended.Name+"\n"+(guardFailure==CampaignAttackFailure.None?"La guarnición y la hueste estacionada defienden juntas.":AttackFailureText(guardFailure));
                    panel.SetArmyState(Army,info,guardFailure==CampaignAttackFailure.None);return;
                }
                if (registry.TryGetOccupant(target, out var enemy) && enemy.Army.FactionId != playerFaction)
                {
                    var attackFailure = registry.PreviewAttack(selectedArmyId, enemy.Army.Id);
                    panel.SetArmyActionLabel("ATACAR HUESTE ENEMIGA");
                    info += $" · Rival: {enemy.Army.Id}\n" + (attackFailure == CampaignAttackFailure.None ?
                        "Atacar gasta el movimiento restante." : AttackFailureText(attackFailure));
                    panel.SetArmyState(Army, info, attackFailure == CampaignAttackFailure.None);
                    return;
                }
                if(SettlementState.TryGet(target,out var city)&&city.OwnerFactionId!=playerFaction)
                {
                    panel.SetArmyActionLabel("OCUPAR FORTALEZA");
                    bool inside=target==Army.Position;
                    bool reachable=PreviewMarch(target,out var capturePath,out var captureFailure);
                    bool canCapture=inside?SettlementState.PreviewCapture(registry,selectedArmyId,city.Id)==SettlementCaptureFailure.None:reachable;
                    info+=" · "+city.Name+"\n"+(inside?"Izar el estandarte y asumir el control.":
                        reachable?$"Marchar y ocupar · Coste {capturePath.TotalCost}"+(capturePath.TotalCost>Army.RemainingMovement?" · varios turnos":""):"No hay una ruta libre.");
                    panel.SetArmyState(Army,info,canCapture);return;
                }
                canMove = PreviewMarch(target,out var path,out var failure) && target != Army.Position && MarchDestination!=target;
                info += failure == CampaignArmyMoveFailure.None ? $" · Coste {path.TotalCost}"+(path.TotalCost>Army.RemainingMovement?" · varios turnos":"") :
                    failure == CampaignArmyMoveFailure.GarrisonCapacity ? " · Fortaleza sin plazas disponibles" :
                    failure == CampaignArmyMoveFailure.GuardedSettlement ? " · Fortaleza defendida" :
                    failure == CampaignArmyMoveFailure.Occupied ? " · Destino ocupado" :
                    failure == CampaignArmyMoveFailure.ArmyInBattle ? " · En combate" :
                    failure == CampaignArmyMoveFailure.InsufficientMovement ? " · Faltan puntos" : " · Sin ruta libre";
            }
            else info += " · Selecciona destino.";
            panel.SetArmyState(Army, info, canMove);
        }

        private void MoveMultipleArmy()
        {
            if (HasPendingBattle) { ResumeBattle(); return; }
            if (IsRivalPhaseActive || IsCampaignFinished || selectedArmyId == null || selection == null || !selection.SelectedCell.HasValue) return;
            if(FogEnabled&&!CanObserve(selection.SelectedCell.Value.Coordinates)){IssueMarch(selection.SelectedCell.Value.Coordinates);RefreshArmy();return;}
            if(SettlementState.TryGet(selection.SelectedCell.Value.Coordinates,out var defended)&&Military.HasHostileGarrison(defended.Id,playerFaction))
            {AttackSelectedArmy();return;}
            if (registry.TryGetOccupant(selection.SelectedCell.Value.Coordinates, out var occupant) && occupant.Army.FactionId != playerFaction)
            { AttackSelectedArmy(); return; }
            if(!DiplomacyAllowsTarget(playerFaction,selection.SelectedCell.Value.Coordinates))return;
            if(CaptureSelectedSettlement())return;
            IssueMarch(selection.SelectedCell.Value.Coordinates);
            RefreshArmy();
        }

        private void CompleteMultipleArmyTurn()
        {
            if(IsRivalPhaseActive){ContinueRivalPhase();return;}
            if (IsCampaignFinished) return;
            try
            {
                var nextRegistry = LogisticsEnabled?registry:registry.BeginTurn();
                CampaignLogisticsResult logisticsResult=null;
                var nextState = State.CompleteTurn();
                var nextRivals=Rivals.CloseTurn(nextState.Turn,incomeMap,SettlementState,cities);
                var closingMilitary=Military;
                if(LogisticsEnabled)
                {
                    var logistics=CloseMilitaryLogistics(nextRivals);
                    nextRegistry=logistics.Armies;closingMilitary=logistics.Military;
                    nextState=new CampaignTurnState(nextState.Turn,logistics.Resources(playerFaction),nextState.IncomePerTurn,nextState.ExpensesPerTurn);
                    foreach(var faction in nextRivals.Factions)nextRivals=nextRivals.WithResources(faction,logistics.Resources(faction));
                    logisticsResult=logistics;
                }
                var nextMilitary=closingMilitary.Advance(nextState.Turn,nextRegistry);
                registry = nextRegistry; State = nextState;Military=nextMilitary;Rivals=nextRivals;LastLogisticsReport=logisticsResult;CityRevision++;
                CloseDiplomacy();AdvanceCities();
                ContinueMultipleMarches();
                BeginRivalPhase();
                panel.SetTurnState(State); RefreshArmy();
            }
            catch (System.InvalidOperationException)
            { panel.SetSaveStatus("Resuelve los encuentros antes de terminar el turno.", false); }
        }
    }
}
