using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CameraNavigationStateTests
    {
        [Test]
        public void ClampsInitialZoom()
        {
            var state = Create(initialZoom: 30f, maximumZoom: 20f);
            Assert.That(state.Zoom, Is.EqualTo(20f));
        }

        [Test]
        public void ClampsZoomAtBothEnds()
        {
            var state = Create(initialZoom: 10f);
            state.ChangeZoom(-100f);
            Assert.That(state.Zoom, Is.EqualTo(4f));
            state.ChangeZoom(100f);
            Assert.That(state.Zoom, Is.EqualTo(20f));
        }

        [Test]
        public void KeepsFocusCenteredWhenViewportContainsWholeBoard()
        {
            var state = Create(initialZoom: 20f);
            state.Pan(new Vector2(100f, 100f));
            Assert.That(state.Focus.x, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(state.Focus.z, Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void AllowsPanAfterZoomingIn()
        {
            var state = Create(initialZoom: 8f);
            state.Pan(new Vector2(4f, 3f));
            Assert.That(state.Focus.x, Is.GreaterThan(20f));
            Assert.That(state.Focus.z, Is.GreaterThan(10f));
        }

        [Test]
        public void ClampsPanToBoardBounds()
        {
            var state = Create(initialZoom: 4f);
            state.Pan(new Vector2(1000f, -1000f));
            Assert.That(state.Focus.x, Is.LessThan(40f));
            Assert.That(state.Focus.z, Is.GreaterThan(0f));
        }

        private static CameraNavigationState Create(float initialZoom, float maximumZoom = 20f)
        {
            return new CameraNavigationState(
                0f, 40f, 0f, 20f,
                new Vector3(20f, 0f, 10f),
                initialZoom, 16f / 9f, 4f, maximumZoom);
        }
    }
}
