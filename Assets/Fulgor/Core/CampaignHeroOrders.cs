using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum HeroOrderFailure
    {
        None, UnknownArmy, UnknownHero, HeroAlreadyOnMap, Occupied, NoHero, AlreadyLed, DifferentFaction,
        NotAdjacent, ArmyInBattle, TooManyFormations, UnknownFormation, NoMovement
    }

    /// <summary>
    /// Órdenes de mapa del héroe. Un héroe solo es una hueste sin formaciones; los batallones vecinos de
    /// su reino se le unen y se separan a casillas contiguas. Las huestes creadas así usan identificadores
    /// estables (<c>hero/…</c> y <c>split/…</c>) que el guardado sabe reconstruir.
    /// </summary>
    public sealed partial class CampaignArmyRegistry
    {
        public const int LoneHeroMovement = 6;
        public const string HeroArmyPrefix = "hero/", SplitArmyPrefix = "split/";

        public static string HeroArmyId(string heroId) => HeroArmyPrefix + heroId;
        public static string SplitArmyId(string formationId) => SplitArmyPrefix + formationId;

        public bool TryGetHeroArmy(string heroId, out CampaignArmyOnMap army)
        {
            army = heroId == null ? null : armies.Values.FirstOrDefault(a => a.Army.HeroId == heroId);
            return army != null;
        }

        /// <summary>Coloca un héroe solo, con el movimiento completo; al empezar la partida o al contratarlo.</summary>
        public bool TryPlaceHero(string heroId, HexCoordinates position, out CampaignArmyRegistry next, out HeroOrderFailure failure)
        {
            next = this;
            var hero = HeroCatalog.Find(heroId);
            if (hero == null) { failure = HeroOrderFailure.UnknownHero; return false; }
            if (TryGetHeroArmy(heroId, out _) || armies.ContainsKey(HeroArmyId(heroId)))
            { failure = HeroOrderFailure.HeroAlreadyOnMap; return false; }
            if (!graph.CanStandAt(position) || TryGetOccupant(position, out _)) { failure = HeroOrderFailure.Occupied; return false; }
            var army = new CampaignArmyState(HeroArmyId(heroId), hero.FactionId, Array.Empty<CampaignFormationState>(), heroId);
            next = new CampaignArmyRegistry(this);
            next.armies.Add(army.Id, new CampaignArmyOnMap(army, new ArmyMovementState(graph, position, LoneHeroMovement)));
            failure = HeroOrderFailure.None; return true;
        }

        /// <summary>
        /// Un batallón propio contiguo se une al héroe. Ambos conservan la casilla del héroe, el menor
        /// movimiento restante y el menor presupuesto por turno.
        /// </summary>
        public bool TryAttach(string heroArmyId, string battalionArmyId, out CampaignArmyRegistry next, out HeroOrderFailure failure)
        {
            next = this;
            if (!TryGetArmy(heroArmyId, out var leader) || !TryGetArmy(battalionArmyId, out var battalion) || heroArmyId == battalionArmyId)
            { failure = HeroOrderFailure.UnknownArmy; return false; }
            if (leader.Army.HeroId == null) { failure = HeroOrderFailure.NoHero; return false; }
            if (battalion.Army.HeroId != null) { failure = HeroOrderFailure.AlreadyLed; return false; }
            if (leader.Army.FactionId != battalion.Army.FactionId) { failure = HeroOrderFailure.DifferentFaction; return false; }
            if (IsLocked(heroArmyId) || IsLocked(battalionArmyId)) { failure = HeroOrderFailure.ArmyInBattle; return false; }
            if (leader.Movement.Position.DistanceTo(battalion.Movement.Position) != 1) { failure = HeroOrderFailure.NotAdjacent; return false; }
            if (leader.Army.Formations.Count + battalion.Army.Formations.Count > 6) { failure = HeroOrderFailure.TooManyFormations; return false; }
            var merged = leader.Army.WithFormations(leader.Army.Formations.Concat(battalion.Army.Formations));
            int budget = Math.Min(leader.Movement.MovementPerTurn, battalion.Movement.MovementPerTurn);
            int remaining = Math.Min(Math.Min(leader.Movement.RemainingMovement, battalion.Movement.RemainingMovement), budget);
            next = new CampaignArmyRegistry(this);
            next.armies.Remove(battalionArmyId);
            next.armies[heroArmyId] = new CampaignArmyOnMap(merged, ArmyMovementState.Restore(graph, leader.Movement.Position, budget, remaining));
            failure = HeroOrderFailure.None; return true;
        }

        /// <summary>Separa una formación del héroe a una casilla contigua libre, como hueste propia.</summary>
        public bool TryDetach(string heroArmyId, string formationId, HexCoordinates destination,
            out CampaignArmyRegistry next, out HeroOrderFailure failure)
        {
            next = this;
            if (!TryGetArmy(heroArmyId, out var leader)) { failure = HeroOrderFailure.UnknownArmy; return false; }
            if (leader.Army.HeroId == null) { failure = HeroOrderFailure.NoHero; return false; }
            if (IsLocked(heroArmyId)) { failure = HeroOrderFailure.ArmyInBattle; return false; }
            var formation = leader.Army.Formations.FirstOrDefault(f => f.Id == formationId);
            if (formation == null || armies.ContainsKey(SplitArmyId(formationId))) { failure = HeroOrderFailure.UnknownFormation; return false; }
            if (leader.Movement.Position.DistanceTo(destination) != 1) { failure = HeroOrderFailure.NotAdjacent; return false; }
            if (!graph.CanStandAt(destination) || TryGetOccupant(destination, out _)) { failure = HeroOrderFailure.Occupied; return false; }
            var split = new CampaignArmyState(SplitArmyId(formationId), leader.Army.FactionId, new[] { formation });
            var rest = leader.Army.WithFormations(leader.Army.Formations.Where(f => f.Id != formationId));
            int remaining = leader.Movement.RemainingMovement;
            next = new CampaignArmyRegistry(this);
            next.armies[heroArmyId] = new CampaignArmyOnMap(rest, rest.IsLoneHero ?
                ArmyMovementState.Restore(graph, leader.Movement.Position, LoneHeroMovement, Math.Min(remaining, LoneHeroMovement)) : leader.Movement);
            next.armies.Add(split.Id, new CampaignArmyOnMap(split,
                ArmyMovementState.Restore(graph, destination, LoneHeroMovement, Math.Min(remaining, LoneHeroMovement))));
            failure = HeroOrderFailure.None; return true;
        }

        /// <summary>
        /// Un héroe solo no combate: el atacante contiguo lo apresa sin batalla, gasta su movimiento y la
        /// hueste del héroe desaparece del mapa. El llamador marca al héroe como cautivo.
        /// </summary>
        public bool TryCaptureLoneHero(string attackerId, string targetId, out CampaignArmyRegistry next,
            out string heroId, out CampaignAttackFailure failure)
        {
            next = this; heroId = null;
            failure = PreviewEngagement(attackerId, targetId);
            if (failure != CampaignAttackFailure.None) return false;
            var target = armies[targetId];
            if (!target.Army.IsLoneHero) { failure = CampaignAttackFailure.UnknownArmy; return false; }
            var attacker = armies[attackerId];
            heroId = target.Army.HeroId;
            next = new CampaignArmyRegistry(this);
            next.armies.Remove(targetId);
            next.armies[attackerId] = new CampaignArmyOnMap(attacker.Army,
                ArmyMovementState.Restore(graph, attacker.Movement.Position, attacker.Movement.MovementPerTurn, 0));
            return true;
        }

        /// <summary>Héroes que estaban en el mapa antes de resolver un combate y ya no están.</summary>
        public static IReadOnlyList<string> HeroesLost(CampaignArmyRegistry before, CampaignArmyRegistry after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            var remaining = new HashSet<string>(after.armies.Values.Select(a => a.Army.HeroId).Where(h => h != null), StringComparer.Ordinal);
            return before.armies.Values.Select(a => a.Army.HeroId)
                .Where(h => h != null && !remaining.Contains(h)).OrderBy(h => h, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        // Huestes nacidas de un héroe o de una separación: su reino sale del héroe o de su formación.
        private static bool TryDynamicArmyFaction(CampaignArmyData saved, Func<string, string> formationFaction, out string faction)
        {
            faction = null;
            if (saved.id.StartsWith(HeroArmyPrefix, StringComparison.Ordinal))
            {
                var hero = HeroCatalog.Find(saved.id.Substring(HeroArmyPrefix.Length));
                if (hero == null || saved.heroId != hero.Id) return false;
                faction = hero.FactionId; return true;
            }
            if (saved.id.StartsWith(SplitArmyPrefix, StringComparison.Ordinal) && saved.heroId == null &&
                saved.formations != null && saved.formations.Length > 0)
            {
                faction = formationFaction(saved.formations[0]?.id);
                return faction != null;
            }
            return false;
        }
    }
}
