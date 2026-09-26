using System;
using Fulgor.Core.Randomness;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Randomness
{
    public sealed class DeterministicRandomTests
    {
        private const double Tolerance = 1d / 4294967296d;

        [Test]
        public void MatchesTypeScriptMulberry32Fixture()
        {
            var random = new DeterministicRandom(12345);
            var expected = new[]
            {
                0.9797282677609473d,
                0.3067522644996643d,
                0.484205421525985d,
                0.817934412509203d,
                0.5094283693470061d,
            };

            foreach (var value in expected)
            {
                Assert.That(random.Next(), Is.EqualTo(value).Within(Tolerance));
            }
        }

        [Test]
        public void RepeatsSequenceForSameSeed()
        {
            var first = new DeterministicRandom(54321);
            var second = new DeterministicRandom(54321);

            for (var index = 0; index < 10; index++)
            {
                Assert.That(first.Next(), Is.EqualTo(second.Next()));
            }
        }

        [Test]
        public void ProducesValuesInHalfOpenUnitInterval()
        {
            var random = new DeterministicRandom(100);
            for (var index = 0; index < 100; index++)
            {
                var value = random.Next();
                Assert.That(value, Is.GreaterThanOrEqualTo(0d));
                Assert.That(value, Is.LessThan(1d));
            }
        }

        [Test]
        public void AdvancesAndExposesState()
        {
            var random = new DeterministicRandom(100);
            var initialState = random.State;
            random.Next();
            Assert.That(random.State, Is.Not.EqualTo(initialState));
        }

        [Test]
        public void GeneratesIntegersInInclusiveInterval()
        {
            var random = new DeterministicRandom(9876);
            for (var index = 0; index < 100; index++)
            {
                Assert.That(random.NextInteger(2, 6), Is.InRange(2, 6));
            }
        }

        [Test]
        public void NormalizesNegativeSeedLikeJavaScriptUnsignedShift()
        {
            var negative = new DeterministicRandom(-1);
            var wrapped = new DeterministicRandom(uint.MaxValue);
            Assert.That(negative.Next(), Is.EqualTo(wrapped.Next()));
        }

        [Test]
        public void RejectsSeedOutsideJavaScriptSafeIntegerRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicRandom(long.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicRandom(long.MinValue));
        }

        [Test]
        public void RejectsReversedIntegerInterval()
        {
            var random = new DeterministicRandom(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInteger(5, 2));
        }
    }
}
