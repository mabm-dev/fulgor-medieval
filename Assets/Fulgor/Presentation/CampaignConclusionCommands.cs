using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignTurnController
    {
        public CampaignObjectiveKind ObjectiveKind {get;private set;}
        public CampaignConclusion Conclusion {get;private set;}
        public bool IsCampaignFinished=>Conclusion!=CampaignConclusion.InProgress;
        public IReadOnlyList<CampaignRetreatResult> LastRetreats {get;private set;}=Array.Empty<CampaignRetreatResult>();
        private void UpdateConclusion()
        {
            Conclusion=CampaignConclusionRules.Evaluate(ObjectiveKind,registry,SettlementState,incomeFaction,Mission?.IsWon==true);
            if(IsCampaignFinished){marchOrders=new CampaignMarchOrders();MarchDestination=null;rivalActors=null;}
        }
        public string RetreatPreview()
        {
            if(ObjectiveKind!=CampaignObjectiveKind.Conquest||registry==null||!registry.TryGetEncounter(ActiveEncounterId,out var encounter))return "";
            registry.TryGetArmy(encounter.AttackerId,out var attacker);registry.TryGetArmy(encounter.DefenderId,out var defender);
            var army=attacker.Army.FactionId==incomeFaction?attacker:defender;
            int budget=encounter.Garrison!=null&&army.Army.Id==encounter.DefenderId&&encounter.Garrison.FieldArmy==null?6:army.Movement.MovementPerTurn;
            var side=attacker.Army.FactionId==incomeFaction?BattleSide.Attacker:BattleSide.Defender;
            int survivors=encounter.Battle.Roster.FormationIds.Select(encounter.Battle.Roster.GetFormation).Count(f=>f.Slot.Side==side&&!f.IsDestroyed);
            var destination=CampaignStrategicResolution.RetreatDestination(registry,Military,SettlementState,army.Movement.Position,army.Army.FactionId,survivors,budget);
            return destination.HasValue?$"Retirada prevista a {destination.Value.Key}; movimiento agotado.":"Sin salida válida: los supervivientes se rendirán al retirarse.";
        }
        private string RetreatSummary()=>string.Join(" · ",LastRetreats.Select(r=>r.Surrendered?
            $"{r.Troops} soldados rendidos":$"{r.Troops} soldados se retiran a {r.Destination.Value.Key}"));
    }
}
