using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Explicit command-line verification only; does not write the player's save slot.
    public sealed class CampaignRivalReviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorRivalReview");
            if(at<0||at+1>=args.Length)yield break;
            string output=args[at+1];Directory.CreateDirectory(output);yield return null;yield return null;
            try
            {
                var campaign=GetComponent<CampaignTurnController>();var board=GetComponent<CampaignBoardPreview>();var selection=GetComponent<CampaignBoardSelection>();
                var towns=GetComponent<CampaignSettlementController>();var initial=board.InitialArmyRegistry;var owners=towns.CreateInitialRegistry(selection.Routes);
                var map=Fulgor.Core.Map.ProceduralMapGenerator.Generate(24,16,12345);
                initial.TryGetArmy("Rival-Z",out var reserve);var from=reserve.Movement.Position;int defenses=0,recordCount=0,turns=0;
                bool recruited=false;
                while(!recruited&&turns++<30)
                {
                    var expected=campaign.Rivals.Resources("rival").Add(CampaignIncomeCalculator.ForFaction(map,campaign.SettlementState,"rival"));
                    campaign.CompleteTurn();int closedTurn=campaign.State.Turn;
                    while(campaign.HasPendingBattle)
                    {
                        var view=GetComponent<BattleLabController>();
                        if(view.PlayerBattleSide!=BattleSide.Defender)throw new InvalidOperationException("Player was not assigned the defensive side.");
                        var treasury=campaign.Rivals.Resources("rival");var state=campaign.State;
                        campaign.CompleteTurn();if(campaign.State!=state)throw new InvalidOperationException("Pending defense repeated the closing turn.");
                        if(!view.WithdrawPlayerArmy()||!view.ReturnToCampaign())throw new InvalidOperationException("Defensive retreat did not resume the rival phase.");
                        if(campaign.State.Turn!=closedTurn||!campaign.Rivals.Resources("rival").Equals(treasury))throw new InvalidOperationException("Resuming defense repeated rival income.");
                        defenses++;
                    }
                    if(campaign.IsRivalPhaseActive)throw new InvalidOperationException("Rival phase did not finish.");
                    var records=campaign.Military.Capture().recruits;
                    foreach(var record in records.Skip(recordCount))
                        if(!expected.TrySpend(RecruitmentCatalog.Get(record.profile).Cost,out expected))throw new InvalidOperationException("Rival recruited troops it could not afford.");
                    if(!campaign.Rivals.Resources("rival").Equals(expected))throw new InvalidOperationException("Rival treasury does not match income and paid recruitment.");
                    recordCount=records.Length;recruited=records.Any(r=>r.factionId=="rival"&&r.status==RecruitmentStatus.Completed);
                }
                if(!recruited||defenses==0)throw new InvalidOperationException("Review did not exercise defense and completed recruitment.");
                campaign.ArmyRegistry.TryGetArmy("Rival-Z",out var moved);
                if(moved.Movement.Position==from)throw new InvalidOperationException("Reserve army did not move after the paused defense.");
                var data=CampaignSaveData.CaptureMultiple("review",campaign.State,campaign.ArmyRegistry,campaign.SelectedArmyId,campaign.PlayerFactionId);
                data.IncludeOwnership("review",owners,campaign.SettlementState);data.IncludeMilitary(campaign.Military);data.IncludeMarches(campaign.MarchOrders);data.IncludeRivals(campaign.Rivals);
                data=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(data));var restoredOwners=data.RestoreSettlementOwners(owners,new[]{"castilla","rival"});
                var military=CampaignMilitaryState.Restore(data.military,initial,restoredOwners,new[]{"castilla","rival"},data.turn);
                data.RestoreMultiple(CampaignSaveData.RivalScenarioSignature(CampaignSaveData.MarchScenarioSignature(CampaignSaveData.MilitaryScenarioSignature(CampaignSaveData.SettlementScenarioSignature("review",owners))),true),
                    initial,campaign.PlayerFactionId,campaign.TerritoryIncome,out _,out var restored,military);
                data.RestoreMarches(restored,campaign.PlayerFactionId);var rival=data.RestoreRivals(true,new[]{"rival"});
                if(!rival.Resources("rival").Equals(campaign.Rivals.Resources("rival"))||rival.ClosedTurn!=campaign.State.Turn)
                    throw new InvalidOperationException("Rival treasury was not restored.");
                File.WriteAllText(Path.Combine(output,"revision_rival.json"),JsonUtility.ToJson(new Summary{
                    format=data.version,turn=campaign.State.Turn,defenses=defenses,recruitmentOrders=recordCount,completedRecruitments=data.military.recruits.Count(r=>r.status==RecruitmentStatus.Completed),
                    reserveFrom=from.Key,reserveTo=moved.Movement.Position.Key,paidCostsVerified=true,treasuryRestored=true,phaseFinished=true,playerSide="Defender"},true));
                Debug.Log("CAMPAIGN_RIVAL_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public int format,turn,defenses,recruitmentOrders,completedRecruitments;public string reserveFrom,reserveTo,playerSide;
            public bool paidCostsVerified,treasuryRestored,phaseFinished;
        }
    }
}
