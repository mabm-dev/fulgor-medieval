using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class BattleLabController
    {
        public BattleGraphicalView GraphicalView { get; private set; }
        private int graphicalClosedFrame=-1;
        public bool ClosedThisFrame=>graphicalClosedFrame==Time.frameCount;
        internal BattleRoster VisualRoster=>Roster;
        internal BattleAttackResolver VisualCommands=>Commands;
        internal bool CanGiveVisualOrder=>IsOpen&&campaignEncounter!=null&&ManualControl;
        internal bool IsAutomatic=>running;
        internal int VisualRevision=>campaignOrderCount;
        internal event System.Action<BattleRecordedOrder,MapPath> VisualOrderApplied;
        internal bool GraphicalReviewPaused;
        internal string OwnArmyName=>playerBattleSide==BattleSide.Attacker?attackerName:defenderName;
        internal string RivalArmyName=>playerBattleSide==BattleSide.Attacker?defenderName:attackerName;
        internal string VisualRetreat=>campaignOwner!=null?campaignOwner.RetreatPreview():"";
        internal string LatestBattleMessage=>log.Count==0?"":System.Linq.Enumerable.Last(log);

        private void EnsureGraphicalView()
        {
            if(GraphicalView==null)GraphicalView=gameObject.AddComponent<BattleGraphicalView>();
            if(!GraphicalView.IsOpen)GraphicalView.Open(this);
        }
        private void CloseGraphicalView()
        {graphicalClosedFrame=Time.frameCount;GraphicalView?.Close();}
        internal void ToggleVisualAutomatic()
        {
            if(!IsOpen||Roster.Outcome!=BattleOutcome.InProgress)return;
            if(automaticSteps>=1000)automaticSteps=0;
            running=!running;nextStep=Time.unscaledTime+.7f;
        }
        internal bool GiveVisualOrder(BattleRecordedOrderKind kind,HexCoordinates? destination=null,int targetId=0)
        {
            if(!CanGiveVisualOrder)return false;
            bool accepted=TryCampaignOrder(new BattleRecordedOrder{actorId=Roster.ActiveFormationId.Value,
                kind=kind,targetId=targetId,q=destination?.Q??0,r=destination?.R??0},out _);
            if(accepted)nextStep=Time.unscaledTime+.85f;
            return accepted;
        }
    }
}
