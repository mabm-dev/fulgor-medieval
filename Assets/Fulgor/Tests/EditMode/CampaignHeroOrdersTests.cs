using System;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignHeroOrdersTests
    {
        private const string Rodrigo = "castilla/1", Zahir = "granada/1";
        private static HexCoordinates At(int q) => new(q, 0);
        private static readonly TroopProfileDefinition Spears = TroopCatalog.Get(TroopProfile.Spearmen);

        private static CampaignFormationState Unit(string id) =>
            new(id, Spears.Attributes, 20L * Spears.Attributes.HealthPerMember, Spears.Attributes.Morale);

        // Una fila de seis llanuras: castellanos en 1 y 2 (y un séptimo batallón en 3), granadinos en 5.
        private static CampaignArmyRegistry Registry(int castilianFormations = 1)
        {
            var graph = new MapRouteGraph(new GeneratedMap(6, 1, 3,
                Enumerable.Range(0, 6).Select(q => new MapCell(At(q), TerrainType.Plain, false)).ToArray()));
            var battalion = new CampaignArmyState("b", "castilla", Enumerable.Range(1, castilianFormations).Select(i => Unit("b-" + i)));
            var reserve = new CampaignArmyState("c", "castilla", new[] { Unit("c-1") });
            var enemy = new CampaignArmyState("e", "granada", new[] { Unit("e-1") });
            return new CampaignArmyRegistry(graph, new[] {
                new CampaignArmyPlacement(battalion, At(2), 4), new CampaignArmyPlacement(reserve, At(3), 6),
                new CampaignArmyPlacement(enemy, At(5), 6) });
        }

        private static CampaignArmyRegistry WithRodrigo(CampaignArmyRegistry registry, int position = 1)
        {
            Assert.That(registry.TryPlaceHero(Rodrigo, At(position), out var next, out var failure), Is.True, failure.ToString());
            return next;
        }

        [Test]
        public void ArmyWithOnlyAHeroIsALoneHeroAndNotDissolved()
        {
            var lone = new CampaignArmyState("hero", "castilla", Array.Empty<CampaignFormationState>(), Rodrigo);
            Assert.That(lone.IsLoneHero, Is.True);
            Assert.That(lone.IsDissolved, Is.False);
            Assert.That(lone.Hero.Name, Is.EqualTo("Rodrigo Díaz"));
            Assert.That(new CampaignArmyState("x", "castilla", Array.Empty<CampaignFormationState>()).IsDissolved, Is.True);
            Assert.Throws<ArgumentException>(() => new CampaignArmyState("x", "granada", Array.Empty<CampaignFormationState>(), Rodrigo));
            Assert.That(lone.WithFormations(new[] { Unit("u") }).HeroId, Is.EqualTo(Rodrigo));
        }

        [Test]
        public void HeroIsPlacedOnceOnAFreeCellWithFullMovement()
        {
            var registry = WithRodrigo(Registry());
            Assert.That(registry.TryGetHeroArmy(Rodrigo, out var hero), Is.True);
            Assert.That(hero.Army.Id, Is.EqualTo("hero/castilla/1"));
            Assert.That(hero.Movement.RemainingMovement, Is.EqualTo(CampaignArmyRegistry.LoneHeroMovement));
            Assert.That(registry.TryPlaceHero(Rodrigo, At(0), out _, out var twice), Is.False);
            Assert.That(twice, Is.EqualTo(HeroOrderFailure.HeroAlreadyOnMap));
            Assert.That(registry.TryPlaceHero(Zahir, At(2), out _, out var busy), Is.False);
            Assert.That(busy, Is.EqualTo(HeroOrderFailure.Occupied));
            Assert.That(registry.TryPlaceHero("nadie/1", At(0), out _, out var unknown), Is.False);
            Assert.That(unknown, Is.EqualTo(HeroOrderFailure.UnknownHero));
        }

        [Test]
        public void AdjacentBattalionJoinsTheHeroAndKeepsTheSlowerMovement()
        {
            var registry = WithRodrigo(Registry());
            Assert.That(registry.TryAttach("hero/castilla/1", "b", out var next, out var failure), Is.True, failure.ToString());
            Assert.That(next.TryGetArmy("b", out _), Is.False, "The battalion joins the hero's army.");
            next.TryGetArmy("hero/castilla/1", out var led);
            Assert.That(led.Army.Formations.Select(f => f.Id), Is.EqualTo(new[] { "b-1" }));
            Assert.That(led.Army.HeroId, Is.EqualTo(Rodrigo));
            Assert.That(led.Movement.Position, Is.EqualTo(At(1)));
            Assert.That(led.Movement.MovementPerTurn, Is.EqualTo(4));
            Assert.That(led.Movement.RemainingMovement, Is.EqualTo(4));
        }

        [Test]
        public void InvalidJoinsLeaveTheRegistryUnchanged()
        {
            var registry = WithRodrigo(Registry());
            Assert.That(registry.TryAttach("hero/castilla/1", "c", out var next, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroOrderFailure.NotAdjacent));
            Assert.That(next, Is.SameAs(registry));
            Assert.That(registry.TryAttach("b", "c", out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroOrderFailure.NoHero));

            var full = WithRodrigo(Registry(6));
            full.TryAttach("hero/castilla/1", "b", out full, out _);
            full.TryMove("c", At(2), out full, out _, out _);
            Assert.That(full.TryAttach("hero/castilla/1", "c", out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroOrderFailure.TooManyFormations));

            Assert.That(WithRodrigo(Registry(), 4).TryAttach("hero/castilla/1", "e", out _, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroOrderFailure.DifferentFaction));
        }

        [Test]
        public void DetachingSendsABattalionToAnAdjacentCellAndCanLeaveTheHeroAlone()
        {
            var registry = WithRodrigo(Registry(2));
            registry.TryAttach("hero/castilla/1", "b", out registry, out _);
            Assert.That(registry.TryDetach("hero/castilla/1", "b-1", At(3), out _, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(HeroOrderFailure.NotAdjacent));
            Assert.That(registry.TryDetach("hero/castilla/1", "b-1", At(2), out var next, out failure), Is.True, failure.ToString());
            next.TryGetArmy("split/b-1", out var split);
            Assert.That(split.Army.HeroId, Is.Null);
            Assert.That(split.Movement.Position, Is.EqualTo(At(2)));

            Assert.That(next.TryDetach("hero/castilla/1", "b-2", At(0), out next, out failure), Is.True, failure.ToString());
            next.TryGetArmy("hero/castilla/1", out var hero);
            Assert.That(hero.Army.IsLoneHero, Is.True);
            Assert.That(hero.Movement.MovementPerTurn, Is.EqualTo(CampaignArmyRegistry.LoneHeroMovement));
        }

        [Test]
        public void LoneHeroCannotAttackAndIsCapturedWithoutBattle()
        {
            var registry = WithRodrigo(Registry(), 4);
            Assert.That(registry.PreviewAttack("hero/castilla/1", "e"), Is.EqualTo(CampaignAttackFailure.NoTroops));
            Assert.That(registry.PreviewAttack("e", "hero/castilla/1"), Is.EqualTo(CampaignAttackFailure.UndefendedHero));
            Assert.That(registry.TryBeginAttack("x", "e", "hero/castilla/1", 1, out _, out _), Is.False);

            Assert.That(registry.TryCaptureLoneHero("e", "hero/castilla/1", out var next, out var heroId, out var failure), Is.True, failure.ToString());
            Assert.That(heroId, Is.EqualTo(Rodrigo));
            Assert.That(next.TryGetHeroArmy(Rodrigo, out _), Is.False);
            next.TryGetArmy("e", out var captor);
            Assert.That(captor.Movement.RemainingMovement, Is.Zero);
            Assert.That(CampaignArmyRegistry.HeroesLost(registry, next), Is.EqualTo(new[] { Rodrigo }));

            var roster = CampaignHeroRoster.Start(new[] { "castilla", "granada" }).CaptureUndefended(heroId, captor.Army.FactionId);
            roster.TryGet(Rodrigo, out var prisoner);
            Assert.That(prisoner.Status, Is.EqualTo(HeroStatus.Captive));
        }

        [Test]
        public void HeroEmpowersItsArmyAndCommandsTheBattle()
        {
            var registry = WithRodrigo(Registry(), 4);
            Assert.That(registry.TryAttach("hero/castilla/1", "c", out registry, out var joined), Is.True, joined.ToString());
            Assert.That(registry.TryBeginAttack("fight", "hero/castilla/1", "e", 7, out var fighting, out var failure), Is.True, failure.ToString());
            fighting.TryGetEncounter("fight", out var encounter);
            var deployed = encounter.Battle.Roster.GetFormation(0);
            Assert.That(deployed.Attributes.Attack, Is.EqualTo(Spears.Attributes.Attack + 4));
            Assert.That(encounter.Battle.AttackerHero.Hero.Id, Is.EqualTo(Rodrigo));
            Assert.That(encounter.Battle.DefenderHero, Is.Null);
            Assert.That(encounter.Battle.HeroOf(BattleSide.Attacker), Is.SameAs(encounter.Battle.AttackerHero));
        }

        [Test]
        public void DestroyedArmyLosesItsHeroForTheVictorToDecide()
        {
            var registry = WithRodrigo(Registry(), 4);
            Assert.That(registry.TryAttach("hero/castilla/1", "c", out registry, out var joined), Is.True, joined.ToString());
            registry.TryBeginAttack("fight", "e", "hero/castilla/1", 7, out var fighting, out _);
            fighting.TryGetEncounter("fight", out var encounter);
            var roster = encounter.Battle.Roster;
            foreach (var id in roster.FormationIds.Where(id => roster.GetFormation(id).Slot.Side == BattleSide.Defender))
                roster.TryApplyResolvedDamage(id, 100000, out _);
            var after = fighting.ResolveEncounter("fight");
            Assert.That(after.TryGetArmy("hero/castilla/1", out _), Is.False);
            Assert.That(CampaignArmyRegistry.HeroesLost(fighting, after), Is.EqualTo(new[] { Rodrigo }));
        }

        [Test]
        public void StartingHeroesGoAloneForThePlayerAndLeadTheRivalArmy()
        {
            var roster = CampaignHeroRoster.Start(new[] { "castilla", "granada" });
            var registry = Registry().PlaceStartingHeroes(roster, "castilla", faction => faction == "castilla" ? At(2) : (HexCoordinates?)null);
            Assert.That(registry.TryGetHeroArmy(Rodrigo, out var rodrigo), Is.True);
            Assert.That(rodrigo.Army.IsLoneHero, Is.True);
            Assert.That(rodrigo.Movement.Position.DistanceTo(At(2)), Is.EqualTo(1), "Nearest free cell to the occupied capital.");
            Assert.That(registry.TryGetHeroArmy(Zahir, out var zahir), Is.True);
            Assert.That(zahir.Army.Id, Is.EqualTo("e"), "The rival hero leads its first army.");

            var again = registry.PlaceStartingHeroes(roster, "castilla", _ => At(2));
            Assert.That(again.Armies.Count, Is.EqualTo(registry.Armies.Count), "Heroes already on the map are not duplicated.");
            var captive = roster.CaptureUndefended(Rodrigo, "granada");
            Assert.That(Registry().PlaceStartingHeroes(captive, "castilla", _ => At(0)).TryGetHeroArmy(Rodrigo, out _), Is.False);
        }

        [Test]
        public void LoneHeroesDoNotKeepAFactionAliveInConquest()
        {
            var graph = new MapRouteGraph(new GeneratedMap(6, 1, 3,
                Enumerable.Range(0, 6).Select(q => new MapCell(At(q), TerrainType.Plain, false)).ToArray()));
            var city = new CampaignSettlementRegistry(graph, new[] { new CampaignSettlement("fort", "Fuerte", "granada", At(5)) });
            var noCities = new CampaignSettlementRegistry(graph, Array.Empty<CampaignSettlement>());
            var troops = new CampaignArmyState("e", "granada", new[] { Unit("e-1") });

            // Solo queda el héroe del jugador: derrota, aunque siga en el mapa.
            var heroOnly = new CampaignArmyRegistry(graph, new[] { new CampaignArmyPlacement(troops, At(5), 6) });
            Assert.That(heroOnly.TryPlaceHero(Rodrigo, At(0), out heroOnly, out _), Is.True);
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest, heroOnly, city, "castilla"), Is.EqualTo(CampaignConclusion.Defeat));

            // Un rival reducido a un héroe solo, sin ciudades, ya está vencido.
            var rivalHero = new CampaignArmyRegistry(graph, new[] { new CampaignArmyPlacement(new CampaignArmyState("b", "castilla", new[] { Unit("b-1") }), At(2), 6) });
            Assert.That(rivalHero.TryPlaceHero(Zahir, At(4), out rivalHero, out _), Is.True);
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest, rivalHero, noCities, "castilla"), Is.EqualTo(CampaignConclusion.Victory));

            // Con una ciudad rival, la partida sigue.
            Assert.That(CampaignConclusionRules.Evaluate(CampaignObjectiveKind.Conquest, rivalHero, city, "castilla"), Is.EqualTo(CampaignConclusion.InProgress));
        }

        [Test]
        public void EmptyStringsFromJsonUtilityMeanNoHero()
        {
            // JsonUtility devuelve "" en lugar de null: una partida sin héroes debe cargar igual.
            var initial = Registry(2);
            var data = initial.Capture();
            foreach (var army in data.armies) { Assert.That(army.heroId, Is.Null); army.heroId = ""; }
            var restored = CampaignArmyRegistry.Restore(data, initial);
            Assert.That(restored.Armies.Count, Is.EqualTo(initial.Armies.Count));
            Assert.That(restored.Armies.All(a => a.Army.HeroId == null), Is.True);

            // Una partida con un héroe real conserva ese héroe y las huestes sin héroe siguen con "".
            var withHero = WithRodrigo(initial);
            var mixed = withHero.Capture();
            foreach (var army in mixed.armies) if (army.heroId == null) army.heroId = "";
            var again = CampaignArmyRegistry.Restore(mixed, initial);
            Assert.That(again.Armies.Count(a => a.Army.HeroId != null), Is.EqualTo(1));
            Assert.That(again.TryGetHeroArmy(Rodrigo, out _), Is.True);
        }

        [Test]
        public void SavedHeroArmiesLoneHeroesAndSplitsAreRestored()
        {
            var initial = Registry(2);
            var registry = WithRodrigo(initial);
            registry.TryAttach("hero/castilla/1", "b", out registry, out _);
            registry.TryDetach("hero/castilla/1", "b-1", At(2), out registry, out _);
            Assert.That(registry.TryPlaceHero(Zahir, At(4), out registry, out _), Is.True);

            var data = registry.Capture();
            Assert.That(data.armies.Single(a => a.id == "hero/castilla/1").heroId, Is.EqualTo(Rodrigo));
            var restored = CampaignArmyRegistry.Restore(data, initial);
            restored.TryGetArmy("hero/castilla/1", out var hero);
            Assert.That(hero.Army.HeroId, Is.EqualTo(Rodrigo));
            Assert.That(hero.Army.Formations.Select(f => f.Id), Is.EqualTo(new[] { "b-2" }));
            Assert.That(restored.TryGetArmy("split/b-1", out var split) && split.Army.FactionId == "castilla", Is.True);
            Assert.That(restored.TryGetArmy("hero/granada/1", out var lone) && lone.Army.IsLoneHero, Is.True);

            var stolen = registry.Capture();
            stolen.armies.Single(a => a.id == "split/b-1").formations[0].id = "e-1";
            Assert.Throws<ArgumentException>(() => CampaignArmyRegistry.Restore(stolen, initial), "Formations cannot change faction.");
            var cloned = registry.Capture();
            cloned.armies.Single(a => a.id == "hero/granada/1").heroId = Rodrigo;
            Assert.Throws<ArgumentException>(() => CampaignArmyRegistry.Restore(cloned, initial), "A hero cannot lead two armies.");
        }
    }
}
