using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Piel de menús y paneles: madera oscura con filo de latón, pergamino con herrajes y
    /// botones de madera. Las texturas salen de ArtSource/UI/generar_estilo_madera_pergamino.py
    /// y viven en Resources/LegacyUI/Estilo; sin ellas se vuelve a los colores planos.
    /// </summary>
    internal static class LegacyUI
    {
        internal static readonly Color Gold=new(.83f,.69f,.34f),Paper=new(.94f,.89f,.75f),Ink=new(.035f,.042f,.05f,.97f);
        internal static readonly Color InkText=new(.12f,.075f,.035f),InkMuted=new(.33f,.24f,.15f),InkTitle=new(.24f,.10f,.05f);
        internal static Font Heading=>Resources.Load<Font>("LegacyUI/Cinzel");
        private const string Style="LegacyUI/Estilo/";
        private static readonly Dictionary<string,Sprite> sprites=new();
        internal static Sprite Sprite(string file){if(sprites.TryGetValue(file,out var s)&&s!=null)return s;s=Resources.Load<Sprite>(Style+file);sprites[file]=s;return s;}

        /// <summary>Panel de madera con filo de latón; admite texto claro encima.</summary>
        internal static void Panel(RectTransform root)
        {
            var image=root.GetComponent<Image>();if(image!=null)image.color=Ink;
            var border=Get<Outline>(root.gameObject);border.effectColor=new Color(.67f,.51f,.23f,.7f);border.effectDistance=new Vector2(1,-1);
            if(image!=null&&image.sprite==null&&Apply(image,"marco_madera",Image.Type.Sliced))border.enabled=false;
        }

        /// <summary>Pergamino con herrajes. Sus textos, también los que se añadan después, pasan a tinta.</summary>
        internal static void Parchment(RectTransform root)
        {
            var image=Get<Image>(root.gameObject);
            var outline=root.GetComponent<Outline>();
            if(Apply(image,"pergamino",Image.Type.Sliced)){if(outline!=null)outline.enabled=false;Get<ParchmentInk>(root.gameObject).Refresh();}
            else Panel(root);
        }

        /// <summary>Fondo de madera en mosaico para pantallas completas.</summary>
        internal static void WoodBackground(RectTransform root)
        {
            var image=Get<Image>(root.gameObject);
            if(Apply(image,"madera_fondo",Image.Type.Tiled))image.pixelsPerUnitMultiplier=1.5f;
        }

        /// <summary>Estandarte de tela azul con ribete dorado, para cabeceras.</summary>
        internal static void Banner(RectTransform root)
        {
            var image=Get<Image>(root.gameObject);Apply(image,"estandarte",Image.Type.Sliced);
        }

        /// <summary>Marco de sillares encima de una vista; el centro queda transparente.</summary>
        internal static RectTransform StoneFrame(RectTransform target,float margin)
        {
            var root=new GameObject("Stone frame",typeof(RectTransform));root.transform.SetParent(target,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=-Vector2.one*margin;rect.offsetMax=Vector2.one*margin;
            var image=root.AddComponent<Image>();image.raycastTarget=false;
            if(!Apply(image,"marco_piedra",Image.Type.Tiled))Object.Destroy(root);else image.fillCenter=false;
            return rect;
        }

        /// <summary>Disco de madera con aro de latón detrás de un icono.</summary>
        internal static Image Medallion(Transform parent,Vector2 position,float size)
        {
            var root=new GameObject("Medallion",typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(size,size);
            var image=root.AddComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;image.sprite=Sprite("medallon");image.enabled=image.sprite!=null;
            return image;
        }

        internal static void Button(Button button)
        {
            var image=button.GetComponent<Image>();if(image!=null)image.color=Color.white;
            var colors=button.colors;colors.normalColor=new Color(.105f,.09f,.065f);colors.highlightedColor=new Color(.34f,.26f,.11f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.5f,.36f,.13f);colors.disabledColor=new Color(.08f,.08f,.08f,.65f);colors.fadeDuration=.12f;button.colors=colors;
            var border=Get<Outline>(button.gameObject);border.effectColor=new Color(.72f,.55f,.25f,.7f);border.effectDistance=new Vector2(1,-1);
            var label=button.GetComponentInChildren<Text>();if(label!=null){label.font=Heading??label.font;label.color=Paper;label.fontSize=Mathf.Min(label.fontSize,18);}
            // Madera con filo de latón y cambio de imagen al pasar, pulsar y desactivar.
            if(image!=null&&Apply(image,"boton_madera",Image.Type.Sliced))
            {
                border.enabled=false;button.transition=Selectable.Transition.SpriteSwap;
                button.spriteState=new SpriteState{highlightedSprite=Sprite("boton_madera_activo"),selectedSprite=Sprite("boton_madera"),pressedSprite=Sprite("boton_madera_pulsado"),disabledSprite=Sprite("boton_madera_inactivo")};
                if(label!=null){var shadow=Get<Shadow>(label.gameObject);shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);}
            }
        }

        /// <summary>Pestaña elegida: madera clara; las demás, madera normal.</summary>
        internal static void SetSelected(Button button,bool selected)
        {
            var image=button.targetGraphic as Image;if(image==null)return;
            var sprite=Sprite(selected?"boton_madera_activo":"boton_madera");
            if(sprite!=null){image.sprite=sprite;image.color=Color.white;}
            else image.color=selected?new Color(.42f,.32f,.16f):new Color(.11f,.15f,.16f);
        }

        internal static void Skin(RectTransform root)
        {
            Parchment(root);foreach(var button in root.GetComponentsInChildren<Button>(true))Button(button);
            foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.gameObject.name.Contains("Title")){text.font=Heading??text.font;text.color=Gold;}
            var ink=root.GetComponent<ParchmentInk>();if(ink!=null)ink.Refresh(true);
        }
        internal static void Ornate(Button button)
        {
            button.GetComponent<Image>().color=Color.clear;var outline=button.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
            button.transition=Selectable.Transition.ColorTint;
            var root=new GameObject("Legacy gold frame",typeof(RectTransform),typeof(RawImage));root.transform.SetParent(button.transform,false);root.transform.SetAsFirstSibling();
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(390,156);
            var frame=root.GetComponent<RawImage>();frame.texture=Resources.Load<Texture2D>("LegacyUI/Frame");frame.raycastTarget=false;button.targetGraphic=frame;
            var colors=button.colors;colors.normalColor=new Color(1,1,1,.28f);colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(1,.8f,.4f);colors.disabledColor=new Color(.5f,.5f,.5f,.18f);button.colors=colors;
            var label=button.GetComponentInChildren<Text>();label.fontSize=24;label.color=Paper;var shadow=label.GetComponent<Shadow>();if(shadow!=null)shadow.enabled=false;
        }

        internal static T Get<T>(GameObject target) where T:Component{var item=target.GetComponent<T>();return item!=null?item:target.AddComponent<T>();}

        private static bool Apply(Image image,string file,Image.Type type)
        {
            var sprite=Sprite(file);if(sprite==null)return false;
            image.sprite=sprite;image.type=type;image.color=Color.white;return true;
        }
    }

    /// <summary>
    /// Pasa a tinta los textos claros de un pergamino una sola vez cada uno, incluidos los que
    /// se crean después. Los rótulos de botón (madera) conservan su color claro.
    /// </summary>
    internal sealed class ParchmentInk : MonoBehaviour
    {
        private readonly HashSet<Text> done=new();
        private bool dirty;
        // Los textos nuevos se tiñen al final del fotograma, cuando ya tienen su componente.
        private void OnTransformChildrenChanged()=>dirty=true;
        private void LateUpdate(){if(dirty)Refresh();}
        internal void Refresh(bool again=false)
        {
            if(again)done.Clear();dirty=false;
            foreach(var text in GetComponentsInChildren<Text>(true))
            {
                if(!done.Add(text)||text.GetComponentInParent<Button>(true)!=null)continue;
                var c=text.color;var light=.299f*c.r+.587f*c.g+.114f*c.b;if(light<.4f)continue;
                var warm=c.r-c.b>.2f;var title=text.gameObject.name.Contains("Title")||text.fontStyle==FontStyle.Bold||(warm&&text.fontSize>=18);
                text.color=new Color(title?LegacyUI.InkTitle.r:light>.75f?LegacyUI.InkText.r:LegacyUI.InkMuted.r,
                    title?LegacyUI.InkTitle.g:light>.75f?LegacyUI.InkText.g:LegacyUI.InkMuted.g,
                    title?LegacyUI.InkTitle.b:light>.75f?LegacyUI.InkText.b:LegacyUI.InkMuted.b,c.a);
                var shadow=text.GetComponent<Shadow>();if(shadow!=null)shadow.enabled=false;
            }
        }
    }
}
