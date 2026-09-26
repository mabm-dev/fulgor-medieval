using System;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public enum TerrainTransitionKind { None, Coast, Height }

    public static class TerrainTransitionMesh
    {
        public const int OwnedDirectionCount = 3;
        public const int VariantCount = 3;
        public const float CoastInset = 0.16f;
        private static readonly HexCoordinates[] OwnedDirections =
        {
            new HexCoordinates(1, 0), new HexCoordinates(1, -1), new HexCoordinates(0, -1),
        };

        public static HexCoordinates Neighbor(HexCoordinates origin, int direction)
        {
            ValidateDirection(direction);
            return origin.Add(OwnedDirections[direction]);
        }

        public static int VariantFor(HexCoordinates coordinates, int direction)
        {
            ValidateDirection(direction);
            unchecked
            {
                uint hash = (uint)coordinates.Q * 73856093u ^
                    (uint)coordinates.R * 19349663u ^ (uint)direction * 83492791u;
                hash ^= hash >> 16;
                return (int)(hash % VariantCount);
            }
        }
        public static TerrainTransitionKind Classify(TerrainType first, TerrainType second)
        {
            if (first == second) return TerrainTransitionKind.None;
            return first == TerrainType.Water || second == TerrainType.Water
                ? TerrainTransitionKind.Coast : TerrainTransitionKind.Height;
        }

        public static Mesh Create(TerrainType first, TerrainType second, int direction,
            float tileRadius, float outerRadius = VisualScaleContract.HexOuterRadius, int variant = 0)
        {
            ValidateDirection(direction);
            if (variant < 0 || variant >= VariantCount)
                throw new ArgumentOutOfRangeException(nameof(variant));
            if (Classify(first, second) == TerrainTransitionKind.None)
                throw new ArgumentException("Equal terrains do not need a transition.");
            ValidateRadius(tileRadius, nameof(tileRadius));
            ValidateRadius(outerRadius, nameof(outerRadius));
            if (tileRadius > outerRadius) throw new ArgumentOutOfRangeException(nameof(tileRadius));

            var firstRadius = first == TerrainType.Water
                ? tileRadius - CoastInset : tileRadius;
            var secondRadius = second == TerrainType.Water
                ? tileRadius - CoastInset : tileRadius;
            var firstEdge = Edge(direction, firstRadius);
            var offset = HexWorldLayout.ToWorld(OwnedDirections[direction], outerRadius);
            var secondEdge = OppositeEdge(direction, secondRadius, offset);
            var firstHeight = TerrainVisualStyle.For(first).Height + 0.006f;
            var secondHeight = TerrainVisualStyle.For(second).Height + 0.006f;
            var firstMiddle = (firstEdge.Item1 + firstEdge.Item2) * 0.5f;
            var secondMiddle = (secondEdge.Item1 + secondEdge.Item2) * 0.5f;
            var across = secondMiddle - firstMiddle;
            across.y = 0f;
            var bend = across.normalized * ((variant - 1) * 0.032f);
            firstMiddle += bend;
            secondMiddle += bend;
            var mesh = new Mesh { name = $"SM_Transition_{first}_{second}_{direction}_{variant}" };
            mesh.vertices = new[]
            {
                firstEdge.Item1 + Vector3.up * firstHeight,
                firstMiddle + Vector3.up * firstHeight,
                firstEdge.Item2 + Vector3.up * firstHeight,
                secondEdge.Item1 + Vector3.up * secondHeight,
                secondMiddle + Vector3.up * secondHeight,
                secondEdge.Item2 + Vector3.up * secondHeight,
            };
            mesh.triangles = new[] { 0, 1, 3, 1, 4, 3, 1, 2, 4, 2, 5, 4 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Tuple<Vector3, Vector3> Edge(int direction, float radius)
        {
            int first = direction == 0 ? 5 : direction == 1 ? 4 : 3;
            int second = direction == 0 ? 0 : direction == 1 ? 5 : 4;
            return Tuple.Create(Vertex(first, radius), Vertex(second, radius));
        }

        private static Tuple<Vector3, Vector3> OppositeEdge(int direction, float radius, Vector3 offset)
        {
            int first = direction == 0 ? 2 : direction == 1 ? 1 : 0;
            int second = direction == 0 ? 3 : direction == 1 ? 2 : 1;
            return Tuple.Create(offset + Vertex(first, radius), offset + Vertex(second, radius));
        }

        private static Vector3 Vertex(int index, float radius)
        {
            float angle = (30f + 60f * index) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static void ValidateDirection(int direction)
        {
            if (direction < 0 || direction >= OwnedDirectionCount)
                throw new ArgumentOutOfRangeException(nameof(direction));
        }

        private static void ValidateRadius(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
