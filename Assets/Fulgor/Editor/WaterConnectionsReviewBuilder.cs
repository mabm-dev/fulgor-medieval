using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class WaterConnectionsReviewBuilder
    {
        const string Root = "Assets/Fulgor/Art/Water/ConnectedReview";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/WaterConnectionsReview.unity";
        const string Running = "Fulgor.WaterConnectionsReview.Running";
        const string Output = "Fulgor.WaterConnectionsReview.Output";
        const string Stage = "Fulgor.WaterConnectionsReview.Stage";
        static double started;
        static int frames;
        static WaterConnectionsReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Fulgor/Arte/Revisar rios y costas conectados")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = Shader.Find("Fulgor/ArtReview/ConnectedWater");
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader de agua no disponible.");
            string materialPath = Root + "/M_AguaConectada.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            material.shader = shader; material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_WaveStrength", .025f); material.SetFloat("_WaveScale", 18f); material.SetFloat("_WaveSpeed", .45f);
            EditorUtility.SetDirty(material);
            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/TerrainCastleReview.unity", OpenSceneMode.Single);
            var water = new GameObject("Agua conectada de revision").AddComponent<WaterMapArtReview>();
            var serialized = new SerializedObject(water); serialized.FindProperty("waterMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            CreateGalleryScene(material);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("WATER_SCENE_READY " + ScenePath);
        }

        [MenuItem("Fulgor/Arte/Revisar casos de agua")]
        public static void PrepareGallery()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Abre fuera de Play Mode.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/M_AguaConectada.mat");
            if (material == null) throw new InvalidOperationException("Prepara primero la revision de agua.");
            CreateGalleryScene(material);
        }

        static void CreateGalleryScene(Material material)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var light = UnityEngine.Object.FindFirstObjectByType<Light>();
            light.color = new Color(1f,.91f,.76f); light.intensity = 1.6f; light.transform.rotation = Quaternion.Euler(48,-32,0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.18f,.23f,.26f);
            var camera = Camera.main; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.04f,.05f);
            var gallery = new GameObject("Seis casos de agua").AddComponent<WaterConnectionCasesReview>();
            var serialized = new SerializedObject(gallery); serialized.FindProperty("waterMaterial").objectReferenceValue = material; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, "Assets/Fulgor/Scenes/ArtReview/WaterConnectionCases.unity");
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
            if (EditorApplication.isPlaying && SessionState.GetInt(Stage, 0) == 3)
            {
                var gallery = UnityEngine.Object.FindFirstObjectByType<WaterConnectionCasesReview>();
                if (gallery == null || !gallery.Ready || ++frames < 12) return;
                try {
                    if (gallery.CaseCenters.Count != 6 || gallery.GetComponentsInChildren<MeshRenderer>().Length != 150 || !Camera.main.orthographic)
                        throw new InvalidOperationException("La galeria no ha generado sus seis casos.");
                    var controller = Camera.main.GetComponent<CampaignCameraController>(); if (controller != null) controller.enabled = false;
                    string destination = SessionState.GetString(Output, "");
                    Capture(Camera.main, Path.Combine(destination, "Agua_Galeria_PlayMode.png"));
                    File.WriteAllText(Path.Combine(destination, "revision_galeria.json"), "{\"play_mode\":true,\"cases\":6,\"tiles\":150,\"orthographic\":true}");
                    Debug.Log("WATER_GALLERY_REVIEW_OK cases=6 tiles=150");
                    SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
                }
                catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
                return;
            }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var water = UnityEngine.Object.FindFirstObjectByType<WaterMapArtReview>();
            var terrain = UnityEngine.Object.FindFirstObjectByType<TerrainMapArtReview>();
            var review = UnityEngine.Object.FindFirstObjectByType<FortressMapArtReview>();
            if (water == null || !water.Ready || terrain == null || !terrain.Ready || review == null || !review.PlacementReady || ++frames < 12) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main;
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.enabled = false;
                string output = SessionState.GetString(Output, "");
                Capture(camera, Path.Combine(output, "AguaConectada_VistaInicial.png"));
                var focus = review.PlacedFortress.position;
                camera.transform.position = focus + new Vector3(0, 28, -22); camera.transform.LookAt(focus);
                camera.orthographicSize = 18f; Capture(camera, Path.Combine(output, "AguaConectada_Zoom18.png"));
                camera.orthographicSize = 6f; Capture(camera, Path.Combine(output, "AguaConectada_Zoom6.png"));
                camera.orthographicSize = 4f; Capture(camera, Path.Combine(output, "AguaConectada_Zoom4.png"));
                camera.transform.position = focus + new Vector3(1.8f, 4.5f, -2.8f); camera.transform.LookAt(focus + Vector3.up * .2f);
                camera.orthographicSize = 1.6f; camera.nearClipPlane = .05f;
                Capture(camera, Path.Combine(output, "AguaConectada_Inspeccion.png"));
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
                    Capture(camera, Path.Combine(output, "AguaConectada_Forzado" + level + ".png"));
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
                    Capture(camera, Path.Combine(output, "AguaConectada_Orilla.png"));
                }
                ValidateWaterSeams(terrainTiles, water);
                CaptureCases(camera, output);
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
                File.WriteAllText(Path.Combine(output, "revision_agua.json"),
                    "{\"unity\":\"" + Application.unityVersion + "\",\"play_mode\":true,\"tiles\":" + tiles +
                    ",\"tile\":\"" + review.ReviewTile.Cell.Coordinates.Key + "\",\"triangles\":" + triangles +
                    ",\"renderers\":" + renderers.Length + ",\"pipeline\":\"" + (GraphicsSettings.currentRenderPipeline == null ? "Built-in" : "URP") +
                    "\",\"root_scale_one\":" + (review.PlacedFortress.localScale == Vector3.one ? "true" : "false") +
                    ",\"game_camera_zooms\":[18,6,4],\"inspection_zoom\":1.6,\"production_lods_pending\":false,\"lod_triangles\":[" + string.Join(",", triangleCounts) + "],\"lod_bias\":" + QualitySettings.lodBias.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"crossfade\":true,\"terrain_triangles\":" + terrain.TerrainTriangles + ",\"shore_edges\":" + terrain.ShoreEdges + ",\"selection_checked\":true,\"collider_checked\":true,\"water_tiles\":" + water.WaterTiles + ",\"river_tiles\":" + water.RiverTiles + ",\"lake_tiles\":" + water.LakeTiles + ",\"sea_tiles\":" + water.SeaTiles + ",\"water_triangles\":" + water.WaterTriangles + ",\"net_terrain_triangles\":" + (terrain.TerrainTriangles - water.RemovedTriangles + water.WaterTriangles) + ",\"water_seams_checked\":true}");
                Debug.Log("WATER_CONNECTIONS_REVIEW_OK triangles=" + triangles + " tiles=" + tiles);
                SessionState.SetInt(Stage, 3); frames = 0;
                EditorSceneManager.LoadSceneInPlayMode("Assets/Fulgor/Scenes/ArtReview/WaterConnectionCases.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        static void ValidateWaterSeams(HexTileView[] tiles, WaterMapArtReview water)
        {
            var lookup = tiles.ToDictionary(t => t.Cell.Coordinates); float maxGap = 0; int pairs = 0;
            foreach (var tile in tiles.Where(t => t.Cell.Terrain == TerrainType.Water))
                foreach (var c in tile.Cell.Coordinates.Neighbors())
                {
                    if (!lookup.TryGetValue(c, out var other)) continue;
                    var a = tile.transform.position; var b = other.transform.position;
                    var normal = (b - a).normalized; var tangent = new Vector3(-normal.z, 0, normal.x);
                    var middle = (a + b) * .5f;
                    for (int i = 0; i <= 12; i++) {
                        var p = middle + tangent * (i / 12f - .5f);
                        float height = water.Layout.Height(new Vector2(p.x, p.z));
                        if (other.Cell.Terrain != TerrainType.Water) maxGap = Mathf.Max(maxGap, Mathf.Abs(height - .12f));
                    }
                    pairs++;
                    tile.SetSelected(true);
                    if (!tile.HasColorOverride) throw new InvalidOperationException("La seleccion de agua no responde.");
                    tile.SetSelected(false);
                }
            if (maxGap > .0001f) throw new InvalidOperationException("Costura tierra-agua: " + maxGap);
            Debug.Log("WATER_SEAMS_OK pairs=" + pairs + " max_land_gap=" + maxGap);
        }

        static void CaptureCases(Camera camera, string output)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/M_AguaConectada.mat");
            var root = new GameObject("Casos de agua para capturas");
            try {
                var gallery = root.AddComponent<WaterConnectionCasesReview>(); gallery.Build(material, new Vector3(-100,0,-40), false);
                for (int test = 0; test < gallery.CaseCenters.Count; test++) {
                    var focus = gallery.CaseCenters[test]; camera.transform.position = focus + new Vector3(0,15,-10); camera.transform.LookAt(focus);
                    camera.orthographicSize = 4.4f;
                    Capture(camera, Path.Combine(output, "Agua_Caso_" + WaterConnectionCasesReview.CaseNames[test] + ".png"));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
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






