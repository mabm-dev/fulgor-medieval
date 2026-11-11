using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    /// <summary>Arquetipos heredados del prototipo web; cada uno aporta dos órdenes propias.</summary>
    public enum HeroArchetype { FrontierKnight, Infanzon, MasterCrossbowman, Alcaide }

    /// <summary>Acciones del héroe desde fuera del campo de batalla, pagadas con puntos de mando.</summary>
    public enum HeroAbility { HeroStrike, FrontalCharge, WarCry, Harass, Flank, ArrowRain, PreciseShot, HoldTheLine, Regroup }

    public enum HeroAbilityTarget { Enemy, Ally, AllEnemies, AllAllies }

    public sealed class HeroAbilityDefinition
    {
        internal HeroAbilityDefinition(HeroAbility ability, string name, int cost, HeroAbilityTarget target, string description)
        { Ability = ability; Name = name; Cost = cost; Target = target; Description = description; }
        public HeroAbility Ability { get; }
        public string Name { get; }
        public int Cost { get; }
        public HeroAbilityTarget Target { get; }
        public string Description { get; }
        public bool NeedsTarget => Target == HeroAbilityTarget.Enemy || Target == HeroAbilityTarget.Ally;
    }

    /// <summary>
    /// Héroe jugable. Mando, defensa y estrategia usan la escala de 1 a 5 de los líderes; el héroe
    /// no ocupa casilla en la batalla: suma bonificaciones a su hueste y actúa una vez por ronda.
    /// </summary>
    public sealed class HeroDefinition
    {
        internal HeroDefinition(string id, string name, string factionId, HeroArchetype archetype,
            int command, int defense, int strategy, bool principal, bool provisional)
        {
            if (command < 1 || command > 5) throw new ArgumentOutOfRangeException(nameof(command));
            if (defense < 1 || defense > 5) throw new ArgumentOutOfRangeException(nameof(defense));
            if (strategy < 1 || strategy > 5) throw new ArgumentOutOfRangeException(nameof(strategy));
            Id = id; Name = name; FactionId = factionId; Archetype = archetype;
            Command = command; Defense = defense; Strategy = strategy;
            IsPrincipal = principal; IsProvisional = provisional;
        }

        public string Id { get; }
        public string Name { get; }
        public string FactionId { get; }
        public HeroArchetype Archetype { get; }
        public int Command { get; }
        public int Defense { get; }
        public int Strategy { get; }
        public bool IsPrincipal { get; }
        /// <summary>Nombre y atributos pendientes de la sesión de diseño.</summary>
        public bool IsProvisional { get; }

        /// <summary>Puntos de mando al empezar cada batalla.</summary>
        public int CommandPoints => 2 + Strategy;
        public int AttackBonus => Command - 1;
        public int DefenseBonus => Defense - 1;
        public int InitiativeBonus => Strategy / 2;
        public IReadOnlyList<HeroAbility> Abilities => HeroCatalog.AbilitiesFor(Archetype);
    }

    public static class HeroCatalog
    {
        public const int HeroesPerFaction = 3;

        private static readonly HeroAbilityDefinition[] AbilityTable = {
            new(HeroAbility.HeroStrike, "Golpe del héroe", 1, HeroAbilityTarget.Enemy, "Daño directo a un batallón enemigo."),
            new(HeroAbility.FrontalCharge, "Carga frontal", 3, HeroAbilityTarget.Enemy, "Gran daño a un batallón enemigo."),
            new(HeroAbility.WarCry, "Grito de guerra", 2, HeroAbilityTarget.AllAllies, "Sube la moral de todos tus batallones."),
            new(HeroAbility.Harass, "Hostigar", 2, HeroAbilityTarget.Enemy, "Daño y pérdida de moral a un batallón enemigo."),
            new(HeroAbility.Flank, "Envolver el flanco", 3, HeroAbilityTarget.Enemy, "Hunde la moral de un batallón enemigo y lo daña."),
            new(HeroAbility.ArrowRain, "Lluvia de proyectiles", 3, HeroAbilityTarget.AllEnemies, "Daño a todos los batallones enemigos."),
            new(HeroAbility.PreciseShot, "Tiro preciso", 2, HeroAbilityTarget.Enemy, "Mucho daño a un batallón enemigo."),
            new(HeroAbility.HoldTheLine, "Mantener la línea", 2, HeroAbilityTarget.AllAllies, "Cura heridas de todos tus batallones."),
            new(HeroAbility.Regroup, "Reagrupar", 2, HeroAbilityTarget.Ally, "Devuelve mucha moral a un batallón propio.") };

        private static readonly Dictionary<HeroArchetype, HeroAbility[]> ByArchetype = new() {
            [HeroArchetype.FrontierKnight] = new[] { HeroAbility.HeroStrike, HeroAbility.FrontalCharge, HeroAbility.WarCry },
            [HeroArchetype.Infanzon] = new[] { HeroAbility.HeroStrike, HeroAbility.Harass, HeroAbility.Flank },
            [HeroArchetype.MasterCrossbowman] = new[] { HeroAbility.HeroStrike, HeroAbility.ArrowRain, HeroAbility.PreciseShot },
            [HeroArchetype.Alcaide] = new[] { HeroAbility.HeroStrike, HeroAbility.HoldTheLine, HeroAbility.Regroup } };

        // Principales: los héroes de la pantalla Elige tu reino. Los otros dos de cada reino son provisionales.
        public static IReadOnlyList<HeroDefinition> All { get; } = Array.AsReadOnly(new[] {
            Principal("asturias", "Conde Gatón del Bierzo", HeroArchetype.Alcaide, 3, 4, 3),
            Principal("leon", "Pedro Ansúrez", HeroArchetype.Alcaide, 3, 3, 4),
            Principal("castilla", "Rodrigo Díaz", HeroArchetype.FrontierKnight, 5, 3, 4),
            Principal("aragon", "Blasco de Alagón", HeroArchetype.FrontierKnight, 4, 3, 3),
            Principal("navarra", "Aznar de Roncal", HeroArchetype.Infanzon, 4, 4, 3),
            Principal("portugal", "Geraldo Sem Pavor", HeroArchetype.Infanzon, 4, 2, 4),
            Principal("cordoba", "General Ghalib", HeroArchetype.FrontierKnight, 4, 3, 4),
            Principal("sevilla", "Ibn Ammar", HeroArchetype.Alcaide, 2, 3, 5),
            Principal("zaragoza", "Héroe de Zaragoza", HeroArchetype.MasterCrossbowman, 3, 3, 3, provisional: true),
            Principal("almoravides", "Sir ibn Abi Bakr", HeroArchetype.FrontierKnight, 5, 3, 3),
            Principal("almohades", "Abu Yahya ibn Abi Hafs", HeroArchetype.Alcaide, 3, 4, 4),
            Principal("granada", "Zahir", HeroArchetype.MasterCrossbowman, 3, 4, 3) }
            .SelectMany(p => new[] { p, Extra(p, 2), Extra(p, 3) }).ToArray());

        public static HeroDefinition Find(string id) => All.FirstOrDefault(h => string.Equals(h.Id, id, StringComparison.Ordinal));

        /// <summary>Héroes del reino en orden de contratación: el principal primero.</summary>
        public static IReadOnlyList<HeroDefinition> ForFaction(string factionId) =>
            All.Where(h => string.Equals(h.FactionId, factionId, StringComparison.Ordinal)).ToList().AsReadOnly();

        public static HeroDefinition PrincipalOf(string factionId) => ForFaction(factionId).FirstOrDefault(h => h.IsPrincipal);

        public static HeroAbilityDefinition Ability(HeroAbility ability) =>
            AbilityTable.FirstOrDefault(a => a.Ability == ability) ?? throw new ArgumentOutOfRangeException(nameof(ability));

        public static IReadOnlyList<HeroAbility> AbilitiesFor(HeroArchetype archetype) =>
            ByArchetype.TryGetValue(archetype, out var abilities) ? Array.AsReadOnly(abilities) :
                throw new ArgumentOutOfRangeException(nameof(archetype));

        private static HeroDefinition Principal(string faction, string name, HeroArchetype archetype,
            int command, int defense, int strategy, bool provisional = false) =>
            new(faction + "/1", name, faction, archetype, command, defense, strategy, true, provisional);

        // Los secundarios alternan arquetipo para que cada reino disponga de órdenes distintas.
        private static HeroDefinition Extra(HeroDefinition principal, int index)
        {
            var archetypes = (HeroArchetype[])Enum.GetValues(typeof(HeroArchetype));
            var archetype = archetypes[((int)principal.Archetype + index - 1) % archetypes.Length];
            var kingdom = KingdomDesignCatalog.Find(principal.FactionId)?.Name ?? principal.FactionId;
            return new HeroDefinition(principal.FactionId + "/" + index, $"Capitán de {kingdom} {(index == 2 ? "I" : "II")}",
                principal.FactionId, archetype, 3, 3, 3, false, true);
        }
    }
}
