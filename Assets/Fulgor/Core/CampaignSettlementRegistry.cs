using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>Immutable settlement identity and current ownership.</summary>
    public sealed class CampaignSettlement
    {
        public CampaignSettlement(string id,string name,string ownerFactionId,HexCoordinates position)
        {
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Settlement ID is required.",nameof(id));
            if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Settlement name is required.",nameof(name));
            if(string.IsNullOrWhiteSpace(ownerFactionId))throw new ArgumentException("Settlement owner is required.",nameof(ownerFactionId));
            Id=id;Name=name;OwnerFactionId=ownerFactionId;Position=position;
        }
        public string Id { get; }
        public string Name { get; }
        public string OwnerFactionId { get; }
        public HexCoordinates Position { get; }
    }

    public sealed partial class CampaignSettlementRegistry
    {
        private readonly MapRouteGraph routes;
        private readonly Dictionary<HexCoordinates,CampaignSettlement> byPosition=new();
        private readonly Dictionary<string,CampaignSettlement> byId=new(StringComparer.Ordinal);
        public CampaignSettlementRegistry(MapRouteGraph routes,IEnumerable<CampaignSettlement> settlements)
        {
            if(routes==null)throw new ArgumentNullException(nameof(routes));
            if(settlements==null)throw new ArgumentNullException(nameof(settlements));
            this.routes=routes;
            var list=new List<CampaignSettlement>();
            foreach(var settlement in settlements) {
                if(settlement==null)throw new ArgumentException("Empty settlement.",nameof(settlements));
                if(!routes.CanStandAt(settlement.Position))throw new ArgumentException("Settlement must occupy passable land.",nameof(settlements));
                if(byPosition.ContainsKey(settlement.Position)||byId.ContainsKey(settlement.Id))throw new ArgumentException("Duplicate settlement ID or position.",nameof(settlements));
                byPosition.Add(settlement.Position,settlement);byId.Add(settlement.Id,settlement);list.Add(settlement);
            }
            Settlements=list.AsReadOnly();
        }
        public IReadOnlyList<CampaignSettlement> Settlements { get; }
        public bool TryGet(HexCoordinates position,out CampaignSettlement settlement)=>byPosition.TryGetValue(position,out settlement);
        public bool TryGet(string id,out CampaignSettlement settlement)=>byId.TryGetValue(id,out settlement);
    }
}
