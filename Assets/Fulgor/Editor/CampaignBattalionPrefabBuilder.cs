using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fulgor.Editor
{
    /// <summary>Monta el grupo de soldados que sustituye a la bandera de las huestes en el mapa de campaña.</summary>
    public static class CampaignBattalionPrefabBuilder
    {
        const string Folder = "Assets/Fulgor/Resources/Hueste", Art = "Assets/Fulgor/Art/Units/SoldadosCastilla";
        public const string PrefabPath = Folder + "/PF_Batallon.prefab";
        // El FBX sale mirando a +Z, que es el rumbo cero de CampaignArmyMarker.
        // El grupo cabe en la peana de la hueste (radio .325) con un pequeño margen.
        const float FootprintRadius = .3f;

        [MenuItem("Fulgor/Arte/Montar batallón de hueste")]
        public static void Build()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string texturePath = Art + "/T_SoldadosCastilla_BaseColor.png";
                if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
                {
                    importer.sRGBTexture = true; importer.maxTextureSize = 2048; importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
                }
                string materialPath = Folder + "/M_SoldadosCastilla.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0f); material.SetFloat("_Smoothness", .12f);
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);

                string modelPath = Art + "/SM_SoldadosCastilla.fbx";
                if (AssetImporter.GetAtPath(modelPath) is ModelImporter modelImporter)
                {
                    modelImporter.importAnimation = true; modelImporter.animationType = ModelImporterAnimationType.Generic;
                    modelImporter.SaveAndReimport();
                }
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) ?? throw new FileNotFoundException("Falta el FBX de los soldados.");
                var march = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"))
                    ?? throw new FileNotFoundException("El FBX de los soldados no trae la marcha.");
                var root = new GameObject("Batallon");
                try
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    instance.name = "Soldados";
                    var skins = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var skin in skins) skin.sharedMaterial = material;
                    // el radio se mide sobre la pose neutra de cada malla
                    float radius = skins
                        .SelectMany(skin => skin.sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(skin.transform.TransformPoint(v))))
                        .Max(p => new Vector2(p.x, p.z).magnitude);
                    instance.transform.localScale *= FootprintRadius / radius;
                    var walk = instance.AddComponent<Fulgor.Presentation.Map.CampaignSoldierWalk>(); // trae su Animator
                    walk.Clip = march;
                    var animator = walk.GetComponent<Animator>();
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                TerritoryMaterial();
                ContactMaterials();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Material translúcido del sombreado de territorio; vive en Resources para que el build conserve su variante transparente.</summary>
        static void TerritoryMaterial()
        {
            var material = UnlitAsset("M_Territorio");
            Transparent(material);
            material.SetColor("_BaseColor", new Color(1, 1, 1, .22f));
            EditorUtility.SetDirty(material);
        }

        /// <summary>Sombra de contacto bajo la hueste y material liso de la insignia del equipo.</summary>
        static void ContactMaterials()
        {
            string texturePath = Folder + "/T_Sombra.png";
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + .5f - size / 2f, y + .5f - size / 2f).magnitude / (size / 2f);
                    float a = 1f - Smooth(.15f, 1f, r);
                    pixels[y * size + x] = new Color(0, 0, 0, a);
                }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            var shadow = UnlitAsset("M_Sombra");
            Transparent(shadow);
            shadow.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            shadow.SetColor("_BaseColor", new Color(0, 0, 0, .6f));
            EditorUtility.SetDirty(shadow);
            BadgeMaterials();
        }

        const int BadgeWidth = 128, BadgeHeight = 160;

        /// <summary>1 dentro del escudo, 0 fuera, con borde suavizado; x e y en [-1, 1] y la punta abajo.</summary>
        static float Shield(float x, float y)
        {
            float half = y > .15f ? 1f : Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((.15f - y) / 1.15f, 2f)));
            if (y > .7f) { float k = (y - .7f) / .3f; half = Mathf.Min(half, 1f - .3f * k * k); }
            float s = Mathf.Min(half - Mathf.Abs(x), 1f - y);
            return Smooth(0f, .07f, s);
        }

        /// <summary>Escudete de la insignia en tres capas: marco dorado, campo perlado (se tiñe con el color del equipo) y brillo.</summary>
        static void BadgeMaterials()
        {
            var frame = new Color32[BadgeWidth * BadgeHeight];
            var field = new Color32[BadgeWidth * BadgeHeight];
            var gloss = new Color32[BadgeWidth * BadgeHeight];
            for (int py = 0; py < BadgeHeight; py++)
                for (int px = 0; px < BadgeWidth; px++)
                {
                    float x = (px + .5f) / BadgeWidth * 2f - 1f, y = (py + .5f) / BadgeHeight * 2f - 1f;
                    float outer = Shield(x, y);
                    float inner = Shield(x / .74f, (y + .03f) / .80f);
                    int i = py * BadgeWidth + px;
                    // marco: oro con degradado en diagonal, filo oscuro por fuera y reflejo arriba a la izquierda
                    float t = Mathf.Clamp01(.5f + .35f * (y - x));
                    var gold = Color.Lerp(new Color(.50f, .33f, .08f), new Color(1f, .88f, .50f), t);
                    float rim = Smooth(.0f, .16f, Mathf.Min(1f - y, 1f - Mathf.Abs(x)) );
                    gold = Color.Lerp(gold * .55f, gold, Mathf.Clamp01(rim * 3f + .3f));
                    frame[i] = new Color(gold.r, gold.g, gold.b, outer * (1f - inner));
                    // campo: gris perlado, más claro hacia el reflejo; el color del equipo lo multiplica
                    float glare = Mathf.Clamp01(1f - new Vector2(x + .3f, y - .35f).magnitude / 1.1f);
                    float v = Mathf.Lerp(.66f, 1f, glare * glare) * Mathf.Lerp(.84f, 1f, Mathf.Clamp01(y + 1f));
                    field[i] = new Color(v, v, v, inner);
                    // brillo: media luna blanca arriba a la izquierda sobre el campo
                    float spot = Mathf.Clamp01(1f - new Vector2((x + .38f) * .9f, (y - .42f) * 1.4f).magnitude / .55f);
                    float cut = Mathf.Clamp01(1f - new Vector2((x + .12f) * .9f, (y - .30f) * 1.4f).magnitude / .55f);
                    gloss[i] = new Color(1, 1, 1, inner * Mathf.Clamp01(spot - cut * .9f) * .85f);
                }
            var frameTexture = BadgeTexture("T_InsigniaMarco", frame);
            var fieldTexture = BadgeTexture("T_InsigniaCampo", field);
            var glossTexture = BadgeTexture("T_InsigniaBrillo", gloss);
            // las tres capas son translúcidas (bordes suavizados) y se pintan en orden
            var frameMaterial = UnlitAsset("M_InsigniaMarco"); Transparent(frameMaterial); frameMaterial.SetTexture("_BaseMap", frameTexture); frameMaterial.SetColor("_BaseColor", Color.white); frameMaterial.renderQueue = 3000;
            var fieldMaterial = UnlitAsset("M_Insignia"); Transparent(fieldMaterial); fieldMaterial.SetTexture("_BaseMap", fieldTexture); fieldMaterial.SetColor("_BaseColor", new Color(.08f, .38f, .88f)); fieldMaterial.renderQueue = 3001;
            var glossMaterial = UnlitAsset("M_InsigniaBrillo"); Transparent(glossMaterial); glossMaterial.SetTexture("_BaseMap", glossTexture); glossMaterial.SetColor("_BaseColor", Color.white); glossMaterial.renderQueue = 3002;
            foreach (var m in new[] { frameMaterial, fieldMaterial, glossMaterial }) EditorUtility.SetDirty(m);
        }

        static Texture2D BadgeTexture(string name, Color32[] pixels)
        {
            string path = Folder + "/" + name + ".png";
            var texture = new Texture2D(BadgeWidth, BadgeHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Paso suave: 0 en a, 1 en b (Mathf.SmoothStep interpola entre dos valores y no sirve como borde).</summary>
        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static Material UnlitAsset(string name)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, path); }
            return material;
        }

        static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
