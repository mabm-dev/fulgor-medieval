using Fulgor.Core.Campaign;
using NUnit.Framework;
namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignRivalReserveTests
    {
        [TestCase(19,20,false)]
        [TestCase(20,19,false)]
        [TestCase(20,20,true)]
        [TestCase(100,100,true)]
        public void ReserveRequiresTwoClosingsAfterPayingRecruitment(int grain,int gold,bool expected)
        {
            Assert.That(CampaignRivalReserve.CanAfford(new CampaignResources(grain,10,10,gold,10),new CampaignResources(10,0,0,10,0),new CampaignResources(5,0,0,5,0)),Is.EqualTo(expected));
        }
        [Test]public void RecruitmentStillRequiresOtherResources()
        {Assert.That(CampaignRivalReserve.CanAfford(new CampaignResources(100,0,0,100,0),new CampaignResources(0,1,0,0,0),default),Is.False);}
        [Test]public void LargeUpkeepCannotOverflowIntoAffordableReserve()
        {Assert.That(CampaignRivalReserve.CanAfford(new CampaignResources(int.MaxValue,0,0,int.MaxValue,0),default,new CampaignResources(int.MaxValue,0,0,int.MaxValue,0)),Is.False);}
    }
}
