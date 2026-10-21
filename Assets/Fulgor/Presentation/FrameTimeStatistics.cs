using System;
using System.Collections.Generic;

namespace Fulgor.Presentation.Map
{
    /// <summary>Resumen de tiempos de fotograma en milisegundos.</summary>
    public readonly struct FrameTimeStatistics
    {
        private FrameTimeStatistics(int frames, double average, double median, double p95, double p99,
            double worst)
        {
            Frames = frames;
            AverageMilliseconds = average;
            MedianMilliseconds = median;
            P95Milliseconds = p95;
            P99Milliseconds = p99;
            WorstMilliseconds = worst;
        }

        public int Frames { get; }
        public double AverageMilliseconds { get; }
        public double MedianMilliseconds { get; }
        public double P95Milliseconds { get; }
        public double P99Milliseconds { get; }
        public double WorstMilliseconds { get; }
        public double AverageFps => AverageMilliseconds > 0d ? 1000d / AverageMilliseconds : 0d;

        public static FrameTimeStatistics From(IReadOnlyCollection<double> milliseconds)
        {
            if (milliseconds == null) throw new ArgumentNullException(nameof(milliseconds));
            if (milliseconds.Count == 0) return default;

            var sorted = new List<double>(milliseconds);
            sorted.Sort();
            var sum = 0d;
            foreach (var value in sorted) sum += value;
            return new FrameTimeStatistics(sorted.Count, sum / sorted.Count, Percentile(sorted, 0.5),
                Percentile(sorted, 0.95), Percentile(sorted, 0.99), sorted[sorted.Count - 1]);
        }

        /// <summary>Percentil por rango más cercano sobre una lista ya ordenada.</summary>
        private static double Percentile(IReadOnlyList<double> sorted, double fraction)
        {
            var rank = (int)Math.Ceiling(fraction * sorted.Count);
            return sorted[Math.Clamp(rank - 1, 0, sorted.Count - 1)];
        }
    }
}
