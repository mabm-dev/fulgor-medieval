using System;
using System.Collections.Generic;

namespace Fulgor.Core.Combat
{
    public sealed class BattleSideReport
    {
        internal BattleSideReport(BattleSide side, IReadOnlyList<BattleFormationState> formations)
        {
            Side = side;
            foreach (var formation in formations)
            {
                if (formation.Slot.Side != side) continue;
                InitialQuantity += formation.InitialQuantity;
                Survivors += formation.Quantity;
                Casualties += formation.Casualties;
                RemainingHealth += formation.TotalHealth;
                if (formation.IsDestroyed) DestroyedFormations++;
                else if (formation.IsWithdrawn) WithdrawnFormations++;
                else SurvivingFormations++;
            }
        }
        public BattleSide Side { get; }
        public long InitialQuantity { get; }
        public long Survivors { get; }
        public long Casualties { get; }
        public long RemainingHealth { get; }
        public int DestroyedFormations { get; }
        public int WithdrawnFormations { get; }
        public int SurvivingFormations { get; }
    }

    /// <summary>Immutable final snapshot, suitable for a later strategic reconciliation step.</summary>
    public sealed class BattleReport
    {
        private BattleReport(BattleRoster roster)
        {
            Outcome = roster.Outcome; Round = roster.Round;
            var formations = new List<BattleFormationState>();
            foreach (var id in roster.FormationIds) formations.Add(roster.GetFormation(id));
            Formations = formations.AsReadOnly();
            Attacker = new BattleSideReport(BattleSide.Attacker, Formations);
            Defender = new BattleSideReport(BattleSide.Defender, Formations);
        }
        public BattleOutcome Outcome { get; }
        public int Round { get; }
        public IReadOnlyList<BattleFormationState> Formations { get; }
        public BattleSideReport Attacker { get; }
        public BattleSideReport Defender { get; }
        public static BattleReport Create(BattleRoster roster)
        {
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (roster.Outcome == BattleOutcome.InProgress)
                throw new InvalidOperationException("A final report requires a finished battle.");
            return new BattleReport(roster);
        }
    }
}
