using System;
using System.Linq;
using Fulgor.Core.Campaign;
using NUnit.Framework;

namespace Fulgor.Core.Tests
{
    public sealed class CampaignResearchTests
    {
        [Test] public void ResearchAdvancesOnlyOnTurnsAndKeepsPartialProgressAfterRestore()
        {
            var state=new CampaignResearch();
            Assert.IsTrue(state.TryStart("cultivation",10,1));
            Assert.IsFalse(state.TryStart("tools",10,1));
            Assert.IsFalse(state.Advance(1));
            state=CampaignResearch.Restore(state.Capture());
            Assert.AreEqual(1,state.Progress);
            Assert.AreEqual(default(CampaignResources),state.IncomePerCity);
            Assert.IsTrue(state.Advance(3));
            Assert.AreEqual(2,state.IncomePerCity.Grain);
            Assert.IsNull(state.Active);
            Assert.IsFalse(state.Advance(10));
        }
        [Test] public void AllFiveAgesRequireResearchAndPopulationAndSurviveSave()
        {
            var state=new CampaignResearch();
            foreach(var tech in CampaignTechnologies.All)
            {
                if(tech.AdvancesAge)
                    Assert.IsNotNull(state.BlockReason(tech.Id,0,1));
                Assert.IsTrue(state.TryStart(tech.Id,30,1),tech.Id);
                Assert.IsTrue(state.Advance(tech.Wisdom));
                state=CampaignResearch.Restore(state.Capture());
            }
            Assert.AreEqual(CampaignAge.LateMiddleAges,state.Age);
            Assert.AreEqual(4,state.WisdomPerTurn(1));
            Assert.AreEqual(9,state.IncomePerCity.Grain);
            Assert.AreEqual(CampaignTechnologies.All.Count,state.Capture().completed.Length);
        }
        [Test] public void V23LateKingdomCanStudyCrossbowsAfterLoading()
        {
            var previousCatalog=CampaignTechnologies.All.Where(t=>!t.Optional).Select(t=>t.Id).ToArray();
            var state=CampaignResearch.Restore(new CampaignResearchData{age=4,completed=previousCatalog});
            Assert.IsTrue(state.TryStart("crossbow_craft",20,1));
            state.Advance(4);
            state=CampaignResearch.Restore(state.Capture());
            Assert.IsTrue(state.Advance(10));
            Assert.IsTrue(CampaignResearch.Restore(state.Capture()).Has("crossbow_craft"));
            Assert.AreEqual(CampaignAge.LateMiddleAges,state.Age);
        }
        [Test]
        public void KingdomCanAdvanceWithoutSeedSelectionAndStudyItAfterLoading()
        {
            var state = CampaignResearch.Restore(new CampaignResearchData {
                age = (int)CampaignAge.EarlyMiddleAges,
                completed = new[] { "cultivation", "tools", "settlement" }
            });
            Assert.IsTrue(state.TryStart("seed_selection", 10, 1));
            Assert.IsFalse(state.Advance(2));
            state = CampaignResearch.Restore(state.Capture());
            Assert.IsTrue(state.Advance(2));
            state = CampaignResearch.Restore(state.Capture());
            Assert.IsTrue(state.Has("seed_selection"));
            Assert.AreEqual(CampaignAge.EarlyMiddleAges, state.Age);
        }

        [Test] public void SavesCannotSkipAgeRequirementsOrDuplicateTechnologies()
        {
            Assert.Throws<ArgumentException>(()=>CampaignResearch.Restore(new CampaignResearchData{age=1,completed=new[]{"settlement"}}));
            Assert.Throws<ArgumentException>(()=>CampaignResearch.Restore(new CampaignResearchData{completed=new[]{"cultivation","cultivation"}}));
            Assert.Throws<ArgumentException>(()=>CampaignResearch.Restore(new CampaignResearchData{completed=Array.Empty<string>(),active="cultivation",progress=4}));
            Assert.AreEqual(CampaignAge.Migrations,new CampaignSaveData().RestoreResearch().Age);
        }
    }
}
