using System.Collections.Generic;
using Fulgor.Core.Campaign;
using UnityEngine;

namespace Fulgor.Presentation.Map
{
    /// <summary>Temporary construction geometry, derived entirely from the saved order and current turn.</summary>
    public sealed class CityConstructionSite : MonoBehaviour
    {
        private GameObject model;
        private readonly List<Material> materials=new();
        private readonly List<Mesh> meshes=new();
        private readonly List<CombineInstance>[] parts={new(),new(),new()};
        private Mesh cube;
        public string ProjectId { get; private set; }
        public int Phase { get; private set; }
        public GameObject Model=>model;

        public void Refresh(CityDevelopment development,int turn,CityBuildingView template)
        {
            string id=development.PendingId;
            int phase=id==null?0:(development.CompletionTurn-turn>=CityProjects.Get(id).Turns?1:2);
            if(id==ProjectId&&phase==Phase)return;
            ClearModel();ProjectId=id;Phase=phase;
            if(id==null||template==null)return;
            if(materials.Count==0) {
                Material("Piedra de obra",new Color(.40f,.37f,.29f));
                Material("Madera de andamio",new Color(.28f,.16f,.075f));
                Material("Tierra removida",new Color(.25f,.205f,.13f));
            }
            var bounds=LocalBounds(template);var size=bounds.size;
            float width=Mathf.Max(.4f,size.x),depth=Mathf.Max(.4f,size.z),height=Mathf.Clamp(size.y,.5f,2.1f);
            model=new GameObject("Obra · "+CityProjects.Get(id).Name);model.layer=gameObject.layer;model.transform.SetParent(transform,false);
            model.transform.localPosition=new Vector3(bounds.center.x,.012f,bounds.center.z);
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;
            primitive.GetComponent<Collider>().enabled=false;primitive.SetActive(false);Release(primitive);
            Add(2,new Vector3(0,.008f,0),new Vector3(width+.20f,.016f,depth+.20f));
            if(template.Kind==CityBuildingKind.Well)BuildWell(width,depth);
            else BuildHouse(width,depth,height);
            // Three merged material groups keep scaffolding from adding a renderer per beam.
            for(int i=0;i<parts.Length;i++) {
                if(parts[i].Count==0)continue;
                var mesh=new Mesh{name="Obra · grupo "+i};mesh.CombineMeshes(parts[i].ToArray(),true,true);meshes.Add(mesh);
                var group=new GameObject(materials[i].name);group.layer=gameObject.layer;group.transform.SetParent(model.transform,false);
                group.AddComponent<MeshFilter>().sharedMesh=mesh;group.AddComponent<MeshRenderer>().sharedMaterial=materials[i];parts[i].Clear();
            }
        }
        private Bounds LocalBounds(CityBuildingView building)
        {
            var result=new Bounds();bool first=true;
            foreach(var filter in building.GetComponentsInChildren<MeshFilter>(true)) {
                if(filter.sharedMesh==null)continue;var bounds=filter.sharedMesh.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                    var point=transform.InverseTransformPoint(filter.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z))));
                    if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
                }
            }
            return result;
        }
        private void BuildHouse(float width,float depth,float height)
        {
            float x=width*.46f,z=depth*.46f,wall=Phase==1?.16f:height*.42f,frame=height*(Phase==1?.48f:.83f);
            foreach(float side in new[]{-1f,1f}) {
                Add(0,new Vector3(side*x,wall*.5f,0),new Vector3(.10f,wall,depth*.94f));
                Add(0,new Vector3(0,wall*.5f,side*z),new Vector3(width*.94f,wall,.10f));
                for(int step=0;step<3;step++) {
                    float at=(step-1)*depth*.44f;
                    Beam(new Vector3(side*x,0,at),new Vector3(side*x,frame,at),.045f);
                }
                Beam(new Vector3(side*x,frame,-z),new Vector3(side*x,frame,z),.05f);
                float outside=side*(x+.15f);
                Beam(new Vector3(outside,0,-z),new Vector3(outside,frame+.15f,-z),.035f);
                Beam(new Vector3(outside,0,z),new Vector3(outside,frame+.15f,z),.035f);
                Beam(new Vector3(outside,.06f,-z),new Vector3(outside,frame,z),.025f);
                for(int plank=0;plank<3;plank++)Add(1,new Vector3(outside+(plank-1)*.045f,frame*.72f,0),new Vector3(.04f,.025f,depth+.15f));
            }
            foreach(float end in new[]{-z,z}) {
                Beam(new Vector3(-x,frame,end),new Vector3(x,frame,end),.05f);
                if(Phase==2) {
                    Beam(new Vector3(-x,frame,end),new Vector3(0,height,end),.045f);
                    Beam(new Vector3(x,frame,end),new Vector3(0,height,end),.045f);
                }
            }
            if(Phase==2)Beam(new Vector3(0,height,-z),new Vector3(0,height,z),.05f);
            Stockpile(width*.3f,-depth*.35f,Mathf.Min(.38f,width*.45f));
        }
        private void BuildWell(float width,float depth)
        {
            float radius=Mathf.Min(width,depth)*.35f;
            for(int i=0;i<12;i++) {
                float angle=i*Mathf.PI*2/12;
                Add(0,new Vector3(Mathf.Cos(angle)*radius,.07f,Mathf.Sin(angle)*radius),new Vector3(.10f,.14f,.10f),Quaternion.Euler(0,-angle*Mathf.Rad2Deg,0));
            }
            foreach(float side in new[]{-1f,1f})Beam(new Vector3(side*(radius+.12f),0,0),new Vector3(side*(radius+.12f),.65f,0),.035f);
            Beam(new Vector3(-radius-.12f,.65f,0),new Vector3(radius+.12f,.65f,0),.045f);
            Stockpile(radius+.15f,-radius,.2f);
        }
        private void Stockpile(float x,float z,float length)
        {
            for(int i=0;i<5;i++)Add(1,new Vector3(x,.035f+i*.027f,z),new Vector3(length,.023f,.055f),Quaternion.Euler(0,i%2==0?0:8,0));
            for(int i=0;i<6;i++)Add(0,new Vector3(x-.20f+(i%3)*.075f,.04f+(i/3)*.065f,z+.14f),new Vector3(.07f,.06f,.08f));
        }
        private void Add(int material,Vector3 position,Vector3 scale,Quaternion? rotation=null)=>parts[material].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(position,rotation??Quaternion.identity,scale)});
        private void Beam(Vector3 a,Vector3 b,float width)=>Add(1,(a+b)*.5f,new Vector3(width,(a-b).magnitude,width),Quaternion.FromToRotation(Vector3.up,b-a));
        private void Material(string name,Color color)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.08f);materials.Add(material);
        }
        private void ClearModel()
        {
            if(model!=null){model.SetActive(false);Release(model);model=null;}
            foreach(var mesh in meshes)Release(mesh);meshes.Clear();foreach(var list in parts)list.Clear();
        }
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){ClearModel();foreach(var material in materials)Release(material);materials.Clear();}
    }
}
