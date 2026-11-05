using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Reusable native UI slices from the approved artwork; no baked numbers or map pixels.</summary>
    internal static class CampaignHudArt
    {
        private static readonly Dictionary<string,Sprite> sprites=new();
        internal static Sprite Slice(string key,string path,Rect uv,Vector4 border=default)
        {
            if(sprites.TryGetValue(key,out var cached)&&cached!=null)return cached;
            var texture=Resources.Load<Texture2D>(path);if(texture==null)return null;
            var sprite=Sprite.Create(texture,new Rect(uv.x*texture.width,uv.y*texture.height,uv.width*texture.width,uv.height*texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            sprites[key]=sprite;return sprite;
        }
        internal static void Surface(RectTransform root,bool paper=false)
        {
            var clip=root.gameObject.AddComponent<CityPanelClip>();clip.Corner=12;
            root.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var bg=Stretch("Fondo",root,paper?22:0);var image=bg.gameObject.AddComponent<Image>();image.raycastTarget=false;
            image.sprite=paper?Slice("paper","CityMap/panel_reference",new Rect(.71f,.53f,.13f,.09f)):
                Slice("wood-card","CityMap/panel_3",new Rect(267f/2172,231f/724,528f/2172,265f/724),new Vector4(22,22,22,22));
            image.type=paper?Image.Type.Simple:Image.Type.Sliced;
            if(paper)
            {
                var frame=Stretch("Marco",root).gameObject.AddComponent<Image>();frame.raycastTarget=false;
                frame.sprite=Slice("paper-frame","CityMap/panel_reference",new Rect(.085f,.125f,.84f,.77f),new Vector4(66,66,66,66));
                frame.type=Image.Type.Sliced;frame.fillCenter=false;frame.pixelsPerUnitMultiplier=2.2f;
            }
        }
        internal static RectTransform Stretch(string name,Transform parent,float inset=0)
        {
            var rect=CampaignMenuUI.Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero);
            rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.one*inset;rect.offsetMax=-Vector2.one*inset;return rect;
        }
        internal static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string text,UnityEngine.Events.UnityAction action)
        {
            var rect=CampaignMenuUI.Rect(name,parent,new Vector2(0,1),position,size);Surface(rect);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<CityPanelClip>();
            button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=CampaignMenuUI.Text("Label",rect,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(22,10),text,17,CampaignMenuUI.Paper);
            label.font=LegacyUI.Heading??label.font;
            var highlight=Stretch("Resalte interior",rect,5);highlight.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;highlight.gameObject.SetActive(false);
            var hover=rect.gameObject.AddComponent<CampaignHudButtonFeedback>();hover.Configure(button,highlight.gameObject,label);
            if(action!=null)button.onClick.AddListener(action);return button;
        }
        internal static void Resource(Transform parent,int index,Vector2 position,float size)
        {
            if(index==4){FulgorIcons.Create("Mano de obra",parent,FulgorIcon.Labor,position,size);return;}
            // Source coordinates are normalized to the original 2172 x 724 reference.
            var source=new[]{new Rect(395,310,100,130),new Rect(762,320,140,113),new Rect(1178,322,129,109),new Rect(1607,322,130,107)}[index];
            var rect=CampaignMenuUI.Rect("Recurso "+index,parent,new Vector2(0,1),position,new Vector2(size,size));
            var image=rect.gameObject.AddComponent<KingdomPortraitGraphic>();image.texture=Resources.Load<Texture2D>("CityMap/resources_reference");
            image.uvRect=new Rect(source.x/2172,source.y/724,source.width/2172,source.height/724);image.raycastTarget=false;
        }
    }
    public sealed class CampaignHudButtonFeedback:MonoBehaviour,UnityEngine.EventSystems.IPointerEnterHandler,UnityEngine.EventSystems.IPointerExitHandler
    {
        private Button button;private GameObject highlight;private Text label;private bool hovered;
        internal void Configure(Button value,GameObject frame,Text text){button=value;highlight=frame;label=text;}
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData data){hovered=true;Refresh();}
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData data){hovered=false;Refresh();}
        private void Update()=>Refresh();
        private void OnDisable(){hovered=false;if(highlight!=null)highlight.SetActive(false);}
        private void Refresh(){if(button==null)return;highlight.SetActive(hovered&&button.interactable);label.color=button.interactable?CampaignMenuUI.Paper:new Color(.49f,.45f,.36f);}
    }
}
