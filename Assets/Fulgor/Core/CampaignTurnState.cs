using System;

namespace Fulgor.Core.Campaign
{
    public readonly struct CampaignResources : IEquatable<CampaignResources>
    {
        public CampaignResources(int grain, int wood, int stone, int gold, int labor)
        {
            if (grain < 0) throw new ArgumentOutOfRangeException(nameof(grain));
            if (wood < 0) throw new ArgumentOutOfRangeException(nameof(wood));
            if (stone < 0) throw new ArgumentOutOfRangeException(nameof(stone));
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (labor < 0) throw new ArgumentOutOfRangeException(nameof(labor));

            Grain = grain;
            Wood = wood;
            Stone = stone;
            Gold = gold;
            Labor = labor;
        }

        public int Grain { get; }
        public int Wood { get; }
        public int Stone { get; }
        public int Gold { get; }
        public int Labor { get; }

        public CampaignResources Add(CampaignResources other) => new CampaignResources(
            checked(Grain + other.Grain), checked(Wood + other.Wood),
            checked(Stone + other.Stone), checked(Gold + other.Gold),
            checked(Labor + other.Labor));

        public bool Equals(CampaignResources other) => Grain == other.Grain &&
            Wood == other.Wood && Stone == other.Stone && Gold == other.Gold && Labor == other.Labor;

        public override bool Equals(object obj) => obj is CampaignResources other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Grain, Wood, Stone, Gold, Labor);
    }

    public sealed class CampaignTurnState
    {
        public CampaignTurnState(int turn, CampaignResources resources, CampaignResources incomePerTurn)
        {
            if (turn < 1) throw new ArgumentOutOfRangeException(nameof(turn));
            Turn = turn;
            Resources = resources;
            IncomePerTurn = incomePerTurn;
        }

        public int Turn { get; }
        public CampaignResources Resources { get; }
        public CampaignResources IncomePerTurn { get; }

        public CampaignTurnState CompleteTurn() => new CampaignTurnState(
            checked(Turn + 1), Resources.Add(IncomePerTurn), IncomePerTurn);
    }
}
