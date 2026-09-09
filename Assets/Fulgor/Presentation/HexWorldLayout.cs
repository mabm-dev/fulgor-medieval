using System;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Traduce la cuadrícula lógica axial a un mundo 3D de hexágonos pointy-top.</summary>
    public static class HexWorldLayout
    {
        private static readonly float SquareRootOfThree = Mathf.Sqrt(3f);

        public static Vector3 ToWorld(HexCoordinates coordinates, float outerRadius = 1f)
        {
            if (outerRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(outerRadius));
            }

            var x = SquareRootOfThree * (coordinates.Q + coordinates.R * 0.5f) * outerRadius;
            var z = 1.5f * coordinates.R * outerRadius;
            return new Vector3(x, 0f, z);
        }
    }
}
