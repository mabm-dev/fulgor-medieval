"""Anatomical revision of Rodrigo. MakeHuman core graphical assets: CC0.

No MakeHuman application code is imported. Parse the documented OBJ/delta
formats and retain upstream source hashes/licences under ArtSource/ThirdParty.
Costume helpers and initial clothing volumes come from the project's own V01.
"""
from pathlib import Path
import bpy, math, random, json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from math import sin,cos,pi,exp
SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[3]
old=(SOURCE.parent/'CastillaRodrigoV01/crear_rodrigo_escultura_v01.py').read_text(encoding='utf8')
assert old.count('# Anatomical head:')==1 and old.count('# Stage and cameras;')==1,'V01 helper boundaries changed'
prefix=old.split('# Anatomical head:')[0]
prefix=prefix.replace("OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV01'","OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV03'")
prefix=prefix.replace("scene.name='Rodrigo_Estudio_geometrico_V01'","scene.name='Rodrigo_Rostro_Peinado_V03'")
exec(compile(prefix,str(SOURCE/'base_costume_from_v01.py'),'exec'),globals())
UPSTREAM=ROOT/'ArtSource/ThirdParty/MakeHumanCore'

# Soften hard regularity in the leather and put compression folds into cloth.
for ob in list(hero.objects):
    if ob.name.startswith('Gorguera'):ob.location.z+=.009
    if ob.name.startswith('Cuello_anatomico'):bpy.data.objects.remove(ob,do_unlink=True);continue
    if ob.type!='MESH':continue
    if ob.name.startswith('Lamina_cuero_torso'):
        for p in ob.data.vertices:
            x,y,z=p.co
            p.co.z+=.004*sin(x*17+z*9)+.002*sin(x*31-z*4)
            p.co.y+=.0016*sin(x*71+z*45)
    if ob.name.startswith('Pantalon'):
        for p in ob.data.vertices:
            x,y,z=p.co;cx=ob.location.x
            wave=.0026*sin(z*190+x*23)*exp(-((z-.44)/.11)**2)
            p.co.y+=wave;p.co.x+=.0016*sin(z*140)*exp(-((z-.46)/.12)**2)
    if ob.name.startswith('Brazal_cuero'):
        for p in ob.data.vertices:p.co.y+=.001*sin(p.co.z*155+p.co.x*83)

# OBJ and target parsing: all indexing refers to the untouched upstream OBJ.
verts=[];body_faces=[];groups={};group='';tex=[];body_uv=[]
for row in (UPSTREAM/'base.obj').read_text().splitlines():
    s=row.split()
    if not s:continue
    if s[0]=='v':verts.append(Vector(tuple(float(q) for q in s[1:4])))
    elif s[0]=='vt':tex.append(tuple(float(q) for q in s[1:3]))
    elif s[0]=='g':group=s[1];groups[group]=set()
    elif s[0]=='f':
        ids=[int(q.split('/')[0])-1 for q in s[1:]]
        groups.setdefault(group,set()).update(ids)
        if group=='body':
            body_faces.append(ids);body_uv.append([int(q.split('/')[1])-1 for q in s[1:]])
weights={'caucasian-male-young.target':.72,'caucasian-male-old.target':.28,
         'head-rectangular.target':.35,'chin-bones-incr.target':.27,
         'nose-curve-convex.target':.20,'eyebrows-trans-down.target':.08,
         'eyebrows-trans-forward.target':.15}
for filename,w in weights.items():
    for row in (UPSTREAM/filename).read_text().splitlines():
        if row.startswith('#'):continue
        s=row.split()
        if len(s)==4:verts[int(s[0])]+=Vector(tuple(float(q)*w for q in s[1:]))
body_ids=set(i for f in body_faces for i in f)
top=max(verts[i].y for i in body_ids);offset=1.795-top*.104
def convert(p):return Vector((p.x*.104,-p.z*.104+.045,p.y*.104+offset))
def keep_head(i):
    p=convert(verts[i])
    return p.z>1.46
selected=[(f,uv) for f,uv in zip(body_faces,body_uv) if all(keep_head(i) for i in f)]
used=sorted(set(i for f,uv in selected for i in f));indices={i:j for j,i in enumerate(used)}
head=mesh('Cabeza_anatomica_CC0_adaptada',[convert(verts[i]) for i in used],[[indices[i] for i in f] for f,uv in selected],skin,2)
uv_layer=head.data.uv_layers.new(name='MakeHuman_OriginalUV')
for poly,(f,uv) in zip(head.data.polygons,selected):
    for loop,uv_index in zip(poly.loop_indices,uv):uv_layer.data[loop].uv=tex[uv_index]
import bmesh
bm=bmesh.new();bm.from_mesh(head.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,
                      plane_co=(0,0,1.515),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
bm.to_mesh(head.data);bm.free();head.data.update()
head['source']='MakeHuman core base + CC0 targets; see provenance.json'
head['portrait_likeness']='Adapted study, not approved or exact reconstruction'
head['target_weights']=json.dumps(weights)
eye_centers=[]
for name in ('joint-l-eye','joint-r-eye'):
    eye_centers.append(convert(sum((verts[i] for i in groups[name]),Vector())/len(groups[name])))
eye_z=sum(p.z for p in eye_centers)/2
print('ANATOMY_LANDMARKS',top,offset,[list(p) for p in eye_centers],flush=True)
# Apply the anatomical subdivision, then work in small geometric creases.
bpy.ops.object.select_all(action='DESELECT');head.select_set(True);bpy.context.view_layer.objects.active=head
for modifier in list(head.modifiers):bpy.ops.object.modifier_apply(modifier=modifier.name)
for vert in head.data.vertices:
    p=vert.co
    neck_limit=1.615 if p.y>.026 else 1.570
    if p.z<neck_limit:
        t=max(0,min(1,(p.z-1.515)/(neck_limit-1.515)));angle=math.atan2(p.x,-(p.y-.008))
        q=max(0,min(1,(p.z-1.548)/(neck_limit-1.548)));blend=1-q*q*(3-2*q)
        p.x=p.x*(1-blend)+((.047+.006*t)*sin(angle))*blend
        p.y=p.y*(1-blend)+(.008-(.046+.008*t)*cos(angle))*blend
    if p.y>-.025:continue
    for c in eye_centers:
        influence=exp(-((p.x-c.x)/.018)**4-((p.z-eye_z)/.012)**4)
        p.z+=((p.z-eye_z)*.55+.0006)*influence
    bridge=exp(-(p.x/.015)**4-((p.z-eye_z+.030)/.026)**4)
    p.x*=1+.075*bridge
    p.y-=.0012*bridge
    for dz in (.044,.056):
        crease=eye_z+dz+.003*cos(p.x*24)
        p.y+=.00038*exp(-((p.z-crease)/.0008)**2-(p.x/.052)**6)
    p.y-=.0019*exp(-((abs(p.x)-.015)/.013)**2-((p.z-eye_z-.017)/.008)**2)
    # Shallow paired glabellar furrows; preserve the underlying anatomy.
    for x in (-.008,.008):
        p.y+=.00045*exp(-((p.x-x)/.0008)**2-((p.z-eye_z-.030)/.010)**4)
head.data.update()

# Anatomical materials follow the model's regions instead of floating lip tubes.
headskin=skin.copy();headskin.name='Piel_rostro_variacion_anatomica'
head.data.materials.clear();head.data.materials.append(headskin)
attr=head.data.color_attributes.new(name='Complexion',type='FLOAT_COLOR',domain='CORNER')
mouth_z=eye_z-.064
def beard_density(p):
    xx=abs(p.x);z=p.z
    front=max(0,min(1,(-p.y-.012)/.035))
    high=eye_z-.074+.043*min(1,xx/.066)+.0022*sin(p.x*170)+.0009*sin(p.x*391)
    low=eye_z-.121+.024*min(1,xx/.070)**.8
    density=max(0,min(1,(high-z)/.010))*max(0,min(1,(z-low)/.007))*front
    if xx<.025 and z>mouth_z-.008:density=0
    moustache=exp(-(p.x/.026)**8-((z-(mouth_z+.010))/.0055)**6)*front
    moustache*=.72+.28*min(1,xx/.006)
    return max(density,moustache),moustache
for poly in head.data.polygons:
    for li in poly.loop_indices:
        p=head.data.vertices[head.data.loops[li].vertex_index].co
        front=max(0,min(1,(-p.y-.025)/.04))
        cheek=exp(-((abs(p.x)-.043)/.022)**2-((p.z-(eye_z-.029))/.025)**2)*front
        lips=exp(-(p.x/.022)**6-((p.z-mouth_z)/.006)**4)*front
        beard,moustache=beard_density(p)
        c=[.29+.026*cheek-.060*lips,.160-.012*cheek-.047*lips,.097-.009*cheek-.025*lips]
        c=[q*(1-.86*beard) for q in c]
        attr.data[li].color=(*c,1)
n=headskin.node_tree.nodes;l=headskin.node_tree.links;p=n.get('Principled BSDF')
col=n.new('ShaderNodeVertexColor');col.layer_name='Complexion'
l.new(col.outputs['Color'],p.inputs['Base Color']);p.inputs['Roughness'].default_value=.59
p.inputs['Subsurface Weight'].default_value=.07;p.inputs['Specular IOR Level'].default_value=.30
variation=n.new('ShaderNodeTexNoise');variation.inputs['Scale'].default_value=38;variation.inputs['Detail'].default_value=3
tone=n.new('ShaderNodeValToRGB');tone.color_ramp.elements[0].color=(.84,.79,.75,1);tone.color_ramp.elements[1].color=(1,.99,.97,1)
l.new(variation.outputs['Fac'],tone.inputs[0])
mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.65
l.new(col.outputs['Color'],mix.inputs[1]);l.new(tone.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],p.inputs['Base Color'])
# Natural male skin UV maps by Mindfront, selected from the official CC0 pack.
# Keep originals intact; colour treatment and beard remain project adjustments.
SKIN=ROOT/'ArtSource/ThirdParty/MakeHumanAkselSkin'
treatment=head.data.color_attributes.new(name='SkinTreatment',type='FLOAT_COLOR',domain='CORNER')
for poly in head.data.polygons:
    for li in poly.loop_indices:
        co=head.data.vertices[head.data.loops[li].vertex_index].co
        beard,_=beard_density(co);q=1-.86*beard
        treatment.data[li].color=(.96*q,.93*q,.88*q,1)
maps={}
for name,filename in [('Albedo','Aksel_Skin_diffuse.png'),('Normal','Aksel_Skin_NRM.png'),('Specular','Aksel_Skin_SPEC.png')]:
    path=(ROOT/'ArtSource/ThirdParty/MakeHumanToigoBronze/young_lightskinned_male_diffuse_Bronze.png') if name=='Albedo' else SKIN/filename
    image=bpy.data.images.load(str(path));image.pack()
    if name!='Albedo':image.colorspace_settings.name='Non-Color'
    node=n.new('ShaderNodeTexImage');node.image=image;node.label='CC0 '+('MargaretToigo' if name=='Albedo' else 'Mindfront')+' '+name;maps[name]=node
tv=n.new('ShaderNodeVertexColor');tv.layer_name='SkinTreatment'
mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
l.new(maps['Albedo'].outputs['Color'],mix.inputs[1]);l.new(tv.outputs['Color'],mix.inputs[2]);l.new(mix.outputs[0],p.inputs['Base Color'])
normal=n.new('ShaderNodeNormalMap');normal.uv_map='MakeHuman_OriginalUV';normal.inputs['Strength'].default_value=.20
l.new(maps['Normal'].outputs['Color'],normal.inputs['Color'])
old_bump=p.inputs['Normal'].links[0].from_node if p.inputs['Normal'].links else None
if old_bump and old_bump.type=='BUMP':l.new(normal.outputs['Normal'],old_bump.inputs['Normal'])
else:l.new(normal.outputs['Normal'],p.inputs['Normal'])
rough=n.new('ShaderNodeMapRange');rough.inputs['From Min'].default_value=0;rough.inputs['From Max'].default_value=1
rough.inputs['To Min'].default_value=.72;rough.inputs['To Max'].default_value=.42
l.new(maps['Specular'].outputs['Color'],rough.inputs['Value']);l.new(rough.outputs['Result'],p.inputs['Roughness'])
head['skin_asset']='CC0 MargaretToigo Bronze albedo + Mindfront Aksel normal/specular; ArtSource/ThirdParty provenance files'
# Real spheres fit inside the base's eyelids, with iris on the spherical surface.
from mathutils import Euler
eyeWhite.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.47,.41,.34,1)
eyeWhite.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.24
iris_mat=mat('Iris_avellana_fibras',(.08,.04,.012),.20)
nn=iris_mat.node_tree.nodes;ll=iris_mat.node_tree.links
vc=nn.new('ShaderNodeVertexColor');vc.layer_name='IrisColor'
ll.new(vc.outputs['Color'],nn.get('Principled BSDF').inputs['Base Color'])
nn.get('Principled BSDF').inputs['Coat Weight'].default_value=.32
nn.get('Principled BSDF').inputs['Coat Roughness'].default_value=.12
gaze=Euler((math.radians(8),0,math.radians(5))).to_matrix()
for c in eye_centers:
    r=.0119
    eye=ell('Globo_ocular_anatomico',c,(r,r,r),eyeWhite,64,40)
    vv=[];ff=[];colors=[];segments=96;rings=22
    for j in range(rings):
        rho=.00001+.00485*j/(rings-1)
        for i in range(segments):
            a=2*pi*i/segments
            local=Vector((rho*sin(a),-math.sqrt(r*r-rho*rho)-.00006,rho*cos(a)))
            vv.append(c+gaze@local)
            fiber=.65+.35*sin(57*a+rho*1200)*sin(23*a-rho*700)
            edge_factor=max(.15,min(1,(.00486-rho)/.0005))
            color=(.095*fiber*edge_factor,.047*fiber*edge_factor,.014*fiber*edge_factor)
            if rho<.00185:color=(.002,.0015,.001)
            colors.append((*color,1))
    for j in range(rings-1):
        for i in range(segments):q=j*segments+i;b=j*segments+(i+1)%segments;ff.append((q,b,b+segments,q+segments))
    ob=mesh('Iris_curvado_con_pupila',vv,ff,iris_mat)
    color_attr=ob.data.color_attributes.new(name='IrisColor',type='FLOAT_COLOR',domain='CORNER')
    for poly in ob.data.polygons:
        for li in poly.loop_indices:color_attr.data[li].color=colors[ob.data.loops[li].vertex_index]

bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
head_eval=head.evaluated_get(deps);hm=head_eval.to_mesh()
hv=[v.co.copy() for v in hm.vertices];hf=[tuple(f.vertices) for f in hm.polygons]
skin_bvh=BVHTree.FromPolygons(hv,hf)
head_eval.to_mesh_clear()
center=Vector((0,.015,eye_z+.005))
def scalp(a,t,extra=0):
    minimum=(.57 if cos(a)>0 else .94)*cos(a)-.17*sin(a)**2+.052*sin(5*a)+.020*sin(11*a+.8)
    phi=minimum+(pi/2-minimum)*t
    direction=Vector((sin(a)*cos(phi),-cos(a)*cos(phi),sin(phi)))
    pos,normal,idx,dist=skin_bvh.ray_cast(center,direction,1)
    if pos is None:pos=center+Vector((.077*direction.x,.09*direction.y,.113*direction.z));normal=direction
    fade=max(0,min(1,t*8))
    wave=(.0025*sin(a*8+t*18)+.0014*sin(a*13-t*11))*fade
    return pos+normal*(.002+.010*fade+wave+extra),normal

# A swept hairline and overlapping wavy locks replace the former conical cap.
v=[];f=[];N=112;J=42
for j in range(J):
    for i in range(N):
        a=2*pi*i/N;t=.065+.935*j/(J-1)
        pos,_=scalp(a,t);v.append(pos)
for j in range(J-1):
    for i in range(N):q=j*N+i;b=j*N+(i+1)%N;f.append((q,b,b+N,q+N))
cap_material=hairm[0].copy();cap_material.name='Cabello_subcapa_mate'
cap_material.node_tree.nodes.get('Principled BSDF').inputs['Specular IOR Level'].default_value=.025
cap_material.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.95
mesh('Cabello_masa_anatomica',v,f,cap_material,1)
def lock(name,points,normals,width,material):
    vv=[];ff=[];S=8
    for j,(p,n) in enumerate(zip(points,normals)):
        t=j/(len(points)-1);tangent=(points[min(j+1,len(points)-1)]-points[max(0,j-1)]).normalized()
        u=tangent.cross(n).normalized();w=u.cross(tangent).normalized()
        radius=width*(.12+.88*sin(pi*(.10+.87*t))**.7)
        for k in range(S):
            a=2*pi*k/S;vv.append(p+u*radius*cos(a)+w*radius*.45*sin(a))
    for j in range(len(points)-1):
        for k in range(S):q=j*S+k;b=j*S+(k+1)%S;ff.append((q,b,b+S,q+S))
    ff.append(tuple(reversed(range(S))));ff.append(tuple((len(points)-1)*S+k for k in range(S)))
    return mesh(name,vv,ff,material,1)
random.seed(3702)
for material in hairm:
    pp=material.node_tree.nodes.get('Principled BSDF');pp.inputs['Specular IOR Level'].default_value=.16
hair_curves=[]
for material in hairm:
    cu=bpy.data.curves.new('Cabello_fibras','CURVE');cu.dimensions='3D';cu.bevel_depth=.00013;cu.bevel_resolution=1
    ob=bpy.data.objects.new('Cabello_fibras',cu);hero.objects.link(ob);cu.materials.append(material);hair_curves.append(cu)
for guide in range(180):
    a0=random.uniform(-pi,pi);phase=random.uniform(0,2*pi)
    if cos(a0)>.35:
        start=random.uniform(-.025,.085);end=random.uniform(.65,.94)
        if guide%17==0:start,end=random.uniform(.40,.65),random.uniform(-.08,.02)
        sweep=(.45 if a0>.20 else -.45)*random.uniform(.6,1.2)
    else:
        start=random.uniform(.43,.94);end=random.uniform(-.18,.02)
        sweep=(.30 if a0>.20 else -.30)*random.uniform(.6,1.2)
    for strand in range(58):
        da=random.gauss(0,.014);dt=random.gauss(0,.009)
        cu=random.choices(hair_curves,[.45,.35,.17,.03])[0]
        sp=cu.splines.new('POLY');sp.points.add(21)
        for j in range(22):
            q=j/21;t=start*(1-q)+(end+dt)*q
            aa=a0+da+sweep*q+.047*sin(q*10+phase)*sin(pi*q)
            volume=.0015+.011*sin(pi*q)**2+.003*q*q
            pos,_=scalp(aa,t,volume)
            pos.z-=.003*q*q
            sp.points[j].co=(*pos,1);sp.points[j].radius=(.13+.87*sin(pi*(.08+.90*q)))*random.uniform(.86,1.08)
# Short undercoat fibers cover spaces between long guide groups, so a smooth
# dark scalp shell cannot appear as a solid plastic patch between waves.
for strand in range(10000):
    a0=random.uniform(-pi,pi);t0=random.uniform(.005,.96);length=random.uniform(.10,.23)
    sign=1 if cos(a0)>.35 else -1;phase=random.uniform(0,2*pi)
    cu=random.choices(hair_curves,[.55,.32,.11,.02])[0]
    sp=cu.splines.new('POLY');sp.points.add(8)
    for j in range(9):
        q=j/8;t=max(-.04,min(.998,t0+sign*length*q))
        pos,_=scalp(a0+.065*sin(q*5+phase),t,.001+.0015*sin(pi*q))
        sp.points[j].co=(*pos,1);sp.points[j].radius=.1+.65*sin(pi*(.08+.90*q))

# Groom directly on the anatomical surface: natural density boundaries and
# cheeks remain skin, rather than a separate angular beard mask.
head_eval=head.evaluated_get(bpy.context.evaluated_depsgraph_get());hm=head_eval.to_mesh();hm.calc_loop_triangles()
samples=[];total=0
for tri in hm.loop_triangles:
    points=[hm.vertices[i].co.copy() for i in tri.vertices];mid=sum(points,Vector())/3
    z=mid.z;xx=abs(mid.x)
    if mid.y> .046 or z<eye_z-.125 or z>eye_z-.018:continue
    # Sideburns, jaw and short cheek beard; lips and centre above mouth excluded.
    density,mustache=beard_density(mid)
    if density<.02:continue
    area=(points[1]-points[0]).cross(points[2]-points[0]).length/2
    samples.append((points,tri.normal.copy(),area*density,mustache>.3));total+=area*density
head_eval.to_mesh_clear()
# Weighted face sampling preserves density without bias toward tiny topology.
import bisect
cumulative=[];acc=0
for points,n,area,m in samples:acc+=area;cumulative.append(acc)
groom=[]
for material in hairm[:3]:
    cu=bpy.data.curves.new('Barba_fibras','CURVE');cu.dimensions='3D';cu.bevel_depth=.00019;cu.bevel_resolution=1
    ob=bpy.data.objects.new('Barba_anatomica',cu);hero.objects.link(ob);cu.materials.append(material);groom.append(cu)
for k in range(12500):
    pts,n,area,mustache=samples[bisect.bisect_left(cumulative,random.random()*total)]
    u=random.random();w=random.random()
    if u+w>1:u=1-u;w=1-w
    p=pts[0]*(1-u-w)+pts[1]*u+pts[2]*w
    density,_=beard_density(p)
    if random.random()>density:continue
    if mustache:
        direction=Vector((.45 if p.x>0 else -.45,-.10,-.65)).normalized();length=random.uniform(.003,.006)
    else:
        direction=Vector((.15*sin(p.x*29),.10,-1)).normalized();length=random.uniform(.0028,.0065)
    # Most fibers hug the surface; thinner tips avoid a wire-brush silhouette.
    pp=[p+n*.0005,p+n*.0013+direction*length*.45,p+n*.0017+direction*length]
    cu=random.choice(groom);sp=cu.splines.new('POLY');sp.points.add(2)
    for point,co,radius in zip(sp.points,pp,(.85,1,.12)):point.co=(*co,1);point.radius=radius

# Brows lie on the real supraorbital ridge, individual curved tapered hairs.
for c in eye_centers:
    s=1 if c.x>0 else -1
    for k in range(200):
        t=random.random();x=s*(.013+.043*t);z=eye_z+.017+.007*sin(pi*t*.85)+random.uniform(-.0018,.0018)
        pos,no,idx,dist=skin_bvh.ray_cast(Vector((x,-.30,z)),Vector((0,1,0)),1)
        if pos is not None:
            p=pos+no*.0005
            line('Ceja_anatomica',[p,p+Vector((s*.0015,-.0004,.0012)),p+Vector((s*.003,-.0001,.0007))],.00023,hairm[0])

# Real draped mantle and shoulder yoke; front edges gather at a brooch.
cloth.node_tree.nodes.get('Principled BSDF').inputs['Sheen Weight'].default_value=.17
def mantle(a,t,extra=0):
    # Weighted long vertical folds that start at shoulders instead of a cone.
    topz=1.492-.018*cos(a)
    z=topz*(1-t)+(.29+.015*sin(3*a+.4)+.008*cos(7*a))*t
    spread=1-exp(-17*t)
    rx=.086+.225*spread+.022*t;ry=.067+.120*spread+.020*t
    wrinkles=(.002+.013*t)*sin(15*a+1.8*t)+.004*sin(23*a-2*t)*sin(pi*t)
    # Unequal shoulders and hanging folds break bilateral regularity.
    return Vector(((rx+wrinkles+extra)*sin(a)+.004*sin(a*3),.025-(ry+wrinkles+extra)*cos(a),z+.006*sin(a)*sin(pi*t)))
N=151;J=75;amin=.73;amax=2*pi-.73;v=[];f=[]
for j in range(J):
    for i in range(N):v.append(mantle(amin+(amax-amin)*i/(N-1),j/(J-1)))
for j in range(J-1):
    for i in range(N-1):q=j*N+i;f.append((q,q+1,q+N+1,q+N))
o=mesh('Capa_lana_pliegues_peso',v,[tuple(reversed(p)) for p in f],cloth,1);o.modifiers.new('Espesor_lana','SOLIDIFY').thickness=.003
for a in (amin,amax):
    for d in (0,.040):line('Ribete_capa',[mantle(a+(d if a==amin else -d),j/120,.003) for j in range(121)],.0016,trim)
    for j in range(60):
        t=.035+j*.0155
        line('Bordado_capa',[mantle(a+(.021 if a==amin else -.021)+.011*sin(pi*k/10),t+.012*k/10,.0032) for k in range(11)],.00065,trim)
for t in (.978,.995):line('Ribete_inferior',[mantle(amin+(amax-amin)*i/190,t,.003) for i in range(191)],.0019,trim)
# Front cloth follows elliptical shoulder/neck cross-sections and converges
# toward a single fastening. No wide floating rectangular scarf.
v=[];f=[];N=111;J=31
for j in range(J):
    t=j/(J-1)
    for i in range(N):
        u=i/(N-1);x=(-.225+.450*u)*(1-t)+(-.200+.275*u)*t
        top=1.454+.047*sin(pi*u)
        bottom=1.285+.148*u+.024*sin(pi*u)
        z=top*(1-t)+bottom*t
        y=-.055-.110*math.sqrt(t)*math.sqrt(max(0,1-(x/.27)**2))-.025*sin(pi*u)
        # Curved folds gather toward the right-hand brooch rather than evenly
        # spaced horizontal bands. Ends tuck into the shoulder cloak.
        wrinkle=.006*sin(t*7*pi+2.5*u)*sin(pi*t)*sin(pi*u)
        y+=wrinkle;z+=.004*sin(u*11+t*5)*sin(pi*t)
        if z<1.455:y=min(y,-.142*math.sqrt(max(0,1-(x/.215)**2))-.008)
        v.append((x,y,z))
for j in range(J-1):
    for i in range(N-1):q=j*N+i;f.append((q,q+1,q+N+1,q+N))
o=mesh('Capa_hombros_recogida',v,[tuple(reversed(p)) for p in f],cloth,0)
# Settle the shoulder cloth against simple costume colliders; apply the result
# to editable geometry, so the saved file has no live simulation dependency.
temporary=bpy.data.collections.new('SIM_TEMP');scene.collection.children.link(temporary)
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
for source_ob in list(hero.objects):
    if source_ob.name.startswith(('Cuero_torax_base','Lamina_cuero_torso','Bandolera_cuero','Manga_cota','Gorguera_cota')):
        me=bpy.data.meshes.new_from_object(source_ob.evaluated_get(deps))
        proxy=bpy.data.objects.new('SIM_COLLIDER',me);temporary.objects.link(proxy);proxy.matrix_world=source_ob.matrix_world.copy();proxy.hide_render=True
        proxy.modifiers.new('Colision','COLLISION');proxy.collision.thickness_outer=.003;proxy.collision.thickness_inner=.001
pin=o.vertex_groups.new(name='Sujecion_hombros_broche')
pin.add(list(range(N)),1,'REPLACE')
pin.add([j*N+N-1 for j in range(J)],1,'REPLACE')
sim=o.modifiers.new('Asentar_lana','CLOTH');s=sim.settings
s.quality=7;s.mass=.30;s.tension_stiffness=32;s.compression_stiffness=32;s.shear_stiffness=12;s.bending_stiffness=.65
s.vertex_group_mass=pin.name;s.pin_stiffness=1
sim.collision_settings.collision_quality=3;sim.collision_settings.distance_min=.003
sim.point_cache.frame_start=1;sim.point_cache.frame_end=38
for frame in range(1,39):
    scene.frame_set(frame);deps=bpy.context.evaluated_depsgraph_get();o.evaluated_get(deps)
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
bpy.ops.object.modifier_apply(modifier=sim.name)
movement=max((p.co-Vector(original)).length for p,original in zip(o.data.vertices,v))
assert movement<.35,('Cloth simulation displacement too large',movement)
v=[p.co.copy() for p in o.data.vertices]
for proxy in list(temporary.objects):bpy.data.objects.remove(proxy,do_unlink=True)
bpy.data.collections.remove(temporary)
scene.frame_set(1)
sub=o.modifiers.new('Suavizar_lana','SUBSURF');sub.levels=1;sub.render_levels=1
o.modifiers.new('Espesor_lana','SOLIDIFY').thickness=.003
o['cloth_settled_frames']=38;o['max_settle_displacement_m']=movement
print('CLOTH_SETTLED',movement,flush=True)
line('Ribete_hombro',[v[(J-1)*N+i] for i in range(N)],.002,trim)
brooch=Vector((.074,-.163,1.426))
ell('Broche_laton',brooch,(.024,.005,.024),bronze)
for r in (.018,.0215):line('Broche_grabado',[(brooch.x+r*cos(a),brooch.y-.006,brooch.z+r*sin(a)) for a in [2*pi*i/64 for i in range(65)]],.00085,edge)
for k in range(8):
    a=2*pi*k/8
    line('Broche_flor',[(brooch.x+.004*cos(a),brooch.y-.0065,brooch.z+.004*sin(a)),(brooch.x+.015*cos(a+.2),brooch.y-.0065,brooch.z+.015*sin(a+.2)),(brooch.x+.007*cos(a+.4),brooch.y-.0065,brooch.z+.007*sin(a+.4))],.0008,bronze)

# Mild asymmetry in the stance, with feet grounded; head stays turned forward.
# Lower-body meshes inherit the existing provisional design, not historical canon.
stage_code=old[old.index('# Stage and cameras;'):]
stage_code=stage_code.replace('Rodrigo_escultura_v01.blend','Rodrigo_escultura_v03.blend').replace("name+'_v01.png'","name+'_v03.png'")
stage_code=stage_code.replace("scene.cycles.samples=40","scene.cycles.samples=48")
stage_code=stage_code.replace("'evaluated_triangles':count","'evaluated_triangles':count")
stage_code=stage_code.replace("scene['status']='Editable procedural geometry study; portrait likeness NOT approved; no rig/UV/LOD/game export.'","scene['status']='Anatomical revision using MakeHuman CC0 core. Likeness pending; costume remains study; no rig/LOD/game export.'")
stage_code=stage_code.replace('RODRIGO_GEOMETRY_OK','RODRIGO_V03_OK')
stage_code=stage_code.replace('geometry_study_not_game_ready','anatomical_revision_not_game_ready')
stage_code=stage_code.replace('No UV/rig/LOD/bake/export','Original head UV retained; costume atlas, rig, LOD, bake and export pending')
stage_code=stage_code.replace('Modelo geometrico editable de Rodrigo.','V03: mirada, proporciones, iris, pelo ondulado por grupos, barba y piel UV revisados; fuentes CC0 registradas.')
stage_code=stage_code.replace('Referencias actuales empaquetadas SOLO en datablocks, ninguna imagen se usa como personaje.','Retratos empaquetados de consulta; piel UV CC0 sobre geometria anatomica real.')
scene['anatomy_source']='MakeHuman CC0 core + project modifications'
scene['upstream_commit']=(UPSTREAM/'commit.txt').read_text().strip()
exec(compile(stage_code,str(SOURCE/'stage_from_v01.py'),'exec'),globals())
manifest['anatomy']={'upstream_commit':scene['upstream_commit'],'asset_license':'CC0-1.0','target_weights':weights,'original_head_uv_retained':True}
manifest['revision']='V03: alert gaze, curved iris, grouped wavy hair, shortened feathered beard'
manifest['skin']={'albedo':'CC0 MargaretToigo Bronze','normal_specular':'CC0 Mindfront Aksel','texture_sources':'ArtSource/ThirdParty/*/provenance.json','original_portrait_as_texture':False}
(SOURCE/'escultura_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
