using System;
using System.Collections.Generic;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    /// <summary>A battle deployed from persistent armies, with explicit validated reconciliation.</summary>
    public sealed class CampaignEncounter
    {
        private readonly CampaignArmyState initialAttacker, initialDefender;
        private readonly Dictionary<int, string> attackerBindings = new(), defenderBindings = new();
        public CampaignEncounter(string id, CampaignArmyState attacker, CampaignArmyState defender, long seed)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Missing encounter ID.", nameof(id));
            initialAttacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            initialDefender = defender ?? throw new ArgumentNullException(nameof(defender));
            if (attacker.Id == defender.Id || attacker.FactionId == defender.FactionId)
                throw new ArgumentException("Encounter requires opposing armies.");
            if (attacker.IsDissolved || defender.IsDissolved)
                throw new ArgumentException("Dissolved armies cannot start a battle.");
            if (attacker.HasAppliedEncounter(id) || defender.HasAppliedEncounter(id))
                throw new ArgumentException("Encounter ID has already been used.", nameof(id));
            Id = id; Seed = seed;
            var formations = new List<BattleFormationState>();
            var positions = new Dictionary<int, HexCoordinates>();
            Deploy(attacker, BattleSide.Attacker, formations, positions, attackerBindings);
            Deploy(defender, BattleSide.Defender, formations, positions, defenderBindings);
            var field = TacticalBattlefield.Generate(seed);
            Roster = new BattleRoster(formations);
            Commands = new BattleAttackResolver(Roster, positions, field.Cells, field.RandomState);
        }
        public string Id { get; }
        public long Seed { get; }
        public BattleRoster Roster { get; }
        public BattleAttackResolver Commands { get; }
        public BattleReport CreateReport() => BattleReport.Create(Roster);

        public CampaignEncounterResult Resolve(CampaignArmyState currentAttacker, CampaignArmyState currentDefender)
        {
            if (currentAttacker == null || currentDefender == null) throw new ArgumentNullException("currentArmy");
            if (currentAttacker.Id != initialAttacker.Id || currentDefender.Id != initialDefender.Id)
                throw new ArgumentException("Different armies supplied for this encounter.");
            var report = CreateReport();
            // Already-applied encounters are handled by the joint reconciler.
            if (!(currentAttacker.HasAppliedEncounter(Id) && currentDefender.HasAppliedEncounter(Id)) &&
                (!initialAttacker.MatchesSnapshot(currentAttacker) || !initialDefender.MatchesSnapshot(currentDefender)))
                throw new ArgumentException("Armies changed while the battle was in progress.");
            return CampaignEncounterResult.Reconcile(Id, currentAttacker, currentDefender, report, attackerBindings, defenderBindings);
        }

        private static void Deploy(CampaignArmyState army, BattleSide side, List<BattleFormationState> formations,
            Dictionary<int, HexCoordinates> positions, Dictionary<int, string> bindings)
        {
            var source = new List<CampaignFormationState>(army.Formations);
            source.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            for (int i = 0; i < source.Count; i++)
            {
                int id = formations.Count, q = side == BattleSide.Attacker ? 1 : 11;
                int r = source.Count == 1 ? 4 : 1 + i * 6 / (source.Count - 1);
                formations.Add(source[i].Deploy(new BattleFormationSlot(id, side, source[i].Kind, r, q)));
                positions.Add(id, new HexCoordinates(q, r)); bindings.Add(id, source[i].Id);
            }
        }
    }
}
