using Fulgor.Core.Campaign;
using Fulgor.Core.Map;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        private CampaignMarchOrders marchOrders=new();
        public CampaignMarchOrders MarchOrders=>marchOrders;
        public HexCoordinates? MarchFor(string armyId)=>marchOrders.Destination(armyId);
        private bool PreviewMarch(HexCoordinates target,out MapPath path,out CampaignArmyMoveFailure failure)=>
            FogEnabled?PreviewFogMarch(target,out path,out failure):Military.TryPlanMarch(registry,selectedArmyId,target,out path,out failure);
        private void IssueMarch(HexCoordinates target)
        {
            if(!DiplomacyAllowsTarget(playerFaction,target))return;
            if(FogEnabled){IssueFogMarch(target);return;}
            if(!marchOrders.TryOrder(selectedArmyId,target,registry,Military,playerFaction,out var next,out _,out _))return;
            marchOrders=next;AdvanceMarch(selectedArmyId);
        }
        private void AdvanceMarch(string armyId)
        {
            var destination=marchOrders.Destination(armyId);
            if(destination.HasValue&&!DiplomacyAllowsTarget(playerFaction,destination.Value)){marchOrders=marchOrders.Cancel(armyId);return;}
            if(FogEnabled){AdvanceFogMarch(armyId);return;}
            marchOrders=marchOrders.AdvanceArmy(armyId,registry,Military,playerFaction,out var moved,out var step,Mission?.Destination);
            registry=moved;
            if(step?.Travelled?.Steps>0)
            {
                armyMarkers?.SetRegistry(registry,armyId,step.Travelled.Cells);
                CaptureAtArmyPosition(armyId);
                if(registry.TryGetArmy(armyId,out var army))Mission=Mission?.ObservePosition(army.Movement.Position);
                UpdateConclusion();if(IsCampaignFinished)marchOrders=new CampaignMarchOrders();
            }
        }
        private void ContinueMultipleMarches()
        {
            // Stable priority; each subsequent army sees positions already updated this turn.
            foreach(var id in marchOrders.ArmyIds)
            {if(IsCampaignFinished)break;AdvanceMarch(id);}
        }
        private static string MarchFailureText(CampaignArmyMoveFailure failure)=>failure switch
        {
            CampaignArmyMoveFailure.Occupied=>"Destino ocupado: la orden esperará.",
            CampaignArmyMoveFailure.GuardedSettlement=>"Fortaleza defendida: la orden esperará. El ataque es manual.",
            CampaignArmyMoveFailure.GarrisonCapacity=>"Fortaleza sin plazas: la orden esperará.",
            CampaignArmyMoveFailure.ArmyInBattle=>"Marcha detenida durante la batalla.",
            _=>"Ruta bloqueada: la orden esperará.",
        };
        private string ActiveMarchText()
        {
            var target=marchOrders.Destination(selectedArmyId);if(!target.HasValue)return "";
            if(!PreviewMarch(target.Value,out var route,out var failure))return "\n"+MarchFailureText(failure);
            return $"\nMarcha a {target.Value.Key} · faltan {route.TotalCost} puntos de ruta."+
                (Army?.RemainingMovement==0?" Continúa al terminar turno.":" Avance por pasos completos al terminar turno.");
        }
    }
}
