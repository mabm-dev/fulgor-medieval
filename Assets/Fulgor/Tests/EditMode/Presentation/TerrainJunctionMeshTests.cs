using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class TerrainJunctionMeshTests
    {
        [Test]
        public void OwnsTwoUniqueJunctionsPerCell()
        {
            var origin = new HexCoordinates(3, 4);
            Assert.That(TerrainJunctionMesh.FirstNeighbor(origin, 0), Is.EqualTo(new HexCoordinates(4, 4)));
            Assert.That(TerrainJunctionMesh.SecondNeighbor(origin, 0), Is.EqualTo(new HexCoordinates(3, 5)));
            Assert.That(TerrainJunctionMesh.FirstNeighbor(origin, 1), Is.EqualTo(new HexCoordinates(2, 4)));
            Assert.That(TerrainJunctionMesh.SecondNeighbor(origin, 1), Is.EqualTo(new HexCoordinates(3, 3)));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void JunctionCapFacesUpAndStaysNearSharedCorner(int corner)
        {
            var mesh = TerrainJunctionMesh.Create(
                TerrainType.Water, TerrainType.Plain, TerrainType.Mountain, corner, 0.97f);
            try
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles.Length, Is.EqualTo(9));
                foreach (var normal in mesh.normals) Assert.That(normal.y, Is.GreaterThan(0f));
                Assert.That(mesh.bounds.size.x, Is.LessThan(0.3f));
                Assert.That(mesh.bounds.size.z, Is.LessThan(0.3f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void RejectsEqualAndInvalidJunctions()
        {
            Assert.Throws<System.ArgumentException>(() => TerrainJunctionMesh.Create(
                TerrainType.Plain, TerrainType.Plain, TerrainType.Plain, 0, 0.97f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TerrainJunctionMesh.FirstNeighbor(new HexCoordinates(0, 0), 2));
        }
    }
}
