using Fulgor.Core.Map;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController : MonoBehaviour
    {
        private CampaignSelectionPanel panel;
        private CampaignBoardSelection selection;
        private CampaignArmyMarker armyMarker;
        public const string LocalSaveKey = "Fulgor.Campaign.Prototype.Save.v1";
        private const string SaveKey = LocalSaveKey;
        private MapRouteGraph activeRoutes;
        private ReachDestinationMission initialMission;
        private HexCoordinates spawn;
        private string saveContext;
        public CampaignTurnState State { get; private set; }
        public ArmyMovementState Army { get; private set; }
        public ReachDestinationMission Mission { get; private set; }
        public HexCoordinates? MarchDestination { get; private set; }

        public void Initialize(CampaignSelectionPanel turnPanel, GeneratedMap map, ReachDestinationMission mission = null, CampaignArmyRegistry armyRegistry = null, string faction = null, bool strategicRivals = false,CampaignObjectiveKind objectiveKind = CampaignObjectiveKind.ReachDestination,bool militaryLogistics=false,bool fogOfWar=false)
        {
            if (turnPanel == null) throw new System.ArgumentNullException(nameof(turnPanel));
            if (map == null) throw new System.ArgumentNullException(nameof(map));
            if(fogOfWar&&armyRegistry==null)throw new System.ArgumentException("Fog requires multiple armies.");
            if(militaryLogistics&&armyRegistry==null)throw new System.ArgumentException("Military logistics requires multiple-army mode.");
            if(!System.Enum.IsDefined(typeof(CampaignObjectiveKind),objectiveKind)||objectiveKind==CampaignObjectiveKind.Conquest&&armyRegistry==null)throw new System.ArgumentException("Conquest requires a multiple-army scenario.");
            ObjectiveKind=objectiveKind;Conclusion=CampaignConclusion.InProgress;LastRetreats=System.Array.Empty<CampaignRetreatResult>();
            if(objectiveKind==CampaignObjectiveKind.Conquest)mission=null;
            ResetCampaignBattle();
            Military=null;Rivals=null;rivalActors=null;
            FogEnabled=fogOfWar;Fog=null;
            LogisticsEnabled=militaryLogistics&&armyRegistry!=null;LastLogisticsReport=null;
            marchOrders=new CampaignMarchOrders();
            Detach();
            panel = turnPanel;
            selection = GetComponent<CampaignBoardSelection>();
            TerritoryIncome = CampaignIncomeCalculator.FromMap(map);
            State = new CampaignTurnState(1, default, TerritoryIncome);
            ResetCities();
            var routes = selection != null && selection.Routes != null ? selection.Routes : new MapRouteGraph(map);
            activeRoutes = routes;
            initialMission = mission;
            saveContext = CampaignSaveData.ScenarioSignature(map, routes, mission);
            Mission = mission;
            MarchDestination = null;
            Army = null;
            foreach (var cell in map.Cells)
            {
                if (!routes.CanStandAt(cell.Coordinates)) continue;
                spawn = cell.Coordinates;
                Army = new ArmyMovementState(routes, spawn, 6);
                break;
            }
            armyMarker = GetComponent<CampaignArmyMarker>();
            if (armyMarker == null) armyMarker = gameObject.AddComponent<CampaignArmyMarker>();
            armyMarker.Initialize(selection);
            panel.EndTurnButton.onClick.AddListener(CompleteTurn);
            panel.MoveArmyButton.onClick.AddListener(MoveToSelectedCell);
            panel.SaveButton.onClick.AddListener(SaveCampaign);
            panel.LoadButton.onClick.AddListener(LoadCampaign);
            panel.LocateArmyButton.onClick.AddListener(LocateArmy);
            panel.LocateObjectiveButton.onClick.AddListener(LocateObjective);
            panel.NewGameButton.onClick.AddListener(NewGame);
            panel.CancelMarchButton.onClick.AddListener(CancelMarch);
            panel.BattleLabButton.onClick.AddListener(OpenBattleLab);
            panel.SetSaveStatus("Guardado local · una ranura", Army != null);
            InitializeConquest(map,routes,faction);
            InitializeMultipleArmies(armyRegistry, incomeFaction);
            Military=new CampaignMilitaryState(initialArmyRegistry,SettlementState);
            InitializeRivals(strategicRivals);InitializeDiplomacy();InitializeFog();State=WithCityEconomy(State,cities);UpdateConclusion();
            panel.NextArmyButton.onClick.AddListener(NextArmy);
            if (selection != null) selection.SelectionChanged += RefreshArmy;
            panel.SetTurnState(State);
            RefreshArmy();
        }

        public void CompleteTurn()
        {
            if (registry != null) { CompleteMultipleArmyTurn(); return; }
            if (State == null || IsCampaignFinished) return;
            State = State.CompleteTurn();
            Military=Military.Advance(State.Turn,null);
            Rivals=Rivals.CloseTurn(State.Turn,incomeMap,SettlementState,cities);
            AdvanceCities();
            Army = Army?.BeginTurn();
            ContinueMarch();
            panel?.SetTurnState(State);
            RefreshArmy();
        }

        public void MoveToSelectedCell()
        {
            if (registry != null) { MoveMultipleArmy(); return; }
            if (IsCampaignFinished || Army == null || selection == null || !selection.SelectedCell.HasValue) return;
            var destination = selection.SelectedCell.Value.Coordinates;
            if (destination != Army.Position && MapPathfinder.TryFindPath(activeRoutes, Army.Position, destination, out _))
            {
                MarchDestination = destination;
                ContinueMarch();
            }
            RefreshArmy();
        }

        public void CancelMarch()
        {
            if(HasPendingBattle||IsRivalPhaseActive)return;
            if(registry!=null)marchOrders=marchOrders.Cancel(selectedArmyId);
            MarchDestination = null;
            RefreshArmy();
        }

        private void ContinueMarch()
        {
            if (Army == null || !MarchDestination.HasValue || IsCampaignFinished) return;
            var destination = MarchDestination.Value;
            if (!MapPathfinder.TryFindPath(activeRoutes, Army.Position, destination, out var route))
            { MarchDestination = null; return; }
            // Reaching the mission objective en route must also end the mission.
            if (Mission != null)
                foreach (var cell in route.Cells)
                    if (cell == Mission.Destination) { destination = cell; break; }
            if (!Army.TryAdvance(destination, out var next, out var executedRoute))
            { MarchDestination = null; return; }
            Army = next;
            armyMarker?.SetArmy(Army, executedRoute.Cells);
            Mission = Mission?.ObservePosition(Army.Position);UpdateConclusion();
            if (IsCampaignFinished || MarchDestination==Army.Position) MarchDestination = null;
        }

        public void OpenBattleLab()
        {
            if (HasPendingBattle) { ResumeBattle(); return; }
            var lab = GetComponent<BattleLabController>();
            if (lab == null) lab = gameObject.AddComponent<BattleLabController>();
            lab.Open(panel);
        }

        public void LocateArmy()
        {
            if (Army != null) selection?.LocateCell(Army.Position);
        }

        public void LocateObjective()
        {
            if (Mission != null) selection?.LocateCell(Mission.Destination);
        }

        public void NewGame()
        {
            var board = GetComponent<CampaignBoardPreview>();
            if (board == null) return;
            try { board.RegenerateBoard(); }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"No se pudo iniciar la partida: {exception.Message}", this);
                panel?.SetSaveStatus("No se pudo iniciar. Revisa la configuración del escenario.", Army != null);
            }
        }

        public CampaignSaveData CaptureCampaignSnapshot()
        {
            if(State==null||registry==null&&Army==null||HasPendingBattle||IsRivalPhaseActive)throw new System.InvalidOperationException("Campaign cannot be saved now.");
            var payload=registry!=null?CampaignSaveData.CaptureMultiple(saveContext,State,registry,selectedArmyId,playerFaction,CaptureCities(),Mission?.IsWon==true):
                CampaignSaveData.Capture(saveContext,State,Army,MarchDestination,CaptureCities());
            IncludeOwnership(payload);Military.ValidateDeployment(registry);payload.IncludeMilitary(Military);payload.IncludeMarches(marchOrders);payload.IncludeRivals(Rivals);payload.IncludeCampaignRules(ObjectiveKind);payload.IncludeLogistics(LogisticsEnabled);RefreshFog();payload.IncludeFog(Fog);payload.IncludeDiplomacy(Diplomacy);
            return payload;
        }

        public void SaveCampaign()=>TrySaveCampaign();
        public bool TrySaveCampaign()
        {
            if (registry != null)return TrySaveMultipleArmyCampaign();
            if (State == null || Army == null) return false;
            try
            {
                var payload=CaptureCampaignSnapshot();
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(payload));
                PlayerPrefs.Save();
                panel.SetSaveStatus($"Partida guardada · turno {State.Turn}", true);return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"No se pudo guardar: {exception.Message}", this);
                panel.SetSaveStatus("No se pudo guardar la partida.", true);return false;
            }
        }

        public void LoadCampaign()
        {
            if(registry!=null&&!CanPersistMultipleArmies())return;
            if(!PlayerPrefs.HasKey(SaveKey)){panel?.SetSaveStatus("Todavía no hay una partida guardada.",true);return;}
            TryLoadCampaignSnapshot(PlayerPrefs.GetString(SaveKey));
        }
        public bool TryLoadCampaignSnapshot(string json)
        {
            if(registry!=null)return TryLoadMultipleArmySnapshot(json);
            if(Army==null||State==null)return false;
            try
            {
                var payload = JsonUtility.FromJson<CampaignSaveData>(json);
                if (payload == null) throw new System.ArgumentException("Empty save.");
                payload.RestoreCampaignRules(ObjectiveKind);payload.RestoreLogistics(LogisticsEnabled);
                payload.Restore(ExpectedSaveContext(payload), activeRoutes, spawn, TerritoryIncome, 6,
                    out var restoredState, out var restoredArmy);
                var restoredMission = initialMission?.ObservePosition(restoredArmy.Position);
                var restoredOrder = payload.RestoreMarchDestination(activeRoutes, restoredArmy);
                if (restoredMission?.IsWon == true && restoredOrder.HasValue)
                    throw new System.ArgumentException("Completed mission cannot have a march order.");
                var restoredCities = RestoreCities(payload);
                var restoredOwners=RestoreOwners(payload);
                var restoredRivals=payload.RestoreRivals(rivalsConfigured,RivalFactions());
                var restoredDiplomacy=payload.RestoreDiplomacy(incomeFaction,KnownFactions());
                var restoredMilitary=RestoreMilitary(payload,restoredOwners);restoredMilitary.ValidateDeployment(null);
                restoredState = WithCityEconomy(restoredState,restoredCities,restoredOwners);
                cities = restoredCities; CityRevision++;
                cityCompletions.Clear();
                State = restoredState;Military=restoredMilitary;Rivals=restoredRivals;Diplomacy=restoredDiplomacy;
                CommitOwnership(restoredOwners,restoredState,null);
                Army = restoredArmy;
                armyMarker?.SetArmy(null);
                Mission = restoredMission;LastRetreats=System.Array.Empty<CampaignRetreatResult>();UpdateConclusion();
                MarchDestination = restoredOrder;
                selection?.ClearSelection();
                panel.SetTurnState(State);
                RefreshArmy();
                panel.SetSaveStatus($"Partida cargada · turno {State.Turn}", true);return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"No se pudo cargar: {exception.Message}", this);
                panel.SetSaveStatus("Guardado incompatible o dañado. Partida actual conservada.", true);return false;
            }
        }

        private void RefreshArmy()
        {
            if (registry != null) { RefreshMultipleArmies(); return; }
            armyMarker?.SetArmy(Army);
            if (panel == null) return;
            UpdateConclusion();panel.SetMission(Mission);panel.SetCampaignConclusion(Conclusion,ObjectiveKind);
            panel.SetMarchOrder(MarchDestination);
            panel.SetMissionControls(Army != null, Mission != null,
                Application.isPlaying && GetComponent<CampaignBoardPreview>() != null);
            if (IsCampaignFinished)
            {
                panel.SetArmyState(Army, "Objetivo alcanzado. Misión completada.", false);
                return;
            }
            if (Army == null || selection == null || !selection.SelectedCell.HasValue)
            {
                panel.SetArmyState(Army, MarchDestination.HasValue ?
                    $"Marcha a {MarchDestination.Value.Key} · continúa al terminar turno." :
                    "Selecciona un destino para la hueste.", false);
                return;
            }
            var target = selection.SelectedCell.Value.Coordinates;
            var reachable = MapPathfinder.TryFindPath(activeRoutes, Army.Position, target, out var path);
            var message = !reachable ? "Destino sin acceso terrestre." :
                target == Army.Position ? "La hueste está en esta casilla." :
                $"Destino {target.Key} · Coste {path.TotalCost}" +
                (path.TotalCost > Army.RemainingMovement ? " · varios turnos" : "");
            panel.SetArmyState(Army, message, reachable && target != Army.Position && target != MarchDestination);

        }

        private void Detach()
        {
            if (panel != null)
            {
                panel.EndTurnButton.onClick.RemoveListener(CompleteTurn);
                panel.MoveArmyButton.onClick.RemoveListener(MoveToSelectedCell);
                panel.SaveButton.onClick.RemoveListener(SaveCampaign);
                panel.LoadButton.onClick.RemoveListener(LoadCampaign);
                panel.LocateArmyButton.onClick.RemoveListener(LocateArmy);
                panel.LocateObjectiveButton.onClick.RemoveListener(LocateObjective);
                panel.NewGameButton.onClick.RemoveListener(NewGame);
                panel.CancelMarchButton.onClick.RemoveListener(CancelMarch);
                panel.BattleLabButton.onClick.RemoveListener(OpenBattleLab);
                panel.NextArmyButton.onClick.RemoveListener(NextArmy);
            }
            if (selection != null) selection.SelectionChanged -= RefreshArmy;
        }

        private void OnDestroy() => Detach();
    }
}
