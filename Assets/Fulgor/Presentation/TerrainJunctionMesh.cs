using System;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    public static class TerrainJunctionMesh
    {
        public const int OwnedCornerCount = 2;

        public static HexCoordinates FirstNeighbor(HexCoordinates origin, int corner)
        {
            ValidateCorner(corner);
            return corner == 0 ? origin.Add(new HexCoordinates(1, 0))
                : origin.Add(new HexCoordinates(-1, 0));
        }

        public static HexCoordinates SecondNeighbor(HexCoordinates origin, int corner)
        {
            ValidateCorner(corner);
            return corner == 0 ? origin.Add(new HexCoordinates(0, 1))
                : origin.Add(new HexCoordinates(0, -1));
        }

        public static Mesh Create(TerrainType center, TerrainType first, TerrainType second,
            int corner, float tileRadius,
            float outerRadius = VisualScaleContract.HexOuterRadius)
        {
            ValidateCorner(corner);
            if (center == first && first == second)
                throw new ArgumentException("Equal terrain junctions do not need a cap.");
            if (tileRadius <= TerrainTransitionMesh.CoastInset || tileRadius > outerRadius)
                throw new ArgumentOutOfRangeException(nameof(tileRadius));

            int centerVertex = corner == 0 ? 0 : 3;
            int firstVertex = corner == 0 ? 2 : 5;
            int secondVertex = corner == 0 ? 4 : 1;
            var firstOffset = HexWorldLayout.ToWorld(
                FirstNeighbor(new HexCoordinates(0, 0), corner), outerRadius);
            var secondOffset = HexWorldLayout.ToWorld(
                SecondNeighbor(new HexCoordinates(0, 0), corner), outerRadius);
            var p0 = Vertex(centerVertex, Radius(center, tileRadius)) +
                Vector3.up * Height(center);
            var p1 = firstOffset + Vertex(firstVertex, Radius(first, tileRadius)) +
                Vector3.up * Height(first);
            var p2 = secondOffset + Vertex(secondVertex, Radius(second, tileRadius)) +
                Vector3.up * Height(second);
            var junction = Vertex(centerVertex, outerRadius);
            junction.y = (p0.y + p1.y + p2.y) / 3f + 0.002f;

            var mesh = new Mesh { name = $"SM_Junction_{center}_{first}_{second}_{corner}" };
            mesh.vertices = new[] { p0, p1, p2, junction };
            mesh.triangles = UpwardTriangles(mesh.vertices);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Radius(TerrainType terrain, float tileRadius) =>
            terrain == TerrainType.Water ? tileRadius - TerrainTransitionMesh.CoastInset : tileRadius;

        private static float Height(TerrainType terrain) =>
            TerrainVisualStyle.For(terrain).Height + 0.008f;

        private static Vector3 Vertex(int index, float radius)
        {
            float angle = (30f + 60f * index) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static int[] UpwardTriangles(Vector3[] vertices)
        {
            var triangles = new int[9];
            WriteUpward(vertices, triangles, 0, 3, 0, 1);
            WriteUpward(vertices, triangles, 3, 3, 1, 2);
            WriteUpward(vertices, triangles, 6, 3, 2, 0);
            return triangles;
        }

        private static void WriteUpward(Vector3[] vertices, int[] triangles, int offset,
            int a, int b, int c)
        {
            var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            triangles[offset] = a;
            triangles[offset + 1] = normal.y >= 0f ? b : c;
            triangles[offset + 2] = normal.y >= 0f ? c : b;
        }

        private static void ValidateCorner(int corner)
        {
            if (corner < 0 || corner >= OwnedCornerCount)
                throw new ArgumentOutOfRangeException(nameof(corner));
        }
    }
}
