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
    [InitializeOnLoad]
    public static class CastillaMapReviewBuilder
    {
        const string Root = "Assets/Fulgor/Art/Buildings/CastillaV11MapReview";
        const string PrefabPath = "Assets/Fulgor/Prefabs/ArtReview/PF_CastillaV11_MapReview.prefab";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/CastillaV11MapReview.unity";
        const string Running = "Fulgor.CastillaMapReview.Running";
        const string Output = "Fulgor.CastillaMapReview.Output";
        const string Stage = "Fulgor.CastillaMapReview.Stage";
        static double started;
        static int frames;

        static CastillaMapReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        static void Folder(string path)
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [MenuItem("Fulgor/Arte/Revisar Castilla V11 en el mapa")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepara la escena fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool urp = QualitySettings.renderPipeline != null || GraphicsSettings.defaultRenderPipeline != null;
            var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Shader de revision no disponible.");
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.maxTextureSize = path.Contains("Castilla_") ? 2048 : 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.sRGBTexture = path.Contains("BaseColor");
                importer.textureType = path.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            Folder("Assets/Fulgor/Prefabs/ArtReview");
            Folder("Assets/Fulgor/Scenes/ArtReview");
            var root = new GameObject("PF_CastillaV11_MapReview");
            try
            {
                foreach (string model in new[] { "SM_CastillaV11_Mapa", "SM_CastillaV11_Patio" })
                {
                    string path = Root + "/" + model + ".fbx";
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.globalScale = 1f;
                    importer.useFileScale = true;
                    importer.bakeAxisConversion = true;
                    importer.importAnimation = false;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.addCollider = false;
                    importer.isReadable = true;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importTangents = ModelImporterTangents.CalculateMikk;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.SaveAndReimport();
                    string family = model.EndsWith("Mapa") ? "Castilla" : "Entorno";
                    string prefix = "T_" + family + "_V11_";
                    string matPath = Root + "/M_" + family + "V11_MapReview.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
                    material.shader = shader;
                    material.SetColor(urp ? "_BaseColor" : "_Color", Color.white);
                    material.SetTexture(urp ? "_BaseMap" : "_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + prefix + "BaseColor.png"));
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + prefix + "Normal.png"));
                    material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/" + prefix + "MetallicSmoothness.png"));
                    material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.EnableKeyword("_METALLICGLOSSMAP");
                    material.SetFloat(urp ? "_Smoothness" : "_GlossMapScale", 1f);
                    material.enableInstancing = true;
                    EditorUtility.SetDirty(material);
                    var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                    instance.transform.SetParent(root.transform, false);
                    foreach (var r in instance.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = new[] { material };
                }
                float radius = 0;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                    foreach (var vertex in mf.sharedMesh.vertices)
                    {
                        var point = root.transform.InverseTransformPoint(mf.transform.TransformPoint(vertex));
                        radius = Mathf.Max(radius, new Vector2(point.x, point.z).magnitude);
                    }
                if (radius < .5f || radius > VisualScaleContract.PropSafeRadius + .001f)
                    throw new InvalidOperationException("La fortaleza excede el radio seguro: " + radius);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity", OpenSceneMode.Single);
            var review = new GameObject("Revision de arte - Castilla V11").AddComponent<FortressMapArtReview>();
            var serialized = new SerializedObject(review);
            serialized.FindProperty("fortressPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("CASTILLA_MAP_SCENE_READY " + ScenePath);
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
            { Debug.LogError("Tiempo agotado al revisar Castilla en el mapa."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
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
                camera.transform.position = focus + new Vector3(0, 28, -22); camera.transform.LookAt(focus);
                camera.orthographicSize = 6f; Capture(camera, Path.Combine(output, "CastillaV11_Mapa_Zoom6.png"));
                camera.orthographicSize = 4f; Capture(camera, Path.Combine(output, "CastillaV11_Mapa_Zoom4.png"));
                camera.transform.position = focus + new Vector3(1.8f, 2.3f, -2.8f); camera.transform.LookAt(focus + Vector3.up * .2f);
                camera.orthographicSize = 1.2f; camera.nearClipPlane = .05f;
                Capture(camera, Path.Combine(output, "CastillaV11_Mapa_Inspeccion.png"));
                var renderers = review.PlacedFortress.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Any(r => r.sharedMaterial == null || r.sharedMaterial.mainTexture == null || !r.sharedMaterial.shader.isSupported))
                    throw new InvalidOperationException("Material de fortaleza incompleto.");
                int triangles = review.PlacedFortress.GetComponentsInChildren<MeshFilter>().Sum(m => (int)m.sharedMesh.GetIndexCount(0) / 3);
                int tiles = UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Length;
                if (tiles != 384) throw new InvalidOperationException("El tablero generado no tiene 384 casillas.");
                File.WriteAllText(Path.Combine(output, "revision_mapa.json"),
                    "{\"unity\":\"" + Application.unityVersion + "\",\"play_mode\":true,\"tiles\":" + tiles +
                    ",\"tile\":\"" + review.ReviewTile.Cell.Coordinates.Key + "\",\"triangles\":" + triangles +
                    ",\"renderers\":" + renderers.Length + ",\"pipeline\":\"" + (GraphicsSettings.currentRenderPipeline == null ? "Built-in" : "URP") +
                    "\",\"root_scale_one\":" + (review.PlacedFortress.localScale == Vector3.one ? "true" : "false") +
                    ",\"game_camera_zooms\":[6,4],\"inspection_zoom\":1.2,\"production_lods_pending\":true}");
                Debug.Log("CASTILLA_MAP_REVIEW_OK triangles=" + triangles + " tiles=" + tiles);
                SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else { camera.targetTexture = target; camera.Render(); camera.targetTexture = null; }
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
