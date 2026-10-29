using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CampaignLaunchBuilder
    {
        public const string MenuScene="Assets/Fulgor/Scenes/Playable/FulgorStart.unity";
        public const string ConquestScene="Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        public const string MissionScene="Assets/Fulgor/Scenes/Playable/FulgorMission.unity";
        public const string CatalogPath="Assets/Fulgor/Scenarios/CampaignLaunchCatalog.asset";
        public static void PrepareForTests()
        {try{Prepare();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void PrepareAndBuild()
        {
            try
            {
                Prepare();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorBuildOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Missing build output.");
                string output=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MenuScene,ConquestScene,MissionScene},locationPathName=output,
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),JsonUtility.ToJson(new Summary{
                    result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        private static void Prepare()
        {
            if(!AssetDatabase.IsValidFolder("Assets/Fulgor/Scenes/Playable"))AssetDatabase.CreateFolder("Assets/Fulgor/Scenes","Playable");
            var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<CampaignLaunchCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            if(!File.Exists(MenuScene)||!File.Exists(ConquestScene)||!File.Exists(MissionScene)||catalog.Entries.Count!=2)
            {
                CreatePlayScene("Assets/Fulgor/Scenes/ArtReview/CampaignConclusionReview.unity",ConquestScene,
                    "castilla-conquest","Conquista de Castilla","Gobierna tus ciudades, recluta huestes y disputa el territorio a un rival. Retiradas, victoria y derrota.");
                CreatePlayScene("Assets/Fulgor/Scenes/CampaignPrototype.unity",MissionScene,
                    "castilla-mission","Misión de exploración","Conduce una hueste hasta el objetivo del mapa. Conserva las reglas de la misión anterior.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CatalogPath);
                var root=new GameObject("Fulgor start");root.AddComponent<CampaignLaunchMenu>().Configure(catalog);
                new GameObject("Optional launch review").AddComponent<CampaignLaunchReviewRunner>();
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),MenuScene);AssetDatabase.SaveAssets();
            }
            EditorSceneManager.OpenScene(MenuScene);
            var menu=Object.FindAnyObjectByType<CampaignLaunchMenu>();var attached=menu.GetComponent<CampaignLaunchReviewRunner>();
            if(attached!=null)
            {
                Object.DestroyImmediate(attached);new GameObject("Optional launch review").AddComponent<CampaignLaunchReviewRunner>();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),MenuScene);
            }
            if(Camera.main==null)
            {
                var camera=new GameObject("Menu camera",typeof(Camera));camera.tag="MainCamera";
                camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=new Color(.04f,.06f,.065f);camera.GetComponent<Camera>().cullingMask=0;
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),MenuScene);
            }
            var playable=new[]{MenuScene,ConquestScene,MissionScene};
            var old=EditorBuildSettings.scenes.Where(s=>!playable.Contains(s.path)).Select(s=>new EditorBuildSettingsScene(s.path,false));
            EditorBuildSettings.scenes=playable.Select(s=>new EditorBuildSettingsScene(s,true)).Concat(old).ToArray();
        }
        private static void CreatePlayScene(string source,string target,string id,string title,string description)
        {
            EditorSceneManager.OpenScene(source);var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CatalogPath);var board=Object.FindAnyObjectByType<CampaignBoardPreview>();
            var previous=board.GetComponent<CampaignConclusionReviewRunner>();if(previous!=null)Object.DestroyImmediate(previous);
            board.gameObject.AddComponent<CampaignPlaySession>().Configure(catalog,id);
            var scenario=(CampaignScenarioDefinition)new SerializedObject(board).FindProperty("scenario").objectReferenceValue;
            var list=new SerializedObject(board.GetComponent<CampaignSettlementController>()).FindProperty("settlements");
            var towns=Enumerable.Range(0,list.arraySize).Select(i=>(CampaignSettlementDefinition)list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),target);
            var entry=new CampaignLaunchEntry(id,title,description,Path.GetFileNameWithoutExtension(target),scenario,towns);
            catalog.Configure(catalog.Entries.Where(e=>e.Id!=id).Concat(new[]{entry}).ToArray());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        [Serializable]private sealed class Summary{public string result;public int errors,warnings;}
    }
}
