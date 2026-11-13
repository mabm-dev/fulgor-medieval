using System;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignSaveData
    {
        public int researchVersion;
        public CampaignResearchData research;
        public void IncludeResearch(CampaignResearch value)
        { researchVersion=1;research=value.Capture(); }
        public CampaignResearch RestoreResearch()
        {
            if(researchVersion==0)
            {
                // JsonUtility materializes absent/null nested records as empty objects.
                if(research!=null&&(research.age!=0||research.progress!=0||!string.IsNullOrEmpty(research.active)||research.completed?.Length>0))throw new ArgumentException("Progreso sin versión de investigación.");
                return new CampaignResearch();
            }
            if(researchVersion!=1||research==null)throw new ArgumentException("Formato de investigación inválido.");
            return CampaignResearch.Restore(research);
        }
    }
}
