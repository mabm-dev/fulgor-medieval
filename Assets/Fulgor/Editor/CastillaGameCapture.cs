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
    /// Captura la fortaleza de Castilla dentro de la campaña real (CampaignPrototype, sin escenas de revisión): comprueba que
    /// el asentamiento instancia el prefab de juego, que tiene caja de selección y que se puede seleccionar.
    /// Uso: -executeMethod Fulgor.Editor.CastillaGameCapture.Batch -fulgorReviewOutput &lt;carpeta&gt;
    /// </summary>
    [InitializeOnLoad]
    public static class CastillaGameCapture
    {
        const string Running = "Fulgor.CastillaGameCapture.Running";
        const string Output = "Fulgor.CastillaGameCapture.Output";
        const string Stage = "Fulgor.CastillaGameCapture.Stage";
        static readonly Vector3 CameraOffset = new(0f, 28f, -22f);
        static double started;
        static int frames;

        static CastillaGameCapture()
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
                EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity", OpenSceneMode.Single);
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
            if (EditorApplication.timeSinceStartup - started > 150)
            { Debug.LogError("Tiempo agotado al capturar la campaña."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var controller = UnityEngine.Object.FindFirstObjectByType<CampaignSettlementController>();
            if (controller == null || !controller.Ready || ++frames < 20) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var views = UnityEngine.Object.FindObjectsByType<CampaignSettlementView>(FindObjectsSortMode.None);
                var fortress = views.FirstOrDefault(v => v.name.Contains("Castilla")) ?? views.FirstOrDefault()
                    ?? throw new InvalidOperationException("No hay asentamientos en la campaña.");
                var renderers = fortress.GetComponentsInChildren<MeshRenderer>();
                var triangles = fortress.GetComponentsInChildren<MeshFilter>().Sum(m => (int)(m.sharedMesh.triangles.Length / 3));
                var collider = fortress.GetComponent<BoxCollider>() ?? throw new InvalidOperationException("El asentamiento no tiene caja de selección.");
                var camera = Camera.main;
                var navigation = camera.GetComponent<CampaignCameraController>();
                if (navigation != null) navigation.enabled = false;
                string output = SessionState.GetString(Output, "");
                var focus = fortress.transform.position;
                camera.transform.position = focus + CameraOffset; camera.transform.LookAt(focus);
                foreach (float zoom in new[] { 6f, 3f, 1.4f })
                {
                    camera.orthographicSize = zoom;
                    for (int i = 0; i < 2; i++) Capture(camera, Path.Combine(output, $"Castilla_Campana_Zoom{zoom:0.0}.png"));
                }
                // la caja de selección debe acertar con un rayo desde la cámara al centro de la fortaleza
                var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth / 2f, camera.pixelHeight / 2f, 0));
                bool acierta = collider.Raycast(ray, out var hit, 100f);
                fortress.SetSelected(true);
                camera.orthographicSize = 3f;
                Capture(camera, Path.Combine(output, "Castilla_Campana_Seleccionada.png"));
                File.WriteAllText(Path.Combine(output, "campana.json"),
                    "{\"asentamiento\":\"" + fortress.name + "\",\"triangulos\":" + triangles + ",\"renderers\":" + renderers.Length +
                    ",\"caja_seleccion\":[" + collider.size.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
                    collider.size.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "," +
                    collider.size.z.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "],\"rayo_acierta\":" + (acierta ? "true" : "false") + "}");
                Debug.Log("CASTILLA_CAMPANA_OK " + fortress.name + " triangulos=" + triangles + " rayo=" + acierta);
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
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
