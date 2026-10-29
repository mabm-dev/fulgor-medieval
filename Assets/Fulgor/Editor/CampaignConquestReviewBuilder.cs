using System;
using System.IO;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CampaignConquestReviewBuilder
    {
        private const string Scene="Assets/Fulgor/Scenes/ArtReview/CampaignConquestReview.unity";
        private const string Scenario="Assets/Fulgor/Scenarios/ConquestReview.asset";
        private const string Rival="Assets/Fulgor/Settlements/RivalConquestReview.asset";
        public static void PrepareAndBuild()
        {
            try
            {
                if(!File.Exists(Scene))Prepare();
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorBuildOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Missing build output.");
                string output=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName=output,
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),JsonUtility.ToJson(new Summary {
                    result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        private static void Prepare()
        {
            if(File.Exists(Scenario)||File.Exists(Rival))throw new InvalidOperationException("Review assets already exist without their scene.");
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/CampaignCombatReview.unity");
            var board=Object.FindAnyObjectByType<CampaignBoardPreview>();var settings=new SerializedObject(board);
            var scenario=Object.Instantiate((CampaignScenarioDefinition)settings.FindProperty("scenario").objectReferenceValue);scenario.name="ConquestReview";
            var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);var graph=scenario.CreateRouteGraph(map);
            var towns=board.GetComponent<CampaignSettlementController>();var townData=new SerializedObject(towns);var list=townData.FindProperty("settlements");
            var home=(CampaignSettlementDefinition)list.GetArrayElementAtIndex(0).objectReferenceValue;var start=home.Create().Position;
            var target=start.Neighbors().First(n=>graph.TryGetMovementCost(start,n,out _));
            var ally=start.Neighbors().First(n=>n!=target&&graph.TryGetMovementCost(start,n,out _));
            var rival=Object.Instantiate(home);rival.name="RivalConquestReview";var data=new SerializedObject(rival);
            data.FindProperty("id").stringValue="fortaleza-rival";data.FindProperty("displayName").stringValue="Fortaleza rival";
            data.FindProperty("ownerFactionId").stringValue="rival";data.FindProperty("ownerDisplayName").stringValue="Señorío rival";
            data.FindProperty("position").vector2IntValue=new Vector2Int(target.Q,target.R);data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(rival,Rival);
            list.arraySize=2;list.GetArrayElementAtIndex(0).objectReferenceValue=home;list.GetArrayElementAtIndex(1).objectReferenceValue=rival;townData.ApplyModifiedPropertiesWithoutUndo();
            var scenarioData=new SerializedObject(scenario);var armies=scenarioData.FindProperty("armies");armies.arraySize=3;
            SetArmy(armies.GetArrayElementAtIndex(0),"Castilla-1","castilla",start,new[]{0,2},new[]{50,30});
            SetArmy(armies.GetArrayElementAtIndex(1),"Castilla-2","castilla",ally,new[]{3},new[]{20});
            SetArmy(armies.GetArrayElementAtIndex(2),"Guarnicion-rival","rival",target,new[]{0},new[]{2});
            scenarioData.ApplyModifiedPropertiesWithoutUndo();scenario.CreateMission(map,graph,scenario.CreateArmyRegistry(graph));
            AssetDatabase.CreateAsset(scenario,Scenario);settings.FindProperty("scenario").objectReferenceValue=scenario;settings.ApplyModifiedPropertiesWithoutUndo();
            var old=board.GetComponent<CampaignCombatReviewCapture>();if(old!=null)Object.DestroyImmediate(old);
            board.gameObject.AddComponent<CampaignConquestReviewRunner>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);AssetDatabase.SaveAssets();
        }
        private static void SetArmy(SerializedProperty army,string id,string faction,HexCoordinates at,int[] profiles,int[] quantities)
        {
            army.FindPropertyRelative("id").stringValue=id;army.FindPropertyRelative("factionId").stringValue=faction;
            army.FindPropertyRelative("position").vector2IntValue=new Vector2Int(at.Q,at.R);army.FindPropertyRelative("movementPerTurn").intValue=6;
            var formations=army.FindPropertyRelative("formations");formations.arraySize=profiles.Length;
            for(int i=0;i<profiles.Length;i++){var f=formations.GetArrayElementAtIndex(i);f.FindPropertyRelative("id").stringValue=id+"-tropas-"+i;f.FindPropertyRelative("profile").enumValueIndex=profiles[i];f.FindPropertyRelative("quantity").intValue=quantities[i];}
        }
        [Serializable]private sealed class Summary{public string result;public int errors,warnings;}
    }
}
