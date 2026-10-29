using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public enum SettlementCaptureFailure { None, UnknownSettlement, UnknownArmy, BattlePending, NotInside, AlreadyOwned }

    public sealed partial class CampaignSettlementRegistry
    {
        public SettlementCaptureFailure PreviewCapture(CampaignArmyRegistry armies,string armyId,string settlementId)
        {
            if(settlementId==null||!TryGet(settlementId,out var city))return SettlementCaptureFailure.UnknownSettlement;
            if(armies==null||!armies.TryGetArmy(armyId,out var army))return SettlementCaptureFailure.UnknownArmy;
            if(armies.HasActiveEncounters)return SettlementCaptureFailure.BattlePending;
            if(army.Movement.Position!=city.Position)return SettlementCaptureFailure.NotInside;
            if(army.Army.FactionId==city.OwnerFactionId)return SettlementCaptureFailure.AlreadyOwned;
            return SettlementCaptureFailure.None;
        }

        public bool TryCapture(CampaignArmyRegistry armies,string armyId,string settlementId,
            out CampaignSettlementRegistry next,out SettlementCaptureFailure failure)
        {
            next=this;failure=PreviewCapture(armies,armyId,settlementId);
            if(failure!=SettlementCaptureFailure.None)return false;
            armies.TryGetArmy(armyId,out var army);next=WithOwner(settlementId,army.Army.FactionId);return true;
        }

        private CampaignSettlementRegistry WithOwner(string id,string owner)
        {
            var list=Settlements.Select(c=>c.Id==id?new CampaignSettlement(c.Id,c.Name,owner,c.Position):c);
            return new CampaignSettlementRegistry(routes,list);
        }

        public SettlementOwnershipData[] CaptureOwnership()=>Settlements.OrderBy(c=>c.Id,StringComparer.Ordinal)
            .Select(c=>new SettlementOwnershipData {settlementId=c.Id,ownerFactionId=c.OwnerFactionId}).ToArray();

        public CampaignSettlementRegistry RestoreOwnership(SettlementOwnershipData[] data,IEnumerable<string> factions)
        {
            if(data==null||data.Length!=Settlements.Count)throw new ArgumentException("Missing settlement ownership.");
            var allowed=new HashSet<string>(factions,StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);var list=new List<CampaignSettlement>();
            foreach(var owner in data)
            {
                if(owner==null||owner.settlementId==null||!TryGet(owner.settlementId,out var original)||
                    !seen.Add(owner.settlementId)||string.IsNullOrWhiteSpace(owner.ownerFactionId)||!allowed.Contains(owner.ownerFactionId))
                    throw new ArgumentException("Unknown, duplicate or invalid settlement owner.");
                list.Add(new CampaignSettlement(original.Id,original.Name,owner.ownerFactionId,original.Position));
            }
            return new CampaignSettlementRegistry(routes,list.OrderBy(c=>c.Id,StringComparer.Ordinal));
        }
    }

    [Serializable]public sealed class SettlementOwnershipData
    {
        public string settlementId,ownerFactionId;
    }
}
