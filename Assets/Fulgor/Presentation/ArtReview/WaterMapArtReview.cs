using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Replaces only the water presentation after the accepted terrain review is ready.</summary>
    public sealed class WaterMapArtReview : MonoBehaviour
    {
        [SerializeField] private Material waterMaterial;
        private readonly List<Mesh> generated = new();
        public bool Ready { get; private set; }
        public ConnectedWaterLayout Layout { get; private set; }
        public int WaterTiles { get; private set; }
        public int RiverTiles { get; private set; }
        public int LakeTiles { get; private set; }
        public int SeaTiles { get; private set; }
        public long RemovedTriangles { get; private set; }
        public long WaterTriangles { get; private set; }

        private IEnumerator Start()
        {
            var terrain = FindFirstObjectByType<TerrainMapArtReview>();
            if (terrain == null || waterMaterial == null) { Debug.LogError("Faltan terreno o material de agua conectada.", this); yield break; }
            while (!terrain.Ready) yield return null;
            var tiles = FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var centers = tiles.ToDictionary(t => t.Cell.Coordinates, t => t.transform.position);
            var water = new HashSet<HexCoordinates>(tiles.Where(t => t.Cell.Terrain == TerrainType.Water).Select(t => t.Cell.Coordinates));
            Layout = new ConnectedWaterLayout(centers, water);
            foreach (var tile in tiles.Where(t => water.Contains(t.Cell.Coordinates)))
            {
                var previous = tile.GetComponent<MeshFilter>().sharedMesh;
                RemovedTriangles += Enumerable.Range(0, previous.subMeshCount).Sum(i => (long)previous.GetIndexCount(i) / 3);
                tile.transform.localRotation = Quaternion.identity;
                var mesh = Layout.BuildMesh(tile.transform.position, "SM_AguaConectada_" + tile.Cell.Coordinates.Key);
                generated.Add(mesh); WaterTriangles += mesh.GetIndexCount(0) / 3;
                tile.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = tile.GetComponent<MeshRenderer>(); renderer.sharedMaterial = waterMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                tile.GetComponent<MeshCollider>().sharedMesh = mesh; tile.Initialize(tile.Cell, tile.Owner, Color.white);
                switch (Layout.Kinds[tile.Cell.Coordinates])
                {
                    case WaterVisualKind.River: RiverTiles++; break;
                    case WaterVisualKind.Lake: LakeTiles++; break;
                    default: SeaTiles++; break;
                }
                WaterTiles++;
            }
            var oldShore = terrain.transform.Find("Orillas de revision");
            if (oldShore != null) oldShore.gameObject.SetActive(false);
            Physics.SyncTransforms();
            var castle = FindFirstObjectByType<FortressMapArtReview>();
            var focus = castle != null && castle.PlacedFortress != null ? castle.PlacedFortress.position : Vector3.zero;
            var target = tiles.Where(t => t.Cell.Terrain == TerrainType.Water).OrderBy(t => (t.transform.position - focus).sqrMagnitude).FirstOrDefault();
            var controller = Camera.main != null ? Camera.main.GetComponent<CampaignCameraController>() : null;
            if (target != null && controller != null) controller.FocusOnWorld(target.transform.position);
            Ready = true;
        }

        private void OnDestroy()
        {
            foreach (var mesh in generated) if (mesh != null) Destroy(mesh);
        }
    }
}

