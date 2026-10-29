using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Optional automation on the review scene; never writes the user's save slot.
    public sealed class CampaignConquestReviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-fulgorConquestReview");
            if(index<0||index+1>=args.Length)yield break;
            string output=args[index+1];Directory.CreateDirectory(output);yield return null;yield return null;
            var campaign=GetComponent<CampaignTurnController>();var towns=GetComponent<CampaignSettlementController>();
            var selection=GetComponent<CampaignBoardSelection>();
            CampaignSettlement target;
            try
            {
                if(!towns.Ready)throw new InvalidOperationException("Settlement view is not ready.");
                target=campaign.SettlementState.Settlements.Single(c=>c.OwnerFactionId!=campaign.PlayerFactionId);
                selection.LocateCell(target.Position);
                if(!campaign.AttackSelectedArmy())throw new InvalidOperationException("Review attack failed.");
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            var before=campaign.State;var beforeIncome=campaign.TerritoryIncome;var initialOwners=towns.CreateInitialRegistry(selection.Routes);
            var view=GetComponent<BattleLabController>();var encounter=view.ActiveCampaignEncounter;int steps=0;
            while(encounter.Roster.Outcome==BattleOutcome.InProgress&&steps++<1000)
            {if(!view.AdvanceCampaignAutomaticStep())break;if(steps%4==0)yield return null;}
            try
            {
                if(encounter.Roster.Outcome!=BattleOutcome.AttackerVictory||!view.ReturnToCampaign())throw new InvalidOperationException("Review assault did not win.");
                campaign.SettlementState.TryGet(target.Id,out var conquered);
                if(conquered.OwnerFactionId!=campaign.PlayerFactionId||!towns.CanManage(target.Id)||campaign.Army.Position!=target.Position)
                    throw new InvalidOperationException("Victory did not occupy and transfer the fort.");
                if(!campaign.State.Resources.Equals(before.Resources)||campaign.TerritoryIncome.Equals(beforeIncome))
                    throw new InvalidOperationException("Conquest stock or income is inconsistent.");
                var data=CampaignSaveData.CaptureMultiple("review",campaign.State,campaign.ArmyRegistry,campaign.SelectedArmyId,campaign.PlayerFactionId);
                data.IncludeOwnership("review",initialOwners,campaign.SettlementState);
                var json=JsonUtility.ToJson(data);data=JsonUtility.FromJson<CampaignSaveData>(json);
                data.RestoreMultiple(CampaignSaveData.SettlementScenarioSignature("review",initialOwners),GetComponent<CampaignBoardPreview>().InitialArmyRegistry,
                    campaign.PlayerFactionId,campaign.TerritoryIncome,out var restoredState,out var restoredArmies);
                var owners=data.RestoreSettlementOwners(initialOwners,new[]{"castilla","rival"});owners.TryGet(target.Id,out var loaded);
                if(loaded.OwnerFactionId!=campaign.PlayerFactionId||!restoredArmies.HasResolvedEncounter(encounter.Id)||!restoredState.Resources.Equals(campaign.State.Resources))
                    throw new InvalidOperationException("Conquest did not survive the v5 JSON round trip.");
                File.WriteAllText(Path.Combine(output,"revision_conquista.json"),JsonUtility.ToJson(new Summary {
                    captured=target.Id,owner=loaded.OwnerFactionId,format=data.version,outcome=encounter.Roster.Outcome.ToString(),
                    grainBefore=beforeIncome.Grain,grainAfter=campaign.TerritoryIncome.Grain,goldBefore=beforeIncome.Gold,goldAfter=campaign.TerritoryIncome.Gold,
                    remainingMovement=campaign.Army.RemainingMovement,historyRestored=true,canManage=towns.CanManage(target.Id)},true));
                Debug.Log("CAMPAIGN_CONQUEST_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public string captured,owner,outcome;public int format,grainBefore,grainAfter,goldBefore,goldAfter,remainingMovement;
            public bool historyRestored,canManage;
        }
    }
}
