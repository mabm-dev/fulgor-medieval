using System.Globalization;

namespace Fulgor.Presentation.Map
{
    /// <summary>Opciones del modo benchmark leídas de la línea de comandos del build.</summary>
    public readonly struct BenchmarkOptions
    {
        public const float DefaultSeconds = 20f;
        public const float DefaultWarmupSeconds = 3f;

        public BenchmarkOptions(bool enabled, float seconds, float warmupSeconds, string label, string outputPath)
        {
            Enabled = enabled;
            Seconds = seconds;
            WarmupSeconds = warmupSeconds;
            Label = label;
            OutputPath = outputPath;
        }

        public bool Enabled { get; }
        public float Seconds { get; }
        public float WarmupSeconds { get; }
        public string Label { get; }
        public string OutputPath { get; }

        /// <summary>
        /// Admite <c>-benchmark</c>, <c>-benchmarkSeconds N</c>, <c>-benchmarkWarmup N</c>,
        /// <c>-benchmarkLabel texto</c> y <c>-benchmarkOutput ruta</c>.
        /// </summary>
        public static BenchmarkOptions Parse(string[] args)
        {
            var enabled = false;
            var seconds = DefaultSeconds;
            var warmup = DefaultWarmupSeconds;
            var label = string.Empty;
            string output = null;
            if (args == null) return new BenchmarkOptions(false, seconds, warmup, label, null);

            for (var i = 0; i < args.Length; i++)
            {
                var next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-benchmark":
                        enabled = true;
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

            return new BenchmarkOptions(enabled, seconds, warmup, label, output);
        }

        private static bool TryReadPositive(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0f;

        private static bool TryReadNonNegative(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0f;
    }
}
