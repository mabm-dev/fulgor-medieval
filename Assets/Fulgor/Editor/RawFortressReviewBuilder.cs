using System;
using System.IO;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class RawFortressReviewBuilder
    {
        const string Art = "Assets/Fulgor/Art/RawFortress";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/RawFortressReview.unity";
        const string Running = "Fulgor.RawReview.Running";
        static int frames;
        static double start;
        static RawFortressReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Fulgor/Arte/Ver fortaleza completa en bruto")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string name in new[] { "BaseColor", "Normal", "MetallicSmoothness" })
            {
                string path = Art + "/T_Fortaleza_" + name + ".png";
                if (!File.Exists(path)) continue;
                var texture = (TextureImporter)AssetImporter.GetAtPath(path);
                texture.textureType = name == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                texture.sRGBTexture = name == "BaseColor";
                texture.maxTextureSize = 4096;
                texture.SaveAndReimport();
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "/SM_Fortaleza_completa_bruta.fbx");
            importer.importAnimation = false; importer.addCollider = false;
            importer.bakeAxisConversion = true; importer.globalScale = 1;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "M_Fortaleza_completa_bruta";
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/T_Fortaleza_BaseColor.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/T_Fortaleza_Normal.png"));
            material.EnableKeyword("_NORMALMAP");
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/T_Fortaleza_MetallicSmoothness.png");
            if (metallic != null) { material.SetTexture("_MetallicGlossMap", metallic); material.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            material.SetFloat("_Smoothness", 1);
            string materialPath = Art + "/M_Fortaleza_completa_bruta.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing != null) { EditorUtility.CopySerialized(material, existing); Object.DestroyImmediate(material); material = existing; }
            else AssetDatabase.CreateAsset(material, materialPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/SM_Fortaleza_completa_bruta.fbx"));
            model.name = "Fortaleza completa original - revision"; model.transform.localScale = Vector3.one * 20;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale *= 20f / Mathf.Max(bounds.size.x, bounds.size.z);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Debug.Log("RAW_FORTRESS_SIZE " + bounds.size);
            var cameraObject = new GameObject("Camara - boton derecho y rueda"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 200; camera.fieldOfView = 42;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f, .2f, .23f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<RawFortressOrbit>();
            cameraObject.transform.position = new Vector3(22, 20, -28); cameraObject.transform.LookAt(new Vector3(0, 5, 0));
            var lightObject = new GameObject("Luz de revision"); var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 2; light.color = new Color(1, .94f, .85f); light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(45, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .53f, .58f);
            RenderSettings.fog = false;
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            Selection.activeGameObject = model;
        }
        public static void Batch()
        {
            try
            {
                Prepare(); SessionState.SetBool(Running, true); frames = 0; start = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick; EditorApplication.update += Tick; EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (start == 0) start = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - start > 180) { Debug.LogError("Raw fortress capture timeout"); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || ++frames < 30) return;
            try
            {
                string output = Path.GetFullPath("Builds/RawFortressReview"); Directory.CreateDirectory(output);
                var camera = Camera.main;
                var target = new RenderTexture(1600, 1000, 24); target.Create();
                var previous = RenderTexture.active;
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(output, "Fortaleza_bruta_Unity.png"), image.EncodeToPNG());
                RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
                Debug.Log("RAW_FORTRESS_REVIEW_OK"); SessionState.SetBool(Running, false); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }
    }
}
