using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>Monta el grupo de batallón que sustituye a la bandera de las huestes en el mapa de campaña.</summary>
    public static class CampaignBattalionPrefabBuilder
    {
        const string Folder = "Assets/Fulgor/Resources/Hueste", Troops = "Assets/Fulgor/Art/RepertorioV02/Tropas";
        public const string PrefabPath = Folder + "/PF_Batallon.prefab";
        // Fila de frente, centro y retaguardia; separación pensada para caber en la peana de la hueste.
        static readonly string[] Rows = { "Lancero", "Espadachin", "Ballestero" };
        const float Spacing = .17f;

        [MenuItem("Fulgor/Arte/Montar batallón de hueste")]
        public static void Build()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var root = new GameObject("Batallon");
                for (int row = 0; row < Rows.Length; row++)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Troops}/SM_{Rows[row]}Natural_A_LOD0.fbx");
                    if (model == null) throw new FileNotFoundException("Falta el modelo de " + Rows[row]);
                    var material = TroopMaterial(Rows[row]);
                    for (int column = -1; column <= 1; column++)
                    {
                        var soldier = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                        soldier.name = $"{Rows[row]}_{column + 2}";
                        soldier.transform.localPosition = new Vector3(column * Spacing, 0, (1 - row) * Spacing);
                        foreach (var renderer in soldier.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                TerritoryMaterial();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Material translúcido del sombreado de territorio; vive en Resources para que el build conserve su variante transparente.</summary>
        static void TerritoryMaterial()
        {
            string path = Folder + "/M_Territorio.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, path); }
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", new Color(1, 1, 1, .22f));
            EditorUtility.SetDirty(material);
        }

        static Material TroopMaterial(string name)
        {
            string path = $"{Folder}/M_Tropa_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", Texture($"T_{name}Natural_A_BaseColor.png", false));
            material.SetTexture("_BumpMap", Texture($"T_{name}Natural_A_Normal.png", true));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D Texture(string file, bool normal)
        {
            string path = $"{Troops}/Textures/{file}";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var wanted = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != wanted) { importer.textureType = wanted; importer.SaveAndReimport(); }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
