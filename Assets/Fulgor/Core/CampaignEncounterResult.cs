using System;
using System.Collections.Generic;
using Fulgor.Core.Combat;

namespace Fulgor.Core.Campaign
{
    /// <summary>Both immutable army results are built before the caller commits either one.</summary>
    public sealed class CampaignEncounterResult
    {
        private CampaignEncounterResult(CampaignArmyState attacker, CampaignArmyState defender, bool alreadyApplied)
        { Attacker = attacker; Defender = defender; AlreadyApplied = alreadyApplied; }
        public CampaignArmyState Attacker { get; }
        public CampaignArmyState Defender { get; }
        public bool AlreadyApplied { get; }

        public static CampaignEncounterResult Reconcile(string encounterId, CampaignArmyState attacker,
            CampaignArmyState defender, BattleReport report, IReadOnlyDictionary<int, string> attackerBindings,
            IReadOnlyDictionary<int, string> defenderBindings)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) throw new ArgumentException("Missing encounter ID.", nameof(encounterId));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (defender == null) throw new ArgumentNullException(nameof(defender));
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (attackerBindings == null) throw new ArgumentNullException(nameof(attackerBindings));
            if (defenderBindings == null) throw new ArgumentNullException(nameof(defenderBindings));
            if (attacker.Id == defender.Id || attacker.FactionId == defender.FactionId)
                throw new ArgumentException("Encounter requires different armies from opposing factions.");
            bool attackerApplied = attacker.HasAppliedEncounter(encounterId);
            bool defenderApplied = defender.HasAppliedEncounter(encounterId);
            if (attackerApplied != defenderApplied)
                throw new ArgumentException("Inconsistent encounter application history.");
            if (attackerApplied) return new CampaignEncounterResult(attacker, defender, true);
            var nextAttacker = attacker.ApplyBattleReport(encounterId, BattleSide.Attacker, report, attackerBindings);
            var nextDefender = defender.ApplyBattleReport(encounterId, BattleSide.Defender, report, defenderBindings);
            return new CampaignEncounterResult(nextAttacker, nextDefender, false);
        }
    }
}
