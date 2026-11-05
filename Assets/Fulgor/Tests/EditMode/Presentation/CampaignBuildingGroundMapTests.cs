using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignBuildingGroundMapTests
    {
        // del mundo a la raíz de un edificio colocado en una posición y con un giro
        private static Matrix4x4 Placed(Vector3 position, float yaw) => Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one).inverse;

        [Test]
        public void TheDistanceIsNegativeUnderTheBuildingAndGrowsAwayFromIt()
        {
            var map = new CampaignBuildingGroundMap(new Rect(-2f, -2f, 4f, 4f));
            map.Add(Placed(Vector3.zero, 0f), Vector2.zero, new Vector2(.4f, .3f), .05f);
            Assert.AreEqual(1, map.Buildings);
            Assert.Less(map.DistanceAt(Vector2.zero), -.09f, "Bajo el edificio, lo más adentro que se guarda.");
            Assert.AreEqual(.1f, map.DistanceAt(new Vector2(.5f, 0f)), .02f, "A 10 cm del lado largo.");
            Assert.AreEqual(.2f, map.DistanceAt(new Vector2(0f, .5f)), .02f, "A 20 cm del lado corto.");
            Assert.AreEqual(CampaignBuildingGroundMap.Far, map.DistanceAt(new Vector2(1.5f, 1.5f)), 1e-4f, "Lejos, el máximo.");
            Assert.AreEqual(CampaignBuildingGroundMap.Far, map.DistanceAt(new Vector2(9f, 0f)), 1e-4f, "Fuera del mapa, también.");
        }

        [Test]
        public void TheFootprintTurnsWithTheBuilding()
        {
            var map = new CampaignBuildingGroundMap(new Rect(-2f, -2f, 4f, 4f));
            map.Add(Placed(new Vector3(.3f, .12f, -.2f), 90f), Vector2.zero, new Vector2(.5f, .1f), 0f);
            // con 90° de giro el lado largo, en x del edificio, queda a lo largo de z del mundo
            Assert.Less(map.DistanceAt(new Vector2(.3f, .2f)), 0f);
            Assert.Greater(map.DistanceAt(new Vector2(.7f, -.2f)), .2f);
        }

        [Test]
        public void TwoBuildingsKeepTheNearestEdge()
        {
            var map = new CampaignBuildingGroundMap(new Rect(-2f, -2f, 4f, 4f));
            map.Add(Placed(new Vector3(-.5f, 0f, 0f), 0f), Vector2.zero, new Vector2(.2f, .2f), 0f);
            map.Add(Placed(new Vector3(.5f, 0f, 0f), 0f), Vector2.zero, new Vector2(.2f, .2f), 0f);
            Assert.AreEqual(.3f, map.DistanceAt(Vector2.zero), .02f, "Entre los dos, a 30 cm de cada uno.");
            Assert.Less(map.DistanceAt(new Vector2(.5f, 0f)), 0f);
        }

        [Test]
        public void ASmallBuildingHasANarrowerBand()
        {
            var map = new CampaignBuildingGroundMap(new Rect(-2f, -2f, 4f, 4f));
            map.Add(Placed(Vector3.zero, 0f), Vector2.zero, new Vector2(.2f, .2f), .1f, 2f);
            Assert.AreEqual(.2f, map.DistanceAt(new Vector2(.3f, 0f)), .03f, "A 10 cm de la pared, la distancia guardada es el doble.");
            Assert.AreEqual(CampaignBuildingGroundMap.Far, map.DistanceAt(new Vector2(.45f, 0f)), .01f, "La franja se acaba antes.");
        }

        [Test]
        public void TheEncodingKeepsMillimetres()
        {
            for (float d = CampaignBuildingGroundMap.Near; d <= CampaignBuildingGroundMap.Far; d += .013f)
                Assert.AreEqual(d, CampaignBuildingGroundMap.Decode(CampaignBuildingGroundMap.Encode(d)), .001f);
            Assert.AreEqual(byte.MaxValue, CampaignBuildingGroundMap.Encode(3f));
            Assert.AreEqual(0, CampaignBuildingGroundMap.Encode(-3f));
        }

        [Test]
        public void TheUvTransformSpansTheWholeTexture()
        {
            var map = new CampaignBuildingGroundMap(new Rect(-3f, 2f, 10f, 5f));
            var uv = map.UvTransform;
            Assert.AreEqual(0f, -3f * uv.x + uv.z, 1e-5f);
            Assert.AreEqual(0f, 2f * uv.y + uv.w, 1e-5f);
            Assert.AreEqual(1f, (-3f + map.Width * map.Texel) * uv.x + uv.z, 1e-5f);
            Assert.AreEqual(1f, (2f + map.Height * map.Texel) * uv.y + uv.w, 1e-5f);
            Assert.AreEqual(map.Width * map.Height, map.Values.Length);
        }

        [Test]
        public void TheOutlineOfAFootprintIsOnItsEdge()
        {
            var holder = new GameObject("huella");
            try
            {
                var footprint = holder.AddComponent<CampaignBuildingFootprint>();
                footprint.Set(new Vector2(.02f, -.01f), new Vector2(.4f, .3f), .1f, 0f, .01f);
                for (int k = 0; k < 16; k++) Assert.AreEqual(0f, footprint.SignedDistance(footprint.Outline(k * Mathf.PI / 8f)), 1e-4f);
                Assert.AreEqual(-.3f, footprint.SignedDistance(new Vector2(.02f, -.01f)), 1e-5f, "En el centro, a medio lado corto del borde.");
            }
            finally { Object.DestroyImmediate(holder); }
        }
    }
}
