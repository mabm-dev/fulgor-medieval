using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Map
{
    public sealed class ResourceDepositLayoutTests
    {
        private static GeneratedMap Campaign(long seed = 12345) => ProceduralMapGenerator.Generate(24, 16, seed);

        [Test]
        public void TheSameMapAlwaysGivesTheSameDeposits()
        {
            CollectionAssert.AreEquivalent(ResourceDepositLayout.For(Campaign()), ResourceDepositLayout.For(Campaign()));
            CollectionAssert.AreNotEquivalent(ResourceDepositLayout.For(Campaign()), ResourceDepositLayout.For(Campaign(777)));
        }

        [Test]
        public void AConquestMapHasSeventeenOfEachResource()
        {
            var deposits = ResourceDepositLayout.For(Campaign());
            Assert.AreEqual(17, deposits.Count(d => d.Value == ResourceDepositKind.Gold));
            Assert.AreEqual(17, deposits.Count(d => d.Value == ResourceDepositKind.Stone));
        }

        [TestCase(1)]
        [TestCase(12345)]
        [TestCase(98765)]
        public void DepositsOnlyLieOnPlainsAndHillsAndNeverTouch(long seed)
        {
            var map = Campaign(seed);
            var terrain = map.Cells.ToDictionary(c => c.Coordinates, c => c.Terrain);
            var deposits = ResourceDepositLayout.For(map);
            Assert.IsNotEmpty(deposits);
            foreach (var deposit in deposits)
            {
                Assert.That(terrain[deposit.Key], Is.EqualTo(TerrainType.Plain).Or.EqualTo(TerrainType.Hill), deposit.Key.Key);
                foreach (var neighbour in deposit.Key.Neighbors()) Assert.IsFalse(deposits.ContainsKey(neighbour), deposit.Key.Key);
            }
        }

        [Test]
        public void GoldLeansTowardsTheHills()
        {
            int goldOnHill = 0, gold = 0, stoneOnHill = 0, stone = 0;
            for (long seed = 1; seed <= 40; seed++)
            {
                var map = Campaign(seed);
                var terrain = map.Cells.ToDictionary(c => c.Coordinates, c => c.Terrain);
                foreach (var deposit in ResourceDepositLayout.For(map))
                {
                    bool hill = terrain[deposit.Key] == TerrainType.Hill;
                    if (deposit.Value == ResourceDepositKind.Gold) { gold++; if (hill) goldOnHill++; }
                    else { stone++; if (hill) stoneOnHill++; }
                }
            }
            Assert.Greater((double)goldOnHill / gold, (double)stoneOnHill / stone);
        }

        [Test]
        public void AVerySmallMapHasNoDeposits()
        {
            var map = new GeneratedMap(2, 1, 5, new[] { new MapCell(new HexCoordinates(0, 0), TerrainType.Plain, true), new MapCell(new HexCoordinates(1, 0), TerrainType.Hill, true) });
            Assert.IsEmpty(ResourceDepositLayout.For(map));
            Assert.AreEqual(ResourceDepositKind.None, ResourceDepositLayout.At(map, new HexCoordinates(0, 0)));
        }

        [Test]
        public void IncomeComesFromTheDepositsAndNotFromTheGeneratorGoldFlag()
        {
            var map = Campaign();
            var deposits = ResourceDepositLayout.For(map);
            var income = CampaignIncomeCalculator.FromMap(map);
            Assert.AreEqual(deposits.Count(d => d.Value == ResourceDepositKind.Gold), income.Gold);
            Assert.AreEqual(map.Cells.Count(c => c.Terrain == TerrainType.Hill) + deposits.Count(d => d.Value == ResourceDepositKind.Stone), income.Stone, "La colina da su piedra y la montaña ya no.");
        }
    }
}
