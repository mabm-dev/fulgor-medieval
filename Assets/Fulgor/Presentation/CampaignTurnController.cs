using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignTurnController : MonoBehaviour
    {
        // Ingreso temporal hasta que existan asentamientos y territorio propio.
        private static readonly CampaignResources PrototypeIncome = new(2, 0, 0, 1, 0);

        private CampaignSelectionPanel panel;
        public CampaignTurnState State { get; private set; }

        public void Initialize(CampaignSelectionPanel turnPanel)
        {
            if (turnPanel == null) throw new System.ArgumentNullException(nameof(turnPanel));
            if (panel != null) panel.EndTurnButton.onClick.RemoveListener(CompleteTurn);
            panel = turnPanel;
            State = new CampaignTurnState(1, default, PrototypeIncome);
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
