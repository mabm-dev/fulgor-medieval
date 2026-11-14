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
            bool hud=GetComponent<CampaignSelectionPanel>()?.HasCampaignHud==true;
            launcher.gameObject.SetActive(!modal&&(!hud||GetComponent<CampaignCityMapView>()?.IsOpen==true));
            // con el HUD de campaña solo aparece en la ciudad, a la izquierda del menú y a su misma altura
            if(hud){var rect=(RectTransform)launcher.transform;rect.anchoredPosition=new Vector2(-174,-8);rect.sizeDelta=new Vector2(156,48);}launcher.interactable=campaign.CanManageDiplomacy;
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
        // Ventana del consejo: marco de madera sobre el mapa atenuado. A la izquierda, los reinos con su relación; a la derecha, el reino
        // elegido con su estado, la relación y la intención vigentes resaltadas, las propuestas que se pueden enviar y las que están en curso.
        private const float WindowWidth=1360,WindowHeight=820,ListWidth=300;
        private void Draw()
        {
            Close();root=CampaignMenuUI.Canvas("Diplomacy council",transform,190);
            var bg=CampaignMenuUI.Full("Council",root.transform,new Color(.03f,.025f,.02f,.78f));
            var window=CampaignMenuUI.Rect("Ventana del consejo",bg,new Vector2(.5f,.5f),Vector2.zero,new Vector2(WindowWidth,WindowHeight));
            CampaignHudArt.Surface(window);
            Label("Title",window,new Vector2(40,-22),new Vector2(900,48),"CONSEJO DIPLOMÁTICO",32,CampaignMenuUI.Gold,true);
            CampaignHudArt.Button("Close",window,new Vector2(WindowWidth-200,-22),new Vector2(170,46),"CERRAR",Close);
            Rule(window,new Vector2(30,-82),WindowWidth-60);

            Label("Reinos",window,new Vector2(40,-98),new Vector2(ListWidth-20,28),"REINOS",17,CampaignMenuUI.Gold,true);
            int index=0;foreach(var id in campaign.Diplomacy.Factions)
            {
                string selected=id;var state=campaign.Diplomacy.Relation(id);
                var entry=CampaignHudArt.Button("Faction "+id,window,new Vector2(30,-134-index++*74),new Vector2(ListWidth,66),"",()=>{faction=selected;Draw();});
                Label("Nombre",entry.transform,new Vector2(18,-8),new Vector2(ListWidth-36,28),FactionName(id),19,CampaignMenuUI.Paper,true);
                Label("Estado",entry.transform,new Vector2(18,-36),new Vector2(ListWidth-36,22),Name(state.relation)+(state.remaining>0?" · "+state.remaining+" turnos":""),14,RelationColor(state.relation));
                Mark(entry,id==faction);
            }
            float left=ListWidth+60,width=WindowWidth-left-30;
            var divider=CampaignMenuUI.Rect("Separador de reinos",window,new Vector2(0,1),new Vector2(ListWidth+44,-98),new Vector2(1,WindowHeight-130));
            divider.gameObject.AddComponent<Image>().color=new Color(.67f,.51f,.23f,.45f);
            if(faction==null){Label("Empty",window,new Vector2(left,-300),new Vector2(width,90),"Este escenario no contiene otros reinos.",24,CampaignMenuUI.Paper);return;}
            var relation=campaign.Diplomacy.Relation(faction);
            Label("Relation",window,new Vector2(left,-98),new Vector2(width,44),FactionName(faction).ToUpperInvariant()+" · "+Name(relation.relation)+(relation.remaining>0?" · "+relation.remaining+" turnos":""),27,RelationColor(relation.relation),true);
            Label("Rules",window,new Vector2(left,-146),new Vector2(width,50),"Pactos y comercio impiden hostilidades. En paz, disputa, conquista y misión las permiten.\nLas propuestas y los pagos se resuelven al terminar turno; pacto y comercio duran 5 turnos.",15,new Color(.82f,.78f,.68f));

            Label("Relación título",window,new Vector2(left,-212),new Vector2(width,26),"RELACIÓN",16,CampaignMenuUI.Gold,true);
            float step=width/4;int col=0;foreach(var type in new[]{DiplomaticRelation.Peace,DiplomaticRelation.Pact,DiplomaticRelation.Trade,DiplomaticRelation.War})
            {
                var value=type;
                var button=CampaignHudArt.Button("Set "+type,window,new Vector2(left+col++*step,-242),new Vector2(step-10,50),Name(type),()=>{campaign.SetDiplomaticRelation(faction,value,value==DiplomaticRelation.War?DiplomaticIntention.Conquest:DiplomaticIntention.Neutral);Draw();});
                Mark(button,relation.relation==type);
            }
            Label("Intent label",window,new Vector2(left,-308),new Vector2(width,26),"INTENCIÓN · "+Intention(relation.intention),16,CampaignMenuUI.Gold,true);
            step=width/5;col=0;foreach(var intent in new[]{DiplomaticIntention.Neutral,DiplomaticIntention.Defense,DiplomaticIntention.Dispute,DiplomaticIntention.Conquest,DiplomaticIntention.Mission})
            {
                var value=intent;
                var button=CampaignHudArt.Button("Intent "+intent,window,new Vector2(left+col++*step,-338),new Vector2(step-10,46),Intention(intent).ToUpperInvariant(),()=>{campaign.SetDiplomaticRelation(faction,relation.relation,value);Draw();});
                button.GetComponentInChildren<Text>().fontSize=15;Mark(button,relation.intention==intent);
            }
            Label("Propuestas título",window,new Vector2(left,-400),new Vector2(width,26),"PROPONER",16,CampaignMenuUI.Gold,true);
            step=width/3;col=0;foreach(var type in new[]{DiplomaticRelation.Peace,DiplomaticRelation.Pact,DiplomaticRelation.Trade})
            {
                var value=type;
                var button=CampaignHudArt.Button("Propose "+type,window,new Vector2(left+col++*step,-430),new Vector2(step-10,54),type==DiplomaticRelation.Trade?"2 ORO POR 2 MADERA":"PROPONER "+Name(type),()=>{campaign.ProposeDiplomacy(faction,value);Draw();});
                button.GetComponentInChildren<Text>().fontSize=15;
                button.interactable=campaign.Rivals.Factions.Contains(faction)&&!campaign.Diplomacy.Proposals.Any(p=>p.sender==campaign.PlayerFactionId&&p.receiver==faction&&p.type==value);
            }
            Rule(window,new Vector2(left,-500),width);
            Label("En curso título",window,new Vector2(left,-510),new Vector2(width,26),"PROPUESTAS EN CURSO",16,CampaignMenuUI.Gold,true);
            var proposals=campaign.Diplomacy.Proposals.Where(p=>p.sender==faction||p.receiver==faction).ToArray();
            if(proposals.Length==0)Label("Sin propuestas",window,new Vector2(left,-542),new Vector2(width,28),"No hay propuestas pendientes con este reino.",15,new Color(.75f,.67f,.52f));
            int row=0;foreach(var proposal in proposals.Take(3))
            {
                bool incoming=proposal.receiver==campaign.PlayerFactionId;string id=proposal.id;float y=-542-row++*62;
                var strip=CampaignMenuUI.Rect("Propuesta",window,new Vector2(0,1),new Vector2(left,y),new Vector2(width,56));
                var band=strip.gameObject.AddComponent<Image>();band.color=new Color(.16f,.12f,.10f,.94f);band.raycastTarget=false;
                string terms=Name(proposal.type)+" · "+(incoming?"Recibida":"Enviada")+"\nEntregas: "+Stock(incoming?proposal.Demand:proposal.Offer)+" · Recibes: "+Stock(incoming?proposal.Offer:proposal.Demand);
                Label("Terms",strip,new Vector2(14,-5),new Vector2(width-(incoming?290:28),46),terms+(proposal.response>0?proposal.response==1?" · Aceptar al cierre":" · Rechazar al cierre":""),14,CampaignMenuUI.Paper);
                if(incoming)
                {
                    CampaignHudArt.Button("Accept",strip,new Vector2(width-270,-8),new Vector2(126,40),"ACEPTAR",()=>{campaign.RespondDiplomacy(id,true);Draw();}).GetComponentInChildren<Text>().fontSize=13;
                    CampaignHudArt.Button("Reject",strip,new Vector2(width-136,-8),new Vector2(126,40),"RECHAZAR",()=>{campaign.RespondDiplomacy(id,false);Draw();}).GetComponentInChildren<Text>().fontSize=13;
                }
            }
            Label("Report",window,new Vector2(left,-WindowHeight+62),new Vector2(width,44),campaign.Diplomacy.LastReport,15,CampaignMenuUI.Gold);
        }
        private static Text Label(string name,Transform parent,Vector2 position,Vector2 size,string value,int font,Color color,bool heading=false)
        {
            var text=CampaignMenuUI.Text(name,parent,new Vector2(0,1),position,size,value,font,color);text.alignment=TextAnchor.UpperLeft;
            if(heading)text.font=LegacyUI.Heading??text.font;
            return text;
        }
        private static void Rule(Transform parent,Vector2 position,float width)
        {
            var line=CampaignMenuUI.Rect("Separador",parent,new Vector2(0,1),position,new Vector2(width,1));
            var image=line.gameObject.AddComponent<Image>();image.color=new Color(.67f,.51f,.23f,.55f);image.raycastTarget=false;
        }
        // el estado vigente lleva el ribete de selección, como una formación elegida
        private static void Mark(Button button,bool active)
        {
            if(!active)return;
            var rim=CampaignHudArt.Stretch("Vigente",button.transform,4);rim.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
        }
        private static Color RelationColor(DiplomaticRelation relation)=>relation switch{
            DiplomaticRelation.War=>new Color(1f,.56f,.45f),DiplomaticRelation.Pact=>new Color(.76f,.93f,.67f),DiplomaticRelation.Trade=>new Color(.93f,.84f,.52f),_=>CampaignMenuUI.Paper};
        private static string FactionName(string id)=>string.IsNullOrEmpty(id)?"":char.ToUpperInvariant(id[0])+id.Substring(1);
        public static string Name(DiplomaticRelation relation)=>relation switch{DiplomaticRelation.Peace=>"PAZ",DiplomaticRelation.Pact=>"PACTO",DiplomaticRelation.Trade=>"COMERCIO",_=>"GUERRA"};
        private static string Intention(DiplomaticIntention value)=>value switch{DiplomaticIntention.Neutral=>"Neutral",DiplomaticIntention.Defense=>"Defensa",DiplomaticIntention.Dispute=>"Disputa",DiplomaticIntention.Conquest=>"Conquista",_=>"Misión"};
        private static string Stock(CampaignResources r)
        {
            var parts=new System.Collections.Generic.List<string>();
            if(r.Grain!=0)parts.Add(r.Grain+" grano");if(r.Wood!=0)parts.Add(r.Wood+" madera");if(r.Stone!=0)parts.Add(r.Stone+" piedra");
            if(r.Gold!=0)parts.Add(r.Gold+" oro");if(r.Labor!=0)parts.Add(r.Labor+" mano de obra");
            return parts.Count==0?"nada":string.Join(", ",parts);
        }
    }
}

