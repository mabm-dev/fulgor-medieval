using System.Collections;
using System.Linq;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CityManagementSceneTests
    {
        private const string SaveKey="Fulgor.Campaign.Prototype.Save.v1";
        private bool hadSave;private string previousSave;
        private string previous;
        private CampaignSettlementController settlements;
        private CampaignBoardSelection selection;
        private CampaignTurnController campaign;
        private CampaignSettlementDefinition clonedDefinition;
        private HexTileView Castle=>settlements.Views.Values.Single().Tile;
        private CityManagementView City=>settlements.GetComponent<CityManagementView>();
        [UnitySetUp]public IEnumerator Setup()
        {
            hadSave=PlayerPrefs.HasKey(SaveKey);previousSave=PlayerPrefs.GetString(SaveKey,"");
            previous=EditorSceneManager.GetActiveScene().path;EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");
            yield return new EnterPlayMode();yield return null;yield return null;
            settlements=Object.FindFirstObjectByType<CampaignSettlementController>();selection=settlements.GetComponent<CampaignBoardSelection>();campaign=settlements.GetComponent<CampaignTurnController>();
            Assert.That(settlements.Ready,Is.True);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying){if(City!=null)City.Close();yield return new ExitPlayMode();}
            if(hadSave)PlayerPrefs.SetString(SaveKey,previousSave);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
            if(clonedDefinition!=null)Object.DestroyImmediate(clonedDefinition);
            if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }
        [UnityTest]public IEnumerator CastleActivationOpensCityAndRestoresCameraAndHud()
        {
            var camera=Camera.main;int mask=camera.cullingMask;var clear=camera.clearFlags;var background=camera.backgroundColor;var position=camera.transform.position;var rotation=camera.transform.rotation;float zoom=camera.orthographicSize;
            var hud=settlements.GetComponentsInChildren<Canvas>();var enabled=hud.Select(c=>c.enabled).ToArray();var state=campaign.State;
            Assert.That(selection.ActivateTile(Castle),Is.True);Assert.That(City.IsOpen,Is.True);Assert.That(City.BuildingCount,Is.EqualTo(3));Assert.That(selection.IsInteractionSuspended,Is.True);
            Assert.That(campaign.State,Is.SameAs(state));Assert.That(hud.All(c=>!c.enabled),Is.True);
            var other=settlements.GetComponentsInChildren<HexTileView>().First(t=>t!=Castle);
            Assert.That(selection.ActivateTile(other),Is.False);Assert.That(selection.SelectedCell.Value.Coordinates,Is.EqualTo(Castle.Cell.Coordinates));
            Assert.That(settlements.BlocksPointer(new Vector2(10,10)),Is.True);
            City.Close();Assert.That(City.ClosedThisFrame,Is.True);yield return null;
            Assert.That(selection.IsInteractionSuspended,Is.False);Assert.That(camera.cullingMask,Is.EqualTo(mask));Assert.That(camera.clearFlags,Is.EqualTo(clear));Assert.That(camera.backgroundColor,Is.EqualTo(background));
            Assert.That(camera.transform.position,Is.EqualTo(position));Assert.That(camera.transform.rotation,Is.EqualTo(rotation));Assert.That(camera.orthographicSize,Is.EqualTo(zoom));
            for(int i=0;i<hud.Length;i++)Assert.That(hud[i].enabled,Is.EqualTo(enabled[i]));
            Assert.That(Object.FindObjectsByType<CityBuildingView>(FindObjectsSortMode.None),Is.Empty);
        }
        [UnityTest]public IEnumerator TreasuryUsesRealCampaignResourcesAndAdvancesTheRealTurn()
        {
            selection.ActivateTile(Castle);City.SelectPanel(CityPanelKind.Treasury);var before=campaign.State;
            Assert.That(City.PrimaryButton.interactable,Is.True);City.PrimaryButton.onClick.Invoke();yield return null;
            Assert.That(City.IsOpen,Is.True);Assert.That(campaign.State.Turn,Is.EqualTo(before.Turn+1));
            Assert.That(campaign.State.Resources,Is.EqualTo(before.Resources.Add(before.IncomePerTurn)));
            Assert.That(City.BodyText,Does.Contain(campaign.State.Resources.Grain.ToString()));Assert.That(City.Panel,Is.EqualTo(CityPanelKind.Treasury));
        }
        [UnityTest]public IEnumerator MarchFromCityUsesExistingMovementAndReturnsToMap()
        {
            var before=campaign.Army;var resources=campaign.State.Resources;selection.ActivateTile(Castle);City.SelectPanel(CityPanelKind.Orders);
            Assert.That(City.PrimaryButton.interactable,Is.True);Assert.That(City.SendArmy(),Is.True);yield return null;
            Assert.That(City.IsOpen,Is.False);Assert.That(campaign.Army.Position,Is.Not.EqualTo(before.Position));Assert.That(campaign.Army.RemainingMovement,Is.LessThan(before.RemainingMovement));
            Assert.That(campaign.State.Resources,Is.EqualTo(resources));Assert.That(campaign.MarchDestination,Is.EqualTo(Castle.Cell.Coordinates));
        }
        [UnityTest]public IEnumerator OtherOwnerIsInspectionOnly()
        {
            settlements.enabled=false;var config=new SerializedObject(settlements);var property=config.FindProperty("settlements");
            clonedDefinition=Object.Instantiate((CampaignSettlementDefinition)property.GetArrayElementAtIndex(0).objectReferenceValue);
            var data=new SerializedObject(clonedDefinition);data.FindProperty("ownerFactionId").stringValue="otro-reino";data.FindProperty("ownerDisplayName").stringValue="Otro reino";data.ApplyModifiedPropertiesWithoutUndo();
            property.GetArrayElementAtIndex(0).objectReferenceValue=clonedDefinition;config.ApplyModifiedPropertiesWithoutUndo();settlements.enabled=true;yield return null;yield return null;
            selection.ActivateTile(Castle);var before=campaign.State;City.SelectPanel(CityPanelKind.Treasury);
            Assert.That(City.PrimaryButton.interactable,Is.False);City.CompleteTurn();Assert.That(campaign.State,Is.SameAs(before));Assert.That(City.SendArmy(),Is.False);Assert.That(City.IsOpen,Is.True);
        }
        [UnityTest]public IEnumerator BoardRegenerationClosesCityAndRemovesPreviewObjects()
        {
            int mask=Camera.main.cullingMask;selection.ActivateTile(Castle);settlements.GetComponent<CampaignBoardPreview>().RegenerateBoard();yield return null;yield return null;
            Assert.That(City.IsOpen,Is.False);Assert.That(settlements.Ready,Is.True);Assert.That(Camera.main.cullingMask,Is.EqualTo(mask));
            Assert.That(Object.FindObjectsByType<CityBuildingView>(FindObjectsSortMode.None),Is.Empty);Assert.That(settlements.Views.Count,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator OrbitAndBuildingFocusOnlyAffectPreviewCamera()
        {
            selection.ActivateTile(Castle);var mainPosition=Camera.main.transform.position;var mainRotation=Camera.main.transform.rotation;var previousPreview=City.PreviewCamera.transform.position;
            City.Rotate(40);City.ChangeZoom(-1);Assert.That(City.PreviewCamera.transform.position,Is.Not.EqualTo(previousPreview));
            CompleteMarket();City.FocusKind(CityBuildingKind.Market);Assert.That(City.Panel,Is.EqualTo(CityPanelKind.Buildings));Assert.That(City.BodyText,Does.Contain("mercado"));
            Physics.SyncTransforms();var market=City.PreviewCamera.transform.parent.GetComponentsInChildren<CityBuildingView>().First(b=>b.Kind==CityBuildingKind.Market);
            Assert.That(Physics.Raycast(City.PreviewCamera.ScreenPointToRay(City.PreviewCamera.WorldToScreenPoint(market.Bounds.center)),out var hit,80,1<<30),Is.True);
            Assert.That(hit.collider.GetComponentInParent<CityBuildingView>(),Is.Not.Null);Assert.That(selection.ResolveHit(hit.collider),Is.Null);
            City.ResetView();Assert.That(City.ZoomDistance,Is.EqualTo(CityManagementView.DefaultDistance));Assert.That(Camera.main.transform.position,Is.EqualTo(mainPosition));Assert.That(Camera.main.transform.rotation,Is.EqualTo(mainRotation));yield return null;
        }
        [UnityTest]public IEnumerator PerspectiveZoomIsLimitedAndResidentsStayInsideTheTown()
        {
            selection.ActivateTile(Castle);Assert.That(City.PreviewCamera.orthographic,Is.False);
            City.ChangeZoom(-1000);Assert.That(City.ZoomDistance,Is.EqualTo(CityManagementView.MinimumDistance));
            City.ChangeZoom(1000);Assert.That(City.ZoomDistance,Is.EqualTo(CityManagementView.MaximumDistance));
            Assert.That(City.Life,Is.Not.Null);Assert.That(City.Life.Residents,Is.EqualTo(4));Assert.That(City.Life.Animals,Is.EqualTo(2));
            var before=City.Life.Positions();for(int i=0;i<100;i++)City.Life.Advance(.05f);var after=City.Life.Positions();
            Assert.That(before.Where((p,i)=>Vector3.Distance(p,after[i])>.01f).Count(),Is.GreaterThan(2));
            foreach(var position in after){Assert.That(Mathf.Abs(position.x),Is.LessThan(4.3f));Assert.That(position.z,Is.InRange(-4f,1f));}
            var elapsed=City.Life.Elapsed;City.Close();yield return null;Assert.That(Object.FindObjectsByType<CityAmbientLife>(FindObjectsSortMode.None),Is.Empty);
        }

        [UnityTest]public IEnumerator CompleteTownStaysInsideFrameAtEveryOrbitAngleAndBuildingFocus()
        {
            selection.ActivateTile(Castle);CompleteMarket();var stage=City.PreviewCamera.transform.parent;
            foreach(var kind in new[]{CityBuildingKind.Market,CityBuildingKind.Fortress}) {
                City.FocusKind(kind);City.ChangeZoom(-1000);
                for(int angle=0;angle<12;angle++) {
                    City.Rotate(30);
                    foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>()) {
                        if(renderer.name=="Terreno de la villa")continue;var bounds=renderer.bounds;
                        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                            var p=City.PreviewCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                            Assert.That(p.z,Is.GreaterThan(0),renderer.name);Assert.That(p.x,Is.InRange(.035f,.965f),renderer.name);Assert.That(p.y,Is.InRange(.035f,.965f),renderer.name);
                        }
                    }
                }
            }
            yield return null;
        }

        private string TownId=>settlements.Registry.Settlements.Single().Id;
        private void MarkAsLegacy(Fulgor.Core.Campaign.CampaignSaveData payload,int version)
        {
            var map=Fulgor.Core.Map.ProceduralMapGenerator.Generate(24,16,12345);
            payload.context=Fulgor.Core.Campaign.CampaignSaveData.ScenarioSignature(map,selection.Routes,campaign.Mission);
            payload.version=version;payload.settlementOwners=null;
        }
        private void StartProject(string id)
        {
            for(int turn=0;turn<30;turn++) {
                if(City.Construct(id))return;
                campaign.CompleteTurn();
            }
            Assert.Fail("No se pudo financiar la obra "+id);
        }
        private void CompleteMarket()
        {
            StartProject("market");City.CompleteTurn();City.CompleteTurn();
            Assert.That(campaign.CityState(TownId).Has("market"),Is.True);
        }
        [UnityTest]public IEnumerator BuildingsAndActivityAppearOnlyWhenConstructionCompletes()
        {
            selection.ActivateTile(Castle);var stage=City.PreviewCamera.transform.parent;
            Assert.That(City.BuildingCount,Is.EqualTo(3));Assert.That(stage.GetComponentsInChildren<CityBuildingView>().Count(b=>b.Kind==CityBuildingKind.Market),Is.Zero);
            Assert.That(stage.Find("Ambiente de la villa/Puesto de mercado 0").gameObject.activeInHierarchy,Is.False);
            var initial=campaign.State;Assert.That(City.Construct("market"),Is.False);Assert.That(campaign.State,Is.SameAs(initial));
            StartProject("market");var paid=campaign.State.Resources;Assert.That(City.BuildingCount,Is.EqualTo(3));
            var site=stage.GetComponent<CityConstructionSite>();Assert.That(site.ProjectId,Is.EqualTo("market"));Assert.That(site.Phase,Is.EqualTo(1));
            Assert.That(site.Model.activeInHierarchy,Is.True);Assert.That(site.Model.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(3));
            Assert.That(City.Construct("market"),Is.False);Assert.That(City.Construct("well"),Is.False);Assert.That(campaign.State.Resources,Is.EqualTo(paid));
            City.CompleteTurn();Assert.That(City.BuildingCount,Is.EqualTo(3));Assert.That(site.Phase,Is.EqualTo(2));
            City.CompleteTurn();yield return null;Assert.That(site.Model,Is.Null);Assert.That(site.ProjectId,Is.Null);
            Assert.That(City.BuildingCount,Is.EqualTo(4));Assert.That(City.Life.Residents,Is.EqualTo(6));Assert.That(City.Life.Animals,Is.EqualTo(5));
            Assert.That(stage.Find("Ambiente de la villa/Puesto de mercado 0").gameObject.activeInHierarchy,Is.True);
            City.Close();yield return null;selection.ActivateTile(Castle);Assert.That(City.BuildingCount,Is.EqualTo(4));
        }
        [UnityTest]public IEnumerator SavedCityKeepsPendingWorkAndNewGameStartsSmallAgain()
        {
            selection.ActivateTile(Castle);StartProject("market");int due=campaign.CityState(TownId).CompletionTurn;var paid=campaign.State.Resources;
            campaign.SaveCampaign();City.CompleteTurn();City.CompleteTurn();Assert.That(City.BuildingCount,Is.EqualTo(4));
            campaign.LoadCampaign();yield return null;
            Assert.That(campaign.CityState(TownId).PendingId,Is.EqualTo("market"));Assert.That(campaign.CityState(TownId).CompletionTurn,Is.EqualTo(due));
            Assert.That(City.BuildingCount,Is.EqualTo(3));Assert.That(campaign.State.Resources,Is.EqualTo(paid));
            City.CompleteTurn();City.CompleteTurn();StartProject("house-3");City.CompleteTurn();Assert.That(City.BuildingCount,Is.EqualTo(5));
            campaign.NewGame();yield return null;yield return null;selection.ActivateTile(Castle);Assert.That(City.BuildingCount,Is.EqualTo(3));
        }
        [UnityTest]public IEnumerator ConstructionPhasePersistsAcrossReopenAndLoad()
        {
            selection.ActivateTile(Castle);StartProject("market");City.CompleteTurn();campaign.SaveCampaign();
            Assert.That(City.PreviewCamera.transform.parent.GetComponent<CityConstructionSite>().Phase,Is.EqualTo(2));
            City.Close();yield return null;selection.ActivateTile(Castle);
            var site=City.PreviewCamera.transform.parent.GetComponent<CityConstructionSite>();Assert.That(site.Phase,Is.EqualTo(2));
            City.CompleteTurn();Assert.That(site.Model,Is.Null);campaign.LoadCampaign();yield return null;
            Assert.That(site.Phase,Is.EqualTo(2));Assert.That(site.Model.activeInHierarchy,Is.True);
            City.CompleteTurn();Assert.That(site.Model,Is.Null);
        }
        [UnityTest]public IEnumerator EveryProjectHasAVisibleSiteAtItsOwnFootprintAndReplacesItOnCompletion()
        {
            selection.ActivateTile(Castle);var stage=City.PreviewCamera.transform.parent;
            foreach(var project in Fulgor.Core.Campaign.CityProjects.All) {
                StartProject(project.Id);var site=stage.GetComponent<CityConstructionSite>();
                var template=stage.GetComponentsInChildren<CityBuildingView>(true).Single(b=>b.ConstructionId==project.Id);
                Assert.That(template.gameObject.activeSelf,Is.False);Assert.That(site.ProjectId,Is.EqualTo(project.Id));
                Assert.That(site.Model.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.vertexCount>0),Is.True);
                var distance=site.Model.transform.position-template.transform.position;distance.y=0;Assert.That(distance.magnitude,Is.LessThan(.5f),project.Id);
                Assert.That(site.Model.GetComponentsInChildren<Collider>(),Is.Empty);
                for(int turn=0;turn<project.Turns;turn++) {
                    City.ChangeZoom(-1000);
                    for(int angle=0;angle<4;angle++) {
                        City.Rotate(90);
                        foreach(var renderer in site.Model.GetComponentsInChildren<MeshRenderer>()) {
                            var bounds=renderer.bounds;
                            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                                var point=City.PreviewCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                                Assert.That(point.z,Is.GreaterThan(0));Assert.That(point.x,Is.InRange(.025f,.975f));Assert.That(point.y,Is.InRange(.025f,.975f));
                            }
                        }
                    }
                    City.CompleteTurn();
                }
                Assert.That(site.Model,Is.Null);Assert.That(template.gameObject.activeSelf,Is.True);
                yield return null;
            }
        }

        [UnityTest]public IEnumerator DamagedConstructionSavePreservesTheWholeCurrentCampaign()
        {
            selection.ActivateTile(Castle);StartProject("market");campaign.SaveCampaign();
            var payload=JsonUtility.FromJson<Fulgor.Core.Campaign.CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            payload.cities[0].completed=new[]{"unknown-building"};
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(payload));
            var state=campaign.State;var army=campaign.Army;var city=campaign.CityState(TownId);
            LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("No se pudo cargar: Invalid completed building"));
            campaign.LoadCampaign();yield return null;
            Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.Army,Is.SameAs(army));Assert.That(campaign.CityState(TownId),Is.SameAs(city));
            Assert.That(City.BuildingCount,Is.EqualTo(3));
        }

        [UnityTest]public IEnumerator InspectionCannotBuildAndUnknownSettlementCannotSpendResources()
        {
            selection.ActivateTile(Castle);campaign.CompleteTurn();var before=campaign.State;
            Assert.That(campaign.TryConstruct("missing-town","market"),Is.False);Assert.That(campaign.State,Is.SameAs(before));
            City.Close();yield return null;settlements.enabled=false;var config=new SerializedObject(settlements);var property=config.FindProperty("settlements");
            clonedDefinition=Object.Instantiate((CampaignSettlementDefinition)property.GetArrayElementAtIndex(0).objectReferenceValue);
            var data=new SerializedObject(clonedDefinition);data.FindProperty("ownerFactionId").stringValue="otro-reino";data.ApplyModifiedPropertiesWithoutUndo();
            property.GetArrayElementAtIndex(0).objectReferenceValue=clonedDefinition;config.ApplyModifiedPropertiesWithoutUndo();settlements.enabled=true;yield return null;yield return null;
            selection.ActivateTile(Castle);Assert.That(City.Construct("market"),Is.False);Assert.That(campaign.TryConstruct(TownId,"market"),Is.False);
        }

        [UnityTest]public IEnumerator CompactFinishPreservesTemplateGeometrySourceMaterialsAndCameraLimits()
        {
            const string path="Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var sourceRenderers=source.GetComponentsInChildren<MeshRenderer>(true);
            var originals=sourceRenderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
            var serialized=originals.Select(EditorJsonUtility.ToJson).ToArray();
            selection.ActivateTile(Castle);var stage=City.PreviewCamera.transform.parent;
            var finish=stage.GetComponent<CityVisualFinish>();Assert.That(finish,Is.Not.Null);Assert.That(finish.MaterialCount,Is.GreaterThan(10));
            Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Fulgor/City/Surface")),Is.False);
            Assert.That(ShaderUtil.ShaderHasError(Shader.Find("Fulgor/City/Ground")),Is.False);
            var originalBuildings=source.GetComponentsInChildren<CityBuildingView>(true);var buildings=stage.GetComponentsInChildren<CityBuildingView>(true);
            foreach(var original in originalBuildings) {
                var instance=buildings.Single(b=>b.ConstructionId==original.ConstructionId);
                Assert.That(instance.transform.localPosition,Is.EqualTo(original.transform.localPosition));Assert.That(instance.transform.localScale,Is.EqualTo(original.transform.localScale));
                Assert.That(instance.transform.localRotation,Is.EqualTo(original.transform.localRotation));
                Assert.That(instance.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh),Is.EqualTo(original.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh)));
            }
            Assert.That(CityManagementView.MinimumDistance,Is.EqualTo(8.5f));Assert.That(CityManagementView.MaximumDistance,Is.EqualTo(14.5f));
            var castleMaterial=stage.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).First(m=>m!=null&&m.name.StartsWith("M_CastillaV11LOD ·"));
            var originalCastle=originals.First(m=>m.name=="M_CastillaV11LOD");
            Assert.That(castleMaterial,Is.Not.SameAs(originalCastle));Assert.That(castleMaterial.GetTexture("_BaseMap"),Is.SameAs(originalCastle.GetTexture("_BaseMap")));
            Assert.That(castleMaterial.GetTexture("_BumpMap"),Is.SameAs(originalCastle.GetTexture("_BumpMap")));
            City.Close();yield return null;
            for(int i=0;i<originals.Length;i++)Assert.That(EditorJsonUtility.ToJson(originals[i]),Is.EqualTo(serialized[i]),originals[i].name);
            Assert.That(finish==null,Is.True);Assert.That(Object.FindObjectsByType<CityVisualFinish>(FindObjectsSortMode.None).All(f=>f.GetComponent<CampaignSettlementView>()!=null),Is.True);
        }

        [UnityTest]public IEnumerator BakedHouseOcclusionIsBoundAgainAfterReopeningCity()
        {
            for(int opening=0;opening<2;opening++) {
                selection.ActivateTile(Castle);yield return null;int count=0;
                foreach(var renderer in City.PreviewCamera.transform.parent.GetComponentsInChildren<MeshRenderer>()) {
                    var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
                    if(properties.GetFloat("_UseHouseAO")>.5f){Assert.That(properties.GetTexture("_HouseAO"),Is.Not.Null);count++;}
                }
                Assert.That(count,Is.EqualTo(2));City.Close();yield return null;
            }
        }

        [UnityTest]public IEnumerator DedicatedEntryButtonOpensCityAndOrdinaryTilesRemainOnMap()
        {
            settlements.LocateFirst();Assert.That(City==null||!City.IsOpen,Is.True);Assert.That(settlements.VisitButton.interactable,Is.True);
            settlements.VisitButton.onClick.Invoke();Assert.That(City.IsOpen,Is.True);City.Close();yield return null;
            var other=settlements.GetComponentsInChildren<HexTileView>().First(t=>t!=Castle);Assert.That(selection.ActivateTile(other),Is.True);Assert.That(City.IsOpen,Is.False);
        }

        [UnityTest]public IEnumerator CompletedMarketChangesIncomeAndMaintenanceStartingWithNextFullTurn()
        {
            selection.ActivateTile(Castle);StartProject("market");var baseline=campaign.TerritoryIncome;
            var paid=campaign.State.Resources;
            Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(baseline));
            Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(default(Fulgor.Core.Campaign.CampaignResources)));
            City.CompleteTurn();Assert.That(campaign.State.Resources,Is.EqualTo(paid.Add(baseline)));
            var beforeCompletion=campaign.State;
            City.CompleteTurn();
            Assert.That(campaign.State.Resources,Is.EqualTo(beforeCompletion.ProjectedResources));
            Assert.That(campaign.State.IncomePerTurn.Gold,Is.EqualTo(baseline.Gold+8));
            Assert.That(campaign.State.ExpensesPerTurn.Grain,Is.EqualTo(2));
            Assert.That(City.CityNoticeText,Does.Contain("OBRA TERMINADA").And.Contain("Mercado"));
            City.SelectPanel(CityPanelKind.Treasury);
            Assert.That(City.BodyText,Does.Contain("PREVISIÓN AL TURNO").And.Contain("GASTOS DE MANTENIMIENTO"));
            var forecast=campaign.State.ProjectedResources;City.CompleteTurn();
            Assert.That(campaign.State.Resources,Is.EqualTo(forecast));
            Assert.That(campaign.State.IncomePerTurn.Gold,Is.EqualTo(baseline.Gold+8));
            City.Close();yield return null;selection.ActivateTile(Castle);
            Assert.That(campaign.State.IncomePerTurn.Gold,Is.EqualTo(baseline.Gold+8));
        }

        [UnityTest]public IEnumerator LoadingRebuildsCityEconomyWithoutDuplicatingEffectsOrCompletingPendingWork()
        {
            selection.ActivateTile(Castle);CompleteMarket();StartProject("well");
            var saved=campaign.State;var pending=campaign.CityState(TownId);campaign.SaveCampaign();
            City.CompleteTurn();Assert.That(campaign.CityState(TownId).Has("well"),Is.True);
            for(int load=0;load<2;load++) {
                campaign.LoadCampaign();yield return null;
                Assert.That(campaign.State.Resources,Is.EqualTo(saved.Resources));
                Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(saved.IncomePerTurn));
                Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(saved.ExpensesPerTurn));
                Assert.That(campaign.CityState(TownId).PendingId,Is.EqualTo("well"));
                Assert.That(campaign.CityState(TownId).CompletionTurn,Is.EqualTo(pending.CompletionTurn));
            }
            City.CompleteTurn();
            Assert.That(campaign.State.Resources,Is.EqualTo(saved.ProjectedResources));
            Assert.That(campaign.State.IncomePerTurn.Grain,Is.EqualTo(campaign.TerritoryIncome.Grain+6));
            Assert.That(campaign.State.IncomePerTurn.Gold,Is.EqualTo(campaign.TerritoryIncome.Gold+8));
            Assert.That(campaign.State.ExpensesPerTurn.Gold,Is.EqualTo(1));
            campaign.NewGame();yield return null;yield return null;
            Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(campaign.TerritoryIncome));
            Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(default(Fulgor.Core.Campaign.CampaignResources)));
        }

        [UnityTest]public IEnumerator LegacySaveDoesNotKeepBonusesFromTheCampaignBeingReplaced()
        {
            selection.ActivateTile(Castle);CompleteMarket();campaign.SaveCampaign();
            var payload=JsonUtility.FromJson<Fulgor.Core.Campaign.CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            MarkAsLegacy(payload,2);payload.cities=null;PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(payload));
            campaign.LoadCampaign();yield return null;
            Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(campaign.TerritoryIncome));
            Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(default(Fulgor.Core.Campaign.CampaignResources)));
            Assert.That(City.BuildingCount,Is.EqualTo(3));
        }

        [UnityTest]public IEnumerator ForeignCityBuildingsDoNotContributeToThePlayerEconomy()
        {
            selection.ActivateTile(Castle);CompleteMarket();campaign.SaveCampaign();City.Close();yield return null;
            var legacy=JsonUtility.FromJson<Fulgor.Core.Campaign.CampaignSaveData>(PlayerPrefs.GetString(SaveKey));
            MarkAsLegacy(legacy,3);PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(legacy));
            settlements.enabled=false;var config=new SerializedObject(settlements);var property=config.FindProperty("settlements");
            clonedDefinition=Object.Instantiate((CampaignSettlementDefinition)property.GetArrayElementAtIndex(0).objectReferenceValue);
            var data=new SerializedObject(clonedDefinition);data.FindProperty("ownerFactionId").stringValue="otro-reino";data.ApplyModifiedPropertiesWithoutUndo();
            property.GetArrayElementAtIndex(0).objectReferenceValue=clonedDefinition;config.ApplyModifiedPropertiesWithoutUndo();
            settlements.enabled=true;yield return null;yield return null;
            campaign.LoadCampaign();yield return null;
            Assert.That(campaign.CityState(TownId).Has("market"),Is.True);
            Assert.That(campaign.State.IncomePerTurn,Is.EqualTo(campaign.TerritoryIncome));
            Assert.That(campaign.State.ExpensesPerTurn,Is.EqualTo(default(Fulgor.Core.Campaign.CampaignResources)));
            Assert.That(campaign.TryConstruct(TownId,"well"),Is.False);
        }

        [UnityTest]public IEnumerator MultipleArmyTurnAlsoCompletesBuildingsAndAppliesTheirEconomy()
        {
            var cells=settlements.GetComponentsInChildren<HexTileView>().Select(t=>t.Cell).ToArray();
            var map=new Fulgor.Core.Map.GeneratedMap(24,16,12345,cells);
            var registry=new Fulgor.Core.Campaign.CampaignArmyRegistry(selection.Routes,
                System.Array.Empty<Fulgor.Core.Campaign.CampaignArmyPlacement>());
            campaign.Initialize(campaign.GetComponent<CampaignSelectionPanel>(),map,null,registry,"castilla");
            selection.ActivateTile(Castle);CompleteMarket();
            Assert.That(campaign.ArmyRegistry,Is.Not.Null);
            Assert.That(campaign.State.IncomePerTurn.Gold,Is.EqualTo(campaign.TerritoryIncome.Gold+8));
            Assert.That(campaign.State.ExpensesPerTurn.Grain,Is.EqualTo(2));
            var forecast=campaign.State.ProjectedResources;City.CompleteTurn();
            Assert.That(campaign.State.Resources,Is.EqualTo(forecast));yield return null;
        }

        [UnityTest]public IEnumerator TreasuryBreakdownCanBeScrolledAndChangingPanelReturnsToTheTop()
        {
            selection.ActivateTile(Castle);City.SelectPanel(CityPanelKind.Treasury);yield return null;
            var scroll=City.Viewport.GetComponentInParent<Canvas>().GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            // Exercise a compact window, independently of the test runner's Game view size.
            scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,260);
            City.SelectPanel(CityPanelKind.Treasury);yield return null;
            Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
            Assert.That(scroll.verticalScrollbar.gameObject.activeInHierarchy,Is.True);
            scroll.verticalNormalizedPosition=0;yield return null;
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.01));
            City.SelectPanel(CityPanelKind.Overview);City.SelectPanel(CityPanelKind.Treasury);yield return null;
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.01));
        }
    }
}
