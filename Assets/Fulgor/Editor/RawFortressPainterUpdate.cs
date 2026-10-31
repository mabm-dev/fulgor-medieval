using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class RawFortressPainterUpdate
    {
        const string Request = "Temp/FulgorPainterUpdate.request";
        const string CorrectedRequest = "Temp/FulgorCorrectedFortress.request";
        const string Art = "Assets/Fulgor/Art/RawFortress";
        const string Prefix = Art + "/Painter4K/SM_Fortaleza_completa_bruta_Material_0_";
        static bool capturing;
        static int frames;
        static bool correctedCapture;
        static RawFortressPainterUpdate() { EditorApplication.update += Tick; }
        [MenuItem("Fulgor/Arte/Actualizar fortaleza desde Painter 4K")]
        public static void RequestUpdate() { Directory.CreateDirectory("Temp"); File.WriteAllText(Request, "update"); }
        [MenuItem("Fulgor/Arte/Sustituir por fortaleza corregida con Painter")]
        public static void RequestCorrected() { Directory.CreateDirectory("Temp"); File.WriteAllText(CorrectedRequest, "update"); }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!capturing && File.Exists(CorrectedRequest))
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                File.Delete(CorrectedRequest);
                try { ReplaceGeometry(); correctedCapture = true; capturing = true; frames = 0; }
                catch (Exception error) { Debug.LogException(error); WriteStatus("FAILED correction: " + error); }
            }
            else if (!capturing && File.Exists(Request))
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                File.Delete(Request);
                try { Apply(); correctedCapture = false; capturing = true; frames = 0; }
                catch (Exception error) { Debug.LogException(error); WriteStatus("FAILED: " + error); }
            }
            else if (capturing && ++frames > 12)
            {
                capturing = false;
                try { Capture(); }
                catch (Exception error) { Debug.LogException(error); WriteStatus("FAILED: " + error); }
            }
        }
        static void ReplaceGeometry()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Fulgor/Scenes/ArtReview/RawFortressReview.unity")
                throw new InvalidOperationException("Abre RawFortressReview para sustituir la fortaleza de revision.");
            string path = Art + "/SM_Fortaleza_completa_corregida.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation = false; importer.importBlendShapes = false;
            importer.addCollider = false; importer.bakeAxisConversion = true;
            importer.globalScale = 1; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/M_Fortaleza_completa_bruta.mat");
            if (material == null || material.GetTexture("_BaseMap") == null ||
                !AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")).Contains("/Painter4K/"))
                throw new InvalidOperationException("Falta el material de Painter; no se sustituye la fortaleza.");
            var old = scene.GetRootGameObjects().Single(o => o.name.StartsWith("Fortaleza completa"));
            var oldRenderers = old.GetComponentsInChildren<MeshRenderer>();
            var oldBounds = oldRenderers[0].bounds;
            foreach (var renderer in oldRenderers) oldBounds.Encapsulate(renderer.bounds);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            try
            {
                replacement.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
                replacement.transform.localScale = old.transform.localScale;
                replacement.name = "Fortaleza completa corregida - Painter";
                var renderers = replacement.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (Vector3.Distance(bounds.size, oldBounds.size) > oldBounds.size.magnitude * .08f)
                    throw new InvalidOperationException("Escala del FBX corregido inesperada: " + bounds.size + "; anterior: " + oldBounds.size);
                long triangles = replacement.GetComponentsInChildren<MeshFilter>().Sum(f =>
                    Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(i => (long)f.sharedMesh.GetIndexCount(i) / 3));
                if (triangles != 1499152) throw new InvalidOperationException("Numero de triangulos inesperado: " + triangles);
                Undo.RegisterCreatedObjectUndo(replacement, "Sustituir fortaleza por corregida");
                Undo.DestroyObjectImmediate(old);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = replacement;
                Directory.CreateDirectory("Builds/RawFortressReview");
                File.WriteAllText("Builds/RawFortressReview/CorrectedFortress_status.txt", "OK scene saved; corrected FBX; Painter material retained; triangles " + triangles + "; bounds " + bounds.size);
                Debug.Log("CORRECTED_FORTRESS_REPLACED " + triangles);
            }
            catch { if (replacement != null) Object.DestroyImmediate(replacement); throw; }
        }
        static void Apply()
        {
            foreach (string suffix in new[] { "AlbedoTransparency", "Normal", "MetallicSmoothness" })
            {
                string path = Prefix + suffix + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = suffix == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = suffix == "AlbedoTransparency";
                importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false; importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8;
                importer.ignoreMipmapLimit = true;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Standalone", overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.RGBA32, textureCompression = TextureImporterCompression.Uncompressed });
                importer.SaveAndReimport();
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/M_Fortaleza_completa_bruta.mat");
            if (material == null) throw new InvalidOperationException("Falta material de revision");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Prefix + "AlbedoTransparency.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Prefix + "Normal.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Prefix + "MetallicSmoothness.png"));
            material.SetFloat("_Smoothness", 1); material.SetFloat("_BumpScale", 1);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            QualitySettings.globalTextureMipmapLimit = 0; QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline) { pipeline.renderScale = 1; pipeline.msaaSampleCount = 4; }
            if (Camera.main != null)
            {
                var data = Camera.main.GetUniversalAdditionalCameraData();
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
            if (!EditorApplication.isPlaying && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("RawFortressReview.unity"))
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static void Capture()
        {
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Falta camara de revision");
            string output = "Builds/RawFortressReview"; Directory.CreateDirectory(output);
            var target = new RenderTexture(3200, 2000, 24); target.Create();
            var previous = RenderTexture.active; float aspect = camera.aspect;
            var image = new Texture2D(3200, 2000, TextureFormat.RGBA32, false);
            try
            {
                camera.aspect = 1.6f;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 3200, 2000), 0, 0); image.Apply();
                File.WriteAllBytes(output + (correctedCapture ? "/Fortaleza_Corregida_Painter_Unity.png" : "/Fortaleza_Painter4K_Unity.png"), image.EncodeToPNG());
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Prefix + "AlbedoTransparency.png");
                WriteStatus("OK Painter color " + tex.width + "x" + tex.height + ", format " + tex.format + ", screenshot 3200x2000");
                Debug.Log("PAINTER_4K_REVIEW_OK");
            }
            finally { camera.aspect = aspect; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
        static void WriteStatus(string message) { Directory.CreateDirectory("Builds/RawFortressReview"); File.WriteAllText("Builds/RawFortressReview/Painter4K_status.txt", message); }
    }
}
