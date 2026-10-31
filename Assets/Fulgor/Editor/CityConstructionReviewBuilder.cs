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
    public static class CityConstructionReviewBuilder
    {
        const string Running="Fulgor.CityConstruction.Running",Output="Fulgor.CityConstruction.Output",Stage="Fulgor.CityConstruction.Stage";
        static int frames;static double started;
        static CityConstructionReviewBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
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
                if(city.BuildingCount!=3||city.Life.Residents!=4||city.Life.Animals!=2)throw new InvalidOperationException("La ciudad inicial no es pequeña.");
                city.ResetView();Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Inicial.png"));
                city.SelectPanel(CityPanelKind.Buildings);Capture(camera,city,Path.Combine(output,"Ciudad_Obras_SinRecursos.png"));
                StartProject(city,"market");Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Mercado_EnObra.png"));
                if(city.BuildingCount!=3)throw new InvalidOperationException("El mercado aparece antes de terminar.");
                city.CompleteTurn();Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Mercado_Estructura.png"));city.CompleteTurn();
                if(city.BuildingCount!=4||city.Life.Residents!=6)throw new InvalidOperationException("No aparece el mercado con sus vendedores.");
                city.ResetView();city.SelectPanel(CityPanelKind.Overview);Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_ConMercado.png"));
                CompleteAll(city);city.ResetView();city.SelectPanel(CityPanelKind.Buildings);Validate(city);Capture(camera,city,Path.Combine(output,"Ciudad_Ampliada.png"));
                if(city.BuildingCount!=12||city.Life.Residents!=14||city.Life.Animals!=8)throw new InvalidOperationException("No crece toda la villa.");
                Validate(city);
                File.WriteAllText(Path.Combine(output,"revision_construccion.json"),"{\"initial_buildings\":3,\"market_waits_for_completion\":true,\"market_buildings\":4,\"complete_buildings\":12,\"fixed_camera\":true}");
                CityArchitectureReviewBuilder.CaptureDetails(output);
                city.Close();Debug.Log("CITY_CONSTRUCTION_REVIEW_OK");SessionState.SetInt(Stage,2);EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        public static void StartProject(CityManagementView city,string id)
        {
            for(int turn=0;turn<100;turn++) {
                if(city.Construct(id))return;
                city.CompleteTurn();
            }
            throw new InvalidOperationException("No se pudo construir "+id);
        }
        public static void CompleteAll(CityManagementView city)
        {
            var campaign=city.GetComponent<CampaignTurnController>();var settlement=city.GetComponent<CampaignSettlementController>().Registry.Settlements[0];
            foreach(var project in Fulgor.Core.Campaign.CityProjects.All) {
                if(campaign.CityState(settlement.Id).Has(project.Id))continue;
                StartProject(city,project.Id);for(int turn=0;turn<project.Turns;turn++)city.CompleteTurn();
            }
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
