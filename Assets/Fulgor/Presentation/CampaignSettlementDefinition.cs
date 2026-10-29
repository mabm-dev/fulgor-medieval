using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    [CreateAssetMenu(fileName="Settlement",menuName="Fulgor/Campaign Settlement")]
    public sealed class CampaignSettlementDefinition : ScriptableObject
    {
        [SerializeField] private string id="fortaleza-castilla";
        [SerializeField] private string displayName="Fortaleza de Castilla";
        [SerializeField] private string ownerFactionId="castilla";
        [SerializeField] private string ownerDisplayName="Castilla";
        [SerializeField] private Vector2Int position=new(13,8);
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject cityPrefab;
        public GameObject CityPrefab=>cityPrefab;
        public GameObject Prefab=>prefab;
        public string OwnerDisplayName=>ownerDisplayName;
        public CampaignSettlement Create()=>new(id,displayName,ownerFactionId,new HexCoordinates(position.x,position.y));
    }
}
