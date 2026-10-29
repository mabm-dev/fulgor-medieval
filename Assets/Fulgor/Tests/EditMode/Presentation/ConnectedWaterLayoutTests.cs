using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class ConnectedWaterLayoutTests
    {
        public static IEnumerable<int> NeighborMasks => Enumerable.Range(0, 64);
        private static Dictionary<HexCoordinates, Vector3> Centers()
        {
            var centers = new Dictionary<HexCoordinates, Vector3>();
            for (int r = 0; r < 7; r++) for (int q = 0; q < 7; q++)
            { var c = new HexCoordinates(q, r); centers.Add(c, HexWorldLayout.ToWorld(c)); }
            return centers;
        }
        private static Vector2 Point(Vector3 p) => new(p.x, p.z);

        [TestCaseSource(nameof(NeighborMasks))]
        public void AllNeighborMasksKeepSharedEdgesAndLandBanksContinuous(int mask)
        {
            var centers = Centers(); var center = new HexCoordinates(3, 3); var neighbors = center.Neighbors();
            var water = new HashSet<HexCoordinates> { center };
            for (int i = 0; i < 6; i++) if ((mask & 1 << i) != 0) water.Add(neighbors[i]);
            var layout = new ConnectedWaterLayout(centers, water);
            var first = layout.BuildMesh(centers[center], "TestCenter", 4, 5);
            try
            {
                var vertices = first.vertices; var normals = first.normals;
                for (int direction = 0; direction < 6; direction++)
                {
                    var other = neighbors[direction];
                    if (!water.Contains(other))
                    {
                        var outward = (centers[other] - centers[center]).normalized;
                        var tangent = new Vector3(-outward.z, 0, outward.x);
                        var edge = centers[center] + outward * (Mathf.Sqrt(3f) / 2);
                        for (int i = 0; i <= 8; i++)
                            Assert.That(layout.Height(Point(edge + tangent * (i / 8f - .5f))), Is.EqualTo(.12f).Within(.00001f), "Land edge mask=" + mask);
                        continue;
                    }
                    var second = layout.BuildMesh(centers[other], "TestNeighbor", 4, 5);
                    try
                    {
                        var secondVertices = second.vertices; var secondNormals = second.normals;
                        var lookup = new Dictionary<Vector2Int, int>();
                        for (int i = 0; i < secondVertices.Length; i++)
                        {
                            var p = secondVertices[i] + centers[other];
                            lookup[new Vector2Int(Mathf.RoundToInt(p.x * 10000), Mathf.RoundToInt(p.z * 10000))] = i;
                        }
                        var directionVector = (centers[other] - centers[center]).normalized;
                        var midpoint = (centers[other] + centers[center]) * .5f;
                        int firstEdgeCount = vertices.Count(v => Mathf.Abs(Vector3.Dot(v + centers[center] - midpoint, directionVector)) < .0001f);
                        int secondEdgeCount = secondVertices.Count(v => Mathf.Abs(Vector3.Dot(v + centers[other] - midpoint, directionVector)) < .0001f);
                        int shared = 0;
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            var p = vertices[i] + centers[center];
                            if (!lookup.TryGetValue(new Vector2Int(Mathf.RoundToInt(p.x * 10000), Mathf.RoundToInt(p.z * 10000)), out int j)) continue;
                            shared++;
                            Assert.That(p.y, Is.EqualTo((secondVertices[j] + centers[other]).y).Within(.0001f), "Height seam mask=" + mask);
                            Assert.That(Vector3.Distance(normals[i], secondNormals[j]), Is.LessThan(.003f), "Normal seam mask=" + mask);
                        }
                        Assert.That(shared, Is.GreaterThanOrEqualTo(5), "Both sides must contain the full edge.");
                        Assert.That(shared, Is.EqualTo(firstEdgeCount));
                        Assert.That(shared, Is.EqualTo(secondEdgeCount));
                    }
                    finally { Object.DestroyImmediate(second); }
                }
            }
            finally { Object.DestroyImmediate(first); }
        }

        [Test]
        public void CurveAndThreeWayBranchRemainRiverAndReachEveryPort()
        {
            var centers = Centers(); var center = new HexCoordinates(3, 3); var neighbors = center.Neighbors();
            foreach (var ports in new[] { new[] { 0, 2 }, new[] { 0, 2, 4 }, new[] { 0, 3 } })
            {
                var water = new HashSet<HexCoordinates>(ports.Select(i => neighbors[i])) { center };
                var layout = new ConnectedWaterLayout(centers, water);
                Assert.That(layout.Kinds[center], Is.EqualTo(WaterVisualKind.River));
                if (ports.Length == 2) {
                    var a = (centers[center] + centers[neighbors[ports[0]]]) * .5f;
                    var b = (centers[center] + centers[neighbors[ports[1]]]) * .5f;
                    for (int step = 0; step <= 12; step++) {
                        float t = step / 12f, u = 1 - t;
                        var p = u * u * a + 2 * u * t * centers[center] + t * t * b;
                        Assert.That(layout.Height(Point(p)), Is.EqualTo(.04f).Within(.0001f), "The bend centerline must stay open.");
                    }
                }
                foreach (int i in ports)
                    for (int sample = 3; sample <= 5; sample++)
                    {
                        var p = Vector3.Lerp(centers[center], centers[neighbors[i]], sample / 8f);
                        Assert.That(layout.Height(Point(p)), Is.EqualTo(.04f).Within(.0001f), "A river port must stay open.");
                    }
            }
        }

        [Test]
        public void EnclosedLakeAndBoundarySeaAreDistinguishedWithoutChangingCells()
        {
            var centers = Centers(); var lake = new HexCoordinates(3, 3); var sea = new HexCoordinates(0, 0);
            var water = new HashSet<HexCoordinates> { lake, sea };
            var layout = new ConnectedWaterLayout(centers, water);
            Assert.That(layout.Kinds[lake], Is.EqualTo(WaterVisualKind.Lake));
            Assert.That(layout.Kinds[sea], Is.EqualTo(WaterVisualKind.Sea));
            Assert.That(water.Count, Is.EqualTo(2)); Assert.That(centers.Count, Is.EqualTo(49));
        }
    }
}

