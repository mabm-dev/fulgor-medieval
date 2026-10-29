using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Mide los tiempos de fotograma del tablero cuando el build se lanza con <c>-benchmark</c>:
    /// fija la cámara en la vista general, descarta el calentamiento, guarda un JSON junto al
    /// ejecutable y cierra el juego. Con -benchmarkOffscreen solicita render explícito a textura
    /// y guarda un PNG; esa medición diagnóstica no equivale a la presentación en pantalla.
    /// </summary>
    public sealed class CampaignBenchmark : MonoBehaviour
    {
        private readonly List<double> frameMilliseconds = new(4096);
        private readonly List<double> cpuMilliseconds = new(4096);
        private readonly List<double> gpuMilliseconds = new(4096);
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private BenchmarkOptions options;
        private float elapsed;
        private bool finished;
        private Camera benchmarkCamera;
        private bool previousCameraEnabled;
        private UnityEngine.Rendering.RenderPipeline.StandardRequest renderRequest;
        private RenderTexture benchmarkTarget,previousTarget;
        private int focusLosses;

        /// <summary>Fotogramas por encima de este tiempo se cuentan como tirones en el informe.</summary>
        public const double SlowFrameMilliseconds = 50d;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromCommandLine()
        {
            var parsed = BenchmarkOptions.Parse(Environment.GetCommandLineArgs());
            if (!parsed.Enabled || FindAnyObjectByType<CampaignBoardPreview>() == null) return;
            var runner = new GameObject("Benchmark").AddComponent<CampaignBenchmark>();
            runner.options = parsed;
        }

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            // Sin esto, perder el foco pausa el juego y el primer fotograma al volver
            // acumula toda la pausa (se midió un fotograma de 16 s al cambiar de ventana).
            Application.runInBackground = true;
            Application.targetFrameRate = -1;
            var cameraController = FindAnyObjectByType<CampaignCameraController>();
            if (cameraController != null) cameraController.enabled = false;
            benchmarkCamera=Camera.main;
            if(options.Offscreen&&benchmarkCamera!=null) {
                previousTarget=benchmarkCamera.targetTexture;previousCameraEnabled=benchmarkCamera.enabled;
                benchmarkTarget=new RenderTexture(Mathf.Max(64,Screen.width),Mathf.Max(64,Screen.height),24,RenderTextureFormat.ARGB32){name="Benchmark render target"};
                benchmarkTarget.Create();benchmarkCamera.targetTexture=benchmarkTarget;benchmarkCamera.enabled=false;
                renderRequest=new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=benchmarkTarget};
            }
            var selection = FindAnyObjectByType<CampaignBoardSelection>();
            if (selection != null) selection.enabled = false;
            Debug.Log($"Fulgor benchmark: {options.WarmupSeconds:F0} s de calentamiento, " +
                $"{options.Seconds:F0} s de medición.");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !finished) focusLosses++;
        }

        private void Update()
        {
            if (finished) return;
            if(benchmarkCamera!=null&&renderRequest!=null)
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(benchmarkCamera,renderRequest);
            FrameTimingManager.CaptureFrameTimings();
            elapsed += Time.unscaledDeltaTime;
            if (elapsed <= options.WarmupSeconds) return;

            frameMilliseconds.Add(Time.unscaledDeltaTime * 1000d);
            if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
            {
                if (timings[0].cpuFrameTime > 0d) cpuMilliseconds.Add(timings[0].cpuFrameTime);
                if (timings[0].gpuFrameTime > 0d) gpuMilliseconds.Add(timings[0].gpuFrameTime);
            }

            if (elapsed < options.WarmupSeconds + options.Seconds) return;
            finished = true;
            try
            {
                WriteReport();
                Application.Quit();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Fulgor benchmark: no se pudo guardar el informe: {exception}");
                Application.Quit(1);
            }
        }

        private void WriteReport()
        {
            var frames = FrameTimeStatistics.From(frameMilliseconds);
            var cpu = FrameTimeStatistics.From(cpuMilliseconds);
            var gpu = FrameTimeStatistics.From(gpuMilliseconds);
            var diagnostics = FindAnyObjectByType<CampaignBoardDiagnostics>();
            var metrics = diagnostics != null && diagnostics.Completed ? diagnostics.Metrics : default;

            var report = new BenchmarkReport
            {
                offscreenRender = benchmarkTarget != null,
                label = options.Label,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                unityVersion = Application.unityVersion,
                gpu = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                cpu = SystemInfo.processorType,
                resolution = $"{Screen.width}x{Screen.height} {Screen.fullScreenMode}",
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                tiles = metrics.Tiles,
                renderers = metrics.Renderers,
                boardBuildMilliseconds = metrics.Milliseconds,
                measuredSeconds = options.Seconds,
                frames = frames.Frames,
                averageFps = frames.AverageFps,
                averageMilliseconds = frames.AverageMilliseconds,
                medianMilliseconds = frames.MedianMilliseconds,
                p95Milliseconds = frames.P95Milliseconds,
                p99Milliseconds = frames.P99Milliseconds,
                worstMilliseconds = frames.WorstMilliseconds,
                cpuSamples = cpu.Frames,
                gpuSamples = gpu.Frames,
                slowFrames = FrameTimeStatistics.CountAbove(frameMilliseconds, SlowFrameMilliseconds),
                focusLosses = focusLosses,
                cpuAverageMilliseconds = cpu.AverageMilliseconds,
                cpuP95Milliseconds = cpu.P95Milliseconds,
                gpuAverageMilliseconds = gpu.AverageMilliseconds,
                gpuP95Milliseconds = gpu.P95Milliseconds,
            };

            var path = ResolveOutputPath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            if(benchmarkTarget!=null) {
                var previous=RenderTexture.active;var image=new Texture2D(benchmarkTarget.width,benchmarkTarget.height,TextureFormat.RGBA32,false);
                try {RenderTexture.active=benchmarkTarget;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.ChangeExtension(path,"png"),image.EncodeToPNG());}
                finally {RenderTexture.active=previous;Destroy(image);}
            }
            Debug.Log($"Fulgor benchmark: {frames.AverageFps:F1} FPS, media {frames.AverageMilliseconds:F2} ms, " +
                $"p95 {frames.P95Milliseconds:F2} ms, CPU {cpu.AverageMilliseconds:F2} ms, " +
                $"GPU {gpu.AverageMilliseconds:F2} ms, {report.slowFrames} tirones, " +
                $"{focusLosses} pérdidas de foco. Informe: {path}");
        }

        private void OnDestroy()
        {
            if(benchmarkTarget!=null&&benchmarkCamera!=null){benchmarkCamera.targetTexture=previousTarget;benchmarkCamera.enabled=previousCameraEnabled;}
            if(benchmarkTarget!=null){benchmarkTarget.Release();Destroy(benchmarkTarget);}
        }

        private string ResolveOutputPath()
        {
            if (!string.IsNullOrEmpty(options.OutputPath)) return Path.GetFullPath(options.OutputPath);
            var folder = Directory.GetParent(Application.dataPath)?.FullName ?? Application.persistentDataPath;
            var name = string.IsNullOrEmpty(options.Label) ? "benchmark" : $"benchmark-{options.Label}";
            return Path.Combine(folder, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        }

        [Serializable]
        private sealed class BenchmarkReport
        {
            public bool offscreenRender;
            public string label;
            public string date;
            public string unityVersion;
            public string gpu;
            public string graphicsApi;
            public string cpu;
            public string resolution;
            public string quality;
            public int tiles;
            public int renderers;
            public double boardBuildMilliseconds;
            public float measuredSeconds;
            public int frames;
            public double averageFps;
            public double averageMilliseconds;
            public double medianMilliseconds;
            public double p95Milliseconds;
            public double p99Milliseconds;
            public double worstMilliseconds;
            public int cpuSamples;
            public int gpuSamples;
            public int slowFrames;
            public int focusLosses;
            public double cpuAverageMilliseconds;
            public double cpuP95Milliseconds;
            public double gpuAverageMilliseconds;
            public double gpuP95Milliseconds;
        }
    }
}
