using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public bool FogEnabled {get;private set;}
        public CampaignFogState Fog {get;private set;}
        private CampaignArmyRegistry fogObservedArmies;
        private CampaignSettlementRegistry fogObservedOwners;
        private int fogObservedTurn;
        private void InitializeFog()
        {
            InvalidateFog();
            Fog=FogEnabled?new CampaignFogState(incomeMap,KnownFactions()):null;
            panel?.SetFogMode(FogEnabled);
            var view=GetComponent<CampaignFogMap>();if(view==null&&FogEnabled)view=gameObject.AddComponent<CampaignFogMap>();
            view?.ResetFog();RefreshFog();
        }
        private void InvalidateFog(){fogObservedArmies=null;fogObservedOwners=null;fogObservedTurn=0;}
        public void RefreshFog()
        {
            if(!FogEnabled||Fog==null||registry==null||State==null||SettlementState==null)return;
            if(fogObservedArmies==registry&&fogObservedOwners==SettlementState&&fogObservedTurn==State.Turn)return;
            Fog=Fog.Refresh(registry,SettlementState,State.Turn);
            fogObservedArmies=registry;fogObservedOwners=SettlementState;fogObservedTurn=State.Turn;
            selection?.RefreshKnowledgeText();
        }
        public CampaignVisibility VisibilityAt(HexCoordinates position)=>!FogEnabled?CampaignVisibility.Visible:Fog?.Visibility(playerFaction,position)??CampaignVisibility.Unknown;
        public bool CanObserve(HexCoordinates position)=>!FogEnabled||Fog?.IsVisible(playerFaction,position)==true;
        private bool PreviewFogMarch(HexCoordinates target,out MapPath path,out CampaignArmyMoveFailure failure)
        {RefreshFog();return CampaignFogOrders.TryPlan(selectedArmyId,target,registry,Military,SettlementState,activeRoutes,Fog,out path,out failure);}
        public bool PreviewKnowledgePath(HexCoordinates origin,HexCoordinates target,out MapPath path)
        {
            path=null;RefreshFog();
            if(!FogEnabled||Fog==null||!Fog.IsExplored(playerFaction,origin)||!Fog.IsExplored(playerFaction,target))return false;
            var blocked=new HashSet<HexCoordinates>(Fog.Cells.Where(c=>!Fog.IsExplored(playerFaction,c)));
            return MapPathfinder.TryFindPath(activeRoutes,origin,target,out path,blocked);
        }
        private void RefreshUnseenTarget(HexCoordinates target,string info)
        {
            bool known=Fog.IsExplored(playerFaction,target);
            panel.SetArmyActionLabel(known?"MARCHAR A TERRENO EXPLORADO":"TERRITORIO SIN EXPLORAR");
            bool allowed=known&&PreviewFogMarch(target,out _,out _)&&target!=Army.Position&&MarchDestination!=target&&!IsCampaignFinished;
            panel.SetArmyState(Army,info+(known?"\nSin observación actual. La ruta se revisará al avanzar.":"\nAcércate por las casillas conocidas para descubrir este territorio."),allowed);
        }
        private void IssueFogMarch(HexCoordinates target)
        {
            RefreshFog();
            if(!marchOrders.TryKnownOrder(selectedArmyId,target,registry,Military,SettlementState,activeRoutes,Fog,playerFaction,out var orders))return;
            marchOrders=orders;AdvanceFogMarch(selectedArmyId);
        }
        private void AdvanceFogMarch(string id)
        {
            var target=marchOrders.Destination(id);if(!target.HasValue||!registry.TryGetArmy(id,out _))return;
            RefreshFog();var result=CampaignFogOrders.Advance(id,target.Value,registry,Military,SettlementState,activeRoutes,Fog,State.Turn,Mission?.Destination);
            registry=result.Armies;Fog=result.Fog;InvalidateFog();
            if(result.Arrived)marchOrders=marchOrders.Cancel(id);
            if(result.Travelled.Steps>0)
            {
                RefreshFog();armyMarkers?.SetRegistry(registry,id,result.Travelled.Cells);CaptureAtArmyPosition(id);
                if(registry.TryGetArmy(id,out var army))Mission=Mission?.ObservePosition(army.Movement.Position);
                UpdateConclusion();if(IsCampaignFinished)marchOrders=new CampaignMarchOrders();
            }
            RefreshFog();
        }
        private bool AdvanceFogRival(string id,HexCoordinates target,bool recovering=false)
        {
            var result=CampaignFogOrders.Advance(id,target,registry,Military,SettlementState,activeRoutes,FogEnabled?Fog:null,State.Turn,null,recovering);
            registry=result.Armies;Fog=result.Fog;InvalidateFog();RefreshFog();
            if(result.Travelled.Steps==0)return false;
            armyMarkers?.SetRegistry(registry,id,result.Travelled.Cells);CaptureForArmy(id);RefreshFog();return true;
        }
    }
}
