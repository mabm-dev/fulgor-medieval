"""Original warm watermill with an independently exportable axle-centered wheel."""
from pathlib import Path
import json
import math
import random
import sys

import bpy
import bmesh
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
sys.path.insert(0,str(SOURCE))
from fulgor_terrain_tools import (source_material,tint_object,terrain,join,
                                 bake_export,stage,caption,load_objects)
ROOT = SOURCE.parents[2]
EXPORT = ROOT/'Assets/Fulgor/Art/Buildings/MolinoAguaV01'
PREVIEW = ROOT/'Builds/ArtPreview'
PREVIEW.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects): bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Molino_Detalle'
rng = random.Random(731021)
stone_mat = source_material('M_MolinoPiedra_Fuente',110,.002,.35)
wood_mat = source_material('M_MolinoMadera_Fuente',160,.0015,.40)
tile_mat = source_material('M_MolinoTeja_Fuente',145,.0015,.30)
parts = []
STONE,PLASTER,WOOD,DARK = (.57,.43,.26),(.72,.57,.34),(.24,.10,.028),(.035,.022,.011)


def mesh_part(name,vertices,faces,color,mat=stone_mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    tint_object(obj,lambda p:color,mat)
    parts.append(obj)
    return obj


def box(name,center,size,color,mat=stone_mat,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        modifier = obj.modifiers.new('Bordes usados','BEVEL')
        modifier.width,modifier.segments = bevel,1
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    tint_object(obj,lambda p:color,mat)
    parts.append(obj)
    return obj


def beam(name,a,b,width,depth,color=WOOD):
    midpoint = (Vector(a)+Vector(b))/2
    obj = box(name,midpoint,(width,depth,(Vector(b)-Vector(a)).length),color,wood_mat,.0015)
    obj.rotation_euler = (Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler()
    return obj


def outward_normals(obj):
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(mesh,faces=list(mesh.faces))
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()


# Body base and warm plaster, with individually modeled lower masonry courses.
box('Zocalo',(0,0,.075),(.68,.40,.15),STONE,bevel=.005)
box('Enlucido',(0,0,.274),(.674,.394,.248),PLASTER,bevel=.004)
for side in (-1,1):
    for row in range(3):
        for col in range(8):
            x = -.302+col*.087+(row%2)*.025
            if x > .33: continue
            tone = rng.uniform(.90,1.08)
            box('Sillar',(x,side*.201,.025+row*.049),(.081,.012,.043),tuple(c*tone for c in STONE),bevel=.0015)
for x in (-.325,.325):
    for y in (-.192,.192): box('PieDerecho',(x,y,.276),(.024,.025,.245),WOOD,wood_mat,.002)
for y in (-.200,.200):
    box('Carrera',(0,y,.397),(.69,.024,.025),WOOD,wood_mat,.0015)
    box('Travesano',(0,y,.24),(.65,.021,.017),WOOD,wood_mat,.001)
    beam('Tornapunta',(-.30,y,.25),(-.17,y,.38),.015,.015)
    beam('Tornapunta',(.17,y,.38),(.30,y,.25),.015,.015)
vertices = [(-.337,-.197,.398),(.337,-.197,.398),(0,-.197,.64),
            (-.337,.197,.398),(.337,.197,.398),(0,.197,.64)]
mesh_part('Hastiales',vertices,[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)],PLASTER)
for y in (-.203,.203):
    beam('ParIzquierdo',(-.337,y,.405),(0,y,.647),.018,.018)
    beam('ParDerecho',(0,y,.647),(.337,y,.405),.018,.018)

# Front door and a shuttered upper opening; dark recesses retain depth at distance.
box('HuecoPuerta',(-.185,-.207,.118),(.148,.012,.234),DARK)
for i in range(6):
    box('TablaPuerta',(-.246+i*.024,-.217,.118),(.021,.012,.220),tuple(c*rng.uniform(.94,1.08) for c in WOOD),wood_mat,.001)
for z in (.052,.176): box('HerrajePuerta',(-.185,-.226,z),(.133,.006,.009),(.055,.047,.031))
box('Dintel',(-.185,-.216,.250),(.180,.025,.026),STONE,bevel=.003)
for x in (-.270,-.100): box('Jamba',(x,-.213,.122),(.019,.022,.244),STONE,bevel=.002)
box('Umbral',(-.185,-.235,.010),(.185,.073,.020),STONE,bevel=.003)
box('VentanaAlta',(.12,-.207,.327),(.105,.009,.092),DARK)
for x in (.066,.174): box('MarcoVentana',(x,-.218,.327),(.012,.022,.110),WOOD,wood_mat,.001)
for z in (.275,.378): box('MarcoVentana',(.12,-.218,z),(.12,.022,.012),WOOD,wood_mat,.001)
box('Contraventana',(.195,-.217,.327),(.033,.015,.088),(.30,.14,.035),wood_mat,.001)
box('VentanaLateral',(.340,.042,.287),(.010,.102,.108),DARK)
for y in (-.013,.097): box('MarcoLateral',(.348,y,.287),(.017,.012,.12),WOOD,wood_mat,.001)
for z in (.230,.343): box('MarcoLateral',(.348,.042,z),(.017,.120,.012),WOOD,wood_mat,.001)
box('Parteluz',(.352,.042,.287),(.019,.012,.108),WOOD,wood_mat)

# Roof sheathing, then individually shaped terracotta tiles following both pitches.
for side in (-1,1):
    vertices = [(0,-.248,.653),(side*.398,-.248,.420),(side*.398,.248,.420),(0,.248,.653),
                (0,-.248,.640),(side*.398,-.248,.407),(side*.398,.248,.407),(0,.248,.640)]
    faces = [(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
    if side == -1: faces = [tuple(reversed(f)) for f in faces]
    mesh_part('Faldon',vertices,faces,(.34,.080,.022),tile_mat)
    for row in range(7):
        a,b = row*.398/7,min(.410,(row+1)*.398/7+.009)
        for col in range(8):
            y0 = -.25+col*.0625
            y1 = y0+.060
            vertices = []
            for x in (a,b):
                for j in range(5):
                    t = j/4
                    vertices.append((side*x,y0+(y1-y0)*t,.653-.233*x/.398+.005+.006*math.sin(math.pi*t)))
            vertices += [(x,y,z-.006) for x,y,z in vertices]
            faces = []
            for j in range(4): faces.extend(((j,j+5,j+6,j+1),(j+11,j+16,j+15,j+10)))
            faces.extend(((0,10,15,5),(4,9,19,14),(0,1,2,3,4,14,13,12,11,10),(5,15,16,17,18,19,9,8,7,6)))
            if side == -1: faces = [tuple(reversed(f)) for f in faces]
            color = (.48*rng.uniform(.88,1.13),.105*rng.uniform(.87,1.20),.032)
            mesh_part('Teja',vertices,faces,color,tile_mat)
for i in range(12):
    box('Cumbrera',(0,-.232+i*.042,.666),(.035,.040,.022),(.54,.14,.045),tile_mat,.006)
box('Chimenea',(-.16,.12,.594),(.079,.080,.233),STONE,bevel=.002)
box('CoronaChimenea',(-.16,.12,.716),(.094,.094,.020),(.49,.25,.11),bevel=.002)
box('BocaChimenea',(-.16,.12,.727),(.060,.060,.003),DARK)

# Small working props near the entrance, included in this visual building study.
box('Caja',(-.437,-.078,.055),(.13,.12,.11),WOOD,wood_mat,.003)
for x in (-.487,-.387): box('ListonCaja',(x,-.142,.055),(.014,.012,.105),(.34,.17,.045),wood_mat,.001)
beam('DiagonalCaja',(-.49,-.149,.015),(-.39,-.149,.095),.014,.010,(.34,.17,.045))
for x,y,s in [(-.43,.08,.053),(-.49,.02,.041)]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=(x,y,s*.95))
    obj = bpy.context.object
    obj.name = 'SacoGrano'
    obj.scale = (s*.7,s*.65,s)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    # Keep the bottom above the shared building origin.
    for v in obj.data.vertices: v.co.z = max(v.co.z,-s*.95)
    tint_object(obj,lambda p:(.55,.39,.17),stone_mat)
    for face in obj.data.polygons: face.use_smooth = True
    parts.append(obj)

body = join(parts,'SM_MolinoAgua_Edificio_LOD0')
# Consolidate source object transforms into a conventional lower-center pivot.
bpy.context.scene.cursor.location = (0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
outward_normals(body)
body_report = bake_export(body,EXPORT,2048)
for collection in list(body.users_collection): collection.objects.unlink(body)

parts = []
for y in (-.058,.058):
    for i in range(24):
        a,b = math.tau*i/24,math.tau*(i+1)/24
        vertices = [(r*math.cos(t),yy,r*math.sin(t)) for yy in (y-.009,y+.009) for r,t in ((.193,a),(.229,a),(.229,b),(.193,b))]
        mesh_part('AroRueda',vertices,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],(.27,.12,.032),wood_mat)
    for i in range(8):
        a = math.tau*i/8
        beam('Radio',(0,y,0),(.202*math.cos(a),y,.202*math.sin(a)),.021,.020,(.33,.16,.043))
for i in range(16):
    a = math.tau*i/16
    obj = box('Paleta',(.218*math.cos(a),0,.218*math.sin(a)),(.046,.135,.014),(.32,.14,.031),wood_mat,.0015)
    obj.rotation_euler.y = -a
bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.034,depth=.38,rotation=(math.pi/2,0,0))
axle = bpy.context.object
axle.name = 'EjeRueda'
tint_object(axle,lambda p:(.18,.075,.018),wood_mat)
parts.append(axle)
wheel = join(parts,'SM_MolinoAgua_Rueda_LOD0')
bpy.context.scene.cursor.location = (0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
outward_normals(wheel)
wheel_report = bake_export(wheel,EXPORT,1024,min_height=-.25)
wheel_report.update(pivot='axle center',rotation_axis_blender='Y',rotation_axis_unity='Z; verify FBX orientation before animation')
scene.collection.objects.unlink(wheel)

river, = load_objects(SOURCE/'Conexiones_agua_v03.blend',['SM_Agua_RioRecto_LOD0'])
bridge,road = load_objects(SOURCE/'Puente_piedra_v01.blend',['SM_PuentePiedra_A_LOD0','SM_CaminoPuente_Recto_LOD0'])
river.location = (0,0,0)
river.rotation_euler = (0,0,0)
body.location,wheel.location = (0,.49,.12),(.15,.12,.255)
for obj in (river,body,wheel): scene.collection.objects.link(obj)
GOLD = (.66,.41,.16)
camera = stage(scene,(0,.1,.22),3.8,PREVIEW/'Molino_agua_detalle_v01.png')
scene.render.resolution_y = 1250
caption(camera,'FULGOR MEDIEVAL / ARQUITECTURA RURAL',-1.70,1.29,.060,GOLD)
caption(camera,'EL MOLINO DE LA RIBERA',-1.70,1.08,.120)
caption(camera,'Terracota, piedra y madera / rueda con eje independiente',-1.70,.92,.049,GOLD)
caption(camera,'ESTUDIO V01 / Agua azul / Rueda estatica / Pendiente de Unity',-1.70,-1.32,.043,GOLD)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Molino_Puente_Conjunto')
def place(obj,q,r,angle=0,offset=(0,0,0)):
    copy = obj.copy()
    copy.location = (math.sqrt(3)*(q+r/2)+offset[0],1.5*r+offset[1],offset[2])
    copy.rotation_euler = (0,0,math.radians(angle))
    patch.collection.objects.link(copy)

for q in (-1,0,1,2): place(river,q,0)
place(body,0,0,offset=(0,.49,.12))
place(wheel,0,0,offset=(.15,.12,.255))
place(bridge,1,0,300)
place(road,0,1,300)
place(road,2,-1,300)
camera = stage(patch,(.7,0,.15),9.7,PREVIEW/'Molino_agua_conjunto_v01.png')
patch.render.resolution_y = 1200
caption(camera,'FULGOR MEDIEVAL / VIDA JUNTO AL RIO',-4.32,3.13,.14,GOLD)
caption(camera,'DEL CAMPO AL MOLINO',-4.32,2.65,.29)
caption(camera,'Molino, puente y caminos / estudio de escala del conjunto',-4.32,2.28,.12,GOLD)
caption(camera,'PROTOTIPO VISUAL / Sin produccion ni reglas economicas / Sin integrar en Unity',-4.32,-3.32,.10,GOLD)
bpy.ops.render.render(write_still=True)

wheel_bottom = wheel.location.z+wheel_report['bounds'][2][0]
assert .005 < wheel_bottom < .04  # Paddles enter the water without crossing the tile base.
body_report.update(assembly_offset=[0,.49,.12])
wheel_report.update(assembly_offset=[.15,.12,.255],static_preview=True,water_immersion=.04-wheel_bottom)
bpy.context.window.scene = patch
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D': area.spaces.active.region_3d.view_perspective = 'CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Molino_agua_v01.blend'))
(PREVIEW/'Molino_agua_v01_metrics.json').write_text(json.dumps([body_report,wheel_report],indent=2),encoding='utf-8')
print('FULGOR_MILL_COMPLETE',json.dumps([body_report,wheel_report]))
