using Fulgor.Presentation.Map;
using NUnit.Framework;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class BenchmarkTests
    {
        [Test]
        public void IsDisabledWithoutBenchmarkFlag()
        {
            var options = BenchmarkOptions.Parse(new[] { "Fulgor.exe", "-screen-width", "1920" });
            Assert.That(options.Enabled, Is.False);
            Assert.That(BenchmarkOptions.Parse(null).Enabled, Is.False);
        }

        [Test]
        public void UsesDefaultsWhenOnlyFlagIsPresent()
        {
            var options = BenchmarkOptions.Parse(new[] { "Fulgor.exe", "-benchmark" });
            Assert.That(options.Enabled, Is.True);
            Assert.That(options.Seconds, Is.EqualTo(BenchmarkOptions.DefaultSeconds));
            Assert.That(options.WarmupSeconds, Is.EqualTo(BenchmarkOptions.DefaultWarmupSeconds));
            Assert.That(options.Label, Is.Empty);
            Assert.That(options.OutputPath, Is.Null);
        }

        [Test]
        public void ReadsAllOptionsWithInvariantDecimals()
        {
            var options = BenchmarkOptions.Parse(new[]
            {
                "Fulgor.exe", "-benchmark", "-benchmarkSeconds", "12.5", "-benchmarkWarmup", "0",
                "-benchmarkLabel", "grd-on", "-benchmarkOutput", "D:/medidas/grd.json",
            });
            Assert.That(options.Seconds, Is.EqualTo(12.5f));
            Assert.That(options.WarmupSeconds, Is.EqualTo(0f));
            Assert.That(options.Label, Is.EqualTo("grd-on"));
            Assert.That(options.OutputPath, Is.EqualTo("D:/medidas/grd.json"));
        }

        [Test]
        public void IgnoresInvalidDurations()
        {
            var options = BenchmarkOptions.Parse(new[]
                { "-benchmark", "-benchmarkSeconds", "0", "-benchmarkWarmup", "-1" });
            Assert.That(options.Seconds, Is.EqualTo(BenchmarkOptions.DefaultSeconds));
            Assert.That(options.WarmupSeconds, Is.EqualTo(BenchmarkOptions.DefaultWarmupSeconds));
        }

        [Test]
        public void RejectsNonFiniteDurations()
        {
            var options = BenchmarkOptions.Parse(new[]
            {
                "-benchmark", "-benchmarkSeconds", "Infinity",
                "-benchmarkWarmup", "NaN",
            });
            Assert.That(options.Seconds, Is.EqualTo(BenchmarkOptions.DefaultSeconds));
            Assert.That(options.WarmupSeconds, Is.EqualTo(BenchmarkOptions.DefaultWarmupSeconds));
        }
        [Test]
        public void SummarizesFrameTimesWithNearestRankPercentiles()
        {
            var samples = new double[100];
            for (var i = 0; i < samples.Length; i++) samples[i] = i + 1;

            var statistics = FrameTimeStatistics.From(samples);

            Assert.That(statistics.Frames, Is.EqualTo(100));
            Assert.That(statistics.AverageMilliseconds, Is.EqualTo(50.5d).Within(1e-9));
            Assert.That(statistics.MedianMilliseconds, Is.EqualTo(50d));
            Assert.That(statistics.P95Milliseconds, Is.EqualTo(95d));
            Assert.That(statistics.P99Milliseconds, Is.EqualTo(99d));
            Assert.That(statistics.WorstMilliseconds, Is.EqualTo(100d));
            Assert.That(statistics.AverageFps, Is.EqualTo(1000d / 50.5d).Within(1e-9));
        }

        [Test]
        public void CountsOnlyFramesAboveTheThreshold()
        {
            var samples = new[] { 1.4d, 2.0d, 50d, 50.1d, 16278.9d };
            Assert.That(FrameTimeStatistics.CountAbove(samples, 50d), Is.EqualTo(2));
            Assert.That(FrameTimeStatistics.CountAbove(new double[0], 50d), Is.Zero);
        }

        [Test]
        public void ReturnsEmptySummaryWithoutSamples()
        {
            var statistics = FrameTimeStatistics.From(new double[0]);
            Assert.That(statistics.Frames, Is.Zero);
            Assert.That(statistics.AverageFps, Is.Zero);
        }
    }
}
