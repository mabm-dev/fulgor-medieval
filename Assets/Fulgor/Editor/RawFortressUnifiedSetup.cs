using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    /// <summary>
    /// Sustituye la fortaleza de revisión por la unificada: malla corregida con el Texture Set de
    /// Painter más las piezas nuevas (almenas, matacanes, tapiz, puente y bandera central),
    /// generadas por Tools/Blender/unificar_fortaleza.py.
    /// </summary>
    public static class RawFortressUnifiedSetup
    {
        const string Art = "Assets/Fulgor/Art/RawFortress";
        const string Tex = Art + "/Unificada";
        const string Fbx = Art + "/SM_Fortaleza_unificada.fbx";
        const string Corrected = Art + "/SM_Fortaleza_completa_corregida.fbx";
        const string Scene = "Assets/Fulgor/Scenes/ArtReview/RawFortressReview.unity";
        const int Triangles = 1385275;
        // Submateriales del FBX por nombre (RANURAS de unificar_fortaleza.py). Unity no conserva el orden
        // de ranuras al crear submeshes, así que se asignan por el nombre importado, nunca por índice.
        // Material_0 → Painter; Hierro → metal; el resto → (textura, suavidad, doble cara, recorte alfa).
        static readonly (string tex, float smooth, bool twoSided, bool clip)[] Pieces =
        {
            ("Almenas", .08f, false, false), ("Matacanes", .08f, false, false), ("Madera", .10f, false, false),
            ("Tapiz", .05f, true, true), ("Pendon", .05f, true, false),
        };

        [MenuItem("Fulgor/Arte/Sustituir por fortaleza unificada (v7 + Painter)")]
        public static void Run()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Scene) throw new InvalidOperationException("Abre RawFortressReview para sustituir la fortaleza de revision.");

            AssetDatabase.ImportAsset(Fbx, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            importer.importAnimation = false; importer.importBlendShapes = false;
            importer.addCollider = false; importer.bakeAxisConversion = true;
            importer.globalScale = 1; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;   // solo para leer los nombres
            importer.SaveAndReimport();

            var painter = AssetDatabase.LoadAssetAtPath<Material>(Art + "/M_Fortaleza_completa_bruta.mat");
            if (painter == null || painter.GetTexture("_BaseMap") == null ||
                !AssetDatabase.GetAssetPath(painter.GetTexture("_BaseMap")).Contains("/Painter4K/"))
                throw new InvalidOperationException("Falta el material de Painter; no se sustituye la fortaleza.");
            var materials = Pieces.ToDictionary(p => p.tex, p => PieceMaterial(p.tex, p.smooth, p.twoSided, p.clip));
            materials["Material_0"] = painter; materials["Hierro"] = IronMaterial();

            var old = scene.GetRootGameObjects().Single(o => o.name.StartsWith("Fortaleza completa"));
            // referencia: la corregida con la transformación de la instancia actual (sirve también al repetir)
            var reference = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Corrected), scene);
            reference.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
            reference.transform.localScale = old.transform.localScale;
            var oldBounds = Bounds(reference);
            Object.DestroyImmediate(reference);
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx), scene);
            try
            {
                replacement.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
                replacement.transform.localScale = old.transform.localScale;
                replacement.name = "Fortaleza completa unificada - Painter";
                var renderer = replacement.GetComponentsInChildren<MeshRenderer>().Single();
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var imported = renderer.sharedMaterials.Select(m => m.name).ToArray();
                if (imported.Length != materials.Count || imported.Distinct().Count() != imported.Length)
                    throw new InvalidOperationException("Submateriales inesperados: " + string.Join(",", imported));
                renderer.sharedMaterials = imported.Select(n => materials.TryGetValue(n, out var m) ? m :
                    throw new InvalidOperationException("Submaterial desconocido en el FBX: " + n)).ToArray();
                int painterSubmesh = Array.IndexOf(imported, "Material_0");
                var bounds = Bounds(replacement);
                // eje a eje: un giro de 90° intercambia X y Z y pasaría una comparación de módulo
                for (int axis = 0; axis < 3; axis++)
                    if (Mathf.Abs(bounds.size[axis] - oldBounds.size[axis]) > oldBounds.size[axis] * .03f)
                        throw new InvalidOperationException("Escala u orientación del FBX unificado inesperada: " + bounds.size + "; anterior: " + oldBounds.size);
                long triangles = mesh.GetIndexCount(painterSubmesh) / 3;
                if (triangles != Triangles) throw new InvalidOperationException("Triangulos de Painter inesperados: " + triangles);

                Undo.RegisterCreatedObjectUndo(replacement, "Sustituir fortaleza por unificada");
                Undo.DestroyObjectImmediate(old);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = replacement;
                Directory.CreateDirectory("Builds/RawFortressReview");
                File.WriteAllText("Builds/RawFortressReview/UnifiedFortress_status.txt",
                    "OK scene saved; unified FBX; submeshes " + string.Join(",", imported.Select(n => n + "=" + materials[n].name)) +
                    "; Painter triangles " + triangles + "; bounds " + bounds.size);
                Debug.Log("UNIFIED_FORTRESS_REPLACED " + triangles);
            }
            catch { if (replacement != null) Object.DestroyImmediate(replacement); throw; }
        }

        /// <summary>Para -batchmode -executeMethod: abre la escena de revisión y sustituye.</summary>
        public static void BatchRun()
        {
            EditorSceneManager.OpenScene(Scene);
            Run();
        }

        /// <summary>Para -batchmode -executeMethod: captura 3200×2000 con la cámara de la escena de revisión.</summary>
        public static void BatchCapture()
        {
            EditorSceneManager.OpenScene(Scene);
            var camera = Camera.main ?? throw new InvalidOperationException("Falta camara de revision");
            var target = new RenderTexture(3200, 2000, 24); target.Create();
            var image = new Texture2D(3200, 2000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active; float aspect = camera.aspect;
            try
            {
                camera.aspect = 1.6f;
                // En batch, el primer render puede salir antes de subir a la GPU texturas recién importadas
                // (la madera, que solo usa el puente, salía negra): se calientan dos renders y se guarda el tercero.
                for (int i = 0; i < 3; i++)
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                        new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 3200, 2000), 0, 0); image.Apply();
                Directory.CreateDirectory("Builds/RawFortressReview");
                File.WriteAllBytes("Builds/RawFortressReview/Fortaleza_Unificada_Painter_Unity.png", image.EncodeToPNG());
                Debug.Log("UNIFIED_FORTRESS_CAPTURED");
            }
            finally { camera.aspect = aspect; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }

        static Bounds Bounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static Material PieceMaterial(string tex, float smooth, bool twoSided, bool clip)
        {
            string texPath = Tex + "/T_Fortaleza_" + tex + "_Albedo.png";
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true;
            ti.alphaSource = clip ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.alphaIsTransparency = clip; ti.mipmapEnabled = true;
            ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 8; ti.maxTextureSize = 2048;
            ti.SaveAndReimport();

            string matPath = Tex + "/M_Fortaleza_" + tex + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", smooth);
            material.SetFloat("_Cull", twoSided ? 0 : 2);
            material.SetFloat("_AlphaClip", clip ? 1 : 0); material.SetFloat("_Cutoff", .5f);
            if (clip) material.EnableKeyword("_ALPHATEST_ON"); else material.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material IronMaterial()
        {
            string matPath = Tex + "/M_Fortaleza_Hierro.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.SetColor("_BaseColor", new Color(.05f, .05f, .05f));
            material.SetFloat("_Metallic", .8f); material.SetFloat("_Smoothness", .4f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
