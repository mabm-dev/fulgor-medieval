using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    [Serializable]
    public sealed class CampaignHeroPosition
    {
        public string factionId, armyId, settlementId;
        internal CampaignHeroPosition Copy() => (CampaignHeroPosition)MemberwiseClone();
    }

    public sealed partial class CampaignMilitaryState
    {
        private List<CampaignHeroPosition> heroPositions = new();

        public CampaignHeroPosition HeroPosition(string faction, CampaignArmyRegistry armies)
        {
            var saved = heroPositions.FirstOrDefault(h => h.factionId == faction);
            if (saved?.settlementId != null && owners.TryGet(saved.settlementId, out var city) && city.OwnerFactionId == faction)
                return saved.Copy();
            if (saved?.armyId != null && armies != null && armies.TryGetArmy(saved.armyId, out var army) && army.Army.FactionId == faction)
                return saved.Copy();
            var field = armies?.Armies.FirstOrDefault(a => a.Army.FactionId == faction);
            return new CampaignHeroPosition { factionId = faction, armyId = field?.Army.Id,
                settlementId = field == null ? owners.Settlements.FirstOrDefault(c => c.OwnerFactionId == faction)?.Id : null };
        }

        public CampaignMilitaryState WithJoinedHero(string source, string target, CampaignArmyRegistry armies)
        {
            if (!armies.TryGetArmy(source, out var army)) return this;
            var hero = HeroPosition(army.Army.FactionId, armies);
            if (hero.armyId != source) return this;
            var next = new CampaignMilitaryState(this);
            next.SetHero(new CampaignHeroPosition { factionId = hero.factionId, armyId = target });
            return next;
        }

        private void SetHero(CampaignHeroPosition hero)
        {
            heroPositions.RemoveAll(h => h.factionId == hero.factionId);
            heroPositions.Add(hero);
        }

        public CampaignMilitaryState WithDepositedArmyHero(string armyId, string cityId, CampaignArmyRegistry previous)
        {
            if (!owners.TryGet(cityId, out var city) || HeroPosition(city.OwnerFactionId, previous).armyId != armyId) return this;
            var next = new CampaignMilitaryState(this);
            next.SetHero(new CampaignHeroPosition { factionId = city.OwnerFactionId, settlementId = cityId });
            return next;
        }

        public bool TryTransferHero(string cityId, string armyId, CampaignArmyRegistry armies, bool deposit,
            out CampaignMilitaryState next, out CampaignArmyRegistry nextArmies)
        {
            next = this; nextArmies = armies;
            if (!CanTransfer(cityId, armyId, armies, out var city, out var army)) return false;
            var hero = HeroPosition(city.OwnerFactionId, armies);
            if (deposit ? hero.armyId != armyId : hero.settlementId != cityId) return false;
            next = new CampaignMilitaryState(this);
            next.SetHero(new CampaignHeroPosition { factionId = city.OwnerFactionId,
                armyId = deposit ? null : armyId, settlementId = deposit ? cityId : null });
            nextArmies = armies.ReplaceFormations(armyId, army.Army.Formations);
            return true;
        }

        /// <summary>Returns a stack or the garrison hero to a visitor, recreating a dissolved host atomically.</summary>
        public bool TrySendToVisitor(string cityId, string armyId, string formationId, bool withHero,
            CampaignArmyRegistry armies, out CampaignMilitaryState next, out CampaignArmyRegistry nextArmies, out string visitorId)
        {
            next = this; nextArmies = armies; visitorId = null;
            if (armies == null || armies.HasActiveEncounters || !owners.TryGet(cityId, out var city)) return false;
            if (withHero && HeroPosition(city.OwnerFactionId, armies).settlementId != cityId) return false;
            bool existing = armyId != null && CanTransfer(cityId, armyId, armies, out _, out _);
            if (existing)
            {
                bool accepted = withHero
                    ? TryTransferHero(cityId, armyId, armies, false, out next, out nextArmies)
                    : TryTransferStack(cityId, armyId, formationId, armies, false, out next, out nextArmies);
                if (accepted) visitorId = armyId;
                return accepted;
            }
            var stack = withHero ? guards[cityId].Formations.FirstOrDefault()?.Id : formationId;
            if (stack == null || !TryMusterStack(cityId, city.OwnerFactionId, stack, armies, out var staged, out var roster, out var id)) return false;
            if (withHero)
            {
                foreach (var group in CampaignTroopStack.Group(staged.Garrison(cityId).Formations))
                {
                    if (!staged.TryTransferStack(cityId, id, group.Id, roster, false, out var moved, out var movedRoster)) return false;
                    staged = moved; roster = movedRoster;
                }
                if (!staged.TryTransferHero(cityId, id, roster, false, out var commanded, out var commandedRoster)) return false;
                staged = commanded; roster = commandedRoster;
            }
            next = staged; nextArmies = roster; visitorId = id;
            return true;
        }
        private void RestoreHeroes(CampaignHeroPosition[] data, HashSet<string> factions)
        {
            if (data == null) return;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var saved in data)
            {
                var hero = saved?.Copy();
                if (hero != null)
                {
                    if (string.IsNullOrEmpty(hero.armyId)) hero.armyId = null;
                    if (string.IsNullOrEmpty(hero.settlementId)) hero.settlementId = null;
                }
                if (hero?.factionId == null || !factions.Contains(hero.factionId) || !seen.Add(hero.factionId) ||
                    (hero.armyId == null) == (hero.settlementId == null)) throw new ArgumentException("Invalid hero location.");
                if (hero.settlementId != null && (!owners.TryGet(hero.settlementId, out var city) || city.OwnerFactionId != hero.factionId))
                    throw new ArgumentException("Hero garrison does not belong to its realm.");
                if (hero.armyId != null)
                {
                    string faction = initialArmies?.TryGetArmy(hero.armyId, out var original) == true ? original.Army.FactionId : MusterOrigin(hero.armyId)?.factionId;
                    if (faction != hero.factionId) throw new ArgumentException("Unknown hero army.");
                }
                heroPositions.Add(hero.Copy());
            }
        }

        public bool TryReorderStack(string cityId, string armyId, string sourceId, string targetId, CampaignArmyRegistry armies,
            out CampaignMilitaryState next, out CampaignArmyRegistry nextArmies)
        {
            next = this; nextArmies = armies;
            if (armies == null || armies.HasActiveEncounters || !owners.TryGet(cityId, out var city)) return false;
            CampaignArmyOnMap army = null;
            if (armyId != null && (!armies.TryGetArmy(armyId, out army) || army.Army.FactionId != city.OwnerFactionId)) return false;
            var roster = armyId == null ? guards[cityId] : army.Army;
            var groups = CampaignTroopStack.Group(roster.Formations).ToList();
            var source = groups.FirstOrDefault(g => g.Contains(sourceId));
            var target = groups.FirstOrDefault(g => g.Contains(targetId));
            if (source == null || source == target) return false;
            int destination = target == null ? groups.Count - 1 : groups.IndexOf(target);
            groups.Remove(source); groups.Insert(destination, source);
            var ordered = groups.SelectMany(g => g.Members);
            if (armyId == null) { next = new CampaignMilitaryState(this); next.guards[cityId] = roster.WithFormations(ordered); }
            else nextArmies = armies.ReorderFormations(armyId, ordered);
            return true;
        }
    }

    public sealed partial class CampaignArmyRegistry
    {
        internal CampaignArmyRegistry ReorderFormations(string id, IEnumerable<CampaignFormationState> formations)
        {
            var current = armies[id]; var next = new CampaignArmyRegistry(this);
            next.armies[id] = new CampaignArmyOnMap(current.Army.WithFormations(formations), current.Movement);
            return next;
        }
    }
}

