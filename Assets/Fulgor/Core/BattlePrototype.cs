using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Combat
{
    /// <summary>Standalone laboratory fixture, never an encounter created from campaign armies.</summary>
    public sealed class BattlePrototype
    {
        private BattlePrototype(BattleRoster roster, BattleAttackResolver commands)
        { Roster = roster; Commands = commands; }
        public BattleRoster Roster { get; }
        public BattleAttackResolver Commands { get; }

        public static BattlePrototype Create(long seed)
        {
            var field = TacticalBattlefield.Generate(seed);
            var formations = new List<BattleFormationState>();
            var positions = new Dictionary<int, HexCoordinates>();
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < 4; i++)
                {
                    var profile = TroopCatalog.Get((TroopProfile)i);
                    int id = side * 4 + i, q = side == 0 ? 1 : 11, r = i * 2 + 1;
                    formations.Add(new BattleFormationState(new BattleFormationSlot(id, (BattleSide)side, profile.Kind, r, q), profile.Attributes, profile.DefaultQuantity));
                    positions.Add(id, new HexCoordinates(q, r));
                }
            var roster = new BattleRoster(formations);
            return new BattlePrototype(roster, new BattleAttackResolver(roster, positions, field.Cells, field.RandomState));
        }
    }
}
