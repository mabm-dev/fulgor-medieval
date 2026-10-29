using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    // Explicit command-line review. Never writes the player's save slot.
    public sealed class CampaignMarchReviewRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorMarchReview");
            if(at<0||at+1>=args.Length)yield break;
            string output=args[at+1];Directory.CreateDirectory(output);yield return null;yield return null;
            try
            {
                var campaign=GetComponent<CampaignTurnController>();var selection=GetComponent<CampaignBoardSelection>();var panel=GetComponent<CampaignSelectionPanel>();
                var board=GetComponent<CampaignBoardPreview>();var initial=board.InitialArmyRegistry;var initialOwners=GetComponent<CampaignSettlementController>().CreateInitialRegistry(selection.Routes);
                string id=campaign.SelectedArmyId;var from=campaign.Army.Position;
                var map=ProceduralMapGenerator.Generate(24,16,12345);
                var target=map.Cells.Select(c=>c.Coordinates).First(cell=>campaign.Military.TryPlanMarch(campaign.ArmyRegistry,id,cell,out var route,out _)&&route.TotalCost>12);
                selection.LocateCell(target);
                if(!panel.MoveArmyButton.interactable)throw new InvalidOperationException("Long march HUD action is unavailable.");
                var stock=campaign.State.Resources;panel.MoveArmyButton.onClick.Invoke();
                if(campaign.MarchFor(id)!=target||campaign.Army.Position==from||campaign.Army.Position==target||!stock.Equals(campaign.State.Resources))
                    throw new InvalidOperationException("Long march did not advance partially at its movement cost.");
                panel.CancelMarchButton.onClick.Invoke();if(campaign.MarchFor(id).HasValue)throw new InvalidOperationException("March cancellation failed.");
                var position=campaign.Army.Position;var movement=campaign.Army.RemainingMovement;
                panel.MoveArmyButton.onClick.Invoke();if(campaign.MarchFor(id)!=target||campaign.Army.RemainingMovement>movement)
                    throw new InvalidOperationException("Replacement march refilled movement or lost its order.");
                position=campaign.Army.Position;movement=campaign.Army.RemainingMovement;
                var data=CampaignSaveData.CaptureMultiple("review",campaign.State,campaign.ArmyRegistry,id,campaign.PlayerFactionId);
                data.IncludeOwnership("review",initialOwners,campaign.SettlementState);data.IncludeMilitary(campaign.Military);data.IncludeMarches(campaign.MarchOrders);
                data=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(data));var owners=data.RestoreSettlementOwners(initialOwners,new[]{"castilla","rival"});
                var military=CampaignMilitaryState.Restore(data.military,initial,owners,new[]{"castilla","rival"},data.turn);
                data.RestoreMultiple(CampaignSaveData.MarchScenarioSignature(CampaignSaveData.MilitaryScenarioSignature(CampaignSaveData.SettlementScenarioSignature("review",initialOwners))),
                    initial,campaign.PlayerFactionId,campaign.TerritoryIncome,out _,out var restored,military);
                var orders=data.RestoreMarches(restored,campaign.PlayerFactionId);restored.TryGetArmy(id,out var army);
                if(orders.Destination(id)!=target||army.Movement.Position!=position||army.Movement.RemainingMovement!=movement)
                    throw new InvalidOperationException("March order or spent movement was lost in the v7 round trip.");
                int turns=0;
                while(campaign.MarchFor(id).HasValue&&turns<100){campaign.CompleteTurn();turns++;}
                campaign.ArmyRegistry.TryGetArmy(id,out var arrived);
                if(arrived.Movement.Position!=target||campaign.MarchFor(id).HasValue||campaign.HasPendingBattle)
                    throw new InvalidOperationException("March did not arrive cleanly across turn boundaries.");
                File.WriteAllText(Path.Combine(output,"revision_marchas.json"),JsonUtility.ToJson(new Summary {
                    format=data.version,army=id,from=from.Key,destination=target.Key,arrived=arrived.Movement.Position.Key,turns=turns,
                    movementAfterLoad=movement,cancelled=true,restored=true,automaticBattle=false},true));
                Debug.Log("CAMPAIGN_MARCH_REVIEW_OK");Application.Quit(0);
            }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
        }
        [Serializable]private sealed class Summary
        {
            public int format,turns,movementAfterLoad;public string army,from,destination,arrived;
            public bool cancelled,restored,automaticBattle;
        }
    }
}
