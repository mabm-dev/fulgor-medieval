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

        private static CampaignMountainRelief Relief(int seed = 7) => new(new[] { Lonely, Left, Right }, seed);

        private static (Vector2 a, Vector2 b) SharedEdge(CampaignMountainRelief relief, HexCoordinates first, HexCoordinates second)
        {
            var c0 = relief.Centre(first); var c1 = relief.Centre(second);
            var middle = (c0 + c1) * .5f; var along = new Vector2(-(c1 - c0).y, (c1 - c0).x).normalized * .5f;
            return (middle - along, middle + along);
        }

        [Test]
        public void TheFootOfAMountainMeetsTheNeighbouringTilesAtTheirEdgeHeight()
        {
            var relief = Relief();
            for (int k = 0; k < 6; k++)
                for (int s = 0; s <= 20; s++)
                {
                    var p = Vector2.Lerp(relief.Corner(Lonely, k), relief.Corner(Lonely, (k + 1) % 6), s / 20f);
                    Assert.AreEqual(CampaignMountainRelief.EdgeHeight, relief.Height(p), 1e-5f, $"arista {k}, punto {s}");
                }
        }

        [Test]
        public void ALonelyMountainRisesToAPeakNearItsCentre()
        {
            var relief = Relief();
            float centre = relief.Height(relief.Centre(Lonely));
            Assert.Greater(centre, CampaignMountainRelief.EdgeHeight + .5f, "La cumbre de una montaña suelta tiene que destacar.");
            Assert.Less(centre, CampaignMountainRelief.EdgeHeight + 1.3f);
            float halfway = relief.Height(Vector2.Lerp(relief.Centre(Lonely), relief.Corner(Lonely, 0), .5f));
            Assert.Greater(centre, halfway);
            Assert.Greater(halfway, CampaignMountainRelief.EdgeHeight);
        }

        [Test]
        public void NeighbouringMountainsShareARidgeInsteadOfDroppingToTheValley()
        {
            var relief = Relief();
            var (a, b) = SharedEdge(relief, Left, Right);
            float saddle = relief.Height((a + b) * .5f);
            Assert.Greater(saddle, CampaignMountainRelief.EdgeHeight + .25f, "Dos montañas vecinas se unen por una cresta, no por un valle.");
            // una montaña suelta junto a llanura sí baja hasta el borde
            var (c, d) = SharedEdge(relief, Lonely, new HexCoordinates(1, 0));
            Assert.AreEqual(CampaignMountainRelief.EdgeHeight, relief.Height((c + d) * .5f), 1e-5f);
        }

        [Test]
        public void NeighbouringMountainTilesMatchAlongTheirSharedEdge()
        {
            var relief = Relief();
            var left = relief.BuildTile(Left, 12, "izquierda"); var right = relief.BuildTile(Right, 12, "derecha");
            var (a, b) = SharedEdge(relief, Left, Right);
            var edge = b - a;
            Vector2 Board(Vector3 v, HexCoordinates cell) => new Vector2(v.x, v.z) + relief.Centre(cell);
            bool OnEdge(Vector2 p) { float t = Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude); return Vector2.Distance(p, a + edge * t) < 1e-4f; }
            var rightVertices = right.vertices; var rightNormals = right.normals;
            int matched = 0;
            for (int i = 0; i < left.vertexCount; i++)
            {
                var p = Board(left.vertices[i], Left);
                if (!OnEdge(p) || Mathf.Abs(left.vertices[i].y - relief.Height(p)) > 1e-4f) continue; // el faldón queda 6 cm por debajo de la superficie
                int j = Enumerable.Range(0, rightVertices.Length).FirstOrDefault(r => Vector2.Distance(Board(rightVertices[r], Right), p) < 1e-4f && Mathf.Abs(rightVertices[r].y - left.vertices[i].y) < .03f);
                Assert.Less(Vector2.Distance(Board(rightVertices[j], Right), p), 1e-4f, "Cada punto de la arista compartida existe en las dos casillas.");
                Assert.AreEqual(left.vertices[i].y, rightVertices[j].y, 1e-5f, "Misma altura a los dos lados.");
                Assert.Less(Vector3.Distance(left.normals[i], rightNormals[j]), 1e-3f, "Misma normal a los dos lados: sin costura de luz.");
                matched++;
            }
            Assert.AreEqual(13, matched, "La arista de 12 tramos tiene 13 puntos.");
            Object.DestroyImmediate(left); Object.DestroyImmediate(right);
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
