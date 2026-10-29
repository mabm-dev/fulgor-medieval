"""Create a connected eroded ridge, bake its own textures, export and render.

Original geometry and materials, no downloads. Run in a fresh Blender process.
"""
from pathlib import Path
import json
import math
import random

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
EXPORT = ROOT/'Assets/Fulgor/Art/Terrain/MontanaNaturalV02'
TEXTURES = EXPORT/'Textures'
PREVIEW = ROOT/'Builds/ArtPreview'
for folder in (EXPORT,TEXTURES,PREVIEW):
    folder.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj,do_unlink=True)
scene = bpy.context.scene
scene.name = 'Montana_Natural_Comparacion'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1


def smoothstep(value):
    value = max(0,min(1,value))
    return value*value*(3-2*value)


SPINE = ((-.59,-.12,.20,.21),(-.34,.03,.58,.25),(-.13,.23,.90,.23),
         (.09,.12,1.03,.20),(.31,.32,.64,.22),(.54,.30,.22,.18))
SPURS = (((-.34,.03,.58,.15),(-.43,-.49,.10,.14)),
         ((.09,.12,.97,.15),(.47,-.42,.12,.16)),
         ((-.13,.23,.84,.15),(-.35,.59,.10,.15)))


def ridge(x,y,a,b):
    dx,dy = b[0]-a[0],b[1]-a[1]
    t = max(0,min(1,((x-a[0])*dx+(y-a[1])*dy)/(dx*dx+dy*dy)))
    px,py = a[0]+t*dx,a[1]+t*dy
    width,height = a[3]+t*(b[3]-a[3]),a[2]+t*(b[2]-a[2])
    distance = math.hypot(x-px,y-py)
    return height*math.exp(-(distance/width)**1.5)


def elevation(x,y):
    # Directional connected ridges, narrow spurs, broad foothills and runoff cuts.
    wx = x+.027*math.sin(13*y+1.7*math.sin(9*x))
    wy = y+.024*math.sin(12*x-1.5*math.cos(8*y))
    value = max(ridge(wx,wy,a,b) for a,b in (*zip(SPINE,SPINE[1:]),*SPURS))
    noise = (.022*math.sin(31*x+2*math.sin(17*y))*math.cos(27*y-4*x)
             +.012*math.sin(61*x+29*y))
    runoff = .042*abs(math.sin(18*x+4*math.sin(8*y)))*abs(math.sin(13*y-3*x))
    edge = max(abs(x)/(.97*math.sqrt(3)/2),
               (abs(x)+math.sqrt(3)*abs(y))/(math.sqrt(3)*.97))
    taper = smoothstep((1-edge)/.32)
    return .42+max(0,value+noise-runoff)*taper


def tint(x,y,z):
    eps = .006
    slope = math.hypot((elevation(x+eps,y)-elevation(x-eps,y))/(2*eps),
                       (elevation(x,y+eps)-elevation(x,y-eps))/(2*eps))
    rock = smoothstep((z-.44)*3.8+slope*.36-.12)
    # Warm geological tones, with grass restricted to gentle foothills.
    light = .5+.5*math.sin(z*17+x*2.3)
    stone = (.31+.12*light,.245+.09*light,.17+.07*light)
    green = (.13,.36,.025)
    return tuple(green[i]*(1-rock)+stone[i]*rock for i in range(3))


N,RINGS = 96,28


def boundary(i):
    sector,fraction = divmod(i, N//6)
    t = fraction/(N//6)
    a,b = math.radians(30+60*sector),math.radians(90+60*sector)
    return (.97*((1-t)*math.cos(a)+t*math.cos(b)),
            .97*((1-t)*math.sin(a)+t*math.sin(b)))


vertices = [(0,0,elevation(0,0))]
for ring in range(1,RINGS+1):
    for i in range(N):
        x,y = boundary(i)
        x,y = x*ring/RINGS,y*ring/RINGS
        vertices.append((x,y,elevation(x,y)))
faces = []
for i in range(N):
    faces.append((0,1+i,1+(i+1)%N))
for ring in range(RINGS-1):
    a,b = 1+ring*N,1+(ring+1)*N
    for i in range(N):
        j = (i+1)%N
        if i%2:
            faces.extend(((a+i,b+i,a+j),(a+j,b+i,b+j)))
        else:
            faces.extend(((a+i,b+i,b+j),(a+i,b+j,a+j)))
top_faces = len(faces)
bottom_start = len(vertices)
vertices += [(*boundary(i),0) for i in range(N)]
outer_start = 1+(RINGS-1)*N
for i in range(N):
    j = (i+1)%N
    faces.append((outer_start+i,bottom_start+i,bottom_start+j,outer_start+j))
faces.append(tuple(range(bottom_start+N-1,bottom_start-1,-1)))
mesh = bpy.data.meshes.new('SM_MontanaNatural_A_LOD0')
mesh.from_pydata(vertices,[],faces)
mesh.update()
mountain = bpy.data.objects.new(mesh.name,mesh)
scene.collection.objects.link(mountain)
uv = mesh.uv_layers.new(name='UV_Atlas')
colors = mesh.color_attributes.new(name='RockTint',type='FLOAT_COLOR',domain='CORNER')
for face in mesh.polygons:
    face.use_smooth = face.index < top_faces
    for corner,index in enumerate(face.loop_indices):
        p = mesh.vertices[mesh.loops[index].vertex_index].co
        if face.index < top_faces:
            uv.data[index].uv = (.02+.96*(p.x+1)/2,.22+.76*(p.y+1)/2)
            color = tint(*p)
        elif face.index < len(mesh.polygons)-1:
            side = face.index-top_faces
            # Per-face seam unwrap prevents the final edge from crossing the atlas.
            u = (side+(1 if corner in (2,3) else 0))/N
            uv.data[index].uv = (.02+.96*u,.02+.16*p.z/.42)
            color = (.25,.105,.038)
        else:
            uv.data[index].uv = (.5,.008)
            color = (.15,.075,.03)
        colors.data[index].color = (*color,1)

mat = bpy.data.materials.new('M_MontanaNatural_ProceduralSource')
mat.use_nodes = True
nodes,links = mat.node_tree.nodes,mat.node_tree.links
shader = nodes.get('Principled BSDF')
shader.inputs['Roughness'].default_value = .92
attribute = nodes.new('ShaderNodeVertexColor')
attribute.layer_name = 'RockTint'
geometry = nodes.new('ShaderNodeNewGeometry')
noise = nodes.new('ShaderNodeTexNoise')
noise.inputs['Scale'].default_value = 72
noise.inputs['Detail'].default_value = 3
links.new(geometry.outputs['Position'],noise.inputs['Vector'])
tones = nodes.new('ShaderNodeValToRGB')
tones.color_ramp.elements[0].position = .22
tones.color_ramp.elements[0].color = (.40,.34,.27,1)
tones.color_ramp.elements[1].position = .78
tones.color_ramp.elements[1].color = (1,1,.92,1)
links.new(noise.outputs['Fac'],tones.inputs['Fac'])
grain = nodes.new('ShaderNodeMixRGB')
grain.blend_type = 'MULTIPLY'
grain.inputs[0].default_value = .55
links.new(attribute.outputs['Color'],grain.inputs[1])
links.new(tones.outputs['Color'],grain.inputs[2])
links.new(grain.outputs['Color'],shader.inputs['Base Color'])
wave = nodes.new('ShaderNodeTexWave')
wave.bands_direction = 'Z'
wave.inputs['Scale'].default_value = 16
wave.inputs['Distortion'].default_value = 2.3
links.new(geometry.outputs['Position'],wave.inputs['Vector'])
heightmix = nodes.new('ShaderNodeMixRGB')
heightmix.inputs[0].default_value = .23
links.new(noise.outputs['Fac'],heightmix.inputs[1])
links.new(wave.outputs['Fac'],heightmix.inputs[2])
bump = nodes.new('ShaderNodeBump')
bump.inputs['Strength'].default_value = .35
bump.inputs['Distance'].default_value = .009
links.new(heightmix.outputs['Color'],bump.inputs['Height'])
links.new(bump.outputs['Normal'],shader.inputs['Normal'])
mesh.materials.append(mat)

# Bake the shader to portable textures; renders below use only the baked material.
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.render.bake.margin = 6
bpy.ops.object.select_all(action='DESELECT')
mountain.select_set(True)
bpy.context.view_layer.objects.active = mountain
images = {}
for kind,suffix in (('DIFFUSE','BaseColor'),('NORMAL','Normal')):
    image = bpy.data.images.new('T_MontanaNatural_'+suffix,1024,1024,alpha=False)
    if kind == 'NORMAL':
        image.colorspace_settings.name = 'Non-Color'
    texture = nodes.new('ShaderNodeTexImage')
    texture.image = image
    nodes.active = texture
    if kind == 'DIFFUSE':
        bpy.ops.object.bake(type=kind,pass_filter={'COLOR'},use_clear=True)
    else:
        bpy.ops.object.bake(type=kind,normal_space='TANGENT',use_clear=True)
    image.filepath_raw = str(TEXTURES/(image.name+'.png'))
    image.file_format = 'PNG'
    image.save()
    images[suffix] = image

baked = bpy.data.materials.new('M_MontanaNatural_A')
baked.use_nodes = True
nodes,links = baked.node_tree.nodes,baked.node_tree.links
shader = nodes.get('Principled BSDF')
shader.inputs['Roughness'].default_value = .92
basecolor = nodes.new('ShaderNodeTexImage')
basecolor.image = images['BaseColor']
links.new(basecolor.outputs['Color'],shader.inputs['Base Color'])
normaltex = nodes.new('ShaderNodeTexImage')
normaltex.image = images['Normal']
normal = nodes.new('ShaderNodeNormalMap')
normal.inputs['Strength'].default_value = .6
links.new(normaltex.outputs['Color'],normal.inputs['Color'])
links.new(normal.outputs['Normal'],shader.inputs['Normal'])
mesh.materials.clear()
mesh.materials.append(baked)
mesh.calc_loop_triangles()
points = [v.co for v in mesh.vertices]
assert all(abs(p.x) <= .97*math.sqrt(3)/2+.0001 and
           abs(p.x)+math.sqrt(3)*abs(p.y) <= math.sqrt(3)*.97+.0001 for p in points)
assert min(p.z for p in points) >= -.0001
report = {'name':mountain.name,'triangles':len(mesh.loop_triangles),'vertices':len(points),
          'materials':1,'textures':'BaseColor + tangent normal, 1024x1024',
          'bounds':[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)],
          'origin':list(mountain.location),'scale':list(mountain.scale)}
bpy.ops.export_scene.fbx(filepath=str(EXPORT/(mountain.name+'.fbx')),use_selection=True,
                         object_types={'MESH'},global_scale=1,apply_unit_scale=True,
                         apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                         add_leaf_bones=False,bake_anim=False,use_triangles=True,
                         path_mode='RELATIVE')


def plain_material(name,color,emission=False):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color,1)
    result.use_nodes = True
    p = result.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = .9
    if emission:
        p.inputs['Emission Color'].default_value = (*color,1)
        p.inputs['Emission Strength'].default_value = 1
    return result


ink = plain_material('M_Preview_Ivory',(.81,.78,.65),True)
gold = plain_material('M_Preview_Gold',(.66,.41,.16),True)
floor_mat = plain_material('M_Preview_Background',(.042,.026,.014))


def stage(target_scene,target,ortho,output):
    bpy.context.window.scene = target_scene
    target_scene.unit_settings.system = 'METRIC'
    target_scene.unit_settings.scale_length = 1
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015))
    bpy.context.object.name = 'PREVIEW_Suelo'
    bpy.context.object.data.materials.append(floor_mat)
    bpy.ops.object.camera_add(location=Vector(target)+Vector((0,-7,8)))
    camera = bpy.context.object
    camera.rotation_euler = (Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type,camera.data.ortho_scale = 'ORTHO',ortho
    target_scene.camera = camera
    for location,power,size in (((-3,-4,7),700,5),((4,2,5),160,4)):
        bpy.ops.object.light_add(type='AREA',location=location)
        light = bpy.context.object
        light.data.energy,light.data.size = power,size
        light.data.color = (1,.94,.84)
        light.rotation_euler = (-light.location).to_track_quat('-Z','Y').to_euler()
    target_scene.world = bpy.data.worlds.new(target_scene.name+'_World')
    target_scene.world.color = (.16,.16,.16)
    target_scene.render.engine = 'CYCLES'
    target_scene.cycles.samples = 48
    target_scene.cycles.use_denoising = True
    target_scene.render.resolution_x,target_scene.render.resolution_y = 1600,1000
    target_scene.render.resolution_percentage = 100
    target_scene.render.image_settings.file_format = 'PNG'
    target_scene.render.filepath = str(PREVIEW/output)
    target_scene.view_settings.view_transform,target_scene.view_settings.exposure = 'Standard',-.4
    return camera


def caption(camera,text,x,y,size,mat=ink,align='LEFT'):
    curve = bpy.data.curves.new('Preview_Text',type='FONT')
    curve.body,curve.size,curve.align_x = text,size,align
    obj = bpy.data.objects.new('PREVIEW_'+text[:16],curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent,obj.location = camera,(x,y,-7)
    curve.materials.append(mat)


with bpy.data.libraries.load(str(SOURCE/'Biomas_color_v01.blend'),link=False) as (_,loaded):
    loaded.objects = ['SM_Montana_A_LOD0','SM_Bosque_A_LOD0','SM_Colina_A_LOD0']
old,forest,hill = loaded.objects
scene.collection.objects.link(old)
old.location,mountain.location = (-1.8,0,0),(1.8,0,0)
camera = stage(scene,(0,0,.2),8.4,'Montana_comparacion_v02.png')
caption(camera,'FULGOR MEDIEVAL / EVOLUCION DEL TERRENO',-3.72,2.10,.15,gold)
caption(camera,'UNA SIERRA MAS NATURAL',-3.72,1.70,.32)
caption(camera,'Crestas conectadas, erosion y roca estratificada / materiales propios',-3.72,1.38,.13,gold)
caption(camera,'ANTES / V01',-1.8,-1.35,.20,align='CENTER')
caption(camera,'AHORA / V02',1.8,-1.35,.20,align='CENTER')
caption(camera,'Conos y bloques regulares',-1.8,-1.65,.13,gold,'CENTER')
caption(camera,'Relieve continuo y textura de roca',1.8,-1.65,.13,gold,'CENTER')
caption(camera,'PROTOTIPO  /  Misma escala  /  Texturas propias 1024 px  /  Pendiente de Unity',
        -3.72,-2.20,.12,gold)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Montana_Natural_Conjunto')
for model,location in ((forest,(-math.sqrt(3),0,0)),(hill,(math.sqrt(3),0,0))):
    patch.collection.objects.link(model)
    model.location = location
copy = mountain.copy()
copy.location = (math.sqrt(3)/2,1.5,0)
patch.collection.objects.link(copy)
with bpy.data.libraries.load(str(SOURCE/'Llanura_v02_color.blend'),link=False) as (_,loaded):
    loaded.objects = ['SM_Llanura_A_LOD0']
plain = loaded.objects[0]
plain.location = (0,0,0)
patch.collection.objects.link(plain)
camera = stage(patch,(0,.6,.10),7.8,'Montana_conjunto_v02.png')
caption(camera,'FULGOR MEDIEVAL',-3.45,1.93,.16,gold)
caption(camera,'RELIEVE CON CARACTER',-3.45,1.54,.29)
caption(camera,'Montana natural v02 / primer estudio con los otros terrenos',-3.45,1.23,.12,gold)
caption(camera,'PALETA CALIDA  /  Atlas propio  /  Integracion y transiciones pendientes',
        -3.45,-1.98,.11,gold)
bpy.ops.render.render(write_still=True)
for image in images.values():
    image.pack()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
            area.spaces.active.shading.color_type = 'MATERIAL'
bpy.context.window.scene = scene
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Montana_natural_v02.blend'))
(PREVIEW/'Montana_natural_v02_metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FULGOR_MOUNTAIN_COMPLETE',json.dumps(report))
