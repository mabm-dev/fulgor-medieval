"""Original coastal/water and rural terrain studies with portable baked maps."""
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
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/AguaRuralNaturalV01'
PREVIEW = ROOT/'Builds/ArtPreview'
PREVIEW.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Agua_Orillas_Galeria'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1


def smooth(value):
    t = max(0,min(1,value))
    return t*t*(3-2*t)


def fade(x,y):
    edge = max(abs(x)/(.97*math.sqrt(3)/2),
               (abs(x)+math.sqrt(3)*abs(y))/(math.sqrt(3)*.97))
    return smooth((1-edge)/.20)


def blend(a,b,t):
    return tuple(a[i]*(1-t)+b[i]*t for i in range(3))


ground_mat = source_material('M_TierraNatural_Fuente',90,.003,.45)
grain_mat = source_material('M_Cereal_Fuente',100,.001,.30)
water_mat = source_material('M_Agua_Fuente',150,.003,.30)
water_shader = water_mat.node_tree.nodes.get('Principled BSDF')
water_shader.inputs['IOR'].default_value = 1.333
wetness = water_mat.node_tree.nodes.new('ShaderNodeVertexColor')
wetness.layer_name = 'WaterMask'
roughness = water_mat.node_tree.nodes.new('ShaderNodeMapRange')
roughness.inputs['To Min'].default_value = .90
roughness.inputs['To Max'].default_value = .18
water_mat.node_tree.links.new(wetness.outputs['Color'],roughness.inputs['Value'])
water_mat.node_tree.links.new(roughness.outputs['Result'],water_shader.inputs['Roughness'])


def water_level(x,y):
    return .04+fade(x,y)*(.0016*math.sin(31*x+18*y)+.0008*math.sin(47*y-12*x))


def shore_distance(kind,x,y):
    if kind == 'Mar':
        return -1
    if kind == 'Costa':
        return y-.025-.06*math.sin(5*x)-.014*math.sin(19*x)
    if kind == 'Rio':
        center = .075*math.sin(math.pi*x/(.97*math.sqrt(3)/2))
        return abs(y-center)-.15
    angle = math.atan2(y-.015,x+.025)
    return math.hypot((x+.025)*1.03,(y-.015)*.88)-(.49+.024*math.sin(angle*5))


def water_terrain(kind):
    def height(x,y):
        d = shore_distance(kind,x,y)
        rise = smooth((d+.10)/.24)
        return water_level(x,y)*(1-rise)+(.12+.004*fade(x,y)*math.sin(9*x+7*y))*rise

    def tint(p):
        x,y,_ = p
        d = shore_distance(kind,x,y)
        if d < -.15:
            depth = smooth((-d-.15)/.40)
            color = blend((.025,.35,.30),(.008,.14,.22),depth)
            ripple = .95+.05*math.sin(37*x+19*y)
            return tuple(c*ripple for c in color)
        if d < -.025:
            color = blend((.025,.35,.30),(.30,.26,.13),smooth((d+.15)/.125))
            if kind == 'Costa':
                foam = math.exp(-((d+.115+.008*math.sin(33*x))/.018)**2)*.18
                color = blend(color,(.55,.60,.46),foam)
            return color
        if d < .10:
            return blend((.30,.26,.13),(.60,.43,.19),smooth((d+.025)/.125))
        return blend((.60,.43,.19),(.13,.36,.025),smooth((d-.10)/.16))

    obj = terrain('SM_'+kind+'Natural_A_LOD0',height,tint,water_mat,count=96,rings=28)
    mask = obj.data.color_attributes.new(name='WaterMask',type='FLOAT_COLOR',domain='CORNER')
    for face in obj.data.polygons:
        for index in face.loop_indices:
            p = obj.data.vertices[obj.data.loops[index].vertex_index].co
            value = 1-smooth((shore_distance(kind,p.x,p.y)+.14)/.10) if face.use_smooth else 0
            mask.data[index].color = (value,value,value,1)
    parts = [obj]
    rng = random.Random(100+sum(ord(c) for c in kind))
    if kind != 'Mar':
        for i in range(11):
            if kind == 'Costa':
                x = -.66+i*.128
                y = .025+.06*math.sin(5*x)+.014*math.sin(19*x)+rng.uniform(.16,.23)
            elif kind == 'Rio':
                x = -.63+i*.125
                center = .075*math.sin(math.pi*x/R)
                y = center+(-1 if i%2 else 1)*rng.uniform(.24,.29)
            else:
                a = math.tau*i/11
                x,y = -.025+.64*math.cos(a),.015+.65*math.sin(a)
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,
                                                 location=(x,y,height(x,y)+.012))
            rock = bpy.context.object
            rock.name = 'Piedra_Orilla'
            rock.scale = (rng.uniform(.022,.045),rng.uniform(.020,.038),rng.uniform(.016,.028))
            tint_object(rock,lambda p:(.52,.42,.25),ground_mat)
            parts.append(rock)
            # Small closed rush blades retain their silhouette without alpha materials.
            vertices,faces = [],[]
            z = height(x+.024,y)
            for blade in range(3):
                bx,by = x+rng.uniform(.015,.033),y+rng.uniform(-.012,.012)
                n = len(vertices)
                for corner in range(3):
                    a = math.tau*corner/3
                    vertices.append((bx+.003*math.cos(a),by+.003*math.sin(a),z))
                vertices.append((bx+.009,by,z+rng.uniform(.035,.065)))
                faces.extend(((n,n+1,n+3),(n+1,n+2,n+3),(n+2,n,n+3),(n+2,n+1,n)))
            mesh = bpy.data.meshes.new('Juncos')
            mesh.from_pydata(vertices,[],faces)
            mesh.update()
            rushes = bpy.data.objects.new('Juncos',mesh)
            scene.collection.objects.link(rushes)
            tint_object(rushes,lambda p:(.20,.31,.035),ground_mat)
            parts.append(rushes)
        obj = join(parts,obj.name)
    report = bake_export(obj,EXPORT,1024,bake_roughness=True)
    report['static_water'] = True
    scene.collection.objects.unlink(obj)
    return obj,report


water_models,reports = [],[]
R = .97*math.sqrt(3)/2
for kind in ('Mar','Costa','Lago','Rio'):
    model,report = water_terrain(kind)
    water_models.append(model)
    reports.append(report)


def segment_distance(x,y,a,b):
    dx,dy = b[0]-a[0],b[1]-a[1]
    t = max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)))
    return math.hypot(x-a[0]-dx*t,y-a[1]-dy*t)


R = .97*math.sqrt(3)/2
curve_points = []
for i in range(25):
    t = i/24
    curve_points.append(((1-t)**2*(-R)+2*(1-t)*t*(-.08)+t*t*(R*.5),
                         t*t*(R*math.sqrt(3)/2)))
ROAD_PATHS = {
    'Recto': [((-R,0),(R,0))],
    'Curva': list(zip(curve_points,curve_points[1:])),
    'Cruce': [((-R,0),(R,0)),((0,0),(R*.5,R*math.sqrt(3)/2))],
}
ROAD_PORTS = {'Recto':[0,180],'Curva':[60,180],'Cruce':[0,60,180]}


def road(kind):
    paths = ROAD_PATHS[kind]
    def distance(x,y):
        return min(segment_distance(x,y,a,b) for a,b in paths)

    def height(x,y):
        d = distance(x,y)
        ruts = .003*math.exp(-((d-.045)/.013)**2)
        return .12+fade(x,y)*(.003*math.sin(17*x+13*y)-ruts)

    def tint(p):
        x,y,_ = p
        d = distance(x,y)
        worn = smooth((.145-d)/.045)
        dust = (.43,.25,.09)
        rut = math.exp(-((d-.045)/.015)**2)*.20
        dust = tuple(c*(1-rut) for c in dust)
        grass = (.13,.36,.025)
        return blend(grass,dust,worn)

    obj = terrain('SM_CaminoNatural_'+kind+'_LOD0',height,tint,ground_mat,count=72,rings=22)
    report = bake_export(obj,EXPORT,1024)
    report['connection_angles_blender_degrees'] = ROAD_PORTS[kind]
    reports.append(report)
    scene.collection.objects.unlink(obj)
    return obj


roads = [road(kind) for kind in ('Recto','Curva','Cruce')]


def field_height(x,y):
    inside = smooth((.62-abs(x))/.05)*smooth((.56-abs(y))/.05)
    return .12+inside*.0025*math.cos(82*y)+fade(x,y)*.002


def field_tint(p):
    inside = smooth((.62-abs(p.x))/.05)*smooth((.56-abs(p.y))/.05)
    furrow = .87+.13*math.cos(82*p.y)
    return blend((.13,.36,.025),(.34*furrow,.235*furrow,.07*furrow),inside)


field_parts = [terrain('SueloCereal',field_height,field_tint,ground_mat,count=72,rings=20)]
rng = random.Random(20261021)
vertices,faces = [],[]
for row in range(14):
    for column in range(19):
        x = -.56+column*.062+rng.uniform(-.012,.012)
        y = -.50+row*.076+rng.uniform(-.006,.006)
        z = field_height(x,y)
        h = rng.uniform(.065,.105)
        lean = rng.uniform(.006,.018)
        n = len(vertices)
        # Closed stalk and grain volume avoid dark coplanar front/back baking.
        for i in range(3):
            a = math.tau*i/3
            vertices.append((x+.0015*math.cos(a),y+.0015*math.sin(a),z))
        vertices.append((x+lean,y,z+h))
        faces.extend(((n,n+1,n+3),(n+1,n+2,n+3),(n+2,n,n+3),(n+2,n+1,n)))
        n = len(vertices)
        vertices.extend(((x+lean,y,z+h+.020),(x+lean,y,z+h-.015)))
        for i in range(4):
            a = math.tau*i/4
            vertices.append((x+lean+.009*math.cos(a),y+.009*math.sin(a),z+h))
        for i in range(4):
            j = (i+1)%4
            faces.extend(((n,n+2+i,n+2+j),(n+1,n+2+j,n+2+i)))
mesh = bpy.data.meshes.new('Espigas')
mesh.from_pydata(vertices,[],faces)
mesh.update()
ears = bpy.data.objects.new('Espigas',mesh)
scene.collection.objects.link(ears)
tint_object(ears,lambda p:(.76,.45,.055),grain_mat)
field_parts.append(ears)
field = join(field_parts,'SM_CerealNatural_A_LOD0')
reports.append(bake_export(field,EXPORT,2048))
scene.collection.objects.unlink(field)

GOLD = (.66,.41,.16)
for model,x in zip(water_models,(-3.45,-1.15,1.15,3.45)):
    scene.collection.objects.link(model)
    model.location.x = x
camera = stage(scene,(0,0,.10),10.4,PREVIEW/'Aguas_costas_naturales_v01.png')
caption(camera,'FULGOR MEDIEVAL / AGUAS Y ORILLAS',-4.62,2.60,.17,GOLD)
caption(camera,'DEL MAR A LOS RIOS',-4.62,2.10,.37)
caption(camera,'Agua turquesa, playas calidas y riberas naturales',-4.62,1.72,.15,GOLD)
for x,label,sub in zip((-3.45,-1.15,1.15,3.45),('MAR','COSTA','LAGO','RIO'),
                       ('Agua abierta','Arena y vegetacion','Orilla cerrada','Cauce entre riberas')):
    caption(camera,label,x,-1.25,.21,align='CENTER')
    caption(camera,sub,x,-1.58,.13,GOLD,'CENTER')
caption(camera,'PROTOTIPO  /  Agua estatica  /  Color, normal y rugosidad propios  /  Pendiente de Unity',
        -4.62,-2.65,.12,GOLD)
bpy.ops.render.render(write_still=True)

rural = bpy.data.scenes.new('Cereal_Caminos_Galeria')
for model,x in zip((field,*roads),(-3.45,-1.15,1.15,3.45)):
    model.location.x = x
    rural.collection.objects.link(model)
camera = stage(rural,(0,0,.1),10.4,PREVIEW/'Cereal_caminos_naturales_v01.png')
caption(camera,'FULGOR MEDIEVAL / PAISAJE RURAL',-4.62,2.60,.17,GOLD)
caption(camera,'COSECHAS Y CAMINOS',-4.62,2.10,.37)
caption(camera,'Cereal dorado, rodadas y sendas con enlaces por los bordes',-4.62,1.72,.15,GOLD)
for x,label,sub in zip((-3.45,-1.15,1.15,3.45),('CEREAL','RECTA','CURVA','CRUCE'),
                       ('Hileras de espigas','Tierra batida','Giro entre casillas','Bifurcacion en tres brazos')):
    caption(camera,label,x,-1.25,.21,align='CENTER')
    caption(camera,sub,x,-1.58,.12,GOLD,'CENTER')
caption(camera,'PROTOTIPO  /  Texturas propias  /  Enlaces visuales  /  Sin cambiar las reglas del mapa',
        -4.62,-2.65,.12,GOLD)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Paisaje_Rural_Conjunto')
def place(model,q,r,angle=0):
    copy = model.copy()
    copy.location = (math.sqrt(3)*(q+r/2),1.5*r,0)
    copy.rotation_euler.z = math.radians(angle)
    patch.collection.objects.link(copy)

# A coherent visual road: three pieces at ground height, then a 60-degree turn.
place(roads[0],-1,0)
place(roads[0],0,0)
place(roads[1],1,0)
place(roads[0],1,1,60)
place(field,-1,1)
plain,forest,hill = load_objects(SOURCE/'Bosque_colina_natural_v02.blend',
    ['SM_LlanuraNatural_A_LOD0','SM_BosqueNatural_A_LOD0','SM_ColinaNatural_A_LOD0'])
place(forest,0,1)
place(plain,-2,1)
place(water_models[1],0,-1)
place(water_models[0],1,-2)
camera = stage(patch,(.15,-.40,.10),11.6,PREVIEW/'Paisaje_rural_natural_v01.png')
patch.render.resolution_y = 1200
caption(camera,'FULGOR MEDIEVAL',-5.15,3.80,.17,GOLD)
caption(camera,'VIDA ENTRE CAMPOS Y COSTAS',-5.15,3.25,.33)
caption(camera,'Estudio visual del kit / caminos enlazados y variantes de agua',-5.15,2.85,.14,GOLD)
caption(camera,'COLOR CALIDO  /  Transiciones y agua animada pendientes  /  Sin integrar en Unity',
        -5.15,-3.85,.12,GOLD)
bpy.ops.render.render(write_still=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
            area.spaces.active.shading.color_type = 'MATERIAL'
bpy.context.window.scene = patch
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Agua_cereal_caminos_natural_v01.blend'))
(PREVIEW/'Agua_cereal_caminos_natural_v01_metrics.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_WATER_RURAL_COMPLETE',json.dumps(reports))
