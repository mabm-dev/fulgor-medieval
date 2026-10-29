using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    public sealed class LegacyUIReviewRunner:MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Create()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-fulgorLegacyReview");if(i<0||i+1>=args.Length)return;
            var root=new GameObject("Optional legacy interface review");DontDestroyOnLoad(root);root.AddComponent<LegacyUIReviewRunner>().output=args[i+1];
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(output);Screen.SetResolution(1920,1080,false);
            yield return new WaitForSeconds(1);
            var menu=FindAnyObjectByType<CampaignLaunchMenu>();if(menu==null){Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();Capture("menu.png");yield return new WaitForSeconds(.5f);
            foreach(var b in menu.GetComponentsInChildren<Button>())if(b.name=="New game"){b.onClick.Invoke();break;}
            yield return new WaitForEndOfFrame();Capture("campanas.png");yield return new WaitForSeconds(.5f);
            menu.StartCampaign("castilla-conquest");
            for(int i=0;i<600;i++){if(FindAnyObjectByType<CampaignTurnController>()?.State!=null&&!CampaignNavigation.IsTransitioning)break;yield return null;}
            yield return new WaitForSeconds(1);
            var controller=FindAnyObjectByType<CampaignTurnController>();if(controller?.State==null){Application.Quit(1);yield break;}
            controller.GetComponent<CampaignBoardSelection>().LocateCell(controller.Army.Position);
            yield return new WaitForEndOfFrame();Capture("campana.png");yield return new WaitForSeconds(.5f);
            Screen.SetResolution(1280,720,false);yield return new WaitForSeconds(.5f);
            yield return new WaitForEndOfFrame();Capture("campana-720.png");yield return new WaitForSeconds(.5f);
            Debug.Log("LEGACY_UI_REVIEW_OK");Application.Quit(0);
        }
        private void Capture(string name)=>CaptureFrame(Path.Combine(output,name));
        internal static void CaptureFrame(string path)
        {
            var camera=Camera.main;var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var changed=new System.Collections.Generic.List<Canvas>();
            var target=new RenderTexture(Screen.width,Screen.height,24);target.Create();
            var oldTarget=camera.targetTexture;int mask=camera.cullingMask;var oldActive=RenderTexture.active;
            foreach(var canvas in canvases)if(canvas.renderMode==RenderMode.ScreenSpaceOverlay){changed.Add(canvas);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+1;}
            camera.targetTexture=target;camera.cullingMask=-1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            foreach(var canvas in changed){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
            camera.targetTexture=oldTarget;camera.cullingMask=mask;RenderTexture.active=oldActive;target.Release();Destroy(target);Destroy(pixels);
        }
    }
}
