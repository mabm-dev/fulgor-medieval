using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignArmyRegistry
    {
        // Called for a defended settlement. The attack already consumed movement.
        // A surviving/withdrawn defender still blocks occupation; no troops are erased.
        public CampaignArmyRegistry ResolveAndOccupyVacatedCell(string id,out HexCoordinates? occupied)
        {
            occupied=null;
            if(!TryGetEncounter(id,out var encounter))return ResolveEncounter(id);
            var report=encounter.Battle.CreateReport();
            var destination=armies[encounter.DefenderId].Movement.Position;
            var next=ResolveEncounter(id);
            if(report.Outcome!=BattleOutcome.AttackerVictory||!next.TryGetArmy(encounter.AttackerId,out var attacker)||
                next.TryGetOccupant(destination,out _))return next;
            var advanced=new CampaignArmyRegistry(next);
            advanced.armies[attacker.Army.Id]=new CampaignArmyOnMap(attacker.Army,
                ArmyMovementState.Restore(graph,destination,attacker.Movement.MovementPerTurn,attacker.Movement.RemainingMovement));
            occupied=destination;return advanced;
        }
    }
}
