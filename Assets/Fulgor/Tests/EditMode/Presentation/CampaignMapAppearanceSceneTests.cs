using System.Collections;
using System.Linq;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class CampaignMapAppearanceSceneTests
    {
        private string previous;
        private CampaignBoardPreview board;
        private CampaignMapAppearance appearance;
        [UnitySetUp]public IEnumerator Setup()
        {
            previous=EditorSceneManager.GetActiveScene().path;EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/CampaignPrototype.unity");yield return new EnterPlayMode();yield return null;yield return null;
            board=Object.FindAnyObjectByType<CampaignBoardPreview>();appearance=board.GetComponent<CampaignMapAppearance>();Assert.That(appearance.Ready,Is.True);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(UnityEditor.EditorApplication.isPlaying)yield return new ExitPlayMode();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        }
        [UnityTest]public IEnumerator CampaignArtRetainsCellsCollisionsAndLogicalStateAcrossLodLevels()
        {
            var root=board.transform.Find("Generated Board");var tiles=root.GetComponentsInChildren<HexTileView>();var campaign=board.GetComponent<CampaignTurnController>();var state=campaign.State;var army=campaign.Army;
            var collisions=tiles.Select(t=>t.GetComponent<MeshCollider>().sharedMesh).ToArray();
            Assert.That(tiles.Length,Is.EqualTo(384));Assert.That(appearance.TileCount,Is.EqualTo(384));Assert.That(appearance.WaterLayout,Is.Not.Null);
            Assert.That(appearance.WaterLayout.Kinds.Count,Is.EqualTo(tiles.Count(t=>t.Cell.Terrain==TerrainType.Water)));
            appearance.ForceLevel(0);var meshes=tiles.Select(t=>t.GetComponent<MeshFilter>().sharedMesh).ToArray();appearance.ForceLevel(2);
            int reduced=0;
            for(int i=0;i<tiles.Length;i++) {
                Assert.That(tiles[i].GetComponent<MeshCollider>().sharedMesh,Is.SameAs(collisions[i]));
                if(tiles[i].Cell.Terrain==TerrainType.Water)Assert.That(tiles[i].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(meshes[i]));
                else if(tiles[i].GetComponent<MeshFilter>().sharedMesh!=meshes[i])reduced++;
            }
            Assert.That(reduced,Is.EqualTo(tiles.Count(t=>t.Cell.Terrain!=TerrainType.Water)));
            Assert.That(campaign.State,Is.SameAs(state));Assert.That(campaign.Army,Is.SameAs(army));
            var selection=board.GetComponent<CampaignBoardSelection>();var tile=tiles.First(t=>t.Cell.Terrain==TerrainType.Plain&&!board.GetComponent<CampaignSettlementController>().HasSettlementAt(t.Cell.Coordinates));
            selection.Select(tile);Assert.That(selection.SelectedCell.Value.Coordinates,Is.EqualTo(tile.Cell.Coordinates));Assert.That(tile.HasColorOverride,Is.True);
            Physics.SyncTransforms();Assert.That(tile.GetComponent<MeshCollider>().Raycast(new Ray(tile.transform.position+Vector3.up*4,Vector3.down),out var hit,8),Is.True);Assert.That(selection.ResolveHit(hit.collider),Is.SameAs(tile));
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(board.GetComponent<CampaignBoardDiagnostics>().Metrics.Renderers));yield return null;
        }
        [UnityTest]public IEnumerator ReducedCampaignWaterKeepsSharedEdgesInHeightNormalAndColor()
        {
            var tiles=board.transform.Find("Generated Board").GetComponentsInChildren<HexTileView>().Where(t=>t.Cell.Terrain==TerrainType.Water).ToDictionary(t=>t.Cell.Coordinates);
            int checkedEdges=0;
            foreach(var first in tiles.Values)foreach(var coordinate in first.Cell.Coordinates.Neighbors()) {
                if(!tiles.TryGetValue(coordinate,out var second)||coordinate.Q<first.Cell.Coordinates.Q||coordinate.Q==first.Cell.Coordinates.Q&&coordinate.R<first.Cell.Coordinates.R)continue;
                var direction=(second.transform.position-first.transform.position).normalized;var middle=(first.transform.position+second.transform.position)*.5f;
                var a=Edge(first,middle,direction);var b=Edge(second,middle,direction);Assert.That(a.Count,Is.EqualTo(b.Count));Assert.That(a.Count,Is.GreaterThan(12));
                // Match positions directly: decimal dictionary rounding is unstable at half steps far from the origin.
                foreach(var point in a.Values) {
                    var closest=b.Values.OrderBy(p=>new Vector2(p.point.x-point.point.x,p.point.z-point.point.z).sqrMagnitude).First();
                    Assert.That(Vector2.Distance(new Vector2(point.point.x,point.point.z),new Vector2(closest.point.x,closest.point.z)),Is.LessThan(.00002f));
                    Assert.That(Mathf.Abs(point.point.y-closest.point.y),Is.LessThan(.0001f));Assert.That(Vector3.Distance(point.normal,closest.normal),Is.LessThan(.002f));Assert.That(Vector4.Distance(point.color,closest.color),Is.LessThan(.002f));
                }checkedEdges++;
            }
            Assert.That(checkedEdges,Is.GreaterThan(5));yield return null;
        }
        private static System.Collections.Generic.Dictionary<Vector2Int,(Vector3 point,Vector3 normal,Color color)> Edge(HexTileView tile,Vector3 middle,Vector3 direction)
        {
            var result=new System.Collections.Generic.Dictionary<Vector2Int,(Vector3,Vector3,Color)>();var mesh=tile.GetComponent<MeshFilter>().sharedMesh;var points=mesh.vertices;var normals=mesh.normals;var colors=mesh.colors;
            for(int i=0;i<points.Length;i++) {
                var world=tile.transform.TransformPoint(points[i]);if(Mathf.Abs(Vector3.Dot(world-middle,direction))>.00005f)continue;
                var key=new Vector2Int(Mathf.RoundToInt(world.x*10000),Mathf.RoundToInt(world.z*10000));result[key]=(world,tile.transform.TransformDirection(normals[i]),colors[i]);
            }
            return result;
        }
        [UnityTest]public IEnumerator RegenerationReappliesArtAndReleasesItsPreviousMaterialsAndWaterMeshes()
        {
            var oldRoot=board.transform.Find("Generated Board");var water=oldRoot.GetComponentsInChildren<HexTileView>().First(t=>t.Cell.Terrain==TerrainType.Water);
            var oldMaterial=water.GetComponent<MeshRenderer>().sharedMaterial;var oldMesh=water.GetComponent<MeshFilter>().sharedMesh;var oldLayout=appearance.WaterLayout;
            board.RegenerateBoard();Assert.That(appearance.Ready,Is.True);Assert.That(appearance.TileCount,Is.EqualTo(384));Assert.That(appearance.WaterLayout,Is.Not.SameAs(oldLayout));
            Assert.That(board.transform.Find("Generated Board"),Is.Not.SameAs(oldRoot));yield return null;yield return null;
            Assert.That(oldMaterial==null,Is.True);Assert.That(oldMesh==null,Is.True);
            var settlements=board.GetComponent<CampaignSettlementController>();Assert.That(settlements.Ready,Is.True);Assert.That(settlements.Views.Count,Is.EqualTo(1));
            Assert.That(board.transform.Find("Generated Board").GetComponentsInChildren<MeshCollider>().Length,Is.EqualTo(384));Assert.That(board.GetComponent<CampaignTurnController>().State.Turn,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator CityAndMapUseTheSameFortressAtlasAndTintWithoutChangingAssets()
        {
            var settings=AssetDatabase.LoadAssetAtPath<CampaignMapAppearanceSettings>("Assets/Fulgor/MapAppearance/CastillaMapAppearance.asset");settings.Validate();
            Assert.That(settings.SelectionShader,Is.Not.Null);Assert.That(settings.SelectionShader.name,Is.EqualTo("Universal Render Pipeline/Unlit"));
            var source=settings.TerrainMaterial((int)TerrainType.Plain);string before=EditorJsonUtility.ToJson(source);
            var tiles=board.transform.Find("Generated Board").GetComponentsInChildren<HexTileView>();var plain=tiles.First(t=>t.Cell.Terrain==TerrainType.Plain).GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(plain,Is.Not.SameAs(source));Assert.That(plain.GetTexture("_BaseMap"),Is.SameAs(source.GetTexture("_BaseMap")));
            Assert.That(Vector4.Distance(plain.GetColor("_BaseColor"),CampaignVisualPalette.TerrainTint(TerrainType.Plain)),Is.LessThan(.00001f));
            var settlements=board.GetComponent<CampaignSettlementController>();var castle=settlements.Views.Values.Single();
            var mapMaterial=castle.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).First(m=>m.name.StartsWith("M_CastillaV11LOD ·"));
            settlements.LocateFirst();Assert.That(castle.Selected,Is.True);Assert.That(castle.Tile.HasColorOverride,Is.False);
            Assert.That(Vector4.Distance(mapMaterial.GetColor("_BaseColor"),CampaignVisualPalette.FortressTint),Is.LessThan(.00001f));
            Assert.That(settlements.OpenCityAt(settlements.Registry.Settlements[0].Position),Is.True);
            var city=board.GetComponent<CityManagementView>();var cityMaterial=city.PreviewCamera.transform.parent.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).First(m=>m.name.StartsWith("M_CastillaV11LOD ·"));
            Assert.That(cityMaterial.GetTexture("_BaseMap"),Is.SameAs(mapMaterial.GetTexture("_BaseMap")));Assert.That(cityMaterial.GetTexture("_BumpMap"),Is.SameAs(mapMaterial.GetTexture("_BumpMap")));
            Assert.That(cityMaterial.GetColor("_BaseColor"),Is.EqualTo(mapMaterial.GetColor("_BaseColor")));city.Close();yield return null;
            Assert.That(EditorJsonUtility.ToJson(source),Is.EqualTo(before));
        }
    }
}
