using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fulgor.Editor
{
    /// <summary>
    /// Prueba de la fortaleza de Castilla en 3D ligero (Tools/Blender/fortaleza_mapa_ligera.py) como visual del
    /// asentamiento en el mapa de campaña, con las mismas capturas que la prueba prerenderizada para compararlas.
    /// </summary>
    [InitializeOnLoad]
    public static class CastillaMapModelReviewBuilder
    {
        // Variante a revisar: "Mapa" (construida en Blender) o "Tripo" (reducida desde un modelo de IA); se elige
        // con -fulgorModelo <nombre> en la línea de comandos y corresponde a la carpeta FortalezaCastilla<nombre>.
        static string Variant
        {
            get
            {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-fulgorModelo");
                return at >= 0 && at + 1 < args.Length ? args[at + 1] : "Mapa";
            }
        }
        static float ArgFloat(string name, float porDefecto)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length && float.TryParse(args[at + 1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : porDefecto;
        }
        static string Root => "Assets/Fulgor/Art/Buildings/FortalezaCastilla" + Variant;
        static string Name => "Fortaleza_Castilla_" + Variant;
        /// <summary>Ruta de la textura (PNG o JPG), o null si no existe.</summary>
        static string TexturePath(string kind) =>
            new[] { ".png", ".jpg" }.Select(e => Root + "/T_" + Name + "_" + kind + e).FirstOrDefault(p => File.Exists(p));
        static string Fbx => Root + "/SM_" + Name + ".fbx";
        static string MaterialPath => Root + "/M_" + Name + ".mat";
        // Modo "prefab de juego": crea Assets/Fulgor/Prefabs/PF_FortalezaCastilla.prefab con los ajustes definitivos y lo asigna
        // al asentamiento de Castilla, sin tocar la escena de campaña ni crear la de revisión.
        static bool gamePrefab;
        const string GamePrefabPath = "Assets/Fulgor/Prefabs/PF_FortalezaCastilla.prefab";
        const string CastillaSettlementAsset = "Assets/Fulgor/Settlements/FortalezaCastilla.asset";
        static string PrefabPath => gamePrefab ? GamePrefabPath : "Assets/Fulgor/Prefabs/ArtReview/PF_" + Name + ".prefab";
        static string ScenePath => "Assets/Fulgor/Scenes/ArtReview/Castilla" + (Variant == "Mapa" ? "" : Variant) + "ModelMapReview.unity";
        const string Running = "Fulgor.CastillaModelReview.Running";
        const string Output = "Fulgor.CastillaModelReview.Output";
        const string Stage = "Fulgor.CastillaModelReview.Stage";
        static readonly Vector3 CameraOffset = new(0f, 28f, -22f);
        // Giro para que la puerta mire a la cámara fija del mapa (hacia −Z).
        const float Yaw = 0f;
        static double started;
        static int frames;

        static CastillaMapModelReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Fulgor/Arte/Revisar Castilla 3D ligera en el mapa")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepara la escena fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var (name, color) in new[] { ("BaseColor", true), ("Normal", false) })
            {
                var path = TexturePath(name);
                if (path == null)
                {
                    if (color) throw new InvalidOperationException("Falta la textura de color en " + Root);
                    continue;   // la Malla Smart de Tripo no trae mapa de normales
                }
                var texture = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new InvalidOperationException("Falta " + path);
                texture.textureType = color ? TextureImporterType.Default : TextureImporterType.NormalMap;
                texture.sRGBTexture = color; texture.mipmapEnabled = true; texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Trilinear; texture.anisoLevel = 8; texture.maxTextureSize = 4096;
                texture.textureCompression = TextureImporterCompression.CompressedHQ;
                texture.SaveAndReimport();
            }
            var model = (ModelImporter)AssetImporter.GetAtPath(Fbx) ?? throw new InvalidOperationException("Falta " + Fbx);
            model.globalScale = 1f; model.useFileScale = true; model.bakeAxisConversion = true;
            model.importAnimation = false; model.importCameras = false; model.importLights = false; model.addCollider = false;
            model.isReadable = true; model.meshCompression = ModelImporterMeshCompression.Off;
            model.importNormals = ModelImporterNormals.Import; model.importTangents = ModelImporterTangents.CalculateMikk;
            // se importan los materiales solo para leer sus nombres: Unity no conserva el orden de las ranuras de Blender
            model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            model.SaveAndReimport();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            // si existen las texturas pintadas en Painter (Tools/Painter/exportar_castilla_unity.py), sustituyen a las de Tripo
            string painterBase = PainterTexture("AlbedoTransparency");
            string painterNormal = PainterTexture("Normal");
            string painterMetal = PainterTexture("MetallicSmoothness");
            if (painterBase != null) ConfigurePainterTexture(painterBase, true, TextureImporterType.Default);
            if (painterNormal != null) ConfigurePainterTexture(painterNormal, false, TextureImporterType.NormalMap);
            if (painterMetal != null) ConfigurePainterTexture(painterMetal, false, TextureImporterType.Default);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(painterBase ?? TexturePath("BaseColor")));
            if (painterMetal != null)
            {
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(painterMetal));
                material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness", 1f);
            }
            string normalPath = painterNormal ?? TexturePath("Normal");
            material.SetTexture("_BumpMap", normalPath == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.SetFloat("_BumpScale", 1f);
            if (normalPath != null) material.EnableKeyword("_NORMALMAP"); else material.DisableKeyword("_NORMALMAP");
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .12f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            Folder("Assets/Fulgor/Prefabs/ArtReview");
            Folder("Assets/Fulgor/Scenes/ArtReview");
            var root = new GameObject("PF_" + Name);
            int triangles;
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
                instance.transform.SetParent(root.transform, false);
                // -fulgorGiro: giro en grados alrededor de la vertical (los 4X no ponen los edificios de frente a la cámara)
                instance.transform.localRotation = Quaternion.Euler(0f, ArgFloat("-fulgorGiro", gamePrefab ? 45f : Yaw), 0f) * instance.transform.localRotation;
                var filter = instance.GetComponentsInChildren<MeshFilter>().Single();
                var meshRenderer = filter.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                // segundo material (pendones de tela): textura propia y doble cara, para que se vean desde los dos lados
                // orden de Tools/Blender/castilla_retoques.py: 0 atlas de Tripo, 1 Pendon, 2 Tapiz (recorte alfa), 3 Almenas
                int slots = filter.sharedMesh.subMeshCount;
                if (slots > 1)
                {
                    var imported = meshRenderer.sharedMaterials.Select(m => m != null ? m.name : "").ToArray();
                    var extra = new System.Collections.Generic.Dictionary<string, Material>
                    {
                        ["Pendon"] = ExtraMaterial("Pendon", false, false), ["Tapiz"] = ExtraMaterial("Tapiz", true, false), ["Almenas"] = ExtraMaterial("Almenas", false, true),
                    };
                    meshRenderer.sharedMaterials = imported.Select(n => extra.TryGetValue(n, out var m) ? m : material).ToArray();
                }
                triangles = (int)(filter.sharedMesh.triangles.Length / 3);
                // escala: la huella llega justo al radio seguro de un asentamiento dentro de la casilla
                float radius = filter.sharedMesh.vertices
                    .Select(v => root.transform.InverseTransformPoint(filter.transform.TransformPoint(v)))
                    .Max(p => new Vector2(p.x, p.z).magnitude);
                // -fulgorRadio permite una huella mayor (un pedestal de roca, por ejemplo); el máximo de una casilla es su radio interior
                instance.transform.localScale *= ArgFloat("-fulgorRadio", gamePrefab ? .85f : VisualScaleContract.PropSafeRadius) / radius;
                // -fulgorAltura: factor de estirado vertical (las fortalezas de los 4X son más altas y esbeltas)
                var s = instance.transform.localScale; s.y *= ArgFloat("-fulgorAltura", 1f); instance.transform.localScale = s;
                // -fulgorHundir: fracción de la altura que se entierra en el terreno para fundir la base de roca con la casilla
                float hundir = ArgFloat("-fulgorHundir", gamePrefab ? .03f : 0f);
                if (hundir > 0f)
                    instance.transform.localPosition += Vector3.down * hundir * filter.GetComponent<MeshRenderer>().bounds.size.y;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            if (gamePrefab)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(CastillaSettlementAsset) ?? throw new InvalidOperationException("Falta " + CastillaSettlementAsset);
                var data = new SerializedObject(asset);
                data.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabPath);
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log("CASTILLA_GAME_PREFAB_READY " + GamePrefabPath + " triangles=" + triangles);
                return;
            }

            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity", OpenSceneMode.Single);
            var review = new GameObject("Revision de arte - Castilla 3D ligera").AddComponent<FortressMapArtReview>();
            var serialized = new SerializedObject(review);
            serialized.FindProperty("fortressPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var settlements = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<CampaignSettlementController>()
                ?? throw new InvalidOperationException("La escena de campaña no tiene asentamientos."));
            settlements.FindProperty("reuseArtReviewFortress").boolValue = true;
            settlements.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("CASTILLA_MODEL_SCENE_READY " + ScenePath + " triangles=" + triangles);
        }

        /// <summary>Para -batchmode -executeMethod: crea el prefab definitivo y lo asigna al asentamiento de Castilla.</summary>
        [MenuItem("Fulgor/Arte/Crear prefab de la fortaleza de Castilla y asignarlo")]
        public static void BatchGamePrefab()
        {
            try { gamePrefab = true; Prepare(); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
            finally { gamePrefab = false; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void Batch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-fulgorReviewOutput");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta el directorio de capturas.");
                Directory.CreateDirectory(args[at + 1]);
                Prepare();
                SessionState.SetString(Output, args[at + 1]); SessionState.SetInt(Stage, 0); SessionState.SetBool(Running, true);
                started = EditorApplication.timeSinceStartup; frames = 0;
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) { EditorApplication.update -= Tick; return; }
            if (SessionState.GetInt(Stage, 0) == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Running, false); EditorApplication.update -= Tick; EditorApplication.Exit(0); return;
            }
            if (started == 0) started = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - started > 120)
            { Debug.LogError("Tiempo agotado al revisar Castilla 3D ligera."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var review = UnityEngine.Object.FindFirstObjectByType<FortressMapArtReview>();
            if (review == null || !review.PlacementReady || ++frames < 12) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main;
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.enabled = false;
                string output = SessionState.GetString(Output, "");
                var focus = review.PlacedFortress.position;
                camera.transform.position = focus + CameraOffset; camera.transform.LookAt(focus);
                foreach (float zoom in new[] { 10f, 6f, 4f, 2f, 1f })
                {
                    camera.orthographicSize = zoom;
                    for (int i = 0; i < 2; i++) Capture(camera, Path.Combine(output, $"Castilla_{Variant}_Zoom{zoom:0}.png"));
                }
                Debug.Log("CASTILLA_MODEL_REVIEW_OK tile=" + review.ReviewTile.Cell.Coordinates.Key);
                SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        /// <summary>Textura exportada por Painter para el Texture Set principal (carpeta Painter), o null si no existe.</summary>
        static string PainterTexture(string kind)
        {
            string folder = Root + "/Painter";
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*_" + kind + ".png").Select(p => p.Replace('\\', '/')).FirstOrDefault()
                : null;
        }

        static void ConfigurePainterTexture(string path, bool srgb, TextureImporterType type)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = type; ti.sRGBTexture = srgb; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 8; ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.SaveAndReimport();
        }

        /// <summary>Material URP Lit para las piezas añadidas en Blender: tela (doble cara), tapiz (doble cara y recorte alfa) o sillares.</summary>
        static Material ExtraMaterial(string kind, bool alphaClip, bool repeat)
        {
            string texPath = TexturePath(kind) ?? throw new InvalidOperationException("Falta la textura " + kind + " en " + Root);
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = true;
            ti.alphaSource = alphaClip ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = alphaClip;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp; ti.SaveAndReimport();
            string matPath = Root + "/M_" + Name + "_" + kind + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, matPath); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Cull", repeat ? 2 : 0); m.SetFloat("_Smoothness", .08f); m.SetFloat("_Metallic", 0);
            m.SetFloat("_AlphaClip", alphaClip ? 1 : 0); m.SetFloat("_Cutoff", .5f);
            if (alphaClip) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Folder(string path)
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
