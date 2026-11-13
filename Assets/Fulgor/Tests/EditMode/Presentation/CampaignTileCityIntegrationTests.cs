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
            Assert.AreSame(header,GameObject.Find("Campaign kingdom").GetComponent<RectTransform>());
            Assert.IsFalse(hud.CitiesButton.gameObject.activeInHierarchy);
            campaign.GetComponent<CampaignCityMapView>().BackButton.onClick.Invoke();yield return null;
            Assert.IsTrue(header.gameObject.activeInHierarchy);Assert.IsTrue(hud.CitiesButton.gameObject.activeInHierarchy);
            hud.CouncilButton.onClick.Invoke();Assert.IsTrue(campaign.GetComponent<CampaignDiplomacyView>().IsOpen);
            campaign.GetComponent<CampaignDiplomacyView>().Close();
        }
        [UnityTest] public IEnumerator ResearchPersistsPaidProgressAndAppliesIncomeOnce()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var seed=campaign.CaptureCampaignSnapshot();
            seed.wood=seed.gold=seed.labor=1000;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(seed)));
            foreach(var faction in campaign.Diplomacy.Factions)
                campaign.SetDiplomaticRelation(faction,Fulgor.Core.Campaign.DiplomaticRelation.Peace,Fulgor.Core.Campaign.DiplomaticIntention.Neutral);
            var before=campaign.State.Resources;
            var income=campaign.State.IncomePerTurn;
            var cost=Fulgor.Core.Campaign.CampaignTechnologies.Get("cultivation").Cost;
            Assert.IsTrue(campaign.TryStartResearch("cultivation"));
            Assert.IsTrue(before.TrySpend(cost,out var paid));
            Assert.AreEqual(paid,campaign.State.Resources);
            Assert.IsFalse(campaign.TryStartResearch("tools"));
            campaign.CompleteTurn();
            Assert.AreEqual(1,campaign.Research.Progress);
            string saved=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(saved));
            Assert.AreEqual(1,campaign.Research.Progress);
            Assert.AreEqual(saved,JsonUtility.ToJson(campaign.CaptureCampaignSnapshot()));
            for(int i=0;i<3;i++)campaign.CompleteTurn();
            Assert.IsTrue(campaign.Research.Has("cultivation"));
            Assert.AreEqual(income.Grain+2*campaign.ResearchCityCount,campaign.State.IncomePerTurn.Grain);
            saved=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(saved));
            Assert.AreEqual(saved,JsonUtility.ToJson(campaign.CaptureCampaignSnapshot()));
            var view=campaign.gameObject.AddComponent<CampaignResearchView>();
            view.Open();yield return null;
            Assert.IsTrue(view.IsOpen);
            Assert.IsTrue(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended);
            for(int i=0;i<5;i++)Assert.IsNotNull(GameObject.Find("Edad "+i));
            Assert.IsNotNull(GameObject.Find("Tecnología institutions"),"El árbol muestra también las edades futuras.");
            var untouched=campaign.State.Resources;
            view.SelectTechnology("institutions");
            Assert.IsFalse(view.ResearchButton.interactable);
            Assert.AreEqual(untouched,campaign.State.Resources,"Consultar un nodo bloqueado no paga la investigación.");
            view.SelectTechnology("tools");
            Assert.IsTrue(view.ResearchButton.interactable);
            view.ResearchButton.onClick.Invoke();yield return null;
            Assert.AreEqual("tools",campaign.Research.Active);
            Assert.IsFalse(view.ResearchButton.interactable);
            CaptureReviewIfRequested("-investigacion");
            view.Close();
        }
        [UnityTest] public IEnumerator CompletedKnowledgeHouseRaisesResearchRateStartingNextTurn()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var seed=campaign.CaptureCampaignSnapshot();
            seed.wood=seed.gold=seed.labor=100;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(seed)));
            foreach(var faction in campaign.Diplomacy.Factions)
                campaign.SetDiplomaticRelation(faction,Fulgor.Core.Campaign.DiplomaticRelation.Peace,Fulgor.Core.Campaign.DiplomaticIntention.Neutral);

            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            var tile=campaign.GetComponentsInChildren<HexTileView>().First(t=>t.Cell.Terrain==TerrainType.Plain
                && campaign.IsTileAssignedToCity(city.Id,t.Cell.Coordinates) && !campaign.IsCityTileOccupied(t.Cell.Coordinates)
                && campaign.VisibilityAt(t.Cell.Coordinates)!=Fulgor.Core.Campaign.CampaignVisibility.Unknown);
            Assert.IsTrue(campaign.TryPlaceCityTile(city.Id,tile.Cell,"knowledge_house"));
            Assert.IsTrue(campaign.TryStartResearch("cultivation"));
            campaign.CompleteTurn();
            Assert.AreEqual(0,campaign.ResearchKnowledgeCities);
            Assert.AreEqual(1,campaign.Research.Progress);
            campaign.CompleteTurn();
            Assert.AreEqual(1,campaign.ResearchKnowledgeCities);
            Assert.AreEqual(2,campaign.Research.Progress);
            var saved=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(saved));
            Assert.AreEqual(2,campaign.WisdomPerTurn);
            campaign.CompleteTurn();
            Assert.IsTrue(campaign.Research.Has("cultivation"));
        }
        [UnityTest] public IEnumerator QuarryUnlockSurvivesLoadAndAppearsInCity()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            Assert.IsFalse(campaign.IsProjectUnlocked("simple_quarry"));
            var snapshot=campaign.CaptureCampaignSnapshot();
            snapshot.research.completed=new[]{"tools"};
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(snapshot)));
            Assert.IsTrue(campaign.IsProjectUnlocked("simple_quarry"));
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var own=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            Assert.IsTrue(towns.OpenCityMapAt(own.Position));
            GameObject.Find("Edificios").GetComponentsInChildren<Button>().First(b=>b.name=="Cantera sencilla").onClick.Invoke();
            var city=campaign.GetComponent<CampaignCityMapView>();
            Assert.IsTrue(city.IsOpen);
            var position=new HexCoordinates(0,0);
            var hill=new MapCell(position.Neighbors().First(),TerrainType.Hill,false);
            var works=new Fulgor.Core.Campaign.CityTileDevelopment();
            Assert.IsTrue(works.TryAdd(position,hill,"simple_quarry"));
            works.Advance();
            Assert.AreEqual(0,works.ActiveIncome.Stone);
            works.Advance();
            Assert.AreEqual(3,works.ActiveIncome.Stone);
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
            yield return null;
            Assert.IsFalse(campaign.GetComponent<CampaignTileBuildings>().HasBuildingAt(tile.Cell.Coordinates));
            Assert.IsTrue(campaign.CancelCityTile(own.Id,tile.Cell.Coordinates));
            yield return null;
            var models=campaign.GetComponent<CampaignTileBuildings>();
            Assert.IsFalse(models.HasBuildingAt(tile.Cell.Coordinates));
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
            Assert.IsNull(GameObject.Find("Campaign kingdom").transform.Find("Cerrar"));
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
            Assert.AreEqual("10 / 20",cityData.Find("Valor 0").GetComponent<Text>().text);
            Assert.AreEqual("7 casillas",cityData.Find("Valor 1").GetComponent<Text>().text);
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().BlocksPointer(RectTransformUtility.WorldToScreenPoint(null,cityData.position)),"La ficha de datos debe bloquear la colocación sobre ella.");
            Assert.IsTrue(queue.Find("Fondo").GetComponent<Image>().sprite.texture.name=="panel_3","Las obras deben usar el arte de referencia, sin la madera antigua.");
            var legend=GameObject.Find("Beneficios").transform;
            Assert.IsNotNull(queue.GetComponent<Mask>());Assert.IsNotNull(legend.GetComponent<Mask>());
            var treasury=GameObject.Find("Reservas del reino").transform;
            var left=treasury.Find("Remate izquierdo").GetComponent<RawImage>().rectTransform;
            var center=treasury.Find("Madera y ribetes").GetComponent<RawImage>().rectTransform;
            var right=treasury.Find("Remate derecho").GetComponent<RawImage>().rectTransform;
            Assert.AreEqual(left.rect.width,center.anchoredPosition.x);
            Assert.AreEqual(center.anchoredPosition.x+center.rect.width,right.anchoredPosition.x);
            Assert.AreEqual(((RectTransform)treasury).rect.width,right.anchoredPosition.x+right.rect.width);
            Assert.Greater(((RectTransform)treasury.Find("Grano")).anchoredPosition.x,center.anchoredPosition.x);
            Assert.GreaterOrEqual(((RectTransform)legend.Find("Rendimiento")).anchoredPosition.x,30,"Los textos deben dejar margen para el marco.");
            Assert.IsFalse(Overlaps((RectTransform)legend.Find("Detalle título"),(RectTransform)legend.Find("Rendimiento")),"La leyenda no debe solapar su título.");
            Assert.IsTrue(selection.ActivateTile(tile));
            Assert.IsFalse(campaign.TryPlaceCityTile(own.Id,tile.Cell,"house"));
            Assert.AreEqual(185,campaign.State.Resources.Wood);
            yield return null;
            Assert.IsFalse(campaign.GetComponent<CampaignTileBuildings>().HasBuildingAt(tile.Cell.Coordinates));
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
            yield return null;
            Assert.IsFalse(campaign.GetComponent<CampaignTileBuildings>().HasBuildingAt(tile.Cell.Coordinates));
            Assert.IsTrue(campaign.CancelCityTile(own.Id,tile.Cell.Coordinates));
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(json));
            Assert.AreEqual(1,campaign.TileCity(own.Id).Pending.Length);
            yield return null;
            var markerName = $"Obra {tile.Cell.Coordinates.Q},{tile.Cell.Coordinates.R}";
            Assert.AreEqual("2",GameObject.Find(markerName).transform.Find("Turnos restantes/Número").GetComponent<Text>().text);
            Assert.AreSame(farm.Find("Imagen").GetComponent<Image>().sprite,GameObject.Find(markerName).GetComponent<Image>().sprite);
            campaign.CompleteTurn();
            for(int i=0;i<300&&campaign.IsRivalPhaseActive;i++)yield return null;
            yield return null;
            Assert.AreEqual("1",GameObject.Find(markerName).transform.Find("Turnos restantes/Número").GetComponent<Text>().text);
            Assert.IsFalse(models.HasBuildingAt(tile.Cell.Coordinates));
            campaign.CompleteTurn();
            for(int i=0;i<300&&campaign.IsRivalPhaseActive;i++)yield return null;
            Assert.AreEqual(12,campaign.TileCity(own.Id).Population);
            Assert.AreEqual("farm",campaign.TileCity(own.Id).BuildingAt(tile.Cell.Coordinates));
            Assert.AreEqual(3,campaign.TileCity(own.Id).ActiveIncome.Grain);
            Assert.AreEqual(3,campaign.CityIncomePerTurn.Grain);
            yield return null;
            cityData=GameObject.Find("Datos de la ciudad").transform;
            Assert.AreEqual("+3",cityData.Find("Producción 0").GetComponent<Text>().text,"La ficha debe mostrar la producción real de la mejora terminada.");
            CaptureReviewIfRequested();
            var done=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(done));
            yield return null;
            Assert.AreEqual(1,models.Placed.Count(model=>models.TryGetCell(model,out var cell)&&cell==tile.Cell.Coordinates&&model.GetComponentInChildren<MeshRenderer>()!=null));
            Assert.AreEqual(12,campaign.TileCity(own.Id).Population);
            Assert.AreEqual("farm",campaign.TileCity(own.Id).BuildingAt(tile.Cell.Coordinates));
            Assert.AreEqual(3,campaign.CityIncomePerTurn.Grain);
            var beforeFood=campaign.LocalFood(own.Id);
            var beforeTreasury=campaign.State.IncomePerTurn.Grain;
            var upgraded=campaign.CaptureCampaignSnapshot();
            upgraded.research.completed=new[]{"seed_selection"};
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(upgraded)));
            yield return null;
            Assert.AreEqual(beforeTreasury+1,campaign.State.IncomePerTurn.Grain);
            Assert.AreEqual(beforeFood+1,campaign.LocalFood(own.Id));
            Assert.AreEqual("+4",GameObject.Find("Datos de la ciudad").transform.Find("Producción 0").GetComponent<Text>().text);
            campaign.GetComponent<CampaignCityMapView>().BackButton.onClick.Invoke();
            Assert.IsFalse(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsTrue(campaign.GetComponent<CampaignArmyMarkerLayer>().CityPresentationVisible);
        }
        [UnityTest] public IEnumerator RecruitmentScreenUsesLiveOrdersAndReturnsToMap()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var seed=campaign.CaptureCampaignSnapshot();
            seed.grain=seed.wood=seed.gold=seed.labor=1000;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(seed)));
            foreach(var faction in campaign.Diplomacy.Factions)
                campaign.SetDiplomaticRelation(faction,Fulgor.Core.Campaign.DiplomaticRelation.Peace,Fulgor.Core.Campaign.DiplomaticIntention.Neutral);
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            Assert.IsTrue(towns.OpenCityMapAt(city.Position));
            GameObject.Find("Reclutar en ciudad").GetComponent<Button>().onClick.Invoke();
            var view=campaign.GetComponent<CampaignRecruitmentView>();
            Assert.IsTrue(view.IsOpen);
            Assert.IsFalse(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsTrue(campaign.GetComponent<CityManagementView>()?.IsOpen!=true);
            Assert.IsTrue(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended);
            view.SelectTroop(2);
            Assert.IsFalse(view.RecruitButton.interactable,"La ballestería sigue bloqueada por investigación.");
            view.SelectTroop(1);view.SelectTroop(0);
            Assert.IsTrue(GameObject.Find("Elegir tropa 0").transform.Find("Selección").gameObject.activeSelf);
            Assert.IsFalse(GameObject.Find("Elegir tropa 1").transform.Find("Selección").gameObject.activeSelf);
            Assert.IsNotNull(GameObject.Find("Ilustración de tropa").GetComponent<Image>().sprite);
            var before=campaign.State.Resources;
            view.RecruitButton.onClick.Invoke();
            Assert.IsFalse(view.RecruitButton.interactable);
            Assert.IsTrue(before.TrySpend(Fulgor.Core.Campaign.RecruitmentCatalog.Get(Fulgor.Core.Combat.TroopProfile.Spearmen).Cost,out var paid));
            Assert.AreEqual(paid,campaign.State.Resources);
            Assert.IsFalse(campaign.TryRecruit(city.Id,Fulgor.Core.Combat.TroopProfile.Spearmen));
            yield return null;
            CaptureReviewIfRequested("-reclutamiento");
            var saved=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(saved));
            yield return null;
            Assert.IsTrue(view.IsOpen);
            view.EndTurnButton.onClick.Invoke();yield return null;
            Assert.IsNotNull(campaign.Military.Pending(city.Id));
            view.EndTurnButton.onClick.Invoke();yield return null;
            Assert.IsNull(campaign.Military.Pending(city.Id));
            Assert.AreEqual(20,campaign.Military.Garrison(city.Id).Formations.Single().Quantity);
            Assert.That(GameObject.Find("Guarnición").transform.Find("Formación 0/Soldados").GetComponent<Text>().text,Does.Contain("20"));
            view.SelectFormation(0);
            Assert.IsTrue(view.TransferButton.interactable,"La hueste inicial adyacente puede recibir la formación.");
            view.TransferButton.onClick.Invoke();yield return null;
            Assert.IsTrue(campaign.Military.Garrison(city.Id).IsDissolved);
            view.SelectRosterSlot(true,0);
            Assert.IsTrue(view.TransferButton.interactable);
            view.TransferButton.onClick.Invoke();yield return null;
            Assert.IsFalse(campaign.Military.Garrison(city.Id).IsDissolved);
            view.BackButton.onClick.Invoke();
            Assert.IsFalse(view.IsOpen);
            Assert.IsFalse(campaign.GetComponent<CampaignBoardSelection>().IsInteractionSuspended);
        }
        [UnityTest] public IEnumerator FriendlyArmiesJoinAndShareTheirTypeCardsAfterSaving()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var own=campaign.ArmyRegistry.Armies.Where(a=>a.Army.FactionId==campaign.PlayerFactionId).ToArray();
            Assert.GreaterOrEqual(own.Length,2);
            var source=own[0];var target=own[1];
            var at=source.Movement.Position.Neighbors().First(p=>campaign.GetComponent<CampaignBoardSelection>().Routes.CanStandAt(p)
                && !campaign.ArmyRegistry.TryGetOccupant(p,out _) && !campaign.SettlementState.TryGet(p,out _));
            var snapshot=campaign.CaptureCampaignSnapshot();
            var savedTarget=snapshot.armyRegistry.armies.Single(a=>a.id==target.Army.Id);
            savedTarget.q=at.Q;savedTarget.r=at.R;snapshot.selectedArmyId=source.Army.Id;
            var staged=Fulgor.Core.Campaign.CampaignArmyRegistry.RestoreRecruited(snapshot.armyRegistry,campaign.ArmyRegistry,campaign.Military);
            snapshot.exploration=campaign.Fog.Refresh(staged,campaign.SettlementState,campaign.State.Turn).Capture();
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(snapshot)));
            Assert.That(campaign.ArmyJoinTargets(),Does.Contain(target.Army.Id));
            var total=source.Army.Formations.Sum(f=>(long)f.Quantity)+target.Army.Formations.Sum(f=>(long)f.Quantity);
            var board=campaign.GetComponent<CampaignBoardSelection>();
            var sourceTile=campaign.GetComponentsInChildren<HexTileView>().Single(t=>t.Cell.Coordinates==source.Movement.Position);
            var targetTile=campaign.GetComponentsInChildren<HexTileView>().Single(t=>t.Cell.Coordinates==at);
            var hit=campaign.GetComponentsInChildren<CampaignArmyClickTarget>().Single(c=>c.Tile.Cell.Coordinates==source.Movement.Position);
            Assert.IsTrue(hit.GetComponent<BoxCollider>().enabled);
            Assert.AreSame(sourceTile,board.ResolveHit(hit.GetComponent<BoxCollider>()));
            Assert.IsTrue(board.ActivateArmyTile(sourceTile));
            Assert.AreEqual(source.Army.Id,campaign.SelectedArmyId);
            Assert.IsTrue(board.ActivateRightClick(targetTile));
            yield return null;
            Assert.IsFalse(campaign.ArmyRegistry.TryGetArmy(source.Army.Id,out _));
            Assert.AreEqual(target.Army.Id,campaign.SelectedArmyId);
            var joined=campaign.ArmyRegistry.Armies.Single(a=>a.Army.Id==target.Army.Id);
            Assert.AreEqual(total,joined.Army.Formations.Sum(f=>(long)f.Quantity));
            var groups=Fulgor.Core.Campaign.CampaignTroopStack.Group(joined.Army.Formations);
            for(int i=0;i<groups.Count;i++)
                Assert.AreEqual(groups[i].Quantity.ToString(),GameObject.Find("Formación "+i).transform.Find("Efectivos").GetComponent<Text>().text);
            CaptureReviewIfRequested("-huestes-unidas");
            string json=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(json));
            Assert.IsFalse(campaign.ArmyRegistry.TryGetArmy(source.Army.Id,out _));
            Assert.AreEqual(json,JsonUtility.ToJson(campaign.CaptureCampaignSnapshot()));
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            Assert.IsTrue(towns.OpenCityMapAt(city.Position));
            var image=GameObject.Find("Edificios").transform.Find("Puesto de intercambio/Imagen");
            Assert.IsNotNull(image,"El selector del mercado debe contener su miniatura.");
            Assert.AreEqual("market_stall",image.GetComponent<Image>().sprite.texture.name);
            CaptureReviewIfRequested("-mercado");
        }
        [UnityTest] public IEnumerator TwoRosterRowsSupportDragStackingAndHeroGarrison()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var seed=campaign.CaptureCampaignSnapshot();
            seed.grain=seed.wood=seed.gold=seed.labor=1000;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(seed)));
            foreach(var faction in campaign.Diplomacy.Factions)
                campaign.SetDiplomaticRelation(faction,Fulgor.Core.Campaign.DiplomaticRelation.Peace,Fulgor.Core.Campaign.DiplomaticIntention.Neutral);
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            Assert.IsTrue(campaign.TryRecruit(city.Id,Fulgor.Core.Combat.TroopProfile.Spearmen));
            campaign.CompleteTurn();campaign.CompleteTurn();
            Assert.IsTrue(towns.OpenRecruitmentAt(city.Position));
            var view=campaign.GetComponent<CampaignRecruitmentView>();
            yield return null;
            var guard=GameObject.Find("Guarnición").transform;
            Assert.IsNotNull(guard.Find("Formación 0"));
            Assert.IsNotNull(guard.Find("Formación hueste 0"));
            var count=campaign.ArmyRegistry.Armies.Single(a=>a.Army.Id==campaign.SelectedArmyId).Army.Formations.Sum(f=>(long)f.Quantity);
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            var from=guard.Find("Formación 0").GetComponent<CampaignRosterSlot>();
            var to=guard.Find("Formación hueste 0").GetComponent<CampaignRosterSlot>();
            from.OnBeginDrag(data);to.OnDrop(data);from.OnEndDrag(data);
            yield return null;
            Assert.IsTrue(campaign.Military.Garrison(city.Id).IsDissolved);
            Assert.AreEqual((count+20).ToString(),guard.Find("Formación hueste 0/Soldados").GetComponent<Text>().text);
            Assert.IsTrue(view.DropRosterSlot(true,0,false,0));
            yield return null;
            Assert.AreEqual(count+20,campaign.Military.Garrison(city.Id).Formations.Sum(f=>(long)f.Quantity));
            var snapshot=campaign.CaptureCampaignSnapshot();
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(snapshot)));
            yield return null;
            // The last formation deposited its hero with the garrison.
            Assert.AreEqual(city.Id,campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry).settlementId);
            CaptureReviewIfRequested("-guarnicion");
            Assert.IsTrue(campaign.TryMuster(city.Id,campaign.Military.Garrison(city.Id).Formations[0].Id));
            yield return null;
            var heroFrom=guard.Find("Héroe en guarnición").GetComponent<CampaignRosterSlot>();
            var heroTo=guard.Find("Héroe visitante").GetComponent<CampaignRosterSlot>();
            heroFrom.OnBeginDrag(data);heroTo.OnDrop(data);heroFrom.OnEndDrag(data);
            yield return null;
            Assert.AreEqual(campaign.SelectedArmyId,campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry).armyId);
        }
        [UnityTest] public IEnumerator EmptyVisitorAcceptsHeroAndTroopsAfterWholeHostWasDeposited()
        {
            yield return null;
            var campaign = Object.FindAnyObjectByType<CampaignTurnController>();
            var towns = campaign.GetComponent<CampaignSettlementController>();
            var board = campaign.GetComponent<CampaignBoardSelection>();
            var city = campaign.SettlementState.Settlements.First(c => c.OwnerFactionId == campaign.PlayerFactionId);
            var heroId = campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry).armyId;
            var snapshot = campaign.CaptureCampaignSnapshot();
            foreach (var army in snapshot.armyRegistry.armies.Where(a => a.id != heroId && campaign.ArmyRegistry.Armies.Any(v => v.Army.Id == a.id && v.Army.FactionId == campaign.PlayerFactionId)))
            {
                var free = board.GetComponentsInChildren<HexTileView>().Select(t => t.Cell.Coordinates).First(at =>
                    at.DistanceTo(city.Position) > 4 && board.Routes.CanStandAt(at) &&
                    !campaign.SettlementState.TryGet(at, out _) && !snapshot.armyRegistry.armies.Any(a => a.q == at.Q && a.r == at.R));
                army.q = free.Q; army.r = free.R;
            }
            var staged = Fulgor.Core.Campaign.CampaignArmyRegistry.RestoreRecruited(snapshot.armyRegistry, campaign.ArmyRegistry, campaign.Military);
            snapshot.exploration = campaign.Fog.Refresh(staged, campaign.SettlementState, campaign.State.Turn).Capture();
            snapshot.selectedArmyId = heroId;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(snapshot)));
            Assert.IsTrue(towns.OpenRecruitmentAt(city.Position));
            var view = campaign.GetComponent<CampaignRecruitmentView>();
            Assert.IsTrue(view.DropRosterSlot(true, 0, false, 0));
            yield return null;
            Assert.AreEqual(city.Id, campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry).settlementId);
            view.SelectRosterHero(false);
            view.SelectRosterHero(true);
            yield return null;
            var visitorId = campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry).armyId;
            Assert.IsNotNull(visitorId, "Sacar al héroe debe recrear la hueste visitante con su guarnición.");
            Assert.IsTrue(campaign.ArmyRegistry.TryGetArmy(visitorId, out var visitor));
            Assert.AreEqual(city.Position, visitor.Movement.Position);
            Assert.IsTrue(campaign.Military.Garrison(city.Id).IsDissolved);
            var savedVisitor = campaign.CaptureCampaignSnapshot();
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(savedVisitor)));
            Assert.AreEqual(visitorId, campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry).armyId);
            CaptureReviewIfRequested("-visitante");
            Assert.IsTrue(view.DropRosterSlot(true, 0, false, 0));
            yield return null;
            Assert.IsTrue(view.DropRosterSlot(false, 0, true, 0), "Arrastrar tropas a visitante vacío también debe recrear la hueste.");
            yield return null;
            Assert.IsTrue(campaign.ArmyRegistry.TryGetArmy(campaign.SelectedArmyId, out visitor));
            Assert.AreEqual(city.Position, visitor.Movement.Position);
            view.SelectRosterHero(false); view.SelectRosterHero(true);
            yield return null;
            Assert.AreEqual(campaign.SelectedArmyId, campaign.Military.HeroPosition(campaign.PlayerFactionId, campaign.ArmyRegistry).armyId);
            view.Close();
            yield return null;
            CaptureReviewIfRequested("-puerta");
        }
        [UnityTest] public IEnumerator RetreatKeepsHeroAndRivalCityUsesMapInspection()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var board=campaign.GetComponent<CampaignBoardSelection>();
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId!=campaign.PlayerFactionId);
            var hero=campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
            var source=campaign.ArmyRegistry.Armies.Single(a=>a.Army.Id==hero.armyId);
            campaign.ArmyRegistry.TryGetOccupant(city.Position,out var occupying);
            var enemy=occupying??campaign.ArmyRegistry.Armies.First(a=>a.Army.FactionId==city.OwnerFactionId);
            var snapshot=campaign.CaptureCampaignSnapshot();
            var ownData=snapshot.armyRegistry.armies.Single(a=>a.id==source.Army.Id);
            snapshot.selectedArmyId=source.Army.Id;
            var enemyData=snapshot.armyRegistry.armies.Single(a=>a.id==enemy.Army.Id);
            enemyData.q=city.Position.Q;enemyData.r=city.Position.R;
            Fulgor.Core.Campaign.CampaignArmyRegistry staged=null;
            bool safe=false;
            foreach(var at in city.Position.Neighbors())
            {
                if(!board.Routes.CanStandAt(at)||campaign.ArmyRegistry.TryGetOccupant(at,out _)||campaign.SettlementState.TryGet(at,out _))continue;
                ownData.q=at.Q;ownData.r=at.R;
                var candidate=Fulgor.Core.Campaign.CampaignArmyRegistry.RestoreRecruited(snapshot.armyRegistry,campaign.ArmyRegistry,campaign.Military);
                if(!Fulgor.Core.Campaign.CampaignStrategicResolution.RetreatDestination(candidate,campaign.Military,campaign.SettlementState,
                    at,campaign.PlayerFactionId,source.Army.Formations.Count,source.Movement.MovementPerTurn).HasValue)continue;
                staged=candidate;safe=true;break;
            }
            Assert.IsTrue(safe,"El escenario debe ofrecer una retirada segura.");
            snapshot.exploration=campaign.Fog.Refresh(staged,campaign.SettlementState,campaign.State.Turn).Capture();
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(snapshot)));
            campaign.SetDiplomaticRelation(city.OwnerFactionId,Fulgor.Core.Campaign.DiplomaticRelation.War,Fulgor.Core.Campaign.DiplomaticIntention.Conquest);
            board.LocateCell(city.Position);
            Assert.IsTrue(campaign.AttackSelectedArmy());
            var battle=campaign.GetComponent<BattleLabController>();
            Assert.IsTrue(battle.WithdrawPlayerArmy());
            Assert.IsTrue(battle.ReturnToCampaign());
            yield return null;
            var escaped=campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry);
            Assert.AreEqual(hero.armyId,escaped.armyId);
            Assert.IsTrue(campaign.ArmyRegistry.TryGetArmy(escaped.armyId,out _));
            Assert.IsTrue(board.ActivateTile(towns.Views[city.Position].Tile));
            Assert.IsTrue(campaign.GetComponent<CityManagementView>()?.IsOpen!=true);
            Assert.IsFalse(campaign.GetComponent<CampaignCityMapView>()?.IsOpen==true);
            Assert.IsNotNull(GameObject.Find("Inspección de ciudad"));
            yield return null;
            CaptureReviewIfRequested("-inspeccion-tras-retirada");
            GameObject.Find("Localizar héroe").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(hero.armyId,campaign.SelectedArmyId);
            Assert.IsTrue(GameObject.Find("Retrato de hueste").GetComponent<KingdomPortraitGraphic>().enabled);
            string json=JsonUtility.ToJson(campaign.CaptureCampaignSnapshot());
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(json));
            Assert.AreEqual(hero.armyId,campaign.Military.HeroPosition(campaign.PlayerFactionId,campaign.ArmyRegistry).armyId);
            var ownCity=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            Assert.IsTrue(towns.OpenCityAt(ownCity.Position));
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsTrue(campaign.GetComponent<CityManagementView>()?.IsOpen!=true);
        }
        [UnityTest] public IEnumerator CityPresenceShowsSelectableGarrisonVisitorAndHeroAboveTheFortress()
        {
            yield return null;
            var campaign=Object.FindAnyObjectByType<CampaignTurnController>();
            var board=campaign.GetComponent<CampaignBoardSelection>();
            var towns=campaign.GetComponent<CampaignSettlementController>();
            var city=campaign.SettlementState.Settlements.First(c=>c.OwnerFactionId==campaign.PlayerFactionId);
            var initial=campaign.CityPresence(city.Id);
            Assert.IsTrue(initial.HeroVisiting);
            Assert.AreEqual(10,initial.VisitorTroops);
            var nearby=campaign.ArmyRegistry.Armies.First(a=>a.Army.FactionId==campaign.PlayerFactionId&&a.Army.Id!=initial.VisitorArmyId);
            Assert.IsTrue(campaign.SelectArmyForRoster(nearby.Army.Id));
            Assert.IsTrue(towns.OpenCityForcesAt(city.Position,true,true));
            var view=campaign.GetComponent<CampaignRecruitmentView>();
            Assert.AreEqual(initial.VisitorArmyId,campaign.SelectedArmyId);
            Assert.IsTrue(GameObject.Find("Héroe visitante").transform.Find("Selección").gameObject.activeSelf);
            Assert.AreEqual("10",GameObject.Find("Formación hueste 0").transform.Find("Soldados").GetComponent<Text>().text);
            view.BackButton.onClick.Invoke();
            Assert.IsTrue(campaign.TryTransferCityHero(city.Id,true));
            var seed=campaign.CaptureCampaignSnapshot();
            seed.grain=seed.wood=seed.gold=seed.labor=1000;
            Assert.IsTrue(campaign.TryLoadCampaignSnapshot(JsonUtility.ToJson(seed)));
            foreach(var faction in campaign.Diplomacy.Factions)
                campaign.SetDiplomaticRelation(faction,Fulgor.Core.Campaign.DiplomaticRelation.Peace,Fulgor.Core.Campaign.DiplomaticIntention.Neutral);
            Assert.IsTrue(campaign.TryRecruit(city.Id,Fulgor.Core.Combat.TroopProfile.Spearmen));
            campaign.CompleteTurn();campaign.CompleteTurn();
            board.LocateCell(city.Position);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsFalse(towns.LocateButton.gameObject.activeInHierarchy);
            Assert.IsFalse(towns.MarchButton.gameObject.activeInHierarchy);
            var panel=GameObject.Find("Settlement information").transform;
            Assert.That(panel.Find("Presencia militar/Seleccionar tropas de guarnición/Label").GetComponent<Text>().text,Does.Contain("20"));
            Assert.That(panel.Find("Presencia militar/Seleccionar tropas visitantes/Label").GetComponent<Text>().text,Does.Contain("10"));
            var badge=GameObject.Find("Fuerzas sobre ciudad · "+city.Id).transform;
            Assert.AreEqual("20",badge.Find("Guarnición/Efectivos/Label").GetComponent<Text>().text);
            Assert.AreEqual("10",badge.Find("Visitante/Efectivos/Label").GetComponent<Text>().text);
            Assert.IsTrue(badge.Find("Guarnición/Retrato/Héroe").GetComponent<RawImage>().enabled);
            Assert.IsFalse(badge.Find("Visitante/Retrato/Héroe").GetComponent<RawImage>().enabled);
            Canvas.ForceUpdateCanvases();
            var badgeRect=(RectTransform)badge;
            Assert.IsTrue(board.BlocksPointer(RectTransformUtility.WorldToScreenPoint(null,badgeRect.position)));
            CaptureReviewIfRequested("-presencia-ciudad");
            badge.Find("Guarnición/Retrato").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(view.IsOpen);
            Assert.IsTrue(GameObject.Find("Héroe en guarnición").transform.Find("Selección").gameObject.activeSelf);
            view.BackButton.onClick.Invoke();
            yield return null;
            board.LocateCell(city.Position);
            yield return new WaitForSecondsRealtime(.4f);
            panel=GameObject.Find("Settlement information").transform;
            panel.Find("Presencia militar/Seleccionar tropas visitantes").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(view.IsOpen);
            Assert.AreEqual(initial.VisitorArmyId,campaign.SelectedArmyId);
            Assert.IsTrue(GameObject.Find("Formación hueste 0").transform.Find("Selección").gameObject.activeSelf);
            view.BackButton.onClick.Invoke();yield return null;
            board.LocateCell(city.Position);yield return new WaitForSecondsRealtime(.4f);
            towns.VisitButton.onClick.Invoke();
            Assert.IsTrue(campaign.GetComponent<CampaignCityMapView>().IsOpen);
            Assert.IsTrue(campaign.GetComponent<CityManagementView>()?.IsOpen!=true);
        }
        private static void CaptureReviewIfRequested(string suffix="")
        {
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-fulgorCityScreenshot");
            if(index<0||index+1>=args.Length)return;
            var output=args[index+1];
            if(suffix.Length>0)output=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output),System.IO.Path.GetFileNameWithoutExtension(output)+suffix+".png");
            var camera=Camera.main;var previousTarget=camera.targetTexture;
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var settings=canvases.Select(c=>(c.worldCamera,c.planeDistance,c.scaleFactor,scaler:c.GetComponent<CanvasScaler>(),scaling:c.GetComponent<CanvasScaler>()?.enabled??false)).ToArray();
            var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;Texture2D texture=null;
            try
            {
                camera.targetTexture=target;
                foreach(var canvas in canvases)
                {
                    var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)scaler.enabled=false;
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.scaleFactor=1;
                    foreach(var text in canvas.GetComponentsInChildren<Text>())text.SetAllDirty();
                }
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));System.IO.File.WriteAllBytes(output,texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previous;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=settings[i].worldCamera;canvases[i].planeDistance=settings[i].planeDistance;canvases[i].scaleFactor=settings[i].scaleFactor;if(settings[i].scaler!=null)settings[i].scaler.enabled=settings[i].scaling;}
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


