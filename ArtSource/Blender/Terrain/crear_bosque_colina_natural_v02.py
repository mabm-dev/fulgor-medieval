"""Natural forest and hill studies, consistent with the approved mountain."""
from pathlib import Path
import json
import math
import random
import sys

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
sys.path.insert(0,str(SOURCE))
from fulgor_terrain_tools import (material,source_material,tint_object,terrain,join,
                                 bake_export,stage,caption,load_objects)

ROOT = SOURCE.parents[2]
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/BiomasNaturalV02'
PREVIEW = ROOT/'Builds/ArtPreview'
PREVIEW.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Bosque_Colina_Natural_Galeria'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1


def fade(x,y):
    edge = max(abs(x)/(.97*math.sqrt(3)/2),
               (abs(x)+math.sqrt(3)*abs(y))/(math.sqrt(3)*.97))
    t = max(0,min(1,(1-edge)/.25))
    return t*t*(3-2*t)


def forest_height(x,y):
    return .18+fade(x,y)*(.018+.012*math.sin(8*x+2*y)*math.cos(11*y))


def hill_height(x,y):
    value = (.29*math.exp(-((x+.23)**2/.20+(y-.10)**2/.30))
             +.18*math.exp(-((x-.36)**2/.09+(y+.14)**2/.19)))
    runoff = .014*abs(math.sin(17*x+3*math.sin(8*y)))*abs(math.sin(11*y))
    grain = .006*math.sin(31*x+17*y)
    return .28+max(0,value-runoff+grain)*fade(x,y)


def ground_tint(p,forest=False):
    x,y,z = p
    variation = .88+.12*math.sin(19*x+3*math.sin(13*y))
    if forest:
        leaflitter = max(0,math.sin(x*13-y*9))*.38
        return ((.075+leaflitter*.14)*variation,
                (.22-leaflitter*.08)*variation,.025)
    eps = .005
    slope = math.hypot((hill_height(x+eps,y)-hill_height(x-eps,y))/(2*eps),
                       (hill_height(x,y+eps)-hill_height(x,y-eps))/(2*eps))
    exposed = max(0,min(1,(slope-.42)*1.8))
    # Sparse ochre exposures instead of the old rings of block colors.
    exposed *= .5+.5*math.sin(6*x-4*y+.7)
    green,stone = (.13,.36,.025),(.38,.26,.12)
    return tuple((green[i]*(1-exposed)+stone[i]*exposed)*variation for i in range(3))


earth = source_material('M_Suelo_Fuente',75,.004,.45)
bark = source_material('M_Corteza_Fuente',95,.006,.65)
leaves = source_material('M_Follaje_Fuente',120,.0008,.32)
stone = source_material('M_Caliza_Fuente',70,.003,.5)


def branch(a,b,radius):
    direction = Vector(b)-Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=7,radius1=radius,radius2=radius*.55,
                                  depth=direction.length,location=(Vector(a)+Vector(b))/2)
    obj = bpy.context.object
    obj.name = 'Rama'
    obj.rotation_euler = direction.to_track_quat('Z','Y').to_euler()
    tint_object(obj,lambda p:(.22,.095,.033),bark)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def leaf_canopy(x,y,z,radius,rng,warm=False):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=(x,y,z))
    obj = bpy.context.object
    obj.name = 'Follaje'
    for v in obj.data.vertices:
        p = v.co
        factor = 1+.09*math.sin(p.x*11+p.y*7)*math.cos(p.z*9)+.04*math.sin(p.z*13-p.x*5)
        v.co *= factor
    obj.scale = (radius,radius*.88,radius*.80)
    base = (.48,.25,.018) if warm else (.055,.25,.015)
    tint_object(obj,lambda p:tuple(c*(.86+.22*(p.z+1)/2) for c in base),leaves)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def leaf_sprays(x,y,z,radius,rng,warm=False):
    vertices,faces = [],[]
    for i in range(65):
        a = rng.uniform(0,math.tau)
        h = rng.uniform(-.7,.8)
        radial = math.sqrt(1-h*h)
        center = Vector((x+radius*radial*math.cos(a),y+radius*.88*radial*math.sin(a),z+radius*.8*h))
        u = Vector((math.cos(a),math.sin(a),.15))*rng.uniform(.014,.027)
        v = Vector((-math.sin(a),math.cos(a),.6))*rng.uniform(.012,.020)
        n = len(vertices)
        vertices.extend((center-u,center+v,center+u,center-v))
        # Both sides are geometry, so leaf silhouettes do not need special URP culling.
        faces.extend(((n,n+2,n+1),(n,n+3,n+2),(n,n+1,n+2),(n,n+2,n+3)))
    mesh = bpy.data.meshes.new('Hojas')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new('Hojas',mesh)
    scene.collection.objects.link(obj)
    color = (.56,.30,.025) if warm else (.075,.30,.020)
    tint_object(obj,lambda p:color,leaves)
    return obj


def oak(x,y,scale,seed,warm=False):
    rng = random.Random(seed)
    base = forest_height(x,y)
    parts = [branch((x,y,base),(x+.025*scale,y,base+.38*scale),.027*scale)]
    for i,(dx,dy,dz,r) in enumerate(((-.09,-.03,.42,.18),(.10,-.01,.45,.17),
                                    (-.04,.10,.53,.17),(.04,.04,.65,.16))):
        location = (x+(dx+rng.uniform(-.03,.03))*scale,
                    y+(dy+rng.uniform(-.03,.03))*scale,
                    base+(dz+rng.uniform(-.035,.035))*scale)
        r *= rng.uniform(.88,1.12)
        parts.append(branch((x+.01,y,base+.24*scale),location,.010*scale))
        parts.append(leaf_canopy(*location,r*scale,rng,warm))
        parts.append(leaf_sprays(*location,r*scale,rng,warm))
    return parts


def rock(x,y,height,rng):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,height+.018))
    obj = bpy.context.object
    obj.name = 'Caliza'
    obj.scale = (rng.uniform(.023,.052),rng.uniform(.025,.043),rng.uniform(.017,.032))
    for v in obj.data.vertices:
        v.co *= rng.uniform(.85,1.15)
    tint_object(obj,lambda p:(.43,.35,.23),stone)
    return obj


forest_parts = [terrain('SueloBosque',forest_height,lambda p:ground_tint(p,True),earth)]
for x,y,s,seed,warm in ((-.34,-.12,.82,100,False),(.28,-.14,.84,200,False),
                       (-.16,.36,.90,300,False),(.29,.37,.68,400,False),
                       (.03,-.49,.45,500,True)):
    forest_parts.extend(oak(x,y,s,seed,warm))
rng = random.Random(699)
for i in range(14):
    a,r = rng.uniform(0,math.tau),rng.uniform(.15,.68)
    x,y = r*math.cos(a),r*math.sin(a)
    forest_parts.append(rock(x,y,forest_height(x,y),rng))
forest = join(forest_parts,'SM_BosqueNatural_A_LOD0')
reports = [bake_export(forest,EXPORT,2048)]

hill_parts = [terrain('SueloColina',hill_height,ground_tint,earth)]
rng = random.Random(902)
for i in range(24):
    a,r = rng.uniform(0,math.tau),rng.uniform(.2,.70)
    x,y = r*math.cos(a),r*math.sin(a)
    hill_parts.append(rock(x,y,hill_height(x,y),rng))
hill = join(hill_parts,'SM_ColinaNatural_A_LOD0')
reports.append(bake_export(hill,EXPORT,1024))


def plain_height(x,y):
    return .12+fade(x,y)*(.003+.003*math.sin(9*x+2*y)*math.cos(8*y))


def plain_tint(p):
    variation = .91+.09*math.sin(p.x*23+3*math.sin(p.y*17))
    dry = max(0,math.sin(p.x*7-p.y*5))*.15
    return ((.13+dry*.25)*variation,(.36+dry*.05)*variation,.025+dry*.05)


def grass_patch(x,y,rng):
    vertices,faces = [],[]
    z = plain_height(x,y)
    for i in range(4):
        a = rng.uniform(0,math.tau)
        width,height = rng.uniform(.002,.004),rng.uniform(.030,.062)
        u = Vector((math.cos(a),math.sin(a),0))
        center = Vector((x+rng.uniform(-.014,.014),y+rng.uniform(-.014,.014),z))
        n = len(vertices)
        vertices.extend((center-u*width,center+u*width,center+u*.012+Vector((0,0,height))))
        faces.extend(((n,n+1,n+2),(n,n+2,n+1)))
    mesh = bpy.data.meshes.new('Hierba')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new('Hierba',mesh)
    scene.collection.objects.link(obj)
    tint_object(obj,lambda p:(.26,.36,.035),leaves)
    return obj


plain_parts = [terrain('SueloLlanura',plain_height,plain_tint,earth,count=60,rings=16)]
rng = random.Random(1103)
for i in range(65):
    a,r = rng.uniform(0,math.tau),math.sqrt(rng.random())*.72
    plain_parts.append(grass_patch(r*math.cos(a),r*math.sin(a),rng))
for i in range(12):
    a,r = rng.uniform(0,math.tau),rng.uniform(.15,.70)
    x,y = r*math.cos(a),r*math.sin(a)
    plain_parts.append(rock(x,y,plain_height(x,y),rng))
plain = join(plain_parts,'SM_LlanuraNatural_A_LOD0')
reports.append(bake_export(plain,EXPORT,1024))
# Keep the plain out of the two-model gallery, but retain it for the assembled scene.
scene.collection.objects.unlink(plain)

forest.location,hill.location = (-1.8,0,0),(1.8,0,0)
camera = stage(scene,(0,0,.12),8.4,PREVIEW/'Bosque_colina_natural_v02.png')
gold = (.66,.41,.16)
caption(camera,'FULGOR MEDIEVAL / TERRENOS NATURALES',-3.72,2.10,.15,gold)
caption(camera,'EL BOSQUE Y LAS LOMAS',-3.72,1.70,.32)
caption(camera,'Copas irregulares, sotobosque y relieve continuo / color calido',-3.72,1.38,.13,gold)
caption(camera,'BOSQUE / V02',-1.8,-1.35,.20,align='CENTER')
caption(camera,'COLINA / V02',1.8,-1.35,.20,align='CENTER')
caption(camera,'Robles, hojarasca y pequenas rocas',-1.8,-1.65,.13,gold,'CENTER')
caption(camera,'Lomas erosionadas y caliza dispersa',1.8,-1.65,.13,gold,'CENTER')
caption(camera,'PROTOTIPO  /  Texturas propias  /  Un material por casilla  /  Pendiente de Unity',
        -3.72,-2.20,.12,gold)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Biomas_Naturales_Conjunto')
for model,location in ((forest,(-math.sqrt(3),0,0)),(hill,(math.sqrt(3),0,0))):
    copy = model.copy()
    copy.location = location
    patch.collection.objects.link(copy)
mountain = load_objects(SOURCE/'Montana_natural_v02.blend',['SM_MontanaNatural_A_LOD0'])[0]
mountain.location = (math.sqrt(3)/2,1.5,0)
patch.collection.objects.link(mountain)
plain.location = (0,0,0)
patch.collection.objects.link(plain)
camera = stage(patch,(0,.6,.10),7.8,PREVIEW/'Biomas_naturales_conjunto_v02.png')
caption(camera,'FULGOR MEDIEVAL',-3.45,1.93,.16,gold)
caption(camera,'PAISAJES CON VIDA',-3.45,1.54,.29)
caption(camera,'Llanura, bosque y colina revisados junto a la sierra aprobada',-3.45,1.23,.12,gold)
caption(camera,'COLOR CALIDO  /  Estudio de conjunto  /  Integracion y transiciones pendientes',
        -3.45,-1.98,.11,gold)
bpy.ops.render.render(write_still=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
            area.spaces.active.shading.color_type = 'MATERIAL'
bpy.context.window.scene = patch
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Bosque_colina_natural_v02.blend'))
(PREVIEW/'Bosque_colina_natural_v02_metrics.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_NATURAL_BIOMES_COMPLETE',json.dumps(reports))
