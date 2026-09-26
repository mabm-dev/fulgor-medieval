using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Original blockout geometry; no gameplay randomness or per-cell assets.</summary>
    public static class TerrainBlockoutMesh
    {
        public const int VariantCount = 3;

        public static int VariantFor(HexCoordinates coordinates)
        {
            unchecked
            {
                uint hash = (uint)coordinates.Q * 73856093u ^ (uint)coordinates.R * 19349663u;
                hash ^= hash >> 16;
                return (int)(hash % VariantCount);
            }
        }

        public static Mesh Create(TerrainType terrain, int variant, float radius)
        {
            if (variant < 0 || variant >= VariantCount) throw new ArgumentOutOfRangeException(nameof(variant));
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f || radius > 1f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            var height = TerrainVisualStyle.For(terrain).Height;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var top = new Vector3[6];
            var bottom = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (30f + 60f * i) * Mathf.Deg2Rad;
                top[i] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                bottom[i] = new Vector3(top[i].x, 0f, top[i].z);
            }
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                AddTriangle(vertices, triangles, new Vector3(0f, height, 0f), top[next], top[i]);
                AddTriangle(vertices, triangles, Vector3.zero, bottom[i], bottom[next]);
                AddTriangle(vertices, triangles, top[i], top[next], bottom[i]);
                AddTriangle(vertices, triangles, top[next], bottom[next], bottom[i]);
            }
            if (terrain == TerrainType.Forest || terrain == TerrainType.Hill || terrain == TerrainType.Mountain)
            {
                int count = terrain == TerrainType.Mountain ? 2 : 3;
                float relief = terrain == TerrainType.Forest ? 0.48f : terrain == TerrainType.Hill ? 0.20f : 0.58f;
                for (int item = 0; item < count; item++)
                {
                    float angle = (variant * 37f + item * 360f / count) * Mathf.Deg2Rad;
                    var center = new Vector3(Mathf.Cos(angle) * 0.46f * radius, height, Mathf.Sin(angle) * 0.46f * radius);
                    float width = (terrain == TerrainType.Forest ? 0.17f : 0.21f) * radius;
                    int sides = terrain == TerrainType.Mountain ? 5 : 6;
                    var peak = center + Vector3.up * (relief * (1f - item * 0.13f + variant * 0.07f));
                    for (int side = 0; side < sides; side++)
                    {
                        float a = (side * 360f / sides + variant * 17f) * Mathf.Deg2Rad;
                        float b = ((side + 1) * 360f / sides + variant * 17f) * Mathf.Deg2Rad;
                        var first = center + new Vector3(Mathf.Cos(a) * width, 0f, Mathf.Sin(a) * width);
                        var second = center + new Vector3(Mathf.Cos(b) * width, 0f, Mathf.Sin(b) * width);
                        AddTriangle(vertices, triangles, peak, second, first);
                    }
                }
            }
            var mesh = new Mesh { name = $"SM_Blockout_{terrain}_{variant}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }
    }
}