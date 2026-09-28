using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Crea las variantes de color del tablero a partir de un material base serializado,
    /// para que el build incluya el shader por referencia y no dependa de Shader.Find.
    /// </summary>
    public static class BoardMaterialFactory
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        public static Material Create(Material baseMaterial, string materialName, Color color, float smoothness)
        {
            if (baseMaterial == null) throw new System.ArgumentNullException(nameof(baseMaterial));

            var material = new Material(baseMaterial) { name = materialName };
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            else material.color = color;
            if (material.HasProperty(SmoothnessId)) material.SetFloat(SmoothnessId, smoothness);
            return material;
        }
    }
}
