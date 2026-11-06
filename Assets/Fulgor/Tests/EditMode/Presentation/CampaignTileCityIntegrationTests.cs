using System.Collections;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignTileCityIntegrationTests
    {
        private string previousScene;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousScene=EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/Playable/FulgorConquest.unity");
            // Reserve a free first-ring tile for construction; the scene now showcases buildings on the entire ring.
            var buildings=Object.FindAnyObjectByType<CampaignTileBuildings>();
            var buildingData=new SerializedObject(buildings);
            var entries=buildingData.FindProperty("entries");
            for(int i=entries.arraySize-1;i>=0;i--)
                if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("cell").vector2IntValue!=new Vector2Int(12,9))entries.DeleteArrayElementAtIndex(i);
            buildingData.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            for(int i=0;i<300;i++)
            {
                if(Object.FindAnyObjectByType<CampaignSettlementController>()?.Ready==true)yield break;
                yield return null;
            }
            Assert.Fail("La campaña no se ha iniciado.");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            if(!string.IsNullOrEmpty(previousScene))EditorSceneManager.OpenScene(previousScene);
        }
        [UnityTest] public IEnumerator CampaignHudUsesLiveRosterAndCityNavigation()
        {
            yield return null;yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var hud=campaign.GetComponent<CampaignSelectionPanel>();
            Assert.IsTrue(hud.HasCampaignHud);
            Assert.IsTrue(campaign.ArmyRegistry.TryGetArmy(campaign.SelectedArmyId,out var active));
            var card=GameObject.Find("Formación 0").GetComponent<Button>();
            Assert.IsTrue(card.interactable);
            Assert.AreEqual(active.Army.Formations[0].Quantity.ToString(),card.transform.Find("Efectivos").GetComponent<Text>().text);
            card.onClick.Invoke();
            Assert.That(hud.FormationDetails,Does.Contain("Moral").And.Contain(active.Army.Formations[0].Quantity.ToString()));
            var before=campaign.SelectedArmyId;
            hud.NextArmyButton.onClick.Invoke();yield return null;
            Assert.AreNotEqual(before,campaign.SelectedArmyId);
            Assert.IsNull(hud.FormationDetails,"Un detalle de la hueste anterior no debe quedar visible.");
            Assert.IsTrue(campaign.ArmyRegistry.TryGetArmy(campaign.SelectedArmyId,out active));
            Assert.AreEqual(active.Army.Formations[0].Quantity.ToString(),card.transform.Find("Efectivos").GetComponent<Text>().text);
            Canvas.ForceUpdateCanvases();
            var header=GameObject.Find("Campaign kingdom").GetComponent<RectTransform>();
            var treasury=GameObject.Find("Campaign resource bar").GetComponent<RectTransform>();
            var army=GameObject.Find("Turn Panel").GetComponent<RectTransform>();
            var turn=GameObject.Find("Turn closing").GetComponent<RectTransform>();
            Assert.IsFalse(Overlaps(header,treasury));Assert.IsFalse(Overlaps(army,turn));
            CaptureReviewIfRequested("-mapa");
            hud.CitiesButton.onClick.Invoke();
            var own=campaign.SettlementState.Settlements.First(s=>s.OwnerFactionId==campaign.PlayerFactionId);
            var list=GameObject.Find("Ciudades del reino").transform;
            list.GetComponentsInChildren<Button>().First(b=>b.name==own.Id).onClick.Invoke();
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsFalse(header.gameObject.activeInHierarchy);
            Assert.IsFalse(hud.CitiesButton.gameObject.activeInHierarchy);
            campaign.GetComponent<CampaignCityMapView>().BackButton.onClick.Invoke();yield return null;
            Assert.IsTrue(header.gameObject.activeInHierarchy);Assert.IsTrue(hud.CitiesButton.gameObject.activeInHierarchy);
            hud.CouncilButton.onClick.Invoke();Assert.IsTrue(campaign.GetComponent<CampaignDiplomacyView>().IsOpen);
            campaign.GetComponent<CampaignDiplomacyView>().Close();
        }
        [UnityTest] public IEnumerator BuildCancelSaveAndCompleteOnRealTiles()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var settlements=campaign.GetComponent<CampaignSettlementController>();
            var own=settlements.Registry.Settlements.First(s=>s.OwnerFactionId==campaign.PlayerFactionId);
            var tile=campaign.GetComponentsInChildren<HexTileView>().First(t=>t.Cell.Coordinates.DistanceTo(own.Position)==1&&t.Cell.Terrain==TerrainType.Plain&&!campaign.IsCityTileOccupied(t.Cell.Coordinates)&&campaign.IsTileAssignedToCity(own.Id,t.Cell.Coordinates));
            Assert.AreEqual(25,campaign.State.Resources.Wood);
            Assert.AreEqual(15,campaign.State.Resources.Labor);
            Assert.IsTrue(campaign.IsCityTileOccupied(new HexCoordinates(12,9)),"El edificio visible debe ocupar su casilla.");
            Assert.IsTrue(campaign.TryPlaceCityTile(own.Id,tile.Cell,"farm"),"El jugador puede construir desde el primer turno.");
            Assert.IsTrue(campaign.CancelCityTile(own.Id,tile.Cell.Coordinates));
            var save=campaign.CaptureCampaignSnapshot();save.wood=200;save.labor=200;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(save)));
            var selection=campaign.GetComponent<CampaignBoardSelection>();
            Assert.IsTrue(selection.ActivateTile(settlements.Views[own.Position].Tile));
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsFalse(campaign.GetComponent<CampaignArmyMarkerLayer>().CityPresentationVisible);
            Assert.IsFalse(campaign.GetComponent<CampaignSelectionPanel>().SaveButton.gameObject.activeInHierarchy);
            Assert.IsFalse(campaign.GetComponentsInChildren<MapCameraControls>(true).Any(c=>c.gameObject.activeInHierarchy),"La campaña no debe mostrar los botones de navegación.");
            Assert.IsTrue(settlements.Views[own.Position].GetComponentsInChildren<MeshFilter>().Any(m=>m.name=="Sombreado del territorio"&&m.sharedMesh==tile.GetComponent<MeshFilter>().sharedMesh&&Vector3.ProjectOnPlane(m.transform.position-tile.transform.position,Vector3.up).sqrMagnitude<.0001f),"El tinte debe seguir la malla del suelo sin afectar los edificios.");
            var mountain=campaign.GetComponentsInChildren<HexTileView>().First(t=>t.Cell.Coordinates.DistanceTo(own.Position)==1&&t.Cell.Terrain==TerrainType.Mountain);
            mountain.SetHovered(false);mountain.SetSelected(false);
            Assert.IsFalse(mountain.HasColorOverride,"La montaña conserva su material, sin relleno territorial.");
            Assert.IsNull(mountain.transform.Find("Oro"),"La montaña no debe mostrar un recurso.");
            var border=settlements.Views[own.Position].GetComponentsInChildren<LineRenderer>().First(l=>l.name=="Territorio del reino");
            Assert.AreEqual(0,border.numCornerVertices,"La frontera debe conservar esquinas rectas de hexágono.");
            Assert.AreEqual(18,border.positionCount,"El primer anillo debe trazarse por sus 18 aristas, sin puntos curvos intermedios.");
            foreach(var renderer in settlements.Views[own.Position].GetComponentsInChildren<MeshRenderer>())
                Assert.IsFalse(renderer.HasPropertyBlock(),"El tinte territorial no debe afectar los materiales de la fortaleza.");
            var session=campaign.GetComponent<CampaignPlaySession>();
            session.TogglePause();
            Assert.IsTrue(session.IsOpen,"La acción de Escape debe abrir el menú de pausa.");
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().IsOpen,"La pausa no debe cerrar la ciudad.");
            session.Resume();yield return null;
            var toolbar=GameObject.Find("Edificios").transform;
            var home=toolbar.Find("Vivienda");var farm=toolbar.Find("Huerto");
            Assert.IsNull(GameObject.Find("Reino").transform.Find("Cerrar"));
            Assert.IsTrue(farm.Find("Selección").gameObject.activeSelf);
            home.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(home.Find("Selección").gameObject.activeSelf);
            var selectedFrame=home.Find("Selección").GetComponent<CitySelectionFrame>();
            Assert.IsNotNull(selectedFrame);
            Assert.IsFalse(selectedFrame.raycastTarget);
            Canvas.ForceUpdateCanvases();
            var frameMesh=selectedFrame.canvasRenderer.GetMesh();
            Assert.AreEqual(32,frameMesh.vertexCount,"El marco debe tener dos ribetes con ocho esquinas.");
            var rect=selectedFrame.rectTransform.rect;
            foreach(var vertex in frameMesh.vertices)
                Assert.IsTrue(vertex.x>rect.xMin&&vertex.x<rect.xMax&&vertex.y>rect.yMin&&vertex.y<rect.yMax,"El selector debe quedar dentro de la tarjeta.");
            Assert.IsFalse(farm.Find("Selección").gameObject.activeSelf);
            farm.GetComponent<Button>().onClick.Invoke();
            campaign.GetComponent<CampaignCityMapView>().Inspect(tile);
            Canvas.ForceUpdateCanvases();
            var queue=GameObject.Find("Cola").transform;
            Assert.IsFalse(Overlaps((RectTransform)queue.Find("Title"),(RectTransform)queue.Find("Cola texto")),"La cola no debe solapar su encabezado.");
            var cityData=GameObject.Find("Datos de la ciudad").transform;
            Assert.IsFalse(Overlaps((RectTransform)queue,(RectTransform)cityData),"La ficha de ciudad debe quedar debajo de las obras sin solaparse.");
            Assert.AreEqual("Sin censo",cityData.Find("Valor 0").GetComponent<Text>().text);
            Assert.AreEqual("7 casillas",cityData.Find("Valor 1").GetComponent<Text>().text);
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().BlocksPointer(RectTransformUtility.WorldToScreenPoint(null,cityData.position)),"La ficha de datos debe bloquear la colocación sobre ella.");
            Assert.IsTrue(queue.Find("Fondo").GetComponent<Image>().sprite.texture.name=="panel_3","Las obras deben usar el arte de referencia, sin la madera antigua.");
            var legend=GameObject.Find("Beneficios").transform;
            Assert.IsNotNull(queue.GetComponent<Mask>());Assert.IsNotNull(legend.GetComponent<Mask>());
            Assert.IsNotNull(GameObject.Find("Reservas del reino").GetComponent<Mask>());
            Assert.GreaterOrEqual(((RectTransform)legend.Find("Rendimiento")).anchoredPosition.x,30,"Los textos deben dejar margen para el marco.");
            Assert.IsFalse(Overlaps((RectTransform)legend.Find("Detalle título"),(RectTransform)legend.Find("Rendimiento")),"La leyenda no debe solapar su título.");
            Assert.IsTrue(selection.ActivateTile(tile));
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id,tile.Cell,"house"));
            Assert.AreEqual(185,campaign.State.Resources.Wood);
            Assert.IsTrue(campaign.CancelCityTile(own.Id,tile.Cell.Coordinates));
            Assert.AreEqual(200,campaign.State.Resources.Wood);
            Assert.IsTrue(campaign.TryPlaceCityTile(own.Id,tile.Cell,"farm"));
            CaptureReviewIfRequested("-cola");
            var json=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            var catalog=AssetDatabase.LoadAssetAtPath<CampaignLaunchCatalog>("Assets/Fulgor/Scenarios/CampaignLaunchCatalog.asset");
            Assert.IsTrue(catalog.TryFindSnapshot(json,out _,out _),"El menú debe reconocer el guardado de obras.");
            var bad=JsonUtility.FromJson<Fulgor.Core.Campaign.CampaignSaveData>(json);
            bad.tileCities.cities[0].pending=new[]{bad.tileCities.cities[0].pending[0],bad.tileCities.cities[0].pending[0]};
            var before=campaign.State;
            LogAssert.Expect(LogType.Warning,new Regex("No se pudo cargar"));
            Assert.IsFalse(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(bad)));
            Assert.AreSame(before,campaign.State);
            Assert.IsTrue(campaign.CancelCityTile(own.Id,tile.Cell.Coordinates));
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(json));
            Assert.AreEqual(1,campaign.TileCity(own.Id).Pending.Length);
            campaign.CompleteTurn();
            for(int i=0;i<300&&campaign.IsRivalPhaseActive;i++)yield return null;
            campaign.CompleteTurn();
            for(int i=0;i<300&&campaign.IsRivalPhaseActive;i++)yield return null;
            Assert.AreEqual("farm",campaign.TileCity(own.Id).BuildingAt(tile.Cell.Coordinates));
            Assert.AreEqual(3,campaign.TileCity(own.Id).ActiveIncome.Grain);
            Assert.AreEqual(3,campaign.CityIncomePerTurn.Grain);
            yield return null;
            cityData=GameObject.Find("Datos de la ciudad").transform;
            Assert.AreEqual("+3",cityData.Find("Producción 0").GetComponent<Text>().text,"La ficha debe mostrar la producción real de la mejora terminada.");
            CaptureReviewIfRequested();
            var done=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(done));
            Assert.AreEqual("farm",campaign.TileCity(own.Id).BuildingAt(tile.Cell.Coordinates));
            Assert.AreEqual(3,campaign.CityIncomePerTurn.Grain);
            campaign.GetComponent<CampaignCityMapView>().BackButton.onClick.Invoke();
            Assert.IsFalse(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsTrue(campaign.GetComponent<CampaignArmyMarkerLayer>().CityPresentationVisible);
        }
        private static void CaptureReviewIfRequested(string suffix="")
        {
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-fulgorCityScreenshot");
            if(index<0||index+1>=args.Length)return;
            var output=args[index+1];
            if(suffix.Length>0)output=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output),System.IO.Path.GetFileNameWithoutExtension(output)+suffix+".png");
            var camera=Camera.main;var previousTarget=camera.targetTexture;
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var settings=canvases.Select(c=>(c.worldCamera,c.planeDistance)).ToArray();
            var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;Texture2D texture=null;
            try
            {
                camera.targetTexture=target;
                foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));System.IO.File.WriteAllBytes(output,texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previous;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=settings[i].worldCamera;canvases[i].planeDistance=settings[i].planeDistance;}
                Canvas.ForceUpdateCanvases();Object.Destroy(target);if(texture!=null)Object.Destroy(texture);
            }
        }
        private static bool Overlaps(RectTransform a,RectTransform b)
        {
            var p=new Vector3[4];var q=new Vector3[4];a.GetWorldCorners(p);b.GetWorldCorners(q);
            return p[0].x<q[2].x&&p[2].x>q[0].x&&p[0].y<q[2].y&&p[2].y>q[0].y;
        }
    }
}
