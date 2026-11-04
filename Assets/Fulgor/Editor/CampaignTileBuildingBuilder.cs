using System;
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
        static readonly Vector2Int FortressCell = new(13, 8);

        /// <summary>Un edificio de casilla: su carpeta de arte (SM_/T_ con su nombre), la casilla de prueba y hacia dónde mira la puerta.</summary>
        readonly struct Building
        {
            public readonly string Name, Label;
            public readonly Vector2Int Cell;
            public readonly float DoorMinute; // minutero: 0 norte, 15 este, 30 sur, 45 oeste; negativo = mirar a la fortaleza
            public Building(string name, string label, Vector2Int cell, float doorMinute) { Name = name; Label = label; Cell = cell; DoorMinute = doorMinute; }
            public string PrefabPath => $"{Folder}/PF_{Name}.prefab";
        }

        // Al empezar, la niebla de guerra solo deja ver lo que está a CampaignFogState.VisionRadius de la fortaleza y de la hueste que sale con ella
        // (PlayableConquest); cualquier otra casilla se dibuja negra. Los edificios de prueba tienen que caer dentro de lo visible.
        static readonly Vector2Int[] StartVisionSources = { new(13, 8), new(13, 9) };

        // casillas de prueba junto a la fortaleza (13,8), en llanura siempre que quede alguna visible
        static readonly Building[] Buildings =
        {
            new("CentroAprovisionamiento", "Centro de aprovisionamiento", new Vector2Int(12, 9), 25f),
            new("Almacen", "Almacén", new Vector2Int(13, 7), 35f), // de espaldas a la fortaleza
            // hechos con TRELLIS.2 desde los sprites de la edad 01; puerta hacia la fortaleza hasta que el usuario diga otra cosa
            new("ViviendaSencilla", "Vivienda sencilla", new Vector2Int(13, 9), -1f),
            new("Pozo", "Pozo", new Vector2Int(12, 10), -1f),
            new("Lenera", "Leñera", new Vector2Int(11, 9), -1f),
            new("PuestoIntercambio", "Puesto de intercambio", new Vector2Int(13, 10), -1f),
            new("CasaConocimiento", "Casa de conocimiento", new Vector2Int(14, 9), -1f),
            new("CampoMilicias", "Campo de milicias", new Vector2Int(13, 6), -1f),
            new("CanteraSencilla", "Cantera sencilla", new Vector2Int(15, 6), -1f),
            new("AldeaConcejil", "Aldea concejil", new Vector2Int(12, 11), -1f),
            new("PatioMesnadas", "Patio de mesnadas", new Vector2Int(12, 8), -1f), // colina junto a la fortaleza: en llanura no queda casilla visible
            new("Empalizada", "Empalizada", new Vector2Int(11, 10), -1f),
            new("HuertoCampo", "Huerto o campo", new Vector2Int(14, 10), -1f),
        };

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
            foreach (var building in Buildings) BuildPrefab(building);
            AssetDatabase.SaveAssets();
        }

        static void BuildPrefab(Building building)
        {
            string art = $"{ArtRoot}/{building.Name}";
            string texturePath = $"{art}/T_{building.Name}_BaseColor.png";
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.sRGBTexture = true; importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
            }
            string materialPath = $"{Folder}/M_{building.Name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .12f);
            // los edificios hechos con TRELLIS traen además un mapa de normales horneado desde la malla alta
            string normalPath = $"{art}/T_{building.Name}_Normal.png";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
            {
                normalImporter.textureType = TextureImporterType.NormalMap; normalImporter.sRGBTexture = false;
                normalImporter.maxTextureSize = 2048; normalImporter.mipmapEnabled = true;
                normalImporter.textureCompression = TextureImporterCompression.Compressed; normalImporter.SaveAndReimport();
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.SetFloat("_BumpScale", 1f); material.EnableKeyword("_NORMALMAP");
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
                instance.transform.localScale *= VisualScaleContract.PropSafeRadius * .8f / radius;
                PrefabUtility.SaveAsPrefabAsset(root, building.PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
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
                var center = new HexCoordinates(FortressCell.x, FortressCell.y);
                var lines = new System.Text.StringBuilder("Terreno junto a la fortaleza:\n");
                foreach (var cell in center.CellsInRadius(3).OrderBy(c => center.DistanceTo(c)).ThenBy(c => c.Q).ThenBy(c => c.R))
                    lines.AppendLine(terrain.TryGetValue(cell, out var data)
                        ? $"({cell.Q},{cell.R}) a {center.DistanceTo(cell)}: {data.Terrain}{(data.HasGold ? " con oro" : "")}"
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
