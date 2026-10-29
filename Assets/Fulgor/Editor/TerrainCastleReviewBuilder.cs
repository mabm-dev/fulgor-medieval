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
    public static class TerrainCastleReviewBuilder
    {
        const string Root = "Assets/Fulgor/Art/Terrain/CastillaMapReview";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/TerrainCastleReview.unity";
        const string Running = "Fulgor.TerrainCastleReview.Running";
        const string Output = "Fulgor.TerrainCastleReview.Output";
        const string Stage = "Fulgor.TerrainCastleReview.Stage";
        static double started;
        static int frames;
        static TerrainCastleReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Fulgor/Arte/Revisar terreno y fortaleza")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("URP Lit no disponible.");
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.maxTextureSize = path.Contains("Montana") ? 2048 : 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.sRGBTexture = path.Contains("BaseColor");
                importer.textureType = path.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            }
            string[] names = { "Mar", "Llanura", "Bosque", "Colina", "Montana", "LlanuraFortaleza" };
            var meshes = new Mesh[6]; var materials = new Material[6];
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i]; string path = Root + "/SM_Terreno_" + name + "_Revision.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
                importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
                importer.addCollider = false; importer.isReadable = true;
                importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var filter = model.GetComponentsInChildren<MeshFilter>().Single();
                var matrix = filter.transform.localToWorldMatrix;
                Debug.Log("TERRAIN_TRANSFORM " + name + " raw=" + filter.sharedMesh.bounds + " matrix=" + matrix);
                var baked = UnityEngine.Object.Instantiate(filter.sharedMesh);
                baked.name = "SM_Terreno_" + name + "_Unity";
                baked.vertices = baked.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
                baked.normals = baked.normals.Select(v => matrix.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                baked.tangents = baked.tangents.Select(v => {
                    var direction = matrix.MultiplyVector(new Vector3(v.x, v.y, v.z)).normalized;
                    return new Vector4(direction.x, direction.y, direction.z, v.w * (matrix.determinant < 0 ? -1 : 1));
                }).ToArray();
                if (matrix.determinant < 0) {
                    for (int sub = 0; sub < baked.subMeshCount; sub++) {
                        var indices = baked.GetTriangles(sub);
                        for (int at = 0; at < indices.Length; at += 3) { int swap = indices[at]; indices[at] = indices[at + 2]; indices[at + 2] = swap; }
                        baked.SetTriangles(indices, sub);
                    }
                }
                baked.RecalculateBounds();
                string meshPath = Root + "/SM_Terreno_" + name + "_Unity.asset";
                var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (savedMesh == null) { AssetDatabase.CreateAsset(baked, meshPath); savedMesh = baked; }
                else { EditorUtility.CopySerialized(baked, savedMesh); UnityEngine.Object.DestroyImmediate(baked); EditorUtility.SetDirty(savedMesh); }
                meshes[i] = savedMesh;
                string textureName = i == 5 ? "Llanura" : name;
                var matPath = Root + "/M_Terreno_" + textureName + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
                material.shader = shader; material.SetColor("_BaseColor", Color.white);
                foreach (var pair in new[] { new[] { "_BaseMap", "BaseColor" }, new[] { "_BumpMap", "Normal" }, new[] { "_MetallicGlossMap", "MetallicSmoothness" } })
                    material.SetTexture(pair[0], AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/T_Terreno_" + textureName + "_" + pair[1] + ".png"));
                material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Smoothness", 1); material.enableInstancing = true; EditorUtility.SetDirty(material); materials[i] = material;
                int edgeVertices = 0;
                foreach (var v in meshes[i].vertices)
                {
                    float edge = Mathf.Max(Mathf.Abs(v.x) / (Mathf.Sqrt(3f) / 2f), (Mathf.Abs(v.x) + Mathf.Sqrt(3f) * Mathf.Abs(v.z)) / Mathf.Sqrt(3f));
                    if (edge > .9999f) edgeVertices++;
                    if (edge > 1.001f) throw new InvalidOperationException("Radio incorrecto: " + name);
                    if (edge > .9999f && Mathf.Abs(v.y - (i == 0 ? .04f : .12f)) > .001f)
                        throw new InvalidOperationException("Altura de borde incorrecta: " + name);
                }
                if (edgeVertices < 6) throw new InvalidOperationException("No se ha aplicado la escala de la malla: " + name);
            }
            string bankPath = Root + "/M_Orilla.mat";
            var bank = AssetDatabase.LoadAssetAtPath<Material>(bankPath);
            if (bank == null) { bank = new Material(shader); AssetDatabase.CreateAsset(bank, bankPath); }
            bank.SetColor("_BaseColor", new Color(.46f, .38f, .24f)); bank.SetFloat("_Smoothness", .1f); EditorUtility.SetDirty(bank);
            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity", OpenSceneMode.Single);
            var fortress = new GameObject("Fortaleza de revision").AddComponent<FortressMapArtReview>();
            var castleSerialized = new SerializedObject(fortress);
            castleSerialized.FindProperty("fortressPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fulgor/Prefabs/ArtReview/PF_CastillaV11_LOD.prefab");
            castleSerialized.ApplyModifiedPropertiesWithoutUndo();
            var terrain = new GameObject("Terrenos de revision").AddComponent<TerrainMapArtReview>();
            var serialized = new SerializedObject(terrain);
            var meshArray = serialized.FindProperty("terrainMeshes"); meshArray.arraySize = 6;
            var matArray = serialized.FindProperty("terrainMaterials"); matArray.arraySize = 5;
            for (int i = 0; i < 5; i++) { meshArray.GetArrayElementAtIndex(i).objectReferenceValue = meshes[i]; matArray.GetArrayElementAtIndex(i).objectReferenceValue = materials[i]; }
            meshArray.GetArrayElementAtIndex(5).objectReferenceValue = meshes[5];
            serialized.FindProperty("shoreMaterial").objectReferenceValue = bank; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("TERRAIN_CASTLE_SCENE_READY " + ScenePath);
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
            var terrain = UnityEngine.Object.FindFirstObjectByType<TerrainMapArtReview>();
            var review = UnityEngine.Object.FindFirstObjectByType<FortressMapArtReview>();
            if (terrain == null || !terrain.Ready || review == null || !review.PlacementReady || ++frames < 12) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main;
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.enabled = false;
                string output = SessionState.GetString(Output, "");
                var focus = review.PlacedFortress.position;
                camera.transform.position = focus + new Vector3(0, 28, -22); camera.transform.LookAt(focus);
                camera.orthographicSize = 18f; Capture(camera, Path.Combine(output, "TerrenoCastilla_Zoom18.png"));
                camera.orthographicSize = 6f; Capture(camera, Path.Combine(output, "TerrenoCastilla_Zoom6.png"));
                camera.orthographicSize = 4f; Capture(camera, Path.Combine(output, "TerrenoCastilla_Zoom4.png"));
                camera.transform.position = focus + new Vector3(1.8f, 4.5f, -2.8f); camera.transform.LookAt(focus + Vector3.up * .2f);
                camera.orthographicSize = 1.6f; camera.nearClipPlane = .05f;
                Capture(camera, Path.Combine(output, "TerrenoCastilla_Inspeccion.png"));
                var renderers = review.PlacedFortress.GetComponentsInChildren<MeshRenderer>();
                if (renderers.SelectMany(r => r.sharedMaterials).Any(m => m == null || m.mainTexture == null || !m.shader.isSupported))
                    throw new InvalidOperationException("Material de fortaleza incompleto.");
                var group = review.PlacedFortress.GetComponent<LODGroup>();
                int[] triangleCounts = group.GetLODs().Select(lod => lod.renderers.Sum(r => {
                    var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                    return Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3);
                })).ToArray();
                if (triangleCounts[0] > 15000 || triangleCounts[1] > 6000)
                    throw new InvalidOperationException("Presupuesto LOD excedido.");
                for (int level = 0; level < group.GetLODs().Length; level++) {
                    group.ForceLOD(level);
                    Capture(camera, Path.Combine(output, "TerrenoCastilla_Forzado" + level + ".png"));
                }
                group.ForceLOD(-1);
                var terrainTiles = UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
                foreach (var family in terrainTiles.GroupBy(t => t.Cell.Terrain)) {
                    var sample = family.First();
                    var surface = sample.GetComponent<MeshCollider>();
                    if (!surface.Raycast(new Ray(sample.transform.position + Vector3.up * 3f, Vector3.down), out _, 6f))
                        throw new InvalidOperationException("No se puede seleccionar el terreno: " + family.Key);
                }
                var coast = terrainTiles.Where(t => t.Cell.Terrain != Fulgor.Core.Map.TerrainType.Water)
                    .OrderBy(t => (t.transform.position - focus).sqrMagnitude)
                    .FirstOrDefault(t => t.Cell.Coordinates.Neighbors().Any(c => terrainTiles.Any(n => n.Cell.Coordinates.Equals(c) && n.Cell.Terrain == Fulgor.Core.Map.TerrainType.Water)));
                if (coast != null) {
                    var coastFocus = coast.transform.position;
                    camera.transform.position = coastFocus + new Vector3(0, 28, -22); camera.transform.LookAt(coastFocus);
                    camera.orthographicSize = 2.5f;
                    Capture(camera, Path.Combine(output, "TerrenoCastilla_Orilla.png"));
                }
                int triangles = triangleCounts[0];
                var tile = review.ReviewTile;
                tile.SetSelected(true);
                if (!tile.HasColorOverride) throw new InvalidOperationException("Seleccion de casilla no responde.");
                tile.SetSelected(false);
                if (tile.HasColorOverride) throw new InvalidOperationException("La seleccion no restaura el material.");
                var collider = tile.GetComponent<MeshCollider>();
                if (collider.sharedMesh != tile.GetComponent<MeshFilter>().sharedMesh) throw new InvalidOperationException("Collider distinto del terreno visible.");
                Debug.Log("TERRAIN_COUNTS triangles=" + terrain.TerrainTriangles + " shore_edges=" + terrain.ShoreEdges);
                int tiles = UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None).Length;
                if (tiles != 384) throw new InvalidOperationException("El tablero generado no tiene 384 casillas.");
                File.WriteAllText(Path.Combine(output, "revision_terreno.json"),
                    "{\"unity\":\"" + Application.unityVersion + "\",\"play_mode\":true,\"tiles\":" + tiles +
                    ",\"tile\":\"" + review.ReviewTile.Cell.Coordinates.Key + "\",\"triangles\":" + triangles +
                    ",\"renderers\":" + renderers.Length + ",\"pipeline\":\"" + (GraphicsSettings.currentRenderPipeline == null ? "Built-in" : "URP") +
                    "\",\"root_scale_one\":" + (review.PlacedFortress.localScale == Vector3.one ? "true" : "false") +
                    ",\"game_camera_zooms\":[18,6,4],\"inspection_zoom\":1.6,\"production_lods_pending\":false,\"lod_triangles\":[" + string.Join(",", triangleCounts) + "],\"lod_bias\":" + QualitySettings.lodBias.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"crossfade\":true,\"terrain_triangles\":" + terrain.TerrainTriangles + ",\"shore_edges\":" + terrain.ShoreEdges + ",\"selection_checked\":true,\"collider_checked\":true}");
                Debug.Log("TERRAIN_CASTLE_REVIEW_OK triangles=" + triangles + " tiles=" + tiles);
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





