"""Refined original shores, dense crops and rutted roads; portable baked maps."""
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
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/AguaRuralNaturalV02'
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


ground_mat = source_material('M_TierraNatural_Fuente',105,.004,.55)
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
    return .04+fade(x,y)*(.0012*math.sin(37*x+23*y+1.1*math.sin(9*y))+
                         .0007*math.sin(61*y-19*x))


def coast_line(x):
    return .02+.11*math.sin(3.4*x)+.045*math.sin(8*x+.5)


def river_shape(x):
    t = x/(.97*math.sqrt(3)/2)
    center = .10*math.sin(math.pi*t)+.025*math.sin(2*math.pi*t)
    width = .14+.014*math.cos(2*math.pi*t)
    return center,width


def shore_distance(kind,x,y):
    if kind == 'Mar':
        return -1
    if kind == 'Costa':
        return y-coast_line(x)
    if kind == 'Rio':
        center,width = river_shape(x)
        return abs(y-center)-width
    angle = math.atan2(y-.015,x+.025)
    return math.hypot((x+.025)*1.03,(y-.015)*.88)-(.44+.065*math.sin(angle*3+.6)+.023*math.sin(angle*7))


WATER_SHALLOW = (.025,.28,.62)
WATER_DEEP = (.006,.065,.32)
foam_mat = material('M_Espuma_Fuente',(.22,.40,.62))
foam_mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = .48


def foam_ribbons(kind):
    if kind != 'Costa':
        return None
    lines = [[(x,coast_line(x)-.045) for x in [-.81+i*1.62/128 for i in range(129)]]]
    vertices,faces = [],[]
    for line in lines:
        for i in range(len(line)-1):
            coverage = math.sin(i*.16+1.7*math.sin(i*.11))+.35*math.sin(i*.63)
            if coverage < .6:
                continue
            a,b = Vector((*line[i],0)),Vector((*line[i+1],0))
            tangent = b-a
            if tangent.length < 1e-6:
                continue
            across = Vector((-tangent.y,tangent.x,0)).normalized()*(.001+.0025*math.sin(i*.42)**2)
            n = len(vertices)
            for p in (a-across,b-across,b+across,a+across):
                p.z = water_level(p.x,p.y)+.0008
                vertices.append(p)
            faces.append((n,n+1,n+2,n+3))
    if not faces:
        return None
    mesh = bpy.data.meshes.new('Espuma_Rota')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new('Espuma_Rota',mesh)
    scene.collection.objects.link(obj)
    mesh.materials.append(foam_mat)
    return obj


def water_terrain(kind):
    def height(x,y):
        d = shore_distance(kind,x,y)
        rise = smooth((d+.025)/.12)
        sand = .12+fade(x,y)*(.006*math.sin(17*x+7*y)*math.cos(13*y))
        return water_level(x,y)*(1-rise)+sand*rise

    def tint(p):
        x,y,_ = p
        d = shore_distance(kind,x,y)
        if d < -.040:
            depth = smooth((-d-.04)/.40)
            color = blend(WATER_SHALLOW,WATER_DEEP,depth)
            ripple = .96+.04*math.sin(51*x+23*y+math.sin(17*y))
            return tuple(c*ripple for c in color)
        if d < .012:
            return blend(WATER_SHALLOW,(.29,.22,.11),smooth((d+.04)/.052))
        if d < .09:
            variation = .94+.06*math.sin(47*x+19*y)
            return tuple(c*variation for c in blend((.29,.22,.11),(.59,.41,.18),smooth((d-.012)/.078)))
        grass_edge = .09+.028*math.sin(17*x+11*y)+.014*math.sin(31*y)
        return blend((.59,.41,.18),(.13,.36,.025),smooth((d-grass_edge)/.07))

    obj = terrain('SM_'+kind+'Natural_A_LOD0',height,tint,water_mat,count=120,rings=36)
    mask = obj.data.color_attributes.new(name='WaterMask',type='FLOAT_COLOR',domain='CORNER')
    for face in obj.data.polygons:
        for index in face.loop_indices:
            p = obj.data.vertices[obj.data.loops[index].vertex_index].co
            value = 1-smooth((shore_distance(kind,p.x,p.y)+.045)/.055) if face.use_smooth else 0
            mask.data[index].color = (value,value,value,1)
    parts = [obj]
    rng = random.Random(100+sum(ord(c) for c in kind))
    if kind != 'Mar':
        candidates = []
        for attempt in range(3000):
            x,y = rng.uniform(-.68,.68),rng.uniform(-.68,.68)
            if .035 < shore_distance(kind,x,y) < .14 and math.hypot(x,y) < .68:
                candidates.append((x,y))
        if not candidates:
            raise ValueError('No safe bank positions for '+kind)
        anchors = rng.sample(candidates,min(4,len(candidates)))
        for i in range(23):
            ax,ay = anchors[i%len(anchors)]
            x,y = ax+rng.uniform(-.07,.07),ay+rng.uniform(-.07,.07)
            if shore_distance(kind,x,y) < .005 or math.hypot(x,y) > .74:
                continue
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,
                                                 location=(x,y,height(x,y)+.012))
            rock = bpy.context.object
            rock.name = 'Piedra_Orilla'
            size = rng.uniform(.013,.050)
            rock.scale = (size*1.5,size*rng.uniform(.6,1.2),size*rng.uniform(.45,.85))
            rock.rotation_euler.z = rng.uniform(0,math.tau)
            tint_object(rock,lambda p:(.52,.42,.25),ground_mat)
            parts.append(rock)
            # Small closed rush blades retain their silhouette without alpha materials.
            vertices,faces = [],[]
            z = height(x+.024,y)
            for blade in range(rng.randint(3,7)):
                bx,by = x+rng.uniform(.015,.033),y+rng.uniform(-.012,.012)
                n = len(vertices)
                for corner in range(3):
                    a = math.tau*corner/3
                    vertices.append((bx+.003*math.cos(a),by+.003*math.sin(a),z))
                vertices.append((bx+rng.uniform(-.018,.018),by+.008,z+rng.uniform(.035,.080)))
                faces.extend(((n,n+1,n+3),(n+1,n+2,n+3),(n+2,n,n+3),(n+2,n+1,n)))
            mesh = bpy.data.meshes.new('Juncos')
            mesh.from_pydata(vertices,[],faces)
            mesh.update()
            rushes = bpy.data.objects.new('Juncos',mesh)
            scene.collection.objects.link(rushes)
            tint_object(rushes,lambda p:(.20,.31,.035),ground_mat)
            parts.append(rushes)
        foam = foam_ribbons(kind)
        if foam is not None:
            parts.append(foam)
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


curve_points,straight_points = [],[]
for i in range(49):
    t = i/48
    curve_points.append(((1-t)**3*(-R)+3*(1-t)**2*t*(-.35)+3*(1-t)*t*t*(R*.5-.175)+t**3*(R*.5),
                         3*(1-t)*t*t*(R*math.sqrt(3)/2-.35*math.sqrt(3)/2)+t**3*(R*math.sqrt(3)/2)))
    straight_points.append((-R+2*R*t,.025*math.sin(math.tau*t)*math.sin(math.pi*t)**2))
ROAD_PATHS = {
    'Recto': list(zip(straight_points,straight_points[1:])),
    'Curva': list(zip(curve_points,curve_points[1:])),
    'Cruce': [*zip(straight_points,straight_points[1:]),((0,0),(R*.5,R*math.sqrt(3)/2))],
}
ROAD_PORTS = {'Recto':[0,180],'Curva':[60,180],'Cruce':[0,60,180]}


def road(kind):
    paths = ROAD_PATHS[kind]
    def distance(x,y):
        return min(segment_distance(x,y,a,b) for a,b in paths)

    def height(x,y):
        d = distance(x,y)
        ruts = .008*math.exp(-((d-.055)/.017)**2)
        crown = .003*math.exp(-(d/.035)**2)
        texture = .002*math.sin(57*x+37*y)*math.sin(41*y)
        return .12+fade(x,y)*(.003*math.sin(17*x+13*y)+texture+ crown-ruts)

    def tint(p):
        x,y,_ = p
        d = distance(x,y)
        width = .14+fade(x,y)*(.012*math.sin(19*x+13*y)+.008*math.sin(33*y))
        worn = smooth((width-d)/.026)
        grain = .91+.09*math.sin(63*x+19*y)*math.cos(47*y)
        dust = (.40*grain,.235*grain,.085*grain)
        rut = math.exp(-((d-.055)/.018)**2)*.32
        dust = tuple(c*(1-rut) for c in dust)
        center_grass = math.exp(-(d/.023)**2)*.30*fade(x,y)
        dust = blend(dust,(.16,.28,.030),center_grass)
        grass = (.13,.36,.025)
        return blend(grass,dust,worn)

    obj = terrain('SM_CaminoNatural_'+kind+'_LOD0',height,tint,ground_mat,count=96,rings=30)
    parts = [obj]
    rng = random.Random(800+sum(ord(c) for c in kind))
    for attempt in range(240):
        x,y = rng.uniform(-.69,.69),rng.uniform(-.69,.69)
        d = distance(x,y)
        if math.hypot(x,y) > .72 or not .09 < d < .19 or rng.random() < .55:
            continue
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,height(x,y)+.004))
        rock = bpy.context.object
        size = rng.uniform(.007,.019)
        rock.scale = (size*1.3,size,size*.55)
        rock.rotation_euler.z = rng.uniform(0,math.tau)
        tint_object(rock,lambda p:(.42,.34,.22),ground_mat)
        parts.append(rock)
    obj = join(parts,obj.name)
    report = bake_export(obj,EXPORT,1024)
    report['connection_angles_blender_degrees'] = ROAD_PORTS[kind]
    reports.append(report)
    scene.collection.objects.unlink(obj)
    return obj


roads = [road(kind) for kind in ('Recto','Curva','Cruce')]


def field_inside(x,y):
    return smooth((.61+.022*math.sin(9*y)-abs(x))/.045)*smooth((.55+.018*math.sin(11*x)-abs(y))/.045)


def field_height(x,y):
    inside = field_inside(x,y)
    return .12+inside*.003*math.cos(114*y+math.sin(8*x))+fade(x,y)*(.002+.003*math.sin(7*x+5*y))


def field_tint(p):
    inside = field_inside(p.x,p.y)
    furrow = .86+.14*math.cos(114*p.y+math.sin(8*p.x))
    return blend((.13,.36,.025),(.39*furrow,.27*furrow,.075*furrow),inside)


field_parts = [terrain('SueloCereal',field_height,field_tint,ground_mat,count=96,rings=28)]
rng = random.Random(20261021)
vertices,faces = [],[]
for row in range(22):
    for column in range(32):
        if rng.random() < .045:
            continue
        x = -.56+column*(1.12/31)+rng.uniform(-.017,.017)
        y = -.50+row*(1.0/21)+rng.uniform(-.012,.012)+.012*math.sin(column*.35)
        z = field_height(x,y)
        h = rng.uniform(.11,.18)
        lean = rng.uniform(.012,.033)
        if ((x-.25)/.11)**2+((y+.17)/.13)**2 < 1:
            h *= .45
            lean *= 1.6
        n = len(vertices)
        # Closed stalk and grain volume avoid dark coplanar front/back baking.
        for i in range(3):
            a = math.tau*i/3
            vertices.append((x+.0015*math.cos(a),y+.0015*math.sin(a),z))
        vertices.append((x+lean,y,z+h))
        faces.extend(((n,n+1,n+3),(n+1,n+2,n+3),(n+2,n,n+3),(n+2,n+1,n)))
        n = len(vertices)
        vertices.extend(((x+lean,y,z+h+.030),(x+lean,y,z+h-.022)))
        for i in range(6):
            a = math.tau*i/6
            vertices.append((x+lean+.008*math.cos(a),y+.008*math.sin(a),z+h))
        for i in range(6):
            j = (i+1)%6
            faces.extend(((n,n+2+i,n+2+j),(n+1,n+2+j,n+2+i)))
mesh = bpy.data.meshes.new('Espigas')
mesh.from_pydata(vertices,[],faces)
mesh.update()
ears = bpy.data.objects.new('Espigas',mesh)
scene.collection.objects.link(ears)
tint_object(ears,lambda p:(.68+.05*math.sin(p.x*29+p.y*13),.41+.04*math.cos(p.y*31),.040),grain_mat)
field_parts.append(ears)
field = join(field_parts,'SM_CerealNatural_A_LOD0')
reports.append(bake_export(field,EXPORT,2048))
scene.collection.objects.unlink(field)

GOLD = (.66,.41,.16)


for model,x in zip(water_models,(-3.45,-1.15,1.15,3.45)):
    scene.collection.objects.link(model)
    model.location.x = x
camera = stage(scene,(0,0,.10),10.4,PREVIEW/'Aguas_costas_naturales_v02.png')
caption(camera,'FULGOR MEDIEVAL / AGUAS Y ORILLAS',-4.62,2.60,.17,GOLD)
caption(camera,'DEL MAR A LOS RIOS',-4.62,2.10,.37)
caption(camera,'Revision V02 / playas irregulares, piedras agrupadas y juncos',-4.62,1.72,.15,GOLD)
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
camera = stage(rural,(0,0,.1),10.4,PREVIEW/'Cereal_caminos_naturales_v02.png')
caption(camera,'FULGOR MEDIEVAL / PAISAJE RURAL',-4.62,2.60,.17,GOLD)
caption(camera,'COSECHAS Y CAMINOS',-4.62,2.10,.37)
caption(camera,'Revision V02 / cosecha densa, grava y rodadas con relieve',-4.62,1.72,.15,GOLD)
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
camera = stage(patch,(.15,-.40,.10),11.6,PREVIEW/'Paisaje_rural_natural_v02.png')
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
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Agua_cereal_caminos_natural_v02.blend'))
(PREVIEW/'Agua_cereal_caminos_natural_v02_metrics.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_WATER_RURAL_COMPLETE',json.dumps(reports))
