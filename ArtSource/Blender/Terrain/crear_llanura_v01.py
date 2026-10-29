"""Run with Blender --background --factory-startup --python this_file.

Original procedural art for Fulgor Medieval. Local files only; no downloads.
"""
from pathlib import Path
import argparse
import json
import math
import random
import sys

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--paleta', choices=('original', 'calida'), default='calida')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
WARM = args.paleta == 'calida'
VERSION = 'v02_color' if WARM else 'v01'
EXPORT = ROOT / 'Assets/Fulgor/Art/Terrain'
if WARM:
    EXPORT = EXPORT / 'ColorV02'
PREVIEW = ROOT / 'Builds/ArtPreview'
for directory in (SOURCE, EXPORT, PREVIEW):
    directory.mkdir(parents=True, exist_ok=True)

# This script runs in a separate factory-startup process, never in the user's scene.
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0


def material(name, color, roughness=0.9, emission=False):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = roughness
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1)
        shader.inputs['Emission Strength'].default_value = 1.0
    return mat


COLOR_NAMES = ('M_Llanura_Oliva', 'M_Llanura_Ocre', 'M_Llanura_Salvia',
               'M_Tierra_Arcilla', 'M_Hierba_Seca', 'M_Cereal_Dorado',
               'M_Piedra_Caliza', 'M_Senda_Polvo')
ORIGINAL_COLORS = ((0.29,0.34,0.115), (0.40,0.37,0.145), (0.35,0.39,0.16),
                   (0.25,0.16,0.085), (0.57,0.45,0.18), (0.73,0.53,0.19),
                   (0.58,0.53,0.39), (0.51,0.37,0.19))
WARM_COLORS = ((0.12,0.40,0.022), (0.24,0.49,0.035), (0.075,0.30,0.025),
               (0.36,0.105,0.035), (0.58,0.39,0.025), (0.95,0.51,0.015),
               (0.73,0.58,0.34), (0.57,0.245,0.065))
PALETTE = [material(name + ('_Calida' if WARM else ''), color)
           for name, color in zip(COLOR_NAMES, WARM_COLORS if WARM else ORIGINAL_COLORS)]


def mesh_object(name, vertices, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    for mat in PALETTE:
        mesh.materials.append(mat)
    for polygon, index in zip(mesh.polygons, indices):
        polygon.material_index = index
    return obj


def perimeter(index):
    sector, fraction = divmod(index % 18, 3)
    a = math.radians(30 + sector * 60)
    b = math.radians(30 + (sector + 1) * 60)
    t = fraction / 3
    return (0.97 * ((1-t)*math.cos(a) + t*math.cos(b)),
            0.97 * ((1-t)*math.sin(a) + t*math.sin(b)))


def ground(name, rng):
    vertices = [(0, 0, 0.12)]
    for ring in (1/3, 2/3, 1):
        for i in range(18):
            x, y = perimeter(i)
            vertices.append((x*ring, y*ring, 0.12 if ring == 1 else 0.12+rng.uniform(-0.004, 0.004)))
    faces, indices = [], []
    for i in range(18):
        j = (i+1) % 18
        faces.append((0, 1+i, 1+j))
        indices.append(rng.choice((0, 0, 1, 2)))
    for ring in range(2):
        start, end = 1+18*ring, 1+18*(ring+1)
        for i in range(18):
            j = (i+1) % 18
            faces.extend(((start+i, end+i, end+j), (start+i, end+j, start+j)))
            # Large shared patches rather than independent saturated triangles.
            tone = (i//3 + ring) % 3
            indices.extend((tone, tone))
    base = len(vertices)
    vertices.extend((*perimeter(i), 0) for i in range(18))
    for i in range(18):
        j = (i+1) % 18
        faces.append((37+i, base+i, base+j, 37+j))
        indices.append(3)
    faces.append(tuple(range(base+17, base-1, -1)))
    indices.append(3)
    return mesh_object(name, vertices, faces, indices)


def grass(rng, x, y, cereal=False):
    vertices, faces = [], []
    for blade in range(3):
        angle = rng.uniform(0, math.tau)
        width = 0.012 if cereal else 0.018
        height = rng.uniform(0.11, 0.19) if cereal else rng.uniform(0.055, 0.12)
        dx, dy = math.cos(angle)*width, math.sin(angle)*width
        bx, by = x+rng.uniform(-0.02, 0.02), y+rng.uniform(-0.02, 0.02)
        n = len(vertices)
        vertices.extend(((bx-dx, by-dy, 0.12), (bx+dx, by+dy, 0.12),
                         (bx+0.025, by+0.01, 0.12+height)))
        faces.append((n, n+1, n+2))
        if cereal:
            n = len(vertices)
            vertices.extend(((bx+0.005, by+0.01, 0.12+height-0.025),
                             (bx+0.045, by+0.01, 0.12+height-0.025),
                             (bx+0.025, by+0.01, 0.12+height+0.025)))
            faces.append((n, n+1, n+2))
    return mesh_object('Cereal' if cereal else 'Mata', vertices, faces,
                       [5 if cereal else 4]*len(faces))


def rock(rng, x, y):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(x, y, 0.14))
    obj = bpy.context.object
    obj.name = 'Caliza'
    obj.scale = (rng.uniform(0.035, 0.07), rng.uniform(0.035, 0.06), 0.035)
    obj.data.materials.append(PALETTE[6])
    return obj


def ribbon():
    vertices, faces = [], []
    for i in range(7):
        x = -0.97*math.sqrt(3)/2 + i*(0.97*math.sqrt(3)/6)
        center = 0.08*math.sin(i*0.8)
        vertices.extend(((x, center-0.115, 0.127), (x, center+0.115, 0.127)))
        if i:
            n = 2*i
            faces.append((n-2, n, n+1, n-1))
    return mesh_object('Senda', vertices, faces, [7]*len(faces))


def build_variant(letter):
    rng = random.Random(501 + ord(letter))
    parts = [ground('Base_'+letter, rng)]
    for i in range(42):
        if letter == 'B':
            x = -0.50 + (i % 6)*0.18 + rng.uniform(-0.012, 0.012)
            y = -0.52 + (i//6)*0.17 + rng.uniform(-0.012, 0.012)
        else:
            angle = rng.uniform(0, math.tau)
            radius = math.sqrt(rng.random())*0.72
            x, y = math.cos(angle)*radius, math.sin(angle)*radius
        if letter == 'C' and abs(y) < 0.24:
            continue
        parts.append(grass(rng, x, y, letter == 'B'))
    for x, y in ((-0.58, -0.2), (0.55, 0.28), (0.62, -0.25)):
        parts.append(rock(rng, x, y))
    if letter == 'C':
        parts.append(ribbon())
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = 'SM_Llanura_'+letter+'_LOD0'
    # Mesh origin stays at center-bottom (0,0,0); all geometry is above that plane.
    uv = obj.data.uv_layers.new(name='UV_Llanura_Planar')
    for polygon in obj.data.polygons:
        for loop in polygon.loop_indices:
            vertex = obj.data.vertices[obj.data.loops[loop].vertex_index].co
            uv.data[loop].uv = ((vertex.x+1)*0.5, (vertex.y+1)*0.5)
    return obj


variants = [build_variant(letter) for letter in 'ABC']
report = []
for obj in variants:
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(obj.name+'.fbx')), use_selection=True,
                             object_types={'MESH'}, global_scale=1, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             add_leaf_bones=False, bake_anim=False, use_triangles=True)
    obj.data.calc_loop_triangles()
    points = [vertex.co for vertex in obj.data.vertices]
    bounds = [[min(p[i] for p in points), max(p[i] for p in points)] for i in range(3)]
    report.append({'name': obj.name, 'triangles': len(obj.data.loop_triangles),
                   'vertices': len(points), 'bounds_blender': bounds,
                   'origin': list(obj.location), 'scale': list(obj.scale)})
    assert bounds[2][0] >= -0.0001 and bounds[2][1] < 0.4
    assert all(abs(p.x) <= 0.97+0.0001 and abs(p.y) <= 0.97+0.0001 for p in points)
    assert all(abs(p.x) <= 0.97*math.sqrt(3)/2+0.0001 and
               abs(p.x)+math.sqrt(3)*abs(p.y) <= math.sqrt(3)*0.97+0.0001 for p in points)

# Separate presentation stage; only the three meshes above were exported.
for obj, x in zip(variants, (-2.65, 0, 2.65)):
    obj.location.x = x
stage = material('M_Preview_Fondo', (0.042, 0.026, 0.014) if WARM else (0.035, 0.048, 0.032))
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.015))
floor = bpy.context.object
floor.name = 'PREVIEW_Suelo'
floor.data.materials.append(stage)

bpy.ops.object.camera_add(location=(0, -7.5, 9))
camera = bpy.context.object
camera.name = 'PREVIEW_Camera'
camera.rotation_euler = (Vector((0, 0, 0.10))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 9.4
scene.camera = camera

ink = material('M_Preview_Marfil', (0.81, 0.78, 0.65), emission=True)
muted = material('M_Preview_Salvia', (0.66, 0.41, 0.16) if WARM else (0.33, 0.40, 0.27), emission=True)


def caption(body, x, y, size, mat=ink, align='LEFT'):
    curve = bpy.data.curves.new('Preview_Text', type='FONT')
    curve.body, curve.size, curve.align_x = body, size, align
    obj = bpy.data.objects.new('PREVIEW_'+body[:20], curve)
    scene.collection.objects.link(obj)
    obj.parent = camera
    obj.location = (x, y, -7)
    curve.materials.append(mat)


caption('FULGOR MEDIEVAL', -4.15, 2.28, 0.19, muted)
caption('TIERRAS DE CASTILLA', -4.15, 1.82, 0.38)
caption('Llanuras / verdes vivos, cereal dorado y tierras calidas' if WARM else
        'Estudio de llanura / primeras variantes de terreno', -4.15, 1.49, 0.15, muted)
for x, title, sub in zip((-2.65, 0, 2.65),
                         ('01 / PASTIZAL', '02 / CEREAL', '03 / SENDA'),
                         ('Oliva, hierba seca y caliza', 'Surcos de cereal dorado', 'Tierra batida y vegetacion baja')):
    caption(title, x, -1.28, 0.19, align='CENTER')
    caption(sub, x, -1.60, 0.13, muted, 'CENTER')
caption(('PROPUESTA V02 COLOR' if WARM else 'PROPUESTA V01') +
        '  /  Malla ligera  /  Escala Blender - Unity 1:1', -4.15, -2.40, 0.13, muted)

for name, location, energy, size, color in (
    ('PREVIEW_Key', (-3,-4,7), 650 if WARM else 1100, 5, (1.0,0.92,0.78) if WARM else (1.0,0.85,0.65)),
    ('PREVIEW_Fill', (4,2,5), 180 if WARM else 650, 4, (0.96,1.0,0.83) if WARM else (0.70,0.80,1.0)),
):
    bpy.ops.object.light_add(type='AREA', location=location)
    lamp = bpy.context.object
    lamp.name = name
    lamp.data.energy, lamp.data.shape, lamp.data.size = energy, 'DISK', size
    lamp.data.color = color
    lamp.rotation_euler = (-lamp.location).to_track_quat('-Z','Y').to_euler()

scene.world.color = (0.16, 0.16, 0.16)
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(PREVIEW/('Llanura_'+VERSION+'.png'))
scene.view_settings.view_transform = 'Standard' if WARM else 'AgX'
scene.view_settings.exposure = -0.4 if WARM else 0
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_perspective = 'CAMERA'
            area.spaces.active.shading.color_type = 'MATERIAL'
bpy.ops.object.select_all(action='DESELECT')
variants[0].select_set(True)
bpy.context.view_layer.objects.active = variants[0]
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/('Llanura_'+VERSION+'.blend')))
bpy.ops.render.render(write_still=True)
(PREVIEW/('Llanura_'+VERSION+'_metrics.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
print('FULGOR_ART_COMPLETE', json.dumps(report))
