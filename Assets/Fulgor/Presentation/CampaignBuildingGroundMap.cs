using System;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Mapa de huellas de los edificios de casilla para el sombreador del terreno (Fulgor/Terreno): para cada punto del tablero guarda la distancia
    /// al borde de la huella del edificio más cercano, en 8 bits de <see cref="Near"/> a <see cref="Far"/> (más lejos, el valor más alto). El suelo
    /// pisado alrededor del edificio y la sombra de contacto junto a su base salen de esa distancia; con una lectura de textura por píxel cuesta
    /// lo mismo con trece edificios que con trescientos.
    /// </summary>
    public sealed class CampaignBuildingGroundMap
    {
        /// <summary>Distancias guardadas, en unidades del tablero: dentro de la huella hasta −0,1, fuera hasta 0,4.</summary>
        public const float Near = -.1f, Far = .4f;
        /// <summary>Texeles por unidad del tablero: unos 3 cm, de sobra para una distancia que se interpola.</summary>
        public const int DefaultTexelsPerUnit = 32;

        private readonly byte[] values;

        public int Width { get; }
        public int Height { get; }
        /// <summary>Esquina (x, z del mundo) del primer texel.</summary>
        public Vector2 Origin { get; }
        /// <summary>Lado de un texel en unidades del mundo.</summary>
        public float Texel { get; }
        public int Buildings { get; private set; }

        public CampaignBuildingGroundMap(Rect area, int texelsPerUnit = DefaultTexelsPerUnit, int maxSize = 4096)
        {
            if (area.width <= 0f || area.height <= 0f) throw new ArgumentException("El área del tablero está vacía.", nameof(area));
            if (texelsPerUnit < 1) throw new ArgumentOutOfRangeException(nameof(texelsPerUnit));
            Texel = Mathf.Max(1f / texelsPerUnit, Mathf.Max(area.width, area.height) / maxSize);
            Width = Mathf.CeilToInt(area.width / Texel); Height = Mathf.CeilToInt(area.height / Texel);
            Origin = area.min;
            values = new byte[Width * Height];
            Array.Fill(values, byte.MaxValue);
        }

        /// <summary>Valores de la textura (R8), fila a fila desde z mínima.</summary>
        public byte[] Values => values;

        /// <summary>De x, z del mundo a coordenadas de textura: uv = xz · (x, y) + (z, w).</summary>
        public Vector4 UvTransform => new(1f / (Width * Texel), 1f / (Height * Texel), -Origin.x / (Width * Texel), -Origin.y / (Height * Texel));

        public static byte Encode(float distance) => (byte)Mathf.RoundToInt(Mathf.Clamp01((distance - Near) / (Far - Near)) * 255f);
        public static float Decode(byte value) => Near + value / 255f * (Far - Near);

        /// <summary>
        /// Añade un edificio: <paramref name="worldToLocal"/> lleva del mundo a la raíz de su prefab, donde está la huella (rectángulo de esquinas
        /// redondeadas con centro, medio ancho y medio largo en x, z de la raíz). Se guarda la distancia multiplicada por
        /// <paramref name="distanceScale"/>: con 2, un edificio pequeño tiene la franja de suelo pisado la mitad de ancha.
        /// </summary>
        public void Add(Matrix4x4 worldToLocal, Vector2 centre, Vector2 halfSize, float cornerRadius, float distanceScale = 1f)
        {
            if (distanceScale <= 0f) throw new ArgumentOutOfRangeException(nameof(distanceScale));
            var localToWorld = worldToLocal.inverse;
            // caja del edificio en el mundo, con el margen de lo que se guarda fuera de la huella
            float reach = halfSize.magnitude + Far / distanceScale;
            var middle = localToWorld.MultiplyPoint3x4(new Vector3(centre.x, 0f, centre.y));
            int x0 = Mathf.Max(0, Mathf.FloorToInt((middle.x - reach - Origin.x) / Texel)), x1 = Mathf.Min(Width - 1, Mathf.CeilToInt((middle.x + reach - Origin.x) / Texel));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((middle.z - reach - Origin.y) / Texel)), z1 = Mathf.Min(Height - 1, Mathf.CeilToInt((middle.z + reach - Origin.y) / Texel));
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                var world = new Vector3(Origin.x + (x + .5f) * Texel, 0f, Origin.y + (z + .5f) * Texel);
                var local = worldToLocal.MultiplyPoint3x4(world);
                float distance = CampaignBuildingFootprint.SignedDistance(new Vector2(local.x, local.z) - centre, halfSize, cornerRadius);
                int at = z * Width + x;
                values[at] = Math.Min(values[at], Encode(distance * distanceScale));
            }
            Buildings++;
        }

        /// <summary>Distancia guardada en el texel que contiene un punto (x, z del mundo); fuera del mapa, <see cref="Far"/>.</summary>
        public float DistanceAt(Vector2 world)
        {
            int x = Mathf.FloorToInt((world.x - Origin.x) / Texel), z = Mathf.FloorToInt((world.y - Origin.y) / Texel);
            return x < 0 || z < 0 || x >= Width || z >= Height ? Far : Decode(values[z * Width + x]);
        }
    }
}
