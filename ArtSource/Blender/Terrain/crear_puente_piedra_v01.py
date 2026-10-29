"""Original warm masonry bridge, separate overlay mesh and road approach tile."""
from pathlib import Path
import json
import math
import random
import sys

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
sys.path.insert(0,str(SOURCE))
from fulgor_terrain_tools import (source_material,tint_object,terrain,join,
                                 bake_export,stage,caption,load_objects)
ROOT = SOURCE.parents[2]
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/PuentePiedraV01'
PREVIEW = ROOT/'Builds/ArtPreview'
PREVIEW.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects): bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Puente_Detalle'
R = math.sqrt(3)/2
rng = random.Random(20261021)
stone_mat = source_material('M_PiedraCaliza_Fuente',95,.0025,.35)
earth_mat = source_material('M_CaminoPuente_Fuente',110,.002,.3)
parts = []


def smooth(t):
    t = max(0,min(1,t))
    return t*t*(3-2*t)


def stone_color(p):
    shade = .94+.06*math.sin(41*p.x+17*p.y+29*p.z)
    # Discreet moisture and moss near the footings, no global green tint.
    moss = (1-smooth((p.z-.12)/.09))*.28
    return tuple((a*(1-moss)+b*moss)*shade for a,b in zip((.58,.43,.25),(.19,.25,.045)))


def mesh_part(name,vertices,faces,color=stone_color,mat=stone_mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    scene.collection.objects.link(obj)
    tint_object(obj,color,mat)
    parts.append(obj)
    return obj


def slab(name,x0,x1,y0,y1,bottom0,bottom1,top0,top1,color=stone_color):
    vertices = [(x0,y0,bottom0),(x1,y0,bottom1),(x1,y1,bottom1),(x0,y1,bottom0),
                (x0,y0,top0),(x1,y0,top1),(x1,y1,top1),(x0,y1,top0)]
    return mesh_part(name,vertices,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],color)


def deck_height(x):
    return .12+.335*smooth((.78-abs(x))/.46)


def inner_arch(x):
    return .125+.235*math.sqrt(max(0,1-(x/.30)**2))


# Continuous arch barrel; the space below it is open, including front and back.
for i in range(40):
    x0,x1 = -.30+i*.60/40,-.30+(i+1)*.60/40
    slab('Boveda',x0,x1,-.16,.16,inner_arch(x0),inner_arch(x1),deck_height(x0),deck_height(x1))
for side in (-1,1):
    for i in range(16):
        a,b = .30+i*.30/16,.30+(i+1)*.30/16
        x0,x1 = (a,b) if side == 1 else (-b,-a)
        slab('Estribo',x0,x1,-.16,.16,.04,.04,deck_height(x0),deck_height(x1))

# Individually colored voussoirs make the load-bearing arch readable at game zoom.
for side in (-1,1):
    y0,y1 = (-.177,-.1605) if side == -1 else (.1605,.177)
    for i in range(15):
        a,b = math.pi*i/15+.008,math.pi*(i+1)/15-.008
        ring = [(.30*math.cos(a),.125+.235*math.sin(a)),
                (.30*math.cos(b),.125+.235*math.sin(b)),
                (.365*math.cos(b),.125+.292*math.sin(b)),
                (.365*math.cos(a),.125+.292*math.sin(a))]
        vertices = [(x,y,z) for y in (y0,y1) for x,z in ring]
        shade = rng.uniform(.88,1.12)
        mesh_part('Dovela',vertices,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],
                  lambda p,s=shade:tuple(c*s for c in stone_color(p)))
    # Thin masonry joints on the spandrels, clipped above the outer arch ring.
    for row in range(4):
        z0,z1 = .15+row*.071,.15+(row+1)*.071-.004
        for col in range(9):
            x0 = -.59+col*.135+(row%2)*.062
            x1 = min(.59,x0+.129)
            if x0 >= .59: continue
            for j in range(5):
                a,b = x0+(x1-x0)*j/5,x0+(x1-x0)*(j+1)/5
                lower = lambda x:max(z0,.128+.296*math.sqrt(max(0,1-(x/.37)**2))) if abs(x)<.37 else z0
                za,zb = lower(a),lower(b)
                ta,tb = min(z1,deck_height(a)-.012),min(z1,deck_height(b)-.012)
                if ta <= za or tb <= zb: continue
                y = side*.161
                vertices = [(a,y,za),(b,y,zb),(b,y,tb),(a,y,ta)]
                faces = [(0,1,2,3)] if side == -1 else [(3,2,1,0)]
                tone = .91 if (row+col)%3 == 0 else 1.035
                mesh_part('Sillar',vertices,faces,lambda p,s=tone:tuple(c*s for c in stone_color(p)))

# Stone paving over the hump and earth ramps at both ends.
for i in range(38):
    a,b = -R+i*2*R/38,-R+(i+1)*2*R/38
    z0,z1 = deck_height(a),deck_height(b)
    is_stone = abs((a+b)/2)<.60
    color = stone_color if is_stone else lambda p:(.37,.23,.09)
    slab('Calzada',a,b,-.119,.119,z0,z1,
         z0+.010*smooth((R-abs(a))/.08),z1+.010*smooth((R-abs(b))/.08),color)
    if is_stone:
        for lane in range(3):
            y0 = -.115+lane*.077
            slab('Losa',a+.002,b-.002,y0,y0+.072,z0+.010,z1+.010,z0+.013,z1+.013,
                 lambda p,s=rng.uniform(.91,1.07):tuple(c*s for c in stone_color(p)))

# Low parapets and broad coping stones follow the deck profile without battlements.
for side in (-1,1):
    for i in range(18):
        a,b = -.60+i*1.20/18,-.60+(i+1)*1.20/18-.002
        z0,z1 = deck_height(a)+.012,deck_height(b)+.012
        center = side*.146
        slab('Pretil',a,b,center-.022,center+.022,z0,z1,z0+.085,z1+.085)
        slab('Albarda',a-.001,b+.001,center-.027,center+.027,z0+.085,z1+.085,z0+.104,z1+.104,
             lambda p:(.65,.51,.32))

bridge = join(parts,'SM_PuentePiedra_A_LOD0')
bridge_report = bake_export(bridge,EXPORT,2048,radius=1)
bridge_report.update(land_height=.12,foundation_bottom=.04,water_height=.04,arch_clear_height=.32,
                     role='separate visual overlay; road faces 0/180; river faces 60/240')
scene.collection.objects.unlink(bridge)


def road_tint(p):
    t = smooth((abs(p.y)-.11)/.055)
    road = (.37,.23,.09)
    grass = (.13,.36,.025)
    return tuple(a*(1-t)+b*t for a,b in zip(road,grass))


road = terrain('SM_CaminoPuente_Recto_LOD0',lambda x,y:.12,road_tint,earth_mat,count=96,rings=20,radius=1)
road_report = bake_export(road,EXPORT,1024,radius=1)
road_report.update(connection_angles_blender_degrees=[0,180],radius=1)
road.use_fake_user = True  # Keep the named template when only its copies are linked.
scene.collection.objects.unlink(road)
river, = load_objects(SOURCE/'Conexiones_agua_v03.blend',['SM_Agua_RioRecto_LOD0'])
river.location = (0,0,0)
river.rotation_euler = (0,0,math.radians(60))
scene.collection.objects.link(river)
scene.collection.objects.link(bridge)
GOLD = (.66,.41,.16)
camera = stage(scene,(0,0,.20),3.6,PREVIEW/'Puente_piedra_detalle_v01.png')
scene.render.resolution_y = 1250
caption(camera,'FULGOR MEDIEVAL / ARQUITECTURA',-1.61,1.23,.060,GOLD)
caption(camera,'PIEDRA SOBRE EL AGUA',-1.61,1.04,.122)
caption(camera,'Arco abierto, dovelas y calzada elevada',-1.61,.90,.052,GOLD)
caption(camera,'ESTUDIO V01 / Piedra calida / Agua azul / Pendiente de Unity',-1.61,-1.25,.043,GOLD)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Puente_Caminos_Conjunto')
def place(model,q,r,angle=0):
    obj = model.copy()
    obj.location = (math.sqrt(3)*(q+r/2),1.5*r,0)
    obj.rotation_euler = (0,0,math.radians(angle))
    patch.collection.objects.link(obj)

place(river,0,0,60)
place(river,0,1,60)
place(river,0,-1,60)
place(bridge,0,0)
place(road,-1,0)
place(road,1,0)
camera = stage(patch,(0,0,.15),7.8,PREVIEW/'Puente_piedra_conjunto_v01.png')
patch.render.resolution_y = 1200
caption(camera,'FULGOR MEDIEVAL / CAMINOS Y RIOS',-3.45,2.52,.12,GOLD)
caption(camera,'UN PASO ENTRE LAS DOS ORILLAS',-3.45,2.13,.23)
caption(camera,'Puente modular de piedra / acceso por camino / cauce continuo',-3.45,1.83,.10,GOLD)
caption(camera,'PROTOTIPO VISUAL / Sin reglas de cruce ni colisiones / Sin integrar en Unity',-3.45,-2.68,.085,GOLD)
bpy.ops.render.render(write_still=True)

# Geometric clearance at the channel intersection, in the bridge local frame.
assert min(inner_arch(x) for x in (-.24,0,.24)) > .04+.20
assert abs(deck_height(-R)-.12) < 1e-6 and abs(deck_height(R)-.12) < 1e-6
bpy.context.window.scene = patch
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D': area.spaces.active.region_3d.view_perspective = 'CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Puente_piedra_v01.blend'))
(PREVIEW/'Puente_piedra_v01_metrics.json').write_text(json.dumps([bridge_report,road_report],indent=2),encoding='utf-8')
print('FULGOR_BRIDGE_COMPLETE',json.dumps([bridge_report,road_report]))
