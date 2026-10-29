"""Original blue water kit with explicit edge profiles and full-size hexes."""
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
                                 bake_export,stage,caption)

ROOT = SOURCE.parents[2]
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/ConexionesAguaV03'
PREVIEW = ROOT/'Builds/ArtPreview'
PREVIEW.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Conexiones_Agua_Galeria'
R = math.sqrt(3)/2
WIDTH = .145
SHALLOW,DEEP = (.025,.28,.62),(.006,.065,.32)
NORMALS = [(math.cos(math.radians(i*60)),math.sin(math.radians(i*60))) for i in range(6)]
# Faces counterclockwise: E, NE, NW, W, SW, SE. +/- shore is signed tangent.
PROFILES = {
    'Mar': ['sea']*6,
    'RioRecto': ['river','land','land','river','land','land'],
    'RioCurva': ['land','river','land','river','land','land'],
    'Desembocadura': ['river','land','shore-','sea','shore+','land'],
    'CostaRecta': ['shore+','land','land','shore-','sea','sea'],
    'Cabo': ['sea','shore+','land','shore-','sea','sea'],
    'Bahia': ['land','shore-','sea','shore+','land','land'],
}


def smooth(t):
    t = min(1,max(0,t))
    return t*t*(3-2*t)


def blend(a,b,t):
    return tuple(x*(1-t)+y*t for x,y in zip(a,b))


def edge_info(x,y):
    i = max(range(6),key=lambda i:x*NORMALS[i][0]+y*NORMALS[i][1])
    nx,ny = NORMALS[i]
    return i,max(0,R-x*nx-y*ny),-x*ny+y*nx


def profile_distance(profile,t):
    if profile == 'sea': return -1
    if profile == 'land': return 1
    if profile == 'river': return abs(t)-WIDTH
    return t if profile == 'shore+' else -t


curve = []
for i in range(81):
    t = i/80
    a,b,c,d = Vector((-R,0)),Vector((-.34,0)),Vector((R*.5-.23,R*math.sqrt(3)/2-.23*math.sqrt(3))),Vector((R*.5,R*math.sqrt(3)/2))
    curve.append((1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d)


def curve_distance(x,y,signed=False):
    p = Vector((x,y))
    best,result = 1e9,0
    for a,b in zip(curve,curve[1:]):
        ab = b-a
        t = max(0,min(1,(p-a).dot(ab)/ab.length_squared))
        delta = p-(a+t*ab)
        dist = delta.length
        if dist < best:
            best = dist
            result = dist if ab.x*delta.y-ab.y*delta.x >= 0 else -dist
    return result if signed else best


def raw_distance(kind,x,y):
    if kind == 'Mar': return -1
    if kind == 'RioRecto':
        return abs(y-.035*math.sin(math.pi*x/R)*math.sin(math.pi*(x+R)/(2*R))**2)-WIDTH
    if kind == 'RioCurva': return curve_distance(x,y)-WIDTH
    if kind == 'CostaRecta': return y-.035*math.sin(math.pi*x/R)**2
    if kind in ('Cabo','Bahia'):
        d = curve_distance(x,y,True)
        return d if kind == 'Cabo' else -d
    # Rounded union of the sea and a widening river; untouched at edge ports.
    a,b = x+R/2,abs(y)-(WIDTH+.10*smooth((.25-x)/.65))
    k = .16
    h = max(k-abs(a-b),0)/k
    return min(a,b)-h*h*k*.25


def distance(kind,x,y):
    # Blend all nearby faces to avoid Voronoi lines where two edges are equidistant.
    total,edge,weight = 0,0,0
    for i,(nx,ny) in enumerate(NORMALS):
        w = smooth((.22-max(0,R-x*nx-y*ny))/.22)
        edge += w*profile_distance(PROFILES[kind][i],-x*ny+y*nx)
        total += w
        weight = max(weight,w)
    raw = raw_distance(kind,x,y)
    return raw*(1-weight)+(edge/total if total else raw)*weight


def height(kind,x,y):
    d = distance(kind,x,y)
    detail = smooth(edge_info(x,y)[1]/.18)
    water = .04+detail*.0012*math.sin(37*x+23*y+math.sin(9*y))
    land = .12+detail*.005*math.sin(17*x+7*y)*math.cos(13*y)
    return water*(1-smooth((d+.025)/.12))+land*smooth((d+.025)/.12)


def color(kind,p):
    x,y,_ = p
    d = distance(kind,x,y)
    detail = smooth(edge_info(x,y)[1]/.18)
    if d < -.04:
        base = blend(SHALLOW,DEEP,smooth((-d-.04)/.40))
        return tuple(c*(1-detail*.025*(1-math.sin(51*x+23*y))) for c in base)
    if d < .012: return blend(SHALLOW,(.29,.22,.11),smooth((d+.04)/.052))
    if d < .09: return blend((.29,.22,.11),(.59,.41,.18),smooth((d-.012)/.078))
    grass_edge = .09+detail*.018*math.sin(17*x+11*y)
    return blend((.59,.41,.18),(.13,.36,.025),smooth((d-grass_edge)/.07))


mat = source_material('M_ConexionesAgua_Fuente',130,.0025,.3)
nodes,links = mat.node_tree.nodes,mat.node_tree.links
shader = nodes.get('Principled BSDF')
mask = nodes.new('ShaderNodeVertexColor')
mask.layer_name = 'WaterMask'
rough = nodes.new('ShaderNodeMapRange')
rough.inputs['To Min'].default_value,rough.inputs['To Max'].default_value = .90,.18
links.new(mask.outputs['Color'],rough.inputs['Value'])
links.new(rough.outputs['Result'],shader.inputs['Roughness'])
detail_attr = nodes.new('ShaderNodeVertexColor')
detail_attr.layer_name = 'EdgeDetail'
links.new(detail_attr.outputs['Color'],next(n for n in nodes if n.type == 'MIX_RGB').inputs[0])
links.new(detail_attr.outputs['Color'],next(n for n in nodes if n.type == 'BUMP').inputs['Strength'])
rockmat = source_material('M_OrillaPiedra_Fuente',80,.003,.3)
models,reports = {},[]
for kind in PROFILES:
    obj = terrain('SM_Agua_'+kind+'_LOD0',lambda x,y:height(kind,x,y),
                  lambda p:color(kind,p),mat,count=120,rings=32,radius=1)
    for name in ('WaterMask','EdgeDetail'):
        attr = obj.data.color_attributes.new(name=name,type='FLOAT_COLOR',domain='CORNER')
        for face in obj.data.polygons:
            for index in face.loop_indices:
                p = obj.data.vertices[obj.data.loops[index].vertex_index].co
                value = (1-smooth((distance(kind,p.x,p.y)+.045)/.055)) if name == 'WaterMask' else .30*smooth(edge_info(p.x,p.y)[1]/.18)
                value = value if face.use_smooth else 0
                attr.data[index].color = (value,value,value,1)
    parts = [obj]
    rng = random.Random(730+sum(map(ord,kind)))
    if kind != 'Mar':
        candidates = []
        for attempt in range(1600):
            x,y = rng.uniform(-.65,.65),rng.uniform(-.70,.70)
            if edge_info(x,y)[1] > .20 and .06 < distance(kind,x,y) < .14:
                candidates.append((x,y))
        for x,y in rng.sample(candidates,min(8,len(candidates))):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,height(kind,x,y)+.01))
            stone = bpy.context.object
            s = rng.uniform(.014,.032)
            stone.scale = (s*1.5,s,s*.6)
            stone.rotation_euler.z = rng.uniform(0,math.tau)
            tint_object(stone,lambda p:(.52,.42,.25),rockmat)
            parts.append(stone)
    if len(parts)>1: obj = join(parts,obj.name)
    report = bake_export(obj,EXPORT,1024,True,radius=1)
    report.update(edge_profiles=PROFILES[kind],radius=1,static_water=True)
    reports.append(report)
    models[kind] = obj
    scene.collection.objects.unlink(obj)


GOLD = (.66,.41,.16)
positions = [(-3.45,1.4),(-1.15,1.4),(1.15,1.4),(3.45,1.4),(-2.3,-1.4),(0,-1.4),(2.3,-1.4)]
labels = ['MAR','RIO RECTO','RIO CURVO','DESEMBOCADURA','COSTA','CABO','BAHIA']
for (kind,model),(x,y),label in zip(models.items(),positions,labels):
    scene.collection.objects.link(model)
    model.location = (x,y,0)
camera = stage(scene,(0,0,.10),11.2,PREVIEW/'Conexiones_agua_v03.png')
scene.render.resolution_y = 1250
caption(camera,'FULGOR MEDIEVAL / KIT DE AGUA AZUL',-5,3.85,.18,GOLD)
caption(camera,'RIOS QUE LLEGAN AL MAR',-5,3.28,.36)
caption(camera,'Curvas, desembocaduras, cabos y bahias / bordes completos',-5,2.87,.15,GOLD)
for (x,y),label in zip(positions,labels):
    # Project the tile centers to the camera before adding screen-aligned text.
    local = camera.matrix_world.inverted() @ Vector((x,y,0))
    caption(camera,label,local.x,local.y-1.00,.16,align='CENTER')
caption(camera,'ESTUDIO V03  /  Agua estatica  /  Materiales propios  /  Pendiente de Unity',-5,-4.05,.12,GOLD)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Desembocadura_Conjunto')
placements = []
def place(kind,q,r,angle=0):
    model = models[kind].copy()
    model.location = (math.sqrt(3)*(q+r/2),1.5*r,0)
    model.rotation_euler.z = math.radians(angle)
    patch.collection.objects.link(model)
    placements.append(dict(kind=kind,q=q,r=r,rotation=angle))

place('Desembocadura',0,0)
place('Mar',-1,0)
place('Mar',-2,0)
place('Mar',-2,1)
place('Mar',-1,-1)
place('CostaRecta',-1,1,300)
place('CostaRecta',0,-1,240)
place('RioRecto',1,0)
place('RioCurva',2,0)
place('RioRecto',2,1,60)
camera = stage(patch,(.2,.4,.10),12.0,PREVIEW/'Desembocadura_conjunto_v03.png')
patch.render.resolution_y = 1200
caption(camera,'FULGOR MEDIEVAL / CONTINUIDAD DEL PAISAJE',-5.3,3.9,.17,GOLD)
caption(camera,'DEL CAUCE A LA COSTA',-5.3,3.35,.36)
caption(camera,'Agua azul / hexagonos sin separacion / relieve enlazado',-5.3,2.94,.15,GOLD)
caption(camera,'PROTOTIPO  /  Sin integrar en Unity  /  LOD y materiales URP pendientes',-5.3,-4.05,.12,GOLD)
bpy.ops.render.render(write_still=True)

# Validate the actual placed neighbors at sample points on shared full-size edges.
axial = [(1,0),(0,1),(-1,1),(-1,0),(0,-1),(1,-1)]
lookup = {(p['q'],p['r']):p for p in placements}
seams = []
def local_point(p,x,y):
    a = math.radians(-p['rotation'])
    return math.cos(a)*x-math.sin(a)*y,math.sin(a)*x+math.cos(a)*y
for p in placements:
    for i,(dq,dr) in enumerate(axial):
        other = lookup.get((p['q']+dq,p['r']+dr))
        if not other or (p['q'],p['r']) >= (other['q'],other['r']): continue
        nx,ny = NORMALS[i]
        height_error,color_error = 0,0
        for j in range(41):
            t = -.5+j/40
            x,y = R*nx-t*ny,R*ny+t*nx
            a = local_point(p,x,y)
            b = local_point(other,x-2*R*nx,y-2*R*ny)
            height_error = max(height_error,abs(height(p['kind'],*a)-height(other['kind'],*b)))
            color_error = max(color_error,max(abs(v-w) for v,w in zip(color(p['kind'],(*a,0)),color(other['kind'],(*b,0)))))
        seams.append(dict(a=[p['q'],p['r']],b=[other['q'],other['r']],height_error=height_error,color_error=color_error))
        assert height_error < 1e-6 and color_error < 1e-6, (p,other,height_error,color_error)

bpy.context.window.scene = patch
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D': area.spaces.active.region_3d.view_perspective = 'CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Conexiones_agua_v03.blend'))
(PREVIEW/'Conexiones_agua_v03_metrics.json').write_text(json.dumps(dict(models=reports,placements=placements,seams=seams),indent=2),encoding='utf-8')
print('FULGOR_CONNECTIONS_COMPLETE',len(reports),'models;',len(seams),'neighbor profiles checked')
