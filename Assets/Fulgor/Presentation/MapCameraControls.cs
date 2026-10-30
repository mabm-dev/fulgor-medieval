using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    /// <summary>Shared campaign and tactical camera controls; never issues unit orders.</summary>
    public sealed class MapCameraControls : MonoBehaviour
    {
        private Action<Vector2> pan;
        private Func<bool> allowed;
        public static RectTransform Create(Transform parent,Vector2 anchor,Vector2 position,Action<Vector2> pan,Action<float> zoom,Action reset,Func<bool> allowed)
        {
            var root=CampaignMenuUI.Rect("Navegación de cámara",parent,anchor,position,new Vector2(560,88));
            CampaignMenuUI.Background(root,LegacyUI.Ink);LegacyUI.Panel(root);
            var controls=root.gameObject.AddComponent<MapCameraControls>();controls.pan=pan;controls.allowed=allowed;
            void Button(string name,float x,Action action,Vector2 direction)
            {
                var button=CampaignMenuUI.Button(name,root,new Vector2(0,1),new Vector2(x,-8),new Vector2(name=="CENTRAR"?128:50,40),name,()=>action());
                button.navigation=new Navigation{mode=Navigation.Mode.None};
                if(direction!=Vector2.zero){button.onClick.RemoveAllListeners();var hold=button.gameObject.AddComponent<CameraPanHold>();hold.Step=()=>{if(allowed())pan(direction*Time.unscaledDeltaTime);};}
            }
            Button("←",8,()=>{},Vector2.left);Button("↑",64,()=>{},Vector2.up);Button("↓",120,()=>{},Vector2.down);Button("→",176,()=>{},Vector2.right);
            Button("−",240,()=>{if(allowed())zoom(1);},Vector2.zero);Button("+",296,()=>{if(allowed())zoom(-1);},Vector2.zero);Button("CENTRAR",360,()=>{if(allowed())reset();},Vector2.zero);
            CampaignMenuUI.Text("Ayuda cámara",root,new Vector2(0,1),new Vector2(10,-54),new Vector2(540,26),"WASD / flechas · arrastrar para mirar · rueda: zoom",16,LegacyUI.Paper);
            return root;
        }
        public static Vector2 KeyboardDirection()
        {
            // Comparación de Unity, no '?.': tras cambiar de escena el objeto seleccionado puede estar destruido.
            var events=EventSystem.current;var selected=events!=null?events.currentSelectedGameObject:null;
            if(selected!=null&&selected.GetComponent<InputField>()!=null)return Vector2.zero;
            var k=Keyboard.current;if(k==null)return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)),1);
        }
    }
    public sealed class CameraPanHold : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public Action Step;private bool held;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)held=true;}
        public void OnPointerUp(PointerEventData e){held=false;}
        public void OnPointerExit(PointerEventData e){held=false;}
        private void Update(){if(held&&Application.isFocused)Step?.Invoke();}
        private void OnDisable(){held=false;}
        private void OnApplicationFocus(bool focused){if(!focused)held=false;}
    }
}
