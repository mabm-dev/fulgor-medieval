using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;

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
        private readonly List<GameObject> placed = new();
        private Transform generation;
        public IReadOnlyList<GameObject> Placed => placed;

        public static float YawForDoorMinute(float minute) => (minute - DoorMinuteAtZeroYaw) * 6f;

        private void LateUpdate()
        {
            var root = transform.Find("Generated Board");
            if (root == null || !root.gameObject.activeInHierarchy) { Clear(); return; }
            if (root == generation) return;
            Clear();
            generation = root;
            var tiles = new Dictionary<HexCoordinates, HexTileView>();
            foreach (var tile in root.GetComponentsInChildren<HexTileView>()) tiles[tile.Cell.Coordinates] = tile;
            foreach (var entry in entries)
            {
                if (entry.prefab == null || !tiles.TryGetValue(new HexCoordinates(entry.cell.x, entry.cell.y), out var tile)) continue;
                var collider = tile.GetComponent<MeshCollider>();
                if (collider == null || collider.sharedMesh == null) continue;
                var surface = new TerrainGroundSurface(collider.sharedMesh);
                if (!surface.TrySample(tile.transform, tile.transform.position, out var ground, out _, out _)) continue;
                var instance = Instantiate(entry.prefab, transform);
                instance.name = string.IsNullOrEmpty(entry.label) ? entry.prefab.name : entry.label;
                instance.transform.SetPositionAndRotation(ground + Vector3.up * .008f, Quaternion.Euler(0f, YawForDoorMinute(entry.doorMinute), 0f));
                placed.Add(instance);
            }
        }

        private void Clear()
        {
            foreach (var item in placed) if (item != null) Destroy(item);
            placed.Clear();
            generation = null;
        }
    }
}
