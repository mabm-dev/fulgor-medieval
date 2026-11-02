using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>Monta el grupo de soldados que sustituye a la bandera de las huestes en el mapa de campaña.</summary>
    public static class CampaignBattalionPrefabBuilder
    {
        const string Folder = "Assets/Fulgor/Resources/Hueste", Art = "Assets/Fulgor/Art/Units/SoldadosCastilla";
        public const string PrefabPath = Folder + "/PF_Batallon.prefab";
        // El FBX sale mirando a +Z, que es el rumbo cero de CampaignArmyMarker.
        // El grupo cabe en la peana de la hueste (radio .325) con un pequeño margen.
        const float FootprintRadius = .3f;

        [MenuItem("Fulgor/Arte/Montar batallón de hueste")]
        public static void Build()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string texturePath = Art + "/T_SoldadosCastilla_BaseColor.png";
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
                {
                    importer.sRGBTexture = true; importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
                }
                string materialPath = Folder + "/M_SoldadosCastilla.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .12f);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);

                string modelPath = Art + "/SM_SoldadosCastilla.fbx";
                if (AssetImporter.GetAtPath(modelPath) is ModelImporter modelImporter)
                {
                    modelImporter.importAnimation = true; modelImporter.animationType = ModelImporterAnimationType.Generic;
                    modelImporter.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) ?? throw new FileNotFoundException("Falta el FBX de los soldados.");
                var march = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"))
                    ?? throw new FileNotFoundException("El FBX de los soldados no trae la marcha.");
                var root = new GameObject("Batallon");
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    instance.name = "Soldados";
                    var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var skin in skins) skin.sharedMaterial = material;
                    // el radio se mide sobre la pose neutra de cada malla
                    float radius = skins
                        .SelectMany(skin => skin.sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(skin.transform.TransformPoint(v))))
                        .Max(p => new Vector2(p.x, p.z).magnitude);
                    instance.transform.localScale *= FootprintRadius / radius;
                    var walk = instance.AddComponent<Fulgor.Presentation.Map.CampaignSoldierWalk>(); // trae su Animator
                    walk.Clip = march;
                    var animator = walk.GetComponent<Animator>();
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                TerritoryMaterial();
                ContactMaterials();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Material translúcido del sombreado de territorio; vive en Resources para que el build conserve su variante transparente.</summary>
        static void TerritoryMaterial()
        {
            var material = UnlitAsset("M_Territorio");
            Transparent(material);
            material.SetColor("_BaseColor", new Color(1, 1, 1, .22f));
            EditorUtility.SetDirty(material);
        }

        /// <summary>Sombra de contacto bajo la hueste y material liso de la insignia del equipo.</summary>
        static void ContactMaterials()
        {
            string texturePath = Folder + "/T_Sombra.png";
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f).magnitude / (size / 2f);
                    float a = 1f - Mathf.SmoothStep(.15f, 1f, r);
                    pixels[y * size + x] = new Color(0, 0, 0, a);
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            var shadow = UnlitAsset("M_Sombra");
            Transparent(shadow);
            shadow.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            shadow.SetColor("_BaseColor", new Color(0, 0, 0, .55f));
            EditorUtility.SetDirty(shadow);
            var badge = UnlitAsset("M_Insignia");
            badge.SetFloat("_Cull", 0);
            badge.SetColor("_BaseColor", new Color(.08f, .38f, .88f));
            EditorUtility.SetDirty(badge);
        }

        static Material UnlitAsset(string name)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, path); }
            return material;
        }

        static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
