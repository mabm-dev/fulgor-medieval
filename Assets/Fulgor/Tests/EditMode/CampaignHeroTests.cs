using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignHeroTests
    {
        private static readonly string[] Factions = { "castilla", "granada" };
        private static readonly CampaignResources Rich = new(1000, 1000, 1000, 5000, 1000);

        [Test]
        public void EveryPlayableKingdomHasAPrincipalAndTwoMoreHeroes()
        {
            var kingdoms = KingdomDesignCatalog.All.Where(k => !k.IsCandidate).Select(k => k.Id).ToArray();
            Assert.That(kingdoms, Has.Length.EqualTo(12));
            foreach (var kingdom in kingdoms)
            {
                var heroes = HeroCatalog.ForFaction(kingdom);
                Assert.That(heroes, Has.Count.EqualTo(HeroCatalog.HeroesPerFaction), kingdom);
                Assert.That(heroes[0].IsPrincipal, Is.True, kingdom);
                Assert.That(heroes.Skip(1).All(h => !h.IsPrincipal && h.IsProvisional), Is.True, kingdom);
            }
            Assert.That(HeroCatalog.PrincipalOf("castilla").Name, Is.EqualTo("Rodrigo Díaz"));
            Assert.That(HeroCatalog.All.Select(h => h.Id).Distinct().Count(), Is.EqualTo(HeroCatalog.All.Count));
        }

        [Test]
        public void ArchetypesGiveHeroStrikePlusTwoOrders()
        {
            foreach (HeroArchetype archetype in Enum.GetValues(typeof(HeroArchetype)))
            {
                var abilities = HeroCatalog.AbilitiesFor(archetype);
                Assert.That(abilities, Has.Count.EqualTo(3));
                Assert.That(abilities[0], Is.EqualTo(HeroAbility.HeroStrike));
            }
            var rodrigo = HeroCatalog.PrincipalOf("castilla");
            Assert.That(rodrigo.Abilities, Is.EqualTo(new[] { HeroAbility.HeroStrike, HeroAbility.FrontalCharge, HeroAbility.WarCry }));
            Assert.That(rodrigo.CommandPoints, Is.EqualTo(6));
        }

        [Test]
        public void GameStartsWithOnlyThePrincipalHeroOfEachFaction()
        {
            var roster = CampaignHeroRoster.Start(Factions);
            Assert.That(roster.Heroes.Select(h => h.HeroId), Is.EqualTo(new[] { "castilla/1", "granada/1" }));
            Assert.That(roster.Heroes.All(h => h.Status == HeroStatus.Active), Is.True);
            Assert.That(roster.Upkeep("castilla"), Is.EqualTo(new CampaignResources(0, 0, 0, 3, 0)));
            Assert.Throws<ArgumentException>(() => CampaignHeroRoster.Start(new[] { "visigodos" }));
        }

        [Test]
        public void EachNewHeroCostsDoubleAndNeedsMoreCitiesAndTurns()
        {
            var roster = CampaignHeroRoster.Start(Factions);
            var second = roster.NextOffer("castilla");
            Assert.That(second.Candidate.Id, Is.EqualTo("castilla/2"));
            Assert.That(second.Cost, Is.EqualTo(new CampaignResources(40, 0, 0, 120, 20)));
            Assert.That(second.RequiredCities, Is.EqualTo(2));
            Assert.That(second.RequiredTurn, Is.EqualTo(5));

            Assert.That(roster.CanRecruit("castilla", 1, 9, Rich, out _), Is.EqualTo(HeroRecruitFailure.Cities));
            Assert.That(roster.CanRecruit("castilla", 2, 4, Rich, out _), Is.EqualTo(HeroRecruitFailure.Turn));
            Assert.That(roster.CanRecruit("castilla", 2, 9, new CampaignResources(40, 0, 0, 119, 20), out _),
                Is.EqualTo(HeroRecruitFailure.Resources));

            Assert.That(roster.TryRecruit("castilla", 2, 5, Rich, out var next, out var left, out var failure), Is.True);
            Assert.That(failure, Is.EqualTo(HeroRecruitFailure.None));
            Assert.That(left, Is.EqualTo(new CampaignResources(960, 1000, 1000, 4880, 980)));
            Assert.That(next.LivingCount("castilla"), Is.EqualTo(2));
            Assert.That(next.Upkeep("castilla"), Is.EqualTo(new CampaignResources(0, 0, 0, 8, 0)));
            Assert.That(roster.LivingCount("castilla"), Is.EqualTo(1), "The previous roster is immutable.");

            var third = next.NextOffer("castilla");
            Assert.That(third.Cost.Gold, Is.EqualTo(240));
            Assert.That(third.RequiredCities, Is.EqualTo(3));
            Assert.That(third.RequiredTurn, Is.EqualTo(9));
            Assert.That(next.TryRecruit("castilla", 3, 9, Rich, out var full, out _, out _), Is.True);
            Assert.That(full.CanRecruit("castilla", 9, 99, Rich, out _), Is.EqualTo(HeroRecruitFailure.Limit));
        }

        [Test]
        public void ExecutedHeroesKeepPricesRisingAndAreNotOfferedAgain()
        {
            var roster = CampaignHeroRoster.Start(Factions);
            roster.TryRecruit("castilla", 2, 5, Rich, out roster, out _, out _);
            roster = roster.Defeat("castilla/2", "granada").ResolveFate("castilla/2", "granada", HeroFate.Execute);
            Assert.That(roster.TryGet("castilla/2", out var dead) && dead.Status == HeroStatus.Dead, Is.True);
            Assert.That(roster.LivingCount("castilla"), Is.EqualTo(1));

            var offer = roster.NextOffer("castilla");
            Assert.That(offer.Candidate.Id, Is.EqualTo("castilla/3"));
            Assert.That(offer.Cost.Gold, Is.EqualTo(240), "Price follows heroes hired, not heroes alive.");
            Assert.That(offer.RequiredCities, Is.EqualTo(2));

            roster.TryRecruit("castilla", 2, 5, Rich, out roster, out _, out _);
            Assert.That(roster.NextOffer("castilla"), Is.Null, "No candidates remain.");
            Assert.That(roster.CanRecruit("castilla", 3, 99, Rich, out _), Is.EqualTo(HeroRecruitFailure.NoCandidate));
        }

        [Test]
        public void VictorDecidesBetweenCaptureAndExecution()
        {
            var roster = CampaignHeroRoster.Start(Factions).Defeat("granada/1", "castilla");
            Assert.That(roster.PendingFates.Single().VictorFactionId, Is.EqualTo("castilla"));
            Assert.That(roster.TryGet("granada/1", out var waiting) && waiting.Status == HeroStatus.Active, Is.True);
            Assert.Throws<InvalidOperationException>(() => roster.ResolveFate("granada/1", "leon", HeroFate.Capture));

            var captured = roster.ResolveFate("granada/1", "castilla", HeroFate.Capture);
            captured.TryGet("granada/1", out var prisoner);
            Assert.That(prisoner.Status, Is.EqualTo(HeroStatus.Captive));
            Assert.That(prisoner.CaptorFactionId, Is.EqualTo("castilla"));
            Assert.That(captured.PendingFates, Is.Empty);
            Assert.That(captured.Upkeep("granada").Gold, Is.Zero, "Captives cost no upkeep.");
            Assert.That(captured.LivingCount("granada"), Is.EqualTo(1), "Captives still count towards the limit.");

            var released = captured.Release("granada/1");
            released.TryGet("granada/1", out var free);
            Assert.That(free.Status, Is.EqualTo(HeroStatus.Active));
            Assert.That(free.CaptorFactionId, Is.Null);
        }

        [Test]
        public void UndefendedHeroIsTakenPrisonerWithoutDecision()
        {
            var roster = CampaignHeroRoster.Start(Factions).CaptureUndefended("castilla/1", "granada");
            roster.TryGet("castilla/1", out var hero);
            Assert.That(hero.Status, Is.EqualTo(HeroStatus.Captive));
            Assert.That(roster.PendingFates, Is.Empty);
            Assert.Throws<ArgumentException>(() => CampaignHeroRoster.Start(Factions).CaptureUndefended("castilla/1", "castilla"));
        }

        [Test]
        public void AiCapturesUntilItHoldsTwoPrisoners()
        {
            var roster = CampaignHeroRoster.Start(new[] { "castilla", "granada", "leon", "aragon" });
            Assert.That(roster.SuggestFate("castilla"), Is.EqualTo(HeroFate.Capture));
            roster = roster.CaptureUndefended("granada/1", "castilla").CaptureUndefended("leon/1", "castilla");
            Assert.That(roster.SuggestFate("castilla"), Is.EqualTo(HeroFate.Execute));
            Assert.That(roster.SuggestFate("aragon"), Is.EqualTo(HeroFate.Capture));
        }

        [Test]
        public void SavedRosterKeepsStatusesHiresAndPendingDecisions()
        {
            var roster = CampaignHeroRoster.Start(Factions);
            roster.TryRecruit("castilla", 2, 5, Rich, out roster, out _, out _);
            roster = roster.CaptureUndefended("granada/1", "castilla").Defeat("castilla/2", "granada");

            var save = new CampaignSaveData();
            save.IncludeHeroes(roster);
            var restored = save.RestoreHeroes(Factions);
            Assert.That(restored.Heroes.Select(h => (h.HeroId, h.Status, h.CaptorFactionId)),
                Is.EqualTo(roster.Heroes.Select(h => (h.HeroId, h.Status, h.CaptorFactionId))));
            Assert.That(restored.HiredCount("castilla"), Is.EqualTo(2));
            Assert.That(restored.PendingFates.Single().HeroId, Is.EqualTo("castilla/2"));
            Assert.That(restored.NextOffer("castilla").Cost.Gold, Is.EqualTo(240));

            Assert.That(new CampaignSaveData().RestoreHeroes(Factions).Heroes, Has.Count.EqualTo(2),
                "Saves made before heroes start with the principal heroes.");
        }

        [Test]
        public void TamperedHeroSavesAreRejected()
        {
            CampaignHeroRosterData Data() => CampaignHeroRoster.Start(Factions).CaptureUndefended("granada/1", "castilla").Capture();
            var foreign = Data(); foreign.heroes[0].heroId = "leon/1";
            var freed = Data(); freed.heroes.Single(h => h.heroId == "granada/1").captorFactionId = null;
            var cheap = Data(); cheap.hired[0].count = 0;
            var self = Data(); self.heroes.Single(h => h.heroId == "granada/1").captorFactionId = "granada";
            foreach (var data in new[] { foreign, freed, cheap, self })
                Assert.Throws<ArgumentException>(() => CampaignHeroRoster.Restore(data, Factions));
        }

        [Test]
        public void HeroActsOncePerRoundAndPaysCommandPoints()
        {
            var roster = Battle();
            var rodrigo = new BattleHeroCommand(HeroCatalog.PrincipalOf("castilla"), BattleSide.Attacker);
            var before = roster.GetFormation(1).TotalHealth;

            Assert.That(rodrigo.TryUse(HeroAbility.FrontalCharge, 1, roster, out var charge, out var failure), Is.True, failure.ToString());
            Assert.That(charge.Damage, Is.EqualTo(125));
            Assert.That(roster.GetFormation(1).TotalHealth, Is.EqualTo(before - 125));
            Assert.That(charge.PointsLeft, Is.EqualTo(3));
            Assert.That(rodrigo.HasActed(roster), Is.True);
            Assert.That(roster.ActiveFormationId, Is.Not.Null, "Hero actions do not consume activations.");

            Assert.That(rodrigo.TryUse(HeroAbility.HeroStrike, 1, roster, out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroCommandFailure.AlreadyActedThisRound));
        }

        [Test]
        public void HeroRejectsUnknownOrdersFriendlyTargetsAndMissingPoints()
        {
            var roster = Battle();
            var rodrigo = new BattleHeroCommand(HeroCatalog.PrincipalOf("castilla"), BattleSide.Attacker);
            Assert.That(rodrigo.CanUse(HeroAbility.ArrowRain, roster, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroCommandFailure.UnknownAbility));
            Assert.That(rodrigo.TryUse(HeroAbility.FrontalCharge, 0, roster, out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroCommandFailure.InvalidTarget));
            Assert.That(rodrigo.CommandPoints, Is.EqualTo(6), "A failed order costs nothing.");

            var weak = new BattleHeroCommand(HeroCatalog.Find("castilla/2"), BattleSide.Attacker);
            Assert.That(weak.CommandPoints, Is.EqualTo(5));
            Assert.That(HeroCatalog.Ability(HeroAbility.HeroStrike).Cost, Is.EqualTo(1));
        }

        [Test]
        public void AreaOrdersAffectEveryFormationOfTheirSide()
        {
            var roster = Battle();
            var zahir = new BattleHeroCommand(HeroCatalog.PrincipalOf("granada"), BattleSide.Defender);
            Assert.That(zahir.TryUse(HeroAbility.ArrowRain, null, roster, out var rain, out _), Is.True);
            Assert.That(rain.Targets, Is.EqualTo(new[] { 0 }));
            Assert.That(rain.Damage, Is.EqualTo(18));
        }

        [Test]
        public void MoraleAndHealingOrdersStayWithinLimits()
        {
            var roster = Battle();
            roster.TryApplyResolvedDamage(0, 100, out _);
            var wounded = roster.GetFormation(0);

            var alcaide = new BattleHeroCommand(HeroCatalog.PrincipalOf("leon"), BattleSide.Attacker);
            Assert.That(alcaide.TryUse(HeroAbility.HoldTheLine, null, roster, out var heal, out _), Is.True);
            Assert.That(heal.Healed, Is.EqualTo(30));
            Assert.That(roster.GetFormation(0).TotalHealth, Is.EqualTo(wounded.TotalHealth + 30));

            var knight = new BattleHeroCommand(HeroCatalog.PrincipalOf("castilla"), BattleSide.Attacker);
            var morale = roster.GetFormation(0).Morale;
            Assert.That(knight.TryUse(HeroAbility.WarCry, null, roster, out var cry, out _), Is.True);
            Assert.That(roster.GetFormation(0).Morale, Is.EqualTo(Math.Min(100, morale + 15)));
            Assert.That(cry.MoraleChange, Is.EqualTo(roster.GetFormation(0).Morale - morale));
        }

        [Test]
        public void HeroBonusesRaiseAttackDefenseAndInitiativeOnly()
        {
            var troop = TroopCatalog.Get(TroopProfile.MenAtArms).Attributes;
            var rodrigo = HeroCatalog.PrincipalOf("castilla");
            var empowered = BattleHeroCommand.Empower(troop, rodrigo);
            Assert.That(empowered.Attack, Is.EqualTo(troop.Attack + 4));
            Assert.That(empowered.Defense, Is.EqualTo(troop.Defense + 2));
            Assert.That(empowered.Initiative, Is.EqualTo(troop.Initiative + 2));
            Assert.That(empowered.HealthPerMember, Is.EqualTo(troop.HealthPerMember));
            Assert.That(empowered.Morale, Is.EqualTo(troop.Morale));
            Assert.That(BattleHeroCommand.Empower(troop, null), Is.SameAs(troop));
        }

        private static BattleRoster Battle()
        {
            var infantry = TroopCatalog.Get(TroopProfile.MenAtArms);
            return new BattleRoster(new[] {
                new BattleFormationState(new BattleFormationSlot(0, BattleSide.Attacker, FormationKind.Infantry, 4, 1), infantry.Attributes, 40),
                new BattleFormationState(new BattleFormationSlot(1, BattleSide.Defender, FormationKind.Infantry, 4, 11), infantry.Attributes, 40) });
        }
    }
}
