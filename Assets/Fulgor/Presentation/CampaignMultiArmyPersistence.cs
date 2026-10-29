using System;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private bool CanPersistMultipleArmies()
        {
            if (State == null || registry == null) return false;
            if (!registry.HasActiveEncounters&&!IsRivalPhaseActive) return true;
            panel.SetSaveStatus("Resuelve los encuentros antes de guardar o cargar.", false);
            return false;
        }

        private bool TrySaveMultipleArmyCampaign()
        {
            if (!CanPersistMultipleArmies()) return false;
            try
            {
                var payload=CaptureCampaignSnapshot();
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(payload));
                PlayerPrefs.Save();
                panel.SetSaveStatus($"Partida guardada · turno {State.Turn}"+(FogEnabled?"":$" · {registry.Armies.Count} huestes"), true);return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo guardar: {exception.Message}", this);
                panel.SetSaveStatus("No se pudo guardar la partida.", true);return false;
            }
        }

        private bool TryLoadMultipleArmySnapshot(string json)
        {
            if (!CanPersistMultipleArmies()) return false;
            try
            {
                var payload = JsonUtility.FromJson<CampaignSaveData>(json);
                if (payload == null) throw new ArgumentException("Empty save.");
                payload.RestoreCampaignRules(ObjectiveKind);payload.RestoreLogistics(LogisticsEnabled);
                var restoredOwners=RestoreOwners(payload);
                var restoredRivals=payload.RestoreRivals(rivalsConfigured,RivalFactions());
                var restoredDiplomacy=payload.RestoreDiplomacy(incomeFaction,KnownFactions());
                var restoredMilitary=RestoreMilitary(payload,restoredOwners);
                payload.RestoreMultiple(ExpectedSaveContext(payload), initialArmyRegistry, playerFaction, TerritoryIncome,
                    out var restoredState, out var restoredRegistry,restoredMilitary);
                restoredMilitary.ValidateDeployment(restoredRegistry);
                var restoredMarches=payload.RestoreMarches(restoredRegistry,playerFaction);
                var restoredFog=payload.RestoreFog(FogEnabled,incomeMap,KnownFactions(),restoredRegistry,restoredOwners);restoredFog?.ValidateOrders(restoredMarches,restoredRegistry,playerFaction,activeRoutes);
                if(payload.missionWon&&restoredMarches.Count!=0)throw new ArgumentException("Completed mission has pending marches.");
                var restoredCities = RestoreCities(payload);
                restoredState = WithCityEconomy(restoredState, restoredCities,restoredOwners,restoredRegistry,restoredMilitary);
                var restoredMission = initialMission;
                if (payload.missionWon)
                {
                    foreach (var unit in restoredRegistry.Armies)
                        if (unit.Army.FactionId == playerFaction)
                            restoredMission = restoredMission?.ObservePosition(unit.Movement.Position);
                    if (restoredMission?.IsWon != true)
                        throw new ArgumentException("Saved victory has no player army at the objective.");
                }

                var restoredConclusion=CampaignConclusionRules.Evaluate(ObjectiveKind,restoredRegistry,restoredOwners,playerFaction,restoredMission?.IsWon==true);
                if(restoredConclusion!=CampaignConclusion.InProgress&&restoredMarches.Count!=0)throw new ArgumentException("Finished campaign has pending marches.");
                // Publish only after all army, city and economic validation succeeds.
                Fog=restoredFog;InvalidateFog();LastLogisticsReport=null;State = restoredState; registry = restoredRegistry;Military=restoredMilitary;marchOrders=restoredMarches;Rivals=restoredRivals;Diplomacy=restoredDiplomacy;
                CommitOwnership(restoredOwners,restoredState,null);
                cities = restoredCities; CityRevision++; cityCompletions.Clear();
                selectedArmyId = payload.selectedArmyId;
                Mission = restoredMission; MarchDestination = null;LastRetreats=Array.Empty<CampaignRetreatResult>();UpdateConclusion();
                // Drop any visual route still interpolating from the replaced game.
                armyMarkers?.Initialize(selection, registry, playerFaction);
                selection?.ClearSelection();
                panel.SetTurnState(State); RefreshArmy();
                panel.SetSaveStatus($"Partida cargada · turno {State.Turn}"+(FogEnabled?"":$" · {registry.Armies.Count} huestes"), true);return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"No se pudo cargar: {exception.Message}", this);
                panel.SetSaveStatus("Guardado incompatible o dañado. Partida actual conservada.", true);return false;
            }
        }
    }
}
