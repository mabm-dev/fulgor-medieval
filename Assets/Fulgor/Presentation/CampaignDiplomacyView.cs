using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignDiplomacyView : MonoBehaviour
    {
        private CampaignTurnController campaign;
        private GameObject root;
        private Button launcher;
        private string faction;
        public bool IsOpen=>root!=null;
        private void Start()
        {
            campaign=GetComponent<CampaignTurnController>();
            var canvas=CampaignMenuUI.Canvas("Diplomacy launcher",transform,145);
            launcher=CampaignMenuUI.Button("Diplomacy",canvas.transform,new Vector2(1,1),new Vector2(-190,-30),new Vector2(164,44),"DIPLOMACIA",Open);
            launcher.GetComponentInChildren<Text>().fontSize=14;
        }
        private void Update()
        {
            if(launcher==null)return;
            bool modal=GetComponent<CityManagementView>()?.IsOpen==true||GetComponent<BattleLabController>()?.IsOpen==true||GetComponent<CampaignPlaySession>()?.IsOpen==true;
            launcher.gameObject.SetActive(!modal);launcher.interactable=campaign.CanManageDiplomacy;
            int pending=campaign.Diplomacy?.Proposals.Count(p=>p.receiver==campaign.PlayerFactionId&&p.response==0)??0;
            launcher.GetComponentInChildren<Text>().text="DIPLOMACIA"+(pending>0?" ("+pending+")":"");
            if(IsOpen&&(!campaign.CanManageDiplomacy||modal))Close();
        }
        public void Close(){if(root==null)return;root.SetActive(false);Destroy(root);root=null;}
        public void Open()
        {
            if(!campaign.CanManageDiplomacy)return;
            faction=campaign.Diplomacy.Factions.Contains(faction)?faction:campaign.Diplomacy.Factions.FirstOrDefault();
            Draw();
        }
        private void Draw()
        {
            Close();root=CampaignMenuUI.Canvas("Diplomacy council",transform,190);
            var bg=CampaignMenuUI.Full("Council",root.transform,new Color(.045f,.035f,.025f,.99f));var center=new Vector2(.5f,.5f);
            CampaignMenuUI.Text("Title",bg,center,new(0,465),new(1400,60),"CONSEJO DIPLOMÁTICO",38,CampaignMenuUI.Gold);
            CampaignMenuUI.Button("Close",bg,new(1,1),new(-25,-25),new(170,45),"CERRAR",Close);
            int index=0;foreach(var id in campaign.Diplomacy.Factions)
            {
                string selected=id;
                CampaignMenuUI.Button("Faction "+id,bg,center,new(-630,350-index++*65),new(230,52),id.ToUpperInvariant(),()=>{faction=selected;Draw();});
            }
            if(faction==null){CampaignMenuUI.Text("Empty",bg,center,Vector2.zero,new(1000,90),"Este escenario no contiene otros reinos.",25,CampaignMenuUI.Paper);return;}
            var relation=campaign.Diplomacy.Relation(faction);
            CampaignMenuUI.Text("Relation",bg,center,new(100,365),new(1100,60),faction.ToUpperInvariant()+" · "+Name(relation.relation)+(relation.remaining>0?" · "+relation.remaining+" turnos":""),28,CampaignMenuUI.Gold);
            CampaignMenuUI.Text("Rules",bg,center,new(100,300),new(1130,60),"Pactos y comercio impiden hostilidades. En paz, disputa, conquista y misión las permiten.\nLas propuestas y los pagos se resuelven al terminar turno; pacto y comercio duran 5 turnos.",20,CampaignMenuUI.Paper);
            int col=0;foreach(var type in new[]{DiplomaticRelation.Peace,DiplomaticRelation.Pact,DiplomaticRelation.Trade,DiplomaticRelation.War})
            {
                var value=type;
                CampaignMenuUI.Button("Set "+type,bg,center,new(-330+col++*290,210),new(275,48),Name(type),()=>{campaign.SetDiplomaticRelation(faction,value,value==DiplomaticRelation.War?DiplomaticIntention.Conquest:DiplomaticIntention.Neutral);Draw();});
            }
            CampaignMenuUI.Text("Intent label",bg,center,new(100,145),new(1100,35),"INTENCIÓN · "+Intention(relation.intention),20,CampaignMenuUI.Gold);
            col=0;foreach(var intent in new[]{DiplomaticIntention.Neutral,DiplomaticIntention.Defense,DiplomaticIntention.Dispute,DiplomaticIntention.Conquest,DiplomaticIntention.Mission})
            {
                var value=intent;
                CampaignMenuUI.Button("Intent "+intent,bg,center,new(-350+col++*230,95),new(220,44),Intention(intent),()=>{campaign.SetDiplomaticRelation(faction,relation.relation,value);Draw();});
            }
            col=0;foreach(var type in new[]{DiplomaticRelation.Peace,DiplomaticRelation.Pact,DiplomaticRelation.Trade})
            {
                var value=type;
                var button=CampaignMenuUI.Button("Propose "+type,bg,center,new(-280+col++*390,15),new(375,55),type==DiplomaticRelation.Trade?"OFRECER 2 ORO POR 2 MADERA":"PROPONER "+Name(type),()=>{campaign.ProposeDiplomacy(faction,value);Draw();});
                button.interactable=campaign.Rivals.Factions.Contains(faction)&&!campaign.Diplomacy.Proposals.Any(p=>p.sender==campaign.PlayerFactionId&&p.receiver==faction&&p.type==value);
            }
            var proposals=campaign.Diplomacy.Proposals.Where(p=>p.sender==faction||p.receiver==faction).ToArray();
            int row=0;foreach(var proposal in proposals)
            {
                bool incoming=proposal.receiver==campaign.PlayerFactionId;string id=proposal.id;float y=-80-row++*90;
                string terms=Name(proposal.type)+" · "+(incoming?"Recibida":"Enviada")+" · Entrega: "+Stock(incoming?proposal.Demand:proposal.Offer)+" · Recibe: "+Stock(incoming?proposal.Offer:proposal.Demand);
                CampaignMenuUI.Text("Terms",bg,center,new(-60,y),new(980,70),terms+(proposal.response>0?proposal.response==1?" · Aceptar al cierre":" · Rechazar al cierre":""),19,CampaignMenuUI.Paper);
                if(incoming)
                {
                    CampaignMenuUI.Button("Accept",bg,center,new(500,y+18),new(160,35),"ACEPTAR",()=>{campaign.RespondDiplomacy(id,true);Draw();});
                    CampaignMenuUI.Button("Reject",bg,center,new(500,y-23),new(160,35),"RECHAZAR",()=>{campaign.RespondDiplomacy(id,false);Draw();});
                }
            }
            CampaignMenuUI.Text("Report",bg,center,new(100,-430),new(1140,75),campaign.Diplomacy.LastReport,20,CampaignMenuUI.Gold);
        }
        public static string Name(DiplomaticRelation relation)=>relation switch{DiplomaticRelation.Peace=>"PAZ",DiplomaticRelation.Pact=>"PACTO",DiplomaticRelation.Trade=>"COMERCIO",_=>"GUERRA"};
        private static string Intention(DiplomaticIntention value)=>value switch{DiplomaticIntention.Neutral=>"Neutral",DiplomaticIntention.Defense=>"Defensa",DiplomaticIntention.Dispute=>"Disputa",DiplomaticIntention.Conquest=>"Conquista",_=>"Misión"};
        private static string Stock(CampaignResources r)=>$"{r.Grain} grano, {r.Wood} madera, {r.Stone} piedra, {r.Gold} oro, {r.Labor} trabajo";
    }
}

