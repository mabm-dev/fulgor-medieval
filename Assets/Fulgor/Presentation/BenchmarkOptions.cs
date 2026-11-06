using System.Globalization;

namespace Fulgor.Presentation.Map
{
    /// <summary>Opciones del modo benchmark leídas de la línea de comandos del build.</summary>
    public readonly struct BenchmarkOptions
    {
        public const float DefaultSeconds = 20f;
        public const float DefaultWarmupSeconds = 3f;

        public BenchmarkOptions(bool enabled, float seconds, float warmupSeconds, string label, string outputPath, bool offscreen = false,
            string scene = null, bool moveCamera = false, bool classicTerrain = false, int exploredRadius = -1)
        {
            Enabled = enabled;
            Offscreen = offscreen;
            Seconds = seconds;
            WarmupSeconds = warmupSeconds;
            Label = label;
            OutputPath = outputPath;
            Scene = scene;
            MoveCamera = moveCamera;
            ClassicTerrain = classicTerrain;
            ExploredRadius = exploredRadius;
        }

        public bool Enabled { get; }
        public bool Offscreen { get; }
        public float Seconds { get; }
        public float WarmupSeconds { get; }
        public string Label { get; }
        public string OutputPath { get; }
        /// <summary>Escena que se carga si el build arranca en el menú (<c>-benchmarkScene FulgorConquest</c>).</summary>
        public string Scene { get; }
        /// <summary>Mueve la cámara por el mapa mientras mide, como al jugar (<c>-benchmarkMove</c>).</summary>
        public bool MoveCamera { get; }
        /// <summary>Mide con las piezas fijas de terreno de antes, para comparar (<c>-benchmarkClassicTerrain</c>).</summary>
        public bool ClassicTerrain { get; }
        /// <summary>
        /// Revisión de la niebla: sin niebla, las casillas a esta distancia de la del centro de la cámara se ven como exploradas y no visibles
        /// (<c>-benchmarkExplored N</c>); con <c>-benchmarkOffscreen</c> queda en el PNG. −1, sin tocar.
        /// </summary>
        public int ExploredRadius { get; }

        /// <summary>
        /// Admite <c>-benchmark</c>, <c>-benchmarkSeconds N</c>, <c>-benchmarkWarmup N</c>,
        /// <c>-benchmarkLabel texto</c>, <c>-benchmarkOutput ruta</c>, <c>-benchmarkScene escena</c>,
        /// <c>-benchmarkMove</c>, <c>-benchmarkClassicTerrain</c> y <c>-benchmarkExplored N</c>.
        /// </summary>
        public static BenchmarkOptions Parse(string[] args)
        {
            var enabled = false;
            var offscreen = false;
            var seconds = DefaultSeconds;
            var warmup = DefaultWarmupSeconds;
            var label = string.Empty;
            string output = null;
            string scene = null;
            var move = false;
            var classic = false;
            var explored = -1;
            if (args == null) return new BenchmarkOptions(false, seconds, warmup, label, null);

            for (var i = 0; i < args.Length; i++)
            {
                var next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-benchmarkOffscreen":
                        offscreen = true;
                        break;
                    case "-benchmark":
                        enabled = true;
                        break;
                    case "-benchmarkMove":
                        move = true;
                        break;
                    case "-benchmarkClassicTerrain":
                        classic = true;
                        break;
                    case "-benchmarkExplored" when int.TryParse(next, NumberStyles.Integer, CultureInfo.InvariantCulture, out var radius) && radius >= 0:
                        explored = radius;
                        i++;
                        break;
                    case "-benchmarkScene" when next != null:
                        scene = next;
                        i++;
                        break;
                    case "-benchmarkSeconds" when TryReadPositive(next, out var value):
                        seconds = value;
                        i++;
                        break;
                    case "-benchmarkWarmup" when TryReadNonNegative(next, out var value):
                        warmup = value;
                        i++;
                        break;
                    case "-benchmarkLabel" when next != null:
                        label = next;
                        i++;
                        break;
                    case "-benchmarkOutput" when next != null:
                        output = next;
                        i++;
                        break;
                }
            }

            return new BenchmarkOptions(enabled, seconds, warmup, label, output, offscreen, scene, move, classic, explored);
        }

        private static bool TryReadPositive(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

        private static bool TryReadNonNegative(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
