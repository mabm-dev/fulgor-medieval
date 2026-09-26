using System;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Traduce entre la cuadrícula lógica axial y un mundo 3D de hexágonos pointy-top.</summary>
    public static class HexWorldLayout
    {
        private static readonly float SquareRootOfThree = Mathf.Sqrt(3f);

        public static Vector3 ToWorld(HexCoordinates coordinates, float outerRadius = 1f)
        {
            ValidateRadius(outerRadius);
            var x = SquareRootOfThree * (coordinates.Q + coordinates.R * 0.5f) * outerRadius;
            var z = 1.5f * coordinates.R * outerRadius;
            return new Vector3(x, 0f, z);
        }

        public static HexCoordinates FromWorld(Vector3 position, float outerRadius = 1f)
        {
            ValidateRadius(outerRadius);
            var r = position.z / (1.5f * outerRadius);
            var q = position.x / (SquareRootOfThree * outerRadius) - r * 0.5f;
            return RoundAxial(q, r);
        }

        private static HexCoordinates RoundAxial(float q, float r)
        {
            var x = q;
            var z = r;
            var y = -x - z;
            var roundedX = Mathf.RoundToInt(x);
            var roundedY = Mathf.RoundToInt(y);
            var roundedZ = Mathf.RoundToInt(z);
            var xDifference = Mathf.Abs(roundedX - x);
            var yDifference = Mathf.Abs(roundedY - y);
            var zDifference = Mathf.Abs(roundedZ - z);

            if (xDifference > yDifference && xDifference > zDifference)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (yDifference > zDifference)
            {
                roundedY = -roundedX - roundedZ;
            }
            else
            {
                roundedZ = -roundedX - roundedY;
            }

            return new HexCoordinates(roundedX, roundedZ);
        }

        private static void ValidateRadius(float outerRadius)
        {
            if (outerRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(outerRadius));
            }
        }
    }
}
