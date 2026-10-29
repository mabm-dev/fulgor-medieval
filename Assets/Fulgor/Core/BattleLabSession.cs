using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Combat
{
    public enum BattleRecordedOrderKind { Attack, Move, Wait, Defend, WithdrawArmy }

    [Serializable]
    public sealed class BattleRecordedOrder
    {
        public BattleRecordedOrderKind kind;
        public int actorId, targetId, q, r;
        internal BattleRecordedOrder Copy() => new BattleRecordedOrder { kind = kind, actorId = actorId, targetId = targetId, q = q, r = r };
    }

    [Serializable]
    public sealed class BattleLabSaveData
    {
        public int version;
        public string rules;
        public long seed;
        public BattleRecordedOrder[] orders;
    }

    /// <summary>Versioned command journal for the laboratory fixture, not campaign battle persistence.</summary>
    public sealed class BattleLabSession
    {
        public const int MaximumOrders = 2000;
        private const string Rules = "tactical-prototype-2";
        private readonly List<BattleRecordedOrder> orders = new();
        public BattleLabSession(long seed) { Seed = seed; Battle = BattlePrototype.Create(seed); }
        public long Seed { get; }
        public BattlePrototype Battle { get; }
        public int OrderCount => orders.Count;

        public bool TryExecute(BattleRecordedOrder requested, out string message)
        {
            message = "Orden no válida.";
            if (orders.Count >= MaximumOrders) { message = "Límite del registro alcanzado; reinicia la batalla."; return false; }
            if (!BattleOrderExecutor.TryExecute(Battle.Commands, requested, out message)) return false;
            var order = requested.Copy();
            orders.Add(order);
            return true;
        }

        public BattleLabSaveData Capture()
        {
            var copy = new BattleRecordedOrder[orders.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = orders[i].Copy();
            return new BattleLabSaveData { version = 1, rules = Rules, seed = Seed, orders = copy };
        }

        public static BattleLabSession Restore(BattleLabSaveData data)
        {
            if (data == null || data.version != 1 || data.rules != Rules || data.orders == null || data.orders.Length > MaximumOrders)
                throw new ArgumentException("Incompatible tactical save.", nameof(data));
            // Replay into a new session; the caller only replaces the live session on success.
            var restored = new BattleLabSession(data.seed);
            for (int i = 0; i < data.orders.Length; i++)
                if (!restored.TryExecute(data.orders[i], out var reason))
                    throw new ArgumentException($"Invalid tactical order {i + 1}: {reason}", nameof(data));
            return restored;
        }
    }
}
