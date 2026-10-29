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
    public static class CampaignRivalReviewBuilder
    {
        private const string Scene="Assets/Fulgor/Scenes/ArtReview/CampaignRivalReview.unity";
        private const string Scenario="Assets/Fulgor/Scenarios/RivalReview.asset";
        public static void PrepareAndBuild()
        {
            try
            {
                if(!File.Exists(Scene))
                {
                    if(File.Exists(Scenario))throw new InvalidOperationException("Review scenario exists without its scene.");
                    EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/CampaignMilitaryReview.unity");
                    var board=Object.FindAnyObjectByType<CampaignBoardPreview>();var settings=new SerializedObject(board);
                    var scenario=Object.Instantiate((CampaignScenarioDefinition)settings.FindProperty("scenario").objectReferenceValue);scenario.name="RivalReview";
                    var data=new SerializedObject(scenario);data.FindProperty("enableStrategicRivals").boolValue=true;
                    var armies=data.FindProperty("armies");armies.arraySize=4;
                    Configure(armies.GetArrayElementAtIndex(0),"Castilla-1","castilla",10);
                    Configure(armies.GetArrayElementAtIndex(1),"Castilla-2","castilla",20);
                    Configure(armies.GetArrayElementAtIndex(2),"Rival-A","rival",50);
                    var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);var graph=scenario.CreateRouteGraph(map);
                    var first=armies.GetArrayElementAtIndex(0).FindPropertyRelative("position").vector2IntValue;var home=new HexCoordinates(first.x,first.y);
                    var far=map.Cells.First(c=>graph.CanStandAt(c.Coordinates)&&c.Coordinates.DistanceTo(home)>6).Coordinates;
                    Configure(armies.GetArrayElementAtIndex(3),"Rival-Z","rival",5);armies.GetArrayElementAtIndex(3).FindPropertyRelative("position").vector2IntValue=new Vector2Int(far.Q,far.R);
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(scenario,Scenario);settings.FindProperty("scenario").objectReferenceValue=scenario;settings.ApplyModifiedPropertiesWithoutUndo();
                    var previous=board.GetComponent<CampaignMilitaryReviewRunner>();if(previous!=null)Object.DestroyImmediate(previous);
                    board.gameObject.AddComponent<CampaignRivalReviewRunner>();
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);AssetDatabase.SaveAssets();
                }
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorBuildOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Missing build output.");
                string output=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),JsonUtility.ToJson(new Summary{
                    result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        private static void Configure(SerializedProperty army,string id,string faction,int quantity)
        {
            army.FindPropertyRelative("id").stringValue=id;army.FindPropertyRelative("factionId").stringValue=faction;army.FindPropertyRelative("movementPerTurn").intValue=6;
            var formations=army.FindPropertyRelative("formations");formations.arraySize=1;
            var f=formations.GetArrayElementAtIndex(0);f.FindPropertyRelative("id").stringValue=id+"-tropas";
            f.FindPropertyRelative("profile").enumValueIndex=0;f.FindPropertyRelative("quantity").intValue=quantity;
        }
        [Serializable]private sealed class Summary{public string result;public int errors,warnings;}
    }
}
