using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fulgor.Editor
{
    /// <summary>
    /// Capturas de revisión del terreno en la escena real de Conquista, sin niebla de guerra: la sierra más grande (montañas juntas),
    /// un pico suelto, la fortaleza con su anillo y una llanura, siempre con la misma cámara para comparar antes y después.
    /// Uso: -executeMethod Fulgor.Editor.CampaignTerrainCapture.Batch -fulgorReviewOutput &lt;carpeta&gt;
    /// </summary>
    [InitializeOnLoad]
    public static class CampaignTerrainCapture
    {
        const string Running = "Fulgor.TerrainCapture.Running";
        const string Output = "Fulgor.TerrainCapture.Output";
        const string Stage = "Fulgor.TerrainCapture.Stage";
        const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        const int Width = 1600, Height = 1000;
        static double started;
        static int frames;

        static CampaignTerrainCapture()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        public static void Batch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-fulgorReviewOutput");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta el directorio de capturas.");
                Directory.CreateDirectory(args[at + 1]);
                EditorSceneManager.OpenScene(ConquestScene, OpenSceneMode.Single);
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
            if (EditorApplication.timeSinceStartup - started > 300)
            { Debug.LogError("Tiempo agotado al capturar el terreno."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var appearance = UnityEngine.Object.FindFirstObjectByType<CampaignMapAppearance>();
            var campaign = UnityEngine.Object.FindFirstObjectByType<CampaignTurnController>();
            if (appearance == null || !appearance.Ready || campaign == null) return;
            // la niebla se apaga para revisar todo el relieve; se espera unos fotogramas a que se retire la cubierta
            var fog = typeof(CampaignTurnController).GetProperty("FogEnabled");
            if (campaign.FogEnabled) fog.GetSetMethod(true).Invoke(campaign, new object[] { false });
            if (++frames < 120) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main ?? throw new InvalidOperationException("La escena no tiene cámara principal.");
                var navigation = camera.GetComponent<CampaignCameraController>();
                if (navigation != null) navigation.enabled = false;
                string output = SessionState.GetString(Output, "");
                var tiles = UnityEngine.Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                    .GroupBy(t => t.Cell.Coordinates).ToDictionary(g => g.Key, g => g.First());
                var mountains = tiles.Values.Where(t => t.Cell.Terrain == TerrainType.Mountain).Select(t => t.Cell.Coordinates).ToHashSet();
                var clusters = Clusters(mountains);
                var report = new System.Text.StringBuilder();
                report.AppendLine($"Montañas: {mountains.Count}; grupos: {clusters.Count}; tamaños: {string.Join(", ", clusters.Select(c => c.Count))}");
                report.AppendLine($"Triángulos de terreno: {tiles.Values.Sum(t => t.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3)}");

                var views = new List<(string name, Vector3 focus, float distance, float elevation)>();
                var biggest = clusters.OrderByDescending(c => c.Count).FirstOrDefault();
                if (biggest != null)
                {
                    var centre = Centre(biggest.Select(c => tiles[c].transform.position));
                    float spread = biggest.Max(c => Vector3.Distance(tiles[c].transform.position, centre));
                    views.Add(("sierra", centre, 4.5f + spread * 2.2f, 48f));
                    views.Add(("sierra_cerca", tiles[biggest.OrderBy(c => Vector3.Distance(tiles[c].transform.position, centre)).First()].transform.position, 3.2f, 42f));
                    report.AppendLine("Sierra: " + string.Join(" ", biggest.Select(c => $"({c.Q},{c.R})")));
                }
                var lonely = clusters.Where(c => c.Count == 1).Select(c => c.First()).OrderBy(c => c.Q).ThenBy(c => c.R).FirstOrDefault();
                if (clusters.Any(c => c.Count == 1))
                {
                    views.Add(("pico_suelto", tiles[lonely].transform.position, 3.4f, 42f));
                    report.AppendLine($"Pico suelto: ({lonely.Q},{lonely.R})");
                }
                var fortress = new HexCoordinates(13, 8);
                if (tiles.TryGetValue(fortress, out var home)) views.Add(("fortaleza", home.transform.position, 6.5f, 52f));
                // -fulgorExplorado <radio>: las casillas a esa distancia de la fortaleza se ven como exploradas y no visibles (la niebla las oscurece)
                if (int.TryParse(ReadArgument("-fulgorExplorado"), out int explored))
                {
                    foreach (var pair in tiles)
                        if (pair.Key.DistanceTo(fortress) <= explored && pair.Key.DistanceTo(fortress) > 0)
                            pair.Value.SetFogVisibility(Fulgor.Core.Campaign.CampaignVisibility.Explored);
                    report.AppendLine($"Casillas exploradas hasta {explored} de la fortaleza");
                }
                var plain = tiles.Values.Where(t => t.Cell.Terrain == TerrainType.Plain)
                    .OrderByDescending(t => Neighbours(t.Cell.Coordinates).Count(n => tiles.TryGetValue(n, out var x) && x.Cell.Terrain == TerrainType.Plain))
                    .ThenBy(t => t.Cell.Coordinates.Q).ThenBy(t => t.Cell.Coordinates.R).FirstOrDefault();
                if (plain != null) { views.Add(("llanura", plain.transform.position, 3.6f, 50f)); report.AppendLine($"Llanura: ({plain.Cell.Coordinates.Q},{plain.Cell.Coordinates.R})"); }

                foreach (var (name, focus, distance, elevation) in views)
                {
                    camera.transform.position = focus + (Vector3.back * Mathf.Cos(elevation * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(elevation * Mathf.Deg2Rad)) * distance;
                    camera.transform.LookAt(focus);
                    for (int i = 0; i < 2; i++) Capture(camera, Path.Combine(output, name + ".png"));
                }
                File.WriteAllText(Path.Combine(output, "terreno.txt"), report.ToString());
                Debug.Log("TERRENO_CAPTURA_OK\n" + report);
                SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        static readonly HexCoordinates[] Directions = { new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1) };
        static IEnumerable<HexCoordinates> Neighbours(HexCoordinates c) => Directions.Select(d => c.Add(d));

        static List<HashSet<HexCoordinates>> Clusters(HashSet<HexCoordinates> cells)
        {
            var left = new HashSet<HexCoordinates>(cells); var result = new List<HashSet<HexCoordinates>>();
            while (left.Count > 0)
            {
                var start = left.OrderBy(c => c.Q).ThenBy(c => c.R).First(); var group = new HashSet<HexCoordinates>(); var stack = new Stack<HexCoordinates>();
                stack.Push(start); left.Remove(start);
                while (stack.Count > 0)
                {
                    var c = stack.Pop(); group.Add(c);
                    foreach (var n in Neighbours(c)) if (left.Remove(n)) stack.Push(n);
                }
                result.Add(group);
            }
            return result;
        }

        static Vector3 Centre(IEnumerable<Vector3> points) { var list = points.ToList(); return list.Aggregate(Vector3.zero, (a, b) => a + b) / list.Count; }

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active; float aspect = camera.aspect;
            try
            {
                target.Create(); camera.aspect = (float)Width / Height;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { camera.aspect = aspect; RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
