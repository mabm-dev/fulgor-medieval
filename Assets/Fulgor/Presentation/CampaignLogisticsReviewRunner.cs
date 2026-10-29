using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Opt-in real-player check of normal campaign accounting and persistence.</summary>
    public sealed class CampaignLogisticsReviewRunner : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Create()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorLogisticsReview");
            if(at<0||at+1>=args.Length||FindAnyObjectByType<CampaignLogisticsReviewRunner>()!=null)return;
            var root=new GameObject("Optional logistics review");DontDestroyOnLoad(root);root.AddComponent<CampaignLogisticsReviewRunner>().output=args[at+1];
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
            Directory.CreateDirectory(output);bool had=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);string previous=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            CampaignLaunchMenu menu=null;for(int i=0;i<600;i++){menu=FindAnyObjectByType<CampaignLaunchMenu>();if(menu?.ContinueButton!=null)break;yield return null;}
            if(menu==null||!menu.StartCampaign("castilla-conquest"))throw new InvalidOperationException("Conquest did not start.");
            CampaignTurnController campaign=null;for(int i=0;i<600;i++){campaign=FindAnyObjectByType<CampaignTurnController>();if(!CampaignNavigation.IsTransitioning&&campaign?.State!=null)break;yield return null;}
            yield return null;yield return null;
            if(!campaign.LogisticsEnabled)throw new InvalidOperationException("Normal conquest has no logistics.");
            var data=campaign.CaptureCampaignSnapshot();if(data.version!=(campaign.FogEnabled?11:10))throw new InvalidOperationException("Logistics did not use v10.");
            var own=data.armyRegistry.armies.First(a=>campaign.ArmyRegistry.TryGetArmy(a.id,out var army)&&army.Army.FactionId==campaign.PlayerFactionId);
            own.formations[0].health-=3;own.formations[0].morale=40;data.grain=data.gold=100000;
            if(!campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(data)))throw new InvalidOperationException("Valid wounded snapshot was rejected.");
            var expected=campaign.State.ProjectedResources;var cost=campaign.ArmyUpkeepPerTurn;int turn=campaign.State.Turn;
            campaign.CompleteTurn();
            if(campaign.State.Turn!=turn+1||!campaign.State.Resources.Equals(expected))throw new InvalidOperationException("Military upkeep differs from the forecast.");
            var reports=campaign.LastLogisticsReport.Reports;var report=reports.First(r=>r.ArmyId==own.id);
            if(!report.Paid||!report.Supplied||report.Healing!=2||report.MoraleChange!=5)throw new InvalidOperationException("Paid rest did not recover exact wounds and morale.");
            // Complete any genuine rival encounters before testing the campaign save.
            for(int n=0;n<20&&campaign.HasPendingBattle;n++)
            {
                var battle=campaign.GetComponent<BattleLabController>();
                for(int step=0;step<1000&&battle.ActiveCampaignEncounter.Roster.Outcome==BattleOutcome.InProgress;step++)battle.AdvanceCampaignAutomaticStep();
                if(!battle.ReturnToCampaign())throw new InvalidOperationException("Rival encounter did not reconcile.");yield return null;
            }
            if(campaign.HasPendingBattle||campaign.IsRivalPhaseActive)throw new InvalidOperationException("Rival closing remains pending.");
            string snapshot=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());var stock=campaign.State.Resources;
            if(!campaign.TryLoadCampaignSnapshot(snapshot)||JsonUtility.ToJson(campaign.CaptureCampaignSnapshot())!=snapshot||!stock.Equals(campaign.State.Resources))throw new InvalidOperationException("Reload replayed logistics or changed survivors.");
            if(had!=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey)||previous!=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,""))throw new InvalidOperationException("Review modified the player's save slot.");
            File.WriteAllText(Path.Combine(output,"revision_logistica.json"),JsonUtility.ToJson(new Summary{format=campaign.FogEnabled?11:10,turn=campaign.State.Turn,grainUpkeep=cost.Grain,goldUpkeep=cost.Gold,healed=report.Healing,morale=report.MoraleChange,forecastMatches=true,reloadExact=true,slotUnchanged=true},true));
            Debug.Log("CAMPAIGN_LOGISTICS_REVIEW_OK");Application.Quit(0);
        }
        [Serializable]private sealed class Summary{public int format,turn,grainUpkeep,goldUpkeep,morale;public long healed;public bool forecastMatches,reloadExact,slotUnchanged;}
    }
}
