using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Fulgor.Presentation.Map
{
    /// <summary>Opt-in player review; does not write the campaign save slot.</summary>
    public sealed class CampaignMapHudReview : MonoBehaviour
    {
        private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-fulgorMapHudReview");
            if (at < 0 || at + 1 >= args.Length || FindAnyObjectByType<CampaignMapHudReview>() != null) return;
            var root = new GameObject("Optional map interface review");
            DontDestroyOnLoad(root);
            root.AddComponent<CampaignMapHudReview>().output = args[at + 1];
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            var routine = Run();
            while (true)
            {
                bool next; object current = null;
                try { next = routine.MoveNext(); if (next) current = routine.Current; }
                catch (Exception e) { Debug.LogException(e); Application.Quit(1); yield break; }
                if (!next) break;
                yield return current;
            }
        }

        private IEnumerator Run()
        {
            Directory.CreateDirectory(output);
            bool hadSave = PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey);
            string save = PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, "");
            CampaignLaunchMenu menu = null;
            for (int n = 0; n < 600; n++)
            {
                menu = FindAnyObjectByType<CampaignLaunchMenu>();
                if (menu?.ContinueButton != null) break;
                yield return null;
            }
            Require(menu != null && menu.StartCampaign("castilla-conquest"), "Campaign start failed.");
            CampaignTurnController campaign = null;
            for (int n = 0; n < 900; n++)
            {
                campaign = FindAnyObjectByType<CampaignTurnController>();
                if (campaign?.State != null && campaign.GetComponent<CampaignSettlementController>()?.Ready == true && !CampaignNavigation.IsTransitioning) break;
                yield return null;
            }
            Require(campaign?.State != null, "Campaign did not initialize.");
            yield return null; yield return null;
            var hud = campaign.GetComponent<CampaignSelectionPanel>();
            var towns = campaign.GetComponent<CampaignSettlementController>();
            var selection = campaign.GetComponent<CampaignBoardSelection>();
            var initialArmy = campaign.SelectedArmyId;
            hud.NextArmyButton.onClick.Invoke();
            Require(campaign.SelectedArmyId != initialArmy, "Next army is not connected.");
            hud.NextArmyButton.onClick.Invoke();
            Require(campaign.SelectedArmyId == initialArmy, "Army selection did not cycle.");
            hud.LocateArmyButton.onClick.Invoke();
            Require(selection.SelectedCell?.Coordinates == campaign.Army.Position, "Locate army failed.");

            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false);
                yield return new WaitForSecondsRealtime(.4f);
                Canvas.ForceUpdateCanvases();
                ValidateLayout(campaign);
                Capture(Path.Combine(output, $"mapa-{size.x}x{size.y}.png"), size, campaign);
            }

            Screen.SetResolution(1920, 1080, false);
            yield return new WaitForSecondsRealtime(.4f);
            towns.LocateButton.onClick.Invoke(); yield return null;
            ValidateLayout(campaign);
            Capture(Path.Combine(output, "mapa-ciudad.png"), new Vector2Int(1920, 1080), campaign);
            Require(towns.VisitButton.interactable, "City management unavailable after locate.");
            towns.VisitButton.onClick.Invoke(); yield return null;
            Require(campaign.GetComponent<CityManagementView>()?.IsOpen == true && selection.IsInteractionSuspended, "City did not suspend map input.");
            campaign.GetComponent<CityManagementView>().Close(); yield return null;

            var diplomacy = campaign.GetComponent<CampaignDiplomacyView>();
            diplomacy.Open(); yield return null;
            Require(diplomacy.IsOpen && selection.IsInteractionSuspended, "Diplomacy did not suspend map input.");
            diplomacy.Close(); yield return null;
            var session = campaign.GetComponent<CampaignPlaySession>();
            session.MenuButton.onClick.Invoke(); yield return null;
            Require(session.IsOpen && selection.IsInteractionSuspended, "Pause did not suspend map input.");
            session.ResumeButton.onClick.Invoke(); yield return null;
            Require(!selection.IsInteractionSuspended, "Map did not resume.");
            Require(hadSave == PlayerPrefs.HasKey(CampaignTurnController.LocalSaveKey) && save == PlayerPrefs.GetString(CampaignTurnController.LocalSaveKey, ""), "Review changed save slot.");
            File.WriteAllText(Path.Combine(output, "review.json"), "{\"resolutions\":[\"1920x1080\",\"1280x720\",\"1024x768\"],\"layout\":true,\"textFits\":true,\"pointerBlocking\":true,\"armyCycle\":true,\"city\":true,\"diplomacy\":true,\"pause\":true,\"saveSlotUnchanged\":true}");
            Debug.Log("CAMPAIGN_MAP_HUD_REVIEW_OK");
            Application.Quit(0);
        }

        private static void ValidateLayout(CampaignTurnController campaign, Camera camera = null, int width = 0, int height = 0)
        {
            Canvas.ForceUpdateCanvases();
            if (width == 0) { width = Screen.width; height = Screen.height; }
            var rects = campaign.GetComponentsInChildren<RectTransform>();
            string[] names = { "Campaign resource bar", "Map Information", "Turn Panel", "Turn closing", "Campaign tools", "Navegación de cámara", "Settlement information" };
            var panels = names.Select(name => rects.Single(r => r.name == name)).ToArray();
            var hud = campaign.GetComponent<CampaignSelectionPanel>();
            foreach (var panel in panels)
            {
                var box = Bounds(panel, camera);
                Require(box.xMin >= -1 && box.yMin >= -1 && box.xMax <= width + 1 && box.yMax <= height + 1, "Off-screen panel: " + panel.name);
                if (camera == null) Require(campaign.GetComponent<CampaignBoardSelection>().BlocksPointer(box.center), "Unblocked panel: " + panel.name);
                foreach (var other in panels)
                    if (other != panel) Require(!box.Overlaps(Bounds(other, camera)), "Overlapping panels: " + panel.name + " / " + other.name);
                foreach (var label in panel.GetComponentsInChildren<Text>())
                    Require(label.preferredHeight <= label.rectTransform.rect.height + 2, "Clipped text: " + panel.name + "/" + label.name + " " + label.text);
            }
            var treasury = rects.Single(r => r.name == "Treasury");
            var diplomacy = rects.Single(r => r.name == "Diplomacy");
            Require(!Bounds(treasury, camera).Overlaps(Bounds(diplomacy, camera)), "Treasury overlaps diplomacy.");
            if (camera == null) Require(!hud.ContainsScreenPoint(new Vector2(Screen.width / 2f, Screen.height / 2f)), "Map center is blocked.");
        }

        private static Rect Bounds(RectTransform rect, Camera camera = null)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void Capture(string path, Vector2Int size, CampaignTurnController campaign)
        {
            var camera = Camera.main;
            var canvases = FindObjectsByType<Canvas>().Where(c => c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var scalers = canvases.Select(c => c.GetComponent<CanvasScaler>()).ToArray();
            var factors = canvases.Select(c => c.scaleFactor).ToArray();
            var oldTarget = camera.targetTexture; int oldMask = camera.cullingMask;
            var oldRect = camera.rect;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(size.x, size.y, 24); target.Create();
            Texture2D image = null;
            try
            {
                camera.targetTexture = target; camera.rect = new Rect(0, 0, 1, 1);
                for (int i = 0; i < canvases.Length; i++)
                {
                    var canvas = canvases[i];
                    if (scalers[i] != null) scalers[i].enabled = false;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 1;
                    // Same match-width-or-height calculation, independent of the host monitor's window limit.
                    canvas.scaleFactor = Mathf.Sqrt(size.x / 1920f * size.y / 1080f);
                }
                camera.cullingMask = -1; Canvas.ForceUpdateCanvases();
                ValidateLayout(campaign, camera, size.x, size.y);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = null;
                    canvases[i].scaleFactor = factors[i]; if (scalers[i] != null) scalers[i].enabled = true;
                }
                camera.targetTexture = oldTarget; camera.rect = oldRect; camera.cullingMask = oldMask; RenderTexture.active = oldActive;
                target.Release(); Destroy(target); if (image != null) Destroy(image);
            }
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
