using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>Prepara el Centro de aprovisionamiento (prefab y casilla de prueba) y gira la fortaleza de Castilla.</summary>
    public static class CampaignTileBuildingBuilder
    {
        const string Art = "Assets/Fulgor/Art/Buildings/CentroAprovisionamiento";
        const string Folder = "Assets/Fulgor/Resources/Edificios";
        public const string PrefabPath = Folder + "/PF_CentroAprovisionamiento.prefab";
        const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        const string FortressPrefab = "Assets/Fulgor/Prefabs/PF_FortalezaCastilla.prefab";

        // casilla de prueba: primer anillo de la fortaleza (13,8), en llanura; la puerta mira a 25 en un minutero
        static readonly Vector2Int Cell = new(12, 9);
        const float DoorMinute = 25f;

        [MenuItem("Fulgor/Arte/Preparar Centro de aprovisionamiento")]
        public static void BuildPrefab()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string texturePath = Art + "/T_CentroAprovisionamiento_BaseColor.png";
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
                {
                    importer.sRGBTexture = true; importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
                }
                string materialPath = Folder + "/M_CentroAprovisionamiento.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .12f);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/SM_CentroAprovisionamiento.fbx") ?? throw new FileNotFoundException("Falta el FBX del edificio.");
                var root = new GameObject("PF_CentroAprovisionamiento");
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    // el FBX sale con la puerta hacia +Z: se gira 180° para que sin giro mire al sur, como espera CampaignTileBuildings
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * instance.transform.localRotation;
                    var filter = instance.GetComponentsInChildren<MeshFilter>().Single();
                    filter.GetComponent<MeshRenderer>().sharedMaterial = material;
                    // la huella llega al 80 % del radio seguro de una casilla; la altura sale proporcional
                    float radius = filter.sharedMesh.vertices
                        .Select(v => root.transform.InverseTransformPoint(filter.transform.TransformPoint(v)))
                        .Max(p => new Vector2(p.x, p.z).magnitude);
                    instance.transform.localScale *= VisualScaleContract.PropSafeRadius * .8f / radius;
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Fulgor/Arte/Colocar Centro de aprovisionamiento en la escena de Conquista")]
        public static void PlaceInConquest()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ConquestScene);
                var board = UnityEngine.Object.FindFirstObjectByType<CampaignBoardPreview>() ?? throw new InvalidOperationException("La escena no tiene tablero.");
                var buildings = board.GetComponent<CampaignTileBuildings>() ?? board.gameObject.AddComponent<CampaignTileBuildings>();
                var serialized = new SerializedObject(buildings);
                var entries = serialized.FindProperty("entries");
                entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("label").stringValue = "Centro de aprovisionamiento";
                entry.FindPropertyRelative("cell").vector2IntValue = Cell;
                entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                entry.FindPropertyRelative("doorMinute").floatValue = DoorMinute;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Gira la fortaleza del juego alrededor de la vertical: negativo es contra las agujas del reloj (40 → 33 son −42°).</summary>
        public static void RotateFortress()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                int at = Array.IndexOf(args, "-fulgorGiro");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta -fulgorGiro <grados>.");
                float degrees = float.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture);
                var root = PrefabUtility.LoadPrefabContents(FortressPrefab);
                try
                {
                    var model = root.transform.GetChild(0);
                    model.localRotation = Quaternion.Euler(0f, degrees, 0f) * model.localRotation;
                    PrefabUtility.SaveAsPrefabAsset(root, FortressPrefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
