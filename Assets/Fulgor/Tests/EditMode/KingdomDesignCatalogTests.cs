using System.Linq;
using System.Text.RegularExpressions;
using Fulgor.Core.Campaign;
using NUnit.Framework;

namespace Fulgor.Core.Tests
{
    public sealed class KingdomDesignCatalogTests
    {
        [Test]
        public void ExistingKingdomsAndCandidatesHaveUniqueIdentitiesAndValidPalettes()
        {
            var all = KingdomDesignCatalog.All;
            Assert.That(all.Count, Is.EqualTo(16));
            Assert.That(all.Count(k => !k.IsCandidate), Is.EqualTo(12));
            Assert.That(all.Select(k => k.Id).Distinct().Count(), Is.EqualTo(all.Count));
            foreach (var kingdom in all)
            {
                Assert.That(KingdomDesignCatalog.Find(kingdom.Id), Is.SameAs(kingdom));
                foreach (var color in new[] { kingdom.PrimaryColor, kingdom.SecondaryColor, kingdom.AccentColor })
                    Assert.That(Regex.IsMatch(color, "^[0-9A-F]{6}$"), Is.True, kingdom.Id);
                Assert.That(kingdom.Building, Is.Not.Empty);
                Assert.That(kingdom.SpecialUnit, Is.Not.Empty);
                Assert.That(kingdom.EliteUnit, Is.Not.Empty);
            }
            Assert.That(KingdomDesignCatalog.Find("unknown"), Is.Null);
        }
    }
}
