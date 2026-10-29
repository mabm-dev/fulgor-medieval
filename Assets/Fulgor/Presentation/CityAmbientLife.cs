using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fulgor.Core.Campaign;

namespace Fulgor.Presentation.Map
{
    /// <summary>Local visual town activity. Routes, waits and gait never affect the campaign.</summary>
    public sealed class CityAmbientLife : MonoBehaviour
    {
        private sealed class Actor
        {
            public Transform Root,Body;
            public Transform[] Limbs;
            public Vector3[] Route;
            public int Target;
            public float Speed,Phase,Wait;
            public bool Stationary,Animal;
        }
        private readonly List<Actor> actors=new();
        private readonly List<Material> materials=new();
        private readonly List<Mesh> meshes=new();
        private Material skin,hair,wood,wool,red,cream;
        private readonly Material[] clothing=new Material[5];
        private float elapsed;
        public int Residents { get; private set; }
        public int Animals { get; private set; }
        public float Elapsed=>elapsed;
        public bool Initialized { get; private set; }
        private static readonly Vector3[] Street={new(0,0,-3.6f),new(0,0,-2.5f),new(.6f,0,-2),new(.8f,0,-1),new(0,0,-.6f),new(-1.2f,0,-.7f),new(-1.35f,0,-1.8f),new(-.6f,0,-2.5f)};
        private void Start()=>Initialize();
        public void Initialize()
        {
            if(Initialized)return;Initialized=true;
            skin=Mat("Piel",new Color(.55f,.36f,.22f));hair=Mat("Cabello",new Color(.10f,.065f,.035f));wood=Mat("Cuero",new Color(.17f,.10f,.05f));
            wool=Mat("Lana",new Color(.72f,.70f,.58f));red=Mat("Rojo",new Color(.45f,.085f,.05f));cream=Mat("Lino",new Color(.65f,.57f,.40f));
            Color[] colors={new(.31f,.16f,.09f),new(.19f,.26f,.13f),new(.18f,.25f,.28f),new(.43f,.30f,.15f),new(.38f,.13f,.08f)};
            for(int i=0;i<5;i++)clothing[i]=Mat("Vestimenta "+i,colors[i]);
            for(int i=0;i<12;i++) {
                var route=new Vector3[Street.Length+1];int begin=i%Street.Length;bool reverse=i%2!=0;
                for(int j=0;j<Street.Length;j++)route[j]=Street[(begin+(reverse?-j:j)+Street.Length)%Street.Length];route[Street.Length]=route[0];
                var actor=Human(i,Vector3.Lerp(route[0],route[1],i>=8?.5f:.12f*(i%3)),false);actor.Route=route;actor.Target=1;actor.Speed=.18f+(i%4)*.022f;actor.Phase=i*.9f;
            }
            Human(12,new Vector3(-1.4f,0,-2.55f),true);Human(13,new Vector3(1.35f,0,-.45f),true);
            for(int i=0;i<4;i++)Chicken(i,new Vector3(2.25f+(i%2)*.28f,0,-3.35f-(i/2)*.25f));
            for(int i=0;i<3;i++)Sheep(i,new Vector3(-3.7f+(i%2)*.24f,0,.05f+(i/2)*.35f));
            Dog();
        }
        public void ApplyDevelopment(CityDevelopment state)
        {
            Initialize();Residents=0;Animals=0;int walkers=0,chickens=0;
            foreach(var actor in actors) {
                bool visible;
                if(!actor.Animal)visible=actor.Stationary?state.Has("market"):walkers++<Mathf.Min(12,state.Houses*2);
                else if(actor.Root.name.StartsWith("Gallina",StringComparison.Ordinal))visible=chickens++<(state.Has("market")?4:1);
                else if(actor.Root.name.StartsWith("Oveja",StringComparison.Ordinal))visible=state.Has("warehouse");
                else visible=true;
                actor.Root.gameObject.SetActive(visible);if(visible){if(actor.Animal)Animals++;else Residents++;}
            }
        }
        private Material Mat(string label,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Ciudad · "+label,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.1f);materials.Add(m);return m;
        }
        private Transform Node(string name,Transform parent,Vector3 position)
        {
            var root=new GameObject(name);root.layer=gameObject.layer;root.transform.SetParent(parent,false);root.transform.localPosition=position;return root.transform;
        }
        private Transform Part(string name,Transform parent,PrimitiveType primitive,Vector3 position,Vector3 scale,Material material)
        {
            var root=GameObject.CreatePrimitive(primitive);root.name=name;root.layer=gameObject.layer;root.transform.SetParent(parent,false);root.transform.localPosition=position;root.transform.localScale=scale;
            var collider=root.GetComponent<Collider>();collider.enabled=false;Release(collider);root.GetComponent<MeshRenderer>().sharedMaterial=material;return root.transform;
        }
        private Actor Human(int index,Vector3 position,bool stationary)
        {
            position.y=.018f;var root=Node("Vecino "+index,transform,position);var body=Node("Cuerpo",root,Vector3.zero);var cloth=clothing[index%5];
            Part("Túnica",body,PrimitiveType.Capsule,new Vector3(0,.25f,0),new Vector3(.13f,.09f,.085f),cloth);
            Part("Cinturón",body,PrimitiveType.Cube,new Vector3(0,.217f,0),new Vector3(.13f,.012f,.09f),wood);
            Part("Cabeza",body,PrimitiveType.Sphere,new Vector3(0,.382f,.004f),new Vector3(.086f,.103f,.086f),skin);
            Part("Cofia",body,PrimitiveType.Sphere,new Vector3(0,.410f,-.008f),new Vector3(.092f,.060f,.090f),index%3==0?cream:hair);
            Part("Nariz",body,PrimitiveType.Sphere,new Vector3(0,.383f,.046f),new Vector3(.021f,.025f,.024f),skin);
            var limbs=new Transform[4];
            for(int side=0;side<2;side++) {
                float sign=side==0?-1:1;var leg=Node("Pierna "+side,body,new Vector3(sign*.035f,.18f,0));
                Part("Calza",leg,PrimitiveType.Capsule,new Vector3(0,-.075f,0),new Vector3(.038f,.073f,.040f),wood);
                Part("Zapato",leg,PrimitiveType.Cube,new Vector3(0,-.165f,.016f),new Vector3(.045f,.027f,.075f),hair);limbs[side]=leg;
                var arm=Node("Brazo "+side,body,new Vector3(sign*.080f,.30f,0));
                Part("Manga",arm,PrimitiveType.Capsule,new Vector3(0,-.053f,0),new Vector3(.041f,.055f,.044f),cloth);
                Part("Mano",arm,PrimitiveType.Sphere,new Vector3(0,-.115f,0),new Vector3(.035f,.042f,.035f),skin);limbs[side+2]=arm;
            }
            if(index%3==1)Part("Falda",body,PrimitiveType.Cylinder,new Vector3(0,.17f,0),new Vector3(.15f,.055f,.13f),cloth);
            var actor=new Actor{Root=root,Body=body,Limbs=limbs,Stationary=stationary,Route=new[]{position},Phase=index*.9f};actors.Add(actor);Residents++;return actor;
        }
        private Vector3[] PenRoute(Vector3 center,float radius,int count)
        {
            var route=new Vector3[count];for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;route[i]=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;}return route;
        }
        private void Chicken(int index,Vector3 center)
        {
            center.y=.018f;var root=Node("Gallina "+index,transform,center);var body=Node("Cuerpo",root,new Vector3(0,.07f,0));var color=index%2==0?wool:clothing[3];
            Part("Plumaje",body,PrimitiveType.Sphere,Vector3.zero,new Vector3(.095f,.09f,.14f),color);
            Part("Cabeza",body,PrimitiveType.Sphere,new Vector3(0,.05f,.065f),Vector3.one*.055f,color);
            Part("Cresta",body,PrimitiveType.Cube,new Vector3(0,.082f,.065f),new Vector3(.012f,.028f,.038f),red);
            Part("Pico",body,PrimitiveType.Sphere,new Vector3(0,.045f,.097f),new Vector3(.025f,.018f,.030f),cream);
            var tail=Part("Cola",body,PrimitiveType.Cube,new Vector3(0,.02f,-.08f),new Vector3(.04f,.065f,.02f),color);tail.localRotation=Quaternion.Euler(-25,0,0);
            var limbs=new Transform[2];for(int i=0;i<2;i++)limbs[i]=Part("Pata "+i,body,PrimitiveType.Cylinder,new Vector3(i==0?-.025f:.025f,-.045f,0),new Vector3(.008f,.026f,.008f),cream);
            actors.Add(new Actor{Root=root,Body=body,Limbs=limbs,Route=PenRoute(center,.16f,5),Target=1,Speed=.065f,Phase=index,Animal=true});Animals++;
        }
        private void Sheep(int index,Vector3 center)
        {
            center.y=.018f;var root=Node("Oveja "+index,transform,center);var body=Node("Cuerpo",root,Vector3.zero);
            Part("Lana",body,PrimitiveType.Sphere,new Vector3(0,.16f,0),new Vector3(.17f,.17f,.28f),wool);
            for(int i=0;i<3;i++)Part("Mechón",body,PrimitiveType.Sphere,new Vector3(i==0?-.065f:.065f,.19f,(i-1)*.06f),new Vector3(.09f,.12f,.10f),wool);
            Part("Cabeza",body,PrimitiveType.Sphere,new Vector3(0,.19f,.17f),new Vector3(.085f,.10f,.09f),wood);
            for(int i=0;i<2;i++)Part("Oreja",body,PrimitiveType.Cube,new Vector3(i==0?-.06f:.06f,.21f,.17f),new Vector3(.05f,.018f,.025f),wood);
            var limbs=FourLegs(body,.055f,.09f,.085f);
            actors.Add(new Actor{Root=root,Body=body,Limbs=limbs,Route=PenRoute(center,.15f,6),Target=1,Speed=.045f,Phase=index*2,Animal=true});Animals++;
        }
        private Transform[] FourLegs(Transform body,float x,float z,float height)
        {
            var limbs=new Transform[4];for(int i=0;i<4;i++)limbs[i]=Part("Pata "+i,body,PrimitiveType.Cylinder,new Vector3(i%2==0?-x:x,height,i<2?-z:z),new Vector3(.024f,height,.025f),wood);return limbs;
        }
        private void Dog()
        {
            var root=Node("Perro de la plaza",transform,new Vector3(0,.018f,-2.5f));var body=Node("Cuerpo",root,Vector3.zero);
            Part("Lomo",body,PrimitiveType.Sphere,new Vector3(0,.135f,0),new Vector3(.12f,.12f,.25f),wood);
            Part("Cabeza",body,PrimitiveType.Sphere,new Vector3(0,.19f,.13f),new Vector3(.09f,.1f,.10f),wood);
            Part("Hocico",body,PrimitiveType.Cube,new Vector3(0,.175f,.19f),new Vector3(.055f,.045f,.075f),hair);
            for(int i=0;i<2;i++)Part("Oreja",body,PrimitiveType.Cube,new Vector3(i==0?-.04f:.04f,.23f,.12f),new Vector3(.027f,.06f,.035f),hair);
            var tail=Part("Cola",body,PrimitiveType.Capsule,new Vector3(0,.17f,-.17f),new Vector3(.02f,.07f,.025f),wood);tail.localRotation=Quaternion.Euler(-40,0,0);
            actors.Add(new Actor{Root=root,Body=body,Limbs=FourLegs(body,.04f,.08f,.06f),Route=Street,Target=1,Speed=.28f,Phase=2,Animal=true});Animals++;
        }
        private void Update()=>Advance(Time.deltaTime);
        public void Advance(float seconds)
        {
            if(!Initialized)Initialize();if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            seconds=Mathf.Min(seconds,.1f);elapsed+=seconds;
            foreach(var actor in actors) {
                if(!actor.Root.gameObject.activeSelf)continue;
                bool walking=false;
                if(!actor.Stationary) {
                    actor.Wait=Mathf.Max(0,actor.Wait-seconds);
                    if(actor.Wait<=0) {
                        var goal=actor.Route[actor.Target];goal.y=.018f;
                        var direction=goal-actor.Root.localPosition;direction.y=0;
                        if(direction.sqrMagnitude<.0144f) {actor.Target=(actor.Target+1)%actor.Route.Length;actor.Wait=.4f+Mathf.Abs(Mathf.Sin(elapsed+actor.Phase))*2.4f;}
                        else {
                            walking=true;var sideways=Vector3.Cross(Vector3.up,direction.normalized)*.08f;
                            var target=goal+sideways;target.y=.018f;actor.Root.localPosition=Vector3.MoveTowards(actor.Root.localPosition,target,actor.Speed*seconds);
                            actor.Root.localRotation=Quaternion.RotateTowards(actor.Root.localRotation,Quaternion.LookRotation(direction),seconds*150);
                        }
                    }
                }
                float gait=Mathf.Sin(elapsed*(actor.Animal?12:8)+actor.Phase)*(walking?1:.08f);
                for(int i=0;i<actor.Limbs.Length;i++)actor.Limbs[i].localRotation=Quaternion.Euler(gait*(i%2==0?1:-1)*(i>=2&&!actor.Animal?-1:1)*(actor.Animal?18:26),0,0);
                if(!actor.Animal)actor.Body.localPosition=new Vector3(0,Mathf.Abs(gait)*.004f,0);
            }
        }
        public Vector3[] Positions()=>actors.Where(a=>a.Root.gameObject.activeSelf).Select(a=>a.Root.localPosition).ToArray();
        private static void Release(UnityEngine.Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        private void OnDestroy(){foreach(var material in materials)Release(material);foreach(var mesh in meshes)Release(mesh);}
    }
}
