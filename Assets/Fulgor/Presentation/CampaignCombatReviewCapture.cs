using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Attached only to the review scene. Explicit review mode does not write user saves.
    public sealed class CampaignCombatReviewCapture : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-fulgorCombatReview");
            if(index<0||index+1>=args.Length)yield break;
            string output=args[index+1];Directory.CreateDirectory(output);
            yield return null;yield return null;
            var campaign=GetComponent<CampaignTurnController>();var selection=GetComponent<CampaignBoardSelection>();
            try
            {
                if(campaign?.ArmyRegistry==null)throw new InvalidOperationException("Missing review deployment.");
                var target=campaign.ArmyRegistry.Armies.Single(a=>a.Army.FactionId=="rival");
                selection.LocateCell(target.Movement.Position);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
            yield return null;
            if(!campaign.AttackSelectedArmy()){Debug.LogError("Review attack was rejected.");Application.Quit(1);yield break;}
            var view=GetComponent<BattleLabController>();
            yield return null;
            int steps=0;
            while(view.ActiveCampaignEncounter.Roster.Outcome==BattleOutcome.InProgress&&steps++<1000)
            {
                if(!view.AdvanceCampaignAutomaticStep())break;
                if(steps%4==0)yield return null;
            }
            if(view.ActiveCampaignEncounter.Roster.Outcome==BattleOutcome.InProgress){Debug.LogError("Review battle did not finish.");Application.Quit(1);yield break;}
            var encounter=view.ActiveCampaignEncounter;var report=encounter.CreateReport();
            yield return null;
            if(!view.ReturnToCampaign()){Debug.LogError("Review reconciliation failed.");Application.Quit(1);yield break;}
            yield return null;
            try
            {
                var restored=CampaignArmyRegistry.Restore(campaign.ArmyRegistry.Capture(),GetComponent<CampaignBoardPreview>().InitialArmyRegistry);
                if(!restored.HasResolvedEncounter(encounter.Id))throw new InvalidOperationException("Missing encounter history.");
                File.WriteAllText(Path.Combine(output,"revision_combate.json"),JsonUtility.ToJson(new Summary {
                    outcome=report.Outcome.ToString(),steps=steps,attackerInitial=report.Attacker.InitialQuantity,
                    attackerSurvivors=report.Attacker.Survivors,attackerCasualties=report.Attacker.Casualties,
                    defenderInitial=report.Defender.InitialQuantity,defenderSurvivors=report.Defender.Survivors,
                    defenderCasualties=report.Defender.Casualties,remainingArmies=restored.Armies.Count,historyRestored=true},true));
                Debug.Log("CAMPAIGN_COMBAT_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public string outcome;public int steps,remainingArmies;
            public long attackerInitial,attackerSurvivors,attackerCasualties,defenderInitial,defenderSurvivors,defenderCasualties;
            public bool historyRestored;
        }
    }
}
