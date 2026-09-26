import bpy
import math
import os


COLLECTION_NAME = "Fulgor_Referencia_Escala"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def create_hex_reference():
    vertices = []
    for index in range(6):
        angle = math.radians(60 * index + 30)
        vertices.append((math.cos(angle), math.sin(angle), 0.0))
    mesh = bpy.data.meshes.new("SM_Hex_Referencia")
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3, 4, 5)])
    mesh.update()
    obj = bpy.data.objects.new("SM_Hex_Referencia", mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def create_reference_block(name, footprint, height, x):
    bpy.ops.mesh.primitive_cube_add(location=(x, 0.0, height * 0.5))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (footprint, footprint, height)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    previous_cursor = bpy.context.scene.cursor.location.copy()
    bpy.context.scene.cursor.location = (x, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    bpy.context.scene.cursor.location = previous_cursor
    return obj


clear_scene()
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.unit_settings.scale_length = 1.0

hex_reference = create_hex_reference()
hex_reference.display_type = "WIRE"
hex_reference.show_in_front = True

create_reference_block("REF_Fortaleza_1.18x0.82", 1.18, 0.82, 0.0)
create_reference_block("REF_Hueste_0.45x0.55", 0.45, 0.55, 2.25)

text_editor = bpy.context.space_data
script_path = bpy.path.abspath(text_editor.text.filepath)
script_directory = os.path.dirname(script_path)
output_path = os.path.join(script_directory, "Fulgor_VisualScale_Template.blend")
bpy.ops.wm.save_as_mainfile(filepath=output_path)

project_root = os.path.normpath(os.path.join(script_directory, "..", ".."))
fbx_directory = os.path.join(project_root, "Assets", "Fulgor", "Art", "Validation")
os.makedirs(fbx_directory, exist_ok=True)
fbx_path = os.path.join(fbx_directory, "SM_VisualScaleValidation.fbx")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    add_leaf_bones=False,
    bake_anim=False,
)

print(f"Plantilla Fulgor guardada: {output_path}")
print(f"FBX de validacion exportado: {fbx_path}")
