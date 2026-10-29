using System.Collections.Generic;
using Fulgor.Core.Combat;
using Fulgor.Core.Map;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fulgor.Presentation.Map
{
    /// <summary>Read-only diorama. Tactical positions, casualties and orders remain in the combat engine.</summary>
    public sealed class BattleGraphicalStage : MonoBehaviour
    {
        public const int StageLayer=29;
        private readonly Dictionary<HexCoordinates,GameObject> tiles=new();
        private readonly Dictionary<HexCoordinates,LineRenderer> edges=new();
        private readonly Dictionary<int,Transform> units=new();
        private readonly Dictionary<int,List<GameObject>> figurines=new();
        private readonly Dictionary<int,Vector3> destinations=new();
        private readonly Dictionary<int,int> quantities=new();
        private readonly List<Material> materials=new();
        private readonly List<Mesh> meshes=new();
        private readonly Dictionary<Material,List<MeshFilter>> scenery=new();
        private readonly Dictionary<int,LineRenderer> rings=new();
        private Material ground,woodland,rough,wood,iron,skin,blue,red,gold,leaf,stone,shadow,trim;
        private LineRenderer route;
        private LineRenderer projectile;
        private int? pendingRoute,attacking,impacted;
        private float attackTime=-10;
        private Camera mainCamera;
        private int oldMask;
        private bool capturedCamera;
        private Vector3 center;
        private float baseSize,zoom=1;
        private int observedRevision=-1;
        private float impactUntil;
        private readonly Dictionary<int,long> health=new();
        private readonly Dictionary<int,Vector3> motionStart=new();
        private readonly Dictionary<int,List<Vector3>> motionRoutes=new();
        private float motionTime;
        public Camera Camera {get;private set;}
        public RenderTexture Texture {get;private set;}
        public int CellCount=>tiles.Count;
        public int FormationCount=>units.Count;
        public float Zoom=>zoom;
        private Vector2 cameraPan;
        public Vector2 CameraPan=>cameraPan;
        public bool MotionPlaying=>Time.unscaledTime<motionTime+.7f;
        public Vector3 PositionOf(int id)=>units[id].position;
        public Vector3 DestinationOf(int id)=>destinations[id];
        public bool IsFormationVisible(int id)=>units.TryGetValue(id,out var unit)&&unit.gameObject.activeSelf;
        public int VisibleFigurines(int id)=>figurines.TryGetValue(id,out var list)?list.FindAll(p=>p.activeSelf).Count:0;
        public IReadOnlyList<HexCoordinates> Cells {get;private set;}
        public static Vector3 CellPosition(HexCoordinates cell)=>new(1.7320508f*(cell.Q+cell.R*.5f),0,-1.5f*cell.R);

        public void Initialize(BattleLabController controller)
        {
            transform.position=new Vector3(20000,0,20000);gameObject.layer=StageLayer;
            var shader=Resources.Load<Shader>("BattleSurface");
            if(shader==null)throw new System.InvalidOperationException("Battle surface shader is missing.");
            Material Mat(string name,Color color){var m=new Material(shader){name=name};m.color=color;m.SetVector("_Origin",transform.position);materials.Add(m);return m;}
            ground=Mat("Pradera",new Color(.43f,.46f,.29f));woodland=Mat("Sotobosque",new Color(.27f,.34f,.22f));rough=Mat("Tierra pedregosa",new Color(.48f,.43f,.33f));
            wood=Mat("Roble",new Color(.31f,.20f,.11f));iron=Mat("Acero",new Color(.54f,.57f,.57f));skin=Mat("Piel",new Color(.73f,.54f,.36f));
            blue=Mat("Tus colores",new Color(.16f,.40f,.63f));red=Mat("Colores rivales",new Color(.65f,.22f,.19f));gold=Mat("Oro",new Color(.96f,.76f,.34f));
            leaf=Mat("Copa de encina",new Color(.28f,.38f,.18f));stone=Mat("Piedra",new Color(.52f,.51f,.43f));shadow=Mat("Contacto",new Color(.19f,.22f,.15f));trim=Mat("Bordes",new Color(.36f,.37f,.26f));
            ground.SetFloat("_Weathering",.16f);woodland.SetFloat("_Weathering",.12f);rough.SetFloat("_Weathering",.14f);
            Cells=controller.VisualCommands.BattlefieldCells;
            var bounds=new Bounds(CellPosition(Cells[0]),Vector3.zero);
            foreach(var cell in Cells)bounds.Encapsulate(CellPosition(cell));
            center=bounds.center;
            BuildTerrain(controller.VisualCommands);
            foreach(int id in controller.VisualRoster.FormationIds)BuildFormation(controller,id);
            CombineScenery();
            var cameraRoot=new GameObject("Cámara de batalla");cameraRoot.transform.SetParent(transform,false);Camera=cameraRoot.AddComponent<Camera>();
            Camera.cullingMask=1<<StageLayer;Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=new Color(.065f,.091f,.088f);
            Camera.orthographic=true;Camera.nearClipPlane=.1f;Camera.farClipPlane=100;Camera.allowHDR=false;Camera.allowMSAA=true;
            Texture=new RenderTexture(1600,960,24,RenderTextureFormat.ARGB32){name="Campo de batalla",antiAliasing=4};Texture.Create();Camera.targetTexture=Texture;
            // Only suppress rendering. Never change the campaign camera's transform or zoom.
            mainCamera=UnityEngine.Camera.main;
            if(mainCamera!=null){capturedCamera=true;oldMask=mainCamera.cullingMask;mainCamera.cullingMask=0;}
            route=Line("Ruta de movimiento",transform,gold,.055f);route.gameObject.SetActive(false);
            projectile=Line("Disparo",transform,gold,.04f);projectile.positionCount=2;projectile.gameObject.SetActive(false);
            FitCamera(1.67f);Sync(controller,true);
        }

        private void BuildTerrain(BattleAttackResolver commands)
        {
            var random=new System.Random(24971);
            foreach(var cell in Cells)
            {
                commands.TryGetTerrain(cell,out var terrain);
                var root=new GameObject("Terreno "+cell.Key);root.transform.SetParent(transform,false);root.transform.localPosition=CellPosition(cell);root.layer=StageLayer;tiles.Add(cell,root);
                var mesh=HexMesh(1,-.22f,random);meshes.Add(mesh);
                var filter=root.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
                var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=terrain==TacticalTerrain.Clear?ground:terrain==TacticalTerrain.Woodland?woodland:rough;
                renderer.shadowCastingMode=ShadowCastingMode.Off;AddStatic(filter,renderer.sharedMaterial);
                var edge=Line("Límite hexagonal",root.transform,trim,.013f);edge.loop=true;edge.positionCount=6;
                for(int i=0;i<6;i++)edge.SetPosition(i,Corner(i,.983f)+Vector3.up*.018f);edges.Add(cell,edge);
                // Keep the middle free so trees and rocks never hide formations or their banners.
                int count=terrain==TacticalTerrain.Woodland?3:terrain==TacticalTerrain.Rough?4:1;
                for(int i=0;i<count;i++)
                {
                    float angle=(float)random.NextDouble()*Mathf.PI*2;
                    var at=new Vector3(Mathf.Cos(angle)*.73f,0,Mathf.Sin(angle)*.73f);
                    if(terrain==TacticalTerrain.Woodland)
                    {
                        Piece(root.transform,PrimitiveType.Cylinder,at+Vector3.up*.24f,new Vector3(.075f,.24f,.075f),wood,true);
                        for(int crown=0;crown<3;crown++)
                        {
                            var offset=new Vector3(Mathf.Cos(crown*2.1f+i)*.12f,.56f+crown*.075f,Mathf.Sin(crown*2.1f+i)*.12f);
                            var foliage=Piece(root.transform,PrimitiveType.Sphere,at+offset,new Vector3(.4f,.4f,.38f),leaf,true);foliage.localRotation=Quaternion.Euler(0,i*37,12);
                        }
                    }
                    else if(terrain==TacticalTerrain.Rough)
                    {var rock=Piece(root.transform,PrimitiveType.Cube,at+Vector3.up*.065f,new Vector3(.19f,.13f,.14f),stone,true);rock.localRotation=Quaternion.Euler(8,i*63,12);}
                    else
                    {
                        Piece(root.transform,PrimitiveType.Cube,at+Vector3.up*.025f,new Vector3(.026f,.055f,.12f),leaf,true);
                        Piece(root.transform,PrimitiveType.Cube,at+Vector3.up*.025f,new Vector3(.12f,.045f,.025f),leaf,true);
                    }
                }
            }
        }

        private void BuildFormation(BattleLabController controller,int id)
        {
            var state=controller.VisualRoster.GetFormation(id);
            var own=state.Slot.Side==controller.PlayerBattleSide;var cloth=own?blue:red;
            var root=new GameObject("Formación "+id);root.transform.SetParent(transform,false);root.layer=StageLayer;
            units.Add(id,root.transform);figurines.Add(id,new List<GameObject>());
            var ring=Line("Bando y selección",root.transform,cloth,.048f);ring.loop=true;ring.positionCount=40;
            for(int i=0;i<40;i++){float a=i*Mathf.PI/20;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.69f,.045f,Mathf.Sin(a)*.69f));}rings.Add(id,ring);
            var body=new GameObject("Tropa");body.transform.SetParent(root.transform,false);body.transform.localRotation=Quaternion.Euler(0,state.Slot.Side==BattleSide.Attacker?90:-90,0);
            bool horse=state.Slot.Kind==FormationKind.Cavalry;
            int total=horse?4:9;
            for(int i=0;i<total;i++)
            {
                var man=new GameObject(horse?"Jinete":"Soldado");man.transform.SetParent(body.transform,false);man.layer=StageLayer;
                man.transform.localPosition=new Vector3((i%(horse?2:3)-(horse?.5f:1))*(horse?.48f:.32f),0,(i/(horse?2:3)-(horse?.5f:1))*(horse?.43f:.34f));figurines[id].Add(man);
                float seat=horse?.32f:0;
                Piece(man.transform,PrimitiveType.Cylinder,new Vector3(0,.012f,0),new Vector3(.19f,.005f,horse?.32f:.16f),shadow);
                if(horse)
                {
                    Piece(man.transform,PrimitiveType.Capsule,new Vector3(0,.23f,0),new Vector3(.2f,.2f,.38f),wood).localRotation=Quaternion.Euler(90,0,0);
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(0,.38f,.15f),new Vector3(.14f,.22f,.12f),wood);
                    for(int k=0;k<4;k++)Piece(man.transform,PrimitiveType.Cube,new Vector3(k%2==0?-.065f:.065f,.1f,k<2?-.1f:.1f),new Vector3(.035f,.2f,.04f),wood);
                }
                Piece(man.transform,PrimitiveType.Capsule,new Vector3(0,seat+.24f,0),new Vector3(.13f,.18f,.13f),cloth);
                Piece(man.transform,PrimitiveType.Sphere,new Vector3(0,seat+.43f,0),Vector3.one*.12f,skin);
                Piece(man.transform,PrimitiveType.Sphere,new Vector3(0,seat+.47f,-.012f),new Vector3(.14f,.095f,.14f),iron);
                for(int leg=0;leg<2;leg++)Piece(man.transform,PrimitiveType.Cube,new Vector3(leg==0?-.04f:.04f,seat+.075f,0),new Vector3(.045f,.15f,.055f),wood);
                if(state.Slot.Kind==FormationKind.Ranged)
                {
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(.08f,seat+.29f,.1f),new Vector3(.025f,.03f,.26f),wood);
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(.08f,seat+.29f,.11f),new Vector3(.22f,.025f,.025f),iron);
                }
                else
                {
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(.12f,seat+.4f,.04f),new Vector3(.022f,.8f,.022f),wood);
                    Piece(man.transform,PrimitiveType.Sphere,new Vector3(.12f,seat+.82f,.04f),new Vector3(.038f,.13f,.038f),iron);
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(-.1f,seat+.27f,.07f),new Vector3(.12f,.18f,.045f),cloth);
                    Piece(man.transform,PrimitiveType.Cube,new Vector3(-.1f,seat+.27f,.095f),new Vector3(.025f,.13f,.008f),gold);
                }
            }
            Piece(body.transform,PrimitiveType.Cube,new Vector3(-.49f,.67f,0),new Vector3(.025f,1.34f,.025f),wood);
            Piece(body.transform,PrimitiveType.Cube,new Vector3(-.34f,1.18f,0),new Vector3(.3f,.25f,.02f),cloth);
            Piece(body.transform,PrimitiveType.Cube,new Vector3(-.34f,1.18f,.014f),new Vector3(.045f,.2f,.009f),gold);
        }

        public void FitCamera(float aspect)
        {
            if(Camera==null)return;
            Camera.aspect=Mathf.Clamp(aspect,.5f,4);
            // Extents in the rotated camera plane, including banners and border hexes.
            Camera.transform.localRotation=Quaternion.Euler(58,0,0);
            Camera.transform.localPosition=center-Camera.transform.forward*35+Vector3.up*.5f;
            float halfX=0,halfY=0;
            foreach(var cell in Cells)for(int i=0;i<6;i++)foreach(float height in new[]{-.3f,1.7f})
            {
                var p=Camera.transform.InverseTransformPoint(transform.TransformPoint(CellPosition(cell)+Corner(i,1)+Vector3.up*height));
                halfX=Mathf.Max(halfX,Mathf.Abs(p.x));halfY=Mathf.Max(halfY,Mathf.Abs(p.y));
            }
            baseSize=Mathf.Max(halfY,halfX/Camera.aspect)*1.09f;ApplyCamera();
        }
        public void SetZoom(float value){zoom=Mathf.Clamp(value,.4f,1.12f);ApplyCamera();}
        public void PanView(Vector2 delta){cameraPan+=delta*baseSize*zoom;ApplyCamera();}
        public void DragView(Vector2 normalizedDelta){PanView(-normalizedDelta*new Vector2(2*Camera.aspect,2/Mathf.Sin(58*Mathf.Deg2Rad)));}
        public void ResetView(){cameraPan=Vector2.zero;zoom=1;ApplyCamera();}
        private void ApplyCamera()
        {
            if(Camera==null)return;
            cameraPan=Vector2.ClampMagnitude(cameraPan,baseSize*1.2f);
            Camera.orthographicSize=baseSize*zoom;
            Camera.transform.localPosition=center-Camera.transform.forward*35+Vector3.up*.5f+new Vector3(cameraPan.x,0,cameraPan.y);
        }
        public bool TryPick(Vector2 viewport,out HexCoordinates result)
        {
            result=default;if(Camera==null)return false;
            var ray=Camera.ViewportPointToRay(viewport);var plane=new Plane(Vector3.up,transform.position);
            if(!plane.Raycast(ray,out float distance))return false;
            var local=transform.InverseTransformPoint(ray.GetPoint(distance));
            float best=1.01f;bool found=false;
            foreach(var cell in Cells){float d=(CellPosition(cell)-local).sqrMagnitude;if(d<best){result=cell;best=d;found=true;}}
            if(!found)return false;
            var delta=local-CellPosition(result);
            return Mathf.Abs(delta.x)<=.8661f&&Mathf.Abs(delta.z)+Mathf.Abs(delta.x)*.5773503f<=1.0001f;
        }
        public void Highlight(HashSet<HexCoordinates> reachable,HashSet<HexCoordinates> targets,HexCoordinates? selected,MapPath path)
        {
            foreach(var pair in edges)
            {
                bool focus=selected.HasValue&&pair.Key==selected.Value;
                pair.Value.sharedMaterial=focus?gold:targets.Contains(pair.Key)?red:reachable.Contains(pair.Key)?blue:trim;
                pair.Value.startWidth=pair.Value.endWidth=focus?.07f:targets.Contains(pair.Key)||reachable.Contains(pair.Key)?.035f:.013f;
            }
            route.gameObject.SetActive(path!=null&&path.Cells.Count>1);
            if(path!=null){route.positionCount=path.Cells.Count;for(int i=0;i<path.Cells.Count;i++)route.SetPosition(i,CellPosition(path.Cells[i])+Vector3.up*.08f);}
        }
        public void Sync(BattleLabController controller,bool immediate=false)
        {
            var roster=controller.VisualRoster;
            if(observedRevision!=controller.VisualRevision||immediate)
            {
                motionTime=Time.unscaledTime;
                foreach(int id in roster.FormationIds)
                {
                    var state=roster.GetFormation(id);bool visible=controller.VisualCommands.TryGetPosition(id,out var cell);
                    var goal=visible?transform.TransformPoint(CellPosition(cell)):destinations.TryGetValue(id,out var previous)?previous:units[id].position;
                    motionStart[id]=immediate?goal:units[id].position;
                    if(immediate||!units[id].gameObject.activeSelf)units[id].position=goal;
                    if(pendingRoute!=id)motionRoutes.Remove(id);
                    destinations[id]=goal;units[id].gameObject.SetActive(visible);
                    if(health.TryGetValue(id,out long before)&&state.TotalHealth<before)impactUntil=Time.unscaledTime+.6f;
                    health[id]=state.TotalHealth;quantities[id]=state.Quantity;
                    int count=state.IsInBattle?Mathf.Clamp(Mathf.CeilToInt((float)state.Quantity/state.InitialQuantity*figurines[id].Count),1,figurines[id].Count):0;
                    for(int i=0;i<figurines[id].Count;i++)figurines[id][i].SetActive(i<count);
                }
                observedRevision=controller.VisualRevision;
                pendingRoute=null;
            }
            foreach(int id in roster.FormationIds)
            {
                float t=immediate?1:Mathf.Clamp01((Time.unscaledTime-motionTime)/.65f);
                Vector3 at=Vector3.Lerp(motionStart[id],destinations[id],Mathf.SmoothStep(0,1,t));
                if(!immediate&&motionRoutes.TryGetValue(id,out var steps)&&steps.Count>1)
                {float progress=t*(steps.Count-1);int index=Mathf.Min(Mathf.FloorToInt(progress),steps.Count-2);at=Vector3.Lerp(steps[index],steps[index+1],progress-index);}
                if(t<1)at.y+=Mathf.Sin(t*Mathf.PI*8)*.025f;
                units[id].position=at;
                bool active=roster.ActiveFormationId==id;rings[id].sharedMaterial=active?gold:roster.GetFormation(id).Slot.Side==controller.PlayerBattleSide?blue:red;
                rings[id].startWidth=rings[id].endWidth=active?.075f:.045f;
                if(impactUntil>Time.unscaledTime&&impacted==id)
                    units[id].position+=Vector3.right*(Mathf.Sin(Time.unscaledTime*65)*.035f);
            }
            float attackProgress=(Time.unscaledTime-attackTime)/.65f;
            bool show=attacking.HasValue&&impacted.HasValue&&attackProgress>=0&&attackProgress<1;
            bool ranged=show&&roster.GetFormation(attacking.Value).Slot.Kind==FormationKind.Ranged;
            projectile.gameObject.SetActive(ranged);
            if(show)
            {
                var from=destinations[attacking.Value]+Vector3.up*.5f;var to=destinations[impacted.Value]+Vector3.up*.4f;
                var at=Vector3.Lerp(from,to,attackProgress)+Vector3.up*(Mathf.Sin(attackProgress*Mathf.PI)*.65f);
                projectile.SetPosition(0,transform.InverseTransformPoint(at));projectile.SetPosition(1,transform.InverseTransformPoint(at+(to-from).normalized*.3f));
                if(!ranged&&units[attacking.Value].gameObject.activeSelf)units[attacking.Value].position+=(to-from).normalized*(Mathf.Sin(attackProgress*Mathf.PI)*.2f);
            }
        }
        public void QueueOrder(BattleRecordedOrder order,MapPath path)
        {
            if(path!=null){pendingRoute=order.actorId;var points=new List<Vector3>();foreach(var cell in path.Cells)points.Add(transform.TransformPoint(CellPosition(cell)));motionRoutes[order.actorId]=points;}
            if(order.kind==BattleRecordedOrderKind.Attack){attacking=order.actorId;impacted=order.targetId;attackTime=Time.unscaledTime;}
        }
        private HexCoordinates Nearest(Vector3 world)
        {var local=transform.InverseTransformPoint(world);HexCoordinates result=Cells[0];float best=float.MaxValue;foreach(var cell in Cells){float d=(CellPosition(cell)-local).sqrMagnitude;if(d<best){best=d;result=cell;}}return result;}
        private static Vector3 Corner(int i,float radius){float a=(30+i*60)*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}
        private static Mesh HexMesh(float radius,float bottom,System.Random random)
        {
            var verts=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color){int at=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);indices.Add(at);indices.Add(at+1);indices.Add(at+2);}
            for(int i=0;i<6;i++)
            {
                var a=Corner(i,radius);var b=Corner((i+1)%6,radius);float shade=.94f+(float)random.NextDouble()*.12f;
                Triangle(Vector3.zero,b,a,new Color(shade,shade,shade,1));
                Triangle(a,b,a+Vector3.up*bottom,new Color(.68f,.65f,.59f));Triangle(b,b+Vector3.up*bottom,a+Vector3.up*bottom,new Color(.68f,.65f,.59f));
            }
            var mesh=new Mesh{name="Hexágono de terreno"};mesh.SetVertices(verts);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();return mesh;
        }
        private Transform Piece(Transform parent,PrimitiveType type,Vector3 at,Vector3 scale,Material material,bool isScenery=false)
        {
            var piece=GameObject.CreatePrimitive(type);piece.name=material.name;piece.transform.SetParent(parent,false);piece.transform.localPosition=at;piece.transform.localScale=scale;piece.layer=StageLayer;
            var collider=piece.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Destroy(collider);}
            var renderer=piece.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(isScenery)AddStatic(piece.GetComponent<MeshFilter>(),material);return piece.transform;
        }
        private void AddStatic(MeshFilter filter,Material material){if(!scenery.TryGetValue(material,out var list))scenery.Add(material,list=new List<MeshFilter>());list.Add(filter);}
        private void CombineScenery()
        {
            foreach(var pair in scenery)
            {
                var inputs=new List<CombineInstance>();
                foreach(var filter in pair.Value){inputs.Add(new CombineInstance{mesh=filter.sharedMesh,transform=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});filter.GetComponent<MeshRenderer>().enabled=false;}
                var mesh=new Mesh{name="Escenario · "+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(inputs.ToArray());meshes.Add(mesh);
                var root=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));root.transform.SetParent(transform,false);root.layer=StageLayer;root.GetComponent<MeshFilter>().sharedMesh=mesh;root.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;
            }
        }
        private static LineRenderer Line(string name,Transform parent,Material mat,float width)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.layer=StageLayer;var line=root.AddComponent<LineRenderer>();line.sharedMaterial=mat;line.useWorldSpace=false;
            line.startWidth=line.endWidth=width;line.numCornerVertices=2;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.alignment=LineAlignment.View;return line;
        }
        public void Release()
        {
            if(capturedCamera&&mainCamera!=null)mainCamera.cullingMask=oldMask;capturedCamera=false;
            if(Camera!=null){Camera.enabled=false;Camera.targetTexture=null;}
            if(Texture!=null){Texture.Release();Destroy(Texture);Texture=null;}
            foreach(var material in materials)if(material!=null)Destroy(material);materials.Clear();
            foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);meshes.Clear();gameObject.SetActive(false);
        }
        private void OnDestroy(){Release();}
    }
}
