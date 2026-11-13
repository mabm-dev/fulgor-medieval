using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignCityPresenceBadge : MonoBehaviour
    {
        private readonly RectTransform[] cells = new RectTransform[2];
        private readonly Button[] portraits = new Button[2], counters = new Button[2];
        private readonly RawImage[] heroes = new RawImage[2];
        private readonly Image[] troops = new Image[2];
        private readonly Text[] counts = new Text[2];
        private CampaignSettlementController towns;
        private HexCoordinates position;
        private bool controllable;
        private readonly bool[] heroHere = new bool[2];
        public RectTransform Rect => (RectTransform)transform;

        internal void Build(CampaignSettlementController controller,HexCoordinates at)
        {
            towns=controller;position=at;
            for(int row=0;row<2;row++)
            {
                int index=row;
                bool visitor=row==1;
                cells[row]=CampaignMenuUI.Rect(visitor?"Visitante":"Guarnición",transform,new Vector2(0,1),Vector2.zero,new Vector2(126,60));
                var role=CampaignMenuUI.Text("Rol",cells[row],new Vector2(0,1),Vector2.zero,new Vector2(126,18),visitor?"VISITANTE":"GUARNICIÓN",13,CampaignMenuUI.Paper);
                role.alignment=TextAnchor.MiddleCenter;
                portraits[row]=CampaignHudArt.Button("Retrato",cells[row],new Vector2(0,-20),new Vector2(42,40),"",()=>OpenForces(visitor,heroHere[index]));
                var image=CampaignHudArt.Stretch("Héroe",portraits[row].transform,4).gameObject.AddComponent<RawImage>();
                image.raycastTarget=false;image.uvRect=new Rect(.25f,.38f,.43f,.58f);heroes[row]=image;
                var soldier=CampaignHudArt.Stretch("Tropas",portraits[row].transform,4).gameObject.AddComponent<Image>();
                soldier.raycastTarget=false;soldier.preserveAspect=true;troops[row]=soldier;
                counters[row]=CampaignHudArt.Button("Efectivos",cells[row],new Vector2(46,-20),new Vector2(80,40),"",()=>OpenForces(visitor,false));
                counts[row]=counters[row].GetComponentInChildren<Text>();counts[row].fontSize=21;
            }
        }

        private void OpenForces(bool visitor,bool hero)
        {
            if(controllable)towns.OpenCityForcesAt(position,visitor,hero);
            else towns.OpenCityAt(position);
        }

        internal bool SetPresence(CampaignCityPresence state,string faction,bool own)
        {
            controllable=own;
            int column=0;
            for(int row=0;row<2;row++)
            {
                bool visitor=row==1;
                bool present=visitor?state.HasVisitor:state.HasGarrison;
                cells[row].gameObject.SetActive(present);
                if(!present)continue;
                cells[row].anchoredPosition=new Vector2(column++*136,0);
                heroHere[row]=visitor?state.HeroVisiting:state.HeroInGarrison;
                long count=visitor?state.VisitorTroops:state.GarrisonTroops;
                var representative=visitor?state.VisitorRepresentative:state.GarrisonRepresentative;
                var portrait=heroHere[row]?Resources.Load<Texture2D>("KingdomSelection/hero-"+faction):null;
                heroes[row].enabled=portrait!=null;heroes[row].texture=portrait;
                troops[row].enabled=portrait==null&&representative!=null;
                if(representative!=null)troops[row].sprite=CampaignRecruitmentView.StackPortrait(CampaignRecruitmentView.Profile(representative));
                counts[row].text=count.ToString();
                portraits[row].interactable=heroHere[row]||count>0;
                counters[row].interactable=count>0;
            }
            Rect.sizeDelta=new Vector2(Mathf.Max(126,column*136-10),60);
            return column>0;
        }
    }
}

