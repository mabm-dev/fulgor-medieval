using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Héroes en la campaña con varias huestes: colocación inicial, unir y separar batallones,
    /// contratación, cautiverio, decisión del vencedor y mantenimiento en oro.
    /// </summary>
    public sealed partial class CampaignTurnController
    {
        public CampaignHeroRoster Heroes { get; private set; }
        public string LastHeroMessage { get; private set; } = "";

        /// <summary>Héroes vencidos cuyo destino decide el jugador.</summary>
        public IReadOnlyList<PendingHeroFate> PlayerHeroDecisions =>
            Heroes == null ? Array.Empty<PendingHeroFate>() :
                Heroes.PendingFates.Where(p => p.VictorFactionId == playerFaction).ToList().AsReadOnly();

        private IEnumerable<string> HeroFactions() => KnownFactions().Where(f => HeroCatalog.PrincipalOf(f) != null);

        private HexCoordinates? CapitalOf(string faction) =>
            SettlementState?.Settlements.Where(s => s.OwnerFactionId == faction).OrderBy(s => s.Id, StringComparer.Ordinal)
                .Select(s => (HexCoordinates?)s.Position).FirstOrDefault();

        private void InitializeHeroes(bool enabled)
        {
            Heroes = null; LastHeroMessage = "";
            var heroPanel = GetComponent<CampaignHeroPanel>();
            if (!enabled || registry == null || HeroCatalog.PrincipalOf(playerFaction) == null)
            { if (heroPanel != null) heroPanel.Initialize(this); return; }
            Heroes = CampaignHeroRoster.Start(HeroFactions());
            registry = registry.PlaceStartingHeroes(Heroes, playerFaction, CapitalOf);
            if (heroPanel == null) heroPanel = gameObject.AddComponent<CampaignHeroPanel>();
            heroPanel.Initialize(this);
        }

        /// <summary>Restaura los héroes de una partida y repone en el mapa los que falten.</summary>
        private CampaignHeroRoster RestoreHeroRoster(CampaignSaveData payload, ref CampaignArmyRegistry restored)
        {
            if (Heroes == null) return null;
            var roster = payload.RestoreHeroes(HeroFactions());
            var pending = new HashSet<string>(roster.PendingFates.Select(p => p.HeroId), StringComparer.Ordinal);
            foreach (var unit in restored.Armies)
                if (unit.Army.HeroId != null && (!roster.TryGet(unit.Army.HeroId, out var hero) ||
                    hero.Status != HeroStatus.Active || pending.Contains(hero.HeroId)))
                    throw new ArgumentException("Saved army is led by an unavailable hero.");
            restored = restored.PlaceStartingHeroes(roster, playerFaction, CapitalOf);
            return roster;
        }

        private void IncludeHeroes(CampaignSaveData payload) { if (Heroes != null) payload.IncludeHeroes(Heroes); }

        /// <summary>Cobra el oro de los héroes activos del jugador y de los rivales al cerrar el turno.</summary>
        private CampaignTurnState ChargeHeroUpkeep(CampaignTurnState state, ref CampaignRivalState rivals)
        {
            if (Heroes == null) return state;
            foreach (var faction in rivals.Factions)
                rivals = rivals.WithResources(faction, rivals.Resources(faction).SubtractClamped(Heroes.Upkeep(faction)));
            return new CampaignTurnState(state.Turn, state.Resources.SubtractClamped(Heroes.Upkeep(playerFaction)),
                state.IncomePerTurn, state.ExpensesPerTurn);
        }

        /// <summary>
        /// Héroes cuya hueste ha desaparecido en un combate. Si vence el jugador, queda la decisión
        /// pendiente; si vence la IA, decide en el acto.
        /// </summary>
        private void RecordHeroDefeats(IReadOnlyList<string> lost, string attackerFaction, string defenderFaction)
        {
            if (Heroes == null || lost.Count == 0) return;
            foreach (var heroId in lost)
            {
                if (!Heroes.TryGet(heroId, out var hero) || hero.Status != HeroStatus.Active) continue;
                var victor = hero.FactionId == attackerFaction ? defenderFaction : attackerFaction;
                Heroes = Heroes.Defeat(heroId, victor);
                if (victor == playerFaction) LastHeroMessage = $"{hero.Definition.Name} ha caído: decide su destino.";
                else
                {
                    var fate = Heroes.SuggestFate(victor);
                    Heroes = Heroes.ResolveFate(heroId, victor, fate);
                    LastHeroMessage = $"{hero.Definition.Name} ha sido {(fate == HeroFate.Capture ? "apresado" : "ejecutado")} por {victor}.";
                }
            }
        }

        public bool ResolveHeroFate(string heroId, HeroFate fate)
        {
            if (Heroes == null || !PlayerHeroDecisions.Any(p => p.HeroId == heroId)) return false;
            Heroes.TryGet(heroId, out var hero);
            Heroes = Heroes.ResolveFate(heroId, playerFaction, fate);
            LastHeroMessage = $"{hero.Definition.Name} {(fate == HeroFate.Capture ? "queda prisionero" : "ha sido ejecutado")}.";
            panel?.SetSaveStatus(LastHeroMessage, true); RefreshArmy();
            return true;
        }

        /// <summary>Un héroe solo enemigo, contiguo: se le apresa sin batalla.</summary>
        private bool CaptureLoneHero(string attackerId, string targetId)
        {
            if (Heroes == null || !registry.TryCaptureLoneHero(attackerId, targetId, out var next, out var heroId, out _)) return false;
            registry.TryGetArmy(attackerId, out var attacker);
            Heroes = Heroes.CaptureUndefended(heroId, attacker.Army.FactionId);
            registry = next; marchOrders = marchOrders.Prune(registry, playerFaction);
            Heroes.TryGet(heroId, out var hero);
            LastHeroMessage = $"{hero.Definition.Name} ha caído prisionero sin combatir.";
            panel?.SetSaveStatus(LastHeroMessage, true);
            armyMarkers?.Initialize(selection, registry, playerFaction); UpdateConclusion(); RefreshArmy();
            return true;
        }

        private bool CanGiveHeroOrders => registry != null && Heroes != null && !HasPendingBattle && !IsRivalPhaseActive && !IsCampaignFinished;

        /// <summary>El héroe seleccionado recibe al batallón de la casilla elegida, o el batallón seleccionado se une al héroe.</summary>
        public bool TryAttachAtSelection(out string message)
        {
            message = "Selecciona un batallón propio contiguo al héroe.";
            if (!CanGiveHeroOrders || selection?.SelectedCell == null || !registry.TryGetArmy(selectedArmyId, out var own) ||
                !registry.TryGetOccupant(selection.SelectedCell.Value.Coordinates, out var other) || other.Army.FactionId != playerFaction)
                return false;
            var leader = own.Army.HeroId != null ? own : other;
            var battalion = leader == own ? other : own;
            if (!registry.TryAttach(leader.Army.Id, battalion.Army.Id, out var next, out var failure))
            { message = HeroOrderText(failure); return false; }
            registry = next; selectedArmyId = leader.Army.Id;
            marchOrders = marchOrders.Cancel(battalion.Army.Id).Cancel(leader.Army.Id);
            message = $"{battalion.Army.Id} se une a {leader.Army.Hero.Name}.";
            armyMarkers?.Initialize(selection, registry, playerFaction); RefreshArmy();
            return true;
        }

        /// <summary>Separa el último batallón del héroe seleccionado hacia la casilla contigua elegida.</summary>
        public bool TryDetachToSelection(out string message)
        {
            message = "Selecciona una casilla libre contigua al héroe.";
            if (!CanGiveHeroOrders || selection?.SelectedCell == null || !registry.TryGetArmy(selectedArmyId, out var own) ||
                own.Army.HeroId == null || own.Army.Formations.Count == 0) return false;
            var formation = own.Army.Formations.OrderBy(f => f.Id, StringComparer.Ordinal).Last();
            if (!registry.TryDetach(own.Army.Id, formation.Id, selection.SelectedCell.Value.Coordinates, out var next, out var failure))
            { message = HeroOrderText(failure); return false; }
            registry = next; marchOrders = marchOrders.Cancel(own.Army.Id);
            message = $"{formation.Id} se separa de {own.Army.Hero.Name}.";
            armyMarkers?.Initialize(selection, registry, playerFaction); RefreshArmy();
            return true;
        }

        /// <summary>Contrata al siguiente héroe en la ciudad propia seleccionada.</summary>
        public bool TryRecruitHeroAtSelection(out string message)
        {
            message = "Selecciona una ciudad propia para contratar un héroe.";
            if (!CanGiveHeroOrders || selection?.SelectedCell == null ||
                !SettlementState.TryGet(selection.SelectedCell.Value.Coordinates, out var city) || city.OwnerFactionId != playerFaction)
                return false;
            int cities = SettlementState.Settlements.Count(s => s.OwnerFactionId == playerFaction);
            if (!Heroes.TryRecruit(playerFaction, cities, State.Turn, State.Resources, out var roster, out var remaining, out var failure))
            { message = RecruitText(failure, Heroes.NextOffer(playerFaction)); return false; }
            var hero = roster.Heroes.First(h => h.FactionId == playerFaction && !Heroes.TryGet(h.HeroId, out _));
            if (!registry.TryPlaceHeroNear(hero.HeroId, city.Position, out var placed, out _))
            { message = "No hay sitio libre junto a la ciudad."; return false; }
            Heroes = roster; registry = placed; selectedArmyId = CampaignArmyRegistry.HeroArmyId(hero.HeroId);
            State = new CampaignTurnState(State.Turn, remaining, State.IncomePerTurn, State.ExpensesPerTurn);
            message = $"{hero.Definition.Name} se une a tu causa en {city.Name}.";
            armyMarkers?.Initialize(selection, registry, playerFaction); panel?.SetTurnState(State); RefreshArmy();
            return true;
        }

        /// <summary>Resumen del héroe seleccionado, de la oferta de contratación y de los cautivos.</summary>
        public string HeroSummary()
        {
            if (Heroes == null) return "";
            var lines = new List<string>();
            if (registry.TryGetArmy(selectedArmyId, out var own) && own.Army.Hero is { } hero)
                lines.Add($"{hero.Name} · {ArchetypeName(hero.Archetype)}\nMando {hero.Command} · Defensa {hero.Defense} · Estrategia {hero.Strategy} · {hero.CommandPoints} puntos de mando\n" +
                    $"{own.Army.Formations.Count}/6 batallones" + (own.Army.IsLoneHero ? " · va solo" : ""));
            else lines.Add("Selecciona un héroe o un batallón contiguo a él.");
            int living = Heroes.LivingCount(playerFaction);
            var offer = Heroes.NextOffer(playerFaction);
            lines.Add($"Héroes {living}/{CampaignHeroRoster.MaxLivingHeroes} · Mantenimiento {Heroes.Upkeep(playerFaction).Gold} oro/turno" +
                (offer == null || living >= CampaignHeroRoster.MaxLivingHeroes ? "" :
                    $"\nSiguiente: {offer.Cost.Gold} oro · {offer.RequiredCities} ciudades · turno {offer.RequiredTurn}"));
            var captives = Heroes.Heroes.Where(h => h.Status == HeroStatus.Captive && (h.FactionId == playerFaction || h.CaptorFactionId == playerFaction)).ToList();
            if (captives.Count > 0)
                lines.Add("Cautivos: " + string.Join(", ", captives.Select(h => h.Definition.Name + (h.FactionId == playerFaction ? " (tuyo)" : ""))));
            return string.Join("\n", lines);
        }

        public bool SelectedArmyHasHero => registry != null && registry.TryGetArmy(selectedArmyId, out var own) && own.Army.HeroId != null;

        internal static string ArchetypeName(HeroArchetype archetype) => archetype switch
        {
            HeroArchetype.FrontierKnight => "Caballero de frontera",
            HeroArchetype.Infanzon => "Infanzón",
            HeroArchetype.MasterCrossbowman => "Maestre ballestero",
            _ => "Alcaide",
        };

        private static string HeroOrderText(HeroOrderFailure failure) => failure switch
        {
            HeroOrderFailure.NotAdjacent => "Debe estar en una casilla contigua.",
            HeroOrderFailure.TooManyFormations => "El héroe ya manda seis batallones.",
            HeroOrderFailure.AlreadyLed => "Ese batallón ya sigue a otro héroe.",
            HeroOrderFailure.NoHero => "Ninguna de las dos huestes lleva héroe.",
            HeroOrderFailure.Occupied => "La casilla está ocupada o no es transitable.",
            HeroOrderFailure.ArmyInBattle => "Hay un combate pendiente.",
            _ => "Orden no disponible.",
        };

        private static string RecruitText(HeroRecruitFailure failure, HeroRecruitOffer offer) => failure switch
        {
            HeroRecruitFailure.Limit => $"Ya tienes {CampaignHeroRoster.MaxLivingHeroes} héroes vivos.",
            HeroRecruitFailure.NoCandidate => "No quedan héroes disponibles para tu reino.",
            HeroRecruitFailure.Cities => $"Necesitas {offer?.RequiredCities} ciudades propias.",
            HeroRecruitFailure.Turn => $"Disponible desde el turno {offer?.RequiredTurn}.",
            HeroRecruitFailure.Resources => $"Faltan recursos: {offer?.Cost.Gold} oro, {offer?.Cost.Grain} grano y {offer?.Cost.Labor} mano de obra.",
            _ => "Contratación no disponible.",
        };
    }
}
