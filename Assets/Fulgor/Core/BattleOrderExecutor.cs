using System;
using Fulgor.Core.Map;

namespace Fulgor.Core.Combat
{
    public static class BattleOrderExecutor
    {
        public static bool TryExecute(BattleAttackResolver commands, BattleRecordedOrder requested, out string message)
        {
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            message = "Orden no válida.";
            if (requested == null || !Enum.IsDefined(typeof(BattleRecordedOrderKind), requested.kind)) return false;
            var order = requested.Copy();
            if (commands.Roster.Outcome != BattleOutcome.InProgress) { message = "La batalla ya terminó."; return false; }
            if (commands.Roster.ActiveFormationId != order.actorId) { message = "Solo actúa la formación activa."; return false; }
            switch (order.kind)
            {
                case BattleRecordedOrderKind.Attack:
                    if (!commands.TryAttack(order.actorId, order.targetId, out var attack, out var attackFailure))
                    { message = "Ataque rechazado: " + attackFailure; return false; }
                    message = $"#{attack.AttackerId} → #{attack.TargetId}: tirada {attack.Roll}, daño {attack.Damage}, bajas {attack.Casualties}, moral {attack.MoraleBefore}→{attack.MoraleAfter}";
                    break;
                case BattleRecordedOrderKind.Move:
                    if (!commands.TryMove(order.actorId, new HexCoordinates(order.q, order.r), out var path, out var moveFailure))
                    { message = "Movimiento rechazado: " + moveFailure; return false; }
                    message = $"#{order.actorId} mueve a {order.q},{order.r} · Coste {path.TotalCost}";
                    break;
                case BattleRecordedOrderKind.Wait:
                    if (!commands.Roster.TryWait()) { message = "Ya esperó esta ronda."; return false; }
                    message = $"#{order.actorId}: formación aplazada.";
                    break;
                case BattleRecordedOrderKind.Defend:
                    if (!commands.Roster.TryDefend()) return false;
                    message = $"#{order.actorId}: defensa +2 hasta su próxima activación.";
                    break;
                case BattleRecordedOrderKind.WithdrawArmy:
                    if (!commands.Roster.TryWithdrawArmy(commands.Roster.ActiveSide.Value)) return false;
                    message = "Hueste retirada con supervivientes.";
                    break;
                default: return false;
            }
            return true;
        }
    }
}
