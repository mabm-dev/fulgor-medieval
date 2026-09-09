using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Map
{
    public sealed class HexCoordinatesTests
    {
        [Test]
        public void CreatesStableKey()
        {
            Assert.That(new HexCoordinates(3, -2).Key, Is.EqualTo("3,-2"));
        }

        [Test]
        public void AddsCoordinatesWithoutChangingOrigin()
        {
            var origin = new HexCoordinates(2, 1);
            Assert.That(origin.Add(new HexCoordinates(-1, 1)), Is.EqualTo(new HexCoordinates(1, 2)));
            Assert.That(origin, Is.EqualTo(new HexCoordinates(2, 1)));
        }

        [Test]
        public void ReturnsSixNeighborsInCanonicalOrder()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    new HexCoordinates(1, 0),
                    new HexCoordinates(1, -1),
                    new HexCoordinates(0, -1),
                    new HexCoordinates(-1, 0),
                    new HexCoordinates(-1, 1),
                    new HexCoordinates(0, 1),
                },
                new HexCoordinates(0, 0).Neighbors());
        }

        [Test]
        public void CalculatesSymmetricDistance()
        {
            var origin = new HexCoordinates(0, 0);
            var destination = new HexCoordinates(2, -1);
            Assert.That(origin.DistanceTo(origin), Is.Zero);
            Assert.That(origin.DistanceTo(destination), Is.EqualTo(2));
            Assert.That(destination.DistanceTo(origin), Is.EqualTo(2));
        }

        [TestCase(0, 1)]
        [TestCase(1, 7)]
        [TestCase(2, 19)]
        [TestCase(3, 37)]
        public void CountsCellsInRadius(int radius, int expectedCount)
        {
            var center = new HexCoordinates(2, -1);
            var cells = center.CellsInRadius(radius);
            Assert.That(cells, Has.Count.EqualTo(expectedCount));
            Assert.That(new HashSet<HexCoordinates>(cells), Has.Count.EqualTo(expectedCount));
            foreach (var cell in cells)
            {
                Assert.That(center.DistanceTo(cell), Is.LessThanOrEqualTo(radius));
            }
        }

        [Test]
        public void RejectsNegativeRadius()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HexCoordinates(0, 0).CellsInRadius(-1));
        }
    }
}
