using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Explicit command-line review only. Does not modify the user's save slot.
    public sealed class CampaignMilitaryReviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorMilitaryReview");
            if(at<0||at+1>=args.Length)yield break;
            string output=args[at+1];Directory.CreateDirectory(output);yield return null;yield return null;
            var campaign=GetComponent<CampaignTurnController>();var towns=GetComponent<CampaignSettlementController>();
            var selection=GetComponent<CampaignBoardSelection>();var initial=GetComponent<CampaignBoardPreview>().InitialArmyRegistry;
            var initialOwners=towns.CreateInitialRegistry(selection.Routes);string newArmy=null;long veteranHealth=0;int veteranMorale=0;
            try
            {
                for(int i=0;i<12;i++)campaign.CompleteTurn();
                var home=campaign.SettlementState.Settlements.Single(s=>s.OwnerFactionId==campaign.PlayerFactionId);
                if(!towns.OpenCityAt(home.Position))throw new InvalidOperationException("City did not open.");
                var city=GetComponent<CityManagementView>();city.SelectPanel(CityPanelKind.Armies);
                if(!city.PrimaryButton.interactable)throw new InvalidOperationException("Recruitment is not available.");
                city.PrimaryButton.onClick.Invoke();city.SecondaryButton.onClick.Invoke();city.SecondaryButton.onClick.Invoke();
                if(campaign.Military.Garrison(home.Id).Formations.Single().Quantity!=20)throw new InvalidOperationException("Recruit did not complete.");
                city.SelectMilitaryMode(CityMilitaryMode.Assign);city.PrimaryButton.onClick.Invoke();
                if(!campaign.Military.Garrison(home.Id).IsDissolved||campaign.Army.RemainingMovement!=0)throw new InvalidOperationException("Assignment failed.");
                campaign.ArmyRegistry.TryGetArmy(campaign.SelectedArmyId,out var field);var veteran=field.Army.Formations.First();veteranHealth=veteran.Health;veteranMorale=veteran.Morale;
                // Deposit all active formations; then raise a new army from the first veteran.
                var forms=field.Army.Formations.Select(f=>f.Id).ToArray();
                foreach(var id in forms)if(!campaign.TryLeaveGarrison(home.Id,id))throw new InvalidOperationException("Garrison transfer failed.");
                if(!campaign.TryMuster(home.Id,veteran.Id))throw new InvalidOperationException("Muster failed.");
                newArmy=campaign.SelectedArmyId;
                if(campaign.Army.RemainingMovement!=0)throw new InvalidOperationException("Muster refilled movement.");
                if(!campaign.TryRecruit(home.Id,TroopProfile.Crossbowmen))throw new InvalidOperationException("Second recruitment failed.");
                city.Close();
                var save=CampaignSaveData.CaptureMultiple("review",campaign.State,campaign.ArmyRegistry,newArmy,campaign.PlayerFactionId);
                save.IncludeOwnership("review",initialOwners,campaign.SettlementState);save.IncludeMilitary(campaign.Military);
                var data=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(save));
                var owners=data.RestoreSettlementOwners(initialOwners,new[]{"castilla","rival"});
                var military=CampaignMilitaryState.Restore(data.military,initial,owners,new[]{"castilla","rival"},data.turn);
                data.RestoreMultiple(CampaignSaveData.MilitaryScenarioSignature(CampaignSaveData.SettlementScenarioSignature("review",initialOwners)),initial,
                    campaign.PlayerFactionId,campaign.TerritoryIncome,out _,out var loaded,military);
                loaded.TryGetArmy(newArmy,out var restored);
                if(restored.Army.Formations.Single().Health!=veteranHealth||restored.Army.Formations.Single().Morale!=veteranMorale||military.Pending(home.Id)==null)
                    throw new InvalidOperationException("Military JSON round trip lost troops or training.");
                // Test a real battle using the player's stored garrison against the rival, without altering live presentation state.
                var rival=loaded.Armies.Single(a=>a.Army.FactionId!=campaign.PlayerFactionId);
                if(!loaded.TryBeginGarrisonAttack("review-defense",rival.Army.Id,home.Id,military.Garrison(home.Id),home.Position,42,out var fighting,out _))
                    throw new InvalidOperationException("Combined garrison defense failed to start.");
                fighting.TryGetEncounter("review-defense",out var encounter);new BattleAutoController(encounter.Battle.Commands).Run(1000);
                if(encounter.Battle.Roster.Outcome==BattleOutcome.InProgress)throw new InvalidOperationException("Defense did not finish.");
                var result=fighting.ResolveGarrisonEncounter("review-defense",out var survivors);
                military=military.WithGarrisonResult(home.Id,survivors);
                if(result.TryGetArmy("guard/"+home.Id,out _))throw new InvalidOperationException("Virtual defender leaked into map armies.");
                military.ValidateDeployment(result);
                File.WriteAllText(Path.Combine(output,"revision_militar.json"),JsonUtility.ToJson(new Summary{
                    format=data.version,newArmy=newArmy,recruited=20,garrisonFormations=data.military.garrisons.Single(g=>g.settlementId==home.Id).formations.Length,
                    pendingRestored=true,healthRestored=veteranHealth,moraleRestored=veteranMorale,remainingMovement=restored.Movement.RemainingMovement,
                    defenseOutcome=encounter.Battle.Roster.Outcome.ToString(),historyRestored=result.HasResolvedEncounter("review-defense")},true));
                Debug.Log("CAMPAIGN_MILITARY_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public int format,recruited,garrisonFormations,moraleRestored,remainingMovement;public long healthRestored;
            public string newArmy,defenseOutcome;public bool pendingRestored,historyRestored;
        }
    }
}
