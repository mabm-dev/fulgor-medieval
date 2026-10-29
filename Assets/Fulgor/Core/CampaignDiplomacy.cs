using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    public enum DiplomaticRelation { Peace, Pact, Trade, War }
    public enum DiplomaticIntention { Neutral, Defense, Dispute, Conquest, Mission }
    [Serializable] public sealed class DiplomaticLink
    {
        public string faction; public DiplomaticRelation relation; public DiplomaticIntention intention; public int remaining;
    }
    [Serializable] public sealed class DiplomaticProposal
    {
        public string id, sender, receiver; public DiplomaticRelation type; public int duration;
        public int offerGrain,offerWood,offerStone,offerGold,offerLabor,demandGrain,demandWood,demandStone,demandGold,demandLabor;
        public int response; // 0 pending, 1 accept, 2 reject
        public DiplomaticProposal Clone()=>(DiplomaticProposal)MemberwiseClone();
        public CampaignResources Offer=>new(offerGrain,offerWood,offerStone,offerGold,offerLabor);
        public CampaignResources Demand=>new(demandGrain,demandWood,demandStone,demandGold,demandLabor);
    }
    [Serializable] public sealed class DiplomacyData
    {
        public int schema=1,sequence; public string player; public DiplomaticLink[] links; public DiplomaticProposal[] proposals;
    }
    // All public data is copied at the boundary, including save payloads.
    public sealed class CampaignDiplomacy
    {
        private readonly string player;
        private readonly SortedDictionary<string,DiplomaticLink> links=new(StringComparer.Ordinal);
        private readonly List<DiplomaticProposal> proposals=new();
        private int sequence;
        public string LastReport {get;private set;}="";
        public CampaignDiplomacy(string player,IEnumerable<string> factions)
        {
            if(string.IsNullOrWhiteSpace(player)||factions==null)throw new ArgumentException("Invalid diplomacy participants.");
            this.player=player;
            foreach(var id in factions.Where(f=>f!=player))
            {if(string.IsNullOrWhiteSpace(id)||links.ContainsKey(id))throw new ArgumentException("Invalid diplomacy faction.");links.Add(id,new DiplomaticLink{faction=id,relation=DiplomaticRelation.War,intention=DiplomaticIntention.Conquest});}
        }
        private static DiplomaticLink Copy(DiplomaticLink x)=>new(){faction=x.faction,relation=x.relation,intention=x.intention,remaining=x.remaining};
        private static DiplomaticProposal Copy(DiplomaticProposal p)=>p.Clone();
        public IReadOnlyList<string> Factions=>links.Keys.ToArray();
        public DiplomaticLink Relation(string faction)=>Copy(links[faction]);
        public IReadOnlyList<DiplomaticProposal> Proposals=>proposals.Select(Copy).ToArray();
        public bool CanAttack(string a,string b)
        {
            if(a==b)return false;
            if(a!=player&&b!=player)return true;
            if(!links.TryGetValue(a==player?b:a,out var link))return true;
            return link.relation==DiplomaticRelation.War||link.relation==DiplomaticRelation.Peace&&
                (link.intention==DiplomaticIntention.Dispute||link.intention==DiplomaticIntention.Conquest||link.intention==DiplomaticIntention.Mission);
        }
        public void SetRelation(string faction,DiplomaticRelation relation,DiplomaticIntention intention,int duration=0)
        {
            if(!links.ContainsKey(faction)||!Enum.IsDefined(typeof(DiplomaticRelation),relation)||!Enum.IsDefined(typeof(DiplomaticIntention),intention)||duration<0)throw new ArgumentException("Invalid diplomatic relation.");
            links[faction]=new DiplomaticLink{faction=faction,relation=relation,intention=intention,remaining=duration==0&&(relation==DiplomaticRelation.Pact||relation==DiplomaticRelation.Trade)?5:duration};
        }
        public bool Propose(string faction,DiplomaticRelation type,CampaignResources offer=default,CampaignResources demand=default,int duration=0)
        {
            if(!links.ContainsKey(faction)||type==DiplomaticRelation.War||!Enum.IsDefined(typeof(DiplomaticRelation),type)||duration<0)return false;
            if(proposals.Any(p=>p.sender==player&&p.receiver==faction&&p.type==type))return false;
            proposals.Add(Create(player,faction,type,offer,demand,duration));return true;
        }
        private DiplomaticProposal Create(string sender,string receiver,DiplomaticRelation type,CampaignResources offer,CampaignResources demand,int duration)=>new(){
            id="diplomacy-"+checked(++sequence),sender=sender,receiver=receiver,type=type,duration=duration==0&&type!=DiplomaticRelation.Peace?5:duration,
            offerGrain=offer.Grain,offerWood=offer.Wood,offerStone=offer.Stone,offerGold=offer.Gold,offerLabor=offer.Labor,
            demandGrain=demand.Grain,demandWood=demand.Wood,demandStone=demand.Stone,demandGold=demand.Gold,demandLabor=demand.Labor};
        public bool Respond(string id,bool accept)
        {var p=proposals.Find(p=>p.id==id&&p.receiver==player);if(p==null)return false;p.response=accept?1:2;return true;}
        private static long Value(CampaignResources r)=>(long)r.Grain+2L*r.Wood+2L*r.Stone+3L*r.Gold+r.Labor;
        private static (int threshold,int peace,int pact,int trade) Profile(string id)=>id switch
        {"castilla"=>(2,-1,1,0),"leon"=>(-1,3,2,0),"aragon"=>(0,1,1,3),"navarra"=>(-1,2,3,1),"granada"=>(-2,4,2,1),_=>(0,0,0,0)};
        private bool Evaluate(DiplomaticProposal p)
        {
            var profile=Profile(p.receiver);var preference=p.type==DiplomaticRelation.Peace?profile.peace:p.type==DiplomaticRelation.Pact?profile.pact:profile.trade;
            return Value(p.Offer)-Value(p.Demand)+preference+(p.type==DiplomaticRelation.Peace&&links[p.receiver].intention==DiplomaticIntention.Conquest?-2:0)>=profile.threshold;
        }
        private static CampaignResources Scale(CampaignResources r,bool increase)=>new(Scale(r.Grain,increase),Scale(r.Wood,increase),Scale(r.Stone,increase),Scale(r.Gold,increase),Scale(r.Labor,increase));
        private static int Scale(int n,bool increase)=>n==0?0:checked((int)(increase?((long)n*4+2)/3:Math.Max(1,(long)n*2/3)));
        public void CloseTurn(ref CampaignResources resources,ref CampaignRivalState rivals)
        {
            foreach(var link in links.Values)if(link.remaining>0&&--link.remaining==0){link.relation=DiplomaticRelation.Peace;link.intention=DiplomaticIntention.Neutral;}
            var pending=new List<DiplomaticProposal>();var reports=new List<string>();
            foreach(var p in proposals)
            {
                bool incoming=p.receiver==player;string faction=incoming?p.sender:p.receiver;
                if(incoming&&p.response==0){pending.Add(p);continue;}
                var sender=incoming?rivals.Resources(faction):resources;var receiver=incoming?resources:rivals.Resources(faction);
                bool affordable=rivals.Factions.Contains(faction)&&sender.TrySpend(p.Offer,out _)&&receiver.TrySpend(p.Demand,out _);
                if((incoming&&p.response==2)||!affordable||!incoming&&!Evaluate(p))
                {
                    if(!incoming&&affordable&&p.type==DiplomaticRelation.Trade&&Value(p.Offer)>0&&Value(p.Demand)>0)
                    {pending.Add(Create(faction,player,p.type,Scale(p.Demand,false),Scale(p.Offer,true),p.duration));reports.Add("Contrapropuesta de "+faction);}
                    else reports.Add("Propuesta rechazada · "+faction);
                    continue;
                }
                sender.TrySpend(p.Offer,out var leftSender);receiver.TrySpend(p.Demand,out var leftReceiver);
                var finalSender=leftSender.Add(p.Demand);var finalReceiver=leftReceiver.Add(p.Offer);
                resources=incoming?finalReceiver:finalSender;rivals=rivals.WithResources(faction,incoming?finalSender:finalReceiver);
                SetRelation(faction,p.type,DiplomaticIntention.Neutral,p.duration);reports.Add("Acuerdo aceptado · "+faction);
            }
            proposals.Clear();proposals.AddRange(pending);
            // Legacy initiative: the first rival with a treasury proposes according to its profile.
            string rival=rivals.Factions.FirstOrDefault();
            if(rival!=null&&!proposals.Any(p=>p.sender==rival||p.receiver==rival))
            {
                var link=links[rival];DiplomaticRelation? type=null;
                if(link.relation==DiplomaticRelation.War&&(rival=="leon"||rival=="navarra"||rival=="granada"))type=DiplomaticRelation.Peace;
                else if(link.relation==DiplomaticRelation.Peace&&(rival=="leon"||rival=="navarra"))type=DiplomaticRelation.Pact;
                else if(link.relation!=DiplomaticRelation.War&&rival=="aragon")type=DiplomaticRelation.Trade;
                if(type.HasValue)
                {
                    if(type!=DiplomaticRelation.Trade)proposals.Add(Create(rival,player,type.Value,default,default,0));
                    else if(rivals.Resources(rival).Wood>=2&&resources.Gold>=2)proposals.Add(Create(rival,player,type.Value,new(0,2,0,0,0),new(0,0,0,2,0),0));
                    else if(rivals.Resources(rival).Grain>=2&&resources.Wood>=1)proposals.Add(Create(rival,player,type.Value,new(2,0,0,0,0),new(0,1,0,0,0),0));
                }
            }
            LastReport=string.Join("\n",reports);
        }
        public DiplomacyData Capture()=>new(){player=player,sequence=sequence,links=links.Values.Select(Copy).ToArray(),proposals=proposals.Select(Copy).ToArray()};
        public static CampaignDiplomacy Restore(DiplomacyData data,string player,IEnumerable<string> factions)
        {
            var state=new CampaignDiplomacy(player,factions);
            if(data==null)return state; // Existing campaigns retain their hostile rules.
            if(data.schema!=1||data.player!=player||data.sequence<0||data.links==null||data.proposals==null||data.links.Length!=state.links.Count)throw new ArgumentException("Invalid diplomacy save.");
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var l in data.links)
            {if(l==null||!seen.Add(l.faction)||!state.links.ContainsKey(l.faction)||l.remaining<0||!Enum.IsDefined(typeof(DiplomaticRelation),l.relation)||!Enum.IsDefined(typeof(DiplomaticIntention),l.intention))throw new ArgumentException("Invalid diplomatic link.");state.links[l.faction]=Copy(l);}
            seen.Clear();
            foreach(var p in data.proposals)
            {
                if(p==null||string.IsNullOrWhiteSpace(p.id)||!seen.Add(p.id)||p.sender==p.receiver||!(p.sender==player&&state.links.ContainsKey(p.receiver)||p.receiver==player&&state.links.ContainsKey(p.sender))||
                    !Enum.IsDefined(typeof(DiplomaticRelation),p.type)||p.type==DiplomaticRelation.War||p.duration<0||p.response<0||p.response>2||p.sender==player&&p.response!=0)throw new ArgumentException("Invalid diplomatic proposal.");
                _=p.Offer;_=p.Demand;state.proposals.Add(Copy(p));
            }
            state.sequence=data.sequence;return state;
        }
    }
    public sealed partial class CampaignSaveData
    {
        public int diplomacyVersion; public DiplomacyData diplomacy;
        public void IncludeDiplomacy(CampaignDiplomacy state){if(state==null)throw new ArgumentNullException(nameof(state));diplomacyVersion=1;diplomacy=state.Capture();}
        public CampaignDiplomacy RestoreDiplomacy(string player,IEnumerable<string> factions)
        {
            if(diplomacyVersion==0)return new CampaignDiplomacy(player,factions);
            if(diplomacyVersion!=1||diplomacy==null)throw new ArgumentException("Missing or unsupported diplomacy state.");
            return CampaignDiplomacy.Restore(diplomacy,player,factions);
        }
    }
}
