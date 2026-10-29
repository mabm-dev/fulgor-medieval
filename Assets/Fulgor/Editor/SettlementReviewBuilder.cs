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
    public static class SettlementReviewBuilder
    {
        const string MainScene="Assets/Fulgor/Scenes/CampaignPrototype.unity";
        const string ReviewScene="Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity";
        const string DefinitionPath="Assets/Fulgor/Settlements/FortalezaCastilla.asset";
        const string Running="Fulgor.Settlement.Running",Output="Fulgor.Settlement.Output",Stage="Fulgor.Settlement.Stage";
        static int frames;static double started;
        static SettlementReviewBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        [MenuItem("Fulgor/Campaña/Preparar fortaleza como asentamiento")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!AssetDatabase.IsValidFolder("Assets/Fulgor/Settlements"))AssetDatabase.CreateFolder("Assets/Fulgor","Settlements");
            var scene=EditorSceneManager.OpenScene(MainScene,OpenSceneMode.Single);var board=Object.FindFirstObjectByType<CampaignBoardPreview>();
            var scenario=(CampaignScenarioDefinition)new SerializedObject(board).FindProperty("scenario").objectReferenceValue;
            var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);
            var coordinate=map.Cells.Where(c=>c.Terrain==TerrainType.Plain&&!c.HasGold).OrderBy(c=>Mathf.Pow(c.Coordinates.Q-12,2)+Mathf.Pow(c.Coordinates.R-8,2)).First().Coordinates;
            var definition=AssetDatabase.LoadAssetAtPath<CampaignSettlementDefinition>(DefinitionPath);
            if(definition==null){definition=ScriptableObject.CreateInstance<CampaignSettlementDefinition>();AssetDatabase.CreateAsset(definition,DefinitionPath);}
            var config=new SerializedObject(definition);config.FindProperty("position").vector2IntValue=new Vector2Int(coordinate.Q,coordinate.R);
            config.FindProperty("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fulgor/Prefabs/ArtReview/PF_CastillaV11_LOD.prefab");config.ApplyModifiedPropertiesWithoutUndo();
            Configure(board,definition,false);EditorSceneManager.SaveScene(scene,MainScene);AssetDatabase.SaveAssets();
            var review=EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/GroundedArmiesReview.unity",OpenSceneMode.Single);
            definition=AssetDatabase.LoadAssetAtPath<CampaignSettlementDefinition>(DefinitionPath);
            Configure(Object.FindFirstObjectByType<CampaignBoardPreview>(),definition,true);EditorSceneManager.SaveScene(review,ReviewScene);
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(MainScene,OpenSceneMode.Single);
        }
        static void Configure(CampaignBoardPreview board,CampaignSettlementDefinition definition,bool reuse)
        {
            if(definition==null||definition.Prefab==null)throw new InvalidOperationException("La definicion del asentamiento no esta guardada.");
            var controller=board.GetComponent<CampaignSettlementController>();if(controller==null)controller=board.gameObject.AddComponent<CampaignSettlementController>();
            var data=new SerializedObject(controller);var array=data.FindProperty("settlements");array.arraySize=1;array.GetArrayElementAtIndex(0).objectReferenceValue=definition;
            data.FindProperty("playerFactionId").stringValue="castilla";data.FindProperty("reuseArtReviewFortress").boolValue=reuse;data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de revision.");
                Directory.CreateDirectory(args[at+1]);Prepare();SessionState.SetString(Output,args[at+1]);SessionState.SetInt(Stage,0);SessionState.SetBool(Running,true);
                started=EditorApplication.timeSinceStartup;frames=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            int stage=SessionState.GetInt(Stage,0);
            if(stage==2&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;
            if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Revision de asentamiento agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||stage==2)return;
            var controller=Object.FindFirstObjectByType<CampaignSettlementController>();
            if(controller==null||!controller.Ready||++frames<12)return;
            try {
                if(controller.Registry.Settlements.Count!=1||controller.Views.Count!=1)throw new InvalidOperationException("Fortaleza duplicada o ausente.");
                if(!controller.LocateFirst())throw new InvalidOperationException("No se localiza la fortaleza.");
                var settlement=controller.Registry.Settlements[0];var view=controller.Views[settlement.Position];
                if(!view.Selected||!controller.Details.Contains("Castilla"))throw new InvalidOperationException("Seleccion o propietario incorrectos.");
                var selection=controller.GetComponent<CampaignBoardSelection>();Physics.SyncTransforms();
                if(!Physics.Raycast(new Ray(view.transform.position+Vector3.up*3,Vector3.down),out var hit,6)||selection.ResolveHit(hit.collider)!=view.Tile)throw new InvalidOperationException("El castillo intercepta la seleccion de su casilla.");
                Canvas.ForceUpdateCanvases();var pointer=RectTransformUtility.WorldToScreenPoint(null,controller.LocateButton.transform.position);
                if(!selection.BlocksPointer(pointer))throw new InvalidOperationException("El HUD deja pasar clics al tablero.");
                var camera=Camera.main;var controls=camera.GetComponent<CampaignCameraController>();if(controls!=null)controls.enabled=false;
                var focus=view.transform.position+Vector3.up*.15f;camera.transform.position=focus+new Vector3(0,7,-5);camera.transform.LookAt(focus);camera.orthographicSize=2.2f;
                string output=SessionState.GetString(Output,"");Capture(camera,Path.Combine(output,stage==0?"Asentamiento_Campana.png":"Asentamiento_TerrenoArtistico.png"),true);
                camera.transform.position=focus+new Vector3(2,3,-3);camera.transform.LookAt(focus);camera.orthographicSize=1.1f;
                Capture(camera,Path.Combine(output,stage==0?"Fortaleza_Campana_Detalle.png":"Fortaleza_Artistica_Detalle.png"),false);
                File.WriteAllText(Path.Combine(output,stage==0?"revision_campana.json":"revision_artistica.json"),"{\"settlements\":1,\"owner\":\"castilla\",\"tile\":\""+settlement.Position.Key+"\",\"selection_checked\":true,\"hit_resolves_to_tile\":true,\"hud_blocks_pointer\":true,\"art_sample_reused\":"+(stage==1?"true":"false")+"}");
                if(stage==0){SessionState.SetInt(Stage,1);frames=0;EditorSceneManager.LoadSceneInPlayMode(ReviewScene,new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));}
                else {Debug.Log("SETTLEMENT_REVIEW_OK campaign_and_art");SessionState.SetInt(Stage,2);EditorApplication.isPlaying=false;}
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,string path,bool hud)
        {
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();var enabled=canvases.Select(c=>c.enabled).ToArray();
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try {
                foreach(var canvas in canvases){if(!hud)canvas.enabled=false;else {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}}
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }finally {
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];canvases[i].enabled=enabled[i];}
                RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            }
        }
    }
}
