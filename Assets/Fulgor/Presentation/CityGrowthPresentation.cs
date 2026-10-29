using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>The prefab is a complete layout template; campaign state controls its visible contents.</summary>
    public static class CityGrowthPresentation
    {
        public static void Apply(GameObject city,CityDevelopment state,int turn)
        {
            CityBuildingView pending=null;
            foreach(var building in city.GetComponentsInChildren<CityBuildingView>(true)) {
                building.gameObject.SetActive(state.Has(building.ConstructionId));
                if(building.ConstructionId==state.PendingId)pending=building;
            }
            var site=city.GetComponent<CityConstructionSite>();if(site==null)site=city.AddComponent<CityConstructionSite>();
            site.Refresh(state,turn,pending);
            var plaza=city.transform.Find("Plaza empedrada");if(plaza!=null)plaza.gameObject.SetActive(state.Has("market"));
            var goods=city.transform.Find("Almacen");if(goods!=null)goods.gameObject.SetActive(state.Has("warehouse"));
            var ambiance=city.transform.Find("Ambiente de la villa");
            if(ambiance!=null)foreach(Transform child in ambiance) {
                if(child.name.StartsWith("Puesto de mercado",System.StringComparison.Ordinal))child.gameObject.SetActive(state.Has("market"));
                if(child.name=="Madera del puesto")child.gameObject.SetActive(state.Has("warehouse"));
            }
            var life=city.GetComponent<CityAmbientLife>();if(life!=null)life.ApplyDevelopment(state);
        }
    }
}
