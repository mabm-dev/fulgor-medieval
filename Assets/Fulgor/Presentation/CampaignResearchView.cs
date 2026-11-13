using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignResearchView : MonoBehaviour
    {
        private GameObject root;
        private CampaignTurnController campaign;
        private string selected, notice = "";
        private CampaignTurnState observedState;
        private int observedRevision;
        private bool observedRival;
        public bool IsOpen => root != null;
        public string SelectedTechnology => selected;
        public Button ResearchButton { get; private set; }

        public void Open()
        {
            campaign = GetComponent<CampaignTurnController>();
            if(campaign == null || !campaign.ResearchEnabled || campaign.HasPendingBattle || campaign.IsRivalPhaseActive)return;
            selected = campaign.Research.Active ?? (CampaignTechnologies.All.FirstOrDefault(t => Visible(t) && t.Age == campaign.Research.Age && !campaign.Research.Has(t.Id)) ?? CampaignTechnologies.All.Last()).Id;
            notice = "";
            Draw();
        }
        public void Close()
        {
            if(root == null)return;
            root.SetActive(false);Destroy(root);root = null;
        }
        private void OnDisable() => Close();
        private void Update()
        {
            if(!IsOpen)return;
            if(campaign.HasPendingBattle){Close();return;}
            if(observedState != campaign.State || observedRevision != campaign.CityRevision || observedRival != campaign.IsRivalPhaseActive)Draw();
        }
        public void SelectTechnology(string id)
        {
            if(!IsOpen || !Visible(CampaignTechnologies.Get(id)))return;
            selected = id;notice = "";Draw();
        }
        private bool Visible(CampaignTechnology tech) => tech != null && (tech.Id != "crossbow_craft" || campaign.PlayerFactionId == "castilla");
        private Text Label(Transform parent,string name,Vector2 position,Vector2 size,string value,int font=20,Color? color=null)
        {
            var label=CampaignMenuUI.Text(name,parent,new Vector2(0,1),position,size,value,font,color??CampaignMenuUI.Paper);
            label.alignment=TextAnchor.MiddleLeft;return label;
        }
        private static readonly Color Completed = new(.61f,.83f,.56f), Locked = new(.76f,.73f,.66f), Running = new(.55f,.80f,1f);
        private Vector2 Position(CampaignTechnology tech)
        {
            int row=tech.AdvancesAge?3:tech.Optional?2:CampaignTechnologies.All.Where(t=>t.Age==tech.Age&&!t.AdvancesAge&&!t.Optional).ToList().IndexOf(tech);
            return new Vector2(26+(int)tech.Age*364,-210-row*126);
        }
        private Color StateColor(CampaignTechnology tech)
        {
            if(campaign.Research.Has(tech.Id))return Completed;
            if(campaign.Research.Active==tech.Id)return Running;
            return campaign.ResearchBlockReason(tech.Id)==null?CampaignMenuUI.Gold:Locked;
        }
        private void Segment(Transform parent, Vector2 a,Vector2 b,Color color)
        {
            var rect=CampaignMenuUI.Rect("Conexión",parent,new Vector2(0,1),(a+b)/2,Vector2.zero);
            rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(Vector2.Distance(a,b),3);
            rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
        }
        private void Connect(Transform parent,CampaignTechnology from,CampaignTechnology to)
        {
            var a=Position(from)+new Vector2(328,-49);
            var b=Position(to)+new Vector2(0,-49);
            var color=campaign.Research.Has(from.Id)?Completed:new Color(.31f,.27f,.18f);
            if(from.Age==to.Age)
            {
                var right=new Vector2(a.x+10,a.y);
                var end=new Vector2(right.x,b.y);
                Segment(parent,a,right,color);Segment(parent,right,end,color);Segment(parent,end,new Vector2(b.x+328,b.y),color);
            }
            else
            {
                var elbow=new Vector2(b.x-12,a.y);
                var end=new Vector2(elbow.x,b.y);
                Segment(parent,a,elbow,color);Segment(parent,elbow,end,color);Segment(parent,end,b,color);
            }
        }
        private void Draw()
        {
            Close();
            observedState=campaign.State;observedRevision=campaign.CityRevision;observedRival=campaign.IsRivalPhaseActive;
            root=CampaignMenuUI.Canvas("Edades e investigación",transform,140);
            root.GetComponent<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var screen=CampaignMenuUI.Full("Fondo de ciencia",root.transform,new Color(.035f,.026f,.018f,1));
            var body=CampaignMenuUI.Rect("Árbol de ciencia",screen,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1880,1040));
            Label(body,"Title",new Vector2(26,-10),new Vector2(360,52),"CIENCIA",36,CampaignMenuUI.Gold);
            var active=CampaignTechnologies.Get(campaign.Research.Active);
            int rate=campaign.WisdomPerTurn;
            Label(body,"Estado del reino",new Vector2(26,-66),new Vector2(1810,38),
                CampaignTechnologies.AgeNames[(int)campaign.Research.Age]+"    ·    "+rate+" sabiduría / turno    ·    "+campaign.ResearchKnowledgeCities+" casas de conocimiento    ·    "+campaign.LargestResearchCity+" habitantes en la mayor ciudad",21);
            Label(body,"Progreso",new Vector2(440,-14),new Vector2(1390,44),active==null?"Elige una tecnología en el árbol.":
                active.Name+" · "+campaign.Research.Progress+" / "+active.Wisdom+" sabiduría · "+Turns(active.Wisdom-campaign.Research.Progress,rate),22,active==null?CampaignMenuUI.Paper:Running);
            var technologies=CampaignTechnologies.All.Where(Visible).ToArray();
            // Connections sit behind all nodes and follow the same age gates as the research rules.
            foreach(var tech in technologies)
            {
                if(tech.AdvancesAge)
                    foreach(var parent in technologies.Where(t=>t.Age==tech.Age&&!t.AdvancesAge&&!t.Optional))Connect(body,parent,tech);
                if((int)tech.Age>0&&!tech.AdvancesAge)
                {
                    var parent=technologies.First(t=>t.AdvancesAge&&(int)t.Age==(int)tech.Age-1);
                    Connect(body,parent,tech);
                }
            }
            for(int i=0;i<5;i++)
            {
                var header=CampaignMenuUI.Rect("Edad "+i,body,new Vector2(0,1),new Vector2(26+i*364,-132),new Vector2(328,58));
                CampaignHudArt.Surface(header);
                var title=Label(header,"Edad",new Vector2(12,-4),new Vector2(304,50),CampaignTechnologies.AgeNames[i],22,
                    (int)campaign.Research.Age==i?CampaignMenuUI.Gold:CampaignMenuUI.Paper);
                title.alignment=TextAnchor.MiddleCenter;
            }
            foreach(var tech in technologies)Node(body,tech);
            var detail=CampaignMenuUI.Rect("Detalle de ciencia",body,new Vector2(0,1),new Vector2(26,-742),new Vector2(1784,216));
            CampaignHudArt.Surface(detail);
            Detail(detail,CampaignTechnologies.Get(selected),rate);
            CampaignHudArt.Button("Volver al mapa",body,new Vector2(26,-982),new Vector2(220,44),"VOLVER",Close);
            Label(body,"Estados",new Vector2(285,-984),new Vector2(1500,40),"Completada · verde     En curso · azul     Disponible · dorado     Bloqueada · gris",18);
        }
        private string Turns(int wisdom,int rate) => rate>0?Mathf.CeilToInt(wisdom/(float)rate)+" turnos":"sin producción de sabiduría";
        private void Node(Transform body,CampaignTechnology tech)
        {
            var button=CampaignHudArt.Button("Tecnología "+tech.Id,body,Position(tech),new Vector2(328,98),"",()=>SelectTechnology(tech.Id));
            Label(button.transform,"Nombre",new Vector2(16,-5),new Vector2(296,48),tech.Name,20,StateColor(tech));
            string state=campaign.Research.Has(tech.Id)?"Completada":campaign.Research.Active==tech.Id?"En curso":
                campaign.ResearchBlockReason(tech.Id)==null?"Disponible":tech.Age>campaign.Research.Age?"Bloqueada por edad":"Revisa los requisitos";
            Label(button.transform,"Estado",new Vector2(16,-55),new Vector2(296,32),
                (tech.AdvancesAge?"ASCENSO · ":tech.Optional?"OPCIONAL · ":"")+state,16,StateColor(tech));
            if(tech.Id==selected)
            {
                var frame=CampaignHudArt.Stretch("Selección",button.transform,5);
                frame.gameObject.AddComponent<CitySelectionFrame>().raycastTarget=false;
            }
        }
        private void Detail(Transform parent,CampaignTechnology tech,int rate)
        {
            if(tech==null)return;
            Label(parent,"Title",new Vector2(22,-8),new Vector2(1290,36),tech.Name+(tech.Optional?" · OPCIONAL":""),26,StateColor(tech));
            Label(parent,"Efecto",new Vector2(22,-51),new Vector2(1270,34),tech.Benefit+
                (tech.Id=="frontier"&&campaign.PlayerFactionId=="castilla"?" Permite reclutar jinetes de leva.":""),21);
            var amounts=new[]{tech.Cost.Grain,tech.Cost.Wood,tech.Cost.Stone,tech.Cost.Gold,tech.Cost.Labor};
            int slot=0;
            for(int i=0;i<5;i++)if(amounts[i]>0)
            {
                CampaignHudArt.Resource(parent,i,new Vector2(22+slot*125,-100),32);
                Label(parent,"Coste "+i,new Vector2(60+slot++*125,-99),new Vector2(78,34),amounts[i].ToString(),23);
            }
            Label(parent,"Esfuerzo",new Vector2(660,-99),new Vector2(600,34),tech.Wisdom+" sabiduría · "+Turns(tech.Wisdom,rate),21);
            string requirements=tech.AdvancesAge?"Requiere "+string.Join(" y ",CampaignTechnologies.All.Where(t=>t.Age==tech.Age&&!t.AdvancesAge&&!t.Optional).Select(t=>t.Name))+
                " y una ciudad de "+new[]{10,12,15,20}[(int)tech.Age]+" habitantes.":
                tech.Optional?"Opcional. Se puede investigar también después de pasar de edad.":"Requiere "+CampaignTechnologies.AgeNames[(int)tech.Age]+".";
            Label(parent,"Requisitos",new Vector2(22,-145),new Vector2(1270,58),requirements,18);
            string block=campaign.ResearchBlockReason(tech.Id);
            bool done=campaign.Research.Has(tech.Id),running=campaign.Research.Active==tech.Id;
            ResearchButton=CampaignHudArt.Button("Investigar "+tech.Id,parent,new Vector2(1350,-24),new Vector2(408,60),
                done?"COMPLETADA":running?"EN CURSO":"INVESTIGAR",()=>{
                    if(!campaign.TryStartResearch(tech.Id))notice=campaign.ResearchBlockReason(tech.Id);
                    Draw();
                });
            ResearchButton.interactable=block==null;
            Label(parent,"Disponibilidad",new Vector2(1350,-101),new Vector2(408,90),
                string.IsNullOrEmpty(notice)?block??"Disponible · pago al comenzar.":notice,18,done?Completed:CampaignMenuUI.Gold);
        }
    }
}
