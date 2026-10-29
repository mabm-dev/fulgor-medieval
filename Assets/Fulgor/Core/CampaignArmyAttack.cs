using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum CampaignAttackFailure { None, UnknownArmy, FriendlyArmy, ArmyInBattle, NotAdjacent, NoMovement }

    public sealed partial class CampaignArmyRegistry
    {
        public CampaignAttackFailure PreviewAttack(string attackerId, string defenderId)
        {
            if (!TryGetArmy(attackerId, out var attacker) || !TryGetArmy(defenderId, out var defender))
                return CampaignAttackFailure.UnknownArmy;
            if (attacker.Army.FactionId == defender.Army.FactionId) return CampaignAttackFailure.FriendlyArmy;
            if (IsLocked(attackerId) || IsLocked(defenderId)) return CampaignAttackFailure.ArmyInBattle;
            if (attacker.Movement.Position.DistanceTo(defender.Movement.Position) != 1) return CampaignAttackFailure.NotAdjacent;
            if (attacker.Movement.RemainingMovement < 1) return CampaignAttackFailure.NoMovement;
            return CampaignAttackFailure.None;
        }

        public bool TryBeginAttack(string encounterId, string attackerId, string defenderId, long seed,
            out CampaignArmyRegistry next, out CampaignAttackFailure failure)
        {
            next = this; failure = PreviewAttack(attackerId, defenderId);
            if (failure != CampaignAttackFailure.None) return false;
            // Construct the encounter before publishing or spending movement.
            next = BeginEncounter(encounterId, attackerId, defenderId, seed);
            var attacker = armies[attackerId];
            next.armies[attackerId] = new CampaignArmyOnMap(attacker.Army,
                ArmyMovementState.Restore(graph, attacker.Movement.Position, attacker.Movement.MovementPerTurn, 0));
            return true;
        }
    }
}
