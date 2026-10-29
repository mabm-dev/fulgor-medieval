using System.Collections.Generic;
using System.Collections.ObjectModel;
using Fulgor.Core.Map;
using Fulgor.Core.Randomness;

namespace Fulgor.Core.Combat
{
    public sealed class TacticalBattlefield
    {
        private TacticalBattlefield(Dictionary<HexCoordinates, TacticalTerrain> cells, uint randomState)
        { Cells = new ReadOnlyDictionary<HexCoordinates, TacticalTerrain>(cells); RandomState = randomState; }
        public IReadOnlyDictionary<HexCoordinates, TacticalTerrain> Cells { get; }
        public uint RandomState { get; }
        public static TacticalBattlefield Generate(long seed)
        {
            var random = new DeterministicRandom(seed);
            var cells = new Dictionary<HexCoordinates, TacticalTerrain>();
            for (int r = 0; r < 9; r++)
                for (int q = 0; q < 13; q++)
                {
                    int roll = random.NextInteger(0, 99);
                    cells.Add(new HexCoordinates(q, r), roll < 60 ? TacticalTerrain.Clear :
                        roll < 85 ? TacticalTerrain.Woodland : TacticalTerrain.Rough);
                }
            return new TacticalBattlefield(cells, random.State);
        }
    }
}
