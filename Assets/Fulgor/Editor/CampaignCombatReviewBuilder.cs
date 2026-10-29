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
    public static class CampaignCombatReviewBuilder
    {
        private const string Scene="Assets/Fulgor/Scenes/ArtReview/CampaignCombatReview.unity";
        private const string Scenario="Assets/Fulgor/Scenarios/CombatReview.asset";
        public static void PrepareAndBuild()
        {
            try
            {
                if(!File.Exists(Scene))Prepare();
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorBuildOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Missing build output.");
                var output=args[at+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},
                    locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4});
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"build-summary.json"),JsonUtility.ToJson(new Summary {
                    result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings},true));
                EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        private static void Prepare()
        {
            if(File.Exists(Scenario))throw new InvalidOperationException("Review scenario already exists without its scene.");
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/CampaignMultiArmyReview.unity");
            var board=Object.FindAnyObjectByType<CampaignBoardPreview>();var config=new SerializedObject(board);
            var source=(CampaignScenarioDefinition)config.FindProperty("scenario").objectReferenceValue;
            var scenario=Object.Instantiate(source);scenario.name="CombatReview";
            var map=ProceduralMapGenerator.Generate(scenario.Width,scenario.Height,scenario.Seed);var graph=scenario.CreateRouteGraph(map);
            var start=source.CreateArmyRegistry(graph).Armies.First(a=>a.Army.FactionId=="castilla").Movement.Position;
            var enemy=start.Neighbors().First(n=>graph.TryGetMovementCost(start,n,out _));
            var ally=map.Cells.First(c=>c.Coordinates!=start&&c.Coordinates!=enemy&&c.Coordinates.DistanceTo(start)==2&&MapPathfinder.TryFindPath(graph,start,c.Coordinates,out _)).Coordinates;
            var data=new SerializedObject(scenario);var armies=data.FindProperty("armies");armies.arraySize=3;
            SetArmy(armies.GetArrayElementAtIndex(0),"Castilla-1","castilla",start,new[]{0,2},new[]{50,30});
            SetArmy(armies.GetArrayElementAtIndex(1),"Castilla-2","castilla",ally,new[]{3},new[]{20});
            SetArmy(armies.GetArrayElementAtIndex(2),"Hueste-rival","rival",enemy,new[]{1,0},new[]{35,35});
            data.ApplyModifiedPropertiesWithoutUndo();scenario.CreateMission(map,graph,scenario.CreateArmyRegistry(graph));
            AssetDatabase.CreateAsset(scenario,Scenario);config.FindProperty("scenario").objectReferenceValue=scenario;config.ApplyModifiedPropertiesWithoutUndo();
            board.gameObject.AddComponent<CampaignCombatReviewCapture>();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Scene);AssetDatabase.SaveAssets();
        }
        private static void SetArmy(SerializedProperty army,string id,string faction,HexCoordinates position,int[] profiles,int[] quantities)
        {
            army.FindPropertyRelative("id").stringValue=id;army.FindPropertyRelative("factionId").stringValue=faction;
            army.FindPropertyRelative("position").vector2IntValue=new Vector2Int(position.Q,position.R);army.FindPropertyRelative("movementPerTurn").intValue=6;
            var formations=army.FindPropertyRelative("formations");formations.arraySize=profiles.Length;
            for(int i=0;i<profiles.Length;i++)
            {
                var formation=formations.GetArrayElementAtIndex(i);formation.FindPropertyRelative("id").stringValue=id+"-tropas-"+i;
                formation.FindPropertyRelative("profile").enumValueIndex=profiles[i];formation.FindPropertyRelative("quantity").intValue=quantities[i];
            }
        }
        [Serializable]private sealed class Summary{public string result;public int errors,warnings;}
    }
}
