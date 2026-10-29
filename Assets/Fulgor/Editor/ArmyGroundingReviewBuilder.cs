using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Fulgor.Core.Campaign;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class ArmyGroundingReviewBuilder
    {
        const string ScenePath="Assets/Fulgor/Scenes/ArtReview/GroundedArmiesReview.unity";
        const string Running="Fulgor.ArmyGrounding.Running",Output="Fulgor.ArmyGrounding.Output",Done="Fulgor.ArmyGrounding.Done";
        static int frames;static double started;
        static ArmyGroundingReviewBuilder() {if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        [MenuItem("Fulgor/Arte/Revisar huestes sobre relieve")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/TerrainLODReview.unity",OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");
                if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de revision.");
                Directory.CreateDirectory(args[at+1]);Prepare();SessionState.SetString(Output,args[at+1]);
                SessionState.SetBool(Running,true);SessionState.SetBool(Done,false);started=EditorApplication.timeSinceStartup;frames=0;
                EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [Serializable] sealed class Sample {public string terrain;public float previousBoundsHeight,groundedHeight,centerGroundHeight;}
        [Serializable] sealed class Report
        {
            public int checkedLandTiles,movementFrames;
            public bool actualCampaignMoveChecked,movementBudgetUnchangedByAnimation,selectionChecked,lodStable;
            public float maxBasePenetration,maxCenterClearance;
            public Sample[] cases;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            if(SessionState.GetBool(Done,false)&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;
            if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Revision de huestes agotada.");EditorApplication.Exit(1);return;}
            var lod=Object.FindFirstObjectByType<TerrainLodArtReview>();
            if(!EditorApplication.isPlaying||SessionState.GetBool(Done,false)||lod==null||!lod.Ready||++frames<20)return;
            try {
                string output=SessionState.GetString(Output,"");
                var owner=Object.FindFirstObjectByType<CampaignBoardSelection>();var turn=owner.GetComponent<CampaignTurnController>();
                var tiles=owner.GetComponentsInChildren<HexTileView>().ToDictionary(t=>t.Cell.Coordinates);
                var camera=Camera.main;var controls=camera.GetComponent<CampaignCameraController>();if(controls!=null)controls.enabled=false;
                var surfaces=new Dictionary<Mesh,TerrainGroundSurface>();
                TerrainGroundSurface Surface(HexTileView tile) {
                    var mesh=tile.GetComponent<MeshCollider>().sharedMesh;
                    if(!surfaces.TryGetValue(mesh,out var value)){value=new TerrainGroundSurface(mesh);surfaces.Add(mesh,value);}return value;
                }
                var report=new Report();
                // Exercise the actual campaign button path, not just a presentation-only probe.
                var before=turn.Army;
                var target=tiles.Values.Where(t=>t.Cell.Coordinates!=before.Position&&t.Cell.Coordinates!=turn.Mission?.Destination)
                    .First(t=>MapPathfinder.TryFindPath(owner.Routes,before.Position,t.Cell.Coordinates,out var path)&&path.TotalCost>0&&path.TotalCost<=before.RemainingMovement);
                owner.Select(target);turn.MoveToSelectedCell();
                if(turn.Army.Position==before.Position)throw new InvalidOperationException("La orden real no ha movido la hueste.");
                var moving=owner.GetComponentsInChildren<CampaignArmyMarker>().Where(m=>m.IsVisible&&m.IsMoving).ToArray();
                if(moving.Length==0)throw new InvalidOperationException("Falta animacion de la orden aceptada.");
                int remaining=turn.Army.RemainingMovement;
                foreach(var marker in moving) {
                    for(int i=0;i<1000&&marker.IsMoving;i++){marker.AdvanceVisual(.02f);report.movementFrames++;}
                    if(marker.IsMoving)throw new InvalidOperationException("La animacion no llega a destino.");
                    var destination=tiles[turn.Army.Position].transform.position;
                    if(Vector2.Distance(new Vector2(marker.VisualPosition.x,marker.VisualPosition.z),new Vector2(destination.x,destination.z))>.501f)
                        throw new InvalidOperationException("Destino visual incorrecto.");
                }
                report.actualCampaignMoveChecked=true;report.movementBudgetUnchangedByAnimation=remaining==turn.Army.RemainingMovement;
                if(!report.movementBudgetUnchangedByAnimation)throw new InvalidOperationException("La animacion ha cambiado los puntos de movimiento.");
                var testObject=new GameObject("Sonda temporal de apoyo");testObject.transform.SetParent(owner.transform,false);
                var probe=testObject.AddComponent<CampaignArmyMarker>();probe.Initialize(owner);
                var cases=new List<Sample>();
                foreach(var family in tiles.Values.Where(t=>t.Cell.Terrain!=TerrainType.Water).GroupBy(t=>t.Cell.Terrain)) {
                    var tile=family.OrderBy(t=>(t.transform.position-target.transform.position).sqrMagnitude).First();
                    var state=new ArmyMovementState(owner.Routes,tile.Cell.Coordinates,6);probe.SetArmy(state);
                    Surface(tile).TrySample(tile.transform,probe.VisualPosition,out var center,out _,out _);
                    var sample=new Sample{terrain=family.Key.ToString(),previousBoundsHeight=tile.GetComponent<MeshCollider>().bounds.max.y+.08f,groundedHeight=probe.VisualPosition.y,centerGroundHeight=center.y};cases.Add(sample);
                    camera.transform.position=tile.transform.position+new Vector3(2.5f,3,-3.5f);camera.transform.LookAt(tile.transform.position+Vector3.up*.45f);camera.orthographicSize=1.65f;
                    var visual=probe.transform.Find("Hueste provisional");var position=visual.position;position.y=sample.previousBoundsHeight;visual.position=position;
                    Capture(camera,Path.Combine(output,"Hueste_"+family.Key+"_Antes.png"));
                    probe.SetArmy(null);probe.SetArmy(state);Capture(camera,Path.Combine(output,"Hueste_"+family.Key+"_Ahora.png"));
                }
                report.cases=cases.ToArray();
                var forest=tiles.Values.First(t=>t.Cell.Terrain==TerrainType.Forest&&t.Cell.Coordinates.Neighbors().Any(c=>tiles.TryGetValue(c,out var n)&&n.Cell.Terrain==TerrainType.Hill));
                var hill=forest.Cell.Coordinates.Neighbors().Select(c=>tiles.TryGetValue(c,out var n)?n:null).First(t=>t!=null&&t.Cell.Terrain==TerrainType.Hill);
                var startState=new ArmyMovementState(owner.Routes,forest.Cell.Coordinates,6);probe.SetArmy(null);probe.SetArmy(startState);
                if(!startState.TryMove(hill.Cell.Coordinates,out var movedState,out var route,out _))throw new InvalidOperationException("Ruta de revision inaccesible.");
                probe.SetArmy(movedState,route.Cells);
                var focus=(forest.transform.position+hill.transform.position)*.5f;
                camera.transform.position=focus+new Vector3(1,4,-5);camera.transform.LookAt(focus+Vector3.up*.35f);camera.orthographicSize=2.1f;
                for(int i=0;i<16;i++){Capture(camera,Path.Combine(output,"Movimiento_"+i.ToString("D2")+".png"));probe.AdvanceVisual(.08f);}
                if(probe.IsMoving)probe.AdvanceVisual(10);
                // Check the rigid base's support on every land tile, including mountain peaks.
                foreach(var tile in tiles.Values.Where(t=>t.Cell.Terrain!=TerrainType.Water)) {
                    probe.SetArmy(new ArmyMovementState(owner.Routes,tile.Cell.Coordinates,6));
                    var position=probe.VisualPosition;var basis=probe.transform.Find("Hueste provisional/Base");
                    var surface=Surface(tile);
                    if(!surface.TrySample(tile.transform,position,out var center,out _,out _))throw new InvalidOperationException("Suelo sin muestra.");
                    report.maxCenterClearance=Mathf.Max(report.maxCenterClearance,position.y-center.y);
                    for(int i=0;i<8;i++) {
                        float angle=i*Mathf.PI*.25f;var offset=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.325f;
                        if(!surface.TrySample(tile.transform,position+offset,out var ground,out _,out var gap)||gap>0)continue;
                        float baseY=position.y-Vector3.Dot(basis.up,offset)/basis.up.y;
                        report.maxBasePenetration=Mathf.Max(report.maxBasePenetration,ground.y-baseY);
                    }
                    report.checkedLandTiles++;
                }
                if(report.checkedLandTiles!=348||report.maxBasePenetration>.0001f)throw new InvalidOperationException("Apoyo de base incorrecto.");
                var finalPosition=probe.VisualPosition;
                for(int level=0;level<3;level++){lod.ForceLevel(level);probe.AdvanceVisual(0);if(Vector3.Distance(probe.VisualPosition,finalPosition)>.0001f)throw new InvalidOperationException("LOD cambia apoyo.");}
                report.lodStable=true;lod.ForceLevel(-1);
                foreach(var tile in tiles.Values){tile.SetSelected(true);if(!tile.HasColorOverride)throw new InvalidOperationException("Seleccion perdida.");tile.SetSelected(false);}
                report.selectionChecked=true;Object.DestroyImmediate(testObject);
                File.WriteAllText(Path.Combine(output,"revision_huestes.json"),JsonUtility.ToJson(report,true));
                Debug.Log("ARMY_GROUNDING_REVIEW_OK land="+report.checkedLandTiles+" samples="+report.movementFrames);
                SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
