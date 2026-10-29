using UnityEngine;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    internal static class LegacyUI
    {
        internal static readonly Color Gold=new(.83f,.69f,.34f),Paper=new(.94f,.89f,.75f),Ink=new(.035f,.042f,.05f,.97f);
        internal static Font Heading=>Resources.Load<Font>("LegacyUI/Cinzel");
        internal static void Panel(RectTransform root)
        {
            var image=root.GetComponent<Image>();if(image!=null)image.color=Ink;
            var border=root.GetComponent<Outline>()??root.gameObject.AddComponent<Outline>();border.effectColor=new Color(.67f,.51f,.23f,.7f);border.effectDistance=new Vector2(1,-1);
        }
        internal static void Button(Button button)
        {
            var image=button.GetComponent<Image>();if(image!=null)image.color=Color.white;
            var colors=button.colors;colors.normalColor=new Color(.105f,.09f,.065f);colors.highlightedColor=new Color(.34f,.26f,.11f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.5f,.36f,.13f);colors.disabledColor=new Color(.08f,.08f,.08f,.65f);colors.fadeDuration=.12f;button.colors=colors;
            var border=button.GetComponent<Outline>()??button.gameObject.AddComponent<Outline>();border.effectColor=new Color(.72f,.55f,.25f,.7f);border.effectDistance=new Vector2(1,-1);
            var label=button.GetComponentInChildren<Text>();if(label!=null){label.font=Heading??label.font;label.color=Paper;label.fontSize=Mathf.Min(label.fontSize,18);}
        }
        internal static void Skin(RectTransform root)
        {
            Panel(root);foreach(var button in root.GetComponentsInChildren<Button>(true))Button(button);
            foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.gameObject.name.Contains("Title")){text.font=Heading??text.font;text.color=Gold;}
        }
        internal static void Ornate(Button button)
        {
            button.GetComponent<Image>().color=Color.clear;var outline=button.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
            var root=new GameObject("Legacy gold frame",typeof(RectTransform),typeof(RawImage));root.transform.SetParent(button.transform,false);root.transform.SetAsFirstSibling();
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(390,156);
            var frame=root.GetComponent<RawImage>();frame.texture=Resources.Load<Texture2D>("LegacyUI/Frame");frame.raycastTarget=false;button.targetGraphic=frame;
            var colors=button.colors;colors.normalColor=new Color(1,1,1,.28f);colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(1,.8f,.4f);colors.disabledColor=new Color(.5f,.5f,.5f,.18f);button.colors=colors;
            var label=button.GetComponentInChildren<Text>();label.fontSize=24;label.color=Paper;
        }
    }
}
