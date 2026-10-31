using UnityEngine;
using UnityEngine.InputSystem;

namespace Fulgor.Presentation.Map
{
    public sealed class RawFortressOrbit : MonoBehaviour
    {
        public Vector3 focus = new Vector3(0, 5, 0);
        public float distance = 28;
        private float yaw = -35, pitch = 32;
        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * .2f;
                pitch = Mathf.Clamp(pitch - delta.y * .15f, 8, 80);
            }
            distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * .015f, 8, 65);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            transform.position = focus - transform.forward * distance;
        }
    }
}
