"""Convierte el GLB de un edificio de casilla (Tripo, por partes) en un FBX listo para Unity.

Uso:
  blender -b --python Tools/Blender/edificio_glb_a_fbx.py -- modelo.glb NombreEdificio

Une las piezas en una sola malla, deja el pivote en el centro de la huella y el suelo en cero, y exporta
Assets/Fulgor/Art/Buildings/<NombreEdificio>/SM_<NombreEdificio>.fbx con sus texturas al lado.
La puerta debe mirar a −Y; el giro y la escala se aplican en Unity (ver CampaignTileBuildingBuilder).
"""
import os
import sys
import bpy

args = sys.argv[sys.argv.index('--') + 1:]
fuente, nombre = os.path.abspath(args[0]), args[1]
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
destino = os.path.join(REPO, 'Assets', 'Fulgor', 'Art', 'Buildings', nombre)
os.makedirs(destino, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=fuente)
mallas = [o for o in bpy.data.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in mallas:
    o.select_set(True)
bpy.context.view_layer.objects.active = mallas[0]
bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
obj.name = 'SM_' + nombre
obj.data.name = obj.name

# centra la huella en el origen y apoya la base en z = 0
mn = [min(v[i] for v in obj.bound_box) for i in range(3)]
mx = [max(v[i] for v in obj.bound_box) for i in range(3)]
obj.data.transform(__import__('mathutils').Matrix.Translation(((-(mn[0] + mx[0]) / 2), -(mn[1] + mx[1]) / 2, -mn[2])))
obj.location = (0, 0, 0)

# las texturas se escriben junto al FBX con nombres propios
for img in bpy.data.images:
    if img.size[0] == 0:
        continue
    img.name = '%s_%s' % (nombre, img.name)
    img.filepath_raw = os.path.join(destino, img.name + '.png')
    img.file_format = 'PNG'
    img.save()

bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(destino, 'SM_%s.fbx' % nombre), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', global_scale=1.0, apply_unit_scale=True,
                         mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
print('LISTO', destino, 'tris', sum(len(p.vertices) - 2 for p in obj.data.polygons),
      'texturas', [i.name for i in bpy.data.images if i.size[0]])
