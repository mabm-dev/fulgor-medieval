using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
    public static class CityViewBuilder
    {
        const string Art="Assets/Fulgor/Art/City/CastillaView";
        const string Prefab="Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab";
        const string Running="Fulgor.CityView.Running",Output="Fulgor.CityView.Output",Done="Fulgor.CityView.Done";
        static int frames;static double started;
        static readonly Dictionary<string,Color> Colors=new() {
            {"Cal",new Color(.64f,.57f,.43f)},{"Piedra",new Color(.36f,.35f,.30f)},{"Madera",new Color(.17f,.095f,.043f)},
            {"Teja",new Color(.37f,.105f,.045f)},{"Hierro",new Color(.08f,.075f,.063f)},{"Oscuro",new Color(.035f,.045f,.047f)},
            {"Tierra",new Color(.30f,.36f,.20f)},{"Camino",new Color(.39f,.31f,.20f)},{"Agua",new Color(.065f,.18f,.18f)},{"Tela",new Color(.38f,.075f,.06f)} };
        static CityViewBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        static Material Material(string name)
        {
            string path=Art+"/M_Villa_"+name+".mat";var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(value==null){value=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(value,path);}
            value.SetColor("_BaseColor",Colors[name]);value.SetFloat("_Smoothness",.12f);value.enableInstancing=true;EditorUtility.SetDirty(value);return value;
        }
        [MenuItem("Fulgor/Campaña/Preparar vista de ciudad")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepara fuera de Play Mode.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if(!AssetDatabase.IsValidFolder("Assets/Fulgor/Prefabs/City"))AssetDatabase.CreateFolder("Assets/Fulgor/Prefabs","City");
            string[] models={"CasaCal","CasaAlta","Mercado","Pozo","Almacen"};
            foreach(string model in models) {
                string path=Art+"/SM_Villa_"+model+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                importer.bakeAxisConversion=true;importer.globalScale=1;importer.useFileScale=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.InPrefab;importer.SaveAndReimport();
            }
            var root=new GameObject("Villa de Castilla");
            try {
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Terreno de la villa";floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(40,.1f,40);Object.DestroyImmediate(floor.GetComponent<Collider>());floor.GetComponent<MeshRenderer>().sharedMaterial=Material("Tierra");
                var castle=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fulgor/Prefabs/ArtReview/PF_CastillaV11_LOD.prefab"),root.transform) as GameObject;
                castle.name="Fortaleza";castle.transform.localPosition=new Vector3(0,0,1.1f);castle.transform.localScale=Vector3.one*3.2f;Tag(castle,"Fortaleza",CityBuildingKind.Fortress);
                int number=1;
                foreach(float x in new[]{-2.65f,2.65f})foreach(float z in new[]{-2.25f,-.55f,1.2f}) {
                    string model=number%2==0?"CasaCal":"CasaAlta";var house=Place(root.transform,model,new Vector3(x,0,z),x<0?-90:90,.78f);
                    Tag(house,"Vivienda "+number,CityBuildingKind.House);number++;
                }
                foreach(float x in new[]{-1.8f,1.8f}){var house=Place(root.transform,"CasaCal",new Vector3(x,0,3.2f),0,.82f);Tag(house,"Vivienda "+number++,CityBuildingKind.House);}
                var market=Place(root.transform,"Mercado",new Vector3(1.65f,0,-2.55f),15,.85f);Tag(market,"Mercado",CityBuildingKind.Market);
                var well=Place(root.transform,"Pozo",new Vector3(-.65f,0,-1.35f),0,1);Tag(well,"Pozo de la plaza",CityBuildingKind.Well);
                var warehouse=Place(root.transform,"CasaCal",new Vector3(-1.7f,0,-2.8f),-10,.95f);Tag(warehouse,"Almacén",CityBuildingKind.Warehouse);
                Place(root.transform,"Almacen",new Vector3(-1.75f,0,-2.15f),0,.9f);
                BuildPaths(root.transform);PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
            var definition=AssetDatabase.LoadAssetAtPath<CampaignSettlementDefinition>("Assets/Fulgor/Settlements/FortalezaCastilla.asset");var data=new SerializedObject(definition);
            data.FindProperty("cityPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();CityAtmosphereBuilder.Prepare();
        }
        static GameObject Place(Transform parent,string model,Vector3 position,float angle,float scale)
        {
            var wrapper=new GameObject(model);wrapper.transform.SetParent(parent,false);wrapper.transform.localPosition=position;wrapper.transform.localRotation=Quaternion.Euler(0,angle,0);wrapper.transform.localScale=Vector3.one*scale;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/SM_Villa_"+model+".fbx");var child=PrefabUtility.InstantiatePrefab(source,wrapper.transform) as GameObject;
            foreach(var renderer in child.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                if(m==null)throw new InvalidOperationException("Material importado sin nombre.");
                string name=m.name.Replace("M_Villa_","").Split('.')[0];if(!Colors.ContainsKey(name))throw new InvalidOperationException("Material desconocido: "+m.name);return Material(name);
            }).ToArray();
            return wrapper;
        }
        static void Tag(GameObject item,string label,CityBuildingKind kind)
        {
            var tag=item.AddComponent<CityBuildingView>();var data=new SerializedObject(tag);data.FindProperty("displayName").stringValue=label;data.FindProperty("kind").enumValueIndex=(int)kind;data.ApplyModifiedPropertiesWithoutUndo();
            var bounds=tag.Bounds;var collider=item.AddComponent<BoxCollider>();collider.center=item.transform.InverseTransformPoint(bounds.center);
            // Transform all eight corners so rotated houses have correct local selection bounds.
            var local=new Bounds(collider.center,Vector3.zero);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)local.Encapsulate(item.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
            collider.center=local.center;collider.size=local.size;
        }
        static void BuildPaths(Transform parent)
        {
            var pieces=new List<CombineInstance>();var random=new System.Random(1212);
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var source=cube.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(cube);
            for(float x=-1.8f;x<=1.8f;x+=.18f)for(float z=-3.5f;z<=-.25f;z+=.18f) {
                if(Mathf.Abs(x)>.68f&&z<-2.5f)continue;
                var position=new Vector3(x+(float)random.NextDouble()*.02f,.006f,z+(float)random.NextDouble()*.02f);
                pieces.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(position,Quaternion.Euler(0,(float)random.NextDouble()*6-3,0),new Vector3(.165f,.012f,.165f))});
            }
            var mesh=new Mesh{name="SM_Plaza_Castilla"};mesh.CombineMeshes(pieces.ToArray());mesh.RecalculateBounds();string path=Art+"/SM_Plaza_Castilla.asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            var root=new GameObject("Plaza empedrada");root.transform.SetParent(parent,false);root.AddComponent<MeshFilter>().sharedMesh=saved;root.AddComponent<MeshRenderer>().sharedMaterial=Material("Camino");
        }
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de revision.");
                Directory.CreateDirectory(args[at+1]);Prepare();EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity",OpenSceneMode.Single);
                SessionState.SetString(Output,args[at+1]);SessionState.SetBool(Running,true);SessionState.SetBool(Done,false);started=EditorApplication.timeSinceStartup;frames=0;
                EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            if(SessionState.GetBool(Done,false)&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Revision de ciudad agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||SessionState.GetBool(Done,false))return;
            var settlements=Object.FindFirstObjectByType<CampaignSettlementController>();if(settlements==null||!settlements.Ready||++frames<14)return;
            try {
                string output=SessionState.GetString(Output,"");var castle=settlements.Registry.Settlements[0];var selection=settlements.GetComponent<CampaignBoardSelection>();var camera=Camera.main;
                int mask=camera.cullingMask;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
                if(!selection.ActivateTile(settlements.Views[castle.Position].Tile))throw new InvalidOperationException("No se activa el castillo.");
                var city=settlements.GetComponent<CityManagementView>();if(city==null||!city.IsOpen||city.BuildingCount!=3||!selection.IsInteractionSuspended)throw new InvalidOperationException("La ciudad no abre o no bloquea el mapa.");
                CityConstructionReviewBuilder.CompleteAll(city);
                foreach(var tab in new[]{CityPanelKind.Overview,CityPanelKind.Buildings,CityPanelKind.Treasury,CityPanelKind.Armies,CityPanelKind.Orders}) {
                    city.SelectPanel(tab);Capture(camera,city,Path.Combine(output,"Ciudad_"+tab+".png"));
                }
                city.SelectPanel(CityPanelKind.Overview);Capture(camera,city,Path.Combine(output,"Ciudad_1280x720.png"),1280,720);Capture(camera,city,Path.Combine(output,"Ciudad_1024x768.png"),1024,768);
                city.FocusKind(CityBuildingKind.Fortress);Capture(camera,city,Path.Combine(output,"Ciudad_Fortaleza.png"));
                city.FocusKind(CityBuildingKind.Market);Capture(camera,city,Path.Combine(output,"Ciudad_Mercado.png"));
                city.Close();
                if(selection.IsInteractionSuspended||camera.cullingMask!=mask||camera.transform.position!=position||camera.transform.rotation!=rotation||camera.orthographicSize!=size)throw new InvalidOperationException("La vuelta al mapa no restaura su estado.");
                File.WriteAllText(Path.Combine(output,"revision_ciudad.json"),"{\"city_opens_from_tile_activation\":true,\"management_tabs\":5,\"buildings\":12,\"camera_and_hud_restored\":true,\"map_input_suspended\":true}");
                Debug.Log("CITY_VIEW_REVIEW_OK");SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,CityManagementView city,string path,int width=1600,int height=1000)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(width,height,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            var canvas=city.Viewport.GetComponentInParent<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float plane=canvas.planeDistance;var previousTarget=camera.targetTexture;float aspect=camera.aspect;
            try {
                RenderPipeline.SubmitRenderRequest(city.PreviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=city.PreviewCamera.targetTexture});
                camera.targetTexture=target;camera.aspect=width/(float)height;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }finally {canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=plane;camera.targetTexture=previousTarget;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
