using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    /// <summary>
    /// Mide el coste del mapa de Conquista con la cámara de inicio: tiempo de generar el tablero, tiempo de render por fotograma (2560×1440,
    /// esperando a que termine la GPU) y fotograma del procesador quieto y moviendo la cámara, en varios casos para aislar qué cuesta.
    /// Uso: -executeMethod Fulgor.Editor.CampaignPerformanceProbe.Batch -fulgorReviewOutput &lt;carpeta&gt;
    /// </summary>
    [InitializeOnLoad]
    public static class CampaignPerformanceProbe
    {
        const string Running = "Fulgor.PerformanceProbe.Running";
        const string Output = "Fulgor.PerformanceProbe.Output";
        const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        const int GpuFrames = 30, CpuFrames = 150;

        // ajustes: 0 = los del proyecto, 1 = sin sombreador del suelo (montañas generadas sí), 2 = sin nada nuevo
        readonly struct Case
        {
            public readonly string Name; public readonly bool Regenerate, NoNoise, HideBuildings, Cpu; public readonly int Settings;
            public Case(string name, bool regenerate, int settings, bool noNoise, bool hideBuildings, bool cpu)
            { Name = name; Regenerate = regenerate; Settings = settings; NoNoise = noNoise; HideBuildings = hideBuildings; Cpu = cpu; }
        }

        static readonly Case[] Cases =
        {
            new("terreno nuevo", true, 0, false, false, true),
            new("terreno nuevo, sombreador sin ruido", false, 0, true, false, false),
            new("montañas nuevas, suelo con el Lit de antes", true, 1, false, false, false),
            new("terreno de antes", true, 2, false, false, true),
            new("terreno de antes, sin edificios", false, 2, false, true, false),
        };

        enum Phase { Settle, Regenerate, Wait, Gpu, CpuIdleStart, CpuIdle, CpuMoveStart, CpuMove, Next, Done }

        static Phase phase;
        static int index, frameMark, waitUntilFrame;
        static float timeMark;
        static double started;
        static Vector3 cameraHome;
        static Quaternion cameraRotation;
        static CampaignMapAppearanceSettings original;
        static readonly System.Text.StringBuilder report = new();

        static CampaignPerformanceProbe()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        public static void Batch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-fulgorReviewOutput");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta la carpeta del informe.");
                Directory.CreateDirectory(args[at + 1]);
                EditorSceneManager.OpenScene(ConquestScene, OpenSceneMode.Single);
                SessionState.SetString(Output, args[at + 1]); SessionState.SetBool(Running, true);
                phase = Phase.Settle; index = 0; report.Clear(); started = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) { EditorApplication.update -= Tick; return; }
            if (phase == Phase.Done)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SessionState.SetBool(Running, false); EditorApplication.update -= Tick; EditorApplication.Exit(0); return;
            }
            if (EditorApplication.timeSinceStartup - started > 900) { Fail("Tiempo agotado midiendo el rendimiento."); return; }
            if (!EditorApplication.isPlaying) return;
            try { Step(); }
            catch (Exception e) { Debug.LogException(e); Fail(e.Message); }
        }

        static void Step()
        {
            var board = Object.FindFirstObjectByType<CampaignBoardPreview>();
            var appearance = Object.FindFirstObjectByType<CampaignMapAppearance>();
            var buildings = Object.FindFirstObjectByType<CampaignTileBuildings>();
            var camera = Camera.main;
            if (board == null || appearance == null || camera == null || !appearance.Ready) return;
            var current = Cases[Math.Min(index, Cases.Length - 1)];
            switch (phase)
            {
                case Phase.Settle:
                    if (Time.frameCount < 90 || buildings == null || buildings.Placed.Count == 0) return;
                    var navigation = camera.GetComponent<CampaignCameraController>(); if (navigation != null) navigation.enabled = false;
                    cameraHome = camera.transform.position; cameraRotation = camera.transform.rotation;
                    original = (CampaignMapAppearanceSettings)Field(typeof(CampaignMapAppearance), "settings").GetValue(appearance);
                    report.AppendLine($"Cámara de inicio: {cameraHome}, mirando {camera.transform.forward}");
                    report.AppendLine("SRP Batcher, Fulgor/Terreno: " + Batcher(Shader.Find("Fulgor/Terreno")));
                    report.AppendLine("SRP Batcher, URP Lit: " + Batcher(Shader.Find("Universal Render Pipeline/Lit")));
                    phase = Phase.Regenerate; return;
                case Phase.Regenerate:
                    camera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
                    if (current.Regenerate)
                    {
                        var settings = original;
                        if (current.Settings > 0)
                        {
                            settings = Object.Instantiate(original);
                            Field(typeof(CampaignMapAppearanceSettings), "terrainShader").SetValue(settings, null);
                            if (current.Settings > 1) Field(typeof(CampaignMapAppearanceSettings), "reliefMountainMaterial").SetValue(settings, null);
                        }
                        Field(typeof(CampaignMapAppearance), "settings").SetValue(appearance, settings);
                        GC.Collect();
                        var watch = Stopwatch.StartNew(); board.RegenerateBoard(); watch.Stop();
                        report.AppendLine($"[{current.Name}] generar el tablero: {watch.ElapsedMilliseconds} ms");
                    }
                    waitUntilFrame = Time.frameCount + 60; phase = Phase.Wait; return;
                case Phase.Wait:
                    if (Time.frameCount < waitUntilFrame) return;
                    if (current.NoNoise)
                        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                            foreach (var m in r.sharedMaterials) if (m != null && m.shader != null && m.shader.name == "Fulgor/Terreno") m.SetFloat("_FulgorSinRuido", 1f);
                    if (current.HideBuildings && buildings != null)
                        foreach (var placed in buildings.Placed) foreach (var r in placed.GetComponentsInChildren<Renderer>()) r.forceRenderingOff = true;
                    phase = Phase.Gpu; return;
                case Phase.Gpu:
                    camera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
                    report.AppendLine($"[{current.Name}] render por fotograma (2560×1440, media de {GpuFrames}): {Gpu(camera):0.00} ms");
                    phase = current.Cpu ? Phase.CpuIdleStart : Phase.Next; return;
                case Phase.CpuIdleStart:
                    frameMark = Time.frameCount; timeMark = Time.realtimeSinceStartup; phase = Phase.CpuIdle; return;
                case Phase.CpuIdle:
                    if (Time.frameCount - frameMark < CpuFrames) return;
                    report.AppendLine($"[{current.Name}] fotograma del procesador, cámara quieta: {1000f * (Time.realtimeSinceStartup - timeMark) / (Time.frameCount - frameMark):0.00} ms");
                    phase = Phase.CpuMoveStart; return;
                case Phase.CpuMoveStart:
                    frameMark = Time.frameCount; timeMark = Time.realtimeSinceStartup; phase = Phase.CpuMove; return;
                case Phase.CpuMove:
                    float t = (Time.frameCount - frameMark) / (float)CpuFrames;
                    camera.transform.position = cameraHome + new Vector3(Mathf.Sin(t * 6.283f) * 6f, Mathf.Sin(t * 12.566f) * 3f, t * 4f);
                    if (Time.frameCount - frameMark < CpuFrames) return;
                    report.AppendLine($"[{current.Name}] fotograma del procesador, moviendo la cámara: {1000f * (Time.realtimeSinceStartup - timeMark) / (Time.frameCount - frameMark):0.00} ms");
                    phase = Phase.Next; return;
                case Phase.Next:
                    index++;
                    if (index >= Cases.Length) { Finish(); return; }
                    phase = Phase.Regenerate; return;
            }
        }

        static FieldInfo Field(Type type, string name) =>
            type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingFieldException(type.Name, name);

        static string Batcher(Shader shader)
        {
            if (shader == null) return "no encontrado";
            var code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (code == null) return "no se puede consultar en esta versión";
            int value = (int)code.Invoke(null, new object[] { shader, 0 });
            string why = value == 0 || reason == null ? "" : " (" + reason.Invoke(null, new object[] { shader, 0, value }) + ")";
            return value == 0 ? "compatible" : $"no compatible, código {value}{why}";
        }

        static float Gpu(Camera camera)
        {
            var target = new RenderTexture(2560, 1440, 24, RenderTextureFormat.ARGB32); target.Create();
            var probe = new Texture2D(1, 1, TextureFormat.RGBA32, false); var previous = RenderTexture.active;
            float aspect = camera.aspect;
            try
            {
                camera.aspect = 2560f / 1440f;
                void Frame()
                {
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target; probe.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); probe.Apply(); // espera a que la GPU acabe
                }
                for (int i = 0; i < 5; i++) Frame();
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < GpuFrames; i++) Frame();
                return (float)watch.Elapsed.TotalMilliseconds / GpuFrames;
            }
            finally { camera.aspect = aspect; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(probe); }
        }

        static void Finish()
        {
            string output = SessionState.GetString(Output, "");
            File.WriteAllText(Path.Combine(output, "rendimiento.txt"), report.ToString());
            Debug.Log("RENDIMIENTO_OK\n" + report);
            phase = Phase.Done; EditorApplication.isPlaying = false;
        }

        static void Fail(string message)
        {
            Debug.LogError("RENDIMIENTO_FALLO " + message);
            SessionState.SetBool(Running, false); EditorApplication.update -= Tick; EditorApplication.Exit(1);
        }
    }
}
