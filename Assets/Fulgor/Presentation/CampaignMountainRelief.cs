using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Relieve continuo de las tierras altas del mapa (colinas y montañas). Dos distancias lo forman:
    /// - al borde de las tierras altas con la tierra baja: una ondulación suave (lomas), que hace de pie a las colinas y a las montañas;
    /// - al borde de la montaña con lo que no es montaña: los picos, encima. Una montaña suelta tiene la cumbre en el centro, dos vecinas comparten
    ///   una cresta y un grupo forma un macizo más alto por dentro.
    /// En el borde con la tierra baja la altura vale exactamente <see cref="EdgeHeight"/>, la misma a la que terminan las piezas de llanura y bosque;
    /// y como es una sola función para todo el tablero, dos casillas vecinas (colina o montaña) casan sin costura. Coordenadas del tablero, radio 1.
    /// Bajo un edificio el suelo se allana: hasta <see cref="FlatRadius"/> del centro queda a la altura media que tenía, y se funde con el relieve
    /// antes de <see cref="FlatBlend"/>, por dentro de la apotema, así que las aristas con las vecinas no cambian.
    /// </summary>
    public sealed class CampaignMountainRelief
    {
        public const float EdgeHeight = .12f;
        /// <summary>Altura sobre el borde de la cumbre de una montaña suelta, con crestas medias.</summary>
        public const float PeakRise = 1.15f;
        /// <summary>Altura sobre el borde de una colina suelta (lomas medias).</summary>
        public const float HillRise = .24f;
        /// <summary>Radio del llano bajo un edificio: cabe la huella más larga (la empalizada llega a 0,55 del centro).</summary>
        public const float FlatRadius = .58f;
        /// <summary>Distancia al centro a la que el llano ya es el relieve de siempre; menor que la apotema (0,866).</summary>
        public const float FlatBlend = .8f;
        private static readonly HexCoordinates[] Directions = { new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1) };
        private readonly float radius, apothem, bucketSize;
        private readonly HashSet<HexCoordinates> mountains, highlands, generated, forests;
        // Relieve continuo: también la llanura y el bosque salen de esta función (ver el constructor)
        private readonly bool continuous;
        private readonly List<Vector4> anchorSegments = new(), forestSegments = new(), shoreSegments = new();
        private readonly Dictionary<Vector2Int, List<int>> anchorBuckets = new(), forestBuckets = new(), shoreBuckets = new();
        /// <summary>Cuánto sube el pie de una montaña antes de llegar a su casilla, y desde qué distancia (en radios).</summary>
        public const float FootRise = .07f, FootReach = .55f;
        /// <summary>Desde qué distancia de una colina (en radios) empieza a subir la llanura vecina.</summary>
        public const float HillReach = .45f;
        /// <summary>Distancia (en radios) a una casilla que no se genera (agua, asentamiento, borde del mapa) en la que el relieve vuelve a su altura.</summary>
        public const float AnchorFade = .7f;
        private readonly List<Vector4> highlandSegments = new();
        private readonly Dictionary<Vector2Int, List<int>> highlandBuckets = new();
        private readonly List<Vector4> segments = new();
        private readonly Dictionary<Vector2Int, List<int>> buckets = new();
        private readonly Dictionary<(long, long), float> heights = new();
        private readonly Dictionary<HexCoordinates, float> flats = new();
        private readonly float offsetX, offsetZ;

        public CampaignMountainRelief(IEnumerable<HexCoordinates> mountainCells, int seed, float outerRadius = VisualScaleContract.HexOuterRadius,
            IEnumerable<HexCoordinates> hillCells = null, IEnumerable<HexCoordinates> builtCells = null,
            IEnumerable<HexCoordinates> lowlandCells = null, IEnumerable<HexCoordinates> forestCells = null, IEnumerable<HexCoordinates> waterCells = null)
        {
            if (outerRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(outerRadius));
            radius = outerRadius; apothem = outerRadius * Mathf.Sqrt(3f) * .5f; bucketSize = 2f * outerRadius;
            mountains = new HashSet<HexCoordinates>(mountainCells ?? throw new ArgumentNullException(nameof(mountainCells)));
            highlands = new HashSet<HexCoordinates>(mountains);
            if (hillCells != null) highlands.UnionWith(hillCells);
            var random = new System.Random(seed);
            offsetX = (float)random.NextDouble() * 512f; offsetZ = (float)random.NextDouble() * 512f;
            AddEdges(mountains, segments, buckets);
            AddEdges(highlands, highlandSegments, highlandBuckets);
            // Con tierra baja (llanura y bosque) el relieve es continuo: una sola función para toda la tierra, sin escalón en el borde de cada
            // casilla. El pie de las montañas y de las colinas empieza en la casilla vecina, la llanura ondula, y solo junto a lo que no se
            // genera (agua, asentamientos, borde del mapa) vuelve a EdgeHeight. Sin tierra baja, el relieve de siempre: 0,12 en el borde.
            generated = new HashSet<HexCoordinates>(highlands);
            forests = new HashSet<HexCoordinates>();
            if (lowlandCells != null) generated.UnionWith(lowlandCells);
            continuous = generated.Count > highlands.Count;
            if (continuous)
            {
                AddEdges(generated, anchorSegments, anchorBuckets);
                if (forestCells != null) forests.UnionWith(forestCells.Where(generated.Contains));
                AddEdges(forests, forestSegments, forestBuckets);
                if (waterCells != null)
                {
                    var water = new HashSet<HexCoordinates>(waterCells);
                    foreach (var cell in generated)
                        foreach (var direction in Directions)
                            if (water.Contains(cell.Add(direction))) AddEdge(cell, cell.Add(direction), shoreSegments, shoreBuckets);
                }
            }
            if (builtCells != null)
                foreach (var cell in builtCells)
                    if (generated.Contains(cell) && !flats.ContainsKey(cell)) flats.Add(cell, MeanNaturalHeight(cell));
        }

        // altura media del relieve sin allanar en el círculo del llano: el edificio queda en una terraza, medio cortada en la loma y medio rellena
        private float MeanNaturalHeight(HexCoordinates cell)
        {
            var centre = Centre(cell); float step = FlatRadius * radius / 6f, sum = 0f; int count = 0;
            for (int i = -6; i <= 6; i++)
            for (int j = -6; j <= 6; j++)
            {
                var p = centre + new Vector2(i, j) * step;
                if ((p - centre).sqrMagnitude > FlatRadius * radius * FlatRadius * radius) continue;
                sum += Natural(p, cell); count++;
            }
            return sum / count;
        }

        // tramos de borde entre las casillas del conjunto y las que no lo son
        private void AddEdges(HashSet<HexCoordinates> cells, List<Vector4> segs, Dictionary<Vector2Int, List<int>> bks)
        {
            foreach (var cell in cells)
            {
                foreach (var direction in Directions)
                {
                    var neighbour = cell.Add(direction);
                    if (cells.Contains(neighbour)) continue;
                    AddEdge(cell, neighbour, segs, bks);
                }
            }
        }

        private void AddEdge(HexCoordinates cell, HexCoordinates neighbour, List<Vector4> segs, Dictionary<Vector2Int, List<int>> bks)
        {
            var centre = Centre(cell);
            var towards = Centre(neighbour) - centre;
            var middle = centre + towards * .5f;
            var along = new Vector2(-towards.y, towards.x).normalized * (radius * .5f);
            AddSegment(middle - along, middle + along, segs, bks);
        }

        public bool IsMountain(HexCoordinates cell) => mountains.Contains(cell);
        /// <summary>Colina o montaña: casillas cuya malla sale de este relieve.</summary>
        public bool IsHighland(HexCoordinates cell) => highlands.Contains(cell);
        /// <summary>Casilla cuya malla sale de este relieve: colinas y montañas y, con relieve continuo, también llanura y bosque.</summary>
        public bool IsGenerated(HexCoordinates cell) => generated.Contains(cell);
        /// <summary>Toda la tierra sale de una sola función, sin escalón en el borde de las casillas.</summary>
        public bool IsContinuous => continuous;
        /// <summary>Casilla de colina o montaña con un edificio: el relieve se allana bajo su huella.</summary>
        public bool IsLevelled(HexCoordinates cell) => flats.ContainsKey(cell);
        public int BoundarySegments => segments.Count;

        public Vector2 Centre(HexCoordinates cell)
        {
            var p = HexWorldLayout.ToWorld(cell, radius);
            return new Vector2(p.x, p.z);
        }

        /// <summary>Esquinas del hexágono (punta arriba), en el orden en que se recorre su contorno.</summary>
        public Vector2 Corner(HexCoordinates cell, int k)
        {
            float a = Mathf.Deg2Rad * (30f + 60f * k);
            return Centre(cell) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }

        /// <summary>Distancia al pie de la montaña: al tramo de borde más cercano con una casilla que no es montaña.</summary>
        public float DistanceToFoot(Vector2 p) => DistanceTo(p, segments, buckets);

        /// <summary>Distancia a la tierra baja: al tramo de borde más cercano con una casilla que no es colina ni montaña.</summary>
        public float DistanceToLowland(Vector2 p) => DistanceTo(p, highlandSegments, highlandBuckets);

        private float DistanceTo(Vector2 p, List<Vector4> segs, Dictionary<Vector2Int, List<int>> bks, int maxReach = 4)
        {
            if (segs.Count == 0) return 4f * bucketSize;
            var home = Bucket(p);
            for (int reach = 1; reach <= maxReach; reach++)
            {
                float best = float.MaxValue;
                for (int x = -reach; x <= reach; x++)
                for (int z = -reach; z <= reach; z++)
                {
                    if (!bks.TryGetValue(new Vector2Int(home.x + x, home.y + z), out var list)) continue;
                    foreach (var index in list) best = Mathf.Min(best, DistanceToSegment(p, segs[index]));
                }
                // un tramo de fuera del cuadrado buscado está, como poco, a reach·cubo − medio tramo
                if (best < float.MaxValue && best <= reach * bucketSize - .5f * radius) return best;
            }
            return 4f * bucketSize;
        }

        /// <summary>Altura del terreno en un punto del tablero; fuera de las colinas y montañas, la del borde.</summary>
        public float Height(Vector2 p)
        {
            var key = ((long)Mathf.Round(p.x * 20000f), (long)Mathf.Round(p.y * 20000f));
            if (heights.TryGetValue(key, out var cached)) return cached;
            float value = Compute(p);
            heights[key] = value;
            return value;
        }

        /// <summary>Cuánto de "montaña" tiene un punto: 0 en el pie, 1 a la altura de la cumbre de una montaña suelta.</summary>
        public float Rise(Vector2 p) => continuous
            ? Mathf.Clamp01(Profile(Mathf.Max(0f, Signed(Warped(p), mountains, segments, buckets))))
            : mountains.Contains(HexWorldLayout.FromWorld(new Vector3(p.x, 0f, p.y), radius)) ? Mathf.Clamp01(Profile(DistanceToFoot(p))) : 0f;

        private HexCoordinates CellAt(Vector2 p) => HexWorldLayout.FromWorld(new Vector3(p.x, 0f, p.y), radius);

        // distancia al borde de un conjunto de casillas: positiva dentro, negativa fuera (fuera solo interesa cerca: se busca a un cubo)
        private float Signed(Vector2 q, HashSet<HexCoordinates> cells, List<Vector4> segs, Dictionary<Vector2Int, List<int>> bks)
        {
            bool inside = cells.Contains(CellAt(q));
            float d = DistanceTo(q, segs, bks, inside ? 4 : 1);
            return inside ? d : -d;
        }

        // el punto, movido por un ruido suave: los pies de montañas, colinas y bosques dejan de seguir el hexágono
        private Vector2 Warped(Vector2 p, float amount = .22f, float shift = 0f) =>
            p + new Vector2(Noise(p.x * .9f + offsetX + shift, p.y * .9f + offsetZ) - .5f,
                Noise(p.x * .9f + offsetZ + 37f, p.y * .9f + offsetX + 11f + shift) - .5f) * (2f * amount * radius);

        /// <summary>
        /// Lo que el sombreador necesita saber de un punto, además de la altura. Relieve de siempre: r = cuánto de montaña. Relieve continuo:
        /// r = montaña (de 0 a 0,5 el pie que se acerca desde fuera, de 0,5 a 1 la subida a la cumbre), b = cuánto de bosque, a = cuánto de orilla.
        /// g = altura sobre el borde, normalizada.
        /// </summary>
        /// <summary>Lo que hay en un punto: r montaña (más de 0,5, roca), g altura, b bosque, a orilla. Para plantar árboles donde toca.</summary>
        public Color SurfaceAt(Vector2 p) => Attributes(p, Height(p));

        private Color Attributes(Vector2 p, float h)
        {
            float g = Mathf.Clamp01((h - EdgeHeight) / 1.3f);
            if (!continuous) return new Color(Rise(p), g, 0f, 1f);
            float dm = Signed(Warped(p), mountains, segments, buckets);
            float m = dm >= 0f ? .5f + .5f * Mathf.Clamp01(Profile(dm)) : .5f * Smooth((dm + FootReach * radius) / (FootReach * radius));
            float forest = forests.Count == 0 ? 0f : Smooth(.5f + Signed(Warped(p, .3f, 53f), forests, forestSegments, forestBuckets) / (.5f * radius));
            float shore = 1f - Smooth(DistanceTo(p, shoreSegments, shoreBuckets, 1) / (.55f * radius));
            return new Color(m, g, forest, shore);
        }

        /// <summary>
        /// Rejilla triangular común a todas las montañas: con radio 1 y esta división, las esquinas y los centros de todas las casillas caen en sus
        /// puntos. La normal de un vértice sale de sus vecinos en esa rejilla (siempre a la resolución completa): son puntos que ya se calculan para
        /// la malla, así que cada vértice cuesta una evaluación y no cinco, y la normal es la misma en las dos casillas de una arista y en todos los
        /// niveles de detalle.
        /// </summary>
        public const int NormalLattice = 32;

        public Vector3 LatticeNormal(Vector2 p)
        {
            float s = radius / NormalLattice;
            var e1 = new Vector2(.8660254f, .5f) * s; var e2 = new Vector2(0f, s);
            float d1 = (Height(p + e1) - Height(p - e1)) / (2f * s);
            float d2 = (Height(p + e2) - Height(p - e2)) / (2f * s);
            float gz = d2, gx = (d1 - .5f * gz) / .8660254f;
            return new Vector3(-gx, 1f, -gz).normalized;
        }

        public Vector3 Normal(Vector2 p)
        {
            const float e = .012f;
            float hx = Height(p + new Vector2(e, 0f)) - Height(p - new Vector2(e, 0f));
            float hz = Height(p + new Vector2(0f, e)) - Height(p - new Vector2(0f, e));
            return new Vector3(-hx, 2f * e, -hz).normalized;
        }

        /// <summary>
        /// Malla de una casilla de colina o montaña, con los vértices relativos al centro de la casilla (sin giro). Las aristas reparten los mismos puntos que
        /// las de la casilla vecina, así que casan; un faldón corto por debajo del contorno tapa las rendijas cuando dos vecinas usan distinto detalle.
        /// Color de vértice: r = cuánto de montaña (0 en el pie), g = altura sobre el borde normalizada.
        /// </summary>
        public Mesh BuildTile(HexCoordinates cell, int subdivisions, string name)
        {
            if (!generated.Contains(cell)) throw new ArgumentException("La casilla no es de colina ni de montaña.", nameof(cell));
            if (subdivisions < 1) throw new ArgumentOutOfRangeException(nameof(subdivisions));
            var centre = Centre(cell);
            var corners = new Vector2[6];
            for (int k = 0; k < 6; k++) corners[k] = Corner(cell, k);
            var positions = new List<Vector3>(); var normals = new List<Vector3>(); var colors = new List<Color>(); var uvs = new List<Vector2>();
            var triangles = new List<int>(); var index = new Dictionary<(long, long), int>();

            int Vertex(Vector2 p)
            {
                var key = ((long)Mathf.Round(p.x * 20000f), (long)Mathf.Round(p.y * 20000f));
                if (index.TryGetValue(key, out var found)) return found;
                float h = Height(p);
                positions.Add(new Vector3(p.x - centre.x, h, p.y - centre.y));
                normals.Add(LatticeNormal(p));
                colors.Add(Attributes(p, h));
                uvs.Add(p);
                index.Add(key, positions.Count - 1);
                return positions.Count - 1;
            }

            Vector2 Point(int k, int i, int j, int n) =>
                centre + (corners[k] - centre) * ((float)i / n) + (corners[(k + 1) % 6] - centre) * ((float)j / n);

            int n = subdivisions;
            for (int k = 0; k < 6; k++)
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n - i; j++)
            {
                int v00 = Vertex(Point(k, i, j, n)), v10 = Vertex(Point(k, i + 1, j, n)), v01 = Vertex(Point(k, i, j + 1, n));
                triangles.Add(v00); triangles.Add(v01); triangles.Add(v10);
                if (i + j < n - 1)
                {
                    int v11 = Vertex(Point(k, i + 1, j + 1, n));
                    triangles.Add(v10); triangles.Add(v01); triangles.Add(v11);
                }
            }

            // faldón: cada tramo del contorno baja 6 cm por debajo de su propia altura
            const float skirt = .06f;
            for (int k = 0; k < 6; k++)
            for (int s = 0; s < n; s++)
            {
                var a = Vector2.Lerp(corners[k], corners[(k + 1) % 6], (float)s / n);
                var b = Vector2.Lerp(corners[k], corners[(k + 1) % 6], (float)(s + 1) / n);
                int top0 = Vertex(a), top1 = Vertex(b);
                int low0 = positions.Count; positions.Add(positions[top0] - Vector3.up * skirt); normals.Add(normals[top0]); colors.Add(colors[top0]); uvs.Add(uvs[top0]);
                int low1 = positions.Count; positions.Add(positions[top1] - Vector3.up * skirt); normals.Add(normals[top1]); colors.Add(colors[top1]); uvs.Add(uvs[top1]);
                triangles.Add(top0); triangles.Add(top1); triangles.Add(low1);
                triangles.Add(top0); triangles.Add(low1); triangles.Add(low0);
            }

            var mesh = new Mesh { name = name, indexFormat = positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetColors(colors); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Mallas de una casilla en los tres niveles de detalle.</summary>
        public readonly struct TileLevels
        {
            public readonly Mesh Full, Medium, Distant;
            public TileLevels(Mesh full, Mesh medium, Mesh distant) { Full = full; Medium = medium; Distant = distant; }
        }

        /// <summary>Detalle medio y lejano que acompañan a un detalle completo, los mismos que usa el aspecto del mapa.</summary>
        public static int MediumSubdivisions(int subdivisions) => Mathf.Max(3, subdivisions / 2);
        public static int DistantSubdivisions(int subdivisions) => Mathf.Max(2, subdivisions / 4);

        // direcciones de las seis esquinas en la rejilla común (puntos a·e1 + b·e2, con e1 a 30° y e2 a 90°): la esquina k está en 32·U[k]
        private static readonly Vector2Int[] LatticeCorners = { new(1, 0), new(0, 1), new(-1, 1), new(-1, 0), new(0, -1), new(1, -1) };

        /// <summary>
        /// Mallas de muchas casillas a la vez, con <paramref name="subdivisions"/> tramos por radio en el detalle completo y
        /// <see cref="MediumSubdivisions"/> y <see cref="DistantSubdivisions"/> en el medio y el lejano: las mismas que dan tres
        /// <see cref="BuildTile"/>, pero cada casilla se calcula una sola vez y en paralelo con las demás. Sus puntos están en la rejilla común con
        /// coordenadas enteras, así que una arista compartida da la misma altura y la misma normal en las dos casillas sin caché compartida, y los
        /// niveles medio y lejano toman uno de cada dos y uno de cada cuatro puntos. Las Mesh se crean al final, en el hilo principal. Si el detalle
        /// no divide la rejilla (<see cref="NormalLattice"/>), se hace casilla a casilla con <see cref="BuildTile"/>.
        /// </summary>
        public Dictionary<HexCoordinates, TileLevels> BuildTiles(IEnumerable<HexCoordinates> cells, int subdivisions, string prefix = "Relieve ")
        {
            var list = cells.Distinct().ToList();
            foreach (var cell in list)
                if (!generated.Contains(cell)) throw new ArgumentException($"La casilla {cell.Key} no es de colina ni de montaña.", nameof(cells));
            int medium = MediumSubdivisions(subdivisions), distant = DistantSubdivisions(subdivisions);
            var result = new Dictionary<HexCoordinates, TileLevels>(list.Count);
            if (subdivisions < 1 || NormalLattice % subdivisions != 0 || NormalLattice % medium != 0 || NormalLattice % distant != 0)
            {
                foreach (var cell in list)
                    result[cell] = new TileLevels(BuildTile(cell, subdivisions, prefix + cell.Key), BuildTile(cell, medium, prefix + cell.Key + " media"),
                        BuildTile(cell, distant, prefix + cell.Key + " lejana"));
                return result;
            }
            var levels = new[] { subdivisions, medium, distant };
            var data = new LatticeMesh[list.Count][];
            void Build(int t) => data[t] = BuildLatticeTile(list[t], levels);
            if (Application.platform == RuntimePlatform.WebGLPlayer) for (int t = 0; t < list.Count; t++) Build(t); // sin hilos
            else Parallel.For(0, list.Count, Build);
            for (int t = 0; t < list.Count; t++)
            {
                string name = prefix + list[t].Key;
                result[list[t]] = new TileLevels(data[t][0].ToMesh(name), data[t][1].ToMesh(name + " media"), data[t][2].ToMesh(name + " lejana"));
            }
            return result;
        }

        // malla calculada fuera del hilo principal
        private sealed class LatticeMesh
        {
            public readonly List<Vector3> Positions = new(), Normals = new();
            public readonly List<Color> Colors = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<int> Triangles = new();

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = Positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(Positions); mesh.SetNormals(Normals); mesh.SetColors(Colors); mesh.SetUVs(0, Uvs);
                mesh.SetTriangles(Triangles, 0); mesh.RecalculateBounds();
                return mesh;
            }
        }

        private Vector2 LatticePoint(int a, int b)
        {
            float s = radius / NormalLattice;
            return new Vector2(a * (.8660254f * s), (a * .5f + b) * s);
        }

        // Una casilla en la rejilla común: alturas de todos sus puntos y de un anillo más (para las normales del borde), calculadas una vez, y
        // de ahí las mallas de cada detalle, en el mismo orden de vértices y triángulos que BuildTile. Solo lee el relieve: se puede llamar en paralelo.
        private LatticeMesh[] BuildLatticeTile(HexCoordinates cell, int[] levels)
        {
            const int N = NormalLattice, Ring = N + 1, Width = 2 * Ring + 1;
            int a0 = 2 * N * cell.Q + N * cell.R, b0 = N * (cell.R - cell.Q); // el centro de la casilla en la rejilla
            int At(int a, int b) => (a + Ring) * Width + (b + Ring);
            var heights = new float[Width * Width];
            for (int a = -Ring; a <= Ring; a++)
            for (int b = -Ring; b <= Ring; b++)
                if (Math.Abs(a + b) <= Ring) heights[At(a, b)] = Compute(LatticePoint(a0 + a, b0 + b));
            var attributes = new Color[Width * Width];
            var known = new bool[Width * Width];
            var centre = Centre(cell);
            float twice = 2f * radius / N;

            var meshes = new LatticeMesh[levels.Length];
            var index = new int[Width * Width];
            for (int level = 0; level < levels.Length; level++)
            {
                int n = levels[level], step = N / n;
                var mesh = meshes[level] = new LatticeMesh();
                for (int i = 0; i < index.Length; i++) index[i] = -1;

                int Vertex(int a, int b)
                {
                    int key = At(a, b);
                    if (index[key] >= 0) return index[key];
                    float h = heights[key];
                    var p = LatticePoint(a0 + a, b0 + b);
                    mesh.Positions.Add(new Vector3(p.x - centre.x, h, p.y - centre.y));
                    // la normal de la rejilla, como LatticeNormal: diferencias a lo largo de e1 (a) y e2 (b)
                    float d1 = (heights[At(a + 1, b)] - heights[At(a - 1, b)]) / twice, gz = (heights[At(a, b + 1)] - heights[At(a, b - 1)]) / twice;
                    mesh.Normals.Add(new Vector3(-(d1 - .5f * gz) / .8660254f, 1f, -gz).normalized);
                    if (!known[key]) { attributes[key] = Attributes(p, h); known[key] = true; }
                    mesh.Colors.Add(attributes[key]);
                    mesh.Uvs.Add(p);
                    index[key] = mesh.Positions.Count - 1;
                    return index[key];
                }

                int Point(int k, int i, int j)
                {
                    var u = LatticeCorners[k]; var v = LatticeCorners[(k + 1) % 6];
                    return Vertex(step * (i * u.x + j * v.x), step * (i * u.y + j * v.y));
                }

                for (int k = 0; k < 6; k++)
                for (int i = 0; i < n; i++)
                for (int j = 0; j < n - i; j++)
                {
                    int v00 = Point(k, i, j), v10 = Point(k, i + 1, j), v01 = Point(k, i, j + 1);
                    mesh.Triangles.Add(v00); mesh.Triangles.Add(v01); mesh.Triangles.Add(v10);
                    if (i + j < n - 1)
                    {
                        int v11 = Point(k, i + 1, j + 1);
                        mesh.Triangles.Add(v10); mesh.Triangles.Add(v01); mesh.Triangles.Add(v11);
                    }
                }

                // faldón, como en BuildTile: el punto s de la arista k está en step·((n − s)·U[k] + s·U[k + 1])
                const float skirt = .06f;
                for (int k = 0; k < 6; k++)
                for (int s = 0; s < n; s++)
                {
                    int top0 = Point(k, n - s, s), top1 = Point(k, n - s - 1, s + 1);
                    int low0 = mesh.Positions.Count; AddSkirt(mesh, top0, skirt);
                    int low1 = mesh.Positions.Count; AddSkirt(mesh, top1, skirt);
                    mesh.Triangles.Add(top0); mesh.Triangles.Add(top1); mesh.Triangles.Add(low1);
                    mesh.Triangles.Add(top0); mesh.Triangles.Add(low1); mesh.Triangles.Add(low0);
                }
            }
            return meshes;
        }

        private static void AddSkirt(LatticeMesh mesh, int top, float depth)
        {
            mesh.Positions.Add(mesh.Positions[top] - Vector3.up * depth); mesh.Normals.Add(mesh.Normals[top]);
            mesh.Colors.Add(mesh.Colors[top]); mesh.Uvs.Add(mesh.Uvs[top]);
        }

        private float Compute(Vector2 p)
        {
            var cell = HexWorldLayout.FromWorld(new Vector3(p.x, 0f, p.y), radius);
            if (!generated.Contains(cell)) return EdgeHeight;
            float natural = Natural(p, cell);
            if (!flats.TryGetValue(cell, out var flat)) return natural;
            float level = 1f - Smooth(((p - Centre(cell)).magnitude - FlatRadius * radius) / ((FlatBlend - FlatRadius) * radius));
            return Mathf.Lerp(natural, flat, level);
        }

        // Toda la tierra de una vez. Las distancias a montañas y colinas llevan signo (negativas fuera) y se miden desde el punto movido por el
        // ruido, así que el pie empieza antes de la casilla y no dibuja su hexágono.
        private float NaturalContinuous(Vector2 p)
        {
            float fade = Smooth(DistanceTo(p, anchorSegments, anchorBuckets, 1) / (AnchorFade * radius));
            if (fade <= 0f) return EdgeHeight;
            var q = Warped(p);
            // la llanura ondula: vaguadas y lomas anchas, y un grano fino
            float h = .10f * Fbm(p.x * .45f + offsetX, p.y * .45f + offsetZ) + .03f * (Fbm(p.x * 2.1f + offsetZ, p.y * 2.1f + offsetX) - .5f)
                + .008f * (Fbm(p.x * 9f + offsetX, p.y * 9f + offsetZ) - .5f);
            float dh = Signed(q, highlands, highlandSegments, highlandBuckets);
            float reach = HillReach * radius;
            if (dh > -reach)
            {
                float swell = Smooth((dh + reach) / (apothem * 1.15f + reach));
                h += HillRise * swell * (.75f + .5f * Fbm(p.x * 1.3f + offsetZ, p.y * 1.3f + offsetX));
            }
            float dm = Signed(q, mountains, segments, buckets);
            float foot = FootReach * radius;
            if (dm > -foot)
            {
                if (dm < 0f) { float t = Smooth((dm + foot) / foot); h += FootRise * t * t; }
                else
                {
                    float s = Smooth(dm / (.35f * radius));
                    float profile = Profile(dm);
                    float ridges = Ridged(p.x * 2.2f + offsetX, p.y * 2.2f + offsetZ);
                    float erosion = Ridged(p.x * 6.5f + offsetZ, p.y * 6.5f + offsetX) - .35f;
                    float detail = Fbm(p.x * 13f + offsetZ, p.y * 13f + offsetX) - .5f;
                    h += FootRise + PeakRise * profile * (.62f + .6f * ridges) + .11f * erosion * Mathf.Clamp01(profile) * s + .025f * detail * s;
                }
            }
            return EdgeHeight + fade * h;
        }

        private float Natural(Vector2 p, HexCoordinates cell)
        {
            if (continuous) return NaturalContinuous(p);
            float height = EdgeHeight;
            // lomas: colinas y montañas forman una tierra alta ondulada que baja hasta la tierra baja
            float toLowland = DistanceToLowland(p);
            if (toLowland > 1e-5f)
            {
                float rim = Smooth(toLowland / (.35f * radius));
                float swell = Smooth(toLowland / (apothem * 1.15f));
                float rolling = .75f + .5f * Fbm(p.x * 1.3f + offsetZ, p.y * 1.3f + offsetX);
                height += HillRise * swell * rolling + .02f * (Fbm(p.x * 9f + offsetX, p.y * 9f + offsetZ) - .5f) * rim;
            }
            if (!mountains.Contains(cell)) return height;
            float near = DistanceToFoot(p);
            if (near <= 1e-5f) return height;
            // una ligera deformación del espacio quita la forma de pirámide; se apaga junto al pie para que el borde siga a la altura justa
            float s = Smooth(near / (.35f * radius));
            var warp = new Vector2(Noise(p.x * .9f + offsetX, p.y * .9f + offsetZ) - .5f,
                Noise(p.x * .9f + offsetZ + 37f, p.y * .9f + offsetX + 11f) - .5f) * (.5f * radius * s);
            float d = Mathf.Lerp(near, DistanceToFoot(p + warp), s);
            float profile = Profile(d);
            float ridges = Ridged(p.x * 2.2f + offsetX, p.y * 2.2f + offsetZ);
            // erosión: crestas finas que marcan barrancos en las laderas, más fuertes cuanto más arriba
            float erosion = Ridged(p.x * 6.5f + offsetZ, p.y * 6.5f + offsetX) - .35f;
            float detail = Fbm(p.x * 13f + offsetZ, p.y * 13f + offsetX) - .5f;
            return height + PeakRise * profile * (.62f + .6f * ridges) + .11f * erosion * Mathf.Clamp01(profile) * s + .025f * detail * s;
        }

        // falda cóncava hasta la apotema (la cumbre de una montaña suelta); más adentro de un macizo sigue subiendo, más despacio
        private float Profile(float distance)
        {
            float t = distance / apothem;
            return t < 1f ? Mathf.Pow(t, 1.35f) : 1f + .45f * (t - 1f);
        }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        private Vector2Int Bucket(Vector2 p) => new(Mathf.FloorToInt(p.x / bucketSize), Mathf.FloorToInt(p.y / bucketSize));

        private void AddSegment(Vector2 a, Vector2 b, List<Vector4> segs, Dictionary<Vector2Int, List<int>> bks)
        {
            segs.Add(new Vector4(a.x, a.y, b.x, b.y));
            var key = Bucket((a + b) * .5f);
            if (!bks.TryGetValue(key, out var list)) bks.Add(key, list = new List<int>());
            list.Add(segs.Count - 1);
        }

        private static float DistanceToSegment(Vector2 p, Vector4 s)
        {
            var a = new Vector2(s.x, s.y); var b = new Vector2(s.z, s.w);
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-12f));
            return Vector2.Distance(p, a + ab * t);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / 16777215f;
            }
        }

        private static float Noise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            float a = Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), ux);
            float b = Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), ux);
            return Mathf.Lerp(a, b, uy);
        }

        private static float Fbm(float x, float y)
        {
            float sum = 0f, amplitude = .5f, total = 0f;
            for (int o = 0; o < 4; o++) { sum += amplitude * Noise(x, y); total += amplitude; x = x * 2.03f + 17.1f; y = y * 2.03f + 5.3f; amplitude *= .5f; }
            return sum / total;
        }

        // crestas: 1 - |2n - 1| al cuadrado, en tres octavas
        private static float Ridged(float x, float y)
        {
            float sum = 0f, amplitude = .55f, total = 0f;
            for (int o = 0; o < 3; o++)
            {
                float r = 1f - Mathf.Abs(2f * Noise(x, y) - 1f);
                sum += amplitude * r * r; total += amplitude;
                x = x * 2.1f + 7.7f; y = y * 2.1f + 3.1f; amplitude *= .5f;
            }
            return sum / total;
        }
    }
}
