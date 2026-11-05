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
    /// Captura cada edificio de casilla dentro de la escena real de Conquista, desde los cuatro lados y con la cámara en perspectiva,
    /// para comprobar a qué lado mira la puerta, el tamaño sobre la casilla y el aspecto con la luz del juego.
    /// Uso: -executeMethod Fulgor.Editor.CampaignTileBuildingCapture.Batch -fulgorReviewOutput &lt;carpeta&gt;
    /// </summary>
    [InitializeOnLoad]
    public static class CampaignTileBuildingCapture
    {
        const string Running = "Fulgor.TileBuildingCapture.Running";
        const string Output = "Fulgor.TileBuildingCapture.Output";
        const string Stage = "Fulgor.TileBuildingCapture.Stage";
        const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        const int Width = 1000, Height = 750;
        static double started;
        static int frames;

        static CampaignTileBuildingCapture()
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
            if (EditorApplication.timeSinceStartup - started > 240)
            { Debug.LogError("Tiempo agotado al capturar los edificios."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var buildings = UnityEngine.Object.FindFirstObjectByType<CampaignTileBuildings>();
            if (buildings == null || buildings.Placed.Count == 0 || ++frames < 90) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main ?? throw new InvalidOperationException("La escena no tiene cámara principal.");
                var navigation = camera.GetComponent<CampaignCameraController>();
                if (navigation != null) navigation.enabled = false;
                string output = SessionState.GetString(Output, "");
                var report = new System.Text.StringBuilder();
                // opcionales: -fulgorElevation <grados> (32 por defecto; el juego mira desde más arriba), -fulgorZoom <factores separados por
                // comas> (3.4; varios dan una captura por distancia, _z1, _z2...), -fulgorOnly <nombres con guiones bajos, separados por comas>
                // y -fulgorSides <sur,este,norte,oeste>
                float elevation = ReadFloat("-fulgorElevation", 32f);
                var zooms = (ReadText("-fulgorZoom") ?? "3.4").Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(z => float.Parse(z, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var only = ReadText("-fulgorOnly")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var sides = ReadText("-fulgorSides")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                // -fulgorBump <intensidad>: cambia la fuerza del mapa de normales de los edificios (0 lo quita) para ver qué oscurece
                if (ReadText("-fulgorBump") != null)
                {
                    float bump = ReadFloat("-fulgorBump", 1f);
                    foreach (var placed in buildings.Placed.Where(b => b != null))
                        foreach (var renderer in placed.GetComponentsInChildren<Renderer>())
                            foreach (var material in renderer.materials) material.SetFloat("_BumpScale", bump);
                }
                foreach (var building in buildings.Placed.Where(b => b != null && (only == null || only.Contains(Safe(b.name)))))
                {
                    var renderers = building.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    int triangles = building.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh.triangles.Length / 3);
                    report.AppendLine($"{building.name}: {triangles} triángulos, tamaño {bounds.size.x:0.00} × {bounds.size.y:0.00} × {bounds.size.z:0.00}, yaw {building.transform.eulerAngles.y:0}");
                    var focus = bounds.center - Vector3.up * bounds.extents.y * .25f;
                    for (int z = 0; z < zooms.Length; z++)
                    {
                        float distance = Mathf.Max(bounds.extents.x, bounds.extents.z) * zooms[z] + 1f;
                        string suffix = zooms.Length > 1 ? $"_z{z + 1}" : "";
                        foreach (var (side, direction) in new[] { ("sur", Vector3.back), ("este", Vector3.right), ("norte", Vector3.forward), ("oeste", Vector3.left) })
                        {
                            if (sides != null && !sides.Contains(side)) continue;
                            camera.transform.position = focus + (direction * Mathf.Cos(elevation * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(elevation * Mathf.Deg2Rad)) * distance;
                            camera.transform.LookAt(focus);
                            for (int i = 0; i < 2; i++) Capture(camera, Path.Combine(output, $"{Safe(building.name)}_{side}{suffix}.png"));
                        }
                    }
                }
                File.WriteAllText(Path.Combine(output, "edificios.txt"), report.ToString());
                Debug.Log("EDIFICIOS_CAPTURA_OK\n" + report);
                SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        static string ReadText(string name)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }

        static float ReadFloat(string name, float fallback) =>
            float.TryParse(ReadText(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;

        static string Safe(string name) => new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
