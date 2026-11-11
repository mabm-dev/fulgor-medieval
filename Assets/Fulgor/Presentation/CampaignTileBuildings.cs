using System;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Coloca edificios sobre casillas del tablero generado; cada entrada dice su casilla y hacia dónde mira la puerta.</summary>
    [RequireComponent(typeof(CampaignBoardPreview))]
    public sealed class CampaignTileBuildings : MonoBehaviour
    {
        // Los modelos del repertorio llevan la puerta a −Y en Blender; sin giro queda mirando al sur del mapa (6 en un reloj).
        public const float DoorMinuteAtZeroYaw = 30f;

        [Serializable]
        public struct Entry
        {
            public string label;
            public Vector2Int cell;
            public GameObject prefab;
            [Tooltip("Hacia dónde mira la puerta, como la aguja de un minutero: 0 es el norte del mapa, 15 el este, 30 el sur, 45 el oeste.")]
            [Range(0f, 60f)] public float doorMinute;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        private sealed class Slot
        {
            public GameObject instance;
            public HexCoordinates cell;
            public CampaignBuildingFootprint footprint;
            public Renderer[] renderers;
            public ShadowCastingMode[] shadows;
            public bool shown = true;
            public string project;
            public ResourceDepositKind deposit;
        }

        /// <summary>La base de un edificio queda estos metros por debajo del suelo que hay bajo el borde de su huella: el borde no flota.</summary>
        public const float SeatDepth = .004f;
        private static readonly int GroundMapId = Shader.PropertyToID("_FulgorHuellas"), GroundMapUv = Shader.PropertyToID("_FulgorHuellasST"),
            GroundMapOn = Shader.PropertyToID("_FulgorHuellasActivas"), BaseY = Shader.PropertyToID("_FulgorBaseY"),
            TrampledColor = Shader.PropertyToID("_FulgorPisadoColor"), Tufts = Shader.PropertyToID("_FulgorMatas"), TuftLight = Shader.PropertyToID("_FulgorMatasLuz");

        private readonly List<GameObject> placed = new();
        private readonly List<Slot> slots = new();
        private Transform generation;
        private Texture2D groundTexture;
        public IReadOnlyList<GameObject> Placed => placed;
        // un yacimiento no ocupa la casilla: encima se construye su mina
        public bool Occupies(HexCoordinates cell) => slots.Exists(slot => slot.project == null && slot.deposit == ResourceDepositKind.None && slot.cell.Equals(cell));
        /// <summary>Huellas publicadas para el suelo pisado del terreno; null sin edificios.</summary>
        public CampaignBuildingGroundMap GroundMap { get; private set; }
        /// <summary>Lo que tardó la última colocación (instanciar, asentar y publicar las huellas) y, de ello, el mapa de huellas.</summary>
        public double PlaceMilliseconds { get; private set; }
        public double GroundMapMilliseconds { get; private set; }

        /// <summary>Casillas en las que se colocan edificios o yacimientos: el relieve de colinas y montañas se allana en ellas antes de colocarlos.</summary>
        public IEnumerable<HexCoordinates> PlannedCells()
        {
            foreach (var entry in entries)
                if (entry.prefab != null) yield return new HexCoordinates(entry.cell.x, entry.cell.y);
            foreach (var deposit in MapDeposits()) yield return deposit.Key;
        }

        private IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> MapDeposits()
        {
            var board = GetComponent<CampaignBoardPreview>();
            return board != null && board.ShowsGold && board.CurrentMap != null ? ResourceDepositLayout.For(board.CurrentMap) : NoDeposits;
        }
        private static readonly IReadOnlyDictionary<HexCoordinates, ResourceDepositKind> NoDeposits = new Dictionary<HexCoordinates, ResourceDepositKind>();

        // Giro de un yacimiento, como minutero: fijo para cada casilla y distinto de una a otra, para que los diecisiete de un recurso no parezcan copias.
        private static float DepositMinute(HexCoordinates cell) => ((uint)(cell.Q * 73856093) ^ (uint)(cell.R * 19349663)) % 60u;

        public static float YawForDoorMinute(float minute) => (minute - DoorMinuteAtZeroYaw) * 6f;

        /// <summary>En partida un edificio solo se dibuja sobre terreno que el jugador ya ha visto; sobre casilla sin explorar quedaría en terreno negro.</summary>
        public static bool IsShownOn(CampaignVisibility visibility) => visibility != CampaignVisibility.Unknown;

        private void LateUpdate()
        {
            var root = transform.Find("Generated Board");
            if (root == null || !root.gameObject.activeInHierarchy) { Clear(); return; }
            if (root != generation) Place(root);
            SyncCityBuildings();
            ApplyFog();
        }

        /// <summary>Indica si hay un edificio colocado en la casilla.</summary>
        public bool HasBuildingAt(HexCoordinates cell)
        {
            foreach (var slot in slots)
                if (slot.instance != null && slot.cell.Equals(cell)) return true;
            return false;
        }

        /// <summary>Casilla en la que está colocado un edificio de esta capa.</summary>
        public bool TryGetCell(GameObject instance, out HexCoordinates cell)
        {
            foreach (var slot in slots)
                if (slot.instance == instance) { cell = slot.cell; return true; }
            cell = default;
            return false;
        }

        private void ApplyFog()
        {
            var campaign = GetComponent<CampaignTurnController>();
            foreach (var slot in slots)
            {
                if (slot.instance == null) continue;
                bool show = campaign == null || IsShownOn(campaign.VisibilityAt(slot.cell));
                if (show == slot.shown) continue;
                slot.shown = show;
                for (int i = 0; i < slot.renderers.Length; i++)
                {
                    if (slot.renderers[i] == null) continue;
                    slot.renderers[i].forceRenderingOff = !show;
                    slot.renderers[i].shadowCastingMode = show ? slot.shadows[i] : ShadowCastingMode.Off;
                }
            }
        }

        private void Place(Transform root)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Clear();
            generation = root;
            tiles.Clear();
            foreach (var tile in root.GetComponentsInChildren<HexTileView>()) tiles[tile.Cell.Coordinates] = tile;
            foreach (var entry in entries)
            {
                if (entry.prefab != null && tiles.TryGetValue(new HexCoordinates(entry.cell.x, entry.cell.y), out var tile))
                    PlaceBuilding(tile, entry.prefab, entry.label, entry.doorMinute, null);
            }
            PlaceDeposits();
            var mapWatch = System.Diagnostics.Stopwatch.StartNew();
            PublishGroundMap(tiles.Values);
            GroundMapMilliseconds = mapWatch.Elapsed.TotalMilliseconds;
            PlaceMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        // Los yacimientos vírgenes; bajo un edificio de prueba o una ciudad no se dibujan.
        private void PlaceDeposits()
        {
            var settlements = GetComponent<CampaignSettlementController>();
            foreach (var deposit in MapDeposits())
            {
                if (!tiles.TryGetValue(deposit.Key, out var tile) || HasBuildingAt(deposit.Key) || settlements != null && settlements.HasSettlementAt(deposit.Key)) continue;
                bool gold = deposit.Value == ResourceDepositKind.Gold;
                var prefab = Resources.Load<GameObject>("Edificios/PF_" + (gold ? "YacimientoOro" : "YacimientoPiedra"));
                if (prefab == null) continue;
                PlaceBuilding(tile, prefab, $"Yacimiento de {(gold ? "oro" : "piedra")} {deposit.Key.Q},{deposit.Key.R}", DepositMinute(deposit.Key), null, deposit.Value);
            }
        }

        private readonly Dictionary<HexCoordinates, HexTileView> tiles = new();
        private int cityRevision = -1;

        private void SyncCityBuildings()
        {
            var campaign = GetComponent<CampaignTurnController>();
            if (campaign == null || campaign.SettlementState == null || cityRevision == campaign.TileCityRevision) return;
            cityRevision = campaign.TileCityRevision;
            var wanted = new Dictionary<HexCoordinates, string>();
            foreach (var city in campaign.SettlementState.Settlements)
            {
                var state = campaign.TileCity(city.Id);
                if (state == null) continue;
                var data = state.Capture(city.Id);
                foreach (var built in data.completed) wanted[new HexCoordinates(built.q, built.r)] = built.project;
            }
            bool changed = false;
            var worked = new HashSet<HexCoordinates>(wanted.Keys);
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (slot.deposit != ResourceDepositKind.None)
                {
                    // la mina o la cantera terminada sustituye al yacimiento; si desaparece, vuelve la roca
                    if (slot.instance.activeSelf == worked.Contains(slot.cell)) { slot.instance.SetActive(!worked.Contains(slot.cell)); changed = true; }
                    continue;
                }
                if (slot.project == null || wanted.TryGetValue(slot.cell, out var project) && project == slot.project)
                {
                    wanted.Remove(slot.cell);
                    continue;
                }
                slot.instance.SetActive(false);
                placed.Remove(slot.instance);
                Destroy(slot.instance);
                slots.RemoveAt(i);
                changed = true;
            }
            foreach (var entry in wanted)
            {
                string asset = entry.Value switch { "house" => "ViviendaSencilla", "farm" => "HuertoCampo", "lumber" => "Lenera", "goldmine" => "MinaOro", "quarry" => "CanteraPiedra", _ => null };
                if (asset == null || !tiles.TryGetValue(entry.Key, out var tile)) continue;
                var prefab = Resources.Load<GameObject>("Edificios/PF_" + asset);
                if (prefab == null) continue;
                PlaceBuilding(tile, prefab, asset, 30f, entry.Value);
                changed = true;
            }
            if (changed) PublishGroundMap(tiles.Values);
        }

        private void PlaceBuilding(HexTileView tile, GameObject prefab, string label, float doorMinute, string project, ResourceDepositKind deposit = ResourceDepositKind.None)
        {
            var collider = tile.GetComponent<MeshCollider>();
            if (collider == null || collider.sharedMesh == null) return;
            var rotation = Quaternion.Euler(0f, YawForDoorMinute(doorMinute), 0f);
            var footprint = prefab.GetComponent<CampaignBuildingFootprint>();
            if (!TrySeat(collider, tile.transform.position, rotation, footprint, out var position)) return;
            var instance = Instantiate(prefab, transform);
            instance.name = string.IsNullOrEmpty(label) ? prefab.name : label;
            instance.transform.SetPositionAndRotation(position, rotation);
            placed.Add(instance);
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            slots.Add(new Slot
            {
                project = project, deposit = deposit, instance = instance, cell = tile.Cell.Coordinates, renderers = renderers,
                footprint = instance.GetComponent<CampaignBuildingFootprint>(), shadows = Array.ConvertAll(renderers, r => r.shadowCastingMode),
            });
            // la base del edificio toma la tierra pisada del terreno de su casilla (sombreador Fulgor/Terreno en modo edificio)
            TakeGround(renderers, tile.Cell.Terrain, position.y + (footprint != null ? footprint.Bottom : 0f));
        }

        /// <summary>
        /// Asiento del edificio: la base queda <see cref="SeatDepth"/> por debajo del suelo que hay bajo el borde de su huella (la mediana de dieciséis
        /// puntos, así una piedra suelta de la pieza no lo levanta). Sin huella, como antes: 8 mm sobre el suelo del centro.
        /// </summary>
        private static bool TrySeat(MeshCollider ground, Vector3 centre, Quaternion rotation, CampaignBuildingFootprint footprint, out Vector3 position)
        {
            position = centre;
            if (!GroundAt(ground, centre, out var middle)) return false;
            if (footprint == null) { position = new Vector3(centre.x, middle + .008f, centre.z); return true; }
            var heights = new List<float>();
            for (int k = 0; k < 16; k++)
            {
                var local = footprint.Outline(k * Mathf.PI / 8f);
                if (GroundAt(ground, centre + rotation * new Vector3(local.x, 0f, local.y), out var height)) heights.Add(height);
            }
            heights.Sort();
            float seat = heights.Count > 0 ? heights[heights.Count / 2] : middle;
            position = new Vector3(centre.x, seat - SeatDepth - footprint.Bottom, centre.z);
            return true;
        }

        private static bool GroundAt(MeshCollider collider, Vector3 world, out float height)
        {
            var bounds = collider.bounds;
            height = 0f;
            if (!collider.Raycast(new Ray(new Vector3(world.x, bounds.max.y + 1f, world.z), Vector3.down), out var hit, bounds.size.y + 2f)) return false;
            height = hit.point.y;
            return true;
        }

        private static void TakeGround(Renderer[] renderers, TerrainType terrain, float baseY)
        {
            var block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(BaseY)) continue;
                renderer.GetPropertyBlock(block);
                block.SetFloat(BaseY, baseY);
                block.SetColor(TrampledColor, CampaignVisualPalette.TrampledGround(terrain));
                block.SetFloat(Tufts, CampaignVisualPalette.GroundTufts(terrain));
                block.SetFloat(TuftLight, CampaignVisualPalette.GroundTuftLight(terrain));
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Cuánto se multiplica la distancia a la huella en el mapa del suelo pisado: 1 en un edificio de casilla normal (lado medio de unos 0,36) y
        /// hasta 2 en uno pequeño, con la franja la mitad de ancha; un pozo no pisa el suelo como una casa.
        /// </summary>
        public static float GroundBandScale(CampaignBuildingFootprint footprint) =>
            1f / Mathf.Clamp(Mathf.Sqrt(footprint.HalfSize.x * footprint.HalfSize.y) / .36f, .5f, 1f);

        // el terreno pinta tierra pisada alrededor de cada huella: un mapa de distancias de todo el tablero, en una textura global
        private void PublishGroundMap(IEnumerable<HexTileView> tiles)
        {
            ReleaseGroundMap();
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var tile in tiles)
            {
                var p = tile.transform.position;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            if (minX > maxX || !slots.Exists(s => s.footprint != null)) return;
            float margin = VisualScaleContract.HexOuterRadius;
            var map = new CampaignBuildingGroundMap(Rect.MinMaxRect(minX - margin, minZ - margin, maxX + margin, maxZ + margin));
            foreach (var slot in slots)
                if (slot.footprint != null && slot.instance.activeSelf)
                    map.Add(slot.instance.transform.worldToLocalMatrix, slot.footprint.Centre, slot.footprint.HalfSize, slot.footprint.CornerRadius,
                        GroundBandScale(slot.footprint));
            groundTexture = new Texture2D(map.Width, map.Height, TextureFormat.R8, true, true)
            {
                name = "Huellas de los edificios", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            groundTexture.SetPixelData(map.Values, 0);
            groundTexture.Apply(true, true);
            Shader.SetGlobalTexture(GroundMapId, groundTexture);
            Shader.SetGlobalVector(GroundMapUv, map.UvTransform);
            Shader.SetGlobalFloat(GroundMapOn, 1f);
            GroundMap = map;
        }

        private void ReleaseGroundMap()
        {
            Shader.SetGlobalFloat(GroundMapOn, 0f);
            GroundMap = null;
            if (groundTexture == null) return;
            if (Application.isPlaying) Destroy(groundTexture); else DestroyImmediate(groundTexture);
            groundTexture = null;
        }

        private void Clear()
        {
            foreach (var item in placed) if (item != null) Destroy(item);
            placed.Clear();
            slots.Clear();
            generation = null;
            tiles.Clear();
            cityRevision = -1;
            ReleaseGroundMap();
        }

        private void OnDestroy() => ReleaseGroundMap();
    }
}
