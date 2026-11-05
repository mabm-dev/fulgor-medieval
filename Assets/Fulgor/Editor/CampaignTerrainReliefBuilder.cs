using System;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>
    /// Prepara el relieve continuo de montaña y el sombreador del terreno: comprueba que Fulgor/Terreno compila, crea el material de la montaña
    /// generada y los asigna en el aspecto del mapa de Castilla. Uso: -executeMethod Fulgor.Editor.CampaignTerrainReliefBuilder.Prepare
    /// </summary>
    public static class CampaignTerrainReliefBuilder
    {
        const string Folder = "Assets/Fulgor/Art/Terrain/Relieve";
        const string MaterialPath = Folder + "/M_Terreno_Montana_Relieve.mat";
        const string SettingsPath = "Assets/Fulgor/MapAppearance/CastillaMapAppearance.asset";
        const string NoisePath = Folder + "/T_Terreno_Ruido.png";

        /// <summary>
        /// Ruido del terreno de 256×256 que se repite sin costuras: cada canal es un fbm de cuatro octavas (8, 16, 32 y 64 celdas) con su propia
        /// semilla. El sombreador lo lee en vez de calcular el ruido por píxel (medido: el cálculo costaba unos 0,65 ms por fotograma a 2560×1440).
        /// </summary>
        static Texture2D BuildNoise(string path)
        {
            const int size = 256, baseCells = 8;
            int[] seeds = { 11, 23, 37, 53 };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                byte Channel(int c) => (byte)Mathf.Clamp(Mathf.RoundToInt(PeriodicFbm(x / (float)size, y / (float)size, baseCells, seeds[c]) * 255f), 0, 255);
                pixels[y * size + x] = new Color32(Channel(0), Channel(1), Channel(2), Channel(3));
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels); texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false; importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float PeriodicFbm(float u, float v, int cells, int seed)
        {
            float sum = 0f, amplitude = .5f, total = 0f;
            for (int octave = 0; octave < 4; octave++)
            {
                sum += amplitude * PeriodicNoise(u * cells, v * cells, cells, seed + octave * 101);
                total += amplitude; cells *= 2; amplitude *= .5f;
            }
            return sum / total;
        }

        static float PeriodicNoise(float x, float y, int period, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy, ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            float H(int a, int b) => Hash(((a % period) + period) % period, ((b % period) + period) % period, seed);
            return Mathf.Lerp(Mathf.Lerp(H(ix, iy), H(ix + 1, iy), ux), Mathf.Lerp(H(ix, iy + 1), H(ix + 1, iy + 1), ux), uy);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xffffff) / 16777215f;
            }
        }

        /// <summary>Falla si Fulgor/Terreno no compila, con los mensajes del compilador.</summary>
        internal static void AssertCompiles(Shader shader)
        {
            if (ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Fulgor/Terreno tiene errores: " + string.Join(" | ", ShaderUtil.GetShaderMessages(shader).Select(m => $"{m.message} ({m.file}:{m.line})")));
        }

        /// <summary>
        /// Devuelve las propiedades _Fulgor* de un material de Fulgor/Terreno a los valores del sombreador (colores, escalas, suelo pisado), menos
        /// el ruido y los interruptores de modo (montaña, edificio), que pone quien prepara el material.
        /// </summary>
        internal static void ResetTerrainDefaults(Material material, Shader shader)
        {
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                string name = shader.GetPropertyName(i);
                if (!name.StartsWith("_Fulgor", StringComparison.Ordinal) || name == "_FulgorMontana" || name == "_FulgorEdificio") continue;
                var type = shader.GetPropertyType(i);
                if (type == UnityEngine.Rendering.ShaderPropertyType.Color || type == UnityEngine.Rendering.ShaderPropertyType.Vector)
                    material.SetVector(name, shader.GetPropertyDefaultVectorValue(i));
                else if (type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range)
                    material.SetFloat(name, shader.GetPropertyDefaultFloatValue(i));
            }
        }

        [MenuItem("Fulgor/Campaña/Preparar relieve de montaña y sombreador del terreno")]
        public static void Prepare()
        {
            try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = Shader.Find("Fulgor/Terreno") ?? throw new InvalidOperationException("No se encuentra el sombreador Fulgor/Terreno.");
            AssertCompiles(shader);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Fulgor/Art/Terrain", "Relieve");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = shader;
            // los colores y escalas del terreno vuelven a los del sombreador, los mismos que usan llanura, bosque y colina
            ResetTerrainDefaults(material, shader);
            material.SetFloat("_FulgorMontana", 1f); material.EnableKeyword("_FULGOR_MONTANA"); material.DisableKeyword("_FULGOR_EDIFICIO");
            var noise = BuildNoise(NoisePath);
            material.SetTexture("_FulgorNoise", noise);
            material.SetFloat("_Smoothness", .12f); material.SetColor("_BaseColor", Color.white);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            var settings = AssetDatabase.LoadAssetAtPath<CampaignMapAppearanceSettings>(SettingsPath)
                ?? throw new InvalidOperationException("Falta " + SettingsPath);
            var data = new SerializedObject(settings);
            data.FindProperty("terrainShader").objectReferenceValue = shader;
            data.FindProperty("reliefMountainMaterial").objectReferenceValue = material;
            data.FindProperty("reliefSubdivisions").intValue = 32;
            data.FindProperty("terrainNoise").objectReferenceValue = noise;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("RELIEVE_PREPARADO: " + MaterialPath);
        }
    }
}
