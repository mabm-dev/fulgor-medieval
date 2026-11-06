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
        // Opcionales: sin ellos el mapa sigue con las cinco piezas fijas (escenas de revisión antiguas)
        [SerializeField] private Shader terrainShader;
        [SerializeField] private Material reliefMountainMaterial;
        [SerializeField] private Texture2D terrainNoise;
        // Los materiales de suelo ya con Fulgor/Terreno (mismo orden que terrainMaterials; el agua, vacío). Tienen que existir como recursos: la
        // build solo incluye las variantes del sombreador que usa algún material guardado, y uno creado al jugar se quedaba sin la suya (negro).
        [SerializeField] private Material[] terrainShaderMaterials;
        [SerializeField, Range(6,48)] private int reliefSubdivisions=32;
        public Shader SelectionShader=>selectionShader;
        /// <summary>Sombreador del suelo (Fulgor/Terreno): misma textura de cada pieza, con variación de gran escala continua entre casillas.</summary>
        public Shader TerrainShader=>terrainShader;
        /// <summary>Material de las montañas generadas con <see cref="CampaignMountainRelief"/>; si falta, la montaña es la pieza fija.</summary>
        public Material ReliefMountainMaterial=>reliefMountainMaterial;
        public int ReliefSubdivisions=>reliefSubdivisions;
        /// <summary>Ruido que se repite sin costuras (cuatro canales) del que el sombreador del terreno lee la variación en vez de calcularla por píxel.</summary>
        public Texture2D TerrainNoise=>terrainNoise;
        public Mesh Full(int index)=>fullMeshes[index];
        public Mesh Medium(int index)=>mediumMeshes[index];
        public Mesh Distant(int index)=>distantMeshes[index];
        public Material TerrainMaterial(int index)=>terrainMaterials[index];
        /// <summary>Material de suelo preparado con <see cref="TerrainShader"/>, o null si no lo hay (se usa <see cref="TerrainMaterial"/>).</summary>
        public Material TerrainShaderMaterial(int index)=>terrainShaderMaterials!=null&&index>=0&&index<terrainShaderMaterials.Length?terrainShaderMaterials[index]:null;
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
