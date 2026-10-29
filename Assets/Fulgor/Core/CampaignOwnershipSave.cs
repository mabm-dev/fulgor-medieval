using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignSaveData
    {
        public SettlementOwnershipData[] settlementOwners;
        public bool multipleArmies;

        public void IncludeOwnership(string baseContext,CampaignSettlementRegistry initial,CampaignSettlementRegistry current)
        {
            if(initial==null||current==null)throw new ArgumentNullException(nameof(current));
            settlementOwners=current.CaptureOwnership();
            context=SettlementScenarioSignature(baseContext,initial);version=5;
        }

        public CampaignSettlementRegistry RestoreSettlementOwners(CampaignSettlementRegistry initial,IEnumerable<string> factions)
        {
            if(initial==null)throw new ArgumentNullException(nameof(initial));
            return version<5?initial:initial.RestoreOwnership(settlementOwners,factions);
        }

        public static string SettlementScenarioSignature(string baseContext,CampaignSettlementRegistry initial)
        {
            if(string.IsNullOrEmpty(baseContext)||initial==null)throw new ArgumentException("Missing settlement identity.");
            var tokens=new List<string>{"ownership-v5",baseContext};
            foreach(var owner in initial.CaptureOwnership())
            {
                initial.TryGet(owner.settlementId,out var city);
                tokens.Add(city.Id);tokens.Add(city.Name);tokens.Add(city.OwnerFactionId);
                tokens.Add(FormattableString.Invariant($"{city.Position.Q}:{city.Position.R}"));
            }
            ulong hash=14695981039346656037UL;
            foreach(var token in tokens)
                foreach(var c in token.Length.ToString(CultureInfo.InvariantCulture)+":"+token)
                    hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",CultureInfo.InvariantCulture);
        }
    }
}
