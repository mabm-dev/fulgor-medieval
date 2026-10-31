using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class CityFramingReviewBuilder
    {
        const string Running="Fulgor.CityFraming.Running",Output="Fulgor.CityFraming.Output",Stage="Fulgor.CityFraming.Stage";
        static int frames;static double started;
        static CityFramingReviewBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de capturas.");
                Directory.CreateDirectory(args[at+1]);EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity",OpenSceneMode.Single);
                SessionState.SetString(Output,args[at+1]);SessionState.SetBool(Running,true);SessionState.SetInt(Stage,0);frames=0;started=EditorApplication.timeSinceStartup;
                EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            int step=SessionState.GetInt(Stage,0);
            if(step==2&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>120){Debug.LogError("Revision de encuadre agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||step==2)return;
            var controller=Object.FindFirstObjectByType<CampaignSettlementController>();if(controller==null||!controller.Ready||++frames<14)return;
            try {
                if(step==0){controller.GetComponent<CampaignBoardSelection>().ActivateTile(controller.Views.Values.First().Tile);frames=0;SessionState.SetInt(Stage,1);return;}
                var city=controller.GetComponent<CityManagementView>();var camera=Camera.main;string output=SessionState.GetString(Output,"");
                CityConstructionReviewBuilder.CompleteAll(city);
                city.ResetView();Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Encuadre_General.png"));
                city.FocusKind(CityBuildingKind.Market);Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Encuadre_Mercado.png"));
                float closest=city.EffectiveDistance;
                Validate(city);
                for(int frame=0;frame<16;frame++){for(int i=0;i<4;i++)city.Life.Advance(.05f);Capture(camera,city,Path.Combine(output,"Paseo_"+frame.ToString("D2")+".png"));}
                File.WriteAllText(Path.Combine(output,"revision_encuadre.json"),"{\"fixed_camera\":true,\"minimum_viewport_margin\":0.025,\"market_selection_checked\":true,\"distance\":"+closest.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                city.Close();Debug.Log("CITY_FRAMING_REVIEW_OK");SessionState.SetInt(Stage,2);EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Validate(CityManagementView city)
        {
            foreach(var renderer in city.PreviewCamera.transform.parent.GetComponentsInChildren<MeshRenderer>()) {
                if(renderer.name=="Terreno de la villa")continue;var bounds=renderer.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                    var p=city.PreviewCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                    if(p.z<=0||p.x<.025f||p.x>.975f||p.y<.025f||p.y>.975f)throw new InvalidOperationException("Objeto fuera de encuadre: "+renderer.name+" "+p);
                }
            }
        }
        static void Capture(Camera camera,CityManagementView city,string path)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            var canvas=city.Viewport.GetComponentInParent<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;var previousTarget=camera.targetTexture;float aspect=camera.aspect;
            try {RenderPipeline.SubmitRenderRequest(city.PreviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=city.PreviewCamera.targetTexture});camera.targetTexture=target;camera.aspect=1.6f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;camera.targetTexture=previousTarget;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
