using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using Fulgor.Core.Randomness;

namespace Fulgor.Core.Combat
{
    public enum TacticalTerrain { Clear, Woodland, Rough }
    public enum BattleAttackFailure { None, BattleFinished, NotActiveFormation, InvalidTarget, FriendlyTarget, OutOfRange }

    public readonly struct BattleAttackResult
    {
        public BattleAttackResult(int attackerId, int targetId, int roll, int terrainDefense,
            int defendingBonus, long damage, int casualties, uint randomState, BattleOutcome outcome, int moraleBefore = 0, int moraleAfter = 0)
        {
            AttackerId = attackerId; TargetId = targetId; Roll = roll; TerrainDefense = terrainDefense;
            DefendingBonus = defendingBonus; Damage = damage; Casualties = casualties;
            RandomState = randomState; Outcome = outcome;
            MoraleBefore = moraleBefore; MoraleAfter = moraleAfter;
        }
        public int AttackerId { get; }
        public int TargetId { get; }
        public int Roll { get; }
        public int TerrainDefense { get; }
        public int DefendingBonus { get; }
        public long Damage { get; }
        public int Casualties { get; }
        public uint RandomState { get; }
        public BattleOutcome Outcome { get; }
        public int MoraleBefore { get; }
        public int MoraleAfter { get; }
    }

    /// <summary>Shared tactical movement and seeded attack commands for future manual and automatic controllers.</summary>
    public sealed partial class BattleAttackResolver
    {
        private readonly BattleRoster roster;
        private readonly Dictionary<int, HexCoordinates> positions = new();
        private readonly Dictionary<HexCoordinates, TacticalTerrain> terrain = new();
        private readonly DeterministicRandom random;

        public BattleAttackResolver(BattleRoster roster, IReadOnlyDictionary<int, HexCoordinates> deployment,
            IReadOnlyDictionary<HexCoordinates, TacticalTerrain> battlefield, long seed)
        {
            this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
            if (deployment == null) throw new ArgumentNullException(nameof(deployment));
            if (battlefield == null) throw new ArgumentNullException(nameof(battlefield));
            foreach (var pair in battlefield)
            {
                if (!Enum.IsDefined(typeof(TacticalTerrain), pair.Value))
                    throw new ArgumentException("Unknown tactical terrain.", nameof(battlefield));
                if (pair.Key.Q < 0 || pair.Key.Q >= 13 || pair.Key.R < 0 || pair.Key.R >= 9)
                    throw new ArgumentException("Prototype battlefield cells must fit the 13 x 9 grid.", nameof(battlefield));
                terrain.Add(pair.Key, pair.Value);
            }
            var occupied = new HashSet<HexCoordinates>();
            foreach (var pair in deployment)
            {
                var formation = roster.GetFormation(pair.Key);
                if (!formation.IsInBattle || !terrain.ContainsKey(pair.Value) || !occupied.Add(pair.Value))
                    throw new ArgumentException("Deploy live formations on distinct battlefield cells.", nameof(deployment));
                positions.Add(pair.Key, pair.Value);
            }
            foreach (var id in roster.FormationIds)
                if (roster.GetFormation(id).IsInBattle && !positions.ContainsKey(id))
                    throw new ArgumentException("Missing formation position.", nameof(deployment));
            random = new DeterministicRandom(seed);
        }

        internal BattleRoster Roster => roster;
        public IReadOnlyList<HexCoordinates> BattlefieldCells
        {
            get
            {
                var cells = new List<HexCoordinates>(terrain.Keys);
                cells.Sort((a, b) => a.R != b.R ? a.R.CompareTo(b.R) : a.Q.CompareTo(b.Q));
                return cells.AsReadOnly();
            }
        }
        public bool TryGetTerrain(HexCoordinates cell, out TacticalTerrain tile) => terrain.TryGetValue(cell, out tile);
        internal int MovementCostAt(HexCoordinates cell) => MovementCostFor(terrain[cell]);
        public uint RandomState => random.State;

        public bool TryAttack(int attackerId, int targetId, out BattleAttackResult result, out BattleAttackFailure failure)
        {
            result = default;
            if (roster.Outcome != BattleOutcome.InProgress)
            { failure = BattleAttackFailure.BattleFinished; return false; }
            if (roster.ActiveFormationId != attackerId)
            { failure = BattleAttackFailure.NotActiveFormation; return false; }
            if (!positions.TryGetValue(targetId, out var targetPosition) ||
                !roster.GetFormation(targetId).IsInBattle)
            { failure = BattleAttackFailure.InvalidTarget; return false; }
            var attacker = roster.GetFormation(attackerId);
            var target = roster.GetFormation(targetId);
            if (attacker.Slot.Side == target.Slot.Side)
            { failure = BattleAttackFailure.FriendlyTarget; return false; }
            if (positions[attackerId].DistanceTo(targetPosition) > attacker.Attributes.Range)
            { failure = BattleAttackFailure.OutOfRange; return false; }

            // No random draw or mutation until the entire command is validated.
            var roll = random.NextInteger(attacker.Attributes.MinimumDamage, attacker.Attributes.MaximumDamage);
            var terrainBonus = DefenseFor(terrain[targetPosition]);
            var defendingBonus = roster.DefenseBonus(targetId);
            var impact = Math.Max(0L, (long)attacker.Attributes.Attack + roll - target.Attributes.Defense - terrainBonus - defendingBonus);
            var damage = checked(impact * attacker.Quantity);
            roster.TryApplyResolvedDamage(targetId, damage, out var casualties);
            if (roster.Outcome == BattleOutcome.InProgress) roster.CompleteActivation();
            result = new BattleAttackResult(attackerId, targetId, roll, terrainBonus, defendingBonus,
                damage, casualties, random.State, roster.Outcome, target.Morale, roster.GetFormation(targetId).Morale);
            failure = BattleAttackFailure.None;
            return true;
        }

        public static int DefenseFor(TacticalTerrain terrain) => terrain switch
        {
            TacticalTerrain.Clear => 0,
            TacticalTerrain.Woodland => 2,
            TacticalTerrain.Rough => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(terrain)),
        };
    }
}
