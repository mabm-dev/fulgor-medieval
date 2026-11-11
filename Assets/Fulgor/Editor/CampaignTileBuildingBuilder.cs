using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>Prepara los edificios de casilla (prefab y casilla de prueba) y gira la fortaleza de Castilla.</summary>
    public static class CampaignTileBuildingBuilder
    {
        const string ArtRoot = "Assets/Fulgor/Art/Buildings";
        const string Folder = "Assets/Fulgor/Resources/Edificios";
        const string ConquestScene = "Assets/Fulgor/Scenes/Playable/FulgorConquest.unity";
        const string FortressPrefab = "Assets/Fulgor/Prefabs/PF_FortalezaCastilla.prefab";
        const string NoisePath = "Assets/Fulgor/Art/Terrain/Relieve/T_Terreno_Ruido.png";
        static readonly Vector2Int FortressCell = new(13, 8);

        /// <summary>Un edificio de casilla: su carpeta de arte (SM_/T_ con su nombre), la casilla de prueba y hacia dónde mira la puerta.</summary>
        readonly struct Building
        {
            public readonly string Name, Label;
            public readonly Vector2Int Cell;
            public readonly float DoorMinute; // minutero: 0 norte, 15 este, 30 sur, 45 oeste; negativo = mirar a la fortaleza
            public readonly float Size; // tamaño respecto a la huella normal (80 % del radio seguro de la casilla); un pozo no es un edificio
            public Building(string name, string label, Vector2Int cell, float doorMinute, float size = 1f) { Name = name; Label = label; Cell = cell; DoorMinute = doorMinute; Size = size; }
            public string PrefabPath => $"{Folder}/PF_{Name}.prefab";
        }

        // Al empezar, la niebla de guerra solo deja ver lo que está a CampaignFogState.VisionRadius de la fortaleza y de la hueste que sale con ella
        // (PlayableConquest); cualquier otra casilla se dibuja negra. Los edificios de prueba tienen que caer dentro de lo visible.
        static readonly Vector2Int[] StartVisionSources = { new(13, 8), new(13, 9) };

        // casillas de prueba junto a la fortaleza (13,8), en llanura siempre que quede alguna visible
        static readonly Building[] Buildings =
        {
            new("CentroAprovisionamiento", "Centro de aprovisionamiento", new Vector2Int(12, 9), 25f),
            new("Almacen", "Almacén", new Vector2Int(14, 11), 35f), // lejos de la fortaleza: (13,7) es la llanura libre de su primer anillo
            // hechos con TRELLIS.2 desde los sprites de la edad 01; puerta hacia la fortaleza hasta que se indique otra
            // Tamaños: todos llenaban la casilla por igual, así que una vivienda de una planta, un puesto o una leñera abultaban como la casa de
            // conocimiento, y el tejadillo del pozo llegaba a la altura de un tejado. Lo pequeño va más pequeño y el patio, que salía corto, más grande.
            new("ViviendaSencilla", "Vivienda sencilla", new Vector2Int(13, 9), -1f, .84f),
            new("Pozo", "Pozo", new Vector2Int(12, 10), -1f, .38f), // un pozo no es un edificio: va más pequeño que el resto
            new("Lenera", "Leñera", new Vector2Int(11, 9), -1f, .8f),
            new("PuestoIntercambio", "Puesto de intercambio", new Vector2Int(13, 10), -1f, .8f),
            new("CasaConocimiento", "Casa de conocimiento", new Vector2Int(14, 9), -1f),
            new("CampoMilicias", "Campo de milicias", new Vector2Int(13, 6), -1f),
            new("CanteraSencilla", "Cantera sencilla", new Vector2Int(15, 6), -1f),
            new("AldeaConcejil", "Aldea concejil", new Vector2Int(12, 11), -1f),
            new("PatioMesnadas", "Patio de mesnadas", new Vector2Int(12, 8), -1f, 1.18f), // colina junto a la fortaleza: en llanura no queda casilla visible
            new("Empalizada", "Empalizada", new Vector2Int(11, 10), -1f),
            new("HuertoCampo", "Huerto o campo", new Vector2Int(14, 10), -1f),
            // edad 02; Tripo la entregó ya con textura y UV (modo directo del script de Blender), sin pasar por TRELLIS
            new("CercaReforzada", "Cerca reforzada", new Vector2Int(15, 9), -1f), // colina: las llanuras visibles ya están ocupadas
            new("CasaAldea", "Casa de aldea", new Vector2Int(14, 8), -1f), // colina con oro junto a la fortaleza: lo único visible que queda libre
        };

        // Yacimientos y sus obras, sin casilla de prueba: CampaignTileBuildings pone el yacimiento donde diga el mapa y lo cambia por la mina o la
        // cantera cuando la ciudad la termina. Tripo los entregó con textura y UV (modo directo del script de Blender).
        static readonly Building[] ResourceModels =
        {
            new("YacimientoOro", "Yacimiento de oro", default, 30f),
            new("YacimientoPiedra", "Yacimiento de piedra", default, 30f),
            new("MinaOro", "Mina de oro", default, 30f),
            new("CanteraPiedra", "Cantera de piedra", default, 30f),
        };

        [MenuItem("Fulgor/Arte/Preparar yacimientos y minas")]
        public static void PrepareResourceModels() => Run(() =>
        {
            Directory.CreateDirectory(Folder);
            foreach (var model in ResourceModels) BuildPrefab(model);
            AssetDatabase.SaveAssets();
        });

        [MenuItem("Fulgor/Arte/Preparar edificios de casilla")]
        public static void BuildPrefab() => Run(BuildAllPrefabs);

        [MenuItem("Fulgor/Arte/Preparar y colocar edificios de casilla")]
        public static void PrepareAndPlace() => Run(() => { BuildAllPrefabs(); PlaceAll(); });

        static void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void BuildAllPrefabs()
        {
            Directory.CreateDirectory(Folder);
            foreach (var building in Buildings.Concat(ResourceModels)) BuildPrefab(building);
            AssetDatabase.SaveAssets();
        }

        static void BuildPrefab(Building building)
        {
            string art = $"{ArtRoot}/{building.Name}";
            string texturePath = $"{art}/T_{building.Name}_BaseColor.png";
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                // la cámara mira el edificio en oblicuo: filtrado anisótropo y compresión de calidad para que las paredes no se vean borrosas
                importer.sRGBTexture = true; importer.maxTextureSize = 4096; importer.mipmapEnabled = true; importer.anisoLevel = 16;
                // un edificio ocupa en pantalla mucho menos que su textura: se lee un nivel de detalle algo más fino del que tocaría (más nítido;
                // el suavizado de bordes del render y el filtro anisótropo contienen el centelleo) y los niveles se reducen con un filtro que no emborrona
                importer.mipMapBias = -.6f; importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.compressionQuality = 100; importer.SaveAndReimport();
            }
            string materialPath = $"{Folder}/M_{building.Name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            // el sombreador del terreno en modo edificio: es Lit, y además la base toma la tierra pisada de su casilla (CampaignTileBuildings le
            // dice la altura de la base y el terreno al colocarlo) con el mismo ruido que el suelo de alrededor
            var shader = Shader.Find("Fulgor/Terreno") ?? throw new InvalidOperationException("Falta el sombreador Fulgor/Terreno.");
            CampaignTerrainReliefBuilder.AssertCompiles(shader);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            if (material.shader != shader) material.shader = shader;
            CampaignTerrainReliefBuilder.ResetTerrainDefaults(material, shader);
            material.SetFloat("_FulgorMontana", 0f); material.SetFloat("_FulgorEdificio", 1f);
            material.EnableKeyword("_FULGOR_EDIFICIO"); material.DisableKeyword("_FULGOR_MONTANA");
            material.SetTexture("_FulgorNoise", AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath) ?? throw new FileNotFoundException("Falta el ruido del terreno", NoisePath));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .12f);
            // los edificios hechos con TRELLIS traen además un mapa de normales horneado desde la malla alta
            string normalPath = $"{art}/T_{building.Name}_Normal.png";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
            {
                normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.sRGBTexture = false;
                normalImporter.maxTextureSize = 2048; normalImporter.mipmapEnabled = true; normalImporter.anisoLevel = 8;
                normalImporter.textureCompression = TextureImporterCompression.Compressed; normalImporter.SaveAndReimport();
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                // suave: el color de TRELLIS ya trae la sombra de las tejas pintada; con 1 el relieve la duplica y las juntas salen negras
                material.SetFloat("_BumpScale", .35f); material.EnableKeyword("_NORMALMAP");
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{art}/SM_{building.Name}.fbx") ?? throw new FileNotFoundException("Falta el FBX de " + building.Name);
            var root = new GameObject("PF_" + building.Name);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                // el FBX sale con la puerta hacia +Z: se gira 180° para que sin giro mire al sur, como espera CampaignTileBuildings
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * instance.transform.localRotation;
                var filter = instance.GetComponentsInChildren<MeshFilter>().Single();
                filter.GetComponent<MeshRenderer>().sharedMaterial = material;
                // la huella llega al 80 % del radio seguro de una casilla; la altura sale proporcional
                float radius = filter.sharedMesh.vertices
                    .Select(v => root.transform.InverseTransformPoint(filter.transform.TransformPoint(v)))
                    .Max(p => new Vector2(p.x, p.z).magnitude);
                instance.transform.localScale *= VisualScaleContract.PropSafeRadius * .8f * building.Size / radius;
                AddFootprint(root, filter, building);
                PrefabUtility.SaveAsPrefabAsset(root, building.PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>
        /// Huella del edificio, desde la malla ya escalada: el contorno de lo que toca el suelo (el primer centímetro desde abajo) ajustado a un
        /// rectángulo de esquinas redondeadas, y el alto de la losa de la base (la altura con más superficie hacia arriba en el cuarto inferior).
        /// Medido en las trece de la edad 01: huellas de 0,33–0,44 de medio lado (la empalizada, 0,53 × 0,16; el pozo, 0,2), losas de 1–2 cm.
        /// </summary>
        static void AddFootprint(GameObject root, MeshFilter filter, Building building)
        {
            var mesh = filter.sharedMesh;
            var toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var points = mesh.vertices.Select(v => toRoot.MultiplyPoint3x4(v)).ToArray();
            float bottom = points.Min(p => p.y), top = points.Max(p => p.y);
            // lo que toca el suelo: los primeros 4 cm desde abajo (la base de TRELLIS no siempre es plana: el huerto tiene los surcos a 3 cm)
            var floor = points.Where(p => p.y < bottom + .04f).Select(p => new Vector2(p.x, p.z)).ToArray();
            static float Percentile(IEnumerable<float> values, float q)
            {
                var sorted = values.OrderBy(v => v).ToArray();
                return sorted[Mathf.Clamp(Mathf.RoundToInt(q * (sorted.Length - 1)), 0, sorted.Length - 1)];
            }
            // los extremos sin el medio por ciento de cada lado: un vértice suelto no agranda la huella
            float minX = Percentile(floor.Select(p => p.x), .005f), maxX = Percentile(floor.Select(p => p.x), .995f);
            float minZ = Percentile(floor.Select(p => p.y), .005f), maxZ = Percentile(floor.Select(p => p.y), .995f);
            var centre = new Vector2((minX + maxX) * .5f, (minZ + maxZ) * .5f);
            var half = new Vector2((maxX - minX) * .5f, (maxZ - minZ) * .5f);
            // esquinas: en un rectángulo los puntos llegan a |x|/hx + |z|/hz = 2; en un óvalo, a √2
            float reach = Percentile(floor.Select(p => Mathf.Abs(p.x - centre.x) / half.x + Mathf.Abs(p.y - centre.y) / half.y), .98f);
            float roundness = Mathf.Clamp01((2f - reach) / (2f - Mathf.Sqrt(2f)));
            float corner = roundness * Mathf.Min(half.x, half.y);

            // superficie hacia arriba por tramos de 2 mm de altura en el cuarto inferior: la losa es el tramo con más
            var triangles = mesh.triangles; var area = new SortedDictionary<int, float>(); float lowQuarter = bottom + (top - bottom) * .25f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = points[triangles[i]]; var b = points[triangles[i + 1]]; var c = points[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a); float twice = normal.magnitude;
                if (twice < 1e-12f || normal.y / twice < .9f) continue;
                float height = (a.y + b.y + c.y) / 3f; if (height > lowQuarter) continue;
                int bin = Mathf.FloorToInt((height - bottom) / .002f);
                area[bin] = (area.TryGetValue(bin, out var sum) ? sum : 0f) + normal.y * .5f;
            }
            float slab = area.Count == 0 ? 0f : (area.OrderByDescending(p => p.Value).First().Key + .5f) * .002f;
            var footprint = root.AddComponent<CampaignBuildingFootprint>();
            footprint.Set(centre, half, corner, bottom, slab);
            float footprintArea = 4f * half.x * half.y - (4f - Mathf.PI) * corner * corner;
            Debug.Log($"Huella de {building.Label}: centro ({centre.x:0.000}, {centre.y:0.000}), medio ancho {half.x:0.000} × {half.y:0.000}, " +
                      $"esquinas {corner:0.000} (redondez {roundness:0.00}), abajo {bottom:0.000}, alto {top - bottom:0.000}, losa {slab:0.000}; " +
                      "superficie hacia arriba por altura (cm: % de la huella): " +
                      string.Join(", ", area.OrderByDescending(p => p.Value).Take(5).Select(p => $"{(p.Key + .5f) * .2f:0.0}: {100f * p.Value / footprintArea:0}")));
        }

        [MenuItem("Fulgor/Arte/Colocar edificios de casilla en la escena de Conquista")]
        public static void PlaceInConquest() => Run(PlaceAll);

        static void PlaceAll()
        {
            var scene = EditorSceneManager.OpenScene(ConquestScene);
            var board = UnityEngine.Object.FindFirstObjectByType<CampaignBoardPreview>() ?? throw new InvalidOperationException("La escena no tiene tablero.");
            var buildings = board.GetComponent<CampaignTileBuildings>() ?? board.gameObject.AddComponent<CampaignTileBuildings>();
            var serialized = new SerializedObject(buildings);
            var entries = serialized.FindProperty("entries");
            entries.arraySize = Buildings.Length;
            for (int i = 0; i < Buildings.Length; i++)
            {
                var building = Buildings[i];
                var cell = new HexCoordinates(building.Cell.x, building.Cell.y);
                // Para probar vale una casilla en niebla, pero en partida el edificio no se dibuja hasta explorarla (CampaignTileBuildings), así que no se vería en las capturas.
                if (!StartVisionSources.Any(s => new HexCoordinates(s.x, s.y).DistanceTo(cell) <= Fulgor.Core.Campaign.CampaignFogState.VisionRadius))
                    Debug.LogWarning($"{building.Label} en {building.Cell} cae en niebla de guerra al empezar: no se verá hasta explorar la casilla.");
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("label").stringValue = building.Label;
                entry.FindPropertyRelative("cell").vector2IntValue = building.Cell;
                entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(building.PrefabPath);
                float door = building.DoorMinute >= 0 ? building.DoorMinute : TowardFortress(building.Cell);
                entry.FindPropertyRelative("doorMinute").floatValue = door;
                Debug.Log($"{building.Label} en {building.Cell}: puerta a {door:0.0} del minutero");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Escribe en el registro el terreno de las casillas a menos de tres de la fortaleza, para elegir dónde probar cada edificio.</summary>
        [MenuItem("Fulgor/Arte/Listar terreno junto a la fortaleza")]
        public static void ListTerrainAroundFortress()
        {
            try
            {
                EditorSceneManager.OpenScene(ConquestScene);
                var board = UnityEngine.Object.FindFirstObjectByType<CampaignBoardPreview>() ?? throw new InvalidOperationException("La escena no tiene tablero.");
                var scenario = new SerializedObject(board).FindProperty("scenario").objectReferenceValue as CampaignScenarioDefinition
                    ?? throw new InvalidOperationException("El tablero no tiene escenario.");
                var map = ProceduralMapGenerator.Generate(scenario.Width, scenario.Height, scenario.Seed);
                var terrain = map.Cells.ToDictionary(c => c.Coordinates);
                var deposits = ResourceDepositLayout.For(map);
                string Deposit(HexCoordinates c) => deposits.TryGetValue(c, out var kind) ? $" con yacimiento de {(kind == ResourceDepositKind.Gold ? "oro" : "piedra")}" : "";
                var center = new HexCoordinates(FortressCell.x, FortressCell.y);
                var lines = new System.Text.StringBuilder("Terreno junto a la fortaleza:\n");
                foreach (var cell in center.CellsInRadius(3).OrderBy(c => center.DistanceTo(c)).ThenBy(c => c.Q).ThenBy(c => c.R))
                    lines.AppendLine(terrain.TryGetValue(cell, out var data)
                        ? $"({cell.Q},{cell.R}) a {center.DistanceTo(cell)}: {data.Terrain}{Deposit(cell)}"
                        : $"({cell.Q},{cell.R}) a {center.DistanceTo(cell)}: fuera del mapa");
                Debug.Log(lines.ToString());
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Minuto del minutero que apunta desde la casilla hacia la fortaleza.</summary>
        static float TowardFortress(Vector2Int cell)
        {
            var from = HexWorldLayout.ToWorld(new HexCoordinates(cell.x, cell.y));
            var to = HexWorldLayout.ToWorld(new HexCoordinates(FortressCell.x, FortressCell.y));
            float degrees = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg; // 0 = norte, positivo hacia el este
            return Mathf.Repeat(degrees / 6f, 60f);
        }

        /// <summary>Gira la fortaleza del juego alrededor de la vertical: negativo es contra las agujas del reloj (40 → 33 son −42°).</summary>
        public static void RotateFortress()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                int at = Array.IndexOf(args, "-fulgorGiro");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta -fulgorGiro <grados>.");
                float degrees = float.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture);
                var root = PrefabUtility.LoadPrefabContents(FortressPrefab);
                try
                {
                    var model = root.transform.GetChild(0);
                    model.localRotation = Quaternion.Euler(0f, degrees, 0f) * model.localRotation;
                    PrefabUtility.SaveAsPrefabAsset(root, FortressPrefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
