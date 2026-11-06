using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private bool classicPending, moveReady;
        private double classicRegenerateMilliseconds = -1d;
        private Vector3 moveFocus, moveForward, moveRight, moveForwardFlat;
        private float moveDistance;
        private static BenchmarkOptions pending;
        // línea de tiempo del arranque: cuánto tarda en cargar la escena, cuándo queda listo el tablero, cuándo se colocan los edificios y cada
        // fotograma lento, también los del calentamiento (que no entran en las estadísticas)
        private static float loadRequestedAt = -1f, sceneLoadedAt = -1f;
        private readonly List<string> timeline = new();
        private bool boardNoted, buildingsNoted, started, exploredApplied;

        /// <summary>Fotogramas por encima de este tiempo se cuentan como tirones en el informe.</summary>
        public const double SlowFrameMilliseconds = 50d;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromCommandLine()
        {
            var parsed = BenchmarkOptions.Parse(Environment.GetCommandLineArgs());
            if (!parsed.Enabled) return;
            if (FindAnyObjectByType<CampaignBoardPreview>() != null) { Create(parsed); return; }
            if (string.IsNullOrEmpty(parsed.Scene)) return;
            // el build empieza en el menú: se carga la escena pedida y el benchmark arranca cuando aparece el tablero
            pending = parsed;
            SceneManager.sceneLoaded += OnSceneLoaded;
            loadRequestedAt = Time.realtimeSinceStartup;
            SceneManager.LoadScene(parsed.Scene);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<CampaignBoardPreview>() == null) return;
            sceneLoadedAt = Time.realtimeSinceStartup;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Create(pending);
        }

        private static void Create(BenchmarkOptions options)
        {
            var runner = new GameObject("Benchmark").AddComponent<CampaignBenchmark>();
            runner.options = options;
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
                // la imagen de revisión, como poco a 1600×1000 aunque la ventana salga pequeña
                benchmarkTarget=new RenderTexture(Mathf.Max(1600,Screen.width),Mathf.Max(1000,Screen.height),24,RenderTextureFormat.ARGB32){name="Benchmark render target"};
                benchmarkTarget.Create();benchmarkCamera.targetTexture=benchmarkTarget;benchmarkCamera.enabled=false;
                renderRequest=new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=benchmarkTarget};
            }
            var selection = FindAnyObjectByType<CampaignBoardSelection>();
            if (selection != null) selection.enabled = false;
            classicPending = options.ClassicTerrain;
            Debug.Log($"Fulgor benchmark: {options.WarmupSeconds:F0} s de calentamiento, " +
                $"{options.Seconds:F0} s de medición.");
            Note(loadRequestedAt >= 0f && sceneLoadedAt >= loadRequestedAt
                ? $"empieza; la escena tardó {sceneLoadedAt - loadRequestedAt:0.00} s en cargar"
                : "empieza en la escena con la que arrancó el juego");
        }

        private void Note(string what)
        {
            var line = $"{Time.realtimeSinceStartup:0.00} s desde el arranque ({elapsed:0.00} s de benchmark): {what}";
            timeline.Add(line);
            Debug.Log("Fulgor benchmark: " + line);
        }

        // revisión de la niebla: se quita y las casillas cerca del centro de la cámara se marcan como exploradas, que es como las oscurece
        private void ApplyExplored()
        {
            var appearance = FindAnyObjectByType<CampaignMapAppearance>();
            if (appearance == null || !appearance.Ready || benchmarkCamera == null) return;
            var fog = FindAnyObjectByType<CampaignFogMap>();
            if (fog != null) { fog.ResetFog(); fog.enabled = false; }
            var tiles = FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var ray = benchmarkCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            var focus = new Plane(Vector3.up, Vector3.up * CampaignMountainRelief.EdgeHeight).Raycast(ray, out float hit) ? ray.GetPoint(hit) : benchmarkCamera.transform.position;
            HexTileView centre = null; float best = float.MaxValue;
            foreach (var tile in tiles)
            {
                float d = Vector2.Distance(new Vector2(tile.transform.position.x, tile.transform.position.z), new Vector2(focus.x, focus.z));
                if (d < best) { best = d; centre = tile; }
            }
            if (centre == null) return;
            int count = 0;
            foreach (var tile in tiles)
                if (tile.Cell.Coordinates.DistanceTo(centre.Cell.Coordinates) <= options.ExploredRadius)
                { tile.SetFogVisibility(Fulgor.Core.Campaign.CampaignVisibility.Explored); count++; }
            exploredApplied = true;
            Note($"sin niebla; {count} casillas exploradas alrededor de ({centre.Cell.Coordinates.Q},{centre.Cell.Coordinates.R})");
        }

        // apunta en la línea de tiempo los fotogramas lentos y los momentos en que quedan listos el tablero y los edificios
        private void NoteStartup()
        {
            double frame = Time.unscaledDeltaTime * 1000d;
            if (!started) Note($"primer fotograma tras la carga: {frame:0} ms desde el anterior (arranque del motor, escena y tablero; no cuenta)");
            else if (frame > SlowFrameMilliseconds) Note($"fotograma de {frame:0} ms");
            if (!boardNoted)
            {
                var appearance = FindAnyObjectByType<CampaignMapAppearance>();
                if (appearance != null && appearance.Ready)
                {
                    boardNoted = true;
                    var diagnostics = FindAnyObjectByType<CampaignBoardDiagnostics>();
                    Note($"tablero listo ({(diagnostics != null && diagnostics.Completed ? diagnostics.Metrics.Milliseconds : -1d):0} ms)");
                }
            }
            if (!buildingsNoted)
            {
                var buildings = FindAnyObjectByType<CampaignTileBuildings>();
                if (buildings == null) buildingsNoted = true;
                else if (buildings.Placed.Count > 0)
                {
                    buildingsNoted = true;
                    Note($"{buildings.Placed.Count} edificios colocados ({buildings.PlaceMilliseconds:0} ms, de ellos mapa de huellas {buildings.GroundMapMilliseconds:0} ms)");
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !finished) focusLosses++;
        }

        private void Update()
        {
            if (finished) return;
            if (classicPending)
            {
                // terreno de antes: se regenera el tablero con las piezas fijas y el calentamiento empieza de nuevo
                var appearance = FindAnyObjectByType<CampaignMapAppearance>();
                var board = FindAnyObjectByType<CampaignBoardPreview>();
                if (appearance == null || board == null || !appearance.Ready) return;
                appearance.UseFixedPiecesOnly();
                var watch = System.Diagnostics.Stopwatch.StartNew(); board.RegenerateBoard(); watch.Stop();
                classicRegenerateMilliseconds = watch.Elapsed.TotalMilliseconds; classicPending = false; elapsed = 0f;
                return;
            }
            if (options.ExploredRadius >= 0 && !exploredApplied) ApplyExplored();
            if (!started)
            {
                // el primer fotograma tras cargar la escena arrastra toda la espera de la carga (se midieron 5,7 s: 2,7 de arranque del motor y 3 de
                // escena con el tablero); se apunta en la línea de tiempo, pero no cuenta para el calentamiento ni para las estadísticas
                NoteStartup();
                started = true;
                return;
            }
            if (options.MoveCamera) MoveCamera();
            if(benchmarkCamera!=null&&renderRequest!=null)
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(benchmarkCamera,renderRequest);
            FrameTimingManager.CaptureFrameTimings();
            elapsed += Time.unscaledDeltaTime;
            NoteStartup();
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

        /// <summary>Recorre el mapa en un ocho de 20×12 unidades cada 10 s, acercándose y alejándose un 30 %: unas 6 unidades por segundo.</summary>
        private void MoveCamera()
        {
            if (benchmarkCamera == null) return;
            var t = benchmarkCamera.transform;
            if (!moveReady)
            {
                moveForward = t.forward;
                moveRight = Vector3.ProjectOnPlane(t.right, Vector3.up).normalized;
                moveForwardFlat = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
                moveDistance = Mathf.Abs(moveForward.y) > .01f ? Mathf.Abs(t.position.y / moveForward.y) : 20f;
                moveFocus = t.position + moveForward * moveDistance;
                moveReady = true;
            }
            float w = 2f * Mathf.PI / 10f, s = elapsed;
            var focus = moveFocus + moveRight * (10f * Mathf.Sin(w * s)) + moveForwardFlat * (6f * Mathf.Sin(2f * w * s));
            t.position = focus - moveForward * (moveDistance * (1f + .3f * Mathf.Sin(1.5f * w * s)));
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
                scene = SceneManager.GetActiveScene().name,
                moveCamera = options.MoveCamera,
                classicTerrain = options.ClassicTerrain,
                classicRegenerateMilliseconds = classicRegenerateMilliseconds,
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
                timeline = timeline.ToArray(),
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
            public string scene;
            public bool moveCamera;
            public bool classicTerrain;
            public double classicRegenerateMilliseconds;
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
            public string[] timeline;
        }
    }
}
