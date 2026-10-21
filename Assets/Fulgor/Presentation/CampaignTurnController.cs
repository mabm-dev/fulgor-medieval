using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignTurnController : MonoBehaviour
    {
        private CampaignSelectionPanel panel;
        public CampaignTurnState State { get; private set; }

        public void Initialize(CampaignSelectionPanel turnPanel, GeneratedMap map)
        {
            if (turnPanel == null) throw new System.ArgumentNullException(nameof(turnPanel));
            if (map == null) throw new System.ArgumentNullException(nameof(map));
            if (panel != null) panel.EndTurnButton.onClick.RemoveListener(CompleteTurn);
            panel = turnPanel;
            State = new CampaignTurnState(1, default, CampaignIncomeCalculator.FromMap(map));
            panel.EndTurnButton.onClick.AddListener(CompleteTurn);
            panel.SetTurnState(State);
        }

        public void CompleteTurn()
        {
            if (State == null) return;
            State = State.CompleteTurn();
            panel?.SetTurnState(State);
        }

        private void OnDestroy()
        {
            if (panel != null) panel.EndTurnButton.onClick.RemoveListener(CompleteTurn);
        }
    }
}
