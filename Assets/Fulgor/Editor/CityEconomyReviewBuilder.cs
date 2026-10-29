using System;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    /// <summary>Captures the economy UI and checks its forecast without touching saves.</summary>
    [InitializeOnLoad]
    public static class CityEconomyReviewBuilder
    {
        private const string Running="Fulgor.EconomyReview.Running",Output="Fulgor.EconomyReview.Output",Step="Fulgor.EconomyReview.Step";
        private static int frames;
        private static double started;
        static CityEconomyReviewBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}

        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-fulgorReviewOutput");
                if(index<0||index+1>=args.Length)throw new ArgumentException("Falta directorio de capturas.");
                Directory.CreateDirectory(args[index+1]);
                EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity",OpenSceneMode.Single);
                SessionState.SetString(Output,args[index+1]);SessionState.SetBool(Running,true);SessionState.SetInt(Step,0);
                frames=0;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
                EditorApplication.isPlaying=true;
            }catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
        }

        private static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            int step=SessionState.GetInt(Step,0);
            if(step==2&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;
            if(EditorApplication.timeSinceStartup-started>120){Debug.LogError("Revisión de economía agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||step==2)return;
            var settlements=Object.FindFirstObjectByType<CampaignSettlementController>();
            if(settlements==null||!settlements.Ready||++frames<14)return;
            try {
                if(step==0){settlements.GetComponent<CampaignBoardSelection>().ActivateTile(settlements.Views.Values.First().Tile);frames=0;SessionState.SetInt(Step,1);return;}
                var city=settlements.GetComponent<CityManagementView>();var campaign=settlements.GetComponent<CampaignTurnController>();
                var camera=Camera.main;string output=SessionState.GetString(Output,"");
                city.SelectPanel(CityPanelKind.Buildings);Capture(camera,city,Path.Combine(output,"01_Coste_Y_Beneficio.png"));
                CityConstructionReviewBuilder.StartProject(city,"market");city.SelectPanel(CityPanelKind.Buildings);
                Capture(camera,city,Path.Combine(output,"02_Mercado_En_Obra.png"));
                city.CompleteTurn();var before=campaign.State;city.CompleteTurn();
                if(!campaign.State.Resources.Equals(before.ProjectedResources)||campaign.State.IncomePerTurn.Gold!=campaign.TerritoryIncome.Gold+8)
                    throw new InvalidOperationException("La economía del mercado no coincide con el cierre.");
                city.SelectPanel(CityPanelKind.Treasury);Capture(camera,city,Path.Combine(output,"03_Hacienda_Mercado.png"));
                var forecast=campaign.State.ProjectedResources;city.CompleteTurn();
                if(!campaign.State.Resources.Equals(forecast))throw new InvalidOperationException("La previsión no coincide con las reservas reales.");
                CityConstructionReviewBuilder.CompleteAll(city);city.SelectPanel(CityPanelKind.Overview);
                Capture(camera,city,Path.Combine(output,"04_Actividad_Villa.png"));
                city.SelectPanel(CityPanelKind.Treasury);Capture(camera,city,Path.Combine(output,"05_Hacienda_Villa.png"));
                var upkeep=campaign.State.ExpensesPerTurn;
                File.WriteAllText(Path.Combine(output,"revision_economia.json"),JsonUtility.ToJson(new Summary {
                    forecastMatchesActual=true,buildings=city.BuildingCount,
                    cityGrain=campaign.CityIncomePerTurn.Grain,cityWood=campaign.CityIncomePerTurn.Wood,
                    cityStone=campaign.CityIncomePerTurn.Stone,cityGold=campaign.CityIncomePerTurn.Gold,
                    cityLabor=campaign.CityIncomePerTurn.Labor,upkeepGrain=upkeep.Grain,upkeepGold=upkeep.Gold},true));
                city.Close();Debug.Log("CITY_ECONOMY_REVIEW_OK");SessionState.SetInt(Step,2);EditorApplication.isPlaying=false;
            }catch(Exception exception){Debug.LogException(exception);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }

        private static void Capture(Camera camera,CityManagementView city,string path)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();
            var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            var canvas=city.Viewport.GetComponentInParent<Canvas>();var mode=canvas.renderMode;
            var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var previousTarget=camera.targetTexture;float aspect=camera.aspect;
            try {
                RenderPipeline.SubmitRenderRequest(city.PreviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=city.PreviewCamera.targetTexture});
                camera.targetTexture=target;camera.aspect=1.6f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();city.SelectPanel(city.Panel);Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }finally {
                canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;camera.targetTexture=previousTarget;
                camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            }
        }

        [Serializable]private sealed class Summary
        {
            public bool forecastMatchesActual;
            public int buildings,cityGrain,cityWood,cityStone,cityGold,cityLabor,upkeepGrain,upkeepGold;
        }
    }
}
