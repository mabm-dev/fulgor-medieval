using System;
using System.Collections.Generic;

namespace Fulgor.Core.Map
{
    /// <summary>Coordenada axial lógica. No depende de UnityEngine.</summary>
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        private static readonly HexCoordinates[] Directions =
        {
            new(1, 0),
            new(1, -1),
            new(0, -1),
            new(-1, 0),
            new(-1, 1),
            new(0, 1),
        };

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;
        public string Key => $"{Q},{R}";

        public HexCoordinates Add(HexCoordinates offset) =>
            new(Q + offset.Q, R + offset.R);

        public IReadOnlyList<HexCoordinates> Neighbors()
        {
            var result = new HexCoordinates[Directions.Length];
            for (var index = 0; index < Directions.Length; index++)
            {
                result[index] = Add(Directions[index]);
            }

            return result;
        }

        public IReadOnlyList<HexCoordinates> CellsInRadius(int radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            var result = new List<HexCoordinates>(1 + 3 * radius * (radius + 1));
            for (var deltaQ = -radius; deltaQ <= radius; deltaQ++)
            {
                var minimumR = Math.Max(-radius, -deltaQ - radius);
                var maximumR = Math.Min(radius, -deltaQ + radius);
                for (var deltaR = minimumR; deltaR <= maximumR; deltaR++)
                {
                    result.Add(Add(new HexCoordinates(deltaQ, deltaR)));
                }
            }

            return result;
        }

        public int DistanceTo(HexCoordinates destination)
        {
            var deltaQ = Q - destination.Q;
            var deltaR = R - destination.R;
            var deltaS = deltaQ + deltaR;
            return (Math.Abs(deltaQ) + Math.Abs(deltaR) + Math.Abs(deltaS)) / 2;
        }

        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public override string ToString() => Key;
        public static bool operator ==(HexCoordinates left, HexCoordinates right) => left.Equals(right);
        public static bool operator !=(HexCoordinates left, HexCoordinates right) => !left.Equals(right);
    }
}
