using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignArmyRegistry
    {
        public bool TryJoin(string sourceId, string targetId, out CampaignArmyRegistry next)
        {
            next = this;
            if (HasActiveEncounters || sourceId == targetId || !TryGetArmy(sourceId, out var source) || !TryGetArmy(targetId, out var target) ||
                source.Army.FactionId != target.Army.FactionId || source.Movement.Position.DistanceTo(target.Movement.Position) != 1 ||
                !graph.TryGetMovementCost(source.Movement.Position, target.Movement.Position, out _) ||
                source.Army.Formations.Count + target.Army.Formations.Count > 6) return false;
            var combined = target.Army.Formations.Concat(source.Army.Formations).ToArray();
            if (combined.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count() != combined.Length) return false;
            var history = new HashSet<string>(target.Army.CaptureEncounterHistory().Union(source.Army.CaptureEncounterHistory()), StringComparer.Ordinal);
            var army = CampaignArmyState.RestoreSurvivors(targetId, target.Army.FactionId, combined, history);
            var movement = ArmyMovementState.Restore(graph, target.Movement.Position, target.Movement.MovementPerTurn, 0);
            next = new CampaignArmyRegistry(this);
            next.armies.Remove(sourceId);
            next.armies[targetId] = new CampaignArmyOnMap(army, movement);
            return true;
        }
    }

    public sealed partial class CampaignMilitaryState
    {
        public bool TryTransferStack(string cityId, string armyId, string formationId, CampaignArmyRegistry armies, bool deposit,
            out CampaignMilitaryState next, out CampaignArmyRegistry nextArmies)
        {
            next = this; nextArmies = armies;
            if (!CanTransfer(cityId, armyId, armies, out _, out var army)) return false;
            var formations = deposit ? army.Army.Formations : guards[cityId].Formations;
            var stack = CampaignTroopStack.Group(formations).FirstOrDefault(g => g.Contains(formationId));
            if (stack == null) return false;
            var military = this; var registry = armies;
            foreach (var member in stack.Members)
            {
                bool success = deposit ? military.TryDeposit(cityId, armyId, member.Id, registry, out var moved, out var roster)
                    : military.TryAssignToArmy(cityId, armyId, member.Id, registry, out moved, out roster);
                if (!success) return false;
                military = moved; registry = roster;
            }
            next = military; nextArmies = registry;
            return true;
        }

        public bool TryMusterStack(string cityId, string faction, string formationId, CampaignArmyRegistry armies,
            out CampaignMilitaryState next, out CampaignArmyRegistry nextArmies, out string newArmyId)
        {
            next = this; nextArmies = armies; newArmyId = null;
            var stack = CampaignTroopStack.Group(Garrison(cityId)?.Formations).FirstOrDefault(g => g.Contains(formationId));
            if (stack == null || !TryMuster(cityId, faction, stack.Id, armies, out var military, out var registry, out var id)) return false;
            foreach (var member in stack.Members.Skip(1))
            {
                if (!military.TryAssignToArmy(cityId, id, member.Id, registry, out var moved, out var roster)) return false;
                military = moved; registry = roster;
            }
            next = military; nextArmies = registry; newArmyId = id;
            return true;
        }

        public bool TryJoinArmies(string sourceId, string targetId, CampaignArmyRegistry armies, out CampaignArmyRegistry next)
        {
            next = armies;
            if (armies == null || !armies.TryGetArmy(sourceId, out var source) || !armies.TryGetArmy(targetId, out var target)) return false;
            if (owners.TryGet(target.Movement.Position, out var city) &&
                (city.OwnerFactionId != target.Army.FactionId || guards[city.Id].Formations.Count + target.Army.Formations.Count +
                    source.Army.Formations.Count + (Pending(city.Id) != null ? 1 : 0) > 6)) return false;
            return armies.TryJoin(sourceId, targetId, out next);
        }
    }
}
