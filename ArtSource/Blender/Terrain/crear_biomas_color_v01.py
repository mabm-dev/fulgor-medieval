"""Original warm terrain studies. Run in a fresh background Blender process."""
from pathlib import Path
import json
import math
import random

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
EXPORT = ROOT / 'Assets/Fulgor/Art/Terrain/BiomasColorV01'
PREVIEW = ROOT / 'Builds/ArtPreview'
for folder in (EXPORT, PREVIEW):
    folder.mkdir(parents=True, exist_ok=True)
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

gallery = bpy.context.scene
gallery.name = 'Biomas_Color_Galeria'
gallery.unit_settings.system = 'METRIC'
gallery.unit_settings.scale_length = 1


def material(name, color, emission=False):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = 0.9
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = 1
    return mat


PALETTE = [material(name, color) for name, color in (
    ('M_Bioma_Prado', (0.12,0.40,0.022)),
    ('M_Bioma_Verdesol', (0.24,0.49,0.035)),
    ('M_Bioma_Hierba', (0.075,0.30,0.025)),
    ('M_Bioma_Arcilla', (0.36,0.105,0.035)),
    ('M_Bioma_CopaOscura', (0.025,0.20,0.035)),
    ('M_Bioma_CopaVerde', (0.055,0.34,0.018)),
    ('M_Bioma_CopaLima', (0.19,0.46,0.020)),
    ('M_Bioma_Otono', (0.78,0.29,0.018)),
    ('M_Bioma_Caliza', (0.73,0.58,0.34)),
    ('M_Bioma_RocaOcre', (0.43,0.25,0.095)),
    ('M_Bioma_RocaCobre', (0.38,0.145,0.055)),
    ('M_Bioma_RocaLuz', (0.62,0.40,0.18)),
)]


def mesh_object(name, vertices, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for mat in PALETTE:
        mesh.materials.append(mat)
    for polygon, index in zip(mesh.polygons, indices):
        polygon.material_index = index
    return obj


def hex_point(i):
    a = math.radians(30+60*i)
    return 0.97*math.cos(a), 0.97*math.sin(a)


def base(name, height, forest=False):
    vertices = [(0,0,height)] + [(*hex_point(i),height) for i in range(6)]
    vertices += [(*hex_point(i),0) for i in range(6)]
    faces, indices = [], []
    for i in range(6):
        j = (i+1) % 6
        faces.extend(((0,1+i,1+j), (1+i,7+i,7+j,1+j)))
        indices.extend(((2 if i%2 else 0) if forest else i%3, 3))
    faces.append(tuple(range(12,6,-1)))
    indices.append(3)
    return mesh_object(name, vertices, faces, indices)


def ico(name, location, scale, tones, seed):
    rng = random.Random(seed)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    for mat in PALETTE:
        obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.material_index = rng.choice(tones)
    return obj


def tree(x, y, size, seed, autumn=False):
    parts = []
    h = 0.18
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.035*size, radius2=0.024*size,
                                  depth=0.30*size, location=(x,y,h+0.15*size))
    trunk = bpy.context.object
    trunk.name = 'Tronco'
    trunk.data.materials.append(PALETTE[3])
    parts.append(trunk)
    tones = (7,7,11) if autumn else (4,5,5,6)
    for i, (dx,dy,z,r) in enumerate(((-0.07,0,0.35,0.20), (0.07,0.02,0.38,0.20),
                                    (0,0.02,0.54,0.18))):
        parts.append(ico('Copa', (x+dx*size,y+dy*size,h+z*size),
                         (r*size,r*size,r*1.05*size), tones, seed+i))
    return parts


def mound(name, x, y, radius, rise, height, seed, mountain=False):
    rng = random.Random(seed)
    count = 9 if mountain else 12
    vertices = []
    for ring, z in ((1, height+0.001), (0.56, height+rise*0.47),
                    (0.24, height+rise*0.78)):
        for i in range(count):
            a = math.tau*i/count
            r = radius*ring*rng.uniform(0.92,1.04)
            vertices.append((x+math.cos(a)*r, y+math.sin(a)*r, z+rng.uniform(0,0.015)))
    vertices.append((x+radius*0.07,y-radius*0.02,height+rise))
    faces, indices = [], []
    for ring in range(2):
        for i in range(count):
            j = (i+1) % count
            faces.extend(((ring*count+i,ring*count+j,(ring+1)*count+j),
                          (ring*count+i,(ring+1)*count+j,(ring+1)*count+i)))
            if mountain:
                tone = (9,10,11)[(i+ring)%3]
            else:
                tone = 9 if i in (7,8,9) else (0,1,2)[(i//3+ring)%3]
            indices.extend((tone,tone))
    tip = len(vertices)-1
    for i in range(count):
        faces.append((2*count+i,2*count+(i+1)%count,tip))
        indices.append(8 if mountain else 1)
    return mesh_object(name, vertices, faces, indices)


def join(parts, name):
    bpy.ops.object.select_all(action='DESELECT')
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    uv = obj.data.uv_layers.new(name='UV_Planar_Provisional')
    for face in obj.data.polygons:
        for index in face.loop_indices:
            p = obj.data.vertices[obj.data.loops[index].vertex_index].co
            uv.data[index].uv = ((p.x+1)*0.5,(p.y+1)*0.5)
    return obj


parts = [base('Bosque',0.18,True)]
for i, (x,y,size) in enumerate(((-0.34,-0.04,0.9),(0.32,-0.10,0.85),
                               (-0.16,0.35,1.0),(0.33,0.37,0.75),(0.04,-0.42,0.66))):
    parts.extend(tree(x,y,size,100+i*5,autumn=i==4))
forest = join(parts,'SM_Bosque_A_LOD0')

parts = [base('Colina',0.28)]
for i,(x,y,r,rise) in enumerate(((-0.23,0.12,0.49,0.32),(0.30,0.15,0.38,0.22),
                                (0.12,-0.37,0.29,0.14))):
    parts.append(mound('Loma',x,y,r,rise,0.28,300+i))
for i,(x,y) in enumerate(((-0.45,-0.32),(0.51,-0.13))):
    parts.append(ico('Caliza',(x,y,0.31),(0.05,0.045,0.04),(8,9),400+i))
hill = join(parts,'SM_Colina_A_LOD0')

parts = [base('Montana',0.42)]
for i,(x,y,r,rise) in enumerate(((0.12,0.18,0.45,0.86),(-0.34,-0.10,0.34,0.55),
                                (0.40,-0.29,0.24,0.39))):
    parts.append(mound('Pico',x,y,r,rise,0.42,500+i,True))
for i,(x,y) in enumerate(((-0.48,0.28),(0.18,-0.45),(-0.13,-0.50))):
    parts.append(ico('Pedrera',(x,y,0.45),(0.07,0.05,0.055),(8,9,11),600+i))
mountain = join(parts,'SM_Montana_A_LOD0')

models = [forest,hill,mountain]
report = []
for obj in models:
    points = [v.co for v in obj.data.vertices]
    assert all(abs(p.x) <= 0.97*math.sqrt(3)/2+0.0001 and
               abs(p.x)+math.sqrt(3)*abs(p.y) <= math.sqrt(3)*0.97+0.0001 for p in points)
    assert min(p.z for p in points) >= -0.0001
    assert tuple(obj.location) == (0,0,0) and tuple(obj.scale) == (1,1,1)
    obj.data.calc_loop_triangles()
    report.append({'name':obj.name,'triangles':len(obj.data.loop_triangles),
                   'vertices':len(points), 'materials':len(obj.data.materials),
                   'bounds':[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)],
                   'origin':list(obj.location),'scale':list(obj.scale)})
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(obj.name+'.fbx')),use_selection=True,
                             object_types={'MESH'},global_scale=1,apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                             add_leaf_bones=False,bake_anim=False,use_triangles=True)

ink = material('M_Preview_Marfil',(0.81,0.78,0.65),True)
gold = material('M_Preview_Oro',(0.66,0.41,0.16),True)
floor_mat = material('M_Preview_Fondo',(0.042,0.026,0.014))


def stage(scene, target, ortho, output):
    bpy.context.window.scene = scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-0.015))
    bpy.context.object.name = 'PREVIEW_Suelo'
    bpy.context.object.data.materials.append(floor_mat)
    bpy.ops.object.camera_add(location=Vector(target)+Vector((0,-8,10)))
    camera = bpy.context.object
    camera.name = 'PREVIEW_Camera'
    camera.rotation_euler = (Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type, camera.data.ortho_scale = 'ORTHO', ortho
    scene.camera = camera
    for location,power,size in (((-3,-4,7),650,5),((4,2,5),180,4)):
        bpy.ops.object.light_add(type='AREA',location=location)
        obj = bpy.context.object
        obj.name = 'PREVIEW_Luz'
        obj.data.energy,obj.data.shape,obj.data.size = power,'DISK',size
        obj.data.color = (1,0.92,0.78)
        obj.rotation_euler = (-obj.location).to_track_quat('-Z','Y').to_euler()
    scene.world = bpy.data.worlds.new(scene.name+'_World')
    scene.world.color = (0.16,0.16,0.16)
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x,scene.render.resolution_y = 1600,1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW/output)
    scene.view_settings.view_transform,scene.view_settings.exposure = 'Standard',-0.4
    return camera


def caption(camera,text,x,y,size,mat=ink,align='LEFT'):
    curve = bpy.data.curves.new('Preview_Text',type='FONT')
    curve.body,curve.size,curve.align_x = text,size,align
    obj = bpy.data.objects.new('PREVIEW_'+text[:20],curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent,obj.location = camera,(x,y,-7)
    curve.materials.append(mat)


for model,x in zip(models,(-2.65,0,2.65)):
    model.location.x = x
camera = stage(gallery,(0,0,0.15),9.4,'Biomas_color_v01.png')
caption(camera,'FULGOR MEDIEVAL',-4.15,2.28,0.19,gold)
caption(camera,'BOSQUES, COLINAS Y CUMBRES',-4.15,1.82,0.32)
caption(camera,'Primer prototipo / siluetas distintas, una misma paleta calida',-4.15,1.49,0.15,gold)
for x,title,sub in zip((-2.65,0,2.65),('04 / BOSQUE','05 / COLINA','06 / MONTANA'),
                        ('Copas verdes y acentos de otono','Lomas verdes y afloramientos ocres',
                         'Roca cobre, ocre y cumbres calizas')):
    caption(camera,title,x,-1.28,0.19,align='CENTER')
    caption(camera,sub,x,-1.60,0.12,gold,'CENTER')
caption(camera,'PROPUESTA V01 COLOR  /  Geometria propia  /  Pendiente de integrar en Unity',
        -4.15,-2.40,0.13,gold)
bpy.ops.render.render(write_still=True)

patch = bpy.data.scenes.new('Biomas_Color_Conjunto')
placements = ((forest,(-math.sqrt(3),0,0)), (hill,(math.sqrt(3),0,0)),
              (mountain,(math.sqrt(3)/2,1.5,0)))
for model,location in placements:
    copy = model.copy()
    copy.location = location
    patch.collection.objects.link(copy)
plain_source = SOURCE/'Llanura_v02_color.blend'
if not plain_source.exists():
    raise FileNotFoundError('Generate the approved warm plain first: '+str(plain_source))
with bpy.data.libraries.load(str(plain_source),link=False) as (data_from,data_to):
    data_to.objects = ['SM_Llanura_A_LOD0']
plain = data_to.objects[0]
if plain is None:
    raise ValueError('The approved plain mesh was not found in its blend file.')
plain.location = (0,0,0)
patch.collection.objects.link(plain)
camera = stage(patch,(0,0.6,0.10),7.8,'Biomas_conjunto_v01.png')
caption(camera,'FULGOR MEDIEVAL',-3.45,1.93,0.16,gold)
caption(camera,'DEL PRADO A LA SIERRA',-3.45,1.54,0.29)
caption(camera,'Llanura + bosque + colina + montana / estudio de conjunto',-3.45,1.23,0.12,gold)
caption(camera,'COLOR CALIDO  /  Primer prototipo  /  Bordes y transiciones pendientes',
        -3.45,-1.98,0.11,gold)
bpy.ops.render.render(write_still=True)

for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
            area.spaces.active.shading.color_type = 'MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Biomas_color_v01.blend'))
(PREVIEW/'Biomas_color_v01_metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FULGOR_BIOMES_COMPLETE',json.dumps(report))
