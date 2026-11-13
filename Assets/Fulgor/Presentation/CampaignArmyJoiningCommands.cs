using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public string[] ArmyJoinTargets() => registry == null || HasPendingBattle || IsRivalPhaseActive || IsCampaignFinished
            ? System.Array.Empty<string>() : registry.Armies.Where(a => a.Army.FactionId == playerFaction &&
                Military.TryJoinArmies(selectedArmyId, a.Army.Id, registry, out _)).Select(a => a.Army.Id).ToArray();

        public bool TryJoinSelectedArmyAt(HexCoordinates position)
        {
            if (registry == null || HasPendingBattle || IsRivalPhaseActive || IsCampaignFinished ||
                !registry.TryGetArmy(selectedArmyId, out var source) || source.Army.FactionId != playerFaction ||
                !registry.TryGetOccupant(position, out var target) || target.Army.FactionId != playerFaction ||
                !Military.TryJoinArmies(selectedArmyId, target.Army.Id, registry, out var joined)) return false;
            marchOrders = marchOrders.Cancel(selectedArmyId).Cancel(target.Army.Id);
            Military = Military.WithJoinedHero(selectedArmyId,target.Army.Id,registry);registry = joined; selectedArmyId = target.Army.Id; CityRevision++;
            selection?.LocateCell(position);
            RefreshArmy();
            return true;
        }
    }
}
