using System.Text;
using Fulgor.Core.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CityManagementView
    {
        private Text cityNotice;
        public string CityNoticeText=>cityNotice!=null?cityNotice.text:"";

        private void RefreshCityNotice()
        {
            if(cityNotice==null)return;
            var finished=CityProjects.Get(campaign.LastCompletedProject(settlement.Id));
            if(finished!=null) {
                cityNotice.text="OBRA TERMINADA · "+finished.Name+" · Su actividad se incluye en el próximo cierre de turno.";
                cityNotice.color=new Color(.93f,.82f,.57f);
            } else {
                var development=campaign.CityState(settlement.Id);
                var pending=CityProjects.Get(development.PendingId);
                cityNotice.text=pending!=null
                    ?pending.Name+" en construcción · Finaliza al llegar al turno "+development.CompletionTurn
                    :"Los edificios terminados aportan recursos y pagan mantenimiento al cerrar cada turno.";
                cityNotice.color=new Color(.80f,.84f,.76f);
            }
        }

        private string CityOverviewText()
        {
            var development=campaign.CityState(settlement.Id);
            var text=new StringBuilder(settlement.Name+"\nControl de "+owner.OwnerName(settlement.OwnerFactionId));
            text.Append("\n\nViviendas: ").Append(development.Houses)
                .Append("\nEdificios construidos: ").Append(buildings.Count);
            var pending=CityProjects.Get(development.PendingId);
            text.Append("\n\n").Append(pending==null?"Sin obras en curso.":pending.Name+" en construcción · turno "+development.CompletionTurn);
            text.Append("\n\nACTIVIDAD DE LA CIUDAD\n").Append(ResourceLines(development.IncomePerTurn,"+"));
            text.Append("\n\nMANTENIMIENTO POR TURNO\n").Append(ResourceLines(development.UpkeepPerTurn,"−"));
            if(!canManage)text.Append("\n\nEsta ciudad no aporta a las arcas de tu reino.");
            text.Append("\n\nConstruye desde Edificios para ampliar la villa. Selecciona un edificio para consultar su actividad.");
            return text.ToString();
        }

        private string TreasuryText()
        {
            var state=campaign.State;
            var current=state.Resources;
            var projected=state.ProjectedResources;
            var text=new StringBuilder("RESERVAS DEL REINO\n");
            text.Append(ResourceLines(current,"",true));
            text.Append("\n\nPREVISIÓN AL TURNO ").Append(state.Turn+1).Append('\n');
            ProjectedLine(text,"Grano",current.Grain,projected.Grain);
            ProjectedLine(text,"Madera",current.Wood,projected.Wood);
            ProjectedLine(text,"Piedra",current.Stone,projected.Stone);
            ProjectedLine(text,"Oro",current.Gold,projected.Gold);
            ProjectedLine(text,"Mano de obra",current.Labor,projected.Labor);
            text.Append("\nINGRESOS POR TURNO\n").Append(ResourceLines(state.IncomePerTurn,"+"));
            text.Append("\n\nRentas del territorio\n").Append(ResourceLines(campaign.TerritoryIncome,"+"));
            text.Append("\n\nEdificios de ciudades propias\n").Append(ResourceLines(campaign.CityIncomePerTurn,"+"));
            text.Append("\n\nGASTOS DE MANTENIMIENTO\n").Append(ResourceLines(state.ExpensesPerTurn,"−"));
            if(campaign.LogisticsEnabled)text.Append("\n\nDe esos gastos, mantenimiento militar\n").Append(ResourceLines(campaign.ArmyUpkeepPerTurn,"−")).Append("\nLa falta de recursos o suministro reduce moral y movimiento militar.");
            if(!state.MaintenanceShortfall.Equals(default(CampaignResources)))
                text.Append("\n\nMANTENIMIENTO SIN CUBRIR\n").Append(ResourceLines(state.MaintenanceShortfall))
                    .Append("\nSe abona lo disponible. No se acumula deuda.");
            text.Append("\n\nLas obras se pagan una sola vez al iniciarlas. Su actividad comienza en el cierre siguiente a su finalización.");
            return text.ToString();
        }

        private static void ProjectedLine(StringBuilder text,string name,int current,int next)
        {
            int balance=next-current;
            text.Append(name).Append(' ').Append(next).Append(" (").Append(balance>=0?"+":"")
                .Append(balance).AppendLine(")");
        }

        private static string BuildingEffectText(string id)
        {
            var project=CityProjects.Get(id);
            return project!=null?"ACTIVO\n"+ProjectEffectText(project):
                "Edificio inicial de la villa. Sin mantenimiento adicional.";
        }

        private static string ProjectEffectText(CityProject project)=>project.Benefit+
            "\n\nINGRESO POR TURNO\n"+ResourceLines(project.IncomePerTurn,"+")+
            "\n\nMANTENIMIENTO\n"+ResourceLines(project.UpkeepPerTurn,"−")+
            "\n\nSe aplica desde el cierre siguiente a la finalización.";

        private static string ResourceLines(CampaignResources resources,string prefix="",bool includeZero=false)
        {
            var text=new StringBuilder();
            void Add(string name,int amount){if(includeZero||amount!=0){if(text.Length>0)text.Append('\n');text.Append(name).Append(' ').Append(prefix).Append(amount);}}
            Add("Grano",resources.Grain);Add("Madera",resources.Wood);Add("Piedra",resources.Stone);
            Add("Oro",resources.Gold);Add("Mano de obra",resources.Labor);
            return text.Length==0?"Ninguno.":text.ToString();
        }
    }
}
