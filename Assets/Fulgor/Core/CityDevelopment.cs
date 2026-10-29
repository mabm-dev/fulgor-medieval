using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public sealed class CityProject
    {
        public string Id { get; }
        public string Name { get; }
        public CampaignResources Cost { get; }
        public int Turns { get; }
        public CampaignResources IncomePerTurn { get; }
        public CampaignResources UpkeepPerTurn { get; }
        public string Benefit { get; }
        public CityProject(string id,string name,CampaignResources cost,int turns,
            CampaignResources incomePerTurn=default,CampaignResources upkeepPerTurn=default,string benefit="")
        {Id=id;Name=name;Cost=cost;Turns=turns;IncomePerTurn=incomePerTurn;UpkeepPerTurn=upkeepPerTurn;Benefit=benefit;}
    }

    /// <summary>Construction costs and recurring effects. Only completed projects contribute.</summary>
    public static class CityProjects
    {
        public static IReadOnlyList<CityProject> All { get; }=Array.AsReadOnly(new[] {
            new CityProject("market","Mercado",new CampaignResources(0,30,10,15,10),2,
                new CampaignResources(0,0,0,8,0),new CampaignResources(2,0,0,0,0),"El comercio aporta oro a las arcas del reino."),
            new CityProject("well","Pozo",new CampaignResources(0,10,25,5,8),1,
                new CampaignResources(6,0,0,0,0),new CampaignResources(0,0,0,1,0),"El agua abastece los huertos y mejora la provisión de grano."),
            new CityProject("warehouse","Almacén",new CampaignResources(0,35,15,10,10),2,
                new CampaignResources(0,4,3,0,0),new CampaignResources(0,0,0,2,0),"La organización de los suministros permite aprovechar más madera y piedra."),
            House("house-3","Vivienda 3"),House("house-4","Vivienda 4"),
            House("house-5","Vivienda 5"),House("house-6","Vivienda 6"),
            House("house-7","Vivienda 7"),House("house-8","Vivienda 8")
        });
        private static CityProject House(string id,string name)=>new CityProject(id,name,
            new CampaignResources(0,20,10,5,6),1,new CampaignResources(0,0,0,0,3),
            new CampaignResources(1,0,0,0,0),"Las nuevas familias aportan mano de obra y necesitan grano para su sustento.");
        public static CityProject Get(string id)=>All.FirstOrDefault(p=>p.Id==id);
    }

    public sealed class CityDevelopment
    {
        private readonly string[] completed;
        public IReadOnlyList<string> Completed { get; }
        public string PendingId { get; }
        public int CompletionTurn { get; }
        public int Houses=>2+completed.Count(id=>id.StartsWith("house-",StringComparison.Ordinal));
        public CampaignResources IncomePerTurn=>SumEffects(false);
        public CampaignResources UpkeepPerTurn=>SumEffects(true);
        private CampaignResources SumEffects(bool upkeep)
        {
            var total=default(CampaignResources);
            foreach(var id in completed) {
                var project=CityProjects.Get(id);
                total=total.Add(upkeep?project.UpkeepPerTurn:project.IncomePerTurn);
            }
            return total;
        }
        public bool Has(string id)=>id=="fortress"||id=="house-1"||id=="house-2"||Array.IndexOf(completed,id)>=0;
        public CityDevelopment():this(Array.Empty<string>(),null,0){}
        private CityDevelopment(string[] built,string pending,int due)
        {completed=(string[])built.Clone();Completed=Array.AsReadOnly(completed);PendingId=pending;CompletionTurn=due;}
        public bool CanStart(string id)=>PendingId==null&&CityProjects.Get(id)!=null&&!Has(id);
        public bool TryStart(string id,int turn,CampaignResources resources,out CityDevelopment next,out CampaignResources remaining)
        {
            next=this;remaining=resources;
            if(turn<1||!CanStart(id))return false;
            var project=CityProjects.Get(id);
            if(!resources.TrySpend(project.Cost,out remaining))return false;
            next=new CityDevelopment(completed,id,checked(turn+project.Turns));return true;
        }
        public CityDevelopment Advance(int turn)
        {
            if(PendingId==null||turn<CompletionTurn)return this;
            return new CityDevelopment(completed.Concat(new[]{PendingId}).ToArray(),null,0);
        }
        public CityDevelopmentData Capture(string settlementId)=>new CityDevelopmentData {
            settlementId=settlementId,completed=(string[])completed.Clone(),pendingId=PendingId,completionTurn=CompletionTurn};
        public static CityDevelopment Restore(CityDevelopmentData data,int turn)
        {
            if(data==null||string.IsNullOrEmpty(data.settlementId)||data.completed==null||turn<1)
                throw new ArgumentException("Invalid city development.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in data.completed)
                if(CityProjects.Get(id)==null||!ids.Add(id))throw new ArgumentException("Invalid completed building.");
            string pending=string.IsNullOrEmpty(data.pendingId)?null:data.pendingId;
            if(pending==null?data.completionTurn!=0:
                CityProjects.Get(pending)==null||ids.Contains(pending)||data.completionTurn<=turn||
                (long)data.completionTurn-turn>CityProjects.Get(pending).Turns)
                throw new ArgumentException("Invalid construction order.");
            return new CityDevelopment(data.completed,pending,data.completionTurn);
        }
    }

    [Serializable]
    public sealed class CityDevelopmentData
    {
        public string settlementId;
        public string[] completed;
        public string pendingId;
        public int completionTurn;
    }
}
