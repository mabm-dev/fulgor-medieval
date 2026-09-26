using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class TerrainBlockoutMeshTests
    {
        [TestCase(TerrainType.Plain)]
        [TestCase(TerrainType.Water)]
        [TestCase(TerrainType.Forest)]
        [TestCase(TerrainType.Hill)]
        [TestCase(TerrainType.Mountain)]
        public void VariantsKeepFootprintGroundPivotAndSelectableCenter(TerrainType terrain)
        {
            for (int variant = 0; variant < TerrainBlockoutMesh.VariantCount; variant++)
            {
                var mesh = TerrainBlockoutMesh.Create(terrain, variant, 1f);
                var instance = new GameObject("Temporary terrain validation");
                try
                {
                    Assert.That(mesh.bounds.min.y, Is.EqualTo(0f).Within(0.001f));
                    Assert.That(mesh.bounds.size.x, Is.EqualTo(VisualScaleContract.HexWidth).Within(0.001f));
                    Assert.That(mesh.bounds.size.z, Is.EqualTo(VisualScaleContract.HexDepth).Within(0.001f));
                    foreach (var vertex in mesh.vertices)
                    {
                        Assert.That(float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsNaN(vertex.z), Is.False);
                        Assert.That(new Vector2(vertex.x, vertex.z).magnitude, Is.LessThanOrEqualTo(1.001f));
                    }
                    var collider = instance.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    Physics.SyncTransforms();
                    Assert.That(collider.Raycast(new Ray(new Vector3(0f, 3f, 0f), Vector3.down), out var hit, 4f), Is.True);
                    Assert.That(hit.point.y, Is.EqualTo(TerrainVisualStyle.For(terrain).Height).Within(0.001f));
                    Assert.That(hit.normal.y, Is.GreaterThan(0.99f));
                }
                finally { Object.DestroyImmediate(instance); Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void ReliefVariantsDifferAndCoordinateChoiceIsStable()
        {
            var first = TerrainBlockoutMesh.Create(TerrainType.Mountain, 0, 1f);
            var second = TerrainBlockoutMesh.Create(TerrainType.Mountain, 1, 1f);
            var repeated = TerrainBlockoutMesh.Create(TerrainType.Mountain, 0, 1f);
            try
            {
                Assert.That(first.vertices.SequenceEqual(second.vertices), Is.False);
                Assert.That(first.vertices, Is.EqualTo(repeated.vertices));
                for (int q = -20; q <= 20; q++)
                    for (int r = -20; r <= 20; r++)
                    {
                        var coordinate = new HexCoordinates(q, r);
                        int variant = TerrainBlockoutMesh.VariantFor(coordinate);
                        Assert.That(variant, Is.InRange(0, TerrainBlockoutMesh.VariantCount - 1));
                        Assert.That(TerrainBlockoutMesh.VariantFor(coordinate), Is.EqualTo(variant));
                    }
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); Object.DestroyImmediate(repeated); }
        }

        [Test]
        public void RejectsInvalidParameters()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainBlockoutMesh.Create(TerrainType.Hill, -1, 1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainBlockoutMesh.Create(TerrainType.Hill, 3, 1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainBlockoutMesh.Create(TerrainType.Hill, 0, float.NaN));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainBlockoutMesh.Create(TerrainType.Hill, 0, 0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainBlockoutMesh.Create((TerrainType)999, 0, 1f));
        }
    }
}