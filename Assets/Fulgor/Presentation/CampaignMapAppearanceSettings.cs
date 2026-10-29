using System;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    [CreateAssetMenu(menuName="Fulgor/Acabado del mapa")]
    public sealed class CampaignMapAppearanceSettings : ScriptableObject
    {
        // Terrain order: water, plain, forest, hill, mountain, cleared settlement ground.
        [SerializeField] private Mesh[] fullMeshes;
        [SerializeField] private Mesh[] mediumMeshes;
        [SerializeField] private Mesh[] distantMeshes;
        [SerializeField] private Material[] terrainMaterials;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Shader selectionShader;
        public Shader SelectionShader=>selectionShader;
        public Mesh Full(int index)=>fullMeshes[index];
        public Mesh Medium(int index)=>mediumMeshes[index];
        public Mesh Distant(int index)=>distantMeshes[index];
        public Material TerrainMaterial(int index)=>terrainMaterials[index];
        public Material WaterMaterial=>waterMaterial;
        public void Validate()
        {
            Check(fullMeshes,6);Check(mediumMeshes,5);Check(distantMeshes,5);Check(terrainMaterials,5);
            if(waterMaterial==null)throw new InvalidOperationException("Falta el material de agua del mapa.");
            foreach(var mesh in fullMeshes)if(!mesh.isReadable)throw new InvalidOperationException("El suelo debe permitir muestreo de geometría.");
        }
        private static void Check<T>(T[] values,int length) where T:UnityEngine.Object
        {
            if(values==null||values.Length!=length)throw new InvalidOperationException("Acabado de mapa incompleto.");
            foreach(var value in values)if(value==null)throw new InvalidOperationException("Recurso de mapa sin asignar.");
        }
    }
}
