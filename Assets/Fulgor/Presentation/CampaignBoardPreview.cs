using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Vista temporal del mapa procedural; no contiene reglas de juego.</summary>
    public sealed class CampaignBoardPreview : MonoBehaviour
    {
        [SerializeField] private int seed = 12345;
        [SerializeField, Range(0.7f, 1f)] private float tileFill = 0.97f;
        [SerializeField] private bool showGold = true;
        [SerializeField] private bool useVerticalTerrainBlockout = true;

        private const float OuterRadius = VisualScaleContract.HexOuterRadius;

        private readonly Dictionary<TerrainType, Material> terrainMaterials = new();
        private readonly Dictionary<TerrainType, Mesh> terrainMeshes = new();
        private readonly Dictionary<(TerrainType, int), Mesh> blockoutMeshes = new();
        private readonly Dictionary<(TerrainType, TerrainType, int, int), Mesh> transitionMeshes = new();
        private readonly Dictionary<(TerrainType, TerrainType, TerrainType, int), Mesh> junctionMeshes = new();
        private readonly Dictionary<WaterVisualKind, Material> waterEdgeMaterials = new();
        private Material goldMaterial;
        private Material heightTransitionMaterial;

        private void Awake()
        {
            var map = ProceduralMapGenerator.Generate(
                ProceduralMapGenerator.DefaultWidth,
                ProceduralMapGenerator.DefaultHeight,
                seed);
            BuildBoard(map);
            ConfigureCamera(map);
            ConfigureLight();
        }

        private void OnDestroy()
        {
            foreach (var mesh in terrainMeshes.Values)
            {
                if (mesh != null) Destroy(mesh);
            }

            foreach (var mesh in blockoutMeshes.Values)
            {
                if (mesh != null) Destroy(mesh);
            }

            foreach (var mesh in transitionMeshes.Values)
            {
                if (mesh != null) Destroy(mesh);
            }

            foreach (var mesh in junctionMeshes.Values)
            {
                if (mesh != null) Destroy(mesh);
            }

            foreach (var material in terrainMaterials.Values)
            {
                if (material != null) Destroy(material);
            }

            if (goldMaterial != null) Destroy(goldMaterial);
            foreach (var material in waterEdgeMaterials.Values)
                if (material != null) Destroy(material);
            if (heightTransitionMaterial != null) Destroy(heightTransitionMaterial);
        }

        private void BuildBoard(GeneratedMap map)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var memoryBefore = System.GC.GetTotalMemory(false);
            var transitionCount = 0;
            var junctionCount = 0;
            var goldCount = 0;

            CreateTerrainMaterials();
            CreateTerrainMeshes();
            goldMaterial = CreateMaterial("Veta de oro", new Color(0.93f, 0.68f, 0.16f), 0.35f);
            waterEdgeMaterials.Add(WaterVisualKind.Sea,
                CreateMaterial("Costa marina", new Color(0.72f, 0.58f, 0.32f), 0.08f));
            waterEdgeMaterials.Add(WaterVisualKind.Lake,
                CreateMaterial("Orilla de lago", new Color(0.60f, 0.50f, 0.31f), 0.10f));
            waterEdgeMaterials.Add(WaterVisualKind.River,
                CreateMaterial("Ribera", new Color(0.43f, 0.35f, 0.22f), 0.04f));
            heightTransitionMaterial = CreateMaterial("Transicion de altura", new Color(0.30f, 0.24f, 0.16f), 0.04f);
            var boardSelection = GetComponent<CampaignBoardSelection>();
            if (boardSelection == null) boardSelection = gameObject.AddComponent<CampaignBoardSelection>();
            var selectionPanel = GetComponent<CampaignSelectionPanel>();
            if (selectionPanel == null) selectionPanel = gameObject.AddComponent<CampaignSelectionPanel>();
            boardSelection.Initialize(selectionPanel);
            var cellsByCoordinates = new Dictionary<HexCoordinates, MapCell>(map.Cells.Count);
            foreach (var cell in map.Cells) cellsByCoordinates.Add(cell.Coordinates, cell);
            var waterKinds = WaterVisualClassifier.Classify(map);

            foreach (var cell in map.Cells)
            {
                var style = TerrainVisualStyle.For(cell.Terrain);
                var tile = new GameObject($"Hex {cell.Coordinates.Key} {cell.Terrain}");
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = HexWorldLayout.ToWorld(cell.Coordinates, OuterRadius);

                var tileMesh = useVerticalTerrainBlockout
                    ? blockoutMeshes[(cell.Terrain, TerrainBlockoutMesh.VariantFor(cell.Coordinates))]
                    : terrainMeshes[cell.Terrain];
                tile.AddComponent<MeshFilter>().sharedMesh = tileMesh;
                tile.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterials[cell.Terrain];
                tile.AddComponent<MeshCollider>().sharedMesh = tileMesh;
                tile.AddComponent<HexTileView>().Initialize(cell, boardSelection, style.Color);

                if (showGold && cell.HasGold)
                {
                    CreateGoldMarker(tile.transform, style.Height);
                    goldCount++;
                }
                if (useVerticalTerrainBlockout)
                {
                    transitionCount += CreateTransitions(tile.transform, cell, cellsByCoordinates, waterKinds);
                    junctionCount += CreateJunctions(tile.transform, cell, cellsByCoordinates, waterKinds);
                }
            }

            watch.Stop();
            var metrics = new BoardBuildMetrics(watch.Elapsed.TotalMilliseconds,
                System.GC.GetTotalMemory(false) - memoryBefore, map.Cells.Count,
                transitionCount, junctionCount, goldCount,
                terrainMeshes.Count + blockoutMeshes.Count + transitionMeshes.Count + junctionMeshes.Count +
                    (goldCount > 0 ? 1 : 0),
                terrainMaterials.Count + waterEdgeMaterials.Count + 2);
            var diagnostics = GetComponent<CampaignBoardDiagnostics>();
            if (diagnostics == null) diagnostics = gameObject.AddComponent<CampaignBoardDiagnostics>();
            diagnostics.Record(metrics);
            selectionPanel.SetDiagnostics(metrics);
        }

        private int CreateTransitions(Transform tile, MapCell cell,
            IReadOnlyDictionary<HexCoordinates, MapCell> cellsByCoordinates,
            IReadOnlyDictionary<HexCoordinates, WaterVisualKind> waterKinds)
        {
            var count = 0;
            for (var direction = 0; direction < TerrainTransitionMesh.OwnedDirectionCount; direction++)
            {
                var neighborCoordinates = TerrainTransitionMesh.Neighbor(cell.Coordinates, direction);
                if (!cellsByCoordinates.TryGetValue(neighborCoordinates, out var neighbor)) continue;
                var kind = TerrainTransitionMesh.Classify(cell.Terrain, neighbor.Terrain);
                if (kind == TerrainTransitionKind.None) continue;
                var variant = TerrainTransitionMesh.VariantFor(cell.Coordinates, direction);
                var key = (cell.Terrain, neighbor.Terrain, direction, variant);
                if (!transitionMeshes.TryGetValue(key, out var mesh))
                {
                    mesh = TerrainTransitionMesh.Create(cell.Terrain, neighbor.Terrain,
                        direction, OuterRadius * tileFill, OuterRadius, variant);
                    transitionMeshes.Add(key, mesh);
                }

                var transition = new GameObject($"Transition {direction} {kind} {variant}");
                transition.transform.SetParent(tile, false);
                transition.AddComponent<MeshFilter>().sharedMesh = mesh;
                transition.AddComponent<MeshRenderer>().sharedMaterial = kind == TerrainTransitionKind.Coast
                    ? WaterMaterial(cell, neighbor, waterKinds) : heightTransitionMaterial;
                count++;
            }
            return count;
        }

        private int CreateJunctions(Transform tile, MapCell cell,
            IReadOnlyDictionary<HexCoordinates, MapCell> cellsByCoordinates,
            IReadOnlyDictionary<HexCoordinates, WaterVisualKind> waterKinds)
        {
            var count = 0;
            for (var corner = 0; corner < TerrainJunctionMesh.OwnedCornerCount; corner++)
            {
                var firstCoordinates = TerrainJunctionMesh.FirstNeighbor(cell.Coordinates, corner);
                var secondCoordinates = TerrainJunctionMesh.SecondNeighbor(cell.Coordinates, corner);
                if (!cellsByCoordinates.TryGetValue(firstCoordinates, out var first) ||
                    !cellsByCoordinates.TryGetValue(secondCoordinates, out var second) ||
                    cell.Terrain == first.Terrain && first.Terrain == second.Terrain) continue;
                var key = (cell.Terrain, first.Terrain, second.Terrain, corner);
                if (!junctionMeshes.TryGetValue(key, out var mesh))
                {
                    mesh = TerrainJunctionMesh.Create(cell.Terrain, first.Terrain,
                        second.Terrain, corner, OuterRadius * tileFill);
                    junctionMeshes.Add(key, mesh);
                }

                var junction = new GameObject($"Junction {corner}");
                junction.transform.SetParent(tile, false);
                junction.AddComponent<MeshFilter>().sharedMesh = mesh;
                var waterCell = cell.Terrain == TerrainType.Water ? cell :
                    first.Terrain == TerrainType.Water ? first : second;
                junction.AddComponent<MeshRenderer>().sharedMaterial =
                    waterCell.Terrain == TerrainType.Water
                        ? waterEdgeMaterials[waterKinds[waterCell.Coordinates]]
                        : heightTransitionMaterial;
                count++;
            }
            return count;
        }

        private Material WaterMaterial(MapCell first, MapCell second,
            IReadOnlyDictionary<HexCoordinates, WaterVisualKind> waterKinds)
        {
            var water = first.Terrain == TerrainType.Water ? first : second;
            return waterEdgeMaterials[waterKinds[water.Coordinates]];
        }

        private void CreateTerrainMaterials()
        {
            foreach (TerrainType terrain in System.Enum.GetValues(typeof(TerrainType)))
            {
                var style = TerrainVisualStyle.For(terrain);
                terrainMaterials.Add(terrain, CreateMaterial(terrain.ToString(), style.Color, 0.12f));
            }
        }

        private void CreateTerrainMeshes()
        {
            foreach (TerrainType terrain in System.Enum.GetValues(typeof(TerrainType)))
            {
                var style = TerrainVisualStyle.For(terrain);
                if (useVerticalTerrainBlockout)
                {
                    for (int variant = 0; variant < TerrainBlockoutMesh.VariantCount; variant++)
                        blockoutMeshes.Add((terrain, variant), TerrainBlockoutMesh.Create(terrain, variant, OuterRadius * tileFill));
                }
                else terrainMeshes.Add(terrain, CreateHexPrism(OuterRadius * tileFill, style.Height));
            }
        }

        private void CreateGoldMarker(Transform parent, float terrainHeight)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Oro";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, terrainHeight + 0.045f, 0f);
            marker.transform.localScale = new Vector3(0.16f, 0.04f, 0.16f);
            marker.GetComponent<MeshRenderer>().sharedMaterial = goldMaterial;
            var collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        private static Material CreateMaterial(string materialName, Color color, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = materialName, color = color };
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private void ConfigureCamera(GeneratedMap map)
        {
            var sceneCamera = Camera.main;
            if (sceneCamera == null) return;

            var first = HexWorldLayout.ToWorld(new HexCoordinates(0, 0), OuterRadius);
            var last = HexWorldLayout.ToWorld(
                new HexCoordinates(map.Width - 1, map.Height - 1), OuterRadius);
            var center = (first + last) * 0.5f;
            var boardWidth = last.x - first.x + 2f * OuterRadius;
            var boardDepth = last.z - first.z + 2f * OuterRadius;

            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = Mathf.Max(boardDepth * 0.62f, boardWidth / sceneCamera.aspect * 0.58f);
            sceneCamera.transform.position = center + new Vector3(0f, 28f, -22f);
            sceneCamera.transform.LookAt(center);
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.025f, 0.04f, 0.05f);

            var controller = sceneCamera.GetComponent<CampaignCameraController>();
            if (controller == null) controller = sceneCamera.gameObject.AddComponent<CampaignCameraController>();
            controller.Initialize(
                first.x - OuterRadius,
                last.x + OuterRadius,
                first.z - OuterRadius,
                last.z + OuterRadius,
                center,
                sceneCamera.orthographicSize,
                sceneCamera.transform.position - center,
                GetComponent<CampaignBoardSelection>());
        }

        private static void ConfigureLight()
        {
            var directionalLight = FindAnyObjectByType<Light>();
            if (directionalLight == null) return;

            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.91f, 0.76f);
            directionalLight.intensity = 1.6f;
            directionalLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.ambientLight = new Color(0.18f, 0.23f, 0.26f);
        }

        private static Mesh CreateHexPrism(float radius, float height)
        {
            const int sides = 6;
            var vertices = new Vector3[sides * 2];
            for (var index = 0; index < sides; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[index] = new Vector3(x, height, z);
                vertices[index + sides] = new Vector3(x, 0f, z);
            }

            var triangles = new List<int>(60);
            for (var index = 1; index < sides - 1; index++)
            {
                triangles.Add(0); triangles.Add(index + 1); triangles.Add(index);
                triangles.Add(sides); triangles.Add(sides + index); triangles.Add(sides + index + 1);
            }

            for (var index = 0; index < sides; index++)
            {
                var next = (index + 1) % sides;
                triangles.Add(index); triangles.Add(next); triangles.Add(sides + index);
                triangles.Add(next); triangles.Add(sides + next); triangles.Add(sides + index);
            }

            var mesh = new Mesh { name = "Hexagono procedural" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
