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
        private Vector3 initialFocus;private float initialZoom;
        private bool dragging,dragMoved;private Vector2 dragStart,previousPointer;
        private CameraNavigationState navigation;
        private Vector3 cameraOffset;
        private CampaignBoardSelection boardSelection;
        public const float FieldOfView = 35f;

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
            this.initialFocus=initialFocus;this.initialZoom=initialZoom;
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
                2.5f,
                initialZoom);
            ApplyState();
        }

        public void FocusOnWorld(Vector3 position)
        {
            if (navigation == null) return;
            navigation.SetAspect(sceneCamera.aspect);
            navigation.ChangeZoom(Mathf.Min(navigation.Zoom, 4.5f) - navigation.Zoom);
            navigation.Pan(new Vector2(position.x - navigation.Focus.x, position.z - navigation.Focus.z));
            ApplyState();
        }

        private void Update()
        {
            if (!CanNavigate){dragging=false;return;}
            navigation.SetAspect(sceneCamera.aspect);

            var mouse = Mouse.current;
            var pointer = mouse == null ? Vector2.zero : mouse.position.ReadValue();
            bool pointerInView = mouse != null && sceneCamera.pixelRect.Contains(pointer);
            bool allowPointer = pointerInView &&
                (boardSelection == null || !boardSelection.BlocksPointer(pointer));
            if(mouse!=null)
            {
                if(allowPointer&&(mouse.leftButton.wasPressedThisFrame||mouse.middleButton.wasPressedThisFrame)){dragging=true;dragMoved=false;dragStart=previousPointer=pointer;}
                if(dragging&&(mouse.leftButton.isPressed||mouse.middleButton.isPressed))
                {
                    if(!dragMoved&&(pointer-dragStart).sqrMagnitude>=64){dragMoved=true;previousPointer=dragStart;}
                    if(dragMoved)DragView(pointer-previousPointer);previousPointer=pointer;
                }
                else dragging=false;
            }
            // La barra de recursos toca el borde superior: la franja de desplazamiento sigue
            // activa sobre el HUD, como en otros 4X; encima de los paneles solo se bloquea el zoom.
            var direction = MapCameraControls.KeyboardDirection() + (pointerInView&&!dragging
                ? MapPointerPolicy.EdgeDirection(pointer, sceneCamera.pixelRect, edgeSize) : Vector2.zero);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            var scaledSpeed = panSpeed * (navigation.Zoom / 10f) * Time.unscaledDeltaTime;
            navigation.Pan(direction * scaledSpeed);

            var scroll = allowPointer ? mouse.scroll.ReadValue().y : 0f;
            if (!Mathf.Approximately(scroll, 0f))
                navigation.ChangeZoom(-Mathf.Sign(scroll) * zoomSpeed);

            ApplyState();
        }

        public bool CanNavigate=>navigation!=null&&Application.isFocused&&(boardSelection==null||!boardSelection.IsInteractionSuspended);
        public void PanView(Vector2 delta){if(!CanNavigate)return;navigation.Pan(delta*panSpeed*(navigation.Zoom/10f));ApplyState();}
        public void ZoomView(float delta){if(!CanNavigate)return;navigation.ChangeZoom(delta*zoomSpeed);ApplyState();}
        public void ResetView(){if(!CanNavigate)return;navigation.ChangeZoom(initialZoom-navigation.Zoom);navigation.Pan(new Vector2(initialFocus.x-navigation.Focus.x,initialFocus.z-navigation.Focus.z));ApplyState();}
        public void DragView(Vector2 pixels)
        {
            if(!CanNavigate)return;
            float units=2*navigation.Zoom/Mathf.Max(1,sceneCamera.pixelHeight);
            navigation.Pan(new Vector2(-pixels.x*units,-pixels.y*units/Mathf.Max(.1f,Mathf.Abs(transform.forward.y))));ApplyState();
        }

        private void ApplyState()
        {
            // El zoom sigue siendo la mitad de la altura visible en el punto de foco; con perspectiva
            // la cámara se aleja o acerca por su dirección habitual hasta encuadrar esa altura.
            sceneCamera.orthographic = false;
            sceneCamera.fieldOfView = FieldOfView;
            sceneCamera.orthographicSize = navigation.Zoom;
            float distance = navigation.Zoom / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            sceneCamera.farClipPlane = Mathf.Max(sceneCamera.farClipPlane, distance * 2f + 100f);
            transform.position = navigation.Focus + cameraOffset.normalized * distance;
            transform.LookAt(navigation.Focus);
        }
    }
}
