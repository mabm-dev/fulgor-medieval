using System;
using System.Collections.Generic;

namespace Fulgor.Core.Combat
{
    /// <summary>
    /// Keeps formations and activation order consistent when resolved damage or an
    /// explicit army withdrawal is applied. Not yet an attack command resolver.
    /// </summary>
    public sealed class BattleRoster
    {
        private readonly Dictionary<int, BattleFormationState> formations = new();
        private readonly BattleTurnOrder order;

        public BattleRoster(IEnumerable<BattleFormationState> deployment)
        {
            if (deployment == null) throw new ArgumentNullException(nameof(deployment));
            var slots = new List<BattleFormationSlot>();
            foreach (var formation in deployment)
            {
                if (formation == null || !formation.IsInBattle)
                    throw new ArgumentException("Deploy only live, non-withdrawn formations.", nameof(deployment));
                if (formations.ContainsKey(formation.Slot.Id))
                    throw new ArgumentException("Duplicate formation ID.", nameof(deployment));
                formations.Add(formation.Slot.Id, formation); slots.Add(formation.Slot);
            }
            order = new BattleTurnOrder(slots);
        }

        public IReadOnlyList<int> FormationIds
        {
            get
            {
                var ids = new List<int>(formations.Keys);
                ids.Sort();
                return ids.AsReadOnly();
            }
        }

        public BattleOutcome Outcome => order.Outcome;
        public int Round => order.Round;
        public int? ActiveFormationId => order.ActiveFormationId;
        public BattleSide? ActiveSide => order.ActiveSide;
        public IReadOnlyList<int> PendingActivations => order.PendingActivations;
        public BattleFormationState GetFormation(int id) => formations.TryGetValue(id, out var state) ? state :
            throw new ArgumentException("Unknown formation.", nameof(id));
        public int DefenseBonus(int id) => order.DefenseBonus(id);
        public bool HasWaited(int id) => order.HasWaited(id);
        internal void UpdateVisualPosition(int id, int row, int column) => order.UpdateVisualPosition(id, row, column);
        public bool TryWait() => order.TryWait();
        public bool TryDefend() => order.TryDefend();
        public bool CompleteActivation() => order.CompleteActivation();

        // The future attack resolver must validate actor, target, range and damage
        // before calling this method. This operation does not consume an activation.
        public bool TryApplyResolvedDamage(int targetId, long damage, out int casualties)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            casualties = 0;
            if (Outcome != BattleOutcome.InProgress || !formations.TryGetValue(targetId, out var target) || !target.IsInBattle)
                return false;
            var next = target.ReceiveDamage(damage);
            casualties = target.Quantity - next.Quantity;
            formations[targetId] = next;
            if (next.IsDestroyed) order.RemoveFromBattle(targetId);
            return true;
        }

        // Applies an explicit whole-army withdrawal, never a morale-triggered one.
        public bool TryWithdrawArmy(BattleSide side)
        {
            if (!Enum.IsDefined(typeof(BattleSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (Outcome != BattleOutcome.InProgress) return false;
            var withdrawn = new List<int>();
            foreach (var pair in formations)
                if (pair.Value.Slot.Side == side && pair.Value.IsInBattle) withdrawn.Add(pair.Key);
            if (!order.TryWithdrawArmy(side)) return false;
            foreach (var id in withdrawn) formations[id] = formations[id].Withdraw();
            return true;
        }
    }
}
