"""Extend only the accepted V04 beard below the jaw onto the upper neck.

Preserve all V04 objects, UVs, upper-face treatment and existing beard fibers.
Source: own V04 blend; external asset provenance remains in ThirdParty.
"""
from pathlib import Path
import bpy,math,random,json,hashlib,struct,bisect
from mathutils import Vector
from mathutils.bvhtree import BVHTree

SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[3]
BASE=SOURCE.parent/'CastillaRodrigoV04/Rodrigo_escultura_v04.blend'
EXPECTED_BASE='179e7e1aa4b1ae1bc39e9f7a6248cf154395e9298b26faa708d8a9ae0ebafa72'
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV05';OUT.mkdir(parents=True,exist_ok=True)
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
assert sha(BASE)==EXPECTED_BASE,'V04 source changed; review before rebuilding the extension'
bpy.ops.wm.open_mainfile(filepath=str(BASE))
scene=bpy.context.scene;scene.name='Rodrigo_Barba_Hasta_Cuello_V05'
hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL'];head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']

def object_signature(ob):
    h=hashlib.sha256()
    def floats(values):
        values=list(values);h.update(struct.pack('<%df'%len(values),*(round(float(q),6) for q in values)))
    floats(x for row in ob.matrix_world for x in row)
    h.update(ob.type.encode())
    if ob.type=='MESH':
        for v in ob.data.vertices:floats(v.co)
        for f in ob.data.polygons:h.update(struct.pack('<%dI'%len(f.vertices),*f.vertices))
        for uv in ob.data.uv_layers:
            for p in uv.data:floats(p.uv)
    elif ob.type=='CURVE':
        floats([ob.data.bevel_depth,ob.data.bevel_resolution])
        for sp in ob.data.splines:
            for p in sp.points:floats([*p.co,p.radius])
    elif ob.type=='CAMERA':floats([ob.data.lens,ob.data.ortho_scale])
    elif ob.type=='LIGHT':floats([ob.data.energy,*ob.data.color,ob.data.size])
    if ob.type in {'MESH','CURVE'}:h.update('|'.join(m.name for m in ob.data.materials).encode())
    return h.hexdigest()

bpy.context.view_layer.update()
protected={ob.name:object_signature(ob) for ob in bpy.data.objects}
treatment=head.data.color_attributes['SkinTreatment']
UPPER_LIMIT=1.585
def upper_skin_hash():
    h=hashlib.sha256()
    for loop in head.data.loops:
        p=head.matrix_world@head.data.vertices[loop.vertex_index].co
        if p.z>=UPPER_LIMIT:h.update(struct.pack('<4f',*treatment.data[loop.index].color))
    return h.hexdigest()
upper_before=upper_skin_hash()
eyes=[o for o in hero.objects if o.name.startswith('Globo_ocular_anatomico')]
eye_z=sum(o.matrix_world.translation.z for o in eyes)/len(eyes)
def smooth(q):
    q=max(0,min(1,q));return q*q*(3-2*q)
def target_neck_density(p):
    if p.z>=UPPER_LIMIT:return 0
    xx=abs(p.x)
    old_low=eye_z-.121+.024*min(1,xx/.070)**.8+.0013*math.sin(p.x*281+.4)+.0006*math.sin(p.x*671)
    top=(1-smooth((p.z-(old_low+.012))/.010))*(1-smooth((p.z-(UPPER_LIMIT-.006))/.006))
    low=1.526+.003*(min(1,xx/.060)**1.3)+.018*smooth((p.y+.025)/.040)+.0008*math.sin(p.x*221+.6)
    bottom=smooth((p.z-low)/.009)
    front=1-smooth((p.y-.005)/.030)
    sides=1-smooth((xx-.053)/.014)
    return top*bottom*front*sides

# Read the original mask before changing any corner colors. Shared vertex
# averages give a continuous overlap with the preserved facial beard.
old_density=[0.0]*len(head.data.vertices);counts=[0]*len(old_density)
for loop in head.data.loops:
    d=max(0,min(1,(1-treatment.data[loop.index].color[0]/.96)/.50))
    old_density[loop.vertex_index]+=d;counts[loop.vertex_index]+=1
old_density=[d/max(1,n) for d,n in zip(old_density,counts)]
world=[head.matrix_world@v.co for v in head.data.vertices]
changed=0;changed_z=[]
for loop in head.data.loops:
    i=loop.vertex_index;p=world[i];old=old_density[i];new=max(old,target_neck_density(p))
    if new>old+.0001:
        q=1-.50*new;treatment.data[loop.index].color=(.96*q,.93*q,.88*q,1)
        changed+=1;changed_z.append(p.z)
assert changed and max(changed_z)<UPPER_LIMIT
assert upper_skin_hash()==upper_before

head.data.calc_loop_triangles()
bvh=BVHTree.FromPolygons(world,[tuple(f.vertices) for f in head.data.polygons])
samples=[];cumulative=[];acc=0;old_area=0
for tri in head.data.loop_triangles:
    ids=list(tri.vertices);points=[world[i] for i in ids];mid=sum(points,Vector())/3
    area=(points[1]-points[0]).cross(points[2]-points[0]).length/2
    average_old=sum(old_density[i] for i in ids)/3
    old_area+=area*average_old*average_old
    if not (1.525<mid.z<UPPER_LIMIT):continue
    normal=(points[1]-points[0]).cross(points[2]-points[0]).normalized()
    radial=Vector((mid.x,mid.y-.008,min(0,mid.z-1.590)))
    if normal.dot(radial)<.002:continue
    delta=max(0,target_neck_density(mid)**2-average_old**2)
    if delta<.015:continue
    acc+=area*delta;samples.append((ids,points,normal));cumulative.append(acc)
assert acc>0,'No neck extension area found'
materials=[o.data.materials[0] for o in hero.objects if o.name.startswith('Barba_anatomica')]
assert len(materials)==3
groom=[]
for material in materials:
    cu=bpy.data.curves.new('Barba_cuello_V05','CURVE');cu.dimensions='3D';cu.bevel_depth=.00012;cu.bevel_resolution=1
    ob=bpy.data.objects.new('Barba_cuello_V05',cu);hero.objects.link(ob);cu.materials.append(material);groom.append(ob)
random.seed(5507);roots=[]
old_strand_count=sum(len(o.data.splines) for o in hero.objects if o.name.startswith('Barba_anatomica'))
root_density=old_strand_count/old_area
attempts=max(1000,round(root_density*acc))
print('ROOT_DENSITY_MATCHED',old_strand_count,old_area,acc,attempts,flush=True)
for k in range(attempts):
    ids,pts,n=samples[bisect.bisect_left(cumulative,random.random()*acc)]
    u=random.random();v=random.random()
    if u+v>1:u=1-u;v=1-v
    weights=(1-u-v,u,v);p=sum((point*w for point,w in zip(pts,weights)),Vector())
    old=sum(old_density[i]*w for i,w in zip(ids,weights))
    delta=max(0,target_neck_density(p)**2-old**2)
    if delta<.005:continue
    direction=Vector((-.10*math.copysign(1,p.x),.10,-1))
    direction=(direction-n*direction.dot(n)).normalized();side=n.cross(direction).normalized()
    length=random.uniform(.0024,.0050);phase=random.uniform(0,2*math.pi)
    obj=random.choice(groom);sp=obj.data.splines.new('POLY');sp.points.add(4)
    for j in range(5):
        t=j/4;candidate=p+direction*(length*t)+side*(.00022*math.sin(phase+t*math.pi)*math.sin(math.pi*t))
        surface,normal,_,_=bvh.find_nearest(candidate,.020)
        assert surface is not None
        position=surface+normal*(.00025+.00055*math.sin(math.pi*t)+.00025*t)
        sp.points[j].co=(*position,1);sp.points[j].radius=(.8,1,.85,.5,.04)[j]*random.uniform(.8,1.15)
    roots.append(list(p))
assert len(roots)>500
for name,sig in protected.items():assert object_signature(bpy.data.objects[name])==sig,name
assert upper_skin_hash()==upper_before
head['neck_extension_v05']='Only lower beard extended under jaw onto upper neck; accepted V04 upper part preserved'
scene['revision']='V05 upper-neck beard extension only'

manifest={'revision':'V05 beard continuation beneath jaw onto upper neck','base_v04_sha256':EXPECTED_BASE,
          'protected_objects':protected,'upper_skin_hash':upper_before,'unchanged_above_world_z':UPPER_LIMIT,
          'modified_color_corners':changed,'changed_color_world_z_min':min(changed_z),'changed_color_world_z_max':max(changed_z),
          'added_strands':len(roots),'added_root_z_min':min(p[2] for p in roots),'added_root_z_max':max(p[2] for p in roots),
          'lower_feather_start_m':1.526,'root_positions_world':roots,'game_ready':False}
manifest['density_match']={'existing_strands':old_strand_count,'existing_weighted_area_m2':old_area,
                           'new_weighted_area_m2':acc,'target_strands_per_m2':root_density,'new_attempts':attempts}
(SOURCE/'extension_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
text=bpy.data.texts.get('LEEME_EXTENSION_V05') or bpy.data.texts.new('LEEME_EXTENSION_V05')
text.write('V05: solo se amplia la barba por debajo de la mandibula hasta la parte alta del cuello.\nObjetos, UV, posicion de cabeza, pelo, mirada y barba superior de V04 se conservan.\nFuente V04 fijada por SHA-256; modificacion local de SkinTreatment y tres curvas nuevas.\nSigue siendo estudio de alta densidad sin aprobacion artistica ni exportacion Unity.\n')
blend=SOURCE/'Rodrigo_escultura_v05.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
cameras={'Frente_tres_cuartos':'CAM_3Q','Espalda':'CAM_Espalda','Rostro':'CAM_Rostro','Perfil_cuello':'CAM_Perfil_Cuello'}
for label,name in cameras.items():
    scene.camera=bpy.data.objects[name];scene.render.filepath=str(OUT/(label+'_v05.png'));bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['CAM_3Q'];bpy.ops.wm.save_as_mainfile(filepath=str(blend))
assert sha(BASE)==EXPECTED_BASE
print('RODRIGO_V05_EXTENSION_OK',len(roots),min(changed_z),max(changed_z),flush=True)
