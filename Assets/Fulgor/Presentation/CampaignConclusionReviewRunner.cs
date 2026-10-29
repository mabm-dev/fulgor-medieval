using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Explicit command-line review only; leaves the player's save slot untouched.
    public sealed class CampaignConclusionReviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorConclusionReview");
            if(at<0||at+1>=args.Length)yield break;
            string output=args[at+1];Directory.CreateDirectory(output);yield return null;yield return null;
            try
            {
                var campaign=GetComponent<CampaignTurnController>();var initial=GetComponent<CampaignBoardPreview>().InitialArmyRegistry;
                var panel=GetComponent<CampaignSelectionPanel>();var owners=GetComponent<CampaignSettlementController>().CreateInitialRegistry(GetComponent<CampaignBoardSelection>().Routes);
                if(campaign.ObjectiveKind!=CampaignObjectiveKind.Conquest||campaign.Mission!=null)throw new InvalidOperationException("Review is not a conquest campaign.");
                campaign.CompleteTurn();var view=GetComponent<BattleLabController>();
                if(!campaign.HasPendingBattle||view.PlayerBattleSide!=BattleSide.Defender)throw new InvalidOperationException("Defensive review battle did not start.");
                if(!view.WithdrawPlayerArmy()||!view.ReturnToCampaign())throw new InvalidOperationException("Retreat did not resolve.");
                var retreat=campaign.LastRetreats.FirstOrDefault(r=>r.FactionId==campaign.PlayerFactionId);
                if(retreat==null||retreat.Surrendered||retreat.Destination==retreat.From||campaign.Conclusion!=CampaignConclusion.InProgress)
                    throw new InvalidOperationException("Retreat did not leave a surviving army and ongoing campaign.");
                campaign.ArmyRegistry.TryGetArmy(retreat.ArmyId,out var escaped);
                if(escaped.Movement.Position!=retreat.Destination||escaped.Movement.RemainingMovement!=0)throw new InvalidOperationException("Escaped army position or movement is inconsistent.");
                int rounds=0,battles=1;
                while(!campaign.IsCampaignFinished&&rounds++<80)
                {
                    campaign.CompleteTurn();
                    while(campaign.HasPendingBattle)
                    {
                        view=GetComponent<BattleLabController>();var battle=view.ActiveCampaignEncounter;
                        for(int step=0;step<1000&&battle.Roster.Outcome==BattleOutcome.InProgress;step++)view.AdvanceCampaignAutomaticStep();
                        if(battle.Roster.Outcome==BattleOutcome.InProgress||!view.ReturnToCampaign())throw new InvalidOperationException("Final battle did not resolve.");
                        battles++;
                    }
                }
                if(!campaign.IsCampaignFinished||panel.EndTurnButton.interactable||campaign.MarchOrders.Count!=0||campaign.IsRivalPhaseActive)
                    throw new InvalidOperationException("Campaign did not stop at a terminal result.");
                var terminal=campaign.Conclusion;var before=campaign.State;campaign.CompleteTurn();if(campaign.State!=before)throw new InvalidOperationException("Finished campaign advanced another turn.");
                if(!panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t=>t.text.Contains(terminal==CampaignConclusion.Victory?"VICTORIA":"DERROTA")))
                    throw new InvalidOperationException("Result banner is absent.");
                var data=CampaignSaveData.CaptureMultiple("review",campaign.State,campaign.ArmyRegistry,campaign.SelectedArmyId,campaign.PlayerFactionId);
                data.IncludeOwnership("review",owners,campaign.SettlementState);data.IncludeMilitary(campaign.Military);data.IncludeMarches(campaign.MarchOrders);data.IncludeRivals(campaign.Rivals);data.IncludeCampaignRules(campaign.ObjectiveKind);
                data=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(data));data.RestoreCampaignRules(CampaignObjectiveKind.Conquest);
                var restoredOwners=data.RestoreSettlementOwners(owners,new[]{"castilla","rival"});var military=CampaignMilitaryState.Restore(data.military,initial,restoredOwners,new[]{"castilla","rival"},data.turn);
                var context=CampaignSaveData.CampaignRulesSignature(CampaignSaveData.RivalScenarioSignature(CampaignSaveData.MarchScenarioSignature(CampaignSaveData.MilitaryScenarioSignature(CampaignSaveData.SettlementScenarioSignature("review",owners))),true),CampaignObjectiveKind.Conquest);
                data.RestoreMultiple(context,initial,campaign.PlayerFactionId,campaign.TerritoryIncome,out _,out var restored,military);
                data.RestoreMarches(restored,campaign.PlayerFactionId);data.RestoreRivals(true,new[]{"rival"});
                if(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest,restored,restoredOwners,campaign.PlayerFactionId)!=terminal)
                    throw new InvalidOperationException("Terminal state did not survive the JSON round trip.");
                int finalTurn=campaign.State.Turn;campaign.NewGame();
                if(campaign.Conclusion!=CampaignConclusion.InProgress||campaign.State.Turn!=1||campaign.ObjectiveKind!=CampaignObjectiveKind.Conquest)
                    throw new InvalidOperationException("New game did not reset the conquest scenario.");
                File.WriteAllText(Path.Combine(output,"revision_final_campana.json"),JsonUtility.ToJson(new Summary{
                    format=data.version,result=terminal.ToString(),turn=finalTurn,battles=battles,retreatFrom=retreat.From.Key,retreatTo=retreat.Destination.Value.Key,
                    movementAfterRetreat=0,terminalRestored=true,commandsFrozen=true,newGameReset=true},true));
                Debug.Log("CAMPAIGN_CONCLUSION_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public int format,turn,battles,movementAfterRetreat;public string result,retreatFrom,retreatTo;
            public bool terminalRestored,commandsFrozen,newGameReset;
        }
    }
}
