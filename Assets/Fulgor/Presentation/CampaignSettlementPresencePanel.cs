using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignSettlementController
    {
        private RectTransform presenceRows;
        private readonly Button[] presenceHeroButtons = new Button[2], presenceTroopButtons = new Button[2];
        private readonly RawImage[] presenceHeroes = new RawImage[2];
        private readonly Text[] presenceTroops = new Text[2];

        public bool OpenCityForcesAt(HexCoordinates position, bool visitor, bool hero)
        {
            if (!Ready || !Registry.TryGet(position, out var city) || !CanManage(city.Id)) return false;
            if (visitor)
            {
                var state = campaign.CityPresence(city.Id);
                if (!state.HasVisitor || !campaign.SelectArmyForRoster(state.VisitorArmyId)) return false;
            }
            if (!OpenRecruitmentAt(position)) return false;
            GetComponent<CampaignRecruitmentView>().FocusRoster(visitor, hero);
            return true;
        }

        private void BuildPresenceRows()
        {
            panel.sizeDelta = new Vector2(380,326);
            details.rectTransform.sizeDelta = new Vector2(344,76);
            status.rectTransform.sizeDelta = new Vector2(344,50);
            LocateButton.gameObject.SetActive(false); MarchButton.gameObject.SetActive(false);
            var previousVisit=VisitButton.gameObject;previousVisit.SetActive(false);Destroy(previousVisit);
            VisitButton=CampaignHudArt.Button("Open city",panel,new Vector2(18,-274),new Vector2(344,36),"GESTIONAR CIUDAD",null);
            presenceRows = CampaignMenuUI.Rect("Presencia militar",panel,new Vector2(0,1),new Vector2(18,-140),new Vector2(344,124));
            for(int row=0;row<2;row++)
            {
                bool visitor = row==1;
                var portrait = CampaignHudArt.Button(visitor?"Seleccionar héroe visitante":"Seleccionar héroe de guarnición",presenceRows,
                    new Vector2(0,-row*64),new Vector2(62,56),"—",()=>OpenSelectedForces(visitor,true));
                presenceHeroButtons[row]=portrait;
                var image=CampaignHudArt.Stretch("Retrato",portrait.transform,5).gameObject.AddComponent<RawImage>();
                image.uvRect=new Rect(.25f,.38f,.43f,.58f);image.raycastTarget=false;presenceHeroes[row]=image;
                var troop=CampaignHudArt.Button(visitor?"Seleccionar tropas visitantes":"Seleccionar tropas de guarnición",presenceRows,
                    new Vector2(72,-row*64),new Vector2(272,56),"",()=>OpenSelectedForces(visitor,false));
                presenceTroopButtons[row]=troop;
                presenceTroops[row]=troop.GetComponentInChildren<Text>();presenceTroops[row].fontSize=16;
            }
        }

        private void OpenSelectedForces(bool visitor,bool hero)
        {
            if(selection.SelectedCell.HasValue)OpenCityForcesAt(selection.SelectedCell.Value.Coordinates,visitor,hero);
        }

        private void RefreshPresenceRows(CampaignSettlement selected)
        {
            if(presenceRows==null)return;
            bool known=selected!=null&&campaign.CanObserve(selected.Position);
            presenceRows.gameObject.SetActive(known);status.gameObject.SetActive(!known);
            if(!known)return;
            var state=campaign.CityPresence(selected.Id);
            bool own=selected.OwnerFactionId==EffectivePlayerFaction;
            for(int row=0;row<2;row++)
            {
                bool visitor=row==1;
                bool hero=visitor?state.HeroVisiting:state.HeroInGarrison;
                long troops=visitor?state.VisitorTroops:state.GarrisonTroops;
                var portrait=hero?Resources.Load<Texture2D>("KingdomSelection/hero-"+selected.OwnerFactionId):null;
                presenceHeroes[row].enabled=portrait!=null;presenceHeroes[row].texture=portrait;
                presenceHeroButtons[row].GetComponentInChildren<Text>().text=hero?"H":"—";
                presenceHeroButtons[row].interactable=own&&hero;
                presenceTroopButtons[row].interactable=own&&troops>0;
                presenceTroops[row].text=(visitor?"VISITANTE":"GUARNICIÓN")+"\n"+(troops>0?troops+" tropas":hero?"Héroe presente":"Sin tropas");
            }
        }
    }
}
