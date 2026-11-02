using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Combat;

namespace Fulgor.Core.Campaign
{
    public enum HeroCommandFailure { None, BattleFinished, AlreadyActedThisRound, UnknownAbility, NotEnoughPoints, InvalidTarget }

    public readonly struct HeroCommandResult
    {
        internal HeroCommandResult(HeroAbility ability, IReadOnlyList<int> targets, long damage, int casualties,
            long healed, int moraleChange, int pointsLeft)
        {
            Ability = ability; Targets = targets; Damage = damage; Casualties = casualties;
            Healed = healed; MoraleChange = moraleChange; PointsLeft = pointsLeft;
        }
        public HeroAbility Ability { get; }
        public IReadOnlyList<int> Targets { get; }
        public long Damage { get; }
        public int Casualties { get; }
        public long Healed { get; }
        public int MoraleChange { get; }
        public int PointsLeft { get; }
    }

    /// <summary>
    /// Héroe que dirige un bando desde fuera de la rejilla, como en Heroes of Might and Magic III:
    /// no ocupa casilla ni puede ser atacado, y actúa como máximo una vez por ronda pagando puntos
    /// de mando. Las acciones no consumen la activación de ningún batallón.
    /// </summary>
    public sealed class BattleHeroCommand
    {
        private int lastRound;

        public BattleHeroCommand(HeroDefinition hero, BattleSide side)
        {
            Hero = hero ?? throw new ArgumentNullException(nameof(hero));
            if (!Enum.IsDefined(typeof(BattleSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            Side = side; CommandPoints = hero.CommandPoints;
        }

        public HeroDefinition Hero { get; }
        public BattleSide Side { get; }
        public int CommandPoints { get; private set; }
        public bool HasActed(BattleRoster roster) => roster != null && lastRound == roster.Round;

        public bool CanUse(HeroAbility ability, BattleRoster roster, out HeroCommandFailure failure)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            failure = HeroCommandFailure.None;
            if (roster.Outcome != BattleOutcome.InProgress) failure = HeroCommandFailure.BattleFinished;
            else if (lastRound == roster.Round) failure = HeroCommandFailure.AlreadyActedThisRound;
            else if (!Hero.Abilities.Contains(ability)) failure = HeroCommandFailure.UnknownAbility;
            else if (HeroCatalog.Ability(ability).Cost > CommandPoints) failure = HeroCommandFailure.NotEnoughPoints;
            return failure == HeroCommandFailure.None;
        }

        /// <summary>Usa una orden. Las de objetivo único exigen un batallón en combate del bando adecuado.</summary>
        public bool TryUse(HeroAbility ability, int? targetId, BattleRoster roster,
            out HeroCommandResult result, out HeroCommandFailure failure)
        {
            result = default;
            if (!CanUse(ability, roster, out failure)) return false;
            var definition = HeroCatalog.Ability(ability);
            var targets = Targets(definition.Target, targetId, roster);
            if (targets.Count == 0) { failure = HeroCommandFailure.InvalidTarget; return false; }

            long damage = 0, healed = 0; int casualties = 0, morale = 0;
            int c = Hero.Command, d = Hero.Defense;
            foreach (var id in targets)
            {
                switch (ability)
                {
                    case HeroAbility.HeroStrike: Hit(roster, id, 10 * c, ref damage, ref casualties); break;
                    case HeroAbility.FrontalCharge: Hit(roster, id, 25 * c, ref damage, ref casualties); break;
                    case HeroAbility.PreciseShot: Hit(roster, id, 18 * c, ref damage, ref casualties); break;
                    case HeroAbility.ArrowRain: Hit(roster, id, 6 * c, ref damage, ref casualties); break;
                    case HeroAbility.Harass: Hit(roster, id, 8 * c, ref damage, ref casualties); Morale(roster, id, -10, ref morale); break;
                    case HeroAbility.Flank: Hit(roster, id, 10 * c, ref damage, ref casualties); Morale(roster, id, -25, ref morale); break;
                    case HeroAbility.WarCry: Morale(roster, id, 15, ref morale); break;
                    case HeroAbility.Regroup: Morale(roster, id, 30, ref morale); break;
                    case HeroAbility.HoldTheLine:
                        if (roster.TryHeal(id, 10 * d, out var amount)) healed += amount;
                        break;
                    default: failure = HeroCommandFailure.UnknownAbility; return false;
                }
            }
            CommandPoints -= definition.Cost;
            lastRound = roster.Round;
            result = new HeroCommandResult(ability, targets.AsReadOnly(), damage, casualties, healed, morale, CommandPoints);
            return true;
        }

        /// <summary>Bonificaciones pasivas que el héroe da a sus batallones al desplegar.</summary>
        public static FormationAttributes Empower(FormationAttributes attributes, HeroDefinition hero)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (hero == null) return attributes;
            return new FormationAttributes(attributes.Attack + hero.AttackBonus, attributes.Defense + hero.DefenseBonus,
                attributes.MinimumDamage, attributes.MaximumDamage, attributes.HealthPerMember, attributes.Movement,
                attributes.Initiative + hero.InitiativeBonus, attributes.Range, attributes.Morale, attributes.Discipline);
        }

        private List<int> Targets(HeroAbilityTarget kind, int? targetId, BattleRoster roster)
        {
            var result = new List<int>();
            bool enemy = kind == HeroAbilityTarget.Enemy || kind == HeroAbilityTarget.AllEnemies;
            if (kind == HeroAbilityTarget.Enemy || kind == HeroAbilityTarget.Ally)
            {
                if (targetId.HasValue && IsValid(roster, targetId.Value, enemy)) result.Add(targetId.Value);
                return result;
            }
            foreach (var id in roster.FormationIds) if (IsValid(roster, id, enemy)) result.Add(id);
            return result;
        }

        private bool IsValid(BattleRoster roster, int id, bool enemy)
        {
            if (!roster.FormationIds.Contains(id)) return false;
            var formation = roster.GetFormation(id);
            return formation.IsInBattle && (formation.Slot.Side != Side) == enemy;
        }

        private static void Hit(BattleRoster roster, int id, long amount, ref long damage, ref int casualties)
        {
            var before = roster.GetFormation(id).TotalHealth;
            if (roster.TryApplyResolvedDamage(id, amount, out var killed))
            { damage += before - roster.GetFormation(id).TotalHealth; casualties += killed; }
        }

        private static void Morale(BattleRoster roster, int id, int delta, ref int total)
        {
            var before = roster.GetFormation(id).Morale;
            if (roster.TryAdjustMorale(id, delta)) total += roster.GetFormation(id).Morale - before;
        }
    }
}
