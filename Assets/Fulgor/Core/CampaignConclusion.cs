using System;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public enum CampaignObjectiveKind { ReachDestination, Conquest }
    public enum CampaignConclusion { InProgress, Victory, Defeat }
    public static class CampaignConclusionRules
    {
        public static CampaignConclusion Evaluate(CampaignObjectiveKind objective,CampaignArmyRegistry armies,CampaignSettlementRegistry owners,string player,bool missionWon=false)
        {
            if(!Enum.IsDefined(typeof(CampaignObjectiveKind),objective))throw new ArgumentOutOfRangeException(nameof(objective));
            if(objective==CampaignObjectiveKind.ReachDestination)return missionWon?CampaignConclusion.Victory:CampaignConclusion.InProgress;
            if(armies==null||owners==null||string.IsNullOrWhiteSpace(player))throw new ArgumentException("Conquest requires an army registry, cities and player faction.");
            if(armies.HasActiveEncounters)return CampaignConclusion.InProgress;
            bool own=armies.Armies.Any(a=>a.Army.FactionId==player)||owners.Settlements.Any(c=>c.OwnerFactionId==player);
            if(!own)return CampaignConclusion.Defeat;
            bool rivals=armies.Armies.Any(a=>a.Army.FactionId!=player)||owners.Settlements.Any(c=>c.OwnerFactionId!=player);
            return rivals?CampaignConclusion.InProgress:CampaignConclusion.Victory;
        }
    }
    public sealed partial class CampaignSaveData
    {
        public CampaignObjectiveKind objectiveKind;
        public void IncludeCampaignRules(CampaignObjectiveKind objective)
        {
            if(version!=8||!Enum.IsDefined(typeof(CampaignObjectiveKind),objective))throw new ArgumentException("Invalid campaign rule state.");
            objectiveKind=objective;context=CampaignRulesSignature(context,objective);version=9;
        }
        public static string CampaignRulesSignature(string rivalContext,CampaignObjectiveKind objective)
        {
            if(string.IsNullOrEmpty(rivalContext)||!Enum.IsDefined(typeof(CampaignObjectiveKind),objective))throw new ArgumentException("Missing campaign rules identity.");
            ulong hash=14695981039346656037UL;
            foreach(char c in "campaign-v9:adjacent-retreat:surrender-if-trapped:presence-conquest:"+(int)objective+":"+rivalContext)
                hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
        public void RestoreCampaignRules(CampaignObjectiveKind expected)
        {
            if(version<9){if(expected!=CampaignObjectiveKind.ReachDestination)throw new ArgumentException("Conquest campaigns require v9 saves.");return;}
            if(!Enum.IsDefined(typeof(CampaignObjectiveKind),objectiveKind)||objectiveKind!=expected)throw new ArgumentException("Saved campaign rules do not match.");
        }
    }
}
