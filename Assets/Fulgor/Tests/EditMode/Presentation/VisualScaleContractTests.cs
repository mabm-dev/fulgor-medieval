using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class VisualScaleContractTests
    {
        private const float Tolerance = 0.00001f;

        [Test]
        public void MatchesPointyTopHexGeometry()
        {
            Assert.That(VisualScaleContract.HexWidth, Is.EqualTo(Mathf.Sqrt(3f)).Within(Tolerance));
            Assert.That(VisualScaleContract.HexDepth, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(VisualScaleContract.HexVerticalSpacing, Is.EqualTo(1.5f).Within(Tolerance));
        }

        [Test]
        public void MatchesWorldLayoutNeighborSpacing()
        {
            var origin = HexWorldLayout.ToWorld(new Fulgor.Core.Map.HexCoordinates(0, 0));
            var qNeighbor = HexWorldLayout.ToWorld(new Fulgor.Core.Map.HexCoordinates(1, 0));
            var rNeighbor = HexWorldLayout.ToWorld(new Fulgor.Core.Map.HexCoordinates(0, 1));
            Assert.That(qNeighbor.x - origin.x, Is.EqualTo(VisualScaleContract.HexHorizontalSpacing).Within(Tolerance));
            Assert.That(rNeighbor.z - origin.z, Is.EqualTo(VisualScaleContract.HexVerticalSpacing).Within(Tolerance));
        }

        [Test]
        public void KeepsPropsInsideInteractionArea()
        {
            Assert.That(VisualScaleContract.FitsInsideTile(1.2f), Is.True);
            Assert.That(VisualScaleContract.FitsInsideTile(1.5f), Is.False);
        }

        [Test]
        public void KeepsSettlementBelowHexWidth()
        {
            Assert.That(VisualScaleContract.SettlementMaximumFootprint, Is.LessThan(VisualScaleContract.HexWidth));
        }

        [Test]
        public void UsesBottomCenterOrigin()
        {
            Assert.That(VisualScaleContract.AssetPivot, Is.EqualTo(Vector3.zero));
        }
    }
}
