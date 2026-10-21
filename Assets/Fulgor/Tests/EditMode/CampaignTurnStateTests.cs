using System;
using Fulgor.Core.Campaign;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignTurnStateTests
    {
        [Test]
        public void CompletingTurnAddsAllIncomeAndDoesNotMutatePreviousState()
        {
            var initial = new CampaignTurnState(1,
                new CampaignResources(5, 4, 3, 2, 1),
                new CampaignResources(2, 1, 0, 3, 4));

            var next = initial.CompleteTurn();

            Assert.That(initial.Turn, Is.EqualTo(1));
            Assert.That(initial.Resources, Is.EqualTo(new CampaignResources(5, 4, 3, 2, 1)));
            Assert.That(next.Turn, Is.EqualTo(2));
            Assert.That(next.Resources, Is.EqualTo(new CampaignResources(7, 5, 3, 5, 5)));
            Assert.That(next.CompleteTurn().Resources,
                Is.EqualTo(new CampaignResources(9, 6, 3, 8, 9)));
        }

        [Test]
        public void SameStateAndIncomeAlwaysProduceSameNextTurn()
        {
            var resources = new CampaignResources(1, 2, 3, 4, 5);
            var income = new CampaignResources(2, 0, 1, 0, 3);
            var first = new CampaignTurnState(7, resources, income).CompleteTurn();
            var second = new CampaignTurnState(7, resources, income).CompleteTurn();

            Assert.That(first.Turn, Is.EqualTo(second.Turn));
            Assert.That(first.Resources, Is.EqualTo(second.Resources));
        }

        [Test]
        public void InvalidStateAndOverflowAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignTurnState(0, default, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignResources(-1, 0, 0, 0, 0));
            var state = new CampaignTurnState(1,
                new CampaignResources(int.MaxValue, 0, 0, 0, 0),
                new CampaignResources(1, 0, 0, 0, 0));
            Assert.Throws<OverflowException>(() => state.CompleteTurn());
        }
    }
}
