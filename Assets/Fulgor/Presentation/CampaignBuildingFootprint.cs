using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Huella de un edificio de casilla, en el espacio de la raíz de su prefab: el rectángulo de esquinas redondeadas que ocupa su base en el suelo,
    /// lo más bajo del modelo y el alto de la losa de la base. La calcula el editor desde la malla (en partida las mallas de los FBX no se pueden
    /// leer) y la usan el suelo pisado alrededor del edificio, el asiento sobre la casilla y la mezcla de la base con el terreno.
    /// </summary>
    public sealed class CampaignBuildingFootprint : MonoBehaviour
    {
        [SerializeField] private Vector2 centre;
        [SerializeField] private Vector2 halfSize = new(.3f, .3f);
        [SerializeField] private float cornerRadius;
        [SerializeField] private float bottom;
        [SerializeField] private float baseHeight = .02f;

        /// <summary>Centro de la huella (x, z de la raíz del prefab).</summary>
        public Vector2 Centre => centre;
        /// <summary>Medio ancho (x) y medio largo (z) de la huella.</summary>
        public Vector2 HalfSize => halfSize;
        /// <summary>Radio de las esquinas: 0 es un rectángulo; el lado menor de <see cref="HalfSize"/>, una huella redonda.</summary>
        public float CornerRadius => cornerRadius;
        /// <summary>Altura de lo más bajo del modelo sobre la raíz.</summary>
        public float Bottom => bottom;
        /// <summary>Alto de la losa de la base sobre lo más bajo del modelo.</summary>
        public float BaseHeight => baseHeight;

        public void Set(Vector2 footprintCentre, Vector2 footprintHalfSize, float footprintCornerRadius, float modelBottom, float slabHeight)
        {
            centre = footprintCentre;
            halfSize = new Vector2(Mathf.Max(.01f, footprintHalfSize.x), Mathf.Max(.01f, footprintHalfSize.y));
            cornerRadius = Mathf.Clamp(footprintCornerRadius, 0f, Mathf.Min(halfSize.x, halfSize.y));
            bottom = modelBottom;
            baseHeight = Mathf.Max(0f, slabHeight);
        }

        /// <summary>Distancia con signo de un punto (x, z de la raíz) al borde de la huella: negativa dentro.</summary>
        public float SignedDistance(Vector2 local) => SignedDistance(local - centre, halfSize, cornerRadius);

        /// <summary>Distancia con signo al borde de un rectángulo de esquinas redondeadas centrado en el origen.</summary>
        public static float SignedDistance(Vector2 p, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x) - half.x + radius, Mathf.Abs(p.y) - half.y + radius);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// <summary>Punto del borde de la huella en la dirección de un ángulo (radianes, desde +x hacia +z), en x, z de la raíz.</summary>
        public Vector2 Outline(float angle)
        {
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            // el borde está donde la distancia con signo pasa por cero a lo largo del rayo desde el centro
            float inside = 0f, outside = Mathf.Max(halfSize.x, halfSize.y) * 1.5f;
            for (int i = 0; i < 24; i++)
            {
                float middle = (inside + outside) * .5f;
                if (SignedDistance(direction * middle, halfSize, cornerRadius) < 0f) inside = middle; else outside = middle;
            }
            return centre + direction * ((inside + outside) * .5f);
        }
    }
}
