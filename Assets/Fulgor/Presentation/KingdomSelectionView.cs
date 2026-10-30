using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Fulgor.Core.Campaign;
namespace Fulgor.Presentation.Map
{
    [Serializable]public sealed class KingdomChoice {public string id,name,ruler,hero,lore,color;}
    [Serializable]public sealed class KingdomChoices {public KingdomChoice[] entries;}
    public sealed class KingdomSelectionView : MonoBehaviour
    {
        private GameObject root;private int index=2;private KingdomChoice[] entries;
        private RawImage ruler,hero,backdrop;private Text kingdom,rulerName,heroName,lore,counter,notice;private Image banner;private KingdomBannerGraphic heraldry;
        public bool IsOpen=>root!=null;public string SelectedId=>entries?[index].id;
        private CampaignLaunchMenu menu;private CanvasGroup presentation;private Coroutine transition;
        public void Open(CampaignLaunchMenu source)
        {
            if(IsOpen)return;menu=source;entries=JsonUtility.FromJson<KingdomChoices>(Resources.Load<TextAsset>("KingdomSelection/catalog").text).entries;
            root=CampaignMenuUI.Canvas("Choose your kingdom",transform,180);var bg=CampaignMenuUI.Full("Midnight",root.transform,new Color(.018f,.022f,.025f));var center=new Vector2(.5f,.5f);
            backdrop=Art("Atmosphere",bg,new(0,-20),new(1920,1080));backdrop.color=new Color(.8f,.76f,.66f,.75f);backdrop.uvRect=new Rect(0,0,1,1);
            CampaignMenuUI.Full("Veil",bg,new Color(.015f,.018f,.023f,.48f));
            Label("Title",bg,new(0,446),new(1600,90),"FULGOR MEDIEVAL",62,LegacyUI.Gold);
            Label("Subtitle",bg,new(0,372),new(1300,40),"E L I G E   T U   R E I N O",24,LegacyUI.Paper);
            ruler=Art("Ruler portrait",bg,new(-450,-5),new(535,710));hero=Art("Hero portrait",bg,new(10,-45),new(400,565));
            rulerName=Label("Ruler name",bg,new(-450,-365),new(520,45),"",27,LegacyUI.Gold);
            heroName=Label("Hero name",bg,new(10,-325),new(370,65),"",23,LegacyUI.Paper);
            Label("Ruler role",bg,new(-450,-405),new(480,28),"GOBERNANTE",15,LegacyUI.Paper);Label("Hero role",bg,new(10,-375),new(340,28),"HÉROE",15,LegacyUI.Gold);
            var pole=CampaignMenuUI.Rect("Banner pole",bg,center,new(185,120),new(5,430));CampaignMenuUI.Background(pole,LegacyUI.Gold);
            var cloth=CampaignMenuUI.Rect("Kingdom banner",bg,center,new(255,220),new(145,220));CampaignMenuUI.Background(cloth,Color.white);banner=cloth.GetComponent<Image>();LegacyUI.Panel(cloth);banner.color=Color.clear;
            var device=CampaignMenuUI.Rect("Banner device",cloth,center,Vector2.zero,new(145,220));heraldry=device.gameObject.AddComponent<KingdomBannerGraphic>();heraldry.raycastTarget=false;
            var panel=CampaignMenuUI.Rect("Chronicle panel",bg,center,new(610,-20),new(490,660));CampaignMenuUI.Background(panel,LegacyUI.Ink);LegacyUI.Panel(panel);Ornaments(panel);
            kingdom=Label("Title Kingdom",panel,new(0,245),new(440,100),"",32,LegacyUI.Gold);
            Label("Chronicle heading",panel,new(0,150),new(400,35),"—  CRÓNICAS DEL REINO  —",18,LegacyUI.Gold);
            lore=Label("Lore",panel,new(0,-5),new(400,265),"",24,LegacyUI.Paper);lore.alignment=TextAnchor.UpperLeft;lore.resizeTextForBestFit=true;lore.resizeTextMinSize=20;lore.resizeTextMaxSize=24;
            Label("Legends",panel,new(0,-240),new(400,100),"LEYENDAS\nDoce destinos de distintas épocas de la historia ibérica.",18,new Color(.67f,.64f,.54f));
            Arrow(bg,new(-835,125),true);Arrow(bg,new(-835,-125),false);
            counter=Label("Index",bg,new(-835,0),new(160,60),"",23,LegacyUI.Gold);
            var back=CampaignMenuUI.Button("Return",bg,center,new(-700,-475),new(270,48),"VOLVER",Close);back.targetGraphic.color=new Color(.12f,.085f,.04f);
            var choose=CampaignMenuUI.Button("Choose",bg,center,new(0,-475),new(400,48),"ELEGIR ESTE REINO",Choose);choose.targetGraphic.color=new Color(.12f,.085f,.04f);
            notice=Label("Selection status",bg,new(520,-465),new(720,70),"",18,LegacyUI.Paper);
            presentation=root.AddComponent<CanvasGroup>();Refresh();
        }
        private static Text Label(string name,Transform p,Vector2 pos,Vector2 size,string text,int font,Color color)=>CampaignMenuUI.Text(name,p,new(.5f,.5f),pos,size,text,font,color);
        private static RawImage Art(string name,Transform p,Vector2 pos,Vector2 size)
        {
            var rect=CampaignMenuUI.Rect(name,p,new(.5f,.5f),pos,size);var image=name.Contains("portrait")?rect.gameObject.AddComponent<KingdomPortraitGraphic>():rect.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            return image;
        }
        private void Arrow(Transform parent,Vector2 pos,bool up)
        {
            var button=CampaignMenuUI.Button(up?"Previous kingdom":"Next kingdom",parent,new(.5f,.5f),pos,new(120,100),up?"▲":"▼",()=>Cycle(up?-1:1));
            LegacyUI.Ornate(button);button.transform.Find("Legacy gold frame").GetComponent<RectTransform>().sizeDelta=new Vector2(175,120);
            button.GetComponentInChildren<Text>().font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");button.GetComponentInChildren<Text>().fontSize=32;
        }
        public void Cycle(int delta){index=(index+delta%entries.Length+entries.Length)%entries.Length;if(transition!=null)StopCoroutine(transition);Refresh();transition=StartCoroutine(Reveal());}
        private IEnumerator Reveal(){presentation.alpha=.32f;float t=0;while(t<.24f){t+=Time.unscaledDeltaTime;presentation.alpha=Mathf.SmoothStep(.32f,1,t/.24f);yield return null;}presentation.alpha=1;transition=null;}
        private void Refresh()
        {
            var e=entries[index];kingdom.text=e.name;rulerName.text=e.ruler;heroName.text=e.hero;lore.text=e.lore;counter.text=$"{index+1:00} / {entries.Length:00}";notice.text="";
            ruler.texture=Resources.Load<Texture2D>("KingdomSelection/ruler-"+e.id+"-v2")??Resources.Load<Texture2D>("KingdomSelection/ruler-"+e.id);backdrop.texture=Resources.Load<Texture2D>("KingdomSelection/background-"+e.id)??ruler.texture;Fit(backdrop);hero.texture=Resources.Load<Texture2D>("KingdomSelection/hero-"+e.id);
            var design=KingdomDesignCatalog.Find(e.id);
            var colorCode=design?.PrimaryColor??e.color;
            if(ColorUtility.TryParseHtmlString("#"+colorCode,out var color))heraldry.color=color;
            heraldry.SetKingdom(e.id);
            Fit(ruler);Fit(hero);
        }
        private static void Fit(RawImage image){if(image.texture==null)return;float a=(float)image.texture.width/image.texture.height;var rect=image.rectTransform.rect;float b=rect.width/rect.height;image.uvRect=a>b?new Rect((1-b/a)/2,0,b/a,1):new Rect(0,(1-a/b)/2,1,a/b);}
        private static void Ornaments(RectTransform panel)
        {
            var center=new Vector2(.5f,.5f);var gold=new Color(.67f,.49f,.22f,.8f);
            for(int side=-1;side<=1;side+=2)for(int top=-1;top<=1;top+=2)
            {
                var h=CampaignMenuUI.Rect("Engraved corner",panel,center,new(side*208,top*308),new(52,2));CampaignMenuUI.Background(h,gold);
                var v=CampaignMenuUI.Rect("Engraved corner",panel,center,new(side*234,top*282),new(2,52));CampaignMenuUI.Background(v,gold);
                var gem=CampaignMenuUI.Rect("Corner jewel",panel,center,new(side*234,top*308),new(8,8));gem.localRotation=Quaternion.Euler(0,0,45);CampaignMenuUI.Background(gem,gold);
            }
        }
        private void Choose()
        {
            PlayerPrefs.SetString("Fulgor.UI.SelectedKingdom",SelectedId);PlayerPrefs.Save();
            if(SelectedId=="castilla"){Close();menu.StartCampaign("castilla-conquest");}
            else notice.text=entries[index].name+" seleccionado · Su campaña llegará próximamente.";
        }
        public void Close(){if(root==null)return;if(transition!=null){StopCoroutine(transition);transition=null;}root.SetActive(false);Destroy(root);root=null;}
        private void Update(){if(!IsOpen)return;var k=Keyboard.current;if(k?.escapeKey.wasPressedThisFrame==true){Close();return;}if(k?.upArrowKey.wasPressedThisFrame==true)Cycle(-1);if(k?.downArrowKey.wasPressedThisFrame==true)Cycle(1);}
    }
}







