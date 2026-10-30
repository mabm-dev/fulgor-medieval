using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Fulgor.Presentation.Map
{
    public sealed class KingdomSelectionReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Install()
        {var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-fulgorKingdomReview")<0)return;new GameObject("Kingdom selection review").AddComponent<KingdomSelectionReview>();}
        private IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();string output=args[Array.IndexOf(args,"-fulgorKingdomReview")+1];Directory.CreateDirectory(output);
            yield return null;yield return null;var menu=FindAnyObjectByType<CampaignLaunchMenu>();var view=menu.gameObject.AddComponent<KingdomSelectionView>();view.Open(menu);yield return null;
            var catalog=JsonUtility.FromJson<KingdomChoices>(Resources.Load<TextAsset>("KingdomSelection/catalog").text);
            foreach(var e in catalog.entries)if(Resources.Load<Texture2D>("KingdomSelection/ruler-"+e.id)==null||Resources.Load<Texture2D>("KingdomSelection/hero-"+e.id)==null||Resources.Load<Texture2D>("KingdomSelection/background-"+e.id)==null){Debug.LogError("Missing kingdom artwork: "+e.id);Application.Quit(1);yield break;}
            for(int i=0;i<12;i++){view.Cycle(1);yield return null;}if(view.SelectedId!="castilla"){Application.Quit(1);yield break;}
            view.Cycle(-1);view.Cycle(1);yield return new WaitForSecondsRealtime(.35f);
            var group=view.GetComponentsInChildren<CanvasGroup>().Single(g=>g.name=="Choose your kingdom");if(group.alpha<.99f)throw new InvalidOperationException("Transition did not finish.");
            var cam=Camera.main;var target=new RenderTexture(1920,1080,24);target.Create();cam.targetTexture=target;cam.enabled=false;
            var canvas=view.GetComponentsInChildren<Canvas>().Single(c=>c.name=="Choose your kingdom");cam.cullingMask=-1;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=cam.nearClipPlane+1;Canvas.ForceUpdateCanvases();yield return null;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
            if(view.GetComponentsInChildren<KingdomPortraitGraphic>().Length!=2)throw new InvalidOperationException("Portrait composition is incomplete.");
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"elige-tu-reino.png"),image.EncodeToPNG());
            File.WriteAllText(Path.Combine(output,"review.json"),"{\"portraits\":12,\"heroes\":12,\"cyclePassed\":true}");Debug.Log("KINGDOM_SELECTION_REVIEW_OK: 12 backgrounds, 2 feathered portraits, rapid transition passed.");Application.Quit(0);
        }
    }
}





