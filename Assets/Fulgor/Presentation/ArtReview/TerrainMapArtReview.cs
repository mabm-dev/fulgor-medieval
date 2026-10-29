using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Applies the terrain art only in the dedicated review scene.</summary>
    public sealed class TerrainMapArtReview : MonoBehaviour
    {
        [SerializeField] private Mesh[] terrainMeshes;
        [SerializeField] private Material[] terrainMaterials;
        [SerializeField] private Material shoreMaterial;
        private Mesh shoreline;
        public bool Ready { get; private set; }
        public int TileCount { get; private set; }
        public int ShoreEdges { get; private set; }
        public long TerrainTriangles { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (terrainMeshes == null || terrainMeshes.Length != 6 || terrainMaterials == null || terrainMaterials.Length != 5 || shoreMaterial == null)
            { Debug.LogError("Faltan los terrenos de la revision.", this); yield break; }
            var fortress = FindFirstObjectByType<FortressMapArtReview>();
            if (fortress != null) while (!fortress.PlacementReady) yield return null;
            var tiles = FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            var byCoordinates = tiles.ToDictionary(t => t.Cell.Coordinates);
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            var outward = new Dictionary<Vector2Int, Vector3>();
            foreach (var tile in tiles)
            {
                int index = (int)tile.Cell.Terrain;
                if (terrainMeshes[index] == null || terrainMaterials[index] == null)
                { Debug.LogError("Terreno incompleto: " + tile.Cell.Terrain, this); yield break; }
                foreach (Transform child in tile.transform)
                    if (child.name.StartsWith("Transition ") || child.name.StartsWith("Junction ")) child.gameObject.SetActive(false);
                var mesh = fortress != null && tile == fortress.ReviewTile ? terrainMeshes[5] : terrainMeshes[index];
                tile.transform.localRotation = Quaternion.Euler(0, 60 * ((tile.Cell.Coordinates.Q * 17 + tile.Cell.Coordinates.R * 31) % 6), 0);
                tile.GetComponent<MeshFilter>().sharedMesh = mesh;
                tile.GetComponent<MeshRenderer>().sharedMaterial = terrainMaterials[index];
                tile.GetComponent<MeshCollider>().sharedMesh = mesh;
                tile.Initialize(tile.Cell, tile.Owner, Color.white);
                TerrainTriangles += Enumerable.Range(0, mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i) / 3);
                if (tile.Cell.Terrain == TerrainType.Water) continue;
                foreach (var coordinate in tile.Cell.Coordinates.Neighbors())
                {
                    if (!byCoordinates.TryGetValue(coordinate, out var neighbor) || neighbor.Cell.Terrain != TerrainType.Water) continue;
                    var normal = (neighbor.transform.position - tile.transform.position).normalized;
                    var tangent = new Vector3(normal.z, 0, -normal.x);
                    var center = tile.transform.position + normal * (Mathf.Sqrt(3f) * .5f) + Vector3.up * .121f;
                    var a = center - tangent * .5f; var b = center + tangent * .5f;
                    var across = normal * .075f - Vector3.up * .080f;
                    var keyA = new Vector2Int(Mathf.RoundToInt(a.x * 10000), Mathf.RoundToInt(a.z * 10000));
                    var keyB = new Vector2Int(Mathf.RoundToInt(b.x * 10000), Mathf.RoundToInt(b.z * 10000));
                    outward.TryGetValue(keyA, out var sumA); outward[keyA] = sumA + normal;
                    outward.TryGetValue(keyB, out var sumB); outward[keyB] = sumB + normal;
                    int at = vertices.Count;
                    vertices.AddRange(new[] { a, b, b + across, a + across });
                    triangles.AddRange(new[] { at, at + 2, at + 1, at, at + 3, at + 2 });
                    ShoreEdges++;
                }
            }
            for (int at = 0; at < vertices.Count; at += 4)
            {
                var edgeNormal = vertices[at + 2] - vertices[at + 1]; edgeNormal.y = 0; edgeNormal.Normalize();
                for (int end = 0; end < 2; end++) {
                    var top = vertices[at + end];
                    var key = new Vector2Int(Mathf.RoundToInt(top.x * 10000), Mathf.RoundToInt(top.z * 10000));
                    var direction = outward[key].normalized;
                    var offset = direction * (.075f / Mathf.Max(.5f, Vector3.Dot(direction, edgeNormal)));
                    vertices[at + (end == 0 ? 3 : 2)] = top + offset - Vector3.up * .080f;
                }
            }
            Physics.SyncTransforms();
            foreach (var tile in tiles)
            {
                var marker = tile.transform.Find("Oro");
                if (marker != null && tile.GetComponent<MeshCollider>().Raycast(new Ray(tile.transform.position + Vector3.up * 3f, Vector3.down), out var hit, 6f))
                {
                    var position = marker.position; position.y = hit.point.y + .045f; marker.position = position;
                }
            }
            shoreline = new Mesh { name = "SM_Orillas_Revision" };
            shoreline.SetVertices(vertices); shoreline.SetTriangles(triangles, 0); shoreline.RecalculateNormals(); shoreline.RecalculateBounds();
            var shore = new GameObject("Orillas de revision"); shore.transform.SetParent(transform, false);
            shore.AddComponent<MeshFilter>().sharedMesh = shoreline;
            shore.AddComponent<MeshRenderer>().sharedMaterial = shoreMaterial;
            if (fortress != null)
            {
                while (!fortress.PlacementReady) yield return null;
                var position = fortress.PlacedFortress.position; position.y = fortress.ReviewTile.transform.position.y + .128f;
                fortress.PlacedFortress.position = position;
            }
            TileCount = tiles.Length; Ready = true;
        }

        private void OnDestroy()
        {
            if (shoreline != null) Destroy(shoreline);
        }
    }
}

