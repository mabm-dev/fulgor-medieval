using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Fulgor.Core.Map;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Fulgor.Editor
{
    [InitializeOnLoad]
    public static class TerrainLodReviewBuilder
    {
        const string Root = "Assets/Fulgor/Art/Terrain/MapLODReview";
        const string Source = "Assets/Fulgor/Art/Terrain/CastillaMapReview";
        const string ScenePath = "Assets/Fulgor/Scenes/ArtReview/TerrainLODReview.unity";
        const string Running = "Fulgor.TerrainLOD.Running", Output = "Fulgor.TerrainLOD.Output", Done = "Fulgor.TerrainLOD.Done";
        static readonly string[] Names = { "Llanura", "Bosque", "Colina", "Montana", "LlanuraFortaleza" };
        static double started;
        static int frames;
        static TerrainLodReviewBuilder() { if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick; }

        [MenuItem("Fulgor/Arte/Revisar LOD de terreno")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepara fuera de Play Mode.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var levels = new Mesh[3][] { new Mesh[5], new Mesh[5], new Mesh[5] };
            for (int i = 0; i < 5; i++)
            {
                levels[0][i] = AssetDatabase.LoadAssetAtPath<Mesh>(Source + "/SM_Terreno_" + Names[i] + "_Unity.asset");
                for (int level = 1; level <= 2; level++)
                {
                    string label = "SM_Terreno_" + Names[i] + "_LOD" + level;
                    string path = Root + "/" + label + ".fbx";
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
                    importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
                    importer.addCollider = false; importer.isReadable = true; importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk; importer.SaveAndReimport();
                    var filter = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>().Single();
                    var matrix = filter.transform.localToWorldMatrix;
                    var mesh = Object.Instantiate(filter.sharedMesh); mesh.name = label + "_Unity";
                    mesh.vertices = mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
                    mesh.normals = mesh.normals.Select(v => matrix.inverse.transpose.MultiplyVector(v).normalized).ToArray();
                    mesh.tangents = mesh.tangents.Select(v => {
                        var direction = matrix.MultiplyVector(new Vector3(v.x,v.y,v.z)).normalized;
                        return new Vector4(direction.x,direction.y,direction.z,v.w * (matrix.determinant < 0 ? -1 : 1));
                    }).ToArray();
                    if (matrix.determinant < 0)
                        for (int sub = 0; sub < mesh.subMeshCount; sub++) {
                            var indices = mesh.GetTriangles(sub);
                            for (int t = 0; t < indices.Length; t += 3) { int swap = indices[t]; indices[t] = indices[t+2]; indices[t+2] = swap; }
                            mesh.SetTriangles(indices,sub);
                        }
                    mesh.RecalculateBounds();
                    ValidateBoundary(levels[0][i],mesh);
                    string destination = Root + "/" + mesh.name + ".asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(destination);
                    if (saved == null) { AssetDatabase.CreateAsset(mesh,destination); saved = mesh; }
                    else { EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
                    levels[level][i] = saved;
                    Debug.Log("LAND_LOD " + Names[i] + " level=" + level + " triangles=" + Triangles(saved));
                }
            }
            var scene = EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/WaterConnectionsReview.unity", OpenSceneMode.Single);
            var review = new GameObject("Detalle adaptable del terreno").AddComponent<TerrainLodArtReview>();
            var serialized = new SerializedObject(review);
            string[] fields = { "fullMeshes", "mediumMeshes", "distantMeshes" };
            for (int level = 0; level < 3; level++) {
                var array = serialized.FindProperty(fields[level]); array.arraySize = 5;
                for (int i = 0; i < 5; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = levels[level][i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("TERRAIN_LOD_SCENE_READY");
        }

        static HashSet<Vector3Int> Boundary(Mesh mesh)
        {
            var result = new HashSet<Vector3Int>();
            foreach (var v in mesh.vertices) {
                float edge = Mathf.Max(Mathf.Abs(v.x)/(Mathf.Sqrt(3)*.5f),(Mathf.Abs(v.x)+Mathf.Sqrt(3)*Mathf.Abs(v.z))/Mathf.Sqrt(3));
                if (edge > 1.001f) throw new InvalidOperationException("Terreno fuera de su casilla.");
                if (edge > .9999f) {
                    if (Mathf.Abs(v.y-.12f) > .0001f) throw new InvalidOperationException("Altura de borde incorrecta.");
                    result.Add(new Vector3Int(Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000)));
                }
            }
            return result;
        }
        static void ValidateBoundary(Mesh original,Mesh reduced)
        {
            if (!Boundary(original).SetEquals(Boundary(reduced))) throw new InvalidOperationException("El LOD ha alterado el perimetro.");
            if (Triangles(reduced) >= Triangles(original)) throw new InvalidOperationException("LOD sin reduccion.");
        }
        static long Triangles(Mesh mesh) => Enumerable.Range(0,mesh.subMeshCount).Sum(i => (long)mesh.GetIndexCount(i)/3);

        public static void Batch()
        {
            try {
                var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args,"-fulgorReviewOutput");
                if (at < 0 || at+1 >= args.Length) throw new ArgumentException("Falta directorio de revision.");
                Directory.CreateDirectory(args[at+1]); Prepare();
                SessionState.SetString(Output,args[at+1]); SessionState.SetBool(Running,true); SessionState.SetBool(Done,false);
                started = EditorApplication.timeSinceStartup; frames = 0; EditorApplication.update -= Tick; EditorApplication.update += Tick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        [Serializable] sealed class Report
        {
            public string unity,device,method;
            public int tiles,landTiles,waterTiles;
            public long[] boardTriangles;
            public long[] visibleTerrainTriangles;
            public double[] medianSynchronousRenderMs,p90SynchronousRenderMs;
            public bool boundariesChecked,selectionChecked,collidersUnchanged,automaticZoomChecked;
            public int samplesPerLevel;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Running,false)) { EditorApplication.update -= Tick; return; }
            if (SessionState.GetBool(Done,false) && !EditorApplication.isPlayingOrWillChangePlaymode) {
                SessionState.SetBool(Running,false); EditorApplication.update -= Tick; EditorApplication.Exit(0); return;
            }
            if (started == 0) started = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup-started > 240) { Debug.LogError("Revision LOD agotada."); EditorApplication.Exit(1); return; }
            var lod = Object.FindFirstObjectByType<TerrainLodArtReview>();
            if (!EditorApplication.isPlaying || SessionState.GetBool(Done,false) || lod == null || !lod.Ready || ++frames < 20) return;
            try {
                var tiles = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
                if (tiles.Length != 384 || lod.TileCount != 348) throw new InvalidOperationException("Casillas incompletas.");
                var originalColliders = tiles.ToDictionary(t => t,t => t.GetComponent<MeshCollider>().sharedMesh);
                var camera = Camera.main; var controller = camera.GetComponent<CampaignCameraController>();
                if (controller != null) controller.enabled = false;
                var castle = Object.FindFirstObjectByType<FortressMapArtReview>(); var focus = castle.PlacedFortress.position;
                camera.transform.position = focus+new Vector3(0,28,-22); camera.transform.LookAt(focus); camera.orthographicSize = 18;
                string output = SessionState.GetString(Output,"");
                var report = new Report { unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,
                    method="1600x1000 URP render plus synchronous ReadPixels; warmed, interleaved, Editor batch; not game FPS",
                    tiles=tiles.Length,landTiles=lod.TileCount,waterTiles=36,boardTriangles=new long[3],visibleTerrainTriangles=new long[3],
                    medianSynchronousRenderMs=new double[3],p90SynchronousRenderMs=new double[3],samplesPerLevel=24 };
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                for (int level = 0; level < 3; level++) {
                    lod.ForceLevel(level);
                    report.boardTriangles[level] = tiles.Sum(t => Triangles(t.GetComponent<MeshFilter>().sharedMesh));
                    report.visibleTerrainTriangles[level] = tiles.Where(t => GeometryUtility.TestPlanesAABB(planes,t.GetComponent<MeshRenderer>().bounds)).Sum(t => Triangles(t.GetComponent<MeshFilter>().sharedMesh));
                    foreach (var tile in tiles) {
                        if (tile.GetComponent<MeshCollider>().sharedMesh != originalColliders[tile]) throw new InvalidOperationException("Collider cambiado por LOD.");
                        tile.SetSelected(true);
                        if (!tile.HasColorOverride) throw new InvalidOperationException("Seleccion perdida.");
                        lod.RefreshNow();
                        if (!tile.HasColorOverride) throw new InvalidOperationException("LOD borra seleccion.");
                        tile.SetSelected(false);
                        if (tile.HasColorOverride) throw new InvalidOperationException("Seleccion residual.");
                    }
                    Capture(camera,Path.Combine(output,"Terreno_Zoom18_LOD"+level+".png"));
                }
                foreach (var family in tiles.GroupBy(t => t.Cell.Terrain)) {
                    var tile = family.First();
                    if (!tile.GetComponent<MeshCollider>().Raycast(new Ray(tile.transform.position+Vector3.up*3,Vector3.down),out _,6))
                        throw new InvalidOperationException("Terreno sin colision.");
                }
                Benchmark(camera,lod,report);
                foreach (float zoom in new[] {18f,10f,4f}) {
                    camera.orthographicSize=zoom; lod.ForceLevel(-1); lod.RefreshNow();
                    int expected = zoom == 18 ? 2 : zoom == 10 ? 1 : 0;
                    if (lod.LevelCounts[expected] != lod.TileCount) throw new InvalidOperationException("Cambio automatico incorrecto.");
                    Capture(camera,Path.Combine(output,"Terreno_Automatico_Zoom"+zoom+".png"));
                }
                // Same camera and zoom for close inspection, including the deliberately forced far mesh.
                var forest = tiles.Where(t => t.Cell.Terrain == TerrainType.Forest).OrderBy(t => (t.transform.position-focus).sqrMagnitude).First();
                camera.transform.position=forest.transform.position+new Vector3(0,7,-5); camera.transform.LookAt(forest.transform.position); camera.orthographicSize=2.5f;
                for (int level=0;level<3;level++) { lod.ForceLevel(level); Capture(camera,Path.Combine(output,"Terreno_Bosque_LOD"+level+".png")); }
                lod.ForceLevel(-1);
                report.boundariesChecked=true; report.selectionChecked=true; report.collidersUnchanged=true; report.automaticZoomChecked=true;
                File.WriteAllText(Path.Combine(output,"revision_lod.json"),JsonUtility.ToJson(report,true));
                Debug.Log("TERRAIN_LOD_REVIEW_OK triangles="+string.Join(",",report.boardTriangles));
                SessionState.SetBool(Done,true); EditorApplication.isPlaying=false;
            }
            catch (Exception e) { Debug.LogException(e); SessionState.SetBool(Running,false); EditorApplication.Exit(1); }
        }

        static void Render(Camera camera,RenderTexture target,Texture2D image)
        {
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
            RenderTexture.active=target; image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(false);
        }
        static void Benchmark(Camera camera,TerrainLodArtReview lod,Report report)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32); target.Create();
            var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false); var previous=RenderTexture.active;
            var times=new List<double>[] {new(),new(),new()};
            try {
                for (int level=0;level<3;level++) { lod.ForceLevel(level); for(int warm=0;warm<8;warm++) Render(camera,target,image); }
                for (int round=0;round<6;round++)
                    for(int step=0;step<3;step++) {
                        int level=(step+round)%3; lod.ForceLevel(level); Render(camera,target,image);
                        for(int sample=0;sample<4;sample++) {
                            var watch=Stopwatch.StartNew(); Render(camera,target,image); watch.Stop(); times[level].Add(watch.Elapsed.TotalMilliseconds);
                        }
                    }
                for(int level=0;level<3;level++) {
                    times[level].Sort(); report.medianSynchronousRenderMs[level]=times[level][times[level].Count/2];
                    report.p90SynchronousRenderMs[level]=times[level][(int)(times[level].Count*.9)];
                }
            }
            finally { RenderTexture.active=previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
        static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32); target.Create();
            var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false); var previous=RenderTexture.active;
            try { Render(camera,target,image); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { RenderTexture.active=previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
        }
    }
}
