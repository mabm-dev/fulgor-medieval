using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public bool SelectArmyForRoster(string id)
        {
            if (registry == null || HasPendingBattle || IsRivalPhaseActive || !registry.TryGetArmy(id, out var army) || army.Army.FactionId != playerFaction) return false;
            selection?.ClearSelection(); selectedArmyId = id; RefreshArmy(); return true;
        }

        public bool TryTransferCityHero(string cityId, bool deposit)
        {
            if (!CanOrderMilitary(cityId)) return false;
            CampaignMilitaryState next; CampaignArmyRegistry roster;
            string visitor = selectedArmyId;
            bool moved = deposit
                ? Military.TryTransferHero(cityId, selectedArmyId, registry, true, out next, out roster)
                : Military.TrySendToVisitor(cityId, selectedArmyId, null, true, registry, out next, out roster, out visitor);
            if (!moved) return false;
            marchOrders = marchOrders.Cancel(selectedArmyId); selectedArmyId = visitor; PublishMilitary(next, roster); return true;
        }

        public bool TryReorderCityStack(string cityId, bool armyRow, string source, string target)
        {
            if (!CanOrderMilitary(cityId) || !Military.TryReorderStack(cityId, armyRow ? selectedArmyId : null, source, target, registry, out var next, out var roster)) return false;
            PublishMilitary(next, roster); return true;
        }
    }
}

