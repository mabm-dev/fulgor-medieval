using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    /// <summary>A troop type presented together, retaining every formation's wounds and identity.</summary>
    public sealed class CampaignTroopStack
    {
        private readonly List<CampaignFormationState> members = new();
        private CampaignTroopStack(CampaignFormationState first) { members.Add(first); }
        public CampaignFormationState Representative => members[0];
        public IReadOnlyList<CampaignFormationState> Members => members.AsReadOnly();
        public string Id => Representative.Id;
        public long Quantity => members.Sum(f => (long)f.Quantity);
        public long Health => members.Sum(f => f.Health);
        public int Morale => (int)(members.Sum(f => (long)f.Morale * f.Quantity) / Quantity);
        public bool Contains(string id) => members.Any(f => f.Id == id);
        public bool IsSameType(CampaignFormationState other)
        {
            var first = Representative;
            var a = first.Attributes; var b = other.Attributes;
            return first.Kind == other.Kind && a.Attack == b.Attack && a.Defense == b.Defense &&
                a.MinimumDamage == b.MinimumDamage && a.MaximumDamage == b.MaximumDamage &&
                a.HealthPerMember == b.HealthPerMember && a.Movement == b.Movement && a.Initiative == b.Initiative &&
                a.Range == b.Range && a.Morale == b.Morale && a.Discipline == b.Discipline;
        }
        public static IReadOnlyList<CampaignTroopStack> Group(IEnumerable<CampaignFormationState> formations)
        {
            var groups = new List<CampaignTroopStack>();
            foreach (var formation in formations ?? Array.Empty<CampaignFormationState>())
            {
                var group = groups.FirstOrDefault(g => g.IsSameType(formation));
                if (group == null) groups.Add(new CampaignTroopStack(formation));
                else group.members.Add(formation);
            }
            return groups.AsReadOnly();
        }
    }
}
