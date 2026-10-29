using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fulgor.Core.Campaign
{
    public sealed partial class CampaignSaveData
    {
        public CampaignArmyRegistryData armyRegistry;
        public string selectedArmyId;
        public bool missionWon;

        public static CampaignSaveData CaptureMultiple(string context, CampaignTurnState state,
            CampaignArmyRegistry registry, string selectedArmyId, string playerFaction,
            CityDevelopmentData[] cities = null, bool missionWon = false)
        {
            if (string.IsNullOrEmpty(context)) throw new ArgumentException("Missing scenario signature.", nameof(context));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            ValidateSelection(registry, selectedArmyId, playerFaction);
            return new CampaignSaveData { version = 4, multipleArmies=true, context = context, turn = state.Turn,
                grain = state.Resources.Grain, wood = state.Resources.Wood, stone = state.Resources.Stone,
                gold = state.Resources.Gold, labor = state.Resources.Labor,
                armyRegistry = registry.Capture(), selectedArmyId = selectedArmyId, missionWon = missionWon,
                cities = cities ?? Array.Empty<CityDevelopmentData>() };
        }

        public void RestoreMultiple(string expectedContext, CampaignArmyRegistry initial,
            string playerFaction, CampaignResources income,
            out CampaignTurnState state, out CampaignArmyRegistry registry, CampaignMilitaryState military = null)
        {
            state = null; registry = null;
            if ((version != 4 && version != 5 && version != 6 && version != 7 && version != 8 && version != 9 && version != 10 && version != 11) || version>=5&&!multipleArmies) throw new ArgumentException("This save does not contain a multiple-army campaign.");
            if (string.IsNullOrEmpty(expectedContext) || context != expectedContext)
                throw new ArgumentException("Save belongs to a different scenario or deployment.");
            if (hasMarchOrder) throw new ArgumentException("Multiple-army saves do not support pending marches.");
            var restoredState = new CampaignTurnState(turn, new CampaignResources(grain, wood, stone, gold, labor), income);
            var restoredRegistry = version>=6?CampaignArmyRegistry.RestoreRecruited(armyRegistry,initial,military):CampaignArmyRegistry.Restore(armyRegistry, initial);
            ValidateSelection(restoredRegistry, selectedArmyId, playerFaction);
            state = restoredState; registry = restoredRegistry;
        }

        private static void ValidateSelection(CampaignArmyRegistry registry, string selected, string faction)
        {
            if (string.IsNullOrWhiteSpace(faction)) throw new ArgumentException("Missing player faction.");
            if (!string.IsNullOrEmpty(selected))
            {
                if (!registry.TryGetArmy(selected, out var active) || active.Army.FactionId != faction)
                    throw new ArgumentException("Selected army must belong to the player.");
                return;
            }
            foreach (var unit in registry.Armies)
                if (unit.Army.FactionId == faction) throw new ArgumentException("Missing selected player army.");
        }

        public static string ArmyScenarioSignature(string mapSignature, CampaignArmyRegistry initial, string playerFaction)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            var parts = new List<string> { "multi-army-v4", mapSignature, playerFaction };
            foreach (var unit in initial.Armies)
            {
                parts.Add(unit.Army.Id); parts.Add(unit.Army.FactionId);
                parts.Add(FormattableString.Invariant($"{unit.Movement.Position.Q}:{unit.Movement.Position.R}:{unit.Movement.MovementPerTurn}"));
                var formations = new List<CampaignFormationState>(unit.Army.Formations);
                formations.Sort((a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
                foreach (var f in formations)
                {
                    var a = f.Attributes; parts.Add(f.Id);
                    parts.Add(FormattableString.Invariant($"{(int)f.Kind}:{f.Health}:{f.Morale}:{a.Attack}:{a.Defense}:{a.MinimumDamage}:{a.MaximumDamage}:{a.HealthPerMember}:{a.Movement}:{a.Initiative}:{a.Range}:{a.Morale}:{a.Discipline}"));
                }
            }
            ulong hash = 14695981039346656037UL;
            foreach (var part in parts)
            {
                if (part == null) throw new ArgumentException("Missing scenario identity.");
                // Length-prefix tokens so user-facing IDs cannot create ambiguous boundaries.
                foreach (var c in part.Length.ToString(CultureInfo.InvariantCulture) + ":" + part)
                    hash = unchecked((hash ^ c) * 1099511628211UL);
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }
    }
}
