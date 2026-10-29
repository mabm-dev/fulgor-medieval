using System;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    public static class CityFinishBuilder
    {
        const string Prefab="Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab";
        [MenuItem("Fulgor/Campaña/Preparar acabado de ciudad compacta")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader=Shader.Find("Fulgor/City/Surface");var ground=Shader.Find("Fulgor/City/Ground");
            if(shader==null||ground==null||ShaderUtil.ShaderHasError(shader)||ShaderUtil.ShaderHasError(ground))throw new InvalidOperationException("Error en los materiales de ciudad.");
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try {
                var finish=root.GetComponent<CityVisualFinish>();if(finish==null)finish=root.AddComponent<CityVisualFinish>();
                var data=new SerializedObject(finish);data.FindProperty("surfaceShader").objectReferenceValue=shader;data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        public static void Batch(){try{Prepare();CityConstructionReviewBuilder.Batch();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
