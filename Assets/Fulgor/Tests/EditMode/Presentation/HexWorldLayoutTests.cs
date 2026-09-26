using System;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class HexWorldLayoutTests
    {
        private const float Tolerance = 0.00001f;

        [Test]
        public void PlacesOriginAtWorldOrigin()
        {
            AssertVector(HexWorldLayout.ToWorld(new HexCoordinates(0, 0)), 0f, 0f, 0f);
        }

        [Test]
        public void PlacesQNeighborUsingPointyTopSpacing()
        {
            AssertVector(HexWorldLayout.ToWorld(new HexCoordinates(1, 0)), Mathf.Sqrt(3f), 0f, 0f);
        }

        [Test]
        public void PlacesRNeighborUsingPointyTopSpacing()
        {
            AssertVector(HexWorldLayout.ToWorld(new HexCoordinates(0, 1)), Mathf.Sqrt(3f) * 0.5f, 0f, 1.5f);
        }

        [Test]
        public void ScalesWorldPositionWithOuterRadius()
        {
            AssertVector(HexWorldLayout.ToWorld(new HexCoordinates(2, -1), 2f), 3f * Mathf.Sqrt(3f), 0f, -3f);
        }

        [Test]
        public void RejectsNonPositiveOuterRadius()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HexWorldLayout.ToWorld(new HexCoordinates(0, 0), 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => HexWorldLayout.ToWorld(new HexCoordinates(0, 0), -1f));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(2, -1)]
        [TestCase(-3, 2)]
        public void RoundTripsCoordinatesThroughWorldPosition(int q, int r)
        {
            var coordinates = new HexCoordinates(q, r);
            Assert.That(HexWorldLayout.FromWorld(HexWorldLayout.ToWorld(coordinates)), Is.EqualTo(coordinates));
        }

        [Test]
        public void FindsCellContainingNearbyWorldPosition()
        {
            var center = HexWorldLayout.ToWorld(new HexCoordinates(2, -1), 2f);
            var nearbyPosition = center + new Vector3(0.1f, 8f, -0.1f);
            Assert.That(HexWorldLayout.FromWorld(nearbyPosition, 2f), Is.EqualTo(new HexCoordinates(2, -1)));
        }

        [Test]
        public void RejectsNonPositiveRadiusWhenConvertingFromWorld()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HexWorldLayout.FromWorld(Vector3.zero, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => HexWorldLayout.FromWorld(Vector3.zero, -1f));
        }

        private static void AssertVector(Vector3 actual, float x, float y, float z)
        {
            Assert.That(actual.x, Is.EqualTo(x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(y).Within(Tolerance));
            Assert.That(actual.z, Is.EqualTo(z).Within(Tolerance));
        }
    }
}
