using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using Fulgor.Core.Campaign;
using Fulgor.Core.Combat;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Configuración reutilizable de un escenario de campaña.</summary>
    [CreateAssetMenu(fileName = "CampaignScenario", menuName = "Fulgor/Campaign Scenario")]
    public sealed class CampaignScenarioDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int width = 24;
        [SerializeField, Min(1)] private int height = 16;
        [SerializeField] private int seed = 12345;
        [SerializeField, Range(0.7f, 1f)] private float tileFill = 0.97f;
        [SerializeField] private bool showGold = true;
        [SerializeField] private bool useVerticalTerrainBlockout = true;
        [Tooltip("Tramos explícitos entre vecinos axiales (Q, R). No se crean rutas por proximidad.")]
        [SerializeField] private ScenarioRoute[] routes = Array.Empty<ScenarioRoute>();

        [SerializeField] private bool useExplicitDestination;
        [Tooltip("Objetivo de la misión: X = Q, Y = R. Debe ser accesible desde la hueste inicial del jugador, o la primera casilla terrestre en modo simple.")]
        [SerializeField] private Vector2Int destination;

        public ReachDestinationMission CreateMission(GeneratedMap map, MapRouteGraph graph, CampaignArmyRegistry registry = null)
        {
            if(objectiveKind==CampaignObjectiveKind.Conquest)return null;
            HexCoordinates? start = null;
            if (registry != null)
                foreach (var unit in registry.Armies)
                    if (unit.Army.FactionId == playerFactionId) { start = unit.Movement.Position; break; }
            return ReachDestinationMission.Create(map, graph, useExplicitDestination ?
                new HexCoordinates(destination.x, destination.y) : (HexCoordinates?)null, start);
        }

        [Tooltip("Activa el despliegue y control de las huestes del escenario.")]
        [SerializeField] private bool useArmyDeployments;
        [SerializeField] private string playerFactionId = "castilla";
        public string PlayerFactionId => playerFactionId;
        [SerializeField] private bool enableStrategicRivals;
        public bool EnableStrategicRivals=>enableStrategicRivals;
        [SerializeField] private bool enableFogOfWar;
        public bool EnableFogOfWar=>enableFogOfWar;
        [SerializeField] private bool enableMilitaryLogistics;
        public bool EnableMilitaryLogistics=>enableMilitaryLogistics;
        [SerializeField] private CampaignObjectiveKind objectiveKind;
        public CampaignObjectiveKind ObjectiveKind=>objectiveKind;
        [SerializeField] private ScenarioArmy[] armies = Array.Empty<ScenarioArmy>();

        public CampaignArmyRegistry CreateArmyRegistry(MapRouteGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if(!Enum.IsDefined(typeof(CampaignObjectiveKind),objectiveKind))throw new ArgumentException("Unknown campaign objective.");
            if (!useArmyDeployments) {if(enableStrategicRivals||enableMilitaryLogistics||enableFogOfWar||objectiveKind==CampaignObjectiveKind.Conquest)throw new ArgumentException("Strategic rivals require army deployments.");return null;}
            if (armies == null || armies.Length == 0)
                throw new ArgumentException("El despliegue activo necesita al menos una hueste.");
            var placements = new List<CampaignArmyPlacement>();
            var globalFormationIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < armies.Length; i++)
            {
                if (armies[i] == null) throw new ArgumentException($"La hueste {i + 1} está vacía.");
                placements.Add(armies[i].CreatePlacement(globalFormationIds));
            }
            var registry = new CampaignArmyRegistry(graph, placements);
            bool foundPlayer = false;
            foreach (var unit in registry.Armies) if (unit.Army.FactionId == playerFactionId) foundPlayer = true;
            if (!foundPlayer) throw new ArgumentException("El despliegue necesita una hueste de la facción del jugador.");
            return registry;
        }

        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public int Seed => seed;
        public float TileFill => float.IsNaN(tileFill) || float.IsInfinity(tileFill) ? 0.97f : Mathf.Clamp(tileFill, 0.7f, 1f);
        public bool ShowGold => showGold;
        public bool UseVerticalTerrainBlockout => useVerticalTerrainBlockout;

        [ContextMenu("Validate scenario (routes, mission and armies)")]
        private void ValidateRoutes()
        {
            try
            {
                var map = ProceduralMapGenerator.Generate(Width, Height, Seed);
                var graph = CreateRouteGraph(map);
                var registry = CreateArmyRegistry(graph);
                CreateMission(map, graph, registry);
                Debug.Log($"Escenario {name}: {map.Cells.Count} casillas, {graph.RouteCount} tramos y {registry?.Armies.Count ?? 0} huestes configuradas válidos.", this);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Escenario {name}: {exception.Message}", this);
            }
        }

        public MapRouteGraph CreateRouteGraph(GeneratedMap map)
        {
            var segments = new List<MapRouteSegment>();
            if (routes != null)
            {
                for (var index = 0; index < routes.Length; index++)
                {
                    var route = routes[index];
                    if (route == null)
                        throw new ArgumentException($"El tramo {index + 1} del escenario está vacío.");
                    segments.Add(route.ToSegment());
                }
            }

            return new MapRouteGraph(map, segments);
        }

        [Serializable]
        private sealed class ScenarioArmy
        {
            [SerializeField] private string id;
            [SerializeField] private string factionId;
            [Tooltip("Posición inicial axial: X = Q, Y = R.")]
            [SerializeField] private Vector2Int position;
            [SerializeField, Min(1)] private int movementPerTurn = 6;
            [SerializeField] private ScenarioFormation[] formations = Array.Empty<ScenarioFormation>();

            public CampaignArmyPlacement CreatePlacement(HashSet<string> globalIds)
            {
                if (formations == null || formations.Length < 1 || formations.Length > 6)
                    throw new ArgumentException($"La hueste {id} necesita de una a seis formaciones.");
                var roster = new List<CampaignFormationState>();
                foreach (var formation in formations)
                {
                    if (formation == null) throw new ArgumentException($"Formación vacía en la hueste {id}.");
                    var state = formation.Create();
                    if (!globalIds.Add(state.Id)) throw new ArgumentException($"ID de formación repetido: {state.Id}.");
                    roster.Add(state);
                }
                return new CampaignArmyPlacement(new CampaignArmyState(id, factionId, roster),
                    new HexCoordinates(position.x, position.y), movementPerTurn);
            }
        }

        [Serializable]
        private sealed class ScenarioFormation
        {
            [SerializeField] private string id;
            [SerializeField] private TroopProfile profile;
            [Tooltip("Cero utiliza la cantidad del catálogo; un valor positivo la sustituye.")]
            [SerializeField, Min(0)] private int quantity;

            public CampaignFormationState Create()
            {
                if (quantity < 0) throw new ArgumentException($"Cantidad negativa en la formación {id}.");
                var definition = TroopCatalog.Get(profile);
                var count = quantity == 0 ? definition.DefaultQuantity : quantity;
                return new CampaignFormationState(id, definition.Attributes,
                    checked((long)count * definition.Attributes.HealthPerMember), definition.Attributes.Morale, definition.Kind);
            }
        }

        [Serializable]
        private sealed class ScenarioRoute
        {
            [Tooltip("Coordenada axial de origen: X = Q, Y = R.")]
            [SerializeField] private Vector2Int from;
            [Tooltip("Coordenada axial de destino: X = Q, Y = R.")]
            [SerializeField] private Vector2Int to;
            [SerializeField] private MapRouteKind kind;

            public MapRouteSegment ToSegment() => new(
                new HexCoordinates(from.x, from.y), new HexCoordinates(to.x, to.y), kind);
        }
    }
}
