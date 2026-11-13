using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public enum HeroStatus { Active, Captive, Dead }

    /// <summary>Destino de un héroe cuya hueste ha sido destruida; lo decide el vencedor.</summary>
    public enum HeroFate { Capture, Execute }

    public enum HeroRecruitFailure { None, UnknownFaction, Limit, NoCandidate, Cities, Turn, Resources }

    public sealed class CampaignHero
    {
        internal CampaignHero(string heroId, string factionId, HeroStatus status, string captorFactionId, int hiredTurn)
        { HeroId = heroId; FactionId = factionId; Status = status; CaptorFactionId = captorFactionId; HiredTurn = hiredTurn; }
        public string HeroId { get; }
        public string FactionId { get; }
        public HeroStatus Status { get; }
        /// <summary>Reino que lo retiene mientras está cautivo.</summary>
        public string CaptorFactionId { get; }
        public int HiredTurn { get; }
        public HeroDefinition Definition => HeroCatalog.Find(HeroId);
        internal CampaignHero With(HeroStatus status, string captor) => new(HeroId, FactionId, status, captor, HiredTurn);
    }

    /// <summary>Héroe vencido a la espera de que el vencedor elija apresarlo o ejecutarlo.</summary>
    public sealed class PendingHeroFate
    {
        internal PendingHeroFate(string heroId, string victorFactionId) { HeroId = heroId; VictorFactionId = victorFactionId; }
        public string HeroId { get; }
        public string VictorFactionId { get; }
    }

    /// <summary>Precio y condiciones del siguiente héroe de un reino.</summary>
    public sealed class HeroRecruitOffer
    {
        internal HeroRecruitOffer(HeroDefinition candidate, CampaignResources cost, int requiredCities, int requiredTurn)
        { Candidate = candidate; Cost = cost; RequiredCities = requiredCities; RequiredTurn = requiredTurn; }
        public HeroDefinition Candidate { get; }
        public CampaignResources Cost { get; }
        public int RequiredCities { get; }
        public int RequiredTurn { get; }
    }

    /// <summary>
    /// Héroes de todos los reinos. Inmutable: cada cambio devuelve un registro nuevo. Las cifras son
    /// provisionales y se concentran aquí para equilibrarlas.
    /// </summary>
    public sealed class CampaignHeroRoster
    {
        /// <summary>Máximo de héroes vivos por reino, activos o cautivos.</summary>
        public const int MaxLivingHeroes = 3;

        private readonly Dictionary<string, CampaignHero> heroes;
        private readonly Dictionary<string, PendingHeroFate> pending;
        private readonly Dictionary<string, int> hired;

        private CampaignHeroRoster(Dictionary<string, CampaignHero> heroes, Dictionary<string, PendingHeroFate> pending,
            Dictionary<string, int> hired)
        { this.heroes = heroes; this.pending = pending; this.hired = hired; }

        /// <summary>Cada reino empieza con su héroe principal activo y gratis.</summary>
        public static CampaignHeroRoster Start(IEnumerable<string> factionIds)
        {
            if (factionIds == null) throw new ArgumentNullException(nameof(factionIds));
            var heroes = new Dictionary<string, CampaignHero>(StringComparer.Ordinal);
            var hired = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var faction in factionIds.Distinct(StringComparer.Ordinal))
            {
                var principal = HeroCatalog.PrincipalOf(faction) ??
                    throw new ArgumentException("Faction without heroes: " + faction, nameof(factionIds));
                heroes.Add(principal.Id, new CampaignHero(principal.Id, faction, HeroStatus.Active, null, 1));
                hired.Add(faction, 1);
            }
            return new CampaignHeroRoster(heroes, new Dictionary<string, PendingHeroFate>(StringComparer.Ordinal), hired);
        }

        public IReadOnlyList<CampaignHero> Heroes =>
            heroes.Values.OrderBy(h => h.HeroId, StringComparer.Ordinal).ToList().AsReadOnly();
        public IReadOnlyList<PendingHeroFate> PendingFates =>
            pending.Values.OrderBy(p => p.HeroId, StringComparer.Ordinal).ToList().AsReadOnly();

        public bool TryGet(string heroId, out CampaignHero hero)
        { hero = null; return heroId != null && heroes.TryGetValue(heroId, out hero); }

        public IReadOnlyList<CampaignHero> ForFaction(string factionId) =>
            Heroes.Where(h => h.FactionId == factionId).ToList().AsReadOnly();

        public int LivingCount(string factionId) => heroes.Values.Count(h => h.FactionId == factionId && h.Status != HeroStatus.Dead);

        /// <summary>Héroes contratados en toda la partida, incluidos los muertos: encarece cada compra.</summary>
        public int HiredCount(string factionId) => factionId != null && hired.TryGetValue(factionId, out var count) ? count : 0;

        /// <summary>Oro por turno de los héroes activos: 3 el principal y 5 cada uno de los demás.</summary>
        public CampaignResources Upkeep(string factionId)
        {
            int gold = 0;
            foreach (var hero in heroes.Values)
                if (hero.FactionId == factionId && hero.Status == HeroStatus.Active)
                    gold = checked(gold + (hero.Definition?.IsPrincipal == true ? 3 : 5));
            return new CampaignResources(0, 0, 0, gold, 0);
        }

        /// <summary>
        /// Siguiente héroe disponible. El oro se duplica con cada contratación de la partida y el
        /// n-ésimo héroe vivo exige n ciudades propias y haber llegado al turno 4·(n-1)+1.
        /// </summary>
        public HeroRecruitOffer NextOffer(string factionId)
        {
            var candidate = HeroCatalog.ForFaction(factionId).FirstOrDefault(h => !heroes.ContainsKey(h.Id));
            if (candidate == null) return null;
            int previous = Math.Max(1, HiredCount(factionId));
            int gold = checked(120 << Math.Min(previous - 1, 20));
            var cost = new CampaignResources(checked(20 + 20 * previous), 0, 0, gold, checked(20 * previous));
            int position = LivingCount(factionId) + 1;
            return new HeroRecruitOffer(candidate, cost, position, 4 * (position - 1) + 1);
        }

        public HeroRecruitFailure CanRecruit(string factionId, int ownedCities, int turn, CampaignResources resources,
            out HeroRecruitOffer offer)
        {
            offer = null;
            if (string.IsNullOrWhiteSpace(factionId) || HeroCatalog.ForFaction(factionId).Count == 0) return HeroRecruitFailure.UnknownFaction;
            if (LivingCount(factionId) >= MaxLivingHeroes) return HeroRecruitFailure.Limit;
            offer = NextOffer(factionId);
            if (offer == null) return HeroRecruitFailure.NoCandidate;
            if (ownedCities < offer.RequiredCities) return HeroRecruitFailure.Cities;
            if (turn < offer.RequiredTurn) return HeroRecruitFailure.Turn;
            if (!resources.TrySpend(offer.Cost, out _)) return HeroRecruitFailure.Resources;
            return HeroRecruitFailure.None;
        }

        public bool TryRecruit(string factionId, int ownedCities, int turn, CampaignResources resources,
            out CampaignHeroRoster next, out CampaignResources remaining, out HeroRecruitFailure failure)
        {
            next = this; remaining = resources;
            failure = CanRecruit(factionId, ownedCities, turn, resources, out var offer);
            if (failure != HeroRecruitFailure.None) return false;
            resources.TrySpend(offer.Cost, out remaining);
            next = Copy();
            next.heroes.Add(offer.Candidate.Id, new CampaignHero(offer.Candidate.Id, factionId, HeroStatus.Active, null, turn));
            next.hired[factionId] = checked(HiredCount(factionId) + 1);
            return true;
        }

        /// <summary>Héroe solo atacado: no combate y cae prisionero sin decisión.</summary>
        public CampaignHeroRoster CaptureUndefended(string heroId, string captorFactionId)
        {
            var hero = RequireActive(heroId, captorFactionId);
            var next = Copy();
            next.heroes[heroId] = hero.With(HeroStatus.Captive, captorFactionId);
            return next;
        }

        /// <summary>Su hueste ha sido destruida: queda a la espera de que el vencedor decida.</summary>
        public CampaignHeroRoster Defeat(string heroId, string victorFactionId)
        {
            RequireActive(heroId, victorFactionId);
            if (pending.ContainsKey(heroId)) return this;
            var next = Copy();
            next.pending.Add(heroId, new PendingHeroFate(heroId, victorFactionId));
            return next;
        }

        public CampaignHeroRoster ResolveFate(string heroId, string victorFactionId, HeroFate fate)
        {
            if (!pending.TryGetValue(heroId ?? "", out var waiting) || waiting.VictorFactionId != victorFactionId)
                throw new InvalidOperationException("No pending decision for this hero and victor.");
            if (!Enum.IsDefined(typeof(HeroFate), fate)) throw new ArgumentOutOfRangeException(nameof(fate));
            var next = Copy();
            next.pending.Remove(heroId);
            next.heroes[heroId] = heroes[heroId].With(fate == HeroFate.Capture ? HeroStatus.Captive : HeroStatus.Dead,
                fate == HeroFate.Capture ? victorFactionId : null);
            return next;
        }

        /// <summary>
        /// Decisión de la IA vencedora: apresa para negociar después, salvo que ya retenga dos
        /// cautivos, en cuyo caso ejecuta.
        /// </summary>
        public HeroFate SuggestFate(string victorFactionId) =>
            heroes.Values.Count(h => h.Status == HeroStatus.Captive && h.CaptorFactionId == victorFactionId) >= 2 ?
                HeroFate.Execute : HeroFate.Capture;

        /// <summary>Rescate o liberación pactada: vuelve activo con su reino.</summary>
        public CampaignHeroRoster Release(string heroId)
        {
            if (!TryGet(heroId, out var hero) || hero.Status != HeroStatus.Captive)
                throw new InvalidOperationException("Only captive heroes can be released.");
            var next = Copy();
            next.heroes[heroId] = hero.With(HeroStatus.Active, null);
            return next;
        }

        /// <summary>Oro que cuesta rescatar a un cautivo. Cifra provisional: crece con el mando del héroe.</summary>
        public static int RansomGold(CampaignHero hero) => 60 + 20 * (hero?.Definition?.Command ?? 1);

        /// <summary>Héroes de un reino que otro retiene.</summary>
        public IReadOnlyList<CampaignHero> CaptivesOf(string factionId) =>
            Heroes.Where(h => h.Status == HeroStatus.Captive && h.FactionId == factionId).ToList().AsReadOnly();

        /// <summary>Héroes ajenos que un reino retiene.</summary>
        public IReadOnlyList<CampaignHero> HeldBy(string factionId) =>
            Heroes.Where(h => h.Status == HeroStatus.Captive && h.CaptorFactionId == factionId).ToList().AsReadOnly();

        /// <summary>Un intercambio exige dos cautivos cruzados: cada reino retiene al héroe del otro.</summary>
        public bool CanExchange(string heroId, string otherHeroId) =>
            TryGet(heroId, out var first) && TryGet(otherHeroId, out var second) &&
            first.Status == HeroStatus.Captive && second.Status == HeroStatus.Captive &&
            first.CaptorFactionId == second.FactionId && second.CaptorFactionId == first.FactionId;

        /// <summary>Intercambio de cautivos: los dos vuelven activos con su reino.</summary>
        public CampaignHeroRoster Exchange(string heroId, string otherHeroId)
        {
            if (!CanExchange(heroId, otherHeroId)) throw new InvalidOperationException("Only crossed captives can be exchanged.");
            return Release(heroId).Release(otherHeroId);
        }

        public CampaignHeroRosterData Capture() => new() {
            heroes = Heroes.Select(h => new CampaignHeroData {
                heroId = h.HeroId, status = h.Status, captorFactionId = h.CaptorFactionId, hiredTurn = h.HiredTurn }).ToArray(),
            pending = PendingFates.Select(p => new PendingHeroFateData { heroId = p.HeroId, victorFactionId = p.VictorFactionId }).ToArray(),
            hired = hired.OrderBy(h => h.Key, StringComparer.Ordinal)
                .Select(h => new HeroHireCountData { factionId = h.Key, count = h.Value }).ToArray() };

        /// <summary>Reconstruye el registro validando héroes, reinos, cautivos y decisiones pendientes.</summary>
        /// <param name="captorFactionIds">Reinos que pueden retener o vencer a un héroe aunque no tengan héroes propios; por omisión, los de <paramref name="factionIds"/>.</param>
        public static CampaignHeroRoster Restore(CampaignHeroRosterData data, IEnumerable<string> factionIds, IEnumerable<string> captorFactionIds = null)
        {
            if (data?.heroes == null || data.pending == null || data.hired == null || factionIds == null)
                throw new ArgumentException("Missing hero state.");
            var factions = new HashSet<string>(factionIds, StringComparer.Ordinal);
            var captors = new HashSet<string>(captorFactionIds ?? factions, StringComparer.Ordinal); captors.UnionWith(factions);
            var heroes = new Dictionary<string, CampaignHero>(StringComparer.Ordinal);
            foreach (var saved in data.heroes)
            {
                var definition = HeroCatalog.Find(saved?.heroId);
                if (definition == null || !factions.Contains(definition.FactionId) || heroes.ContainsKey(definition.Id) ||
                    !Enum.IsDefined(typeof(HeroStatus), saved.status) || saved.hiredTurn < 1 ||
                    (saved.status == HeroStatus.Captive) != (saved.Captor != null) ||
                    saved.Captor != null && (saved.Captor == definition.FactionId || !captors.Contains(saved.Captor)))
                    throw new ArgumentException("Invalid saved hero.");
                heroes.Add(definition.Id, new CampaignHero(definition.Id, definition.FactionId, saved.status, saved.Captor, saved.hiredTurn));
            }
            var hired = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var count in data.hired)
                if (count == null || !factions.Contains(count.factionId) || !hired.TryAdd(count.factionId, count.count) ||
                    count.count < heroes.Values.Count(h => h.FactionId == count.factionId))
                    throw new ArgumentException("Invalid hero hiring count.");
            foreach (var faction in factions)
                if (heroes.Values.Count(h => h.FactionId == faction && h.Status != HeroStatus.Dead) > MaxLivingHeroes)
                    throw new ArgumentException("Too many living heroes.");
            var pending = new Dictionary<string, PendingHeroFate>(StringComparer.Ordinal);
            foreach (var fate in data.pending)
                if (fate == null || !heroes.TryGetValue(fate.heroId ?? "", out var hero) || hero.Status != HeroStatus.Active ||
                    fate.victorFactionId == hero.FactionId || !captors.Contains(fate.victorFactionId) ||
                    !pending.TryAdd(hero.HeroId, new PendingHeroFate(hero.HeroId, fate.victorFactionId)))
                    throw new ArgumentException("Invalid pending hero decision.");
            return new CampaignHeroRoster(heroes, pending, hired);
        }

        private CampaignHero RequireActive(string heroId, string opponentFactionId)
        {
            if (!TryGet(heroId, out var hero) || hero.Status != HeroStatus.Active)
                throw new InvalidOperationException("Only active heroes can be defeated.");
            if (string.IsNullOrWhiteSpace(opponentFactionId) || opponentFactionId == hero.FactionId)
                throw new ArgumentException("The victor must be another faction.", nameof(opponentFactionId));
            return hero;
        }

        private CampaignHeroRoster Copy() => new(
            new Dictionary<string, CampaignHero>(heroes, StringComparer.Ordinal),
            new Dictionary<string, PendingHeroFate>(pending, StringComparer.Ordinal),
            new Dictionary<string, int>(hired, StringComparer.Ordinal));
    }

    [Serializable] public sealed class CampaignHeroData
    {
        public string heroId, captorFactionId; public HeroStatus status; public int hiredTurn;
        /// <summary>JsonUtility devuelve "" en lugar de null: vacío y nulo significan «sin captor».</summary>
        public string Captor => string.IsNullOrEmpty(captorFactionId) ? null : captorFactionId;
    }
    [Serializable] public sealed class PendingHeroFateData { public string heroId, victorFactionId; }
    [Serializable] public sealed class HeroHireCountData { public string factionId; public int count; }
    [Serializable] public sealed class CampaignHeroRosterData
    {
        public CampaignHeroData[] heroes;
        public PendingHeroFateData[] pending;
        public HeroHireCountData[] hired;
    }

    public sealed partial class CampaignSaveData
    {
        /// <summary>0 en partidas anteriores a los héroes: se empieza con los principales.</summary>
        public int heroesVersion; public CampaignHeroRosterData heroes;
        public void IncludeHeroes(CampaignHeroRoster roster)
        { if (roster == null) throw new ArgumentNullException(nameof(roster)); heroesVersion = 1; heroes = roster.Capture(); }
        public CampaignHeroRoster RestoreHeroes(IEnumerable<string> factions, IEnumerable<string> captors = null)
        {
            if (heroesVersion == 0) return CampaignHeroRoster.Start(factions);
            if (heroesVersion != 1 || heroes == null) throw new ArgumentException("Missing or unsupported hero state.");
            return CampaignHeroRoster.Restore(heroes, factions, captors);
        }
    }
}
