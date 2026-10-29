using System;
using System.Text;
using System.Collections.Generic;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Shared tactical interface for the laboratory and real campaign encounters.</summary>
    public sealed partial class BattleLabController : MonoBehaviour
    {
        private BattlePrototype battle;
        private BattleLabSession session;
        private const string TacticalSaveKey = "Fulgor.BattleLab.Save.v1";
        private BattleAutoController automatic;
        private readonly Queue<string> log = new();
        private string target = "4", q = "2", r = "1";
        private bool running;
        private bool showFinalReport = true;
        private float nextStep;
        private int automaticSteps;
        private CampaignSelectionPanel panel;
        private IReadOnlyList<HexCoordinates> battlefieldCells;
        public bool IsOpen { get; private set; }

        public void Open(CampaignSelectionPanel campaignPanel)
        {
            panel = campaignPanel;
            if (campaignEncounter != null) return;
            if (battle == null) StartBattle();
            IsOpen = true;
            panel.SetHudVisible(false);
        }

        private void StartBattle()
        {
            if (campaignEncounter != null) return;
            session = new BattleLabSession(12345);
            battle = session.Battle;
            automatic = new BattleAutoController(Commands);
            battlefieldCells = Commands.BattlefieldCells;
            target = "4"; q = "2"; r = "1";
            running = false; showFinalReport = true; automaticSteps = 0; log.Clear();
            Record("Batalla de laboratorio · semilla 12345. Sin consecuencias en campaña.");
        }

        private void Close()
        {
            ReturnToCampaign();
        }

        private void Update()
        {
            if (IsOpen && campaignEncounter != null) EnsureGraphicalView();
            if (GraphicalReviewPaused || GraphicalView?.RetreatConfirmationVisible==true) return;
            if (!IsOpen || (!running && !EnemyTurn) || Time.unscaledTime < nextStep || automaticSteps >= 1000) return;
            nextStep = Time.unscaledTime + (campaignEncounter!=null?.85f:.35f);
            AutomaticStep();
        }

        private void AutomaticStep()
        {
            if (automaticSteps >= 1000)
            { running = false; Record("Límite automático alcanzado. Batalla pendiente."); return; }
            try
            {
                var choice = automatic.ChooseOrder();
                if (!choice.HasValue) { running = false; return; }
                var order = choice.Value;
                var recorded = new BattleRecordedOrder { actorId = order.ActorId,
                    kind = order.Kind == BattleAutoOrderKind.Attack ? BattleRecordedOrderKind.Attack :
                        order.Kind == BattleAutoOrderKind.Move ? BattleRecordedOrderKind.Move : BattleRecordedOrderKind.Defend,
                    targetId = order.TargetId ?? 0, q = order.Destination?.Q ?? 0, r = order.Destination?.R ?? 0 };
                if (!ExecuteRecorded(recorded, out var message)) { running = false; Record(message); return; }
                automaticSteps++;
                Record(message);
                if (Roster.Outcome != BattleOutcome.InProgress) running = false;
            }
            catch (Exception e) { running = false; Record("Orden automática rechazada: " + e.Message); }
        }

        private void ExecuteOrder(BattleRecordedOrderKind kind, int targetId = 0, int destinationQ = 0, int destinationR = 0)
        {
            if (!ManualControl) return;
            ExecuteRecorded(new BattleRecordedOrder { kind = kind, actorId = Roster.ActiveFormationId.Value,
                targetId = targetId, q = destinationQ, r = destinationR }, out var message);
            Record(message);
        }

        private void SaveBattle()
        {
            try
            {
                if (campaignEncounter != null) return;
                PlayerPrefs.SetString(TacticalSaveKey, JsonUtility.ToJson(session.Capture()));
                PlayerPrefs.Save();
                Record($"Batalla guardada · {session.OrderCount} órdenes.");
            }
            catch (Exception e) { Record("No se pudo guardar la batalla: " + e.Message); }
        }

        private void LoadBattle()
        {
            running = false;
            if (campaignEncounter != null) return;
            if (!PlayerPrefs.HasKey(TacticalSaveKey)) { Record("No hay batalla guardada."); return; }
            try
            {
                var json = PlayerPrefs.GetString(TacticalSaveKey);
                if (json.Length > 1000000) throw new ArgumentException("Guardado demasiado grande.");
                var restored = BattleLabSession.Restore(JsonUtility.FromJson<BattleLabSaveData>(json));
                session = restored; battle = session.Battle;
                automatic = new BattleAutoController(Commands);
                battlefieldCells = Commands.BattlefieldCells;
                automaticSteps = session.OrderCount;
                log.Clear();
                Record($"Batalla cargada · ronda {Roster.Round}, {session.OrderCount} órdenes recuperadas. Automático pausado.");
            }
            catch (Exception e) { Record("Carga rechazada; batalla actual conservada: " + e.Message); }
        }

        private string FinalReportText()
        {
            var report = BattleReport.Create(Roster);
            var winner = report.Outcome == BattleOutcome.AttackerVictory ? "Victoria atacante" :
                report.Outcome == BattleOutcome.DefenderVictory ? "Victoria defensora" : "Empate";
            var text = new StringBuilder($"PARTE FINAL · {winner} · Ronda {report.Round}\n");
            text.AppendLine($"Semilla {BattleSeed} · {OrderCount} órdenes · Azar {Commands.RandomState}");
            text.AppendLine(campaignEncounter == null ? "Laboratorio: sin consecuencias en campaña.\n" :
                $"{attackerName} contra {defenderName}. Al volver se aplican bajas, heridas y moral.\n");
            foreach (var side in new[] { report.Attacker, report.Defender })
            {
                text.AppendLine(side.Side == BattleSide.Attacker ? "ATACANTES" : "DEFENSORES");
                text.AppendLine($"Iniciales {side.InitialQuantity} · Bajas {side.Casualties} · Vivos {side.Survivors}");
                text.AppendLine($"Formaciones: {side.SurvivingFormations} en campo, {side.WithdrawnFormations} retiradas, {side.DestroyedFormations} destruidas.");
            }
            text.AppendLine();
            foreach (var formation in report.Formations)
                text.AppendLine($"#{formation.Slot.Id}: {formation.Quantity}/{formation.InitialQuantity} vivos · {formation.TotalHealth} PV · moral {formation.Morale} · " +
                    (formation.IsDestroyed ? "destruida" : formation.IsWithdrawn ? "retirada" : "en campo"));
            return text.ToString();
        }

        private void SaveFinalReport()
        {
            if (Roster.Outcome == BattleOutcome.InProgress) return;
            try
            {
                var folder = System.IO.Path.Combine(Application.persistentDataPath, "BattleReports");
                System.IO.Directory.CreateDirectory(folder);
                var path = System.IO.Path.Combine(folder, "batalla-" + Guid.NewGuid().ToString("N") + ".txt");
                System.IO.File.WriteAllText(path, FinalReportText(), new UTF8Encoding(false));
                Record("Parte guardado en: " + path);
                Debug.Log("Parte táctico guardado: " + path, this);
            }
            catch (Exception e) { Record("No se pudo guardar el parte: " + e.Message); }
        }

        private void Record(string message)
        {
            log.Enqueue(message);
            while (log.Count > 6) log.Dequeue();
        }

        private void OnGUI()
        {
            if (!IsOpen || Roster == null || campaignEncounter != null) return;
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1260f, Screen.height / 860f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            try
            {
                GUI.ModalWindow(GetEntityId().GetHashCode(), new Rect((Screen.width / scale - 1220) / 2,
                    (Screen.height / scale - 820) / 2, 1220, 820), DrawWindow, campaignEncounter == null ? "LABORATORIO TÁCTICO · provisional" : $"BATALLA · {attackerName} contra {defenderName}");
            }
            finally { GUI.matrix = previous; GUI.enabled = true; }
        }

        private void DrawWindow(int id)
        {
            var roster = Roster;
            GUI.Label(new Rect(20, 28, 890, 30), $"Ronda {roster.Round} · Activa #{roster.ActiveFormationId} · {roster.ActiveSide} · {roster.Outcome}"+(campaignEncounter!=null?$" · Controlas al {(playerBattleSide==BattleSide.Attacker?"atacante":"defensor")}":""));
            GUI.Label(new Rect(20, 60, 1180, 30), campaignEncounter!=null&&campaignOwner.ObjectiveKind==Fulgor.Core.Campaign.CampaignObjectiveKind.Conquest?campaignOwner.RetreatPreview():"Pulsa una casilla para elegir destino u objetivo; confirma con MOVER o ATACAR. Mover consume la activación.");
            bool manual = ManualControl;
            GUI.enabled = manual;
            GUI.Label(new Rect(20, 100, 65, 25), "Objetivo"); target = GUI.TextField(new Rect(85, 100, 55, 25), target, 4);
            if (GUI.Button(new Rect(150, 100, 110, 28), "ATACAR") && int.TryParse(target, out var targetId))
            {
                ExecuteOrder(BattleRecordedOrderKind.Attack, targetId);
            }
            GUI.Label(new Rect(280, 100, 25, 25), "Q"); q = GUI.TextField(new Rect(305, 100, 45, 25), q, 3);
            GUI.Label(new Rect(360, 100, 25, 25), "R"); r = GUI.TextField(new Rect(385, 100, 45, 25), r, 3);
            if (GUI.Button(new Rect(440, 100, 100, 28), "MOVER") && int.TryParse(q, out var dq) && int.TryParse(r, out var dr))
            {
                ExecuteOrder(BattleRecordedOrderKind.Move, destinationQ: dq, destinationR: dr);
            }
            GUI.enabled = !running && campaignEncounter == null;
            if (GUI.Button(new Rect(940, 100, 120, 28), "GUARDAR BATALLA")) SaveBattle();
            if (GUI.Button(new Rect(1070, 100, 120, 28), "CARGAR BATALLA")) LoadBattle();
            // Re-evaluate after every command; victory may have closed the roster.
            GUI.enabled = ManualControl;
            if (GUI.Button(new Rect(550, 100, 100, 28), "ESPERAR")) ExecuteOrder(BattleRecordedOrderKind.Wait);
            GUI.enabled = ManualControl;
            if (GUI.Button(new Rect(660, 100, 100, 28), "DEFENDER")) ExecuteOrder(BattleRecordedOrderKind.Defend);
            GUI.enabled = campaignEncounter != null ? roster.Outcome == BattleOutcome.InProgress : ManualControl;
            if (GUI.Button(new Rect(770, 100, 145, 28), campaignEncounter == null ? "RETIRAR BANDO" : "RETIRAR MI HUESTE")) {
                if (campaignEncounter != null) WithdrawPlayerArmy(); else ExecuteOrder(BattleRecordedOrderKind.WithdrawArmy);
            }
            GUI.enabled = ManualControl;
            if (GUI.Button(new Rect(20, 145, 160, 30), "PASO AUTOMÁTICO")) AutomaticStep();
            GUI.enabled = roster.Outcome == BattleOutcome.InProgress;
            if (GUI.Button(new Rect(190, 145, 160, 30), running ? "PAUSAR" : "AUTO AMBOS BANDOS")) {
                if (automaticSteps >= 1000) automaticSteps = 0;
                running = !running; nextStep = Time.unscaledTime;
            }
            GUI.enabled = campaignEncounter == null;
            if (GUI.Button(new Rect(360, 145, 160, 30), "REINICIAR BATALLA")) StartBattle();
            GUI.enabled = true;
            if (GUI.Button(new Rect(530, 145, 160, 30), campaignEncounter != null && roster.Outcome != BattleOutcome.InProgress ? "APLICAR Y VOLVER" : "VOLVER A CAMPAÑA")) Close();
            GUI.enabled = Roster.Outcome != BattleOutcome.InProgress;
            if (GUI.Button(new Rect(700, 145, 160, 30), showFinalReport ? "VER FUERZAS" : "VER PARTE FINAL")) showFinalReport = !showFinalReport;
            if (GUI.Button(new Rect(870, 145, 170, 30), "GUARDAR PARTE TXT")) SaveFinalReport();
            GUI.enabled = true;
            DrawBattlefield();
            var summary = new StringBuilder("ID · Bando · Tipo · Cantidad · Salud · Posición · Defensa · Moral\n");
            foreach (var formationId in Roster.FormationIds)
            {
                var formation = Roster.GetFormation(formationId);
                string location = Commands.TryGetPosition(formationId, out var cell) ? cell.Key : formation.IsDestroyed ? "Destruida" : "Retirada";
                summary.AppendLine($"#{formationId} · {(formation.Slot.Side == BattleSide.Attacker ? "Atac." : "Def.")} · {(formation.Slot.Kind == FormationKind.Infantry ? "Inf." : formation.Slot.Kind == FormationKind.Ranged ? "Dist." : "Cab.")} · {formation.Quantity} · {formation.TotalHealth} · {location} · +{Roster.DefenseBonus(formationId)}\n    Moral {formation.Morale} · {MoraleLabel(formation.MoraleStatus)} · herido: {formation.FrontMemberHealth} PV");
            }
            if (Roster.Outcome != BattleOutcome.InProgress && showFinalReport)
                GUI.Label(new Rect(650, 205, 550, 400), FinalReportText(), new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true });
            else GUI.Label(new Rect(650, 205, 550, 360), summary.ToString());
            GUI.Box(new Rect(20, 620, 1180, 180), "Parte de batalla");
            GUI.Label(new Rect(30, 650, 1160, 140), string.Join("\n", log));
        }

        private void DrawBattlefield()
        {
            var style = new GUIStyle(GUI.skin.button) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            MapPath path = null;
            BattleMoveFailure reason = BattleMoveFailure.InvalidDestination;
            bool validInput = int.TryParse(q, out var selectedQ) && int.TryParse(r, out _);
            int.TryParse(r, out var selectedR);
            var selected = new HexCoordinates(selectedQ, selectedR);
            bool allowed = false;
            if (validInput && Roster.ActiveFormationId.HasValue)
                allowed = Commands.TryPreviewMove(Roster.ActiveFormationId.Value, selected, out path, out reason);
            var routeCells = new HashSet<HexCoordinates>();
            if (path != null) foreach (var cell in path.Cells) routeCells.Add(cell);
            GUI.Label(new Rect(20, 190, 600, 28), "CAMPO TÁCTICO · coordenadas Q,R · pulsa para seleccionar");
            foreach (var cell in battlefieldCells)
            {
                Commands.TryGetTerrain(cell, out var tile);
                bool occupied = Commands.TryGetOccupant(cell, out var occupant);
                var originalColor = GUI.backgroundColor;
                var originalContent = GUI.contentColor;
                GUI.backgroundColor = tile == TacticalTerrain.Clear ? new Color(.58f, .60f, .38f) :
                    tile == TacticalTerrain.Woodland ? new Color(.22f, .48f, .26f) : new Color(.57f, .43f, .30f);
                if (routeCells.Contains(cell)) GUI.backgroundColor = new Color(.95f, .69f, .20f);
                if (occupied) GUI.backgroundColor = Roster.GetFormation(occupant).Slot.Side == (campaignEncounter==null?BattleSide.Attacker:playerBattleSide) ?
                    new Color(.26f, .52f, .95f) : new Color(.88f, .29f, .24f);
                if (occupied && Roster.ActiveFormationId == occupant) GUI.contentColor = Color.yellow;
                string terrainLabel = tile == TacticalTerrain.Clear ? "D" : tile == TacticalTerrain.Woodland ? "A" : "E";
                string marker = validInput && cell == selected ? " [1]" : routeCells.Contains(cell) ? " ·" : "";
                var label = cell.Key + "\n" + (occupied ? "#" + occupant : terrainLabel) + marker;
                if (GUI.Button(new Rect(20 + cell.Q * 34 + cell.R * 17, 225 + cell.R * 34, 33, 32), label, style))
                {
                    q = cell.Q.ToString(); r = cell.R.ToString();
                    if (occupied) target = occupant.ToString();
                }
                GUI.backgroundColor = originalColor; GUI.contentColor = originalContent;
            }
            GUI.Label(new Rect(20, 540, 610, 25), "D: despejado (1) · A: arbolado (2) · E: escarpado (2)");
            GUI.Label(new Rect(20, 565, 610, 25), campaignEncounter==null?"Azul: atacante · Rojo: defensor · Amarillo: ruta · [1]: destino":"Azul: tus tropas · Rojo: rival · Amarillo: ruta · [1]: destino");
            string preview = !validInput ? "Introduce coordenadas numéricas Q y R." :
                path != null ? $"Ruta a {selected.Key}: {path.Steps} pasos · coste {path.TotalCost}" +
                    (allowed ? " · movimiento válido" : " · supera el movimiento disponible") :
                "Destino " + selected.Key + " · " + MoveFailureText(reason);
            GUI.Label(new Rect(20, 590, 1180, 25), preview);
        }

        private static string MoraleLabel(FormationMoraleStatus status) => status switch
        {
            FormationMoraleStatus.Steady => "firme",
            FormationMoraleStatus.Wavering => "vacilante",
            _ => "quebrada",
        };

        private static string MoveFailureText(BattleMoveFailure failure) => failure switch
        {
            BattleMoveFailure.Occupied => "casilla ocupada",
            BattleMoveFailure.AlreadyThere => "posición actual",
            BattleMoveFailure.Unreachable => "sin ruta libre",
            BattleMoveFailure.InvalidDestination => "fuera del campo",
            BattleMoveFailure.BattleFinished => "batalla terminada",
            BattleMoveFailure.NotActiveFormation => "formación no activa",
            _ => "sin formación activa",
        };

        private void OnDisable() { if (IsOpen) Close(); }
    }
}
