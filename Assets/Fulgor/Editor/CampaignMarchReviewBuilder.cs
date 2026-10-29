using System;
using System.IO;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CampaignMarchReviewBuilder
    {
        private const string Scene="Assets/Fulgor/Scenes/ArtReview/CampaignMarchReview.unity";
        private const string Scenario="Assets/Fulgor/Scenarios/MarchReview.asset";
        public static void PrepareAndBuild()
        {
            try
            {
                if(!File.Exists(Scene))
                {
                    if(File.Exists(Scenario))throw new InvalidOperationException("Review scenario exists without its scene.");
                    EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/CampaignMilitaryReview.unity");
                    var board=Object.FindAnyObjectByType<CampaignBoardPreview>();var settings=new SerializedObject(board);
                    var scenario=Object.Instantiate((CampaignScenarioDefinition)settings.FindProperty("scenario").objectReferenceValue);scenario.name="MarchReview";
                    var data=new SerializedObject(scenario);var armies=data.FindProperty("armies");
                    for(int i=0;i<armies.arraySize;i++)if(armies.GetArrayElementAtIndex(i).FindPropertyRelative("factionId").stringValue=="castilla")
                        armies.GetArrayElementAtIndex(i).FindPropertyRelative("movementPerTurn").intValue=2;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(scenario,Scenario);settings.FindProperty("scenario").objectReferenceValue=scenario;settings.ApplyModifiedPropertiesWithoutUndo();
                    var previous=board.GetComponent<CampaignMilitaryReviewRunner>();if(previous!=null)Object.DestroyImmediate(previous);
                    board.gameObject.AddComponent<CampaignMarchReviewRunner>();
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
        [Serializable]private sealed class Summary{public string result;public int errors,warnings;}
    }
}
