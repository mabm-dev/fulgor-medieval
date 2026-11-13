using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public enum CampaignAge { Migrations, EarlyMiddleAges, Expansion, HighMiddleAges, LateMiddleAges }

    public sealed class CampaignTechnology
    {
        public string Id { get; }
        public string Name { get; }
        public CampaignAge Age { get; }
        public int Wisdom { get; }
        public CampaignResources Cost { get; }
        public CampaignResources Income { get; }
        public bool Optional { get; }
        public bool AdvancesAge { get; }
        public string Benefit { get; }
        public CampaignTechnology(string id,string name,CampaignAge age,int wisdom,CampaignResources cost,
            CampaignResources income,string benefit,bool advancesAge=false,bool optional=false)
        { Id=id;Name=name;Age=age;Wisdom=wisdom;Cost=cost;Income=income;Benefit=benefit;AdvancesAge=advancesAge;Optional=optional; }
    }

    public static class CampaignTechnologies
    {
        public static readonly string[] AgeNames={"Edad de las Migraciones","Alta Edad Media","Edad de la Expansión","Plena Edad Media","Baja Edad Media"};
        public static IReadOnlyList<CampaignTechnology> All { get; }=Array.AsReadOnly(new[] {
            Tech("cultivation","Cultivo organizado",0,4,10,new CampaignResources(2,0,0,0,0),"+2 grano por ciudad y turno."),
            Tech("tools","Herramientas de hierro",0,4,10,new CampaignResources(0,1,1,0,0),"+1 madera y +1 piedra por ciudad y turno. Desbloquea la cantera en colinas."),
            new CampaignTechnology("seed_selection","Selección de simientes",CampaignAge.Migrations,4,new CampaignResources(8,5,0,0,0),default,"+1 grano por huerto terminado y turno.",optional:true),
            Advance("settlement","Consolidación del reino",0,6,25),
            Tech("rotation","Rotación de cultivos",1,8,20,new CampaignResources(3,0,0,0,0),"+3 grano por ciudad y turno."),
            Tech("records","Escritura administrativa",1,8,20,new CampaignResources(0,0,0,2,0),"+2 oro por ciudad y turno. +1 sabiduría por turno."),
            Advance("frontier","Organización de la frontera",1,12,45),
            Tech("masonry","Cantería organizada",2,12,35,new CampaignResources(0,0,3,0,0),"+3 piedra por ciudad y turno."),
            Tech("routes","Red de intercambios",2,12,35,new CampaignResources(0,0,0,3,0),"+3 oro por ciudad y turno. +1 por puesto de intercambio."),
            Advance("charters","Fueros y organización urbana",2,18,70),
            Tech("guilds","Oficios y gremios",3,18,50,new CampaignResources(0,2,0,0,2),"+2 madera y +2 mano de obra por ciudad y turno."),
            Tech("studies","Estudios y traducciones",3,18,50,default,"+2 sabiduría por turno."),
            new CampaignTechnology("crossbow_craft","Oficio de ballestería",CampaignAge.HighMiddleAges,14,new CampaignResources(0,35,0,25,10),default,"Permite reclutar ballesteros de leva en Castilla.",optional:true),
            Advance("institutions","Instituciones del reino",3,24,100),
            Tech("irrigation","Mejora de regadíos",4,24,70,new CampaignResources(4,0,0,0,0),"+4 grano por ciudad y turno."),
            Tech("accounting","Contabilidad mercantil",4,24,70,new CampaignResources(0,0,0,5,0),"+5 oro por ciudad y turno.")
        });
        private static CampaignTechnology Tech(string id,string name,int age,int wisdom,int gold,CampaignResources income,string benefit) =>
            new(id,name,(CampaignAge)age,wisdom,new CampaignResources(0,gold,0,gold,5),income,benefit);
        private static CampaignTechnology Advance(string id,string name,int age,int wisdom,int gold) =>
            new(id,name,(CampaignAge)age,wisdom,new CampaignResources(0,gold,gold/2,gold,10),default,"Avanza a "+AgeNames[age+1]+".",true);
        public static CampaignTechnology Get(string id)=>All.FirstOrDefault(t=>t.Id==id);
    }

    [Serializable]
    public sealed class CampaignResearchData
    {
        public int age, progress;
        public string active;
        public string[] completed;
    }

    public sealed class CampaignResearch
    {
        private readonly HashSet<string> completed=new(StringComparer.Ordinal);
        public CampaignAge Age { get; private set; }
        public string Active { get; private set; }
        public int Progress { get; private set; }
        public bool Has(string id)=>completed.Contains(id);
        public int WisdomPerTurn(int cities)=>cities<=0?0:cities+(Has("records")?1:0)+(Has("studies")?2:0);
        public CampaignResources IncomePerCity
        {
            get { var total=default(CampaignResources);foreach(var id in completed)total=total.Add(CampaignTechnologies.Get(id).Income);return total; }
        }
        public string BlockReason(string id,int largestPopulation,int cities)
        {
            var tech=CampaignTechnologies.Get(id);
            if(tech==null)return "Investigación desconocida.";
            if(Has(id))return "Investigación completada.";
            if(Active!=null)return "Hay una investigación en curso.";
            if(cities<1)return "Necesitas una ciudad propia.";
            if(tech.Age!=Age && !(tech.Optional && tech.Age<Age))return "Requiere "+CampaignTechnologies.AgeNames[(int)tech.Age]+".";
            if(tech.AdvancesAge)
            {
                if(CampaignTechnologies.All.Any(t=>t.Age==Age&&!t.AdvancesAge&&!t.Optional&&!Has(t.Id)))return "Completa las dos investigaciones de esta edad.";
                int needed=new[]{10,12,15,20}[(int)Age];
                if(largestPopulation<needed)return "Necesitas una ciudad con "+needed+" habitantes.";
            }
            return null;
        }
        public bool TryStart(string id,int population,int cities)
        {
            if(BlockReason(id,population,cities)!=null)return false;
            Active=id;Progress=0;return true;
        }
        public bool Advance(int wisdom)
        {
            if(Active==null||wisdom<=0)return false;
            var tech=CampaignTechnologies.Get(Active);
            Progress+=Math.Min(wisdom,tech.Wisdom-Progress);
            if(Progress<tech.Wisdom)return false;
            completed.Add(Active);Active=null;Progress=0;
            if(tech.AdvancesAge)Age++;
            return true;
        }
        public CampaignResearchData Capture()=>new(){age=(int)Age,active=Active,progress=Progress,completed=CampaignTechnologies.All.Where(t=>Has(t.Id)).Select(t=>t.Id).ToArray()};
        public static CampaignResearch Restore(CampaignResearchData data)
        {
            if(data==null)return new CampaignResearch();
            if(data.age<0||data.age>4||data.completed==null)throw new ArgumentException("Progreso de investigación inválido.");
            var state=new CampaignResearch();
            // Replay in catalog order so forged saves cannot skip the transition requirements.
            foreach(var tech in CampaignTechnologies.All)
                if(data.completed.Contains(tech.Id))
                {
                    if(!state.TryStart(tech.Id,int.MaxValue,1))throw new ArgumentException("Requisitos de investigación inválidos.");
                    state.Advance(tech.Wisdom);
                }
            if(data.completed.Length!=state.completed.Count||(int)state.Age!=data.age)throw new ArgumentException("Edad o tecnología guardada inválida.");
            if(!string.IsNullOrEmpty(data.active))
            {
                if(!state.TryStart(data.active,int.MaxValue,1)||data.progress<0||data.progress>=CampaignTechnologies.Get(data.active).Wisdom)throw new ArgumentException("Investigación activa inválida.");
                state.Progress=data.progress;
            }
            else if(data.progress!=0)throw new ArgumentException("Progreso sin investigación activa.");
            return state;
        }
    }
}
