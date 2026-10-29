using System;
using System.Linq;
using Fulgor.Core.Campaign;
using NUnit.Framework;
using UnityEngine;
namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignDiplomacyTests
    {
        private static CampaignDiplomacy State(string rival="aragon")=>new("castilla",new[]{"castilla",rival});
        private static CampaignResources Rich=>new(100,100,100,100,100);
        [Test]public void RelationsAreSymmetricAndIntentionsRespectTreaties()
        {
            var d=State();Assert.That(d.CanAttack("castilla","aragon"),Is.True);
            d.SetRelation("aragon",DiplomaticRelation.Peace,DiplomaticIntention.Neutral);Assert.That(d.CanAttack("aragon","castilla"),Is.False);
            d.SetRelation("aragon",DiplomaticRelation.Peace,DiplomaticIntention.Dispute);Assert.That(d.CanAttack("aragon","castilla"),Is.True);
            d.SetRelation("aragon",DiplomaticRelation.Pact,DiplomaticIntention.Conquest);Assert.That(d.CanAttack("castilla","aragon"),Is.False);
            d.SetRelation("aragon",DiplomaticRelation.Trade,DiplomaticIntention.Mission);Assert.That(d.CanAttack("aragon","castilla"),Is.False);
        }
        [Test]public void TreatiesExpireToNeutralPeaceAfterFiveClosings()
        {
            var d=State("castilla-rival");var rivals=new CampaignRivalState(true,new[]{"castilla-rival"});var stock=Rich;
            d.SetRelation("castilla-rival",DiplomaticRelation.Pact,DiplomaticIntention.Neutral);
            for(int n=0;n<4;n++)d.CloseTurn(ref stock,ref rivals);
            Assert.That(d.Relation("castilla-rival").remaining,Is.EqualTo(1));d.CloseTurn(ref stock,ref rivals);
            Assert.That(d.Relation("castilla-rival").relation,Is.EqualTo(DiplomaticRelation.Peace));Assert.That(d.CanAttack("castilla","castilla-rival"),Is.False);
        }
        [Test]public void TradeTransfersBothTreasuriesExactlyOnceAtClosing()
        {
            var d=State();var rivals=new CampaignRivalState(true,new[]{"aragon"}).WithResources("aragon",Rich);var stock=Rich;
            Assert.That(d.Propose("aragon",DiplomaticRelation.Trade,new(0,0,0,2,0),new(0,2,0,0,0)),Is.True);
            Assert.That(d.Propose("aragon",DiplomaticRelation.Trade),Is.False);Assert.That(stock,Is.EqualTo(Rich));
            d.CloseTurn(ref stock,ref rivals);Assert.That(stock.Gold,Is.EqualTo(98));Assert.That(stock.Wood,Is.EqualTo(102));
            Assert.That(rivals.Resources("aragon").Gold,Is.EqualTo(102));Assert.That(rivals.Resources("aragon").Wood,Is.EqualTo(98));
            Assert.That(d.Relation("aragon").remaining,Is.EqualTo(5));d.CloseTurn(ref stock,ref rivals);Assert.That(stock.Gold,Is.EqualTo(98));
        }
        [Test]public void UnaffordableTradeIsRejectedWithoutMintingResources()
        {
            var d=State();var rivals=new CampaignRivalState(true,new[]{"aragon"});var stock=Rich;
            d.Propose("aragon",DiplomaticRelation.Trade,new(0,0,0,2,0),new(0,2,0,0,0));d.CloseTurn(ref stock,ref rivals);
            Assert.That(stock,Is.EqualTo(Rich));Assert.That(rivals.Resources("aragon"),Is.EqualTo(default(CampaignResources)));Assert.That(d.Relation("aragon").relation,Is.EqualTo(DiplomaticRelation.War));
        }
        [Test]public void UnfavorableTradeProducesCounterOfferThatNeedsPlayerAnswer()
        {
            var d=State();var rivals=new CampaignRivalState(true,new[]{"aragon"}).WithResources("aragon",Rich);var stock=Rich;
            d.Propose("aragon",DiplomaticRelation.Trade,new(0,0,0,1,0),new(0,12,0,0,0));d.CloseTurn(ref stock,ref rivals);
            var p=d.Proposals.Single();Assert.That(p.sender,Is.EqualTo("aragon"));Assert.That(p.offerWood,Is.EqualTo(8));Assert.That(p.demandGold,Is.EqualTo(2));
            d.CloseTurn(ref stock,ref rivals);Assert.That(stock,Is.EqualTo(Rich));Assert.That(d.Respond(p.id,true),Is.True);d.CloseTurn(ref stock,ref rivals);
            Assert.That(stock.Gold,Is.EqualTo(98));Assert.That(stock.Wood,Is.EqualTo(108));
        }
        [Test]public void DefensiveRivalOffersPeaceAndPlayerCanRejectThenAccept()
        {
            var d=State("leon");var rivals=new CampaignRivalState(true,new[]{"leon"});var stock=Rich;
            d.CloseTurn(ref stock,ref rivals);var first=d.Proposals.Single();Assert.That(d.Respond(first.id,false),Is.True);
            d.CloseTurn(ref stock,ref rivals);Assert.That(d.Relation("leon").relation,Is.EqualTo(DiplomaticRelation.War));
            d.Respond(d.Proposals.Single().id,true);d.CloseTurn(ref stock,ref rivals);Assert.That(d.CanAttack("castilla","leon"),Is.False);
        }
        [Test]public void SaveRoundTripPreservesQueuedAnswerAndDurationWithoutAliasing()
        {
            var d=State("leon");var rivals=new CampaignRivalState(true,new[]{"leon"});var stock=Rich;d.CloseTurn(ref stock,ref rivals);d.Respond(d.Proposals.Single().id,true);
            var payload=new CampaignSaveData{diplomacy=d.Capture()};var json=JsonUtility.ToJson(payload);var saved=JsonUtility.FromJson<CampaignSaveData>(json);
            var restored=CampaignDiplomacy.Restore(saved.diplomacy,"castilla",new[]{"castilla","leon"});saved.diplomacy.links[0].relation=DiplomaticRelation.Pact;
            Assert.That(restored.Relation("leon").relation,Is.EqualTo(DiplomaticRelation.War));restored.CloseTurn(ref stock,ref rivals);Assert.That(restored.Relation("leon").relation,Is.EqualTo(DiplomaticRelation.Peace));
        }
        [Test]public void InvalidSaveRejectsUnknownFactionsNegativeTermsAndDuplicateLinks()
        {
            var data=State().Capture();data.links[0].faction="unknown";Assert.Throws<ArgumentException>(()=>CampaignDiplomacy.Restore(data,"castilla",new[]{"aragon"}));
            var d=State();d.Propose("aragon",DiplomaticRelation.Trade);data=d.Capture();data.proposals[0].offerGold=-1;Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignDiplomacy.Restore(data,"castilla",new[]{"aragon"}));
            data=State().Capture();data.links=new[]{data.links[0],data.links[0]};Assert.Throws<ArgumentException>(()=>CampaignDiplomacy.Restore(data,"castilla",new[]{"aragon"}));
        }
        [Test]public void JsonRoundTripOfLegacySaveDoesNotMistakeInlineNullForDiplomacy()
        {
            var old=JsonUtility.FromJson<CampaignSaveData>(JsonUtility.ToJson(new CampaignSaveData()));
            Assert.That(old.RestoreDiplomacy("castilla",new[]{"aragon"}).CanAttack("castilla","aragon"),Is.True);
            old.diplomacyVersion=2;Assert.Throws<ArgumentException>(()=>old.RestoreDiplomacy("castilla",new[]{"aragon"}));
        }
        [Test]public void OldSaveKeepsHostilityAndPublicSnapshotsCannotMutateState()
        {
            var d=CampaignDiplomacy.Restore(null,"castilla",new[]{"aragon"});d.Relation("aragon").relation=DiplomaticRelation.Peace;
            Assert.That(d.CanAttack("castilla","aragon"),Is.True);d.Propose("aragon",DiplomaticRelation.Peace);d.Proposals[0].response=2;Assert.That(d.Proposals[0].response,Is.Zero);
        }
    }
}

