using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class TerrainTransitionMeshTests
    {
        [Test]
        public void ClassifiesOnlyUnequalTerrainPairs()
        {
            Assert.That(TerrainTransitionMesh.Classify(TerrainType.Plain, TerrainType.Plain),
                Is.EqualTo(TerrainTransitionKind.None));
            Assert.That(TerrainTransitionMesh.Classify(TerrainType.Water, TerrainType.Forest),
                Is.EqualTo(TerrainTransitionKind.Coast));
            Assert.That(TerrainTransitionMesh.Classify(TerrainType.Mountain, TerrainType.Water),
                Is.EqualTo(TerrainTransitionKind.Coast));
            Assert.That(TerrainTransitionMesh.Classify(TerrainType.Hill, TerrainType.Mountain),
                Is.EqualTo(TerrainTransitionKind.Height));
        }

        [Test]
        public void OwnsThreeUniqueNeighborEdges()
        {
            var origin = new HexCoordinates(4, 7);
            Assert.That(TerrainTransitionMesh.Neighbor(origin, 0), Is.EqualTo(new HexCoordinates(5, 7)));
            Assert.That(TerrainTransitionMesh.Neighbor(origin, 1), Is.EqualTo(new HexCoordinates(5, 6)));
            Assert.That(TerrainTransitionMesh.Neighbor(origin, 2), Is.EqualTo(new HexCoordinates(4, 6)));
        }

        [TestCase(TerrainType.Water, TerrainType.Plain)]
        [TestCase(TerrainType.Plain, TerrainType.Forest)]
        [TestCase(TerrainType.Mountain, TerrainType.Hill)]
        public void BridgeConnectsBothTileEdges(TerrainType first, TerrainType second)
        {
            for (var direction = 0; direction < TerrainTransitionMesh.OwnedDirectionCount; direction++)
            {
                var mesh = TerrainTransitionMesh.Create(first, second, direction, 0.97f);
                try
                {
                    Assert.That(mesh.vertexCount, Is.EqualTo(6));
                    Assert.That(mesh.triangles.Length, Is.EqualTo(12));
                    Assert.That(mesh.normals[0].y, Is.GreaterThan(0f));
                    Assert.That(mesh.bounds.size.x, Is.GreaterThan(0f));
                    Assert.That(mesh.bounds.size.z, Is.GreaterThan(0f));
                    Assert.That(mesh.bounds.min.y, Is.EqualTo(
                        Mathf.Min(TerrainVisualStyle.For(first).Height,
                            TerrainVisualStyle.For(second).Height) + 0.006f).Within(0.001f));
                    Assert.That(mesh.bounds.max.y, Is.EqualTo(
                        Mathf.Max(TerrainVisualStyle.For(first).Height,
                            TerrainVisualStyle.For(second).Height) + 0.006f).Within(0.001f));
                }
                finally { Object.DestroyImmediate(mesh); }
            }
        }

        [Test]
        public void CoastExtendsIntoWaterEnoughToReadFromStrategicCamera()
        {
            var mesh = TerrainTransitionMesh.Create(TerrainType.Water, TerrainType.Plain, 0, 0.97f);
            try
            {
                var vertices = mesh.vertices;
                var waterMiddle = (vertices[0] + vertices[1]) * 0.5f;
                var landMiddle = (vertices[2] + vertices[3]) * 0.5f;
                var width = Vector2.Distance(
                    new Vector2(waterMiddle.x, waterMiddle.z),
                    new Vector2(landMiddle.x, landMiddle.z));
                Assert.That(width, Is.GreaterThan(0.15f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
        [Test]
        public void ProfilesAreStableAndVisiblyDifferent()
        {
            var first = TerrainTransitionMesh.Create(TerrainType.Water, TerrainType.Plain, 0, 0.97f, 1f, 0);
            var second = TerrainTransitionMesh.Create(TerrainType.Water, TerrainType.Plain, 0, 0.97f, 1f, 2);
            var repeated = TerrainTransitionMesh.Create(TerrainType.Water, TerrainType.Plain, 0, 0.97f, 1f, 0);
            try
            {
                Assert.That(first.vertices, Is.EqualTo(repeated.vertices));
                Assert.That(first.vertices, Is.Not.EqualTo(second.vertices));
                var coordinate = new HexCoordinates(7, 5);
                for (var direction = 0; direction < TerrainTransitionMesh.OwnedDirectionCount; direction++)
                {
                    var variant = TerrainTransitionMesh.VariantFor(coordinate, direction);
                    Assert.That(variant, Is.InRange(0, TerrainTransitionMesh.VariantCount - 1));
                    Assert.That(TerrainTransitionMesh.VariantFor(coordinate, direction), Is.EqualTo(variant));
                }
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(repeated);
            }
        }
        [Test]
        public void RejectsInvalidOrUnneededTransitions()
        {
            Assert.Throws<System.ArgumentException>(() => TerrainTransitionMesh.Create(
                TerrainType.Forest, TerrainType.Forest, 0, 0.97f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainTransitionMesh.Neighbor(
                new HexCoordinates(0, 0), 3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainTransitionMesh.Create(
                TerrainType.Water, TerrainType.Plain, 0, 0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TerrainTransitionMesh.Create(
                TerrainType.Water, TerrainType.Plain, 0, 1.01f));
        }
    }
}
