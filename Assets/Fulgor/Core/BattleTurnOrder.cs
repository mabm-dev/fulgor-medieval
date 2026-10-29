using System;
using System.Collections.Generic;

namespace Fulgor.Core.Combat
{
    public enum BattleSide { Attacker, Defender }
    public enum FormationKind { Infantry, Ranged, Cavalry }
    public enum BattleOutcome { InProgress, AttackerVictory, DefenderVictory, Draw }

    /// <summary>Deployment order supplied by the tactical board. Row zero is the top visual row.</summary>
    public readonly struct BattleFormationSlot
    {
        public BattleFormationSlot(int id, BattleSide side, FormationKind kind, int row, int column)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (!Enum.IsDefined(typeof(BattleSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (!Enum.IsDefined(typeof(FormationKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));
            Id = id; Side = side; Kind = kind; Row = row; Column = column;
        }
        public int Id { get; }
        public BattleSide Side { get; }
        public FormationKind Kind { get; }
        public int Row { get; }
        public int Column { get; }
    }

    /// <summary>
    /// Shared activation scheduling for manual and automatic tactical decisions.
    /// Does not calculate damage, movement, morale, or strategic battle results.
    /// </summary>
    public sealed class BattleTurnOrder
    {
        private readonly Dictionary<int, BattleFormationSlot> formations = new();
        private readonly HashSet<int> inBattle = new();
        private readonly HashSet<int> waited = new();
        private readonly HashSet<int> defending = new();
        private readonly List<int> pending = new();

        public BattleTurnOrder(IEnumerable<BattleFormationSlot> deployment)
        {
            if (deployment == null) throw new ArgumentNullException(nameof(deployment));
            var occupied = new HashSet<(int, int)>();
            int attackers = 0, defenders = 0;
            foreach (var slot in deployment)
            {
                if (formations.ContainsKey(slot.Id)) throw new ArgumentException("Duplicate formation ID.", nameof(deployment));
                if (!occupied.Add((slot.Row, slot.Column))) throw new ArgumentException("Two formations share a deployment cell.", nameof(deployment));
                formations.Add(slot.Id, slot); inBattle.Add(slot.Id);
                if (slot.Side == BattleSide.Attacker) attackers++; else defenders++;
            }
            if (attackers < 1 || defenders < 1 || attackers > 6 || defenders > 6)
                throw new ArgumentException("Deploy between one and six formations per side.", nameof(deployment));
            Round = 1;
            BuildRound();
        }

        public int Round { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public int? ActiveFormationId => Outcome == BattleOutcome.InProgress && pending.Count > 0 ? pending[0] : (int?)null;
        public BattleSide? ActiveSide => ActiveFormationId.HasValue ? formations[ActiveFormationId.Value].Side : (BattleSide?)null;
        public IReadOnlyList<int> PendingActivations => Array.AsReadOnly(pending.ToArray());

        public bool IsInBattle(int id) => inBattle.Contains(id);
        public bool HasWaited(int id) => waited.Contains(id);
        public int DefenseBonus(int id) => inBattle.Contains(id) && defending.Contains(id) ? 2 : 0;

        public bool TryWait()
        {
            if (!ActiveFormationId.HasValue) return false;
            var id = ActiveFormationId.Value;
            if (!waited.Add(id)) return false;
            var side = formations[id].Side;
            pending.RemoveAt(0);
            int index = 0;
            while (index < pending.Count && formations[pending[index]].Side == side) index++;
            pending.Insert(index, id);
            BeginActivation();
            return true;
        }

        public bool TryDefend()
        {
            if (!ActiveFormationId.HasValue) return false;
            defending.Add(ActiveFormationId.Value);
            CompleteActivation();
            return true;
        }

        // Called after a legal move or attack has been resolved by the battle engine.
        public bool CompleteActivation()
        {
            if (!ActiveFormationId.HasValue) return false;
            pending.RemoveAt(0);
            ContinueOrder();
            return true;
        }

        /// <summary>Destroyed and explicitly withdrawn formations no longer act or occupy a cell.</summary>
        public bool RemoveFromBattle(int id)
        {
            if (Outcome != BattleOutcome.InProgress || !inBattle.Contains(id)) return false;
            var previousActive = ActiveFormationId;
            inBattle.Remove(id); defending.Remove(id);
            pending.Remove(id);
            UpdateOutcome();
            if (Outcome != BattleOutcome.InProgress) { pending.Clear(); return true; }
            if (previousActive == id) ContinueOrder();
            return true;
        }

        public bool TryWithdrawArmy(BattleSide side)
        {
            if (!Enum.IsDefined(typeof(BattleSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
            if (Outcome != BattleOutcome.InProgress) return false;
            var removed = new List<int>();
            foreach (var id in inBattle)
                if (formations[id].Side == side) removed.Add(id);
            foreach (var id in removed)
            {
                inBattle.Remove(id);
                defending.Remove(id);
            }
            // Apply the whole withdrawal before evaluating the outcome: do not
            // start intermediate activations or rounds while removing the army.
            UpdateOutcome();
            pending.Clear();
            return removed.Count > 0;
        }

        internal void UpdateVisualPosition(int id, int row, int column)
        {
            if (!inBattle.Contains(id)) throw new ArgumentException("Formation is not in battle.", nameof(id));
            var slot = formations[id];
            formations[id] = new BattleFormationSlot(id, slot.Side, slot.Kind, row, column);
            // Do not reorder pending actions halfway through a round.
        }

        private void ContinueOrder()
        {
            if (pending.Count == 0)
            {
                Round = checked(Round + 1);
                waited.Clear();
                BuildRound();
            }
            else BeginActivation();
        }

        private void BuildRound()
        {
            pending.Clear();
            pending.AddRange(inBattle);
            pending.Sort((left, right) => Compare(formations[left], formations[right]));
            BeginActivation();
        }

        private void BeginActivation()
        {
            // Defense lasts until this formation begins its next activation.
            if (ActiveFormationId.HasValue) defending.Remove(ActiveFormationId.Value);
        }

        private void UpdateOutcome()
        {
            bool attacker = false, defender = false;
            foreach (var id in inBattle)
                if (formations[id].Side == BattleSide.Attacker) attacker = true; else defender = true;
            Outcome = attacker && defender ? BattleOutcome.InProgress :
                attacker ? BattleOutcome.AttackerVictory : defender ? BattleOutcome.DefenderVictory : BattleOutcome.Draw;
        }

        private static int Compare(BattleFormationSlot left, BattleFormationSlot right)
        {
            int result = left.Side.CompareTo(right.Side);
            if (result == 0) result = left.Kind.CompareTo(right.Kind);
            if (result == 0) result = left.Row.CompareTo(right.Row);
            if (result == 0) result = left.Column.CompareTo(right.Column);
            return result == 0 ? left.Id.CompareTo(right.Id) : result;
        }
    }
}
