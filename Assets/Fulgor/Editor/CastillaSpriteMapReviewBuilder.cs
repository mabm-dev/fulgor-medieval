using System;
using System.IO;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fulgor.Editor
{
    /// <summary>
    /// Prueba de edificio prerenderizado: la imagen de la fortaleza de Castilla como un plano orientado a la
    /// cámara fija del mapa (ortográfica, sin giro). Con proyección ortográfica, mover el plano a lo largo de
    /// la línea de visión no cambia dónde se ve, así que se adelanta hacia la cámara para que el terreno no lo corte.
    /// </summary>
    [InitializeOnLoad]
    public static class CastillaSpriteMapReviewBuilder
    {
        const string Root = "Assets/Fulgor/Art/Sprites/Fortalezas";
        const string TexturePath = Root + "/T_Sprite_Fortaleza_Castilla.png";
        const string MaterialPath = Root + "/M_Sprite_Fortaleza_Castilla.mat";
        const string PrefabPath = "Assets/Fulgor/Prefabs/ArtReview/PF_Fortaleza_Castilla_Sprite.prefab";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/CastillaSpriteMapReview.unity";
        const string Running = "Fulgor.CastillaSpriteReview.Running";
        const string Output = "Fulgor.CastillaSpriteReview.Output";
        const string Stage = "Fulgor.CastillaSpriteReview.Stage";
        // Desfase de la cámara de campaña (CampaignBoardPreview.ConfigureCamera): inclinación fija de unos 52°.
        static readonly Vector3 CameraOffset = new(0f, 28f, -22f);
        // Ancho en pantalla de la fortaleza, en unidades de mundo: algo más que la huella máxima de un asentamiento.
        const float Width = 1.3f;
        // Punto de la imagen (fracción desde la esquina superior izquierda) que se apoya en el centro de la casilla.
        static readonly Vector2 GroundAnchor = new(.511f, .63f);
        static double started;
        static int frames;

        static CastillaSpriteMapReviewBuilder()
        {
            if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
        }

        [MenuItem("Fulgor/Arte/Revisar Castilla prerenderizada en el mapa")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepara la escena fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath)
                ?? throw new InvalidOperationException("Falta la imagen recortada: " + TexturePath);
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Shader Unlit de URP no disponible.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.shader = shader;
            // La luz ya viene en la imagen: sin iluminar y con transparencia por alfa.
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(.93f, .93f, .93f, 1f));
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 2);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);

            float height = Width * texture.height / texture.width;
            var forward = -CameraOffset.normalized;
            var up = Vector3.Cross(forward, Vector3.right).normalized;   // vertical de la pantalla
            if (up.y < 0) up = -up;
            var center = up * ((GroundAnchor.y - .5f) * height) + Vector3.right * ((.5f - GroundAnchor.x) * Width);
            // borde inferior por encima del suelo con margen para el relieve de la casilla
            float below = -(center - up * (height * .5f)).y;
            float advance = (below + .15f) / -forward.y;
            center -= forward * advance;

            Folder("Assets/Fulgor/Prefabs/ArtReview");
            Folder("Assets/Fulgor/Scenes/ArtReview");
            var root = new GameObject("PF_Fortaleza_Castilla_Sprite");
            try
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Imagen";
                quad.transform.SetParent(root.transform, false);
                quad.transform.localPosition = center;
                quad.transform.localRotation = Quaternion.LookRotation(forward, up);
                quad.transform.localScale = new Vector3(Width, height, 1f);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity", OpenSceneMode.Single);
            var review = new GameObject("Revision de arte - Castilla prerenderizada").AddComponent<FortressMapArtReview>();
            var serialized = new SerializedObject(review);
            serialized.FindProperty("fortressPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // el asentamiento de Castilla usa la imagen como visual en lugar de instanciar además la fortaleza 3D
            var settlements = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<CampaignSettlementController>()
                ?? throw new InvalidOperationException("La escena de campaña no tiene asentamientos."));
            settlements.FindProperty("reuseArtReviewFortress").boolValue = true;
            settlements.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("CASTILLA_SPRITE_SCENE_READY " + ScenePath + " adelanto=" + advance.ToString("0.000"));
        }

        public static void Batch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-fulgorReviewOutput");
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Falta el directorio de capturas.");
                Directory.CreateDirectory(args[at + 1]);
                Prepare();
                SessionState.SetString(Output, args[at + 1]); SessionState.SetInt(Stage, 0); SessionState.SetBool(Running, true);
                started = EditorApplication.timeSinceStartup; frames = 0;
                EditorApplication.update -= Tick; EditorApplication.update += Tick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) { EditorApplication.update -= Tick; return; }
            if (SessionState.GetInt(Stage, 0) == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Running, false); EditorApplication.update -= Tick; EditorApplication.Exit(0); return;
            }
            if (started == 0) started = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - started > 120)
            { Debug.LogError("Tiempo agotado al revisar Castilla prerenderizada."); SessionState.SetBool(Running, false); EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || SessionState.GetInt(Stage, 0) != 0) return;
            var review = UnityEngine.Object.FindFirstObjectByType<FortressMapArtReview>();
            if (review == null || !review.PlacementReady || ++frames < 12) return;
            SessionState.SetInt(Stage, 1);
            try
            {
                var camera = Camera.main;
                var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.enabled = false;
                string output = SessionState.GetString(Output, "");
                var focus = review.PlacedFortress.position;
                camera.transform.position = focus + CameraOffset; camera.transform.LookAt(focus);
                // 10: vista amplia; 6 y 4: zoom de juego (4 es el tope de CampaignCameraController); 2: más allá del tope
                foreach (float zoom in new[] { 10f, 6f, 4f, 2f })
                {
                    camera.orthographicSize = zoom;
                    for (int i = 0; i < 2; i++) Capture(camera, Path.Combine(output, $"Castilla_Sprite_Zoom{zoom:0}.png"));
                }
                Debug.Log("CASTILLA_SPRITE_REVIEW_OK tile=" + review.ReviewTile.Cell.Coordinates.Key);
                SessionState.SetInt(Stage, 2); EditorApplication.isPlaying = false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        static void Folder(string path)
        {
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) Folder(parent);
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
