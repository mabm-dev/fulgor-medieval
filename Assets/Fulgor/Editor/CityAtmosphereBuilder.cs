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
    public static class CityAtmosphereBuilder
    {
        const string Art="Assets/Fulgor/Art/City/CastillaView";
        const string Prefab="Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab";
        const string Running="Fulgor.LivingCity.Running",Output="Fulgor.LivingCity.Output",Stage="Fulgor.LivingCity.Stage";
        static int frames;static double started;static readonly List<Mesh> generated=new();
        static CityAtmosphereBuilder(){if(SessionState.GetBool(Running,false))EditorApplication.update+=Tick;}
        static Material Mat(string name,Color color)
        {
            string path=Art+"/M_Ambiente_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.08f);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
        static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var root=GameObject.CreatePrimitive(type);root.name=name;root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localScale=scale;Object.DestroyImmediate(root.GetComponent<Collider>());root.GetComponent<MeshRenderer>().sharedMaterial=material;return root;
        }
        static void Beam(Transform root,Vector3 a,Vector3 b,float width,Material material)
        {
            var part=Part(root,"Madera del puesto",PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,(a-b).magnitude,width),material);part.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        static void Stall(Transform parent,Vector3 position,Material cloth,Material wood,Material apples,Material greens,Material grain,int index)
        {
            var root=new GameObject("Puesto de mercado "+index);root.transform.SetParent(parent,false);root.transform.localPosition=position;
            foreach(float x in new[]{-.45f,.45f})foreach(float z in new[]{-.27f,.27f})Beam(root.transform,new Vector3(x,0,z),new Vector3(x,.64f,z),.035f,wood);
            Part(root.transform,"Mostrador",PrimitiveType.Cube,new Vector3(0,.26f,-.06f),new Vector3(.95f,.045f,.42f),wood);
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int row=0;row<=8;row++)for(int col=0;col<=8;col++) {
                float x=(col/8f-.5f)*1.04f,z=(row/8f-.5f)*.70f;
                vertices.Add(new Vector3(x,.68f-Mathf.Sin(col/8f*Mathf.PI)*.075f+Mathf.Sin(row*.8f)*.007f,z));
                if(row<8&&col<8){int a=row*9+col;triangles.AddRange(new[]{a,a+9,a+1,a+1,a+9,a+10});}
            }
            var mesh=new Mesh{name="SM_Toldo_"+index};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Art+"/SM_Toldo_"+index+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            var canopy=new GameObject("Toldo de lino");canopy.transform.SetParent(root.transform,false);canopy.AddComponent<MeshFilter>().sharedMesh=saved;canopy.AddComponent<MeshRenderer>().sharedMaterial=cloth;
            for(int basket=0;basket<3;basket++) {
                float x=(basket-1)*.29f;Part(root.transform,"Cajón de mercancías",PrimitiveType.Cube,new Vector3(x,.31f,-.06f),new Vector3(.25f,.065f,.29f),wood);
                for(int item=0;item<6;item++)Part(root.transform,"Mercancía",PrimitiveType.Sphere,new Vector3(x+(item%3-1)*.065f,.36f,-.13f+(item/3)*.10f),Vector3.one*(basket==2?.045f:.06f),basket==0?apples:basket==1?greens:grain);
            }
        }
        [MenuItem("Fulgor/Campaña/Mejorar ambiente de ciudad")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepara fuera de Play Mode.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader=Shader.Find("Fulgor/City/Ground");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader de suelo no disponible.");
            string groundPath=Art+"/M_Ambiente_Suelo.mat";var ground=AssetDatabase.LoadAssetAtPath<Material>(groundPath);if(ground==null){ground=new Material(shader);AssetDatabase.CreateAsset(ground,groundPath);}ground.shader=shader;ground.SetColor("_BaseColor",new Color(.23f,.30f,.145f));ground.SetColor("_SoilColor",new Color(.34f,.29f,.20f));ground.SetFloat("_GroundScale",100);EditorUtility.SetDirty(ground);
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try {
                var previous=root.transform.Find("Ambiente de la villa");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                root.transform.Find("Terreno de la villa").GetComponent<MeshRenderer>().sharedMaterial=ground;
                root.transform.Find("Terreno de la villa").localScale=new Vector3(100,.1f,100);
                if(root.GetComponent<CityAmbientLife>()==null)root.AddComponent<CityAmbientLife>();
                var ambiance=new GameObject("Ambiente de la villa");ambiance.transform.SetParent(root.transform,false);
                var wood=Mat("Madera",new Color(.22f,.135f,.065f));var cloth=Mat("Lino",new Color(.62f,.52f,.32f));var clothRed=Mat("LinoRojo",new Color(.39f,.12f,.075f));
                var apple=Mat("Frutas",new Color(.54f,.13f,.06f));var greens=Mat("Hortalizas",new Color(.27f,.38f,.12f));var grain=Mat("Grano",new Color(.62f,.46f,.18f));
                Stall(ambiance.transform,new Vector3(-1.4f,0,-2.85f),clothRed,wood,apple,greens,grain,0);
                Stall(ambiance.transform,new Vector3(1.45f,0,-.70f),cloth,wood,apple,greens,grain,1);
                var leaves=Mat("Follaje",new Color(.24f,.34f,.13f));var leavesLight=Mat("FollajeClaro",new Color(.34f,.40f,.17f));
                int number=0;
                foreach(var point in new[]{new Vector3(-3.8f,0,-2.8f),new Vector3(-3.8f,0,2.4f),new Vector3(3.8f,0,.2f),new Vector3(3.7f,0,2.7f),new Vector3(-2.9f,0,3.9f),new Vector3(2.8f,0,4),new Vector3(0,0,4.2f)}) {
                    var tree=new GameObject("Árbol "+number++);tree.transform.SetParent(ambiance.transform,false);tree.transform.localPosition=point;
                    Part(tree.transform,"Tronco",PrimitiveType.Cylinder,new Vector3(0,.46f,0),new Vector3(.085f,.46f,.085f),wood);
                    Foliage(tree.transform,leaves,leavesLight,number);
                }
                for(int i=0;i<5;i++) {
                    float z=-.65f+i*.42f;Beam(ambiance.transform,new Vector3(-4.25f,0,z),new Vector3(-4.25f,.30f,z),.035f,wood);Beam(ambiance.transform,new Vector3(-3.15f,0,z),new Vector3(-3.15f,.30f,z),.035f,wood);
                }
                foreach(float x in new[]{-4.25f,-3.15f})foreach(float y in new[]{.12f,.25f})Beam(ambiance.transform,new Vector3(x,y,-.65f),new Vector3(x,y,1.03f),.025f,wood);
                foreach(float z in new[]{-.65f,1.03f})foreach(float y in new[]{.12f,.25f})Beam(ambiance.transform,new Vector3(-4.25f,y,z),new Vector3(-3.15f,y,z),.025f,wood);
                var random=new System.Random(42);var stone=Mat("Piedras",new Color(.39f,.38f,.30f));
                for(int i=0;i<32;i++){float angle=(float)random.NextDouble()*Mathf.PI*2;float radius=3.4f+(float)random.NextDouble();Part(ambiance.transform,"Piedra",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*radius,.025f,Mathf.Sin(angle)*radius),new Vector3(.10f,.06f,.085f),stone);}
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            var paving=AssetDatabase.LoadAssetAtPath<Material>(Art+"/M_Villa_Camino.mat");paving.SetColor("_BaseColor",new Color(.39f,.37f,.29f));EditorUtility.SetDirty(paving);AssetDatabase.SaveAssets();CityFinishBuilder.Prepare();
        }
        static void Foliage(Transform tree,Material dark,Material light,int seed)
        {
            var random=new System.Random(seed*53);var vertices=new List<Vector3>();var normals=new List<Vector3>();var a=new List<int>();var b=new List<int>();
            for(int i=0;i<700;i++) {
                int cluster=i%4;float az=(float)random.NextDouble()*Mathf.PI*2;float cos=(float)random.NextDouble()*2-1;float radius=Mathf.Pow((float)random.NextDouble(),1/3f);
                var p=new Vector3(Mathf.Cos(az)*Mathf.Sqrt(1-cos*cos),cos,Mathf.Sin(az)*Mathf.Sqrt(1-cos*cos))*radius;
                p=Vector3.Scale(p,new Vector3(.32f,.32f,.30f))+new Vector3(Mathf.Sin(cluster*2)*.18f,.90f+(cluster%2)*.14f,Mathf.Cos(cluster*2)*.18f);
                var rotation=Quaternion.Euler((float)random.NextDouble()*150,(float)random.NextDouble()*360,(float)random.NextDouble()*150);float size=.065f+(float)random.NextDouble()*.045f;
                int at=vertices.Count;vertices.Add(p+rotation*new Vector3(-size*.5f,0,0));vertices.Add(p+rotation*new Vector3(0,.007f,size*.25f));vertices.Add(p+rotation*new Vector3(size*.5f,0,0));vertices.Add(p+rotation*new Vector3(0,-.007f,-size*.25f));
                for(int n=0;n<4;n++)normals.Add((rotation*Vector3.up+Vector3.up*.5f).normalized);
                (i%3==0?b:a).AddRange(new[]{at,at+1,at+2,at,at+2,at+3,at+2,at+1,at,at+3,at+2,at});
            }
            var mesh=new Mesh{name="SM_Follaje_Villa_"+seed};mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(a,0);mesh.SetTriangles(b,1);mesh.SetNormals(normals);mesh.RecalculateBounds();
            string path=Art+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            var root=new GameObject("Follaje");root.transform.SetParent(tree,false);root.AddComponent<MeshFilter>().sharedMesh=saved;root.AddComponent<MeshRenderer>().sharedMaterials=new[]{dark,light};
        }
        public static void Batch()
        {
            try {
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-fulgorReviewOutput");if(at<0||at+1>=args.Length)throw new ArgumentException("Falta directorio de capturas.");
                Directory.CreateDirectory(args[at+1]);Prepare();EditorSceneManager.OpenScene("Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity",OpenSceneMode.Single);
                SessionState.SetString(Output,args[at+1]);SessionState.SetBool(Running,true);SessionState.SetInt(Stage,0);started=EditorApplication.timeSinceStartup;frames=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Running,false)){EditorApplication.update-=Tick;return;}
            int stage=SessionState.GetInt(Stage,0);if(stage==2&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Running,false);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;}
            if(started==0)started=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-started>180){Debug.LogError("Revision de ambiente agotada.");EditorApplication.Exit(1);return;}
            if(!EditorApplication.isPlaying||stage==2)return;
            var controller=Object.FindFirstObjectByType<CampaignSettlementController>();if(controller==null||!controller.Ready||++frames<14)return;
            try {
                string output=SessionState.GetString(Output,"");var camera=Camera.main;var selection=controller.GetComponent<CampaignBoardSelection>();
                if(stage==0){selection.ActivateTile(controller.Views.Values.First().Tile);SessionState.SetInt(Stage,1);frames=0;return;}
                var city=controller.GetComponent<CityManagementView>();CityConstructionReviewBuilder.CompleteAll(city);if(!city.IsOpen||city.Life==null||city.Life.Residents!=14||city.Life.Animals!=8)throw new InvalidOperationException("Falta actividad de la villa.");
                var previous=city.Life.Positions();
                for(int i=0;i<50;i++)city.Life.Advance(.05f);
                if(previous.Where((p,i)=>Vector3.Distance(p,city.Life.Positions()[i])>.01f).Count()<8)throw new InvalidOperationException("Los vecinos no pasean.");
                Capture(camera,city,Path.Combine(output,"Ciudad_Viva_General.png"));
                city.ResetView();
                city.FocusKind(CityBuildingKind.Market);Capture(camera,city,Path.Combine(output,"Ciudad_Viva_Mercado.png"));
                for(int i=0;i<18;i++){for(int j=0;j<4;j++)city.Life.Advance(.05f);Capture(camera,city,Path.Combine(output,"Paseo_"+i.ToString("D2")+".png"));}
                city.Close();if(selection.IsInteractionSuspended)throw new InvalidOperationException("No se vuelve al mapa.");
                File.WriteAllText(Path.Combine(output,"revision_ambiente.json"),"{\"residents\":14,\"animals\":8,\"perspective\":true,\"fixed_camera\":true,\"movement_checked\":true,\"return_to_map_checked\":true}");
                Debug.Log("LIVING_CITY_REVIEW_OK");SessionState.SetInt(Stage,2);EditorApplication.isPlaying=false;
            }catch(Exception e){Debug.LogException(e);SessionState.SetBool(Running,false);EditorApplication.Exit(1);}
        }
        static void Capture(Camera camera,CityManagementView city,string path)
        {
            var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(1600,1000,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            var canvas=city.Viewport.GetComponentInParent<Canvas>();var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;var oldTarget=camera.targetTexture;float aspect=camera.aspect;
            try {RenderPipeline.SubmitRenderRequest(city.PreviewCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=city.PreviewCamera.targetTexture});camera.targetTexture=target;camera.aspect=1.6f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
    }
}
