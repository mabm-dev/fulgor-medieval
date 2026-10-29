using System.Collections.Generic;
using Fulgor.Core.Map;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public sealed class CampaignArmyMarkerLayer : MonoBehaviour
    {
        private readonly Dictionary<string, CampaignArmyMarker> markers = new();
        public void Initialize(CampaignBoardSelection owner, CampaignArmyRegistry registry, string playerFaction)
        {
            Clear();
            foreach (var unit in registry.Armies)
            {
                var root = new GameObject("Hueste " + unit.Army.Id);
                root.transform.SetParent(transform, false);
                var marker = root.AddComponent<CampaignArmyMarker>();
                marker.Initialize(owner);
                marker.SetColor(unit.Army.FactionId == playerFaction ? new Color(.08f, .38f, .88f) : new Color(.85f, .16f, .12f));
                marker.SetArmy(unit.Movement); markers.Add(unit.Army.Id, marker);
                marker.SetFogVisible(unit.Army.FactionId==playerFaction||GetComponent<CampaignTurnController>()?.CanObserve(unit.Movement.Position)!=false);
            }
        }
        public void SetRegistry(CampaignArmyRegistry registry, string movingArmyId = null, IReadOnlyList<HexCoordinates> route = null)
        {
            foreach (var pair in markers)
            {
                registry.TryGetArmy(pair.Key,out var unit);var campaign=GetComponent<CampaignTurnController>();
                bool own=unit!=null&&unit.Army.FactionId==campaign?.PlayerFactionId;
                pair.Value.SetArmy(unit?.Movement,pair.Key==movingArmyId&&(campaign?.FogEnabled!=true||own)?route:null);
                pair.Value.SetFogVisible(unit!=null&&(own||campaign?.CanObserve(unit.Movement.Position)!=false));
            }
        }
        public void Clear()
        {
            foreach (var marker in markers.Values)
                if (marker != null) { marker.gameObject.SetActive(false); Destroy(marker.gameObject); }
            markers.Clear();
        }
    }
}
