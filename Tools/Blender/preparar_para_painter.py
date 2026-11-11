"""Prepara un modelo sin textura (GLB de Tripo, en piezas) para pintarlo en Substance Painter: malla de juego (low) y malla alta (high).

Une las piezas, fusiona los vértices duplicados del GLB y guarda dos mallas:
  high  la original, con un identificador por pieza en el color de vértice (color plano y distinto): en Painter sirve para hornear el mapa de
        ID de color y seleccionar cada pieza (tejado, muros, ventanas...) al pintar.
  low   una copia suavizada y simplificada, con UV limpias (pocas islas grandes, misma densidad de texel, en 0–1). El relieve fino de una malla
        de Tripo (yeso, tejas, piedras modeladas a mano) rompe cualquier corte por ángulo en miles de islas, y trasladar UV a la original estira
        las caras; por eso la malla de juego es la copia y Painter hornea el detalle de la alta sobre ella (normales, AO, ID).

Uso:
  blender -b --python preparar_para_painter.py -- <glb> <carpeta_de_salida> <Nombre>
Variables: MARGEN (margen entre islas en UV, 0.002), ANGULO (ángulo de corte en grados, 66), SUAVIZADO (pasadas, 40), PROXY (fracción de
caras que se conserva, 0.2), FUSION (distancia de fusión de vértices en metros, 0.00002).
Salida: <Nombre>_low.fbx, <Nombre>_high.fbx, <Nombre>_painter.blend, <Nombre>_ids.png, <Nombre>_uv.png e informe.json.
"""
import bpy, bmesh, sys, os, json, math, random
from mathutils import Vector

glb, out, name = sys.argv[sys.argv.index("--") + 1:][:3]
margin = float(os.environ.get("MARGEN", "0.002"))
angle = math.radians(float(os.environ.get("ANGULO", "66")))
os.makedirs(out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=glb)
scene = bpy.context.scene
parts = [o for o in scene.objects if o.type == "MESH"]
random.seed(11)

for o in sorted(parts, key=lambda p: p.name):
    color = (random.random() * .85 + .1, random.random() * .85 + .1, random.random() * .85 + .1, 1.0)
    attribute = o.data.color_attributes.new(name="ID", type="BYTE_COLOR", domain="CORNER")
    for item in attribute.data:
        item.color = color

bpy.ops.object.select_all(action="DESELECT")
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
high = bpy.context.view_layer.objects.active
high.name = name + "_high"
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

# el GLB trae los vértices duplicados en cada arista (normales partidas)
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
before = len(high.data.vertices)
bpy.ops.mesh.remove_doubles(threshold=float(os.environ.get("FUSION", "0.00002")))
bpy.ops.object.mode_set(mode="OBJECT")
high.data.calc_loop_triangles()
high_tris = len(high.data.loop_triangles)

# malla de juego: copia suavizada y simplificada, con UV
bpy.ops.object.select_all(action="DESELECT"); high.select_set(True); bpy.context.view_layer.objects.active = high
bpy.ops.object.duplicate()
low = bpy.context.view_layer.objects.active
low.name = name + "_low"
while low.data.color_attributes:
    low.data.color_attributes.remove(low.data.color_attributes[0])
smooth = low.modifiers.new("suavizar", "SMOOTH"); smooth.factor = 1.0; smooth.iterations = int(os.environ.get("SUAVIZADO", "40"))
decimate = low.modifiers.new("simplificar", "DECIMATE"); decimate.ratio = float(os.environ.get("PROXY", "0.2"))
bpy.ops.object.modifier_apply(modifier="suavizar"); bpy.ops.object.modifier_apply(modifier="simplificar")
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=angle, island_margin=margin, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
bpy.ops.uv.average_islands_scale()
bpy.ops.uv.pack_islands(margin=margin, rotate=True)
bpy.ops.object.mode_set(mode="OBJECT")

me = low.data
me.calc_loop_triangles()
low_tris = len(me.loop_triangles)
bm = bmesh.new(); bm.from_mesh(me); uv = bm.loops.layers.uv.active
parent = {}
def find(a):
    while parent.setdefault(a, a) != a:
        parent[a] = parent[parent[a]]; a = parent[a]
    return a
area_uv = 0.0
for f in bm.faces:
    keys = [(l.vert.index, round(l[uv].uv.x, 5), round(l[uv].uv.y, 5)) for l in f.loops]
    for k in keys[1:]:
        parent[find(k)] = find(keys[0])
    pts = [l[uv].uv for l in f.loops]
    area_uv += abs(sum(pts[i].x * pts[(i + 1) % len(pts)].y - pts[(i + 1) % len(pts)].x * pts[i].y for i in range(len(pts)))) / 2
islands = len({find(k) for k in list(parent)})
coords = [l[uv].uv for f in bm.faces for l in f.loops]
rango = [round(min(c.x for c in coords), 3), round(max(c.x for c in coords), 3), round(min(c.y for c in coords), 3), round(max(c.y for c in coords), 3)]
bm.free()

report = {"nombre": name, "piezas": len(parts), "triangulos_high": high_tris, "triangulos_low": low_tris,
          "vertices_antes_de_fusionar": before, "islas_uv_low": islands, "uv_ocupacion_low": round(area_uv, 3), "uv_rango": rango}
with open(os.path.join(out, "informe.json"), "w", encoding="utf-8") as f:
    json.dump(report, f, indent=2, ensure_ascii=False)
print("INFO", json.dumps(report, ensure_ascii=False))

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, name + "_painter.blend"))
for obj, suffix in ((low, "_low"), (high, "_high")):
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True); bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + suffix + ".fbx"), use_selection=True, axis_forward="-Z", axis_up="Y",
                             mesh_smooth_type="FACE", add_leaf_bones=False, bake_space_transform=True)

# vistas de comprobación: la malla alta por identificador de pieza, y el despliegue de la baja
mat = bpy.data.materials.new("ids"); mat.use_nodes = True
nodes = mat.node_tree.nodes; nodes.clear()
attr = nodes.new("ShaderNodeVertexColor"); attr.layer_name = "ID"
emit = nodes.new("ShaderNodeEmission"); outn = nodes.new("ShaderNodeOutputMaterial")
mat.node_tree.links.new(attr.outputs["Color"], emit.inputs["Color"]); mat.node_tree.links.new(emit.outputs["Emission"], outn.inputs["Surface"])
high.data.materials.clear(); high.data.materials.append(mat)
low.hide_render = True
scene.render.engine = "BLENDER_WORKBENCH"; scene.display.shading.light = "FLAT"; scene.display.shading.color_type = "MATERIAL"
scene.render.resolution_x = 900; scene.render.resolution_y = 700
scene.world = bpy.data.worlds.new("w"); scene.world.color = (0.1, 0.1, 0.12)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scene.collection.objects.link(cam); scene.camera = cam
bb = [high.matrix_world @ Vector(c) for c in high.bound_box]
lo = Vector((min(v[i] for v in bb) for i in range(3))); hi = Vector((max(v[i] for v in bb) for i in range(3)))
centre = (lo + hi) / 2; size = hi - lo; radius = max(size) * 1.9
cam.location = centre + Vector((0, -radius, size.z * .55 + radius * .35))
cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = os.path.join(out, name + "_ids.png")
bpy.ops.render.render(write_still=True)

# despliegue UV de la baja como imagen (islas en negro sobre blanco), 1024 px
size_px = 1024
canvas = [1.0] * (size_px * size_px * 4)
for poly in low.data.polygons:
    pts = [low.data.uv_layers.active.data[li].uv for li in poly.loop_indices]
    for i in range(len(pts)):
        a = pts[i]; b = pts[(i + 1) % len(pts)]
        steps = int(max(abs(b.x - a.x), abs(b.y - a.y)) * size_px) + 1
        for s in range(steps + 1):
            x = int((a.x + (b.x - a.x) * s / steps) * (size_px - 1)); y = int((a.y + (b.y - a.y) * s / steps) * (size_px - 1))
            if 0 <= x < size_px and 0 <= y < size_px:
                k = (y * size_px + x) * 4; canvas[k] = canvas[k + 1] = canvas[k + 2] = 0.0
img = bpy.data.images.new(name + "_uv", size_px, size_px); img.pixels = canvas
img.filepath_raw = os.path.join(out, name + "_uv.png"); img.file_format = "PNG"; img.save()
