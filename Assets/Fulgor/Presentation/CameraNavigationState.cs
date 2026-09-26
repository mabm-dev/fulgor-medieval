using System;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Estado y límites de cámara sin lectura directa de dispositivos.</summary>
    public sealed class CameraNavigationState
    {
        private readonly float minimumX;
        private readonly float maximumX;
        private readonly float minimumZ;
        private readonly float maximumZ;
        private readonly float minimumZoom;
        private readonly float maximumZoom;
        private float aspect;

        public CameraNavigationState(
            float minimumX,
            float maximumX,
            float minimumZ,
            float maximumZ,
            Vector3 initialFocus,
            float initialZoom,
            float aspect,
            float minimumZoom,
            float maximumZoom)
        {
            if (minimumX > maximumX || minimumZ > maximumZ)
                throw new ArgumentException("Los límites del tablero no son válidos.");
            if (minimumZoom <= 0f || minimumZoom > maximumZoom)
                throw new ArgumentException("Los límites de zoom no son válidos.");

            this.minimumX = minimumX;
            this.maximumX = maximumX;
            this.minimumZ = minimumZ;
            this.maximumZ = maximumZ;
            this.minimumZoom = minimumZoom;
            this.maximumZoom = maximumZoom;
            this.aspect = Mathf.Max(0.1f, aspect);
            Zoom = Mathf.Clamp(initialZoom, minimumZoom, maximumZoom);
            Focus = ClampFocus(initialFocus);
        }

        public Vector3 Focus { get; private set; }
        public float Zoom { get; private set; }

        public void SetAspect(float value)
        {
            aspect = Mathf.Max(0.1f, value);
            Focus = ClampFocus(Focus);
        }

        public void Pan(Vector2 worldDelta)
        {
            Focus = ClampFocus(Focus + new Vector3(worldDelta.x, 0f, worldDelta.y));
        }

        public void ChangeZoom(float delta)
        {
            Zoom = Mathf.Clamp(Zoom + delta, minimumZoom, maximumZoom);
            Focus = ClampFocus(Focus);
        }

        private Vector3 ClampFocus(Vector3 focus)
        {
            var halfVisibleX = Zoom * aspect * 0.72f;
            var halfVisibleZ = Zoom * 0.72f;
            return new Vector3(
                ClampAxis(focus.x, minimumX, maximumX, halfVisibleX),
                0f,
                ClampAxis(focus.z, minimumZ, maximumZ, halfVisibleZ));
        }

        private static float ClampAxis(float value, float minimum, float maximum, float halfVisible)
        {
            var low = minimum + halfVisible;
            var high = maximum - halfVisible;
            return low > high ? (minimum + maximum) * 0.5f : Mathf.Clamp(value, low, high);
        }
    }
}
