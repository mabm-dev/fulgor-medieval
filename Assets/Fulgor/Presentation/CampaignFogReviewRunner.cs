using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignFogReviewRunner : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Create()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorFogReview");
            if(at<0||at+1>=args.Length||FindAnyObjectByType<CampaignFogReviewRunner>()!=null)return;
            var root=new GameObject("Optional fog review");DontDestroyOnLoad(root);root.AddComponent<CampaignFogReviewRunner>().output=args[at+1];
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;var run=Run();
            while(true)
            {
                bool next;object current=null;
                try{next=run.MoveNext();if(next)current=run.Current;}catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
                if(!next)break;yield return current;
            }
        }
        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);bool had=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);string saved=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            CampaignLaunchMenu menu=null;for(int i=0;i<600;i++){menu=FindAnyObjectByType<CampaignLaunchMenu>();if(menu?.ContinueButton!=null)break;yield return null;}
            if(menu==null)throw new InvalidOperationException("No launch menu.");var catalog=menu.Catalog;
            if(!menu.StartCampaign("castilla-conquest"))throw new InvalidOperationException("Conquest did not launch.");
            CampaignTurnController campaign=null;
            for(int i=0;i<600;i++){campaign=FindAnyObjectByType<CampaignTurnController>();if(!CampaignNavigation.IsTransitioning&&campaign?.State!=null&&campaign.GetComponent<CampaignSettlementController>().Ready)break;yield return null;}
            yield return null;yield return null;
            if(campaign?.FogEnabled!=true||campaign.Fog==null)throw new InvalidOperationException("Conquest has no fog.");
            var selection=campaign.GetComponent<CampaignBoardSelection>();var view=campaign.GetComponent<CampaignFogMap>();view.ApplyNow();
            int unknown=view.UnknownCount,before=campaign.Fog.ExploredCount(campaign.PlayerFactionId);
            var remote=campaign.ArmyRegistry.Armies.First(a=>a.Army.FactionId!=campaign.PlayerFactionId&&!campaign.CanObserve(a.Movement.Position));
            selection.LocateCell(remote.Movement.Position);
            if(campaign.AttackSelectedArmy()||campaign.HasPendingBattle||campaign.GetComponent<CampaignSelectionPanel>().MoveArmyButton.interactable)throw new InvalidOperationException("Unseen enemy accepts a remote order.");
            var marker=campaign.GetComponentsInChildren<CampaignArmyMarker>().Single(m=>m.gameObject.name=="Hueste "+remote.Army.Id);
            if(marker.IsVisible||unknown==0)throw new InvalidOperationException("Remote army or terrain is exposed.");
            string actor=campaign.SelectedArmyId;var scenario=catalog.Find("castilla-conquest").Scenario;
            var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);HexCoordinates? destination=null;MapPath planned=null;
            foreach(var cell in map.Cells)
            {
                if(!campaign.CanObserve(cell.Coordinates)||campaign.SettlementState.TryGet(cell.Coordinates,out _))continue;
                if(!CampaignFogOrders.TryPlan(actor,cell.Coordinates,campaign.ArmyRegistry,campaign.Military,campaign.SettlementState,selection.Routes,campaign.Fog,out var path,out _)||path.Steps==0||path.TotalCost>campaign.Army.RemainingMovement)continue;
                var trial=CampaignFogOrders.Advance(actor,cell.Coordinates,campaign.ArmyRegistry,campaign.Military,campaign.SettlementState,selection.Routes,campaign.Fog,campaign.State.Turn);
                if(trial.Arrived&&trial.Fog.ExploredCount(campaign.PlayerFactionId)>before){destination=cell.Coordinates;planned=path;break;}
            }
            if(!destination.HasValue)throw new InvalidOperationException("No observable frontier route.");
            int movement=campaign.Army.RemainingMovement;var stock=campaign.State.Resources;
            selection.LocateCell(destination.Value);campaign.MoveToSelectedCell();yield return null;
            int after=campaign.Fog.ExploredCount(campaign.PlayerFactionId);
            if(campaign.Army.Position!=destination.Value||campaign.Army.RemainingMovement!=movement-planned.TotalCost||after<=before||!stock.Equals(campaign.State.Resources))throw new InvalidOperationException("Exploration movement changed its cost or resources.");
            string snapshot=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            if(!catalog.TryFindSnapshot(snapshot,out var entry,out var payload)||payload.version!=11)throw new InvalidOperationException("Menu does not recognize exploration.");
            if(!campaign.TryLoadCampaignSnapshot(snapshot)||JsonUtility.ToJson(campaign.CaptureCampaignSnapshot())!=snapshot)throw new InvalidOperationException("Exploration reload differs.");
            // Build the compatible v10 identity using the same immutable launch configuration.
            var routes=scenario.CreateRouteGraph(map);var initial=scenario.CreateArmyRegistry(routes);var mission=scenario.CreateMission(map,routes,initial);
            var owners=new CampaignSettlementRegistry(routes,entry.Settlements.Select(s=>s.Create()));
            string context=CampaignSaveData.ScenarioSignature(map,routes,mission);context=CampaignSaveData.ArmyScenarioSignature(context,initial,scenario.PlayerFactionId);
            context=CampaignSaveData.SettlementScenarioSignature(context,owners);context=CampaignSaveData.MilitaryScenarioSignature(context);context=CampaignSaveData.MarchScenarioSignature(context);
            context=CampaignSaveData.RivalScenarioSignature(context,scenario.EnableStrategicRivals);context=CampaignSaveData.CampaignRulesSignature(context,scenario.ObjectiveKind);context=CampaignSaveData.LogisticsSignature(context);
            payload.version=10;payload.context=context;payload.exploration=null;
            if(!campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(payload))||campaign.Fog.ExploredCount(campaign.PlayerFactionId)!=map.Cells.Count||campaign.CanObserve(remote.Movement.Position))throw new InvalidOperationException("Legacy map knowledge or current enemy visibility was lost.");
            int legacy=campaign.Fog.ExploredCount(campaign.PlayerFactionId);
            if(!campaign.TryLoadCampaignSnapshot(snapshot)||campaign.Fog.ExploredCount(campaign.PlayerFactionId)!=after)throw new InvalidOperationException("Loading earlier knowledge retained a later full map.");
            if(had!=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey)||saved!=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,""))throw new InvalidOperationException("Review modified the player's save.");
            File.WriteAllText(Path.Combine(output,"revision_niebla.json"),JsonUtility.ToJson(new Summary{format=11,beforeExplored=before,afterExplored=after,unknownTiles=unknown,legacyExplored=legacy,hiddenEnemy=true,remoteOrderRejected=true,moveConfirmed=true,reloadExact=true,slotUnchanged=true},true));
            Debug.Log("CAMPAIGN_FOG_REVIEW_OK");Application.Quit(0);
        }
        [Serializable]private sealed class Summary{public int format,beforeExplored,afterExplored,unknownTiles,legacyExplored;public bool hiddenEnemy,remoteOrderRejected,moveConfirmed,reloadExact,slotUnchanged;}
    }
}
