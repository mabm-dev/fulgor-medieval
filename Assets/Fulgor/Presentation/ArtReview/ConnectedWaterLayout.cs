using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Core.Map;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>A shared world-space shoreline field; neighboring meshes sample the same surface.</summary>
    public sealed class ConnectedWaterLayout
    {
        private readonly struct Node
        {
            public Node(Vector2 center, float radius, WaterVisualKind kind) { Center = center; Radius = radius; Kind = kind; }
            public readonly Vector2 Center;
            public readonly float Radius;
            public readonly WaterVisualKind Kind;
        }
        private readonly struct Link
        {
            public Link(Node a, Node b) { A = a; B = b; }
            public readonly Node A, B;
        }
        private readonly struct Edge
        {
            public Edge(Vector2 a, Vector2 b) { A = a; B = b; }
            public readonly Vector2 A, B;
        }
        private readonly List<Node> nodes = new();
        private readonly List<Link> links = new();
        private readonly List<Edge> coast = new();
        public IReadOnlyDictionary<HexCoordinates, WaterVisualKind> Kinds => kinds;
        private readonly Dictionary<HexCoordinates, WaterVisualKind> kinds = new();
        public int CoastEdges => coast.Count;
        public static readonly Color LandColor = new Color(.455f, .561f, .290f);

        public ConnectedWaterLayout(IReadOnlyDictionary<HexCoordinates, Vector3> centers, ISet<HexCoordinates> water)
        {
            if (centers == null || water == null) throw new ArgumentNullException();
            var sorted = water.OrderBy(c => c.R).ThenBy(c => c.Q).ToArray();
            var broad = new HashSet<HexCoordinates>();
            foreach (var c in sorted)
            {
                var neighbors = c.Neighbors();
                for (int i = 0; i < 6; i++)
                    if (water.Contains(neighbors[i]) && water.Contains(neighbors[(i + 1) % 6])) broad.Add(c);
                if (neighbors.Count(water.Contains) >= 4) broad.Add(c);
            }
            var remaining = new HashSet<HexCoordinates>(water);
            foreach (var start in sorted)
            {
                if (!remaining.Remove(start)) continue;
                var region = new List<HexCoordinates>(); var queue = new Queue<HexCoordinates>(); queue.Enqueue(start);
                bool boundary = false;
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue(); region.Add(c);
                    foreach (var n in c.Neighbors())
                    {
                        boundary |= !centers.ContainsKey(n);
                        if (remaining.Remove(n)) queue.Enqueue(n);
                    }
                }
                bool hasBody = region.Any(broad.Contains);
                foreach (var c in region)
                    kinds[c] = region.Count == 1 || broad.Contains(c)
                        ? (boundary ? WaterVisualKind.Sea : WaterVisualKind.Lake)
                        : hasBody || region.Count > 1 ? WaterVisualKind.River : WaterVisualKind.Lake;
            }
            var byCoordinate = new Dictionary<HexCoordinates, Node>();
            foreach (var c in sorted)
            {
                var position = centers[c]; var kind = kinds[c];
                var node = new Node(new Vector2(position.x, position.z), kind == WaterVisualKind.River ? .225f : 1.06f, kind);
                nodes.Add(node); byCoordinate.Add(c, node);
            }
            var curved = new HashSet<HexCoordinates>(sorted.Where(c => kinds[c] == WaterVisualKind.River && c.Neighbors().Count(water.Contains) == 2));
            // Round a two-port bend with a quadratic arc; each arc ends at the shared midpoint.
            foreach (var c in curved)
            {
                var adjacent = c.Neighbors().Where(water.Contains).ToArray(); var control = byCoordinate[c];
                var first = byCoordinate[adjacent[0]]; var last = byCoordinate[adjacent[1]];
                var a = (first.Center + control.Center) * .5f; var b = (last.Center + control.Center) * .5f;
                float ra = (first.Radius + control.Radius) * .5f, rb = (last.Radius + control.Radius) * .5f;
                Node previous = new Node(a, ra, WaterVisualKind.River);
                for (int sample = 1; sample <= 12; sample++) {
                    float t = sample / 12f, u = 1 - t;
                    var next = new Node(u * u * a + 2 * u * t * control.Center + t * t * b,
                        u * u * ra + 2 * u * t * control.Radius + t * t * rb, WaterVisualKind.River);
                    links.Add(new Link(previous, next)); previous = next;
                }
                int index = nodes.FindIndex(n => n.Center == control.Center);
                nodes.RemoveAt(index);
            }
            foreach (var c in sorted)
            {
                var node = byCoordinate[c];
                var adjacentWater = c.Neighbors().Where(water.Contains).ToArray();
                if (node.Kind == WaterVisualKind.River && adjacentWater.Length == 1) {
                    var neighbor = adjacentWater[0]; var continuation = new HexCoordinates(2 * c.Q - neighbor.Q, 2 * c.R - neighbor.R);
                    if (!centers.ContainsKey(continuation)) {
                        var end = new Node(node.Center * 2 - byCoordinate[neighbor].Center, node.Radius, node.Kind);
                        links.Add(new Link(node, end));
                    }
                }
                foreach (var n in c.Neighbors())
                {
                    if (water.Contains(n))
                    {
                        if (n.R > c.R || n.R == c.R && n.Q > c.Q) {
                            var other = byCoordinate[n];
                            if (!(curved.Contains(c) && curved.Contains(n))) {
                                var midpoint = new Node((node.Center + other.Center) * .5f, (node.Radius + other.Radius) * .5f, WaterVisualKind.River);
                                links.Add(new Link(curved.Contains(c) ? midpoint : node, curved.Contains(n) ? midpoint : other));
                            }
                        }
                    }
                    else if (centers.TryGetValue(n, out var neighbor))
                    {
                        var direction = (new Vector2(neighbor.x, neighbor.z) - node.Center).normalized;
                        var middle = node.Center + direction * (Mathf.Sqrt(3f) * .5f);
                        var tangent = new Vector2(-direction.y, direction.x) * .5f;
                        coast.Add(new Edge(middle - tangent, middle + tangent));
                    }
                }
            }
        }

        private static float Smooth(float a, float b, float value)
        {
            float t = Mathf.Clamp01((value - a) / (b - a)); return t * t * (3 - 2 * t);
        }
        private static float Segment(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var direction = b - a;
            t = Mathf.Clamp01(Vector2.Dot(p - a, direction) / direction.sqrMagnitude);
            return Vector2.Distance(p, a + direction * t);
        }
        private static float SmoothMinimum(float a, float b, float radius)
        {
            float h = Mathf.Max(radius - Mathf.Abs(a - b), 0) / radius;
            return Mathf.Min(a, b) - h * h * radius * .25f;
        }

        public float Distance(Vector2 point, out WaterVisualKind kind)
        {
            point = new Vector2(Mathf.Round(point.x * 100000) / 100000, Mathf.Round(point.y * 100000) / 100000);
            float distance = 10000; kind = WaterVisualKind.Sea;
            foreach (var node in nodes)
            {
                float d = Vector2.Distance(point, node.Center) - node.Radius;
                if (d < distance) { distance = d; kind = node.Kind; }
            }
            foreach (var link in links)
            {
                float d = Segment(point, link.A.Center, link.B.Center, out float t) - Mathf.Lerp(link.A.Radius, link.B.Radius, t);
                if (d < distance) { distance = d; kind = t < .5f ? link.A.Kind : link.B.Kind; }
            }
            // A continuous, restrained variation avoids perfectly straight banks.
            distance += .024f * Mathf.Sin(point.x * 3.1f + .7f * Mathf.Cos(point.y * 2.3f));
            float boundary = 10000;
            foreach (var edge in coast) boundary = SmoothMinimum(boundary, Segment(point, edge.A, edge.B, out _), .16f);
            return Mathf.Max(distance, .13f - boundary);
        }
        public float Height(Vector2 point) => .04f + .08f * Smooth(-.025f, .105f, Distance(point, out _));
        public Vector3 Normal(Vector2 point)
        {
            const float e = .015f;
            float dx = (Height(point + Vector2.right * e) - Height(point - Vector2.right * e)) / (2 * e);
            float dz = (Height(point + Vector2.up * e) - Height(point - Vector2.up * e)) / (2 * e);
            return new Vector3(-dx, 1, -dz).normalized;
        }
        public Color ColorAt(Vector2 point, out float distance)
        {
            distance = Distance(point, out var kind);
            Color shallow = new(.08f, .43f, .59f);
            Color deep = kind == WaterVisualKind.Lake ? new Color(.045f, .25f, .34f) : new Color(.025f, .16f, .35f);
            Color water = Color.Lerp(shallow, deep, Smooth(.04f, .65f, -distance));
            Color wet = new(.29f, .34f, .27f), sand = new(.54f, .46f, .30f);
            Color color = distance < -.025f ? water : distance < .025f
                ? Color.Lerp(water, wet, Smooth(-.025f, .025f, distance)) : distance < .06f
                ? Color.Lerp(wet, sand, Smooth(.025f, .06f, distance))
                : Color.Lerp(sand, LandColor, Smooth(.06f, .12f, distance));
            color = color.linear; color.a = 1 - Smooth(-.025f, .08f, distance); return color;
        }

        public Mesh BuildMesh(Vector3 center, string name, int segmentsPerEdge = 12, int rings = 18)
        {
            int count = segmentsPerEdge * 6;
            var vertices = new Vector3[1 + count * rings]; var normals = new Vector3[vertices.Length];
            var colors = new Color[vertices.Length]; var uv = new Vector2[vertices.Length];
            void Vertex(int index, float x, float z)
            {
                var world = new Vector2(center.x + x, center.z + z);
                vertices[index] = new Vector3(x, Height(world) - center.y, z);
                normals[index] = Normal(world); colors[index] = ColorAt(world, out float d); uv[index] = new Vector2(d, 0);
            }
            Vertex(0, 0, 0);
            for (int ring = 1; ring <= rings; ring++)
                for (int i = 0; i < count; i++)
                {
                    float t = (i % segmentsPerEdge) / (float)segmentsPerEdge;
                    float a = Mathf.Deg2Rad * (30 + 60 * (i / segmentsPerEdge)); float b = a + Mathf.PI / 3;
                    float x = Mathf.Lerp(Mathf.Cos(a), Mathf.Cos(b), t) * ring / rings;
                    float z = Mathf.Lerp(Mathf.Sin(a), Mathf.Sin(b), t) * ring / rings;
                    Vertex(1 + (ring - 1) * count + i, x, z);
                }
            var triangles = new List<int>();
            for (int i = 0; i < count; i++) triangles.AddRange(new[] { 0, 1 + (i + 1) % count, 1 + i });
            for (int ring = 0; ring < rings - 1; ring++)
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count, a = 1 + ring * count, b = a + count;
                    triangles.AddRange(new[] { a + i, b + j, b + i, a + i, a + j, b + j });
                }
            var points = vertices.ToList(); var normalList = normals.ToList(); var colorList = colors.ToList(); var uvList = uv.ToList();
            float HexEdge(Vector3 v) => Mathf.Max(Mathf.Abs(v.x) / (Mathf.Sqrt(3f) / 2), (Mathf.Abs(v.x) + Mathf.Sqrt(3f) * Mathf.Abs(v.z)) / Mathf.Sqrt(3f));
            for (int pass = 0; pass < 2; pass++) {
                var mids = new Dictionary<long, int>();
                long Key(int a, int b) => ((long)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                void Split(int a, int b) {
                    long key = Key(a, b); if (mids.ContainsKey(key)) return;
                    var local = (points[a] + points[b]) * .5f; var world = new Vector2(center.x + local.x, center.z + local.z);
                    local.y = Height(world) - center.y; int index = points.Count; points.Add(local); normalList.Add(Normal(world));
                    colorList.Add(ColorAt(world, out float d)); uvList.Add(new Vector2(d, 0)); mids.Add(key, index);
                }
                for (int at = 0; at < triangles.Count; at += 3) {
                    int a = triangles[at], b = triangles[at + 1], c = triangles[at + 2];
                    float min = Mathf.Min(uvList[a].x, Mathf.Min(uvList[b].x, uvList[c].x));
                    float max = Mathf.Max(uvList[a].x, Mathf.Max(uvList[b].x, uvList[c].x));
                    bool bank = min < .17f && max > -.09f;
                    foreach (var edge in new[] { new[] { a, b }, new[] { b, c }, new[] { c, a } }) {
                        var middle = (points[edge[0]] + points[edge[1]]) * .5f;
                        if (bank || HexEdge(middle) > .9999f) Split(edge[0], edge[1]);
                    }
                }
                var refined = new List<int>();
                for (int at = 0; at < triangles.Count; at += 3) {
                    int a = triangles[at], b = triangles[at + 1], c = triangles[at + 2];
                    bool ab = mids.TryGetValue(Key(a, b), out int x), bc = mids.TryGetValue(Key(b, c), out int y), ca = mids.TryGetValue(Key(c, a), out int z);
                    int mask = (ab ? 1 : 0) | (bc ? 2 : 0) | (ca ? 4 : 0);
                    switch (mask) {
                        case 0: refined.AddRange(new[] { a,b,c }); break;
                        case 1: refined.AddRange(new[] { a,x,c, x,b,c }); break;
                        case 2: refined.AddRange(new[] { b,y,a, y,c,a }); break;
                        case 4: refined.AddRange(new[] { c,z,b, z,a,b }); break;
                        case 3: refined.AddRange(new[] { b,y,x, a,x,c, x,y,c }); break;
                        case 6: refined.AddRange(new[] { c,z,y, b,y,a, y,z,a }); break;
                        case 5: refined.AddRange(new[] { a,x,z, c,z,b, z,x,b }); break;
                        default: refined.AddRange(new[] { a,x,z, x,b,y, z,y,c, x,y,z }); break;
                    }
                }
                triangles = refined;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = points.ToArray(); mesh.normals = normalList.ToArray(); mesh.colors = colorList.ToArray(); mesh.uv = uvList.ToArray();
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return mesh;
        }
    }
}
