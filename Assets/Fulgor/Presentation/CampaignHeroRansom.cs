using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Cautivos: pagar el rescate de un héroe propio, pedir rescate por uno ajeno e intercambiar cautivos cruzados.
    /// El héroe liberado vuelve activo y aparece junto a la primera ciudad de su reino.
    /// </summary>
    public sealed partial class CampaignTurnController
    {
        /// <summary>Héroes del jugador que retiene otro reino.</summary>
        public IReadOnlyList<CampaignHero> OwnCaptives => Heroes?.CaptivesOf(playerFaction) ?? Array.Empty<CampaignHero>();
        /// <summary>Héroes ajenos que retiene el jugador.</summary>
        public IReadOnlyList<CampaignHero> HeldCaptives => Heroes?.HeldBy(playerFaction) ?? Array.Empty<CampaignHero>();

        /// <summary>Paga al captor el rescate de un héroe propio.</summary>
        public bool TryPayRansom(string heroId, out string message)
        {
            message = "Rescate no disponible.";
            if (!CanGiveHeroOrders || !Heroes.TryGet(heroId, out var hero) || hero.Status != HeroStatus.Captive || hero.FactionId != playerFaction) return false;
            int gold = CampaignHeroRoster.RansomGold(hero);
            if (!State.Resources.TrySpend(new CampaignResources(0, 0, 0, gold, 0), out var remaining))
            { message = $"Faltan recursos: el rescate de {hero.Definition.Name} cuesta {gold} de oro."; return false; }
            if (!TryReturnHome(hero, Heroes.Release(heroId), out message)) return false;
            State = new CampaignTurnState(State.Turn, remaining, State.IncomePerTurn, State.ExpensesPerTurn);
            if (Rivals != null && Rivals.Factions.Contains(hero.CaptorFactionId))
                Rivals = Rivals.WithResources(hero.CaptorFactionId, Rivals.Resources(hero.CaptorFactionId).Add(new CampaignResources(0, 0, 0, gold, 0)));
            message = $"{hero.Definition.Name} vuelve a tu reino por {gold} de oro.";
            PublishHeroReturn(message); return true;
        }

        /// <summary>Pide al reino de un cautivo que pague su rescate. Acepta si su tesoro alcanza.</summary>
        public bool TryDemandRansom(string heroId, out string message)
        {
            message = "Rescate no disponible.";
            if (!CanGiveHeroOrders || !Heroes.TryGet(heroId, out var hero) || hero.Status != HeroStatus.Captive || hero.CaptorFactionId != playerFaction) return false;
            int gold = CampaignHeroRoster.RansomGold(hero);
            var price = new CampaignResources(0, 0, 0, gold, 0);
            if (Rivals == null || !Rivals.Factions.Contains(hero.FactionId) || !Rivals.Resources(hero.FactionId).TrySpend(price, out var theirs))
            { message = $"El reino de {hero.Definition.Name} no puede pagar {gold} de oro."; return false; }
            if (!TryReturnHome(hero, Heroes.Release(heroId), out message)) return false;
            Rivals = Rivals.WithResources(hero.FactionId, theirs);
            State = new CampaignTurnState(State.Turn, State.Resources.Add(price), State.IncomePerTurn, State.ExpensesPerTurn);
            message = $"Cobras {gold} de oro por liberar a {hero.Definition.Name}.";
            PublishHeroReturn(message); return true;
        }

        /// <summary>Intercambia un héroe propio cautivo por uno ajeno que retienes, si cada reino tiene al del otro.</summary>
        public bool TryExchangeCaptives(string ownHeroId, string heldHeroId, out string message)
        {
            message = "Intercambio no disponible.";
            if (!CanGiveHeroOrders || !Heroes.TryGet(ownHeroId, out var own) || !Heroes.TryGet(heldHeroId, out var held) ||
                own.FactionId != playerFaction || held.CaptorFactionId != playerFaction) return false;
            if (!Heroes.CanExchange(ownHeroId, heldHeroId))
            { message = $"{held.Definition.Name} no es del reino que retiene a {own.Definition.Name}."; return false; }
            var before = Heroes; var registryBefore = registry;
            if (!TryReturnHome(own, Heroes.Exchange(ownHeroId, heldHeroId), out message)) return false;
            if (!TryReturnHome(held, Heroes, out message)) { Heroes = before; registry = registryBefore; return false; }
            message = $"{own.Definition.Name} vuelve a tu reino a cambio de {held.Definition.Name}.";
            PublishHeroReturn(message); return true;
        }

        // El héroe reaparece junto a la primera ciudad de su reino; sin ciudad o sin sitio, el acuerdo no se cierra.
        private bool TryReturnHome(CampaignHero hero, CampaignHeroRoster released, out string message)
        {
            message = "";
            var capital = CapitalOf(hero.FactionId);
            if (!capital.HasValue) { message = $"El reino de {hero.Definition.Name} no tiene ciudad a la que volver."; return false; }
            if (!registry.TryPlaceHeroNear(hero.HeroId, capital.Value, out var placed, out _))
            { message = "No hay sitio libre junto a la ciudad de destino."; return false; }
            Heroes = released; registry = placed; return true;
        }

        private void PublishHeroReturn(string message)
        {
            LastHeroMessage = message;
            armyMarkers?.Initialize(selection, registry, playerFaction);
            panel?.SetTurnState(State); panel?.SetSaveStatus(message, true); RefreshArmy();
        }
    }
}
