using System;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using NUnit.Framework;

namespace Fulgor.Core.Tests.Campaign
{
    public sealed class CampaignSettlementRegistryTests
    {
        private static MapRouteGraph Map()=>new(new GeneratedMap(2,1,1,new[]{new MapCell(new HexCoordinates(0,0),TerrainType.Plain,false),new MapCell(new HexCoordinates(1,0),TerrainType.Water,false)}));
        private static CampaignSettlement Castle(string id="castle",int q=0)=>new(id,"Fortaleza de Castilla","castilla",new HexCoordinates(q,0));
        [Test] public void RegistryCopiesScenarioAndPreservesIdentityAndOwner()
        {
            var castle=Castle();var source=new List<CampaignSettlement>{castle};var registry=new CampaignSettlementRegistry(Map(),source);source.Clear();
            Assert.That(registry.Settlements.Count,Is.EqualTo(1));Assert.That(registry.TryGet("castle",out var byId),Is.True);
            Assert.That(registry.TryGet(new HexCoordinates(0,0),out var byTile),Is.True);Assert.That(byId,Is.SameAs(byTile));
            Assert.That(byId.OwnerFactionId,Is.EqualTo("castilla"));Assert.That(byId.Name,Is.EqualTo("Fortaleza de Castilla"));
        }
        [TestCase(null,"Name","owner")][TestCase("","Name","owner")][TestCase("id",null,"owner")][TestCase("id"," ","owner")][TestCase("id","Name",null)][TestCase("id","Name","")]
        public void IncompleteIdentityIsRejected(string id,string name,string owner)=>Assert.Throws<ArgumentException>(()=>new CampaignSettlement(id,name,owner,default));
        [Test] public void DuplicatePositionIsRejected()=>Assert.Throws<ArgumentException>(()=>new CampaignSettlementRegistry(Map(),new[]{Castle(),Castle("other")}));
        [Test] public void DuplicateIdIsRejected()
        {
            var graph=new MapRouteGraph(new GeneratedMap(2,1,1,new[]{new MapCell(new HexCoordinates(0,0),TerrainType.Plain,false),new MapCell(new HexCoordinates(1,0),TerrainType.Plain,false)}));
            Assert.Throws<ArgumentException>(()=>new CampaignSettlementRegistry(graph,new[]{Castle(),Castle("castle",1)}));
        }
        [TestCase(1)][TestCase(2)]public void WaterAndMissingCellsAreRejected(int q)=>Assert.Throws<ArgumentException>(()=>new CampaignSettlementRegistry(Map(),new[]{Castle(q:q)}));
        [Test] public void EmptyRegistryAndMissingLookupsAreSafe()
        {
            var registry=new CampaignSettlementRegistry(Map(),Array.Empty<CampaignSettlement>());Assert.That(registry.Settlements,Is.Empty);
            Assert.That(registry.TryGet("missing",out _),Is.False);Assert.That(registry.TryGet(new HexCoordinates(5,5),out _),Is.False);
        }
        [Test] public void MissingInputsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(()=>new CampaignSettlementRegistry(null,new[]{Castle()}));
            Assert.Throws<ArgumentNullException>(()=>new CampaignSettlementRegistry(Map(),null));
            Assert.Throws<ArgumentException>(()=>new CampaignSettlementRegistry(Map(),new CampaignSettlement[]{null}));
        }
    }
}
