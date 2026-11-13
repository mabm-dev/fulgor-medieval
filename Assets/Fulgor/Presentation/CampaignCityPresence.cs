using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    public readonly struct CampaignCityPresence
    {
        public CampaignCityPresence(CampaignArmyState guard, CampaignArmyOnMap visitor, CampaignHeroPosition hero, string cityId)
        {
            GarrisonTroops = guard?.Formations.Sum(f => (long)f.Quantity) ?? 0;
            VisitorTroops = visitor?.Army.Formations.Sum(f => (long)f.Quantity) ?? 0;
            GarrisonRepresentative = guard?.Formations.FirstOrDefault();
            VisitorRepresentative = visitor?.Army.Formations.FirstOrDefault();
            VisitorArmyId = visitor?.Army.Id;
            HeroInGarrison = hero?.settlementId == cityId;
            HeroVisiting = visitor != null && hero?.armyId == visitor.Army.Id;
        }
        public long GarrisonTroops { get; }
        public long VisitorTroops { get; }
        public string VisitorArmyId { get; }
        public CampaignFormationState GarrisonRepresentative { get; }
        public CampaignFormationState VisitorRepresentative { get; }
        public bool HeroInGarrison { get; }
        public bool HeroVisiting { get; }
        public bool HasGarrison => GarrisonTroops > 0 || HeroInGarrison;
        public bool HasVisitor => VisitorArmyId != null;
    }

    public sealed partial class CampaignTurnController
    {
        public CampaignCityPresence CityPresence(string id)
        {
            if (Military == null || SettlementState == null || !SettlementState.TryGet(id, out var city)) return default;
            CampaignArmyOnMap visitor = null;
            registry?.TryGetOccupant(city.Position, out visitor);
            return new CampaignCityPresence(Military.Garrison(id), visitor, Military.HeroPosition(city.OwnerFactionId, registry), id);
        }
    }
}
