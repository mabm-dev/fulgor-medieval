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

        /// <summary>
        /// Mide lo que cuesta el relieve de las colinas y montañas del tablero de Conquista: casilla a casilla (tres BuildTile por casilla, como
        /// antes) y todas a la vez (BuildTiles), y comprueba que las dos formas dan las mismas mallas. Tres vueltas, para ver el arranque en frío.
        /// Uso: -executeMethod Fulgor.Editor.CampaignTerrainReliefBuilder.Measure
        /// </summary>
        public static void Measure()
        {
            try
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorConquest.unity");
                var board = UnityEngine.Object.FindFirstObjectByType<CampaignBoardPreview>() ?? throw new InvalidOperationException("La escena no tiene tablero.");
                var scenario = new SerializedObject(board).FindProperty("scenario").objectReferenceValue as CampaignScenarioDefinition
                    ?? throw new InvalidOperationException("El tablero no tiene escenario.");
                var map = Fulgor.Core.Map.ProceduralMapGenerator.Generate(scenario.Width, scenario.Height, scenario.Seed);
                var mountains = map.Cells.Where(c => c.Terrain == Fulgor.Core.Map.TerrainType.Mountain).Select(c => c.Coordinates).ToArray();
                var hills = map.Cells.Where(c => c.Terrain == Fulgor.Core.Map.TerrainType.Hill).Select(c => c.Coordinates).ToArray();
                var highland = mountains.Concat(hills).ToArray();
                const int detail = 32, seed = 1086;
                var report = new System.Text.StringBuilder($"Relieve de Conquista: {mountains.Length} montañas y {hills.Length} colinas, detalle {detail}\n");
                for (int run = 1; run <= 3; run++)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var single = new CampaignMountainRelief(mountains, seed, VisualScaleContract.HexOuterRadius, hills);
                    var oneByOne = highland.ToDictionary(c => c, c => new CampaignMountainRelief.TileLevels(single.BuildTile(c, detail, "a"),
                        single.BuildTile(c, CampaignMountainRelief.MediumSubdivisions(detail), "b"), single.BuildTile(c, CampaignMountainRelief.DistantSubdivisions(detail), "c")));
                    double before = watch.Elapsed.TotalMilliseconds; watch.Restart();
                    var together = new CampaignMountainRelief(mountains, seed, VisualScaleContract.HexOuterRadius, hills).BuildTiles(highland, detail);
                    double after = watch.Elapsed.TotalMilliseconds;
                    // las dos formas, vértice a vértice
                    float position = 0f, normal = 0f, color = 0f; int different = 0;
                    foreach (var cell in highland)
                    {
                        var x = oneByOne[cell]; var y = together[cell];
                        foreach (var (p, q) in new[] { (x.Full, y.Full), (x.Medium, y.Medium), (x.Distant, y.Distant) })
                        {
                            if (p.vertexCount != q.vertexCount || !p.triangles.SequenceEqual(q.triangles))
                            {
                                // vértices en el mismo sitio dentro de una misma malla (la clave redondeada de BuildTile a veces no los junta)
                                static int Repeated(Mesh m) => m.vertices.Length - m.vertices.Select(v => (Mathf.RoundToInt(v.x * 1e4f), Mathf.RoundToInt(v.y * 1e4f), Mathf.RoundToInt(v.z * 1e4f))).Distinct().Count();
                                if (different++ < 4)
                                    report.AppendLine($"  {cell.Key} {p.name}: casilla a casilla {p.vertexCount} vértices ({Repeated(p)} repetidos), todas a la vez {q.vertexCount} ({Repeated(q)} repetidos)");
                                continue;
                            }
                            var pv = p.vertices; var qv = q.vertices; var pn = p.normals; var qn = q.normals; var pc = p.colors; var qc = q.colors;
                            for (int i = 0; i < pv.Length; i++)
                            {
                                position = Mathf.Max(position, Vector3.Distance(pv[i], qv[i])); normal = Mathf.Max(normal, Vector3.Distance(pn[i], qn[i]));
                                color = Mathf.Max(color, Mathf.Max(Mathf.Abs(pc[i].r - qc[i].r), Mathf.Abs(pc[i].g - qc[i].g)));
                            }
                        }
                        foreach (var m in new[] { x.Full, x.Medium, x.Distant, y.Full, y.Medium, y.Distant }) UnityEngine.Object.DestroyImmediate(m);
                    }
                    report.AppendLine($"vuelta {run}: casilla a casilla {before:0} ms, todas a la vez {after:0} ms; mallas distintas {different}, " +
                                      $"diferencia máxima: posición {position:0.000000}, normal {normal:0.000000}, color {color:0.000000}");
                }
                Debug.Log("RELIEVE_MEDIDA\n" + report);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
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
