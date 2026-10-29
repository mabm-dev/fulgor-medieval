using System;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    public static class CampaignFogBuilder
    {
        public static void PrepareForTests()
        {try{Prepare();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void PrepareAndBuild()
        {try{Prepare();CampaignLogisticsBuilder.PrepareAndBuild();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        private static void Prepare()
        {
            var scenario=AssetDatabase.LoadAssetAtPath<CampaignScenarioDefinition>(CampaignLogisticsBuilder.ScenarioPath);
            if(scenario==null)throw new InvalidOperationException("Prepare the playable conquest first.");
            var data=new SerializedObject(scenario);data.FindProperty("enableFogOfWar").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssets();
        }
    }
}
