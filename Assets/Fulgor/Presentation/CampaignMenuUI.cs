using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    internal static class CampaignMenuUI
    {
        internal static readonly Color Gold=new(.91f,.77f,.45f),Ink=new(.04f,.06f,.065f),Paper=new(.91f,.91f,.83f);
        internal static GameObject Canvas(string name,Transform parent,int order)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(parent,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            return root;
        }
        internal static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);var rect=(RectTransform)root.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;root.layer=5;return rect;
        }
        internal static void Background(RectTransform rect,Color color)
        {var image=rect.gameObject.AddComponent<Image>();image.color=color;}
        internal static RectTransform Full(string name,Transform parent,Color color)
        {
            var rect=Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero);rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;Background(rect,color);return rect;
        }
        internal static Text Text(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,string value,int fontSize,Color color)
        {
            var rect=Rect(name,parent,anchor,position,size);var text=rect.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(name.Contains("Title"))text.font=LegacyUI.Heading??text.font;
            text.text=value;text.fontSize=fontSize;text.color=color;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.alignment=TextAnchor.MiddleCenter;return text;
        }
        internal static Button Button(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,string value,UnityAction click)
        {
            var rect=Rect(name,parent,anchor,position,size);Background(rect,new Color(.25f,.28f,.19f));
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();var colors=button.colors;colors.disabledColor=new Color(.35f,.35f,.35f);button.colors=colors;
            var label=Text("Label",rect,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*12,value,20,Paper);label.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(click);LegacyUI.Button(button);return button;
        }
    }
}
