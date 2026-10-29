using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    [Serializable]
    public sealed class CampaignFormationData
    {
        public string id;
        public long health;
        public int morale;
    }

    [Serializable]
    public sealed class CampaignArmyData
    {
        public string id;
        public int q, r, remainingMovement;
        public CampaignFormationData[] formations;
        public string[] appliedEncounters;
    }

    [Serializable]
    public sealed class CampaignArmyRegistryData
    {
        public CampaignArmyData[] armies;
        public string[] resolvedEncounters;
    }

    public sealed partial class CampaignArmyRegistry
    {
        public bool HasActiveEncounters => encounters.Count != 0;

        public CampaignArmyRegistryData Capture()
        {
            if (HasActiveEncounters)
                throw new InvalidOperationException("Resolve active encounters before saving the campaign.");
            var saved = new List<CampaignArmyData>();
            foreach (var unit in Armies)
            {
                var formations = new List<CampaignFormationData>();
                foreach (var formation in unit.Army.Formations)
                    formations.Add(new CampaignFormationData {
                        id = formation.Id, health = formation.Health, morale = formation.Morale });
                formations.Sort((a, b) => StringComparer.Ordinal.Compare(a.id, b.id));
                saved.Add(new CampaignArmyData { id = unit.Army.Id, q = unit.Movement.Position.Q,
                    r = unit.Movement.Position.R, remainingMovement = unit.Movement.RemainingMovement,
                    formations = formations.ToArray(), appliedEncounters = unit.Army.CaptureEncounterHistory() });
            }
            var history = new List<string>(resolved); history.Sort(StringComparer.Ordinal);
            return new CampaignArmyRegistryData { armies = saved.ToArray(), resolvedEncounters = history.ToArray() };
        }

        // The scenario supplies immutable troop rules; a save only stores survivors.
        // Reconstruct into a fresh registry so invalid data cannot mutate a live game.
        public static CampaignArmyRegistry Restore(CampaignArmyRegistryData data, CampaignArmyRegistry initial)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (data == null || data.armies == null || data.resolvedEncounters == null)
                throw new ArgumentException("Missing army registry state.");
            if (initial.HasActiveEncounters) throw new ArgumentException("Initial deployment cannot contain active encounters.");
            var history = ValidateHistory(data.resolvedEncounters);
            var restored = new CampaignArmyRegistry(initial.graph, Array.Empty<CampaignArmyPlacement>());
            restored.resolved.UnionWith(history);
            var occupied = new HashSet<HexCoordinates>();
            foreach (var saved in data.armies)
            {
                if (saved == null || !initial.TryGetArmy(saved.id, out var original) || restored.armies.ContainsKey(saved.id))
                    throw new ArgumentException("Unknown or duplicate saved army.");
                var position = new HexCoordinates(saved.q, saved.r);
                if (!occupied.Add(position) || !MapPathfinder.TryFindPath(initial.graph, original.Movement.Position, position, out _))
                    throw new ArgumentException("Saved army position is occupied or inaccessible.");
                var movement = ArmyMovementState.Restore(initial.graph, position,
                    original.Movement.MovementPerTurn, saved.remainingMovement);
                if (saved.formations == null || saved.formations.Length == 0 || saved.formations.Length > 6)
                    throw new ArgumentException("Saved army requires surviving formations.");
                var known = new Dictionary<string, CampaignFormationState>(StringComparer.Ordinal);
                foreach (var formation in original.Army.Formations) known.Add(formation.Id, formation);
                var survivors = new List<CampaignFormationState>();
                foreach (var formation in saved.formations)
                {
                    if (formation == null || formation.id == null || !known.TryGetValue(formation.id, out var source))
                        throw new ArgumentException("Unknown or duplicate saved formation.");
                    known.Remove(formation.id);
                    if (formation.health > source.Health)
                        throw new ArgumentException("Saved formation exceeds its scenario strength.");
                    survivors.Add(new CampaignFormationState(source.Id, source.Attributes,
                        formation.health, formation.morale, source.Kind));
                }
                var applied = ValidateHistory(saved.appliedEncounters);
                if (!applied.IsSubsetOf(history)) throw new ArgumentException("Army history contains an unresolved encounter.");
                var army = CampaignArmyState.RestoreSurvivors(original.Army.Id, original.Army.FactionId, survivors, applied);
                restored.armies.Add(army.Id, new CampaignArmyOnMap(army, movement));
            }
            return restored;
        }

        private static HashSet<string> ValidateHistory(string[] entries)
        {
            if (entries == null) throw new ArgumentException("Missing encounter history.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in entries)
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                    throw new ArgumentException("Invalid or duplicate encounter history.");
            return ids;
        }
    }
}
