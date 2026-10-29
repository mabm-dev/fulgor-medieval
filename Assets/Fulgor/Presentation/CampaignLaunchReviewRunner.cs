using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignLaunchReviewRunner : MonoBehaviour
    {
        private static bool reviewing;
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorLaunchReview");
            if(reviewing||at<0||at+1>=args.Length)yield break;
            reviewing=true;DontDestroyOnLoad(gameObject);string output=args[at+1];Directory.CreateDirectory(output);
            var routines=new Stack<IEnumerator>();routines.Push(Run(output));
            while(routines.Count>0)
            {
                Exception failure=null;bool next=false;object current=null;
                try{next=routines.Peek().MoveNext();if(next)current=routines.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){Debug.LogException(failure);Application.Quit(1);yield break;}
                if(!next){routines.Pop();continue;}
                if(current is IEnumerator nested){routines.Push(nested);continue;}
                yield return current;
            }
            Application.Quit(0);
        }
        private static IEnumerator Ready(bool menu)
        {
            for(int frame=0;frame<600;frame++)
            {
                if(!CampaignNavigation.IsTransitioning)
                {
                    if(menu&&FindAnyObjectByType<CampaignLaunchMenu>()?.ContinueButton!=null)yield break;
                    if(!menu&&FindAnyObjectByType<CampaignPlaySession>()?.MenuButton!=null&&FindAnyObjectByType<CampaignTurnController>()?.State!=null)yield break;
                }
                yield return null;
            }
            throw new InvalidOperationException("Scene did not become ready.");
        }
        private IEnumerator Run(string output)
        {
            bool had=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);string before=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,"");
            yield return Ready(true);var menu=FindAnyObjectByType<CampaignLaunchMenu>();var catalog=menu.Catalog;
            if(menu.CampaignButtons.Count!=2||!menu.StartCampaign("castilla-conquest"))throw new InvalidOperationException("Start menu does not offer conquest.");
            yield return Ready(false);var campaign=FindAnyObjectByType<CampaignTurnController>();var session=FindAnyObjectByType<CampaignPlaySession>();
            if(campaign.ObjectiveKind!=CampaignObjectiveKind.Conquest||!campaign.Rivals.Enabled)throw new InvalidOperationException("Conquest settings were not loaded.");
            string conquest=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            if(!catalog.TryFindSnapshot(conquest,out var conquestEntry,out _))throw new InvalidOperationException("Fresh conquest snapshot is not recognized.");
            if(!session.Open()||!campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended)throw new InvalidOperationException("Pause did not block map input.");
            session.Resume();if(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended)throw new InvalidOperationException("Resume did not restore map input.");
            session.Open();if(!session.DiscardAndReturn())throw new InvalidOperationException("Returning to start failed.");
            yield return Ready(true);menu=FindAnyObjectByType<CampaignLaunchMenu>();if(!menu.StartCampaign("castilla-mission"))throw new InvalidOperationException("Mission did not launch.");
            yield return Ready(false);campaign=FindAnyObjectByType<CampaignTurnController>();session=FindAnyObjectByType<CampaignPlaySession>();
            if(campaign.ArmyRegistry!=null||campaign.ObjectiveKind!=CampaignObjectiveKind.ReachDestination)throw new InvalidOperationException("Mission mode changed.");
            campaign.CompleteTurn();campaign.CompleteTurn();var stock=campaign.State.Resources;int turn=campaign.State.Turn;
            string snapshot=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            if(!catalog.TryFindSnapshot(snapshot,out var missionEntry,out _))throw new InvalidOperationException("Mission snapshot is not recognized.");
            session.Open();session.DiscardAndReturn();yield return Ready(true);
            if(!CampaignNavigation.Start(missionEntry,snapshot))throw new InvalidOperationException("Snapshot continuation was rejected.");
            yield return Ready(false);campaign=FindAnyObjectByType<CampaignTurnController>();
            if(campaign.State.Turn!=turn||!campaign.State.Resources.Equals(stock))throw new InvalidOperationException("Continuation replayed or lost a turn.");
            session=FindAnyObjectByType<CampaignPlaySession>();session.Open();session.DiscardAndReturn();yield return Ready(true);
            if(!CampaignNavigation.Start(conquestEntry,conquest))throw new InvalidOperationException("Conquest continuation was rejected.");
            yield return Ready(false);campaign=FindAnyObjectByType<CampaignTurnController>();
            if(campaign.ObjectiveKind!=CampaignObjectiveKind.Conquest||campaign.State.Turn!=1)throw new InvalidOperationException("Conquest continuation selected another game.");
            if(had!=PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey)||before!=PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey,""))
                throw new InvalidOperationException("Review modified the player's save slot.");
            File.WriteAllText(Path.Combine(output,"revision_inicio.json"),JsonUtility.ToJson(new Summary{
                campaigns=2,format=JsonUtility.FromJson<CampaignSaveData>(conquest).version,restoredTurn=turn,conquestStarted=true,missionStarted=true,continuedBoth=true,pauseBlocksInput=true,slotUnchanged=true},true));
            Debug.Log("CAMPAIGN_LAUNCH_REVIEW_OK");
        }
        [Serializable]private sealed class Summary
        {public int campaigns,format,restoredTurn;public bool conquestStarted,missionStarted,continuedBoth,pauseBlocksInput,slotUnchanged;}
    }
}
