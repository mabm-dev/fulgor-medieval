using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Six inspectable examples, separate from the campaign's generated water mask.</summary>
    public sealed class WaterConnectionCasesReview : MonoBehaviour
    {
        [SerializeField] private Material waterMaterial;
        private readonly List<Mesh> generated = new();
        public static readonly string[] CaseNames = { "Recto", "Curva", "Ramales", "Desembocadura", "Costa", "Lago" };
        public readonly List<Vector3> CaseCenters = new();
        public bool Ready { get; private set; }

        private void Start()
        {
            if (!Ready && waterMaterial != null) Build(waterMaterial, Vector3.zero, true);
        }

        public void Build(Material material, Vector3 offset, bool configureCamera)
        {
            if (Ready) return;
            var land = FlatLand(); generated.Add(land);
            for (int test = 0; test < CaseNames.Length; test++)
            {
                var origin = offset + new Vector3((test % 3) * 14, 0, (test / 3) * 12);
                var centers = new Dictionary<HexCoordinates, Vector3>(); var water = new HashSet<HexCoordinates>();
                for (int r = 0; r < 5; r++) for (int q = 0; q < 5; q++) { var c = new HexCoordinates(q, r); centers[c] = HexWorldLayout.ToWorld(c) + origin; }
                if (test == 0) for (int q = 0; q < 5; q++) water.Add(new HexCoordinates(q, 2));
                if (test == 1) { for (int q = 0; q <= 2; q++) water.Add(new HexCoordinates(q, 2)); for (int r = 3; r < 5; r++) water.Add(new HexCoordinates(2, r)); }
                if (test == 2) foreach (var pair in new[] { new[] { 2,2 }, new[] { 3,2 }, new[] { 4,2 }, new[] { 1,3 }, new[] { 0,4 }, new[] { 2,1 }, new[] { 2,0 } }) water.Add(new HexCoordinates(pair[0], pair[1]));
                if (test == 3 || test == 4) for (int r = 0; r < 5; r++) for (int q = 3; q < 5; q++) water.Add(new HexCoordinates(q, r));
                if (test == 3) for (int q = 0; q < 3; q++) water.Add(new HexCoordinates(q, 2));
                if (test == 4) { water.Remove(new HexCoordinates(3, 2)); water.Add(new HexCoordinates(2, 0)); water.Add(new HexCoordinates(2, 4)); }
                if (test == 5) { var c = new HexCoordinates(2,2); water.Add(c); foreach (var n in c.Neighbors()) water.Add(n); }
                var layout = new ConnectedWaterLayout(centers, water);
                var group = new GameObject(CaseNames[test]); group.transform.SetParent(transform, false);
                foreach (var pair in centers)
                {
                    var go = new GameObject("Casilla " + pair.Key.Key); go.transform.SetParent(group.transform, false); go.transform.position = pair.Value;
                    Mesh mesh = land;
                    if (water.Contains(pair.Key)) { mesh = layout.BuildMesh(pair.Value, "SM_Caso_" + CaseNames[test]); generated.Add(mesh); }
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                CaseCenters.Add(centers[new HexCoordinates(2,2)]);
            }
            if (configureCamera && Camera.main != null)
            {
                var camera = Camera.main; var focus = (CaseCenters[0] + CaseCenters[5]) * .5f;
                camera.orthographic = true; camera.orthographicSize = 14f;
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller == null) controller = camera.gameObject.AddComponent<CampaignCameraController>();
                controller.Initialize(offset.x - 2, offset.x + 42, offset.z - 2, offset.z + 20, focus, 14, new Vector3(0,30,-20));
            }
            Ready = true;
        }

        private static Mesh FlatLand()
        {
            var vertices = new Vector3[7]; var colors = new Color[7]; var normals = new Vector3[7]; var uv = new Vector2[7];
            vertices[0] = Vector3.up * .12f; var indices = new List<int>();
            for (int i = 0; i < 6; i++) { float a = Mathf.Deg2Rad * (30 + i * 60); vertices[i + 1] = new Vector3(Mathf.Cos(a), .12f, Mathf.Sin(a)); indices.AddRange(new[] { 0, (i + 1) % 6 + 1, i + 1 }); }
            for (int i = 0; i < 7; i++) { colors[i] = ConnectedWaterLayout.LandColor.linear; colors[i].a = 0; normals[i] = Vector3.up; uv[i] = new Vector2(.2f, 0); }
            var mesh = new Mesh { name = "SM_Suelo_Casos_Agua" }; mesh.vertices = vertices; mesh.colors = colors; mesh.normals = normals; mesh.uv = uv; mesh.SetTriangles(indices, 0); mesh.RecalculateBounds(); return mesh;
        }

        private void OnDestroy()
        {
            foreach (var mesh in generated) if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }
    }
}
