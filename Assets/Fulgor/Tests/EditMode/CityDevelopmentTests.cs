using System;
using Fulgor.Core.Campaign;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CityDevelopmentTests
    {
        private static CampaignResources Funds=>new(100,100,100,100,100);
        [Test] public void NewSettlementContainsOnlyFortressAndTwoHouses()
        {
            var city=new CityDevelopment();Assert.That(city.Houses,Is.EqualTo(2));
            foreach(var id in new[]{"fortress","house-1","house-2"})Assert.That(city.Has(id),Is.True);
            foreach(var project in CityProjects.All)Assert.That(city.Has(project.Id),Is.False);
        }
        [Test] public void MarketCostsArePaidOnceAndVisibilityWaitsForCompletion()
        {
            var city=new CityDevelopment();Assert.That(city.TryStart("market",4,Funds,out var pending,out var remaining),Is.True);
            Assert.That(remaining,Is.EqualTo(new CampaignResources(100,70,90,85,90)));
            Assert.That(city.PendingId,Is.Null);Assert.That(pending.Has("market"),Is.False);
            Assert.That(pending.Advance(5),Is.SameAs(pending));
            Assert.That(pending.TryStart("well",4,remaining,out _,out var unchanged),Is.False);Assert.That(unchanged,Is.EqualTo(remaining));
            var finished=pending.Advance(6);Assert.That(finished.Has("market"),Is.True);Assert.That(finished.PendingId,Is.Null);
            Assert.That(finished.TryStart("market",6,Funds,out _,out _),Is.False);
        }
        [Test] public void MissingResourcesUnknownProjectAndInvalidTurnDoNotMutateState()
        {
            var city=new CityDevelopment();
            foreach(var id in new[]{"market","unknown",null}) {
                Assert.That(city.TryStart(id,1,default,out var next,out var remaining),Is.False);
                Assert.That(next,Is.SameAs(city));Assert.That(remaining,Is.EqualTo(default(CampaignResources)));
            }
            Assert.That(city.TryStart("market",0,Funds,out _,out _),Is.False);
        }
        [Test] public void EachResourceIsRequiredBeforeSpending()
        {
            var cost=CityProjects.Get("market").Cost;
            foreach(var funds in new[]{new CampaignResources(0,29,10,15,10),new CampaignResources(0,30,9,15,10),new CampaignResources(0,30,10,14,10),new CampaignResources(0,30,10,15,9)}) {
                Assert.That(funds.TrySpend(cost,out var remaining),Is.False);Assert.That(remaining,Is.EqualTo(funds));
            }
        }
        [Test] public void SaveRoundTripPreservesBuiltBuildingsAndPendingWorkWithoutSharingArrays()
        {
            new CityDevelopment().TryStart("market",1,Funds,out var pending,out _);
            var built=pending.Advance(3);built.TryStart("house-3",3,Funds,out var next,out _);
            var data=next.Capture("town");var restored=CityDevelopment.Restore(data,3);data.completed[0]="well";
            Assert.That(restored.Has("market"),Is.True);Assert.That(restored.Has("well"),Is.False);
            Assert.That(restored.PendingId,Is.EqualTo("house-3"));Assert.That(restored.Advance(4).Houses,Is.EqualTo(3));
        }
        [Test] public void DamagedCityPayloadsAreRejected()
        {
            foreach(var data in new[]{
                new CityDevelopmentData{settlementId="town",completed=new[]{"unknown"}},
                new CityDevelopmentData{settlementId="town",completed=new[]{"market","market"}},
                new CityDevelopmentData{settlementId="town",completed=new[]{"market"},pendingId="market",completionTurn=5},
                new CityDevelopmentData{settlementId="town",completed=Array.Empty<string>(),pendingId="market",completionTurn=4},
                new CityDevelopmentData{settlementId="town",completed=Array.Empty<string>(),pendingId="market",completionTurn=8},
                new CityDevelopmentData{settlementId="town",completed=Array.Empty<string>(),completionTurn=5},
                new CityDevelopmentData{settlementId="town",completed=null}})
                Assert.Throws<ArgumentException>(()=>CityDevelopment.Restore(data,4));
        }
        [Test] public void SaveValidatesCityIdentityAndLegacyVersionsStartWithBaseSettlement()
        {
            var data=new CityDevelopment().Capture("town");
            var save=new CampaignSaveData{version=3,turn=1,cities=new[]{data}};
            Assert.That(save.RestoreCities(new[]{"town"}).Count,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>save.RestoreCities(new[]{"other"}));
            save.cities=new[]{data,data};Assert.Throws<ArgumentException>(()=>save.RestoreCities(new[]{"town"}));
            foreach(int version in new[]{1,2}){save.version=version;save.cities=null;Assert.That(save.RestoreCities(new[]{"town"}),Is.Empty);}
        }

        [Test] public void RecurringEffectsWaitForCompletionAndDoNotPayAgainWhenAdvanced()
        {
            var initial=new CityDevelopment();
            Assert.That(initial.IncomePerTurn,Is.EqualTo(default(CampaignResources)));
            Assert.That(initial.UpkeepPerTurn,Is.EqualTo(default(CampaignResources)));
            initial.TryStart("market",1,Funds,out var pending,out _);
            Assert.That(pending.Advance(2).IncomePerTurn,Is.EqualTo(default(CampaignResources)));
            var built=pending.Advance(3);
            Assert.That(built.IncomePerTurn,Is.EqualTo(new CampaignResources(0,0,0,8,0)));
            Assert.That(built.UpkeepPerTurn,Is.EqualTo(new CampaignResources(2,0,0,0,0)));
            Assert.That(built.Advance(100).IncomePerTurn,Is.EqualTo(built.IncomePerTurn));
            Assert.That(built.Advance(100).UpkeepPerTurn,Is.EqualTo(built.UpkeepPerTurn));
        }

        [Test] public void WholeTownAndRestoredTownHaveTheSameEconomicEffects()
        {
            var city=new CityDevelopment();int turn=1;
            foreach(var project in CityProjects.All) {
                Assert.That(city.TryStart(project.Id,turn,new CampaignResources(1000,1000,1000,1000,1000),out var pending,out _),Is.True);
                turn+=project.Turns;city=pending.Advance(turn);
            }
            Assert.That(city.IncomePerTurn,Is.EqualTo(new CampaignResources(6,4,3,8,18)));
            Assert.That(city.UpkeepPerTurn,Is.EqualTo(new CampaignResources(8,0,0,3,0)));
            var restored=CityDevelopment.Restore(city.Capture("town"),turn);
            Assert.That(restored.IncomePerTurn,Is.EqualTo(city.IncomePerTurn));
            Assert.That(restored.UpkeepPerTurn,Is.EqualTo(city.UpkeepPerTurn));
        }
    }
}
