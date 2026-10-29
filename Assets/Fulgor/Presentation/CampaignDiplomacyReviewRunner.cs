using System;
using System.Collections;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
namespace Fulgor.Presentation.Map
{
    // Opt-in executable review; never reads or writes the player's save slot.
    public sealed class CampaignDiplomacyReviewRunner : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Install()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorDiplomacyReview");
            if(at<0||at+1>=args.Length)return;
            var runner=new GameObject("Diplomacy build review").AddComponent<CampaignDiplomacyReviewRunner>();runner.output=args[at+1];DontDestroyOnLoad(runner.gameObject);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(output);
            for(int i=0;i<600&&FindAnyObjectByType<CampaignLaunchMenu>()?.ContinueButton==null;i++)yield return null;
            var menu=FindAnyObjectByType<CampaignLaunchMenu>();
            if(menu==null||!menu.StartCampaign("castilla-conquest")){Fail("Cannot launch conquest");yield break;}
            for(int i=0;i<600&&(FindAnyObjectByType<CampaignTurnController>()?.State==null||FindAnyObjectByType<CampaignPlaySession>()?.MenuButton==null);i++)yield return null;
            yield return null;yield return null;
            var c=FindAnyObjectByType<CampaignTurnController>();
            if(c?.CanManageDiplomacy!=true){Fail("Diplomacy unavailable");yield break;}
            string rival=c.Diplomacy.Factions.First();
            c.SetDiplomaticRelation(rival,DiplomaticRelation.Pact,DiplomaticIntention.Neutral);
            c.ProposeDiplomacy(rival,DiplomaticRelation.Trade);
            string snapshot=JsonUtility.ToJson(c.CaptureCampaignSnapshot());
            c.SetDiplomaticRelation(rival,DiplomaticRelation.War,DiplomaticIntention.Conquest);
            if(!c.TryLoadCampaignSnapshot(snapshot)||c.Diplomacy.CanAttack(c.PlayerFactionId,rival)){Fail("Treaty did not survive load");yield break;}
            var council=c.GetComponent<CampaignDiplomacyView>();council.Open();
            if(!council.IsOpen||!c.GetComponent<CampaignBoardSelection>().IsInteractionSuspended){Fail("Council did not block map input");yield break;}
            yield return null;yield return new WaitForEndOfFrame();
            CaptureCouncil(c);
            for(int i=0;i<20;i++)yield return null;
            File.WriteAllText(Path.Combine(output,"diplomacy-review.json"),"{\"passed\":true,\"snapshotRestored\":true,\"treatyBlocksHostilities\":true,\"councilBlocksInput\":true}");
            Debug.Log("DIPLOMACY_BUILD_REVIEW_OK");Application.Quit(0);
        }
        private void CaptureCouncil(CampaignTurnController campaign)
        {
            var camera=Camera.main;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);target.Create();
            camera.targetTexture=target;camera.enabled=false;
            var canvas=campaign.GetComponentsInChildren<Canvas>().Single(item=>item.name=="Diplomacy council");
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,"diplomacia-1920.png"),image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;target.Release();Destroy(target);Destroy(image);
        }
        private void Fail(string reason){Debug.LogError(reason);File.WriteAllText(Path.Combine(output,"diplomacy-review.json"),"{\"passed\":false}");Application.Quit(1);}
    }
}


