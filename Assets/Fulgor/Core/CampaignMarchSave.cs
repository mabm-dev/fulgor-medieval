using System;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignSaveData
    {
        public CampaignMarchData[] marchOrders;
        public int marchOrderCount;
        public void IncludeMarches(CampaignMarchOrders orders)
        {
            if(version!=6||orders==null)throw new ArgumentException("Military state must precede march orders.");
            marchOrders=orders.Capture();marchOrderCount=marchOrders.Length;context=MarchScenarioSignature(context);version=7;
        }
        public static string MarchScenarioSignature(string militaryContext)
        {
            if(string.IsNullOrEmpty(militaryContext))throw new ArgumentException("Missing military context.");
            ulong hash=14695981039346656037UL;
            foreach(char c in "march-v7:ordinal-priority:whole-steps:pause-blocks:stop-forts:"+militaryContext)
                hash=unchecked((hash^c)*1099511628211UL);
            return hash.ToString("X16",System.Globalization.CultureInfo.InvariantCulture);
        }
        public CampaignMarchOrders RestoreMarches(CampaignArmyRegistry armies,string faction)
        {
            if(version<7)return new CampaignMarchOrders();
            if(marchOrders==null||marchOrders.Length!=marchOrderCount)throw new ArgumentException("Missing or incomplete march orders.");
            return CampaignMarchOrders.Restore(marchOrders,armies,faction);
        }
    }
}
