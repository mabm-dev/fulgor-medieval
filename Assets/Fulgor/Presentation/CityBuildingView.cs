using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public enum CityBuildingKind { Fortress, House, Market, Warehouse, Well }
    public sealed class CityBuildingView : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private CityBuildingKind kind;
        public string DisplayName=>displayName;
        public CityBuildingKind Kind=>kind;
        public string ConstructionId=>kind switch {
            CityBuildingKind.Fortress=>"fortress",CityBuildingKind.Market=>"market",
            CityBuildingKind.Warehouse=>"warehouse",CityBuildingKind.Well=>"well",
            _=>"house-"+displayName.Substring(displayName.LastIndexOf(' ')+1)};
        public Bounds Bounds
        {
            get {
                var renderers=GetComponentsInChildren<MeshRenderer>();
                var result=new Bounds(transform.position,Vector3.zero);
                foreach(var renderer in renderers)result.Encapsulate(renderer.bounds);return result;
            }
        }
    }
}
