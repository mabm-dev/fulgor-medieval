"""Reproducible detail pass over every current art model. No downloaded assets.

Run with -- --family Montana/Biomas/Rural/Agua/Puente/Molino.
Keeps the original sources and exports a new family with portable baked maps.
"""
from pathlib import Path
import argparse
import json
import math
import random
import sys

import bpy
import bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

SOURCE = Path(__file__).resolve().parent
TERRAIN = SOURCE.parent/'Terrain'
ROOT = SOURCE.parents[2]
sys.path.insert(0,str(TERRAIN))
sys.path.insert(0,str(SOURCE))
from fulgor_detail_mesh import separate_duplicate_faces
from fulgor_terrain_tools import source_material,bake_export,stage,caption,load_objects,join

FAMILIES = {
 'Montana':('Montana_natural_v02.blend',['SM_MontanaNatural_A_LOD0']),
 'Biomas':('Bosque_colina_natural_v02.blend',['SM_BosqueNatural_A_LOD0','SM_ColinaNatural_A_LOD0','SM_LlanuraNatural_A_LOD0']),
 'Rural':('Agua_cereal_caminos_natural_v02.blend',
          ['SM_'+k+'Natural_A_LOD0' for k in ('Mar','Costa','Lago','Rio')]+
          ['SM_CaminoNatural_'+k+'_LOD0' for k in ('Recto','Curva','Cruce')]+['SM_CerealNatural_A_LOD0']),
 'Agua':('Conexiones_agua_v03.blend',['SM_Agua_'+k+'_LOD0' for k in ('Mar','RioRecto','RioCurva','Desembocadura','CostaRecta','Cabo','Bahia')]),
 'Puente':('Puente_piedra_v01.blend',['SM_PuentePiedra_A_LOD0','SM_CaminoPuente_Recto_LOD0']),
 'Molino':('Molino_agua_v01.blend',['SM_MolinoAgua_Edificio_LOD0','SM_MolinoAgua_Rueda_LOD0']),
}
parser = argparse.ArgumentParser()
parser.add_argument('--family',choices=FAMILIES,required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
FAMILY = args.family
EXPORT = ROOT/'Assets/Fulgor/Art/DetalleV04'/FAMILY
PREVIEW = ROOT/'Builds/ArtPreview/DetalleV04'
PREVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = FAMILY+'_Detalle'
source_file,names = FAMILIES[FAMILY]
originals = load_objects(TERRAIN/source_file,names)


def smooth(t):
    t = max(0,min(1,t))
    return t*t*(3-2*t)


def edge_fade(x,y,radius):
    gap = min(radius*math.sqrt(3)/2-abs(x),
              (math.sqrt(3)*radius-abs(x)-math.sqrt(3)*abs(y))/2)
    return smooth(gap/.12)


def material_detail(mat,kind):
    mat = mat.copy()
    mat.name += '_DetalleFuente'
    nodes,links = mat.node_tree.nodes,mat.node_tree.links
    shader = nodes.get('Principled BSDF')
    # All source textures keep the old UV while the new atlas is packed independently.
    uv = nodes.new('ShaderNodeUVMap')
    uv.uv_map = 'UV_Source'
    for n in list(nodes):
        if n.type == 'TEX_IMAGE' and not n.inputs['Vector'].is_linked:
            links.new(uv.outputs['UV'],n.inputs['Vector'])
        if n.type == 'NORMAL_MAP': n.uv_map = 'UV_Source'
    color = shader.inputs['Base Color'].links[0].from_socket
    geometry = nodes.new('ShaderNodeNewGeometry')
    position = geometry.outputs['Position']
    def mathnode(op,a,b):
        n=nodes.new('ShaderNodeMath'); n.operation=op
        for socket,value in zip(n.inputs,(a,b)):
            if isinstance(value,(int,float)): socket.default_value=value
            else: links.new(value,socket)
        return n.outputs[0]
    split=nodes.new('ShaderNodeSeparateColor'); links.new(color,split.inputs[0])
    red,green,blue = [split.outputs[i] for i in range(3)]
    ratio=mathnode('DIVIDE',green,mathnode('MAXIMUM',red,.001))
    wood=mathnode('MULTIPLY',mathnode('GREATER_THAN',ratio,.31),mathnode('LESS_THAN',ratio,.63))
    wood=mathnode('MULTIPLY',wood,mathnode('LESS_THAN',red,.40)) if kind in ('building','wheel','bridge') else 0
    water=mathnode('MULTIPLY',mathnode('LESS_THAN',red,mathnode('MULTIPLY',blue,.40)),mathnode('GREATER_THAN',blue,mathnode('MULTIPLY',green,.9)))
    noise=nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value=95
    noise.inputs['Detail'].default_value=5
    noise.inputs['Roughness'].default_value=.72
    links.new(position,noise.inputs['Vector'])
    wave=nodes.new('ShaderNodeTexWave'); wave.wave_type='BANDS'; wave.bands_direction='X'
    wave.inputs['Scale'].default_value=125
    wave.inputs['Distortion'].default_value=6
    wave.inputs['Detail'].default_value=4
    links.new(position,wave.inputs['Vector'])
    grain=nodes.new('ShaderNodeMixRGB')
    if isinstance(wood,int): grain.inputs[0].default_value=wood
    else: links.new(wood,grain.inputs[0])
    links.new(noise.outputs['Fac'],grain.inputs[1]); links.new(wave.outputs['Fac'],grain.inputs[2])
    ramp=nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.20
    ramp.color_ramp.elements[0].color=(.38,.32,.24,1)
    ramp.color_ramp.elements[1].position=.78
    ramp.color_ramp.elements[1].color=(1.13,1.09,.98,1)
    links.new(grain.outputs[0],ramp.inputs[0])
    mix=nodes.new('ShaderNodeMixRGB'); mix.blend_type='MULTIPLY'
    links.new(mathnode('SUBTRACT',.42,mathnode('MULTIPLY',water,.38)),mix.inputs[0])
    links.new(color,mix.inputs[1]); links.new(ramp.outputs[0],mix.inputs[2])
    links.new(mix.outputs[0],shader.inputs['Base Color'])
    bump=nodes.new('ShaderNodeBump'); bump.inputs['Distance'].default_value=.0035 if kind=='mountain' else .0018
    links.new(mathnode('SUBTRACT',.40,mathnode('MULTIPLY',water,.24)),bump.inputs['Strength'])
    links.new(grain.outputs[0],bump.inputs['Height'])
    if shader.inputs['Normal'].is_linked: links.new(shader.inputs['Normal'].links[0].from_socket,bump.inputs['Normal'])
    links.new(bump.outputs[0],shader.inputs['Normal'])
    return mat


class Details:
    def __init__(self): self.v=[]; self.f=[]; self.c=[]
    def add(self,vertices,faces,color):
        n=len(self.v); self.v.extend(Vector(p) for p in vertices)
        self.f.extend(tuple(n+i for i in face) for face in faces)
        self.c.extend([color]*len(faces))
    def leaf(self,base,direction,width,color):
        base,direction=Vector(base),Vector(direction)
        u=Vector((-direction.y,direction.x,0))
        if u.length<.001: u=Vector((1,0,0))
        u.normalize(); u*=width
        center=base+direction*.5
        self.add([base,center+u,base+direction,center-u,center+Vector((0,0,.0013)),center-Vector((0,0,.0005))],
                 [(0,1,4),(1,2,4),(2,3,4),(3,0,4),(1,0,5),(2,1,5),(3,2,5),(0,3,5)],color)
    def stone(self,p,size,rng,color=(.43,.34,.20)):
        p=Vector(p)
        verts=[Vector((0,0,1)),Vector((0,0,-.55))]
        for i in range(6):
            a=math.tau*i/6; verts.append(Vector((math.cos(a),math.sin(a),rng.uniform(-.15,.15))))
        self.add([p+Vector((v.x*size*1.4,v.y*size,v.z*size*.55)) for v in verts],
                 [(0,2+i,2+(i+1)%6) for i in range(6)]+[(1,2+(i+1)%6,2+i) for i in range(6)],color)
    def rod(self,a,b,r,color,segments=5):
        a,b=Vector(a),Vector(b); axis=(b-a).normalized()
        u=axis.cross(Vector((0,0,1)))
        if u.length<.001:u=Vector((1,0,0))
        u.normalize(); v=axis.cross(u)
        vertices=[p+r*(u*math.cos(math.tau*i/segments)+v*math.sin(math.tau*i/segments)) for p in (a,b) for i in range(segments)]
        faces=[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
        faces += [tuple(range(segments-1,-1,-1)),tuple(range(segments,segments*2))]
        self.add(vertices,faces,color)
    def object(self):
        if not self.f:return None
        mesh=bpy.data.meshes.new('Detalles_Geometria'); mesh.from_pydata(self.v,[],self.f);mesh.update()
        attr=mesh.color_attributes.new(name='ArtTint',type='FLOAT_COLOR',domain='CORNER')
        for face,color in zip(mesh.polygons,self.c):
            for i in face.loop_indices:attr.data[i].color=(*color,1)
        bm=bmesh.new();bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(mesh);bm.free();mesh.update()
        obj=bpy.data.objects.new('Detalles_Geometria',mesh);scene.collection.objects.link(obj)
        mesh.materials.append(source_material('M_Detalles_Fuente',120,.001,.23))
        return obj


class Surface:
    def __init__(self,obj):
        self.mesh=obj.data; self.mesh.calc_loop_triangles()
        self.tris=list(self.mesh.loop_triangles)
        self.tree=BVHTree.FromPolygons([v.co for v in self.mesh.vertices],[t.vertices[:] for t in self.tris],all_triangles=True)
        self.uv=self.mesh.uv_layers['UV_Source'].data
        image=next(n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'BaseColor' in n.image.name)
        self.width,self.height=image.size;self.pixels=image.pixels[:]
    def hit(self,x,y,ceiling=3):
        p,n,index,_=self.tree.ray_cast(Vector((x,y,ceiling)),Vector((0,0,-1)))
        if p is None:return None
        tri=self.tris[index]
        uv=barycentric_transform(p,*[self.mesh.vertices[i].co for i in tri.vertices],
                                *[Vector((*self.uv[i].uv,0)) for i in tri.loops])
        ix=max(0,min(self.width-1,int(uv.x*self.width)));iy=max(0,min(self.height-1,int(uv.y*self.height)))
        start=(iy*self.width+ix)*4
        return p,n,self.pixels[start:start+3]


def enrich_ground(obj,kind,radius,rng):
    surface=Surface(obj);details=Details();counts={'plants':0,'stones':0,'flowers':0}
    ceiling=.36 if kind=='forest' else 3
    samples=1100 if kind in ('plain','hill','forest') else 700
    for i in range(samples):
        x,y=rng.uniform(-.72,.72),rng.uniform(-.76,.76)
        if edge_fade(x,y,radius)<.99:continue
        hit=surface.hit(x,y,ceiling)
        if not hit:continue
        p,n,c=hit
        if n.z<.55:continue
        green=c[1]>c[0]*1.18 and c[1]>c[2]*1.6
        sand=c[0]>c[1]*1.08 and c[2]<c[0]*.8
        if kind=='mountain' and p.z>.47 and not green:
            if rng.random()<.12:
                details.stone(p+Vector((0,0,.006)),rng.uniform(.008,.026),rng);counts['stones']+=1
            continue
        if green and counts['plants']<(45 if kind=='forest' else 135 if kind in ('plain','hill') else 70):
            if kind=='forest':
                # Low fern fronds form a separate understory around the tree roots.
                for stem in range(3):
                    a=rng.random()*math.tau
                    for j in range(3):
                        base=p+Vector((math.cos(a),math.sin(a),.8))*(.008+j*.009)
                        for side in (-1,1):
                            direction=Vector((math.cos(a+side*.9),math.sin(a+side*.9),.4))*(.035-j*.006)
                            details.leaf(base,direction,.0065,(.16,.34,.035))
            else:
                for blade in range(rng.randint(4,7)):
                    a=rng.random()*math.tau; h=rng.uniform(.032,.075)
                    details.leaf(p,Vector((math.cos(a)*h*.45,math.sin(a)*h*.45,h)),rng.uniform(.002,.0045),
                                 (.15+rng.random()*.10,.33+rng.random()*.09,.025))
            counts['plants']+=1
            if rng.random()<.18 and kind not in ('water','forest'):
                top=p+Vector((.003,0,rng.uniform(.045,.085)))
                details.rod(p,top,.0012,(.15,.26,.022),3)
                petal=(.62,.065,.025) if kind=='cereal' or rng.random()<.35 else (.78,.68,.36)
                for j in range(5):
                    a=math.tau*j/5
                    details.leaf(top,Vector((.012*math.cos(a),.012*math.sin(a),.004)),.006,petal)
                counts['flowers']+=1
        elif sand and p.z<.18 and counts['stones']<50 and rng.random()<.30:
            details.stone(p+Vector((0,0,.003)),rng.uniform(.005,.015),rng);counts['stones']+=1
            if kind=='water' and .065<p.z<.135 and rng.random()<.35:
                for j in range(5):
                    a=rng.uniform(0,math.tau);h=rng.uniform(.06,.13)
                    details.leaf(p,Vector((.025*math.cos(a),.025*math.sin(a),h)),.003,(.23,.32,.045))
                counts['plants']+=1
    if kind=='cereal':
        for i in range(200):
            hit=surface.hit(rng.uniform(-.55,.55),rng.uniform(-.49,.49))
            if hit and hit[0].z>.22:
                p=hit[0]
                for j in range(4):
                    a=math.tau*j/4
                    details.rod(p,p+Vector((math.cos(a)*.012,math.sin(a)*.012,.028)),.0007,(.74,.49,.11),3)
    extra=details.object()
    return join([obj,extra],obj.name) if extra else obj,counts


def architecture_details(obj,kind,rng):
    details=Details()
    if kind=='wheel':
        for y in (-.069,.069):
            for i in range(16):
                a=math.tau*i/16;p=Vector((.211*math.cos(a),y,.211*math.sin(a)))
                details.rod(p,p+Vector((0,math.copysign(.006,y),0)),.0055,(.08,.065,.043),6)
            for i in range(8):
                a=math.tau*i/8;p=Vector((.065*math.cos(a),y,.065*math.sin(a)))
                details.rod(p,p+Vector((0,math.copysign(.006,y),0)),.004,(.09,.07,.042),6)
    if kind=='building':
        for side in (-1,1):
            for i in range(8):
                y=-.22+i*.063
                details.rod((side*.345,y,.422),(side*.414,y,.392),.008,(.24,.105,.031),4)
        for z in (.052,.176):
            for x in (-.235,-.135):details.rod((x,-.230,z),(x,-.234,z),.0038,(.075,.065,.04),6)
        # Small vine on the right corner, with stems and individual folded leaves.
        start=Vector((.326,-.219,.045))
        for j in range(14):
            p=start+Vector((.007*math.sin(j),0,j*.021))
            q=start+Vector((.007*math.sin(j+1),0,(j+1)*.021))
            details.rod(p,q,.0017,(.15,.11,.03),4)
            side=-1 if j%2 else 1
            details.leaf(p,Vector((side*.027,-.009,.015)),.009,(.14,.28,.023))
        # Pegs along exposed wooden framing and a few straw stalks near the sacks.
        for x in (-.31,.31):
            for z in (.25,.38):details.rod((x,-.214,z),(x,-.222,z),.0035,(.11,.06,.02),6)
        for i in range(22):
            x,y=rng.uniform(-.52,-.37),rng.uniform(-.16,.10)
            details.rod((x,y,.005),(x+.03,y+.01,.008),.0008,(.59,.40,.10),3)
    if kind=='bridge':
        # Fine plant growth at the abutments, leaving the road surface unobstructed.
        for side in (-1,1):
            for i in range(16):
                x=side*rng.uniform(.39,.56);y=rng.choice((-.176,.176));z=rng.uniform(.12,.15)
                for j in range(3):details.leaf((x,y,z),Vector((rng.uniform(-.015,.015),math.copysign(.008,y),rng.uniform(.015,.033))),.0035,(.19,.27,.035))
    extra=details.object()
    return join([obj,extra],obj.name) if extra else obj


def classify(name):
    for token,kind in [('Montana','mountain'),('Bosque','forest'),('Colina','hill'),('Llanura','plain'),
                       ('Cereal','cereal'),('Edificio','building'),('Rueda','wheel'),('PuentePiedra','bridge'),('Camino','road')]:
        if token in name:return kind
    return 'water'


models,reports=[],[]
for original in originals:
    original.location=(0,0,0);original.rotation_euler=(0,0,0)
    original.use_fake_user=True
    obj=original.copy();obj.data=original.data.copy();scene.collection.objects.link(obj)
    obj.name=original.name.removesuffix('_LOD0')+'_Detalle_LOD0'
    kind=classify(original.name)
    radius=1 if FAMILY in ('Agua','Puente') else .97
    rng=random.Random(42000+sum(map(ord,original.name)))
    obj.data.uv_layers.active.name='UV_Source'
    obj.data.uv_layers.new(name='UV_AtlasDetalle')
    obj.data.uv_layers.active_index=1
    for i,mat in enumerate(obj.data.materials):obj.data.materials[i]=material_detail(mat,kind)
    if kind in ('mountain','hill'):
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        mod=obj.modifiers.new('Relieve fino','SUBSURF');mod.subdivision_type='SIMPLE';mod.levels=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        for v in obj.data.vertices:
            if v.co.z>(.423 if kind=='mountain' else .285):
                x,y,z=v.co
                amount=.009 if kind=='mountain' else .003
                v.co.z+=amount*math.sin(47*x+19*y)*math.sin(31*y+11*z)*edge_fade(x,y,radius)
    elif kind=='water':
        for v in obj.data.vertices:
            if .036<v.co.z<.045:
                x,y,z=v.co;v.co.z+=.00065*math.sin(73*x+31*y)*math.sin(41*y-17*x)*edge_fade(x,y,radius)
    if kind=='bridge':
        bpy.context.view_layer.objects.active=obj
        mod=obj.modifiers.new('Cantos de piedra usados','BEVEL');mod.width=.0015;mod.segments=2;mod.limit_method='ANGLE'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    additions={}
    if kind in ('mountain','hill','forest','plain','cereal','road','water'):
        obj,additions=enrich_ground(obj,kind,radius,rng)
    else:obj=architecture_details(obj,kind,rng)
    # Join retains the source object's origin and atlas. New props use vertex colors.
    obj.data.uv_layers.active_index=obj.data.uv_layers.find('UV_AtlasDetalle')
    resolution=2048 if kind not in ('wheel',) else 1024
    separated_backfaces=separate_duplicate_faces(obj.data)
    report=bake_export(obj,EXPORT,resolution,bake_roughness=True,radius=radius,
                       min_height=-.25 if kind=='wheel' else 0,source_uv_to_remove='UV_Source')
    report.update(source_model=original.name,source_blend=source_file,kind=kind,additions=additions,
                  source_uv_removed=True,uv_channels=len(obj.data.uv_layers),
                  separated_backface_vertices=separated_backfaces)
    obj.use_fake_user=True
    models.append(obj);reports.append(report)
    for collection in list(obj.users_collection):collection.objects.unlink(obj)
    print('DETAIL_MODEL_READY',obj.name,report['triangles'],flush=True)


GOLD=(.66,.41,.16)
def place(obj,x,y,z=0):
    copy=obj.copy();copy.location=(x,y,z);copy.rotation_euler=(0,0,0);scene.collection.objects.link(copy)
    return copy


if FAMILY in ('Molino','Puente'):
    river,=load_objects(TERRAIN/'Conexiones_agua_v03.blend',['SM_Agua_RioRecto_LOD0'])
    for x,set_models in ((-1.35,originals),(1.35,models)):
        water=place(river,x,0)
        if FAMILY=='Molino':
            place(set_models[0],x,.49,.12);place(set_models[1],x+.15,.12,.255)
        else:
            water.rotation_euler.z=math.radians(60)
            place(set_models[0],x,0)
    camera=stage(scene,(0,.05,.22),5.9,PREVIEW/(FAMILY+'_comparacion_v04.png'))
    scene.render.resolution_y=1250
    caption(camera,'FULGOR MEDIEVAL / '+FAMILY.upper(),-2.62,1.99,.10,GOLD)
    caption(camera,'MATERIALES CON MAS VIDA',-2.62,1.65,.21)
    caption(camera,'ANTES',-1.35,-1.40,.12,align='CENTER')
    caption(camera,'DETALLE V04',1.35,-1.40,.12,align='CENTER')
    caption(camera,'Geometria y mapas propios / Rueda y agua estaticas / Pendiente de Unity',-2.62,-2.05,.07,GOLD)
else:
    columns=min(4,len(models));rows=math.ceil(len(models)/columns)
    for i,obj in enumerate(models):
        x=(i%columns-(columns-1)/2)*2.25;y=((rows-1)/2-i//columns)*2.75
        place(obj,x,y)
    ortho=max(5.4,columns*2.45)
    camera=stage(scene,(0,0,.25 if FAMILY=='Montana' else .12),ortho,PREVIEW/(FAMILY+'_galeria_v04.png'))
    scene.render.resolution_y=1250 if rows>1 else 1050
    top=ortho*scene.render.resolution_y/1600/2
    caption(camera,'FULGOR MEDIEVAL / '+FAMILY.upper(),-ortho*.445,top-.35,.15,GOLD)
    caption(camera,'REVISION DE DETALLE V04',-ortho*.445,top-.77,.28)
    caption(camera,'Relieve, vegetacion y superficies / paleta calida y agua azul',-ortho*.445,top-1.08,.12,GOLD)
    caption(camera,'ESTUDIO VISUAL / LOD, medicion e integracion en Unity pendientes',-ortho*.445,-top+.25,.10,GOLD)
scene.cycles.samples=64
bpy.ops.render.render(write_still=True)

if FAMILY in ('Molino','Puente'):
    scene=bpy.data.scenes.new(FAMILY+'_Vista_Cercana')
    bpy.context.window.scene=scene
    water=place(river,0,0)
    if FAMILY=='Molino':
        place(models[0],0,.49,.12);place(models[1],.15,.12,.255)
    else:
        water.rotation_euler.z=math.radians(60);place(models[0],0,0)
    camera=stage(scene,(0,.14,.30),2.85,PREVIEW/(FAMILY+'_detalle_v04.png'))
    scene.render.resolution_y=1250
    caption(camera,'FULGOR MEDIEVAL / '+FAMILY.upper(),-1.27,.98,.052,GOLD)
    caption(camera,'DETALLE V04',-1.27,.81,.10)
    caption(camera,'Geometria y texturas propias / Estudio sin integrar en Unity',-1.27,-1.00,.038,GOLD)
    bpy.ops.render.render(write_still=True)

# A uniform before/after close-up for every terrain family.
if FAMILY not in ('Molino','Puente'):
    scene=bpy.data.scenes.new(FAMILY+'_Comparacion')
    bpy.context.window.scene=scene
    index=7 if FAMILY=='Rural' else 3 if FAMILY=='Agua' else 0
    for x,obj in ((-1.30,originals[index]),(1.30,models[index])):place(obj,x,0)
    camera=stage(scene,(0,0,.25 if FAMILY=='Montana' else .14),5.7,PREVIEW/(FAMILY+'_comparacion_v04.png'))
    scene.render.resolution_y=1200
    caption(camera,'FULGOR MEDIEVAL / '+FAMILY.upper(),-2.52,1.78,.10,GOLD)
    caption(camera,'ANTES Y DESPUES',-2.52,1.47,.22)
    caption(camera,'ANTES',-1.30,-1.22,.13,align='CENTER')
    caption(camera,'DETALLE V04',1.30,-1.22,.13,align='CENTER')
    caption(camera,'Misma camara y luz / Modelos reales / Pendiente de Unity',-2.52,-1.85,.075,GOLD)
    bpy.ops.render.render(write_still=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(FAMILY+'_detalle_v04.blend')))
(EXPORT/'detalle_manifest.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
(PREVIEW/(FAMILY+'_metrics.json')).write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_DETAIL_FAMILY_COMPLETE',FAMILY,len(models),flush=True)
