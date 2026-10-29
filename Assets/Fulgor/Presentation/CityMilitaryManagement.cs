using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public enum CityMilitaryMode { Recruit, Assign, Defend, Muster }
    public sealed partial class CityManagementView
    {
        private RectTransform militaryNav;
        private readonly List<Button> militaryTabs=new();
        private Text militarySelection;
        private int militaryItem;
        public CityMilitaryMode MilitaryMode {get;private set;}
        public void SelectMilitaryMode(CityMilitaryMode mode)
        {
            if(!Enum.IsDefined(typeof(CityMilitaryMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            MilitaryMode=mode;militaryItem=0;if(IsOpen)SelectPanel(CityPanelKind.Armies);
        }
        public void ChangeMilitaryItem(int delta){militaryItem+=delta;if(IsOpen)Refresh();}
        private void ConfigureMilitaryNavigation()
        {
            bool visible=Panel==CityPanelKind.Armies;
            if(visible&&militaryNav==null)
            {
                militaryNav=Fixed("Military navigation",heading.transform.parent,new Vector2(24,-105),new Vector2(282,96));
                string[] names={"RECLUTAR","DESTINAR","DEFENDER","NUEVA HUESTE"};
                for(int i=0;i<4;i++)
                {
                    var mode=(CityMilitaryMode)i;
                    militaryTabs.Add(CreateButton(Fixed("Military "+mode,militaryNav,new Vector2(i%2*144,-(i/2)*32),new Vector2(138,28)),names[i],()=>SelectMilitaryMode(mode)));
                }
                CreateButton(Fixed("Previous troop",militaryNav,new Vector2(0,-67),new Vector2(42,28)),"‹",()=>ChangeMilitaryItem(-1));
                CreateButton(Fixed("Next troop",militaryNav,new Vector2(240,-67),new Vector2(42,28)),"›",()=>ChangeMilitaryItem(1));
                militarySelection=Text("Military choice",militaryNav,new Vector2(47,-70),new Vector2(188,24),"",16,new Color(.93f,.82f,.57f));
                militarySelection.alignment=TextAnchor.MiddleCenter;
                foreach(var child in militaryNav.GetComponentsInChildren<Transform>(true))child.gameObject.layer=5;
            }
            if(militaryNav!=null)militaryNav.gameObject.SetActive(visible);
            if(scroll!=null)scroll.viewport.offsetMax=new Vector2(-24,visible?-215:-115);
            for(int i=0;i<militaryTabs.Count;i++)militaryTabs[i].GetComponent<Image>().color=i==(int)MilitaryMode?new Color(.47f,.37f,.18f):new Color(.26f,.27f,.20f);
        }
        private int Choice(int count)
        {if(count==0){militaryItem=0;return 0;}militaryItem=((militaryItem%count)+count)%count;return militaryItem;}
        private void RefreshMilitaryPanel()
        {
            heading.text="DEFENSA Y HUESTES";
            var guard=campaign.Military?.Garrison(settlement.Id);
            var job=campaign.Military?.Pending(settlement.Id);
            var text=new StringBuilder("GUARNICIÓN DE LA PLAZA\n");
            if(guard==null||guard.IsDissolved)text.AppendLine("Sin tropas de guarnición.");
            else foreach(var f in guard.Formations)text.AppendLine(FormationLine(f));
            text.AppendLine("\nDefensa: hasta 6 formaciones entre guarnición, hueste estacionada y plaza reservada de reclutamiento.");
            if(job!=null)text.Append("\nEN FORMACIÓN\n").Append(RecruitmentCatalog.Get(job.profile).Name).Append("\nFaltan ")
                .Append(Math.Max(0,job.completionTurn-campaign.State.Turn)).Append(" turnos · listo en turno ").Append(job.completionTurn).AppendLine(".\nCoste ya abonado.");
            CampaignArmyOnMap active=null;
            campaign.ArmyRegistry?.TryGetArmy(campaign.SelectedArmyId,out active);
            bool allowed=canManage&&campaign.ArmyRegistry!=null&&!campaign.HasPendingBattle&&!campaign.IsCampaignFinished;
            if(!canManage)text.AppendLine("\nInspección: las órdenes requieren controlar esta ciudad.");
            if(campaign.HasPendingBattle)text.AppendLine("\nResuelve la batalla antes de dar órdenes.");
            if(campaign.ArmyRegistry==null)text.AppendLine("\nEste escenario usa una hueste simple. La gestión militar requiere un escenario de varias huestes.");
            if(MilitaryMode==CityMilitaryMode.Recruit)
            {
                int index=Choice(RecruitmentCatalog.All.Count);var spec=RecruitmentCatalog.All[index];var cost=spec.Cost;
                militarySelection.text=$"PERFIL {index+1} / {RecruitmentCatalog.All.Count}";
                text.Append("\nRECLUTAR\n").Append(spec.Name).Append("\n").Append(spec.Quantity).Append(" soldados · ").Append(spec.Turns).AppendLine(" turnos")
                    .AppendLine(ResourceLines(cost)).AppendLine("\nAl terminar se incorporan a la guarnición. Una leva por ciudad; puede coexistir con una obra civil.");
                bool ready=allowed&&campaign.Military.TryRecruit(settlement.Id,campaign.PlayerFactionId,spec.Profile,campaign.State.Turn,campaign.State.Resources,campaign.ArmyRegistry,out _,out _);
                if(!campaign.State.Resources.TrySpend(cost,out _))text.Append("\nFALTAN RECURSOS\n").AppendLine(ResourceLines(campaign.State.Resources.MissingFor(cost)));
                else if(allowed&&job==null&&!ready)text.AppendLine("\nNo hay plaza de defensa libre o la casilla está ocupada por un rival.");
                Action(primary,"RECLUTAR · "+spec.Quantity+" SOLDADOS",()=>{campaign.TryRecruit(settlement.Id,spec.Profile);Refresh();});primary.interactable=ready;
                Action(secondary,"FIN DE TURNO",CompleteTurn);secondary.interactable=canManage&&campaignPanel.EndTurnButton.interactable;
            }
            else
            {
                var forms=MilitaryMode==CityMilitaryMode.Defend?active?.Army.Formations:guard?.Formations;
                forms??=Array.Empty<CampaignFormationState>();int index=Choice(forms.Count);var selected=forms.Count>0?forms[index]:null;
                militarySelection.text=forms.Count>0?$"FORMACIÓN {index+1} / {forms.Count}":"SIN FORMACIONES";
                if(selected!=null)text.Append("\nSELECCIONADA\n").AppendLine(FormationLine(selected));
                bool near=active!=null&&active.Movement.Position.DistanceTo(settlement.Position)<=1&&active.Army.FactionId==campaign.PlayerFactionId;
                bool ready=false;
                switch(MilitaryMode)
                {
                    case CityMilitaryMode.Assign:
                        text.AppendLine("\nDESTINAR A LA HUESTE\nTraslada la formación seleccionada de la guarnición a la hueste activa, situada aquí o en una casilla adyacente.");
                        ready=allowed&&selected!=null&&near&&campaign.Military.TryAssignToArmy(settlement.Id,active.Army.Id,selected.Id,campaign.ArmyRegistry,out _,out _);
                        Action(primary,"DESTINAR A HUESTE",()=>{campaign.TryAssignGarrison(settlement.Id,selected.Id);Refresh();});break;
                    case CityMilitaryMode.Defend:
                        text.AppendLine("\nDEJAR EN LA PLAZA\nTraslada la formación de la hueste activa a la guarnición. Si era la última, la hueste desaparece del mapa.");
                        ready=allowed&&selected!=null&&near&&campaign.Military.TryDeposit(settlement.Id,active.Army.Id,selected.Id,campaign.ArmyRegistry,out _,out _);
                        Action(primary,"DEJAR EN GUARNICIÓN",()=>{campaign.TryLeaveGarrison(settlement.Id,selected.Id);Refresh();});break;
                    default:
                        text.AppendLine("\nFORMAR NUEVA HUESTE\nSale de la guarnición con la formación seleccionada. La casilla de ciudad debe estar libre de huestes. Podrá moverse el próximo turno.");
                        ready=allowed&&selected!=null&&campaign.Military.TryMuster(settlement.Id,campaign.PlayerFactionId,selected.Id,campaign.ArmyRegistry,out _,out _,out _);
                        Action(primary,"FORMAR NUEVA HUESTE",()=>{campaign.TryMuster(settlement.Id,selected.Id);Refresh();});break;
                }
                primary.interactable=ready;
                text.Append("\nHUESTE ACTIVA\n").AppendLine(active==null?"Ninguna.":active.Army.Id+" · "+active.Movement.Position.Key+" · "+active.Army.Formations.Count+" / 6 formaciones");
                if(active!=null&&MilitaryMode!=CityMilitaryMode.Muster&&!near)text.AppendLine("Acerca la hueste a la ciudad para trasladar tropas.");
                text.AppendLine("\nSe trasladan formaciones completas, con sus heridas y moral. El traslado consume el movimiento restante de la hueste.");
                Action(secondary,"VER HUESTE EN EL MAPA",()=>{Close();campaign.LocateArmy();});secondary.interactable=active!=null;
            }
            text.AppendLine(campaign.LogisticsEnabled?"\nMantenimiento por formación: 1 grano cada 20 soldados (doble para caballería) y 1 oro cada 25. Descansar con suministro y pago permite recuperar heridas y moral. Las bajas se reponen reclutando.":"\nReglas provisionales: coste inicial, sin mantenimiento militar periódico en este escenario.");body.text=text.ToString();
        }
        private static string FormationLine(CampaignFormationState f)
        {
            string name=f.Kind==FormationKind.Cavalry?"Caballería":f.Kind==FormationKind.Ranged?"Tiradores":"Infantería";
            foreach(var spec in RecruitmentCatalog.All)
            {
                var a=TroopCatalog.Get(spec.Profile).Attributes;var b=f.Attributes;
                if(a.Attack==b.Attack&&a.Defense==b.Defense&&a.HealthPerMember==b.HealthPerMember&&a.Range==b.Range)
                {name=spec.Name;break;}
            }
            return $"{name} · {f.Quantity} soldados\nSalud {f.Health} · Moral {f.Morale}";
        }
    }
}
