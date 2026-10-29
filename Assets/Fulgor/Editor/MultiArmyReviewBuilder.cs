using System;
using System.IO;
using System.Linq;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class MultiArmyReviewBuilder
    {
        private const string Scene = "Assets/Fulgor/Scenes/ArtReview/CampaignMultiArmyReview.unity";
        private const string Scenario = "Assets/Fulgor/Scenarios/MultipleArmyReview.asset";
        private const string Running = "Fulgor.MultiArmyReview.Running", Output = "Fulgor.MultiArmyReview.Output";
        private static int frames;
        private static double started;
        static MultiArmyReviewBuilder() { if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick; }

        public static void Prepare()
        {
            try
            {
                if (File.Exists(Scene) || File.Exists(Scenario)) throw new InvalidOperationException("Review assets already exist; keep the existing review.");
                EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");
                var board = Object.FindAnyObjectByType<CampaignBoardPreview>();
                var settings = new SerializedObject(board);
                var source = settings.FindProperty("scenario").objectReferenceValue as CampaignScenarioDefinition;
                var scenario = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<CampaignScenarioDefinition>();
                scenario.name = "MultipleArmyReview";
                var map = ProceduralMapGenerator.Generate(scenario.Width, scenario.Height, scenario.Seed);
                var graph = scenario.CreateRouteGraph(map);
                // The same settlement/city art and map, with an explicit demonstration deployment.
                // Choose central accessible land so markers are legible in the normal map view.
                var land = map.Cells.Where(c => graph.CanStandAt(c.Coordinates)).OrderBy(c =>
                    Math.Abs(c.Coordinates.Q - map.Width / 2) + Math.Abs(c.Coordinates.R - map.Height / 2)).ToArray();
                var own = land[0].Coordinates;
                var second = land.First(c => c.Coordinates != own && c.Coordinates.DistanceTo(own) >= 2 &&
                    MapPathfinder.TryFindPath(graph, own, c.Coordinates, out _)).Coordinates;
                var other = land.First(c => c.Coordinates.DistanceTo(own) >= 4 && c.Coordinates != second &&
                    MapPathfinder.TryFindPath(graph, own, c.Coordinates, out _)).Coordinates;
                var data = new SerializedObject(scenario);
                data.FindProperty("useArmyDeployments").boolValue = true;
                data.FindProperty("playerFactionId").stringValue = "castilla";
                data.FindProperty("useExplicitDestination").boolValue = false;
                var armies = data.FindProperty("armies"); armies.arraySize = 3;
                SetArmy(armies.GetArrayElementAtIndex(0), "Castilla-1", "castilla", own, 0, 50);
                SetArmy(armies.GetArrayElementAtIndex(1), "Castilla-2", "castilla", second, 2, 30);
                SetArmy(armies.GetArrayElementAtIndex(2), "Hueste-rival", "rival", other, 0, 40);
                data.ApplyModifiedPropertiesWithoutUndo();
                var registry = scenario.CreateArmyRegistry(graph); scenario.CreateMission(map, graph, registry);
                AssetDatabase.CreateAsset(scenario, Scenario);
                settings.FindProperty("scenario").objectReferenceValue = scenario; settings.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Scene);
                AssetDatabase.SaveAssets(); Debug.Log("MULTI_ARMY_REVIEW_PREPARED"); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void SetArmy(SerializedProperty army, string id, string faction, HexCoordinates position, int profile, int quantity)
        {
            army.FindPropertyRelative("id").stringValue = id;
            army.FindPropertyRelative("factionId").stringValue = faction;
            army.FindPropertyRelative("position").vector2IntValue = new Vector2Int(position.Q, position.R);
            army.FindPropertyRelative("movementPerTurn").intValue = 6;
            var formations = army.FindPropertyRelative("formations"); formations.arraySize = 1;
            var formation = formations.GetArrayElementAtIndex(0);
            formation.FindPropertyRelative("id").stringValue = id + "-tropas";
            formation.FindPropertyRelative("profile").enumValueIndex = profile;
            formation.FindPropertyRelative("quantity").intValue = quantity;
        }

        public static void Build()
        {
            try
            {
                string output = Argument("-fulgorBuildOutput"); Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                    locationPathName = output, target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4 });
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-summary.json"),
                    JsonUtility.ToJson(new BuildSummary { result = report.summary.result.ToString(),
                        errors = report.summary.totalErrors, warnings = report.summary.totalWarnings }, true));
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Review()
        {
            try
            {
                string output = Argument("-fulgorReviewOutput"); Directory.CreateDirectory(output);
                EditorSceneManager.OpenScene(Scene);
                SessionState.SetString(Output, output); SessionState.SetBool(Running, true);
                frames = 0; started = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick; EditorApplication.update += Tick; EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Running, false)) { EditorApplication.update -= Tick; return; }
            if (started == 0) started = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - started > 120) { EditorApplication.Exit(1); return; }
            if (!EditorApplication.isPlaying || ++frames < 20) return;
            var campaign = Object.FindAnyObjectByType<CampaignTurnController>();
            if (campaign?.ArmyRegistry == null) return;
            try
            {
                var panel = campaign.GetComponent<CampaignSelectionPanel>();
                if (campaign.ArmyRegistry.Armies.Count != 3 || !panel.SaveButton.interactable || !panel.LoadButton.interactable)
                    throw new InvalidOperationException("Multiple army review failed.");
                campaign.LocateArmy(); Canvas.ForceUpdateCanvases();
                Capture(Camera.main, panel, Path.Combine(SessionState.GetString(Output, ""), "Guardado_Multihueste.png"));
                Debug.Log("MULTI_ARMY_REVIEW_OK"); SessionState.SetBool(Running, false);
                EditorApplication.update -= Tick; EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running, false); EditorApplication.Exit(1); }
        }

        private static void Capture(Camera camera, CampaignSelectionPanel panel, string path)
        {
            var target = new RenderTexture(1600, 1000, 24); target.Create();
            var image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            var canvases = panel.GetComponentsInChildren<Canvas>().Where(c => c.enabled).ToArray();
            var modes = canvases.Select(c => c.renderMode).ToArray();
            try
            {
                camera.targetTexture = target;
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                Canvas.ForceUpdateCanvases();
                // Recalculate UI scaling for the output target instead of the editor's Game view.
                foreach (var canvas in canvases)
                {
                    var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                    if (scaler != null && scaler.enabled) { scaler.enabled = false; scaler.enabled = true; }
                    foreach (var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>()) text.SetAllDirty();
                }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
            }
        }
        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name);
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Missing output argument.");
            return args[at + 1];
        }
        [Serializable] private sealed class BuildSummary { public string result; public int errors, warnings; }
    }
}
