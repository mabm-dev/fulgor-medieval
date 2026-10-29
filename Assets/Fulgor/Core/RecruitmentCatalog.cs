using System;
using System.Collections.Generic;
using Fulgor.Core.Combat;

namespace Fulgor.Core.Campaign
{
    public sealed class RecruitmentSpec
    {
        internal RecruitmentSpec(TroopProfile profile,int quantity,int turns,CampaignResources cost)
        {Profile=profile;Quantity=quantity;Turns=turns;Cost=cost;}
        public TroopProfile Profile {get;}
        public int Quantity {get;}
        public int Turns {get;}
        public CampaignResources Cost {get;}
        public string Name=>TroopCatalog.Get(Profile).Name;
        public CampaignFormationState Create(string id)
        {
            var troop=TroopCatalog.Get(Profile);
            return new CampaignFormationState(id,troop.Attributes,checked((long)Quantity*troop.Attributes.HealthPerMember),troop.Attributes.Morale,troop.Kind);
        }
    }
    public static class RecruitmentCatalog
    {
        public static IReadOnlyList<RecruitmentSpec> All {get;}=Array.AsReadOnly(new RecruitmentSpec[]{
            new(TroopProfile.Spearmen,20,2,new CampaignResources(15,10,0,12,12)),
            new(TroopProfile.MenAtArms,15,3,new CampaignResources(15,10,15,25,15)),
            new(TroopProfile.Crossbowmen,15,2,new CampaignResources(10,20,0,20,12)),
            new(TroopProfile.LightCavalry,10,3,new CampaignResources(20,10,0,35,15))});
        public static RecruitmentSpec Get(TroopProfile profile)
        {int i=(int)profile;if(i<0||i>=All.Count)throw new ArgumentOutOfRangeException(nameof(profile));return All[i];}
    }

    public enum RecruitmentStatus { Pending, Completed, Cancelled }
    [Serializable]public sealed class RecruitmentRecord
    {
        public int serial,startTurn,completionTurn;
        public string settlementId,factionId;
        public TroopProfile profile;
        public RecruitmentStatus status;
        public string FormationId=>"citytroop/"+serial;
        internal RecruitmentRecord Copy()=>(RecruitmentRecord)MemberwiseClone();
    }
    [Serializable]public sealed class MusterRecord
    {
        public int serial;public string settlementId,factionId;
        public string ArmyId=>"muster/"+serial;
        internal MusterRecord Copy()=>(MusterRecord)MemberwiseClone();
    }
    [Serializable]public sealed class GarrisonData
    {
        public string settlementId;
        public CampaignFormationData[] formations;
        public string[] appliedEncounters;
    }
    [Serializable]public sealed class MilitarySaveData
    {
        public RecruitmentRecord[] recruits;
        public MusterRecord[] musters;
        public GarrisonData[] garrisons;
    }
    public sealed class FormationOrigin
    {
        internal FormationOrigin(string faction,CampaignFormationState template){FactionId=faction;Template=template;}
        public string FactionId {get;}
        public CampaignFormationState Template {get;}
    }
}
