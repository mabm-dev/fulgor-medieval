using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Tablero técnico temporal para validar escala, cámara y geometría.</summary>
    public sealed class CampaignBoardPreview : MonoBehaviour
    {
        [SerializeField, Min(1)] private int radius = 3;
        [SerializeField, Min(0.1f)] private float outerRadius = 1f;
        [SerializeField, Range(0.7f, 1f)] private float tileFill = 0.97f;

        private Mesh hexMesh;
        private readonly List<Material> materials = new();

        private void Awake()
        {
            BuildBoard();
            ConfigureCamera();
            ConfigureLight();
        }

        private void OnDestroy()
        {
            if (hexMesh != null)
            {
                Destroy(hexMesh);
            }

            foreach (var material in materials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        private void BuildBoard()
        {
            hexMesh = CreateHexPrism(outerRadius * tileFill, 0.16f);
            materials.Add(CreateMaterial("Pradera clara", new Color(0.42f, 0.52f, 0.29f)));
            materials.Add(CreateMaterial("Pradera oscura", new Color(0.31f, 0.43f, 0.25f)));
            materials.Add(CreateMaterial("Tierra", new Color(0.48f, 0.38f, 0.24f)));

            foreach (var coordinates in new HexCoordinates(0, 0).CellsInRadius(radius))
            {
                var tile = new GameObject($"Hex {coordinates.Key}");
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = HexWorldLayout.ToWorld(coordinates, outerRadius);

                var meshFilter = tile.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = hexMesh;
                var meshRenderer = tile.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = materials[PositiveModulo(coordinates.Q - coordinates.R, materials.Count)];
            }
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader)
            {
                name = materialName,
                color = color,
            };
            material.SetFloat("_Smoothness", 0.12f);
            return material;
        }

        private static int PositiveModulo(int value, int divisor) => (value % divisor + divisor) % divisor;

        private static void ConfigureCamera()
        {
            var sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                return;
            }

            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 6.4f;
            sceneCamera.transform.position = new Vector3(0f, 10f, -8f);
            sceneCamera.transform.LookAt(Vector3.zero);
            sceneCamera.backgroundColor = new Color(0.025f, 0.04f, 0.05f);
        }

        private static void ConfigureLight()
        {
            var directionalLight = FindAnyObjectByType<Light>();
            if (directionalLight == null)
            {
                return;
            }

            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.91f, 0.76f);
            directionalLight.intensity = 1.6f;
            directionalLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.ambientLight = new Color(0.18f, 0.23f, 0.26f);
        }

        private static Mesh CreateHexPrism(float radiusValue, float height)
        {
            const int sides = 6;
            var vertices = new Vector3[sides * 2];
            for (var index = 0; index < sides; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index + 30f);
                var x = Mathf.Cos(angle) * radiusValue;
                var z = Mathf.Sin(angle) * radiusValue;
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

            var mesh = new Mesh { name = "Hexágono técnico" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
