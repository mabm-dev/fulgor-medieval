using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class BattleLabController
    {
        private CampaignEncounter campaignEncounter;
        private CampaignTurnController campaignOwner;
        private string attackerName, defenderName;
        private int campaignOrderCount;
        private BattleSide playerBattleSide=BattleSide.Attacker;
        public BattleSide PlayerBattleSide=>playerBattleSide;
        private BattleRoster Roster => campaignEncounter?.Roster ?? battle?.Roster;
        private BattleAttackResolver Commands => campaignEncounter?.Commands ?? battle?.Commands;
        private long BattleSeed => campaignEncounter?.Seed ?? session.Seed;
        private int OrderCount => campaignEncounter != null ? campaignOrderCount : session.OrderCount;
        private bool EnemyTurn => campaignEncounter != null && Roster.Outcome == BattleOutcome.InProgress && Roster.ActiveSide != playerBattleSide;
        private bool ManualControl => !running && Roster?.ActiveFormationId != null && (campaignEncounter == null || Roster.ActiveSide == playerBattleSide);
        public CampaignEncounter ActiveCampaignEncounter => campaignEncounter;
        public string EncounterReport => campaignEncounter != null && Roster.Outcome != BattleOutcome.InProgress ? FinalReportText() : "";

        public void OpenEncounter(CampaignTurnController owner, CampaignSelectionPanel campaignPanel, CampaignActiveEncounter encounter)
        {
            if (owner == null || encounter == null || campaignPanel == null) throw new System.ArgumentNullException("encounter");
            if (campaignEncounter != encounter.Battle)
            {
                owner.ArmyRegistry.TryGetArmy(encounter.AttackerId,out var attacker);
                playerBattleSide=attacker.Army.FactionId==owner.PlayerFactionId?BattleSide.Attacker:BattleSide.Defender;
                campaignEncounter = encounter.Battle; campaignOrderCount = 0; automaticSteps = 0;
                attackerName = encounter.AttackerId; defenderName = encounter.DefenderId;
                automatic = new BattleAutoController(Commands); battlefieldCells = Commands.BattlefieldCells;
                target = "1"; q = "2"; r = "4"; log.Clear();
                Record($"{attackerName} contra {defenderName}. Controlas al {(playerBattleSide==BattleSide.Attacker?"atacante":"defensor")}; la IA dirige al rival.");
                Record("El resultado se aplica al volver. Volver durante el combate lo deja pendiente.");
            }
            campaignOwner = owner; panel = campaignPanel; running = false;
            showFinalReport = true; nextStep = Time.unscaledTime + .35f;
            IsOpen = true; panel.SetHudVisible(false); EnsureGraphicalView();
        }

        private bool ExecuteRecorded(BattleRecordedOrder order, out string message)
        {
            if (campaignEncounter == null) return session.TryExecute(order, out message);
            Fulgor.Core.Map.MapPath visualPath=null;
            if(order!=null&&order.kind==BattleRecordedOrderKind.Move)Commands.TryPreviewMove(order.actorId,new Fulgor.Core.Map.HexCoordinates(order.q,order.r),out visualPath,out _);
            bool accepted = BattleOrderExecutor.TryExecute(Commands, order, out message);
            if (accepted) { campaignOrderCount++; VisualOrderApplied?.Invoke(order,visualPath); }
            return accepted;
        }

        public bool TryCampaignOrder(BattleRecordedOrder order, out string message)
        {
            message = "Espera tu turno de batalla.";
            if (!IsOpen || campaignEncounter == null || !ManualControl) return false;
            bool accepted = ExecuteRecorded(order, out message); Record(message); return accepted;
        }

        public bool AdvanceCampaignAutomaticStep()
        {
            if (!IsOpen || campaignEncounter == null || Roster.Outcome != BattleOutcome.InProgress) return false;
            int before = campaignOrderCount; AutomaticStep(); return campaignOrderCount != before;
        }

        public bool WithdrawPlayerArmy()
        {
            if (!IsOpen || campaignEncounter == null || Roster.Outcome != BattleOutcome.InProgress) return false;
            running = false;
            if (!Roster.TryWithdrawArmy(playerBattleSide)) return false;
            campaignOrderCount++; Record("Tu hueste se retira conservando sus supervivientes y heridas.");
            return true;
        }

        public bool ReturnToCampaign()
        {
            bool finished = campaignEncounter != null && Roster.Outcome != BattleOutcome.InProgress;
            if (finished && (campaignOwner == null || !campaignOwner.ApplyEncounterResult(campaignEncounter.Id)))
            { Record("No se pudo aplicar el resultado; la batalla se conserva."); return false; }
            running = false; IsOpen = false; CloseGraphicalView(); panel?.SetHudVisible(true);
            if (finished)
            {
                campaignEncounter = null; campaignOwner = null;
                if (battle != null)
                {
                    automatic = new BattleAutoController(battle.Commands);
                    battlefieldCells = battle.Commands.BattlefieldCells; automaticSteps = session.OrderCount;
                }
            }
            GetComponent<CampaignTurnController>()?.BattleViewClosed();
            return true;
        }

        public void ResetEncounterView()
        {
            if (campaignEncounter == null) return;
            running = false; IsOpen = false; CloseGraphicalView(); panel?.SetHudVisible(true);
            campaignEncounter = null; campaignOwner = null; campaignOrderCount = 0;
            // Restore the independent laboratory if it was open before the encounter.
            if (battle != null) { automatic = new BattleAutoController(battle.Commands); battlefieldCells = battle.Commands.BattlefieldCells; }
        }
    }
}
