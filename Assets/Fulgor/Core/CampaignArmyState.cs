using System;
using System.Collections.Generic;
using Fulgor.Core.Combat;

namespace Fulgor.Core.Campaign
{
    /// <summary>Persistent survivors; no tactical withdrawal flag or deployment coordinates.</summary>
    public sealed class CampaignFormationState
    {
        public CampaignFormationState(string id, FormationAttributes attributes, long health, int morale, FormationKind kind = FormationKind.Infantry)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing formation ID.", nameof(id));
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            if (health < 1 || health > (long)int.MaxValue * attributes.HealthPerMember)
                throw new ArgumentOutOfRangeException(nameof(health));
            if (morale < 0 || morale > 100) throw new ArgumentOutOfRangeException(nameof(morale));
            if (!Enum.IsDefined(typeof(FormationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id; Health = health; Morale = morale; Kind = kind;
        }
        public string Id { get; }
        public FormationKind Kind { get; }
        public FormationAttributes Attributes { get; }
        public long Health { get; }
        public int Morale { get; }
        public int Quantity => checked((int)((Health - 1) / Attributes.HealthPerMember + 1));

        public BattleFormationState Deploy(BattleFormationSlot slot)
        {
            if (slot.Kind != Kind) throw new ArgumentException("Deployment must preserve troop kind.", nameof(slot));
            return BattleFormationState.FromSurvivors(slot, Attributes, Quantity, Health, Morale);
        }

        internal bool Matches(CampaignFormationState other)
        {
            var a = Attributes; var b = other.Attributes;
            return Id == other.Id && Kind == other.Kind && Health == other.Health && Morale == other.Morale &&
                a.Attack == b.Attack && a.Defense == b.Defense && a.MinimumDamage == b.MinimumDamage &&
                a.MaximumDamage == b.MaximumDamage && a.HealthPerMember == b.HealthPerMember &&
                a.Movement == b.Movement && a.Initiative == b.Initiative && a.Range == b.Range &&
                a.Morale == b.Morale && a.Discipline == b.Discipline;
        }
    }

    /// <summary>Immutable army roster and encounter application history.</summary>
    public sealed class CampaignArmyState
    {
        private readonly HashSet<string> appliedEncounters;
        public CampaignArmyState(string id, string factionId, IEnumerable<CampaignFormationState> formations)
            : this(id, factionId, formations, new HashSet<string>(StringComparer.Ordinal)) { }

        private CampaignArmyState(string id, string factionId, IEnumerable<CampaignFormationState> formations,
            HashSet<string> appliedEncounters)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing army ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(factionId)) throw new ArgumentException("Missing faction ID.", nameof(factionId));
            if (formations == null) throw new ArgumentNullException(nameof(formations));
            var copy = new List<CampaignFormationState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var formation in formations)
            {
                if (formation == null || !ids.Add(formation.Id)) throw new ArgumentException("Invalid or duplicate formation.", nameof(formations));
                copy.Add(formation);
            }
            if (copy.Count > 6) throw new ArgumentException("Army supports at most six formations.", nameof(formations));
            Id = id; FactionId = factionId; Formations = copy.AsReadOnly();
            this.appliedEncounters = new HashSet<string>(appliedEncounters, StringComparer.Ordinal);
        }

        public string Id { get; }
        public string FactionId { get; }
        public IReadOnlyList<CampaignFormationState> Formations { get; }
        public bool IsDissolved => Formations.Count == 0;
        public bool HasAppliedEncounter(string encounterId) => encounterId != null && appliedEncounters.Contains(encounterId);

        public CampaignArmyState WithFormations(IEnumerable<CampaignFormationState> formations) =>
            new CampaignArmyState(Id,FactionId,formations,appliedEncounters);

        internal string[] CaptureEncounterHistory()
        {
            var history = new List<string>(appliedEncounters);
            history.Sort(StringComparer.Ordinal);
            return history.ToArray();
        }

        internal static CampaignArmyState RestoreSurvivors(string id, string factionId,
            IEnumerable<CampaignFormationState> formations, HashSet<string> appliedEncounters) =>
            new CampaignArmyState(id, factionId, formations, appliedEncounters);

        internal bool MatchesSnapshot(CampaignArmyState other)
        {
            if (Id != other.Id || FactionId != other.FactionId || Formations.Count != other.Formations.Count ||
                !appliedEncounters.SetEquals(other.appliedEncounters)) return false;
            var byId = new Dictionary<string, CampaignFormationState>(StringComparer.Ordinal);
            foreach (var formation in other.Formations) byId.Add(formation.Id, formation);
            foreach (var formation in Formations)
                if (!byId.TryGetValue(formation.Id, out var match) || !formation.Matches(match)) return false;
            return true;
        }

        public CampaignArmyState ApplyBattleReport(string encounterId, BattleSide side, BattleReport report,
            IReadOnlyDictionary<int, string> formationBindings)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) throw new ArgumentException("Missing encounter ID.", nameof(encounterId));
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (formationBindings == null) throw new ArgumentNullException(nameof(formationBindings));
            if (!Enum.IsDefined(typeof(BattleSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (HasAppliedEncounter(encounterId)) return this;
            var current = new Dictionary<string, CampaignFormationState>(StringComparer.Ordinal);
            foreach (var formation in Formations) current.Add(formation.Id, formation);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var survivors = new List<CampaignFormationState>();
            int count = 0;
            foreach (var result in report.Formations)
            {
                if (result.Slot.Side != side) continue;
                count++;
                if (!formationBindings.TryGetValue(result.Slot.Id, out var id) || id == null ||
                    !current.TryGetValue(id, out var previous) || !seen.Add(id))
                    throw new ArgumentException("Formation bindings do not match this army.", nameof(formationBindings));
                if (previous.Kind != result.Slot.Kind || previous.Quantity != result.InitialQuantity || previous.Attributes.HealthPerMember != result.Attributes.HealthPerMember ||
                    result.TotalHealth > previous.Health || result.Morale > previous.Morale)
                    throw new ArgumentException("Battle result does not match the army's initial survivors.", nameof(report));
                if (!result.IsDestroyed)
                    survivors.Add(new CampaignFormationState(id, previous.Attributes, result.TotalHealth, result.Morale, previous.Kind));
            }
            if (count != Formations.Count || formationBindings.Count != count)
                throw new ArgumentException("The whole army and only that army must be bound to the report.", nameof(formationBindings));
            var history = new HashSet<string>(appliedEncounters, StringComparer.Ordinal) { encounterId };
            return new CampaignArmyState(Id, FactionId, survivors, history);
        }
    }
}
