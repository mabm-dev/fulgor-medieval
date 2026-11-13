using System.Linq;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed partial class CampaignSelectionPanel
    {
        public void ShowSettlementInspection(CampaignSettlement city)
        {
            var campaign = GetComponent<CampaignTurnController>();
            var towns = GetComponent<CampaignSettlementController>();
            if (!HasCampaignHud || city == null || campaign == null || !campaign.CanObserve(city.Position)) return;
            selectedFormation = -1;
            foreach (var frame in troopSelection) frame.SetActive(false);
            OpenDrawer("CIUDAD RIVAL");
            campaignDrawer.name = "Inspección de ciudad";
            HudText("Control de ciudad", campaignDrawer, new Vector2(40,-90), new Vector2(275,60),
                city.Name + "\nControl · " + towns.OwnerName(city.OwnerFactionId), 20, false, LegacyUI.InkTitle);
            long defenders = campaign.Military.Garrison(city.Id)?.Formations.Sum(f => (long)f.Quantity) ?? 0;
            if (campaign.ArmyRegistry.TryGetOccupant(city.Position,out var stationed))
                defenders += stationed.Army.Formations.Sum(f => (long)f.Quantity);
            HudText("Inspección", campaignDrawer, new Vector2(40,-156), new Vector2(275,74),
                "Ciudad rival · " + city.Position.Key + "\nDefensores visibles · " + defenders, 19, false, LegacyUI.InkText);
            var hero = campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
            bool available = hero.armyId != null && campaign.ArmyRegistry.TryGetArmy(hero.armyId,out _) ||
                hero.settlementId != null && campaign.SettlementState.TryGet(hero.settlementId,out _);
            CampaignHudArt.Button("Localizar héroe", campaignDrawer, new Vector2(40,-246), new Vector2(275,42), "VER TU HÉROE", () =>
            {
                CloseDrawer();
                var current = campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
                if (current.armyId != null && campaign.SelectArmyForRoster(current.armyId)) campaign.LocateArmy();
                else if (current.settlementId != null && campaign.SettlementState.TryGet(current.settlementId,out var home)) towns.OpenRecruitmentAt(home.Position);
            }).interactable = available;
        }
    }
}
