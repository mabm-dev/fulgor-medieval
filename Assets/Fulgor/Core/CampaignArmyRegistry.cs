using System;
using System.Collections.Generic;
using Fulgor.Core.Map;

namespace Fulgor.Core.Campaign
{
    public readonly struct CampaignArmyPlacement
    {
        public CampaignArmyPlacement(CampaignArmyState army, HexCoordinates position, int movementPerTurn)
        { Army = army; Position = position; MovementPerTurn = movementPerTurn; }
        public CampaignArmyState Army { get; }
        public HexCoordinates Position { get; }
        public int MovementPerTurn { get; }
    }

    public sealed class CampaignArmyOnMap
    {
        internal CampaignArmyOnMap(CampaignArmyState army, ArmyMovementState movement)
        { Army = army; Movement = movement; }
        public CampaignArmyState Army { get; }
        public ArmyMovementState Movement { get; }
    }

    public sealed class CampaignActiveEncounter
    {
        internal CampaignActiveEncounter(string attackerId, string defenderId, CampaignEncounter battle, GarrisonEncounterData garrison = null)
        { AttackerId = attackerId; DefenderId = defenderId; Battle = battle; Garrison = garrison; }
        public string AttackerId { get; }
        public string DefenderId { get; }
        public CampaignEncounter Battle { get; }
        public GarrisonEncounterData Garrison { get; }
    }

    public enum CampaignArmyMoveFailure { None, UnknownArmy, ArmyInBattle, Occupied, Unreachable, InsufficientMovement, GuardedSettlement, GarrisonCapacity }

    /// <summary>Immutable strategic positions and locks around mutable, active tactical encounters.</summary>
    public sealed partial class CampaignArmyRegistry
    {
        private readonly MapRouteGraph graph;
        private readonly Dictionary<string, CampaignArmyOnMap> armies;
        private readonly Dictionary<string, CampaignActiveEncounter> encounters;
        private readonly HashSet<string> resolved;

        public CampaignArmyRegistry(MapRouteGraph graph, IEnumerable<CampaignArmyPlacement> placements)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (placements == null) throw new ArgumentNullException(nameof(placements));
            armies = new Dictionary<string, CampaignArmyOnMap>(StringComparer.Ordinal);
            encounters = new Dictionary<string, CampaignActiveEncounter>(StringComparer.Ordinal);
            resolved = new HashSet<string>(StringComparer.Ordinal);
            var occupied = new HashSet<HexCoordinates>();
            foreach (var placement in placements)
            {
                if (placement.Army == null || placement.Army.IsDissolved)
                    throw new ArgumentException("Place only armies with surviving formations.", nameof(placements));
                if (armies.ContainsKey(placement.Army.Id) || !occupied.Add(placement.Position))
                    throw new ArgumentException("Duplicate army ID or occupied position.", nameof(placements));
                armies.Add(placement.Army.Id, new CampaignArmyOnMap(placement.Army,
                    new ArmyMovementState(graph, placement.Position, placement.MovementPerTurn)));
            }
        }

        private CampaignArmyRegistry(CampaignArmyRegistry previous)
        {
            graph = previous.graph;
            armies = new Dictionary<string, CampaignArmyOnMap>(previous.armies, StringComparer.Ordinal);
            encounters = new Dictionary<string, CampaignActiveEncounter>(previous.encounters, StringComparer.Ordinal);
            resolved = new HashSet<string>(previous.resolved, StringComparer.Ordinal);
        }

        public IReadOnlyList<CampaignArmyOnMap> Armies
        {
            get
            {
                var ids = new List<string>(armies.Keys); ids.Sort(StringComparer.Ordinal);
                var result = new List<CampaignArmyOnMap>();
                foreach (var id in ids) result.Add(armies[id]);
                return result.AsReadOnly();
            }
        }
        public bool TryGetArmy(string id, out CampaignArmyOnMap army)
        { army = null; return id != null && armies.TryGetValue(id, out army); }
        public bool TryGetEncounter(string id, out CampaignActiveEncounter encounter)
        { encounter = null; return id != null && encounters.TryGetValue(id, out encounter); }
        public bool HasResolvedEncounter(string id) => id != null && resolved.Contains(id);
        public bool IsLocked(string armyId)
        {
            foreach (var encounter in encounters.Values)
                if (encounter.AttackerId == armyId || encounter.DefenderId == armyId) return true;
            return false;
        }
        public bool TryGetOccupant(HexCoordinates cell, out CampaignArmyOnMap army)
        {
            foreach (var value in armies.Values)
                if (value.Movement.Position == cell) { army = value; return true; }
            army = null; return false;
        }

        public bool TryMove(string armyId, HexCoordinates destination, out CampaignArmyRegistry next,
            out MapPath path, out CampaignArmyMoveFailure failure, ISet<HexCoordinates> extraBlocked = null)
        {
            next = this; path = null;
            if (!TryGetArmy(armyId, out var unit)) { failure = CampaignArmyMoveFailure.UnknownArmy; return false; }
            if (IsLocked(armyId)) { failure = CampaignArmyMoveFailure.ArmyInBattle; return false; }
            if (TryGetOccupant(destination, out var occupant) && occupant.Army.Id != armyId)
            { failure = CampaignArmyMoveFailure.Occupied; return false; }
            var blocked = new HashSet<HexCoordinates>();
            if(extraBlocked!=null)blocked.UnionWith(extraBlocked);
            foreach (var army in armies.Values) if (army.Army.Id != armyId) blocked.Add(army.Movement.Position);
            if (!unit.Movement.TryMove(destination, out var movement, out path, out var moveFailure, blocked))
            {
                failure = moveFailure == ArmyMoveFailure.InsufficientMovement ?
                    CampaignArmyMoveFailure.InsufficientMovement : CampaignArmyMoveFailure.Unreachable;
                return false;
            }
            if (!ReferenceEquals(movement, unit.Movement))
            {
                next = new CampaignArmyRegistry(this);
                next.armies[armyId] = new CampaignArmyOnMap(unit.Army, movement);
            }
            failure = CampaignArmyMoveFailure.None; return true;
        }

        public CampaignArmyRegistry BeginEncounter(string id, string attackerId, string defenderId, long seed)
        {
            if (string.IsNullOrWhiteSpace(id) || encounters.ContainsKey(id) || resolved.Contains(id))
                throw new ArgumentException("Encounter ID is missing or already used.", nameof(id));
            if (!TryGetArmy(attackerId, out var attacker) || !TryGetArmy(defenderId, out var defender))
                throw new ArgumentException("Both armies must exist on the map.");
            if (IsLocked(attackerId) || IsLocked(defenderId)) throw new InvalidOperationException("Army is already in battle.");
            if (attacker.Movement.Position.DistanceTo(defender.Movement.Position) != 1)
                throw new ArgumentException("Armies must be adjacent to start this prototype encounter.");
            var battle = new CampaignEncounter(id, attacker.Army, defender.Army, seed);
            var next = new CampaignArmyRegistry(this);
            next.encounters.Add(id, new CampaignActiveEncounter(attackerId, defenderId, battle));
            return next;
        }

        public CampaignArmyRegistry ResolveEncounter(string id)
        {
            if (HasResolvedEncounter(id)) return this;
            if (!TryGetEncounter(id, out var encounter)) throw new ArgumentException("Unknown encounter.", nameof(id));
            var attacker = armies[encounter.AttackerId]; var defender = armies[encounter.DefenderId];
            var result = encounter.Battle.Resolve(attacker.Army, defender.Army);
            var next = new CampaignArmyRegistry(this);
            next.UpdateSurvivors(result.Attacker, attacker.Movement);
            next.UpdateSurvivors(result.Defender, defender.Movement);
            next.encounters.Remove(id); next.resolved.Add(id);
            return next;
        }

        public CampaignArmyRegistry BeginTurn()
        {
            if (encounters.Count != 0) throw new InvalidOperationException("Resolve active encounters before advancing the campaign.");
            var next = new CampaignArmyRegistry(this);
            foreach (var pair in armies)
                next.armies[pair.Key] = new CampaignArmyOnMap(pair.Value.Army, pair.Value.Movement.BeginTurn());
            return next;
        }

        private void UpdateSurvivors(CampaignArmyState army, ArmyMovementState movement)
        {
            if (army.IsDissolved) armies.Remove(army.Id);
            else armies[army.Id] = new CampaignArmyOnMap(army, movement);
        }
    }
}
