using System;
using System.IO;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>
    /// Prepara los árboles del mapa: importa el FBX y el atlas de Tools/Blender/arboles_fulgor.py, y crea el material (Fulgor/Terreno en modo
    /// árbol, recortado por alfa y a dos caras) y el prefab que lee <see cref="CampaignForest"/>.
    /// Uso: -executeMethod Fulgor.Editor.CampaignVegetationBuilder.Prepare
    /// </summary>
    public static class CampaignVegetationBuilder
    {
        const string Art = "Assets/Fulgor/Art/Vegetation";
        const string Folder = "Assets/Fulgor/Resources/Vegetacion";
        const string Model = Art + "/SM_Arboles.fbx", Texture = Art + "/T_Arboles_BaseColor.png";
        const string NoisePath = "Assets/Fulgor/Art/Terrain/Relieve/T_Terreno_Ruido.png";

        [MenuItem("Fulgor/Arte/Preparar árboles del mapa")]
        public static void Prepare()
        {
            try { Run(); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model = (ModelImporter)AssetImporter.GetAtPath(Model) ?? throw new FileNotFoundException("Falta el modelo de los árboles", Model);
            // las mallas se funden por casilla al jugar: tienen que poder leerse; las normales son las de la copa, no se recalculan
            model.isReadable = true; model.importNormals = ModelImporterNormals.Import; model.materialImportMode = ModelImporterMaterialImportMode.None;
            model.importAnimation = false; model.importCameras = false; model.importLights = false; model.meshCompression = ModelImporterMeshCompression.Off;
            model.SaveAndReimport();
            var texture = (TextureImporter)AssetImporter.GetAtPath(Texture) ?? throw new FileNotFoundException("Falta el atlas de los árboles", Texture);
            texture.sRGBTexture = true; texture.alphaSource = TextureImporterAlphaSource.FromInput; texture.alphaIsTransparency = true;
            texture.mipmapEnabled = true; texture.mipMapsPreserveCoverage = true; texture.alphaTestReferenceValue = .4f; // de lejos la copa no se vacía
            texture.maxTextureSize = 1024; texture.anisoLevel = 4; texture.wrapMode = TextureWrapMode.Clamp;
            texture.textureCompression = TextureImporterCompression.CompressedHQ; texture.SaveAndReimport();

            Directory.CreateDirectory(Folder);
            var shader = Shader.Find("Fulgor/Terreno") ?? throw new InvalidOperationException("Falta el sombreador Fulgor/Terreno.");
            CampaignTerrainReliefBuilder.AssertCompiles(shader);
            string materialPath = Folder + "/M_Arboles.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = shader;
            CampaignTerrainReliefBuilder.ResetTerrainDefaults(material, shader);
            material.shaderKeywords = Array.Empty<string>();
            material.SetFloat("_FulgorMontana", 0f); material.SetFloat("_FulgorEdificio", 0f); material.SetFloat("_FulgorArbol", 1f);
            material.EnableKeyword("_FULGOR_ARBOL");
            material.SetTexture("_FulgorNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Texture));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .05f);
            // recorte por alfa y dos caras: las tarjetas de hojas se ven por los dos lados
            material.SetFloat("_AlphaClip", 1f); material.SetFloat("_Cutoff", .4f); material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", 0f); material.doubleSidedGI = true;
            material.SetFloat("_AlphaToMask", 1f); // con el suavizado de bordes, el recorte de las hojas también sale suave
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var root = new GameObject("PF_Arboles");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                int count = 0;
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true)) { renderer.sharedMaterial = material; count++; }
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/PF_Arboles.prefab");
                Debug.Log($"Árboles preparados: {count} modelos");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
