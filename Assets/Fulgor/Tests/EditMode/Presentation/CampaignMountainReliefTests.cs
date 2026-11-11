using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignMountainReliefTests
    {
        private static readonly HexCoordinates Lonely = new(0, 0), Left = new(6, 0), Right = new(7, 0);
        private static readonly HexCoordinates HillAlone = new(0, 6), HillBeside = new(3, 6), MountainBeside = new(4, 6);
        private const float Edge = CampaignMountainRelief.EdgeHeight;

        private static CampaignMountainRelief Relief(int seed = 7) => new(new[] { Lonely, Left, Right }, seed);

        private static CampaignMountainRelief WithHills() =>
            new(new[] { Lonely, MountainBeside }, 7, VisualScaleContract.HexOuterRadius, new[] { HillAlone, HillBeside });

        private static (Vector2 a, Vector2 b) SharedEdge(CampaignMountainRelief relief, HexCoordinates first, HexCoordinates second)
        {
            var c0 = relief.Centre(first); var c1 = relief.Centre(second);
            var middle = (c0 + c1) * .5f; var along = new Vector2(-(c1 - c0).y, (c1 - c0).x).normalized * .5f;
            return (middle - along, middle + along);
        }

        // cada punto de la arista compartida existe en las dos mallas, con la misma altura y la misma normal; devuelve cuántos hay
        private static int MatchSharedEdge(CampaignMountainRelief relief, HexCoordinates first, Mesh firstMesh, HexCoordinates second, Mesh secondMesh)
        {
            var (a, b) = SharedEdge(relief, first, second);
            var edge = b - a;
            Vector2 Board(Vector3 v, HexCoordinates cell) => new Vector2(v.x, v.z) + relief.Centre(cell);
            bool OnEdge(Vector2 p) { float t = Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude); return Vector2.Distance(p, a + edge * t) < 1e-4f; }
            var firstVertices = firstMesh.vertices; var firstNormals = firstMesh.normals;
            var secondVertices = secondMesh.vertices; var secondNormals = secondMesh.normals;
            int matched = 0;
            for (int i = 0; i < firstVertices.Length; i++)
            {
                var p = Board(firstVertices[i], first);
                if (!OnEdge(p) || Mathf.Abs(firstVertices[i].y - relief.Height(p)) > 1e-4f) continue; // el faldón queda 6 cm por debajo de la superficie
                int j = Enumerable.Range(0, secondVertices.Length).FirstOrDefault(r =>
                    Vector2.Distance(Board(secondVertices[r], second), p) < 1e-4f && Mathf.Abs(secondVertices[r].y - firstVertices[i].y) < .03f);
                Assert.Less(Vector2.Distance(Board(secondVertices[j], second), p), 1e-4f, "Cada punto de la arista compartida existe en las dos casillas.");
                Assert.AreEqual(firstVertices[i].y, secondVertices[j].y, 1e-5f, "Misma altura a los dos lados.");
                Assert.Less(Vector3.Distance(firstNormals[i], secondNormals[j]), 1e-3f, "Misma normal a los dos lados: sin costura de luz.");
                matched++;
            }
            return matched;
        }

        [Test]
        public void TheFootOfAMountainMeetsTheNeighbouringTilesAtTheirEdgeHeight()
        {
            var relief = Relief();
            for (int k = 0; k < 6; k++)
                for (int s = 0; s <= 20; s++)
                {
                    var p = Vector2.Lerp(relief.Corner(Lonely, k), relief.Corner(Lonely, (k + 1) % 6), s / 20f);
                    Assert.AreEqual(Edge, relief.Height(p), 1e-5f, $"arista {k}, punto {s}");
                }
        }

        [Test]
        public void ALonelyMountainRisesToAPeakNearItsCentre()
        {
            var relief = Relief();
            float centre = relief.Height(relief.Centre(Lonely));
            Assert.Greater(centre, Edge + .5f, "La cumbre de una montaña suelta tiene que destacar.");
            Assert.Less(centre, Edge + 1.6f);
            float halfway = relief.Height(Vector2.Lerp(relief.Centre(Lonely), relief.Corner(Lonely, 0), .5f));
            Assert.Greater(centre, halfway);
            Assert.Greater(halfway, Edge);
        }

        [Test]
        public void NeighbouringMountainsShareARidgeInsteadOfDroppingToTheValley()
        {
            var relief = Relief();
            var (a, b) = SharedEdge(relief, Left, Right);
            float saddle = relief.Height((a + b) * .5f);
            Assert.Greater(saddle, Edge + .25f, "Dos montañas vecinas se unen por una cresta, no por un valle.");
            // una montaña suelta junto a llanura sí baja hasta el borde
            var (c, d) = SharedEdge(relief, Lonely, new HexCoordinates(1, 0));
            Assert.AreEqual(Edge, relief.Height((c + d) * .5f), 1e-5f);
        }

        [Test]
        public void NeighbouringMountainTilesMatchAlongTheirSharedEdge()
        {
            var relief = Relief();
            var left = relief.BuildTile(Left, 12, "izquierda"); var right = relief.BuildTile(Right, 12, "derecha");
            Assert.AreEqual(13, MatchSharedEdge(relief, Left, left, Right, right), "La arista de 12 tramos tiene 13 puntos.");
            Object.DestroyImmediate(left); Object.DestroyImmediate(right);
        }

        [Test]
        public void AHillRisesGentlyAndMeetsThePlainAtTheEdgeHeight()
        {
            var relief = WithHills();
            Assert.IsTrue(relief.IsHighland(HillAlone)); Assert.IsFalse(relief.IsMountain(HillAlone));
            for (int k = 0; k < 6; k++)
                for (int s = 0; s <= 10; s++)
                    Assert.AreEqual(Edge, relief.Height(Vector2.Lerp(relief.Corner(HillAlone, k), relief.Corner(HillAlone, (k + 1) % 6), s / 10f)), 1e-5f);
            float top = relief.Height(relief.Centre(HillAlone));
            Assert.Greater(top, Edge + .1f, "Una colina se nota.");
            Assert.Less(top, Edge + .45f, "Pero es una loma, no una montaña.");
            Assert.Less(top, relief.Height(relief.Centre(Lonely)), "Una colina queda por debajo de una montaña.");
            Assert.AreEqual(0f, relief.Rise(relief.Centre(HillAlone)), "En la colina no hay roca de montaña.");
        }

        [Test]
        public void AHillAndANeighbouringMountainMatchAlongTheirEdge()
        {
            var relief = WithHills();
            var hill = relief.BuildTile(HillBeside, 12, "colina"); var mountain = relief.BuildTile(MountainBeside, 12, "montaña");
            Assert.AreEqual(13, MatchSharedEdge(relief, HillBeside, hill, MountainBeside, mountain));
            var (a, b) = SharedEdge(relief, HillBeside, MountainBeside);
            Assert.Greater(relief.Height((a + b) * .5f), Edge + .05f, "Entre colina y montaña no se baja a la llanura: es ladera.");
            Object.DestroyImmediate(hill); Object.DestroyImmediate(mountain);
        }

        [Test]
        public void AHillWithABuildingIsLevelUnderItAndKeepsItsEdges()
        {
            var natural = WithHills();
            var built = new CampaignMountainRelief(new[] { Lonely, MountainBeside }, 7, VisualScaleContract.HexOuterRadius,
                new[] { HillAlone, HillBeside }, new[] { HillBeside, new HexCoordinates(9, 9) });
            Assert.IsTrue(built.IsLevelled(HillBeside)); Assert.IsFalse(built.IsLevelled(HillAlone));
            Assert.IsFalse(built.IsLevelled(new HexCoordinates(9, 9)), "La llanura no se allana: ya es llana.");

            var centre = built.Centre(HillBeside);
            float level = built.Height(centre), lowest = float.MaxValue, highest = float.MinValue;
            for (int k = 0; k < 12; k++)
                for (int s = 1; s <= 4; s++)
                {
                    var p = centre + new Vector2(Mathf.Cos(k * Mathf.PI / 6f), Mathf.Sin(k * Mathf.PI / 6f)) * (CampaignMountainRelief.FlatRadius * s / 4f);
                    Assert.AreEqual(level, built.Height(p), 1e-5f, "Llano en todo el círculo de la huella.");
                    lowest = Mathf.Min(lowest, natural.Height(p)); highest = Mathf.Max(highest, natural.Height(p));
                }
            Assert.That(level, Is.InRange(lowest, highest), "Una terraza a la altura media de la loma: medio cortada, medio rellena.");

            // fuera del llano, el relieve de siempre: las aristas no cambian y casan con la montaña vecina
            for (int k = 0; k < 6; k++)
                for (int s = 0; s <= 10; s++)
                {
                    var p = Vector2.Lerp(built.Corner(HillBeside, k), built.Corner(HillBeside, (k + 1) % 6), s / 10f);
                    Assert.AreEqual(natural.Height(p), built.Height(p), 1e-6f);
                }
            var hill = built.BuildTile(HillBeside, 12, "colina con edificio"); var mountain = built.BuildTile(MountainBeside, 12, "montaña");
            Assert.AreEqual(13, MatchSharedEdge(built, HillBeside, hill, MountainBeside, mountain));
            Object.DestroyImmediate(hill); Object.DestroyImmediate(mountain);
        }

        [Test]
        public void AllTilesAtOnceFollowTheReliefAndMatchAcrossDetailLevels()
        {
            var relief = WithHills();
            var cells = new[] { Lonely, MountainBeside, HillAlone, HillBeside };
            var tiles = relief.BuildTiles(cells, 32);
            Assert.AreEqual(4, tiles.Count);
            try
            {
                foreach (var cell in cells)
                {
                    var levels = tiles[cell];
                    foreach (var (mesh, n) in new[] { (levels.Full, 32), (levels.Medium, 16), (levels.Distant, 8) })
                    {
                        Assert.AreEqual(6 * n * n + 6 * n * 2, mesh.triangles.Length / 3, mesh.name);
                        var v = mesh.vertices; var normals = mesh.normals;
                        for (int i = 0; i < v.Length; i += 5)
                        {
                            var p = new Vector2(v[i].x, v[i].z) + relief.Centre(cell);
                            float surface = relief.Height(p);
                            if (Mathf.Abs(v[i].y - surface) > .03f) continue; // faldón, 6 cm por debajo
                            Assert.AreEqual(surface, v[i].y, 1e-4f, "La misma altura que el relieve.");
                            Assert.Less(Vector3.Distance(relief.LatticeNormal(p), normals[i]), 2e-3f, "La misma normal de la rejilla.");
                        }
                    }
                }
                // la arista de la colina con la montaña vecina casa vértice a vértice, y también entre niveles de detalle distintos
                Assert.AreEqual(33, MatchSharedEdge(relief, HillBeside, tiles[HillBeside].Full, MountainBeside, tiles[MountainBeside].Full));
                Assert.AreEqual(17, MatchSharedEdge(relief, HillBeside, tiles[HillBeside].Medium, MountainBeside, tiles[MountainBeside].Full));
                Assert.AreEqual(9, MatchSharedEdge(relief, MountainBeside, tiles[MountainBeside].Distant, HillBeside, tiles[HillBeside].Medium));
            }
            finally
            {
                foreach (var levels in tiles.Values) { Object.DestroyImmediate(levels.Full); Object.DestroyImmediate(levels.Medium); Object.DestroyImmediate(levels.Distant); }
            }
        }

        [Test]
        public void ADetailOutsideTheLatticeIsBuiltTileByTile()
        {
            var relief = Relief();
            var tiles = relief.BuildTiles(new[] { Lonely }, 12);
            var levels = tiles[Lonely];
            Assert.AreEqual(6 * 12 * 12 + 6 * 12 * 2, levels.Full.triangles.Length / 3);
            Assert.AreEqual(6 * 6 * 6 + 6 * 6 * 2, levels.Medium.triangles.Length / 3);
            Assert.AreEqual(6 * 3 * 3 + 6 * 3 * 2, levels.Distant.triangles.Length / 3);
            Object.DestroyImmediate(levels.Full); Object.DestroyImmediate(levels.Medium); Object.DestroyImmediate(levels.Distant);
            Assert.Throws<System.ArgumentException>(() => relief.BuildTiles(new[] { new HexCoordinates(30, 30) }, 32), "Solo colinas y montañas.");
        }

        [Test]
        public void TheReliefIsTheSameForTheSameSeed()
        {
            var p = Vector2.Lerp(Relief().Centre(Left), Relief().Centre(Right), .3f);
            Assert.AreEqual(Relief(7).Height(p), Relief(7).Height(p), 1e-6f);
            Assert.AreNotEqual(Relief(7).Height(p), Relief(8).Height(p));
        }

        [Test]
        public void ATileMeshIsValidAndFacesUp()
        {
            var relief = Relief();
            var mesh = relief.BuildTile(Lonely, 16, "suelta");
            Assert.IsFalse(mesh.vertices.Any(v => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)));
            Assert.AreEqual(mesh.vertexCount, mesh.colors.Length);
            // 6·16² triángulos de superficie y 2 por tramo de faldón
            Assert.AreEqual(6 * 16 * 16 + 6 * 16 * 2, mesh.triangles.Length / 3);
            Assert.Greater(mesh.normals.Count(n => n.y > .2f), mesh.vertexCount * .7f, "La superficie mira hacia arriba.");
            var tri = mesh.triangles; var v = mesh.vertices;
            var face = Vector3.Cross(v[tri[1]] - v[tri[0]], v[tri[2]] - v[tri[0]]);
            Assert.Greater(face.y, 0f, "Los triángulos están en el sentido que Unity dibuja desde arriba.");
            Object.DestroyImmediate(mesh);
        }
    }
}
