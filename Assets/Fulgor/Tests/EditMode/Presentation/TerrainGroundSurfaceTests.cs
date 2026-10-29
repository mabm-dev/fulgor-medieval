using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fulgor.Presentation.Tests.Map
{
    public sealed class TerrainGroundSurfaceTests
    {
        private readonly List<Object> created = new();
        private Mesh Ground(float height=.12f,float slope=.2f,bool canopy=true)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            foreach(var p in new[] {new Vector2(-.84f,-1),new Vector2(-.84f,1),new Vector2(.84f,1),new Vector2(.84f,-1)})
                vertices.Add(new Vector3(p.x,height+p.x*slope,p.y));
            triangles.AddRange(new[] {0,1,2,0,2,3});
            if(canopy) {
                vertices.AddRange(new[] {new Vector3(-.3f,2,-.3f),new Vector3(-.3f,2,.3f),new Vector3(.3f,2,.3f),new Vector3(.3f,2,-.3f)});
                triangles.AddRange(new[] {4,5,6,4,6,7});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();created.Add(mesh);return mesh;
        }
        private GameObject Root(string label) { var go=new GameObject(label);created.Add(go);return go; }
        [TearDown] public void Cleanup() { for(int i=created.Count-1;i>=0;i--) if(created[i]!=null)Object.DestroyImmediate(created[i]);created.Clear(); }

        [Test] public void SamplesSlopedGroundInsteadOfDetachedCanopy()
        {
            var mesh=Ground();var tile=Root("Ground");var surface=new TerrainGroundSurface(mesh);
            Assert.That(mesh.bounds.max.y,Is.EqualTo(2));
            Assert.That(surface.TrySample(tile.transform,new Vector3(.2f,10,.1f),out var point,out var normal,out var gap),Is.True);
            Assert.That(point.y,Is.EqualTo(.16f).Within(.0001));Assert.That(gap,Is.Zero);
            Assert.That(Vector3.Angle(normal,new Vector3(-.2f,1,0)),Is.LessThan(.01));
        }
        [Test] public void SamplingRespectsTileTransformAndRejectsFarOutside()
        {
            var tile=Root("Rotated ground");tile.transform.SetPositionAndRotation(new Vector3(3,4,5),Quaternion.Euler(0,60,0));tile.transform.localScale=new Vector3(2,1.5f,2);
            var surface=new TerrainGroundSurface(Ground());var world=tile.transform.TransformPoint(new Vector3(.2f,0,.1f));
            Assert.That(surface.TrySample(tile.transform,world,out var point,out _,out _),Is.True);
            Assert.That(point.y,Is.EqualTo(4.24f).Within(.0001));
            Assert.That(surface.TrySample(tile.transform,world+Vector3.right*20,out _,out _,out _),Is.False);
        }
        [Test] public void SmallGapUsesNearestGroundWithoutFallingToZero()
        {
            var tile=Root("Ground");var surface=new TerrainGroundSurface(Ground());
            Assert.That(surface.TrySample(tile.transform,new Vector3(.86f,0,0),out var point,out _,out var gap),Is.True);
            Assert.That(gap,Is.EqualTo(.02f).Within(.0001));Assert.That(point.y,Is.EqualTo(.288f).Within(.0001));
            Assert.That(surface.TrySample(tile.transform,new Vector3(1,0,0),out _,out _,out _),Is.False);
        }
        [Test] public void EmptyMeshHasNoInventedGround()
        {
            var mesh=new Mesh();created.Add(mesh);var tile=Root("Empty");
            Assert.That(new TerrainGroundSurface(mesh).TrySample(tile.transform,Vector3.zero,out _,out _,out _),Is.False);
        }
        private (CampaignArmyMarker marker,MapRouteGraph graph) Army()
        {
            var root=Root("Board");var owner=root.AddComponent<CampaignBoardSelection>();
            var cells=new MapCell[3];
            for(int i=0;i<3;i++) {
                var coordinate=new HexCoordinates(i,0);cells[i]=new MapCell(coordinate,TerrainType.Plain,false);
                var tile=new GameObject("Tile");tile.transform.SetParent(root.transform);tile.transform.position=HexWorldLayout.ToWorld(coordinate);
                tile.AddComponent<MeshFilter>();tile.AddComponent<MeshRenderer>();tile.AddComponent<MeshCollider>().sharedMesh=Ground(.12f+.2f*tile.transform.position.x);
                tile.AddComponent<HexTileView>().Initialize(cells[i],owner,Color.white);
            }
            var graph=new MapRouteGraph(new GeneratedMap(3,1,1,cells));owner.InitializeWithRoutes(null,graph);
            var marker=root.AddComponent<CampaignArmyMarker>();marker.Initialize(owner);return(marker,graph);
        }
        [Test] public void MarkerMovesAcrossGroundAndGapsWithoutChangingLogicalState()
        {
            var (marker,graph)=Army();var initial=new ArmyMovementState(graph,new HexCoordinates(0,0),6);
            marker.SetArmy(initial);Assert.That(marker.VisualPosition.y,Is.EqualTo(.126f).Within(.001));
            initial.TryMove(new HexCoordinates(2,0),out var next,out var path,out _);marker.SetArmy(next,path.Cells);
            float previousX=marker.VisualPosition.x;
            for(int i=0;i<160 && marker.IsMoving;i++) {
                marker.AdvanceVisual(.01f);var p=marker.VisualPosition;
                Assert.That(p.x,Is.GreaterThanOrEqualTo(previousX));previousX=p.x;
                Assert.That(p.y,Is.InRange(.12f+.2f*p.x,.18f+.2f*p.x));
                marker.SetArmy(next); // UI refresh cannot restart the animation.
            }
            Assert.That(marker.IsMoving,Is.False);Assert.That(marker.VisualPosition.x,Is.EqualTo(HexWorldLayout.ToWorld(next.Position).x).Within(.001));
            Assert.That(initial.Position,Is.EqualTo(new HexCoordinates(0,0)));Assert.That(next.RemainingMovement,Is.EqualTo(4));
        }
        [Test] public void RapidConsecutiveOrdersQueueTheirAcceptedRoutes()
        {
            var (marker,graph)=Army();var first=new ArmyMovementState(graph,new HexCoordinates(0,0),6);marker.SetArmy(first);
            first.TryMove(new HexCoordinates(1,0),out var middle,out var route1,out _);marker.SetArmy(middle,route1.Cells);marker.AdvanceVisual(.1f);
            float x=marker.VisualPosition.x;
            middle.TryMove(new HexCoordinates(2,0),out var end,out var route2,out _);marker.SetArmy(end,route2.Cells);
            Assert.That(marker.VisualPosition.x,Is.EqualTo(x).Within(.0001));marker.AdvanceVisual(10);
            Assert.That(marker.VisualPosition.x,Is.EqualTo(HexWorldLayout.ToWorld(end.Position).x).Within(.0001));Assert.That(marker.IsMoving,Is.False);
        }
        [Test] public void HiddenArmyCancelsMovementAndFreshPlacementIsGrounded()
        {
            var (marker,graph)=Army();var first=new ArmyMovementState(graph,new HexCoordinates(0,0),6);marker.SetArmy(first);
            first.TryMove(new HexCoordinates(2,0),out var end,out var path,out _);marker.SetArmy(end,path.Cells);marker.SetArmy(null);
            Assert.That(marker.IsMoving,Is.False);Assert.That(marker.IsVisible,Is.False);
            marker.SetArmy(first);Assert.That(marker.VisualPosition.y,Is.EqualTo(.126f).Within(.001));
        }
        [Test] public void MovementWithoutAnAcceptedRouteSnapsInsteadOfCrossingUnknownCells()
        {
            var (marker,graph)=Army();marker.SetArmy(new ArmyMovementState(graph,new HexCoordinates(0,0),6));
            marker.SetArmy(new ArmyMovementState(graph,new HexCoordinates(2,0),6));
            Assert.That(marker.IsMoving,Is.False);Assert.That(marker.VisualPosition.x,Is.EqualTo(HexWorldLayout.ToWorld(new HexCoordinates(2,0)).x).Within(.001));
        }
        [Test] public void StationaryMarkerRegroundsWhenTerrainMeshIsReplaced()
        {
            var (marker,graph)=Army();marker.SetArmy(new ArmyMovementState(graph,new HexCoordinates(0,0),6));
            var collider=marker.GetComponentInChildren<HexTileView>().GetComponent<MeshCollider>();collider.sharedMesh=Ground(.4f);
            marker.AdvanceVisual(0);Assert.That(marker.VisualPosition.y,Is.EqualTo(.406f).Within(.001));
        }
    }
}
