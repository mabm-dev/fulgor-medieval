using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Review-only mesh LOD. Keeps the tile renderer, material and original collision surface.</summary>
    public sealed class TerrainLodArtReview : MonoBehaviour
    {
        [SerializeField] private Mesh[] fullMeshes;
        [SerializeField] private Mesh[] mediumMeshes;
        [SerializeField] private Mesh[] distantMeshes;
        [SerializeField, Range(.01f, .5f)] private float mediumThreshold = .15f;
        [SerializeField, Range(.01f, .5f)] private float distantThreshold = .08f;
        private sealed class Tile
        {
            public MeshFilter Filter;
            public readonly Mesh[] Meshes = new Mesh[3];
            public int Level;
        }
        private readonly List<Tile> tiles = new();
        private Camera viewCamera;
        private Matrix4x4 previousView, previousProjection;
        private int forcedLevel = -1;
        private bool dirty = true;
        public bool Ready { get; private set; }
        public int TileCount => tiles.Count;
        public int[] LevelCounts { get; } = new int[3];

        private IEnumerator Start()
        {
            var terrain = FindFirstObjectByType<TerrainMapArtReview>();
            var water = FindFirstObjectByType<WaterMapArtReview>();
            while ((terrain != null && !terrain.Ready) || (water != null && !water.Ready)) yield return null;
            if (fullMeshes == null || mediumMeshes == null || distantMeshes == null ||
                fullMeshes.Length != 5 || mediumMeshes.Length != 5 || distantMeshes.Length != 5)
            { Debug.LogError("Faltan mallas de detalle de terreno.", this); yield break; }
            for (int i = 0; i < 5; i++)
                if (fullMeshes[i] == null || mediumMeshes[i] == null || distantMeshes[i] == null)
                { Debug.LogError("Nivel de detalle de terreno incompleto.", this); yield break; }
            foreach (var tile in FindObjectsByType<HexTileView>(FindObjectsSortMode.None))
            {
                var filter = tile.GetComponent<MeshFilter>();
                int index = Array.IndexOf(fullMeshes, filter.sharedMesh);
                if (index < 0) continue; // Connected water owns its mesh and remains untouched.
                var entry = new Tile { Filter = filter };
                entry.Meshes[0] = fullMeshes[index]; entry.Meshes[1] = mediumMeshes[index]; entry.Meshes[2] = distantMeshes[index];
                tiles.Add(entry);
            }
            Ready = true;
            RefreshNow();
        }

        public void ForceLevel(int level)
        {
            if (level < -1 || level > 2) throw new ArgumentOutOfRangeException(nameof(level));
            forcedLevel = level; dirty = true; RefreshNow();
        }

        public void RefreshNow()
        {
            if (!Ready) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;
            Array.Clear(LevelCounts, 0, LevelCounts.Length);
            foreach (var tile in tiles)
            {
                if (tile.Filter == null) continue;
                // Nominal tile diameter 2, measured against the vertical camera span.
                float depth = Vector3.Dot(tile.Filter.transform.position - viewCamera.transform.position, viewCamera.transform.forward);
                float span = viewCamera.orthographic ? 2f * viewCamera.orthographicSize :
                    2f * Mathf.Max(.01f, depth) * Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad);
                float size = 2f / Mathf.Max(.01f, span);
                int level = forcedLevel;
                if (level < 0)
                {
                    // Ten percent hysteresis stops small zoom changes from toggling meshes.
                    float medium = mediumThreshold * (tile.Level == 0 ? .9f : 1.1f);
                    float distant = distantThreshold * (tile.Level == 2 ? 1.1f : .9f);
                    level = size >= medium ? 0 : size >= distant ? 1 : 2;
                }
                if (tile.Filter.sharedMesh != tile.Meshes[level]) tile.Filter.sharedMesh = tile.Meshes[level];
                tile.Level = level; LevelCounts[level]++;
            }
            previousView = viewCamera.worldToCameraMatrix; previousProjection = viewCamera.projectionMatrix; dirty = false;
        }

        private void LateUpdate()
        {
            if (viewCamera == null || dirty || previousView != viewCamera.worldToCameraMatrix || previousProjection != viewCamera.projectionMatrix)
                RefreshNow();
        }

        private void OnDisable()
        {
            foreach (var tile in tiles)
                if (tile.Filter != null) { tile.Filter.sharedMesh = tile.Meshes[0]; tile.Level = 0; }
            dirty = true;
        }
    }
}
