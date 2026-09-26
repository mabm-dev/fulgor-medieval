using UnityEngine;
using UnityEngine.InputSystem;

namespace Fulgor.Presentation.Map
{
    [RequireComponent(typeof(Camera))]
    public sealed class CampaignCameraController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float panSpeed = 12f;
        [SerializeField, Min(0.1f)] private float zoomSpeed = 2.5f;
        [SerializeField, Range(0f, 64f)] private float edgeSize = 14f;

        private Camera sceneCamera;
        private CameraNavigationState navigation;
        private Vector3 cameraOffset;
        private CampaignBoardSelection boardSelection;

        public void Initialize(
            float minimumX,
            float maximumX,
            float minimumZ,
            float maximumZ,
            Vector3 initialFocus,
            float initialZoom,
            Vector3 offset,
            CampaignBoardSelection selectionOwner = null)
        {
            sceneCamera = GetComponent<Camera>();
            cameraOffset = offset;
            boardSelection = selectionOwner;
            navigation = new CameraNavigationState(
                minimumX,
                maximumX,
                minimumZ,
                maximumZ,
                initialFocus,
                initialZoom,
                sceneCamera.aspect,
                4f,
                initialZoom);
            ApplyState();
        }

        private void Update()
        {
            if (navigation == null || !Application.isFocused) return;
            navigation.SetAspect(sceneCamera.aspect);

            var mouse = Mouse.current;
            var pointer = mouse == null ? Vector2.zero : mouse.position.ReadValue();
            bool allowPointer = mouse != null && sceneCamera.pixelRect.Contains(pointer) &&
                (boardSelection == null || !boardSelection.BlocksPointer(pointer));
            var direction = ReadKeyboardDirection() + (allowPointer
                ? MapPointerPolicy.EdgeDirection(pointer, sceneCamera.pixelRect, edgeSize) : Vector2.zero);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            var scaledSpeed = panSpeed * (navigation.Zoom / 10f) * Time.unscaledDeltaTime;
            navigation.Pan(direction * scaledSpeed);

            var scroll = allowPointer ? mouse.scroll.ReadValue().y : 0f;
            if (!Mathf.Approximately(scroll, 0f))
                navigation.ChangeZoom(-Mathf.Sign(scroll) * zoomSpeed);

            ApplyState();
        }

        private static Vector2 ReadKeyboardDirection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            var horizontal = 0f;
            var vertical = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            return new Vector2(horizontal, vertical);
        }

        private void ApplyState()
        {
            sceneCamera.orthographicSize = navigation.Zoom;
            transform.position = navigation.Focus + cameraOffset;
            transform.LookAt(navigation.Focus);
        }
    }
}
