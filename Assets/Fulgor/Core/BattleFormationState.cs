using System;

namespace Fulgor.Core.Combat
{
    public enum FormationMoraleStatus { Steady, Wavering, Broken }

    /// <summary>Immutable troop attributes. Content and balancing values come from scenario data.</summary>
    public sealed class FormationAttributes
    {
        public FormationAttributes(int attack, int defense, int minimumDamage, int maximumDamage,
            int healthPerMember, int movement, int initiative, int range, int morale, int discipline)
        {
            if (attack < 0) throw new ArgumentOutOfRangeException(nameof(attack));
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            if (minimumDamage < 1) throw new ArgumentOutOfRangeException(nameof(minimumDamage));
            if (maximumDamage < minimumDamage) throw new ArgumentOutOfRangeException(nameof(maximumDamage));
            if (healthPerMember < 1) throw new ArgumentOutOfRangeException(nameof(healthPerMember));
            if (movement < 0) throw new ArgumentOutOfRangeException(nameof(movement));
            if (initiative < 0) throw new ArgumentOutOfRangeException(nameof(initiative));
            if (range < 1) throw new ArgumentOutOfRangeException(nameof(range));
            if (morale < 0 || morale > 100) throw new ArgumentOutOfRangeException(nameof(morale));
            if (discipline < 0 || discipline > 100) throw new ArgumentOutOfRangeException(nameof(discipline));
            Attack = attack; Defense = defense; MinimumDamage = minimumDamage; MaximumDamage = maximumDamage;
            HealthPerMember = healthPerMember; Movement = movement; Initiative = initiative;
            Range = range; Morale = morale; Discipline = discipline;
        }
        public int Attack { get; }
        public int Defense { get; }
        public int MinimumDamage { get; }
        public int MaximumDamage { get; }
        public int HealthPerMember { get; }
        public int Movement { get; }
        public int Initiative { get; }
        public int Range { get; }
        public int Morale { get; }
        public int Discipline { get; }
    }

    /// <summary>Damage is applied to aggregate health; surviving partial health is never discarded.</summary>
    public sealed class BattleFormationState
    {
        public BattleFormationState(BattleFormationSlot slot, FormationAttributes attributes, int quantity)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
            Slot = slot; Attributes = attributes; InitialQuantity = quantity;
            TotalHealth = checked((long)quantity * attributes.HealthPerMember);
            Morale = attributes.Morale;
        }

        private BattleFormationState(BattleFormationState previous, long health, bool withdrawn, int? morale = null)
        {
            Slot = previous.Slot; Attributes = previous.Attributes;
            InitialQuantity = previous.InitialQuantity; TotalHealth = health; IsWithdrawn = withdrawn;
            Morale = morale ?? previous.Morale;
        }

        public BattleFormationSlot Slot { get; }
        public FormationAttributes Attributes { get; }
        public int InitialQuantity { get; }
        public long TotalHealth { get; }
        public int Quantity => TotalHealth == 0 ? 0 : checked((int)((TotalHealth - 1) / Attributes.HealthPerMember + 1));
        public int Casualties => InitialQuantity - Quantity;
        public int FrontMemberHealth => Quantity == 0 ? 0 : checked((int)(TotalHealth - (long)(Quantity - 1) * Attributes.HealthPerMember));
        public int Morale { get; }
        public FormationMoraleStatus MoraleStatus => Morale > 50 ? FormationMoraleStatus.Steady :
            Morale > 25 ? FormationMoraleStatus.Wavering : FormationMoraleStatus.Broken;
        public bool IsWithdrawn { get; }
        public bool IsDestroyed => TotalHealth == 0;
        public bool IsInBattle => !IsDestroyed && !IsWithdrawn;

        public BattleFormationState ReceiveDamage(long resolvedDamage)
        {
            if (resolvedDamage < 0) throw new ArgumentOutOfRangeException(nameof(resolvedDamage));
            if (!IsInBattle || resolvedDamage == 0) return this;
            var health = resolvedDamage >= TotalHealth ? 0 : TotalHealth - resolvedDamage;
            var survivors = health == 0 ? 0 : (health - 1) / Attributes.HealthPerMember + 1;
            var casualties = Quantity - survivors;
            // ceil((casualties * 100 / quantityBefore) * (1.5 - discipline / 200))
            // Exact integer form avoids rounding differences at threshold values.
            long numerator = casualties * (300 - Attributes.Discipline);
            long denominator = 2L * Quantity;
            var loss = Math.Min(Morale, checked((int)((numerator + denominator - 1) / denominator)));
            return new BattleFormationState(this, health, false, Morale - loss);
        }

        public static BattleFormationState FromSurvivors(BattleFormationSlot slot, FormationAttributes attributes,
            int quantity, long health, int morale)
        {
            var baseline = new BattleFormationState(slot, attributes, quantity);
            if (health <= (long)(quantity - 1) * attributes.HealthPerMember || health > baseline.TotalHealth)
                throw new ArgumentOutOfRangeException(nameof(health), "Health must match the surviving quantity.");
            if (morale < 0 || morale > 100) throw new ArgumentOutOfRangeException(nameof(morale));
            return new BattleFormationState(baseline, health, false, morale);
        }

        // Withdrawal preserves survivors and wounds for a future strategic result.
        public BattleFormationState Withdraw() => !IsInBattle ? this : new BattleFormationState(this, TotalHealth, true);
    }
}
