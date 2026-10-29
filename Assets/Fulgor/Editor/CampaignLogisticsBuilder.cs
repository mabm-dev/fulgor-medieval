using System;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CampaignLogisticsBuilder
    {
        public const string ScenarioPath="Assets/Fulgor/Scenarios/PlayableConquest.asset";
        public static void PrepareForTests()
        {try{Prepare();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void PrepareAndBuild()
        {try{Prepare();CampaignLaunchBuilder.PrepareAndBuild();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        private static void Prepare()
        {
            EditorSceneManager.OpenScene(CampaignLaunchBuilder.ConquestScene);
            var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>(CampaignLaunchBuilder.CatalogPath);var entry=catalog.Find("castilla-conquest");
            var scenario=AssetDatabase.LoadAssetAtPath<CampaignScenarioDefinition>(ScenarioPath);
            if(scenario==null){scenario=Object.Instantiate(entry.Scenario);scenario.name="PlayableConquest";AssetDatabase.CreateAsset(scenario,ScenarioPath);}
            var config=new SerializedObject(scenario);config.FindProperty("enableMilitaryLogistics").boolValue=true;config.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(scenario);
            var board=Object.FindAnyObjectByType<CampaignBoardPreview>();var data=new SerializedObject(board);data.FindProperty("scenario").objectReferenceValue=scenario;data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),CampaignLaunchBuilder.ConquestScene);
            var replacement=new CampaignLaunchEntry(entry.Id,entry.Title,entry.Description,entry.SceneName,scenario,entry.Settlements.ToArray());
            catalog.Configure(catalog.Entries.Select(e=>e.Id==entry.Id?replacement:e).ToArray());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
    }
}
