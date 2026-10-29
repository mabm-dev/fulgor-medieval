using System;
using System.Collections.Generic;
using System.Linq;
using Fulgor.Presentation.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Fulgor.Editor
{
    /// <summary>Reproducible authoring pass; runtime owns only local material copies.</summary>
    public static class CityQualityBuilder
    {
        const string Art="Assets/Fulgor/Art/City/CastillaFinish";
        const string Original="Assets/Fulgor/Art/City/CastillaView";
        const string Prefab="Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab";
        static Material stone,wood,iron,grass,dryGrass;
        static System.Random random=new(7419);

        static Material Surface(string name,string substance,int kind,float scale,float relief)
        {
            string path=Art+"/M_"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Fulgor/City/Surface"));AssetDatabase.CreateAsset(mat,path);}
            mat.shader=Shader.Find("Fulgor/City/Surface");mat.SetColor("_BaseColor",Color.white);
            mat.SetTexture("_DetailMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_"+substance+"_BaseColor.png"));
            mat.SetTexture("_DetailNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_"+substance+"_Normal.png"));
            mat.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_"+substance+"_Roughness.png"));
            mat.SetFloat("_UseTexture",1);mat.SetFloat("_UseAtlas",0);mat.SetFloat("_SurfaceKind",kind);
            mat.SetFloat("_DetailScale",scale);mat.SetFloat("_NormalStrength",relief);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        static Material Plain(string name,Color color)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Art+"/"+name+".mat");}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.05f);mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;EditorUtility.SetDirty(mat);return mat;
        }
        [MenuItem("Fulgor/Campaña/Preparar calidad de Castilla con Substance")]
        public static void Prepare()
        {
            random=new System.Random(7419);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{Art}).Select(AssetDatabase.GUIDToAssetPath)){
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                bool normal=path.EndsWith("_Normal.png",StringComparison.Ordinal);
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.EndsWith("_BaseColor.png",StringComparison.Ordinal);importer.mipmapEnabled=true;
                importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
                importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            foreach(string name in new[]{"Fulgor/City/Surface","Fulgor/City/Ground"}){
                var shader=Shader.Find(name);if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Shader incorrecto: "+name);
            }
            var library=AssetDatabase.LoadAssetAtPath<CityFinishMaterials>(Art+"/CastillaFinish.asset");
            if(library==null){library=ScriptableObject.CreateInstance<CityFinishMaterials>();AssetDatabase.CreateAsset(library,Art+"/CastillaFinish.asset");}
            library.Limestone=Surface("Caliza","Caliza",1,3.8f,.18f);library.Plaster=Surface("Revoco","Revoco",0,3.2f,.14f);
            library.Oak=Surface("Roble","Roble",2,2.3f,.20f);library.Terracotta=Surface("Terracota","Terracota",3,4.8f,.18f);
            library.HouseLimeOcclusion=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/SM_Villa_CasaCal_AO.png");
            library.HouseTallOcclusion=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/SM_Villa_CasaAlta_AO.png");
            library.MarketOcclusion=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/SM_Villa_Mercado_AO.png");
            var ground=AssetDatabase.LoadAssetAtPath<Material>(Art+"/M_Suelo.mat");
            if(ground==null){ground=new Material(Shader.Find("Fulgor/City/Ground"));AssetDatabase.CreateAsset(ground,Art+"/M_Suelo.mat");}
            ground.shader=Shader.Find("Fulgor/City/Ground");ground.SetColor("_BaseColor",new Color(.86f,.90f,.86f));ground.SetColor("_SoilColor",new Color(.94f,.90f,.81f));ground.SetFloat("_GroundScale",100);
            ground.SetTexture("_GrassMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_Hierba_BaseColor.png"));
            ground.SetTexture("_SoilMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_Tierra_BaseColor.png"));
            ground.SetTexture("_SoilNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_Tierra_Normal.png"));
            ground.SetTexture("_SoilRoughness",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/T_Tierra_Roughness.png"));
            library.Ground=ground;EditorUtility.SetDirty(ground);EditorUtility.SetDirty(library);
            stone=AssetDatabase.LoadAssetAtPath<Material>(Original+"/M_Villa_Piedra.mat");
            wood=AssetDatabase.LoadAssetAtPath<Material>(Original+"/M_Villa_Madera.mat");iron=AssetDatabase.LoadAssetAtPath<Material>(Original+"/M_Villa_Hierro.mat");
            grass=Plain("M_Pradera_Hojas",new Color(.27f,.32f,.16f));dryGrass=Plain("M_Pradera_Seca",new Color(.41f,.38f,.22f));
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try{
                var finish=root.GetComponent<CityVisualFinish>();var serialized=new SerializedObject(finish);
                serialized.FindProperty("authoredMaterials").objectReferenceValue=library;serialized.ApplyModifiedPropertiesWithoutUndo();
                var ambience=root.transform.Find("Ambiente de la villa");
                foreach(Transform child in ambience.Cast<Transform>().ToArray())if(child.name=="Piedra")Object.DestroyImmediate(child.gameObject);
                var previous=root.transform.Find("Acabado del asentamiento");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var details=new GameObject("Acabado del asentamiento");details.transform.SetParent(root.transform,false);
                Paving(details.transform);Meadow(details.transform);Branches(ambience);
                foreach(var building in root.GetComponentsInChildren<CityBuildingView>(true)){
                    if(building.Kind==CityBuildingKind.House){
                        int number=int.Parse(building.DisplayName.Split(' ').Last());
                        var positions=new[]{new Vector3(-2.5f,0,-1.65f),new Vector3(2.5f,0,-1.65f),new Vector3(-2.75f,0,.10f),new Vector3(2.75f,0,.10f),new Vector3(-2.75f,0,1.8f),new Vector3(2.75f,0,1.8f),new Vector3(-1.8f,0,3.2f),new Vector3(1.8f,0,3.2f)};
                        var rotations=new[]{-82f,98f,-94f,86f,-90f,94f,174f,186f};
                        building.transform.localPosition=positions[number-1];building.transform.localRotation=Quaternion.Euler(0,rotations[number-1],0);
                    }
                    if(building.Kind==CityBuildingKind.Market)building.transform.localPosition=new Vector3(1.65f,0,-3.1f);
                    var old=building.transform.Find("Utensilios de la vivienda");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    if(building.Kind==CityBuildingKind.House&&building.DisplayName=="Vivienda 1")Household(building.transform,0);
                    if(building.Kind==CityBuildingKind.House&&building.DisplayName=="Vivienda 2")Household(building.transform,1);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<MeshRenderer>().sharedMaterial=material;return obj;
        }
        static void Beam(Transform parent,Vector3 a,Vector3 b,float radius,Material material)
        {
            var obj=Primitive(parent,"Madera",PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius,(b-a).magnitude*.5f,radius),material);
            obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        static void Combine(Transform group,string id)
        {
            var source=group.GetComponentsInChildren<MeshRenderer>().ToArray();int index=0;
            foreach(var byMaterial in source.GroupBy(r=>r.sharedMaterial)){
                var combines=byMaterial.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=group.worldToLocalMatrix*r.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{name="SM_"+id+"_"+index,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
                string path=Art+"/"+mesh.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                var child=new GameObject("Detalle "+index++);child.transform.SetParent(group,false);child.AddComponent<MeshFilter>().sharedMesh=saved;child.AddComponent<MeshRenderer>().sharedMaterial=byMaterial.Key;
            }
            foreach(var item in source)Object.DestroyImmediate(item.gameObject);
        }
        static void Paving(Transform parent)
        {
            var group=new GameObject("Umbral y acceso de piedra");group.transform.SetParent(parent,false);
            for(int row=0;row<5;row++)for(int col=0;col<5;col++){
                float x=(col-2)*.165f+(row%2)*.03f,z=-.58f-row*.15f;
                var block=Primitive(group.transform,"Piedra de paso",PrimitiveType.Cube,new Vector3(x,.008f,z),new Vector3(.15f,.018f,.135f),stone);
                block.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*5-2.5f,0);
            }
            Combine(group.transform,"Acceso");
        }
        static void Household(Transform house,int variant)
        {
            var group=new GameObject("Utensilios de la vivienda");group.transform.SetParent(house,false);
            Barrel(group.transform,new Vector3(.40f,0,-.52f),.17f,.26f);
            if(variant==0){Barrel(group.transform,new Vector3(.22f,0,-.56f),.13f,.20f);Crate(group.transform,new Vector3(-.43f,0,-.52f));}
            else{
                for(int i=0;i<4;i++)Beam(group.transform,new Vector3(-.42f,.035f+(i/2)*.05f,-.35f+(i%2)*.06f),new Vector3(-.42f,.035f+(i/2)*.05f,-.70f+(i%2)*.06f),.035f,wood);
                Beam(group.transform,new Vector3(-.40f,.02f,-.54f),new Vector3(-.50f,.53f,-.42f),.017f,wood);
            }
            Combine(group.transform,"Hogar"+variant);
        }
        static void Barrel(Transform parent,Vector3 p,float diameter,float height)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();const int count=14;
            for(int ring=0;ring<4;ring++)for(int i=0;i<count;i++){
                float angle=i*Mathf.PI*2/count;float radius=diameter*.5f*(ring==0||ring==3?.86f:1);
                vertices.Add(p+new Vector3(Mathf.Cos(angle)*radius,ring*height/3,Mathf.Sin(angle)*radius));
                if(ring<3){int a=ring*count+i,b=ring*count+(i+1)%count;triangles.AddRange(new[]{a,b,a+count,b,b+count,a+count});}
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var barrel=new GameObject("Barril de duelas");barrel.transform.SetParent(parent,false);barrel.AddComponent<MeshFilter>().sharedMesh=mesh;barrel.AddComponent<MeshRenderer>().sharedMaterial=wood;
            foreach(float y in new[]{height*.17f,height*.81f})Primitive(parent,"Aro de hierro",PrimitiveType.Cylinder,p+Vector3.up*y,new Vector3(diameter*.98f,.009f,diameter*.98f),iron);
            Primitive(parent,"Tapa",PrimitiveType.Cylinder,p+Vector3.up*(height-.006f),new Vector3(diameter*.84f,.008f,diameter*.84f),wood);
        }
        static void Crate(Transform parent,Vector3 p)
        {
            for(int level=0;level<3;level++)foreach(int sign in new[]{-1,1}){
                Primitive(parent,"Tablas",PrimitiveType.Cube,p+new Vector3(0,.04f+level*.055f,sign*.105f),new Vector3(.25f,.045f,.018f),wood);
                Primitive(parent,"Tablas",PrimitiveType.Cube,p+new Vector3(sign*.12f,.04f+level*.055f,0),new Vector3(.018f,.045f,.21f),wood);
            }
            Primitive(parent,"Fondo",PrimitiveType.Cube,p+new Vector3(0,.013f,0),new Vector3(.25f,.025f,.21f),wood);
        }
        static void Branches(Transform ambience)
        {
            int index=0;
            foreach(Transform tree in ambience){
                if(!tree.name.StartsWith("Árbol ",StringComparison.Ordinal))continue;
                var previous=tree.Find("Ramaje");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                var branches=new GameObject("Ramaje");branches.transform.SetParent(tree,false);
                for(int j=0;j<6;j++){
                    float angle=(index*.7f+j*2.399f);var end=new Vector3(Mathf.Cos(angle)*.24f,.85f+(j%3)*.11f,Mathf.Sin(angle)*.24f);
                    Beam(branches.transform,new Vector3(0,.35f+j*.04f,0),end,.025f-j*.002f,wood);
                    Beam(branches.transform,end,end+new Vector3(Mathf.Cos(angle+1)*.12f,.14f,Mathf.Sin(angle+1)*.12f),.012f,wood);
                }
                Combine(branches.transform,"Ramaje"+index);
                tree.localRotation=Quaternion.Euler(0,index*71,0);
                tree.localScale=new Vector3(.91f+(index%3)*.09f,.88f+(index%4)*.08f,1.04f-(index%3)*.07f);index++;
            }
        }
        static void Meadow(Transform parent)
        {
            var group=new GameObject("Hierbas junto a los bordes");group.transform.SetParent(parent,false);
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<190;i++){
                float x=(float)random.NextDouble()*7.6f-3.8f,z=(float)random.NextDouble()*7.7f-3.5f;
                if(Mathf.Abs(x)<2.8f&&z<3.3f)continue;
                for(int blade=0;blade<5;blade++){
                    float angle=(float)random.NextDouble()*Mathf.PI*2;float width=.012f,height=.055f+(float)random.NextDouble()*.065f;
                    var p=new Vector3(x+(float)random.NextDouble()*.09f,.001f,z+(float)random.NextDouble()*.09f);var side=new Vector3(Mathf.Cos(angle)*width,0,Mathf.Sin(angle)*width);int at=vertices.Count;
                    vertices.Add(p-side);vertices.Add(p+side);vertices.Add(p+Vector3.up*height+side*.7f);triangles.AddRange(new[]{at,at+2,at+1,at+1,at+2,at});
                }
            }
            var mesh=new Mesh{name="SM_Hierbas"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Art+"/SM_Hierbas.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            group.AddComponent<MeshFilter>().sharedMesh=saved;group.AddComponent<MeshRenderer>().sharedMaterial=grass;
        }
        public static void Batch(){try{Prepare();CityConstructionReviewBuilder.Batch();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
