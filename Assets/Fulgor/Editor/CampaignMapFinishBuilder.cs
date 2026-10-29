using System;
using System.IO;
using System.Linq;
using Fulgor.Core.Map;
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
    public static class CampaignMapFinishBuilder
    {
        const string Asset="Assets/Fulgor/MapAppearance/CastillaMapAppearance.asset";
        const string Scene="Assets/Fulgor/Scenes/CampaignPrototype.unity";
        const string Running="Fulgor.MapFinish.Running",Output="Fulgor.MapFinish.Output",Done="Fulgor.MapFinish.Done";
        static double started;static int frames;
        static CampaignMapFinishBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        [MenuItem("Fulgor/Campaña/Preparar acabado del mapa")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if(!AssetDatabase.IsValidFolder("Assets/Fulgor/MapAppearance"))AssetDatabase.CreateFolder("Assets/Fulgor","MapAppearance");
            var settings=AssetDatabase.LoadAssetAtPath<CampaignMapAppearanceSettings>(Asset);
            if(settings==null){settings=ScriptableObject.CreateInstance<CampaignMapAppearanceSettings>();AssetDatabase.CreateAsset(settings,Asset);}
            var data=new SerializedObject(settings);string art="Assets/Fulgor/Art/Terrain/CastillaMapReview",lod="Assets/Fulgor/Art/Terrain/MapLODReview";
            string[] names={"Mar","Llanura","Bosque","Colina","Montana","LlanuraFortaleza"};
            var full=data.FindProperty("fullMeshes");full.arraySize=6;var mats=data.FindProperty("terrainMaterials");mats.arraySize=5;
            for(int i=0;i<6;i++){full.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Mesh>(art+"/SM_Terreno_"+names[i]+"_Unity.asset");if(i<5)mats.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(art+"/M_Terreno_"+names[i]+".mat");}
            foreach(var level in new[]{1,2}) {
                var array=data.FindProperty(level==1?"mediumMeshes":"distantMeshes");array.arraySize=5;
                for(int i=0;i<5;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Mesh>(lod+"/SM_Terreno_"+names[i+1]+"_LOD"+level+"_Unity.asset");
            }
            data.FindProperty("waterMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fulgor/Art/Water/ConnectedReview/M_AguaConectada.mat");
            // Runtime-created selection materials also need this shader retained in the player.
            data.FindProperty("selectionShader").objectReferenceValue=Shader.Find("Universal Render Pipeline/Unlit");
            data.ApplyModifiedPropertiesWithoutUndo();settings.Validate();
            var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);var board=Object.FindFirstObjectByType<CampaignBoardPreview>();
            var appearance=board.GetComponent<CampaignMapAppearance>();if(appearance==null)appearance=board.gameObject.AddComponent<CampaignMapAppearance>();
            var config=new SerializedObject(appearance);config.FindProperty("settings").objectReferenceValue=settings;config.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
        }
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de capturas.");
                Directory.CreateDirectory(args[at+1]);Prepare();SessionState.SetString(Output,args[at+1]);SessionState.SetBool(Done,false);SessionState.SetBool(Running,true);
                started=EditorApplication.timeSinceStartup;frames=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            if(SessionState.GetBool(Done,false)&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Revisión de mapa agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||SessionState.GetBool(Done,false))return;
            var controller=Object.FindFirstObjectByType<CampaignSettlementController>();if(controller==null||!controller.Ready||++frames<12)return;
            try {
                var board=controller.GetComponent<CampaignBoardPreview>();var style=board.GetComponent<CampaignMapAppearance>();
                if(!style.Ready||style.TileCount!=384)throw new InvalidOperationException("El acabado del mapa no está listo.");
                string output=SessionState.GetString(Output,"");var camera=Camera.main;var controls=camera.GetComponent<CampaignCameraController>();
                Capture(camera,Path.Combine(output,"Mapa_General.png"),true);
                controller.LocateFirst();Capture(camera,Path.Combine(output,"Mapa_Castilla.png"),true);
                var tiles=board.transform.Find("Generated Board").GetComponentsInChildren<HexTileView>();var castle=controller.Views.Values.First();var focus=castle.transform.position;
                foreach(var kind in new[]{TerrainType.Water,TerrainType.Forest,TerrainType.Hill,TerrainType.Mountain}) {
                    var tile=tiles.Where(t=>t.Cell.Terrain==kind).OrderBy(t=>(t.transform.position-focus).sqrMagnitude).First();controls.FocusOnWorld(tile.transform.position);style.RefreshNow();Capture(camera,Path.Combine(output,"Mapa_"+kind+".png"),true);
                }
                controls.enabled=false;camera.transform.position=focus+new Vector3(2,3,-3);camera.transform.LookAt(focus);camera.orthographicSize=1.45f;style.RefreshNow();Capture(camera,Path.Combine(output,"Mapa_Castilla_Detalle.png"),false);controls.enabled=true;
                controller.OpenCityAt(controller.Registry.Settlements[0].Position);var city=controller.GetComponent<CityManagementView>();
                RenderPipeline.SubmitRenderRequest(city.PreviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=city.PreviewCamera.targetTexture});Capture(camera,Path.Combine(output,"Ciudad_MismoAcabado.png"),true);city.Close();
                board.RegenerateBoard();if(!style.Ready||style.TileCount!=384)throw new InvalidOperationException("No se reaplica el acabado al regenerar.");
                File.WriteAllText(Path.Combine(output,"revision_mapa.json"),"{\"campaign_scene\":true,\"tiles\":384,\"connected_water\":true,\"automatic_lod\":true,\"regeneration_checked\":true,\"city_checked\":true}");
                Debug.Log("CAMPAIGN_MAP_FINISH_OK");SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,string path,bool hud)
        {
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();var enabled=canvases.Select(c=>c.enabled).ToArray();
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;var previousTarget=camera.targetTexture;float aspect=camera.aspect;
            try {
                camera.targetTexture=target;camera.aspect=1.6f;
                foreach(var canvas in canvases){if(!hud)canvas.enabled=false;else if(canvas.enabled){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}}
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }finally {for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];canvases[i].enabled=enabled[i];}camera.targetTexture=previousTarget;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
