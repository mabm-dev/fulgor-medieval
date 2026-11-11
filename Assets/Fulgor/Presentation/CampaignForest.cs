using System;
using System.Collections.Generic;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>
    /// Árboles y matas del mapa de campaña. Se plantan sobre el relieve continuo, no por casilla: una rejilla de todo el tablero, movida al azar,
    /// decide los sitios, y cada sitio lleva árbol según cuánto bosque hay allí (<see cref="CampaignMountainRelief.SurfaceAt"/>), así que el
    /// borde del bosque es irregular y hay algún árbol suelto en la llanura. Los de cada casilla se funden en una malla hija de esa casilla, que la
    /// niebla de guerra oculta y oscurece con ella. Los modelos salen de <c>Resources/Vegetacion/PF_Arboles</c> (Tools/Blender/arboles_fulgor.py).
    /// </summary>
    public static class CampaignForest
    {
        public const string PrefabPath = "Vegetacion/PF_Arboles";
        /// <summary>Nombre del objeto hijo de cada casilla que lleva sus árboles.</summary>
        public const string GroupName = "Árboles";
        /// <summary>Lado de la rejilla de sitios, en radios de casilla.</summary>
        public const float Spacing = .2f;
        /// <summary>Claro en el centro de cada casilla, donde se plantan las huestes; y el que deja un edificio o un asentamiento.</summary>
        public const float CentreClearing = .36f, BuiltClearing = .86f;

        private sealed class Kinds
        {
            public readonly List<Mesh> HolmOaks = new(), Oaks = new(), Pines = new(), Shrubs = new();
            public Mesh DryOak;
            public Material Material;
            // el FBX trae cada malla con su escala y su giro en el objeto: hay que aplicarlos al fundirla
            public readonly Dictionary<Mesh, Matrix4x4> Pose = new();
        }

        /// <summary>Un árbol plantado: sitio en el tablero, giro, tamaño y cuál.</summary>
        public readonly struct Tree
        {
            public readonly Vector2 Position; public readonly float Yaw, Scale; public readonly int Kind, Variant;
            public Tree(Vector2 position, float yaw, float scale, int kind, int variant) { Position = position; Yaw = yaw; Scale = scale; Kind = kind; Variant = variant; }
        }
        public const int HolmOak = 0, Oak = 1, Pine = 2, Shrub = 3, DryOak = 4;

        /// <summary>
        /// Dónde van los árboles de una casilla: solo depende del relieve y de la casilla, así que el mismo mapa da siempre el mismo bosque.
        /// </summary>
        public static List<Tree> Layout(CampaignMountainRelief relief, HexCoordinates cell, bool built) =>
            Layout(relief, cell, built ? BuiltClearing : CentreClearing, built);

        /// <summary>Como el anterior, con el radio del claro: un asentamiento ocupa toda su casilla.</summary>
        public static List<Tree> Layout(CampaignMountainRelief relief, HexCoordinates cell, float clearing, bool built)
        {
            var trees = new List<Tree>();
            if (relief == null || !relief.IsGenerated(cell)) return trees;
            var centre = relief.Centre(cell);
            int x0 = Mathf.FloorToInt((centre.x - 1f) / Spacing), x1 = Mathf.CeilToInt((centre.x + 1f) / Spacing);
            int z0 = Mathf.FloorToInt((centre.y - 1f) / Spacing), z1 = Mathf.CeilToInt((centre.y + 1f) / Spacing);
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                var p = new Vector2((x + .15f + .7f * Hash(x, z, 1)) * Spacing, (z + .15f + .7f * Hash(x, z, 2)) * Spacing);
                if (!HexWorldLayout.FromWorld(new Vector3(p.x, 0f, p.y)).Equals(cell)) continue;
                var surface = relief.SurfaceAt(p);
                // ni en la roca de la montaña ni en la orilla
                if (surface.r > .56f || surface.a > .45f) continue;
                float forest = surface.b, chance = Hash(x, z, 3);
                float away = (p - centre).magnitude;
                bool shrub = false;
                if (chance < forest * .30f + .01f) { if (away < clearing) continue; }
                else if (chance > 1f - (.035f + forest * .06f)) { if (away < (built ? clearing : clearing * .8f)) continue; shrub = true; }
                else continue;
                float yaw = Hash(x, z, 4) * 360f, pick = Hash(x, z, 5);
                if (shrub) { trees.Add(new Tree(p, yaw, Mathf.Lerp(.8f, 1.3f, Hash(x, z, 6)), Shrub, Mathf.FloorToInt(pick * 97f))); continue; }
                // rodales: la especie cambia despacio por el mapa; en lo alto, más encina y pino
                float stand = Patch(p.x * .28f, p.y * .28f) + (surface.g > .08f ? .12f : 0f) + (Hash(x, z, 7) - .5f) * .3f;
                int kind = Hash(x, z, 8) < .025f ? DryOak : stand < .42f ? Oak : stand < .68f ? HolmOak : Pine;
                trees.Add(new Tree(p, yaw, Mathf.Lerp(.76f, 1.1f, Hash(x, z, 6)), kind, Mathf.FloorToInt(pick * 97f)));
            }
            return trees;
        }

        /// <summary>Planta los árboles de las casillas dadas; devuelve cuántos. Las mallas creadas se apuntan en <paramref name="owned"/>.</summary>
        public static int Plant(IEnumerable<HexTileView> tiles, CampaignMountainRelief relief, IReadOnlyDictionary<HexCoordinates, float> clearings, List<Mesh> owned)
        {
            var kinds = Load();
            if (kinds == null || relief == null || !relief.IsContinuous) return 0;
            int planted = 0;
            var parts = new List<CombineInstance>();
            foreach (var tile in tiles)
            {
                var cell = tile.Cell.Coordinates;
                if (tile.Cell.Terrain == TerrainType.Water || tile.Cell.Terrain == TerrainType.Mountain) continue;
                var trees = clearings != null && clearings.TryGetValue(cell, out var clearing) ? Layout(relief, cell, clearing, true) : Layout(relief, cell, false);
                if (trees.Count == 0) continue;
                var centre = relief.Centre(cell);
                parts.Clear();
                foreach (var tree in trees)
                {
                    var mesh = Pick(kinds, tree);
                    if (mesh == null) continue;
                    // hundido un centímetro: en cuesta, el pie del tronco no asoma
                    var position = new Vector3(tree.Position.x - centre.x, relief.Height(tree.Position) - .012f, tree.Position.y - centre.y);
                    parts.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, Quaternion.Euler(0f, tree.Yaw, 0f), Vector3.one * tree.Scale) * kinds.Pose[mesh] });
                }
                if (parts.Count == 0) continue;
                var combined = new Mesh { name = "Árboles " + cell.Key, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(parts.ToArray(), true, true, false);
                combined.RecalculateBounds();
                owned?.Add(combined);
                var child = new GameObject(GroupName, typeof(MeshFilter), typeof(MeshRenderer)) { layer = tile.gameObject.layer };
                child.transform.SetParent(tile.transform, false);
                // la casilla puede estar girada (las de piezas fijas): los árboles van en coordenadas del tablero
                child.transform.rotation = tile.transform.parent != null ? tile.transform.parent.rotation : Quaternion.identity;
                child.GetComponent<MeshFilter>().sharedMesh = combined;
                var renderer = child.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = kinds.Material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                planted += parts.Count;
            }
            return planted;
        }

        private static Mesh Pick(Kinds kinds, Tree tree)
        {
            if (tree.Kind == DryOak) return kinds.DryOak ?? Of(kinds.Oaks, tree.Variant);
            return Of(tree.Kind switch { HolmOak => kinds.HolmOaks, Oak => kinds.Oaks, Pine => kinds.Pines, _ => kinds.Shrubs }, tree.Variant);
        }

        private static Mesh Of(List<Mesh> list, int variant) => list.Count == 0 ? null : list[variant % list.Count];

        private static Kinds Load()
        {
            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null) return null;
            var kinds = new Kinds();
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                string name = filter.name;
                kinds.Pose[mesh] = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                if (name.StartsWith("Arbol_Roble_Seco", StringComparison.Ordinal)) kinds.DryOak = mesh;
                else if (name.StartsWith("Arbol_Encina", StringComparison.Ordinal)) kinds.HolmOaks.Add(mesh);
                else if (name.StartsWith("Arbol_Roble", StringComparison.Ordinal)) kinds.Oaks.Add(mesh);
                else if (name.StartsWith("Arbol_Pino", StringComparison.Ordinal)) kinds.Pines.Add(mesh);
                else if (name.StartsWith("Mata", StringComparison.Ordinal)) kinds.Shrubs.Add(mesh);
                if (kinds.Material == null) kinds.Material = filter.GetComponent<MeshRenderer>()?.sharedMaterial;
            }
            return kinds.Material == null ? null : kinds;
        }

        private static float Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + salt * 2246822519u);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / 16777216f;
            }
        }

        // ruido de valor suave, de 0 a 1
        private static float Patch(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy, 9), Hash(ix + 1, iy, 9), ux), Mathf.Lerp(Hash(ix, iy + 1, 9), Hash(ix + 1, iy + 1, 9), ux), uy);
        }
    }
}
