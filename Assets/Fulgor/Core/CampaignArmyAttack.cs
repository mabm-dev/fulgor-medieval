using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public enum CampaignAttackFailure { None, UnknownArmy, FriendlyArmy, ArmyInBattle, NotAdjacent, NoMovement, NoTroops, UndefendedHero }

    public sealed partial class CampaignArmyRegistry
    {
        /// <summary>Batalla entre huestes con tropas. A un héroe solo no se le ataca: se le apresa.</summary>
        public CampaignAttackFailure PreviewAttack(string attackerId, string defenderId)
        {
            var failure = PreviewEngagement(attackerId, defenderId);
            if (failure == CampaignAttackFailure.None && armies[defenderId].Army.IsLoneHero) return CampaignAttackFailure.UndefendedHero;
            return failure;
        }

        // Condiciones comunes a atacar y a apresar a un héroe solo; un héroe solo no puede atacar.
        private CampaignAttackFailure PreviewEngagement(string attackerId, string defenderId)
        {
            if (!TryGetArmy(attackerId, out var attacker) || !TryGetArmy(defenderId, out var defender))
                return CampaignAttackFailure.UnknownArmy;
            if (attacker.Army.FactionId == defender.Army.FactionId) return CampaignAttackFailure.FriendlyArmy;
            if (IsLocked(attackerId) || IsLocked(defenderId)) return CampaignAttackFailure.ArmyInBattle;
            if (attacker.Movement.Position.DistanceTo(defender.Movement.Position) != 1) return CampaignAttackFailure.NotAdjacent;
            if (attacker.Movement.RemainingMovement < 1) return CampaignAttackFailure.NoMovement;
            if (attacker.Army.Formations.Count == 0) return CampaignAttackFailure.NoTroops;
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
