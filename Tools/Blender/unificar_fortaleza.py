"""Une la fortaleza corregida de Painter con las piezas nuevas de la v7 (almenas, matacanes,
tapiz, puente y bandera central). La malla solo pierde caras: conserva UV y el Texture Set
Material_0 de Painter. Las piezas reciben la misma deformación que correct_fortress_geometry.py."""
import bpy, bmesh, numpy as np, math, json, hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree

root = Path(__file__).resolve().parents[2]
GLB = 'D:/IA/ComfyUI_windows_portable/ComfyUI/output/3d/fortaleza_completa.glb'
V7 = 'D:/IA/ComfyUI_windows_portable/ComfyUI/output/3d/fortaleza_completa_autentica_v7.blend'
art = root / 'Assets/Fulgor/Art/RawFortress'
tex_out = art / 'Unificada'; tex_out.mkdir(parents=True, exist_ok=True)
src_out = root / 'ArtSource/Blender/FortalezaUnificada'; src_out.mkdir(parents=True, exist_ok=True)
review = root / 'Builds/FortressGeometry'
prefix = art / 'Painter4K/SM_Fortaleza_completa_bruta_Material_0_'
PIEZAS = ['Almenas', 'Matacanes', 'Pendones', 'Puente', 'Tapiz', 'Vara', 'Mensulas']

def smooth(a, b, v):
    t = np.clip((v - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

def corregir(co):
    """Misma deformación que Tools/Blender/correct_fortress_geometry.py (muralla norte y coronación)."""
    fixed = co.copy(); x, y, z = co.T
    knots = np.array([-.30, -.26, -.22, -.18, -.14, -.10, -.06, -.02, .02, .06, .10, .14, .18, .21, .30])
    bow = np.array([0, 0, .010, .025, .033, .032, .030, .028, .025, .020, .015, .010, .005, 0, 0])
    offset = sum(np.interp(x + d, knots, bow) for d in [-.008, -.004, 0, .004, .008]) / 5
    fixed[:, 1] -= offset * smooth(.205, .275, y) * smooth(-.293, -.253, z)
    cx, cy = -.020, -.013
    crown = np.full(len(co), .002, dtype=np.float32)
    for px, py in [(-.030, -.143), (-.020, .117)]:
        crown += .0055 * np.exp(-((x - px) ** 2 + (y - py) ** 2) / (.027 ** 2))
    for px in [-.139, .100]:
        for py in [-.131, .104]:
            crown -= .005 * np.exp(-((x - px) ** 2 + (y - py) ** 2) / (.031 ** 2))
    fixed[:, 2] += crown * (1 - smooth(.165, .205, np.maximum(abs(x - cx), abs(y - cy)))) * smooth(.180, .230, z)
    return fixed

def coords(me):
    co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get('co', co); return co.reshape(-1, 3)

# ---------- v7 y malla original de referencia ----------
bpy.ops.wm.open_mainfile(filepath=V7)
for o in [o for o in bpy.data.objects if o.type != 'MESH']: bpy.data.objects.remove(o)
bpy.ops.import_scene.gltf(filepath=GLB)
fuente = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name not in PIEZAS + ['Fortaleza'])
fuente.name = 'Fuente_original'
co0 = coords(fuente.data)
zmin = float(co0[:, 2].min())

# la deformación reimplementada debe reproducir exactamente la corrección publicada
ref = np.load(review / 'corrected_vertices.npy')
err = float(np.abs(corregir(co0) - ref).max())
assert err < 1e-6, f'La deformación no coincide con la corrección publicada: {err}'

fort = bpy.data.objects['Fortaleza']; me = fort.data
# la v7 solo borró caras: sus vértices deben estar sobre los originales
kd = KDTree(len(co0))
for i, v in enumerate(co0): kd.insert(v, i)
kd.balance()
co7 = coords(me); muestra = co7[::97]
desvio = max(kd.find(Vector(v))[2] for v in muestra)
assert desvio < 1e-6, f'La malla v7 no está sobre la original: {desvio}'

# ---------- Texture Set de Painter ----------
def cargar(suf, noncolor=False):
    im = bpy.data.images.load(str(prefix) + suf + '.png', check_existing=True)
    if noncolor: im.colorspace_settings.name = 'Non-Color'
    return im
alb = cargar('AlbedoTransparency'); nrm = cargar('Normal', True); ms = cargar('MetallicSmoothness', True)
mat = bpy.data.materials.new('Material_0'); mat.use_nodes = True
nodes = mat.node_tree.nodes; nodes.clear(); links = mat.node_tree.links
sh = nodes.new('ShaderNodeBsdfPrincipled'); out = nodes.new('ShaderNodeOutputMaterial'); links.new(sh.outputs['BSDF'], out.inputs['Surface'])
c = nodes.new('ShaderNodeTexImage'); c.image = alb; links.new(c.outputs['Color'], sh.inputs['Base Color'])
n = nodes.new('ShaderNodeTexImage'); n.image = nrm
nm = nodes.new('ShaderNodeNormalMap'); links.new(n.outputs['Color'], nm.inputs['Color']); links.new(nm.outputs['Normal'], sh.inputs['Normal'])
p = nodes.new('ShaderNodeTexImage'); p.image = ms
sep = nodes.new('ShaderNodeSeparateColor'); links.new(p.outputs['Color'], sep.inputs['Color']); links.new(sep.outputs['Red'], sh.inputs['Metallic'])
inv = nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1
links.new(p.outputs['Alpha'], inv.inputs[1]); links.new(inv.outputs[0], sh.inputs['Roughness'])
me.materials.clear(); me.materials.append(mat)
if me.has_custom_normals: me.normals_split_custom_set([(0.0, 0.0, 0.0)] * len(me.loops))

# ---------- almenas: color de Painter transferido desde la malla original ----------
emit = bpy.data.materials.new('tmp_emit'); emit.use_nodes = True; en = emit.node_tree.nodes; en.clear()
t = en.new('ShaderNodeTexImage'); t.image = alb
e = en.new('ShaderNodeEmission'); o = en.new('ShaderNodeOutputMaterial')
emit.node_tree.links.new(t.outputs[0], e.inputs[0]); emit.node_tree.links.new(e.outputs[0], o.inputs[0])
fuente.data.materials.clear(); fuente.data.materials.append(emit)
alm = bpy.data.objects['Almenas']; am = alm.data.materials[0]; an = am.node_tree
img = bpy.data.images.new('almenas_painter', 2048, 2048)
ti = next(x for x in an.nodes if x.type == 'TEX_IMAGE' and x.outputs[0].links and x.outputs[0].links[0].to_socket.name == 'Base Color')
# fuera el relieve de juntas de la v7: en esta línea las juntas vendrán del material de detalle
for x in [x for x in an.nodes if x.type == 'NORMAL_MAP' or (x.type == 'TEX_IMAGE' and x.name != ti.name)]: an.nodes.remove(x)
tgt = an.nodes.new('ShaderNodeTexImage'); tgt.image = img; an.nodes.active = tgt
sc = bpy.context.scene; sc.render.engine = 'CYCLES'
prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
for dv in prefs.devices: dv.use = (dv.type == 'OPTIX')
sc.cycles.device = 'GPU'; sc.cycles.samples = 4
for x in bpy.context.view_layer.objects: x.select_set(False)
fuente.select_set(True); alm.select_set(True); bpy.context.view_layer.objects.active = alm
bk = sc.render.bake; bk.use_selected_to_active = True; bk.cage_extrusion = 0.004; bk.max_ray_distance = 0.012; bk.margin = 6
bpy.ops.object.bake(type='EMIT')
bk.use_selected_to_active = False
img.filepath_raw = str(tex_out / 'T_Fortaleza_Almenas_Albedo.png'); img.file_format = 'PNG'; img.save()
ti.image = bpy.data.images.load(img.filepath_raw); an.nodes.remove(tgt); bpy.data.images.remove(img)

# ---------- matacanes: tono del parapeto en la pintura de Painter ----------
mo = bpy.data.objects['Matacanes']; mc = coords(mo.data)
kdm = KDTree(len(mc))
for i, v in enumerate(mc[::7]): kdm.insert(v, i)
kdm.balance()
fm = fuente.data; nf = len(fm.polygons)
cent = np.empty(nf * 3, np.float32); fm.polygons.foreach_get('center', cent); cent = cent.reshape(-1, 3)
# solo el parapeto: caras próximas a una ménsula y por encima de ella (el muro de debajo está más sucio)
mcs = mc[::7]
def parapeto(v):
    _, i, d = kdm.find(Vector(v)); return d < 0.006 and v[2] > mcs[i][2] + 0.002
cerca = np.array([parapeto(v) for v in cent[::5]])
idx = np.arange(0, nf, 5)[cerca]
ls = np.empty(nf, np.int32); fm.polygons.foreach_get('loop_start', ls)
uv = np.empty(len(fm.loops) * 2, np.float32); fm.uv_layers.active.data.foreach_get('uv', uv); uv = uv.reshape(-1, 2)
AW, AH = alb.size; apx = np.array(alb.pixels[:], np.float32).reshape(AH, AW, 4)
fu = uv[ls[idx]]
base = np.median(apx[np.clip((fu[:, 1] % 1) * AH, 0, AH - 1).astype(int), np.clip((fu[:, 0] % 1) * AW, 0, AW - 1).astype(int), :3], 0)
S = 1024; rng = np.random.default_rng(9)
def ruido(k):
    r = rng.random((S // k + 2, S // k + 2)); r = np.kron(r, np.ones((k, k)))[:S, :S]
    h = max(k // 2, 1); return (r + np.roll(r, h, 0) + np.roll(r, h, 1) + np.roll(np.roll(r, h, 0), h, 1)) / 4
v = 0.55 * ruido(64) + 0.3 * ruido(16) + 0.15 * ruido(2)
col = np.clip(base, 0, 1)[None, None, :] * (0.88 + 0.24 * v)[..., None]
col *= (1 - 0.18 * (ruido(128) > 0.72))[..., None]
mimg = bpy.data.images.new('matacanes_painter', S, S)
mimg.pixels.foreach_set(np.concatenate([col.reshape(-1, 3), np.ones((S * S, 1))], 1).astype(np.float32).ravel())
mimg.filepath_raw = str(tex_out / 'T_Fortaleza_Matacanes_Albedo.png'); mimg.file_format = 'PNG'; mimg.save()
mt = next(x for x in mo.data.materials[0].node_tree.nodes if x.type == 'TEX_IMAGE')
mt.image = bpy.data.images.load(mimg.filepath_raw); bpy.data.images.remove(mimg)

# texturas del resto de piezas, con nombre estable para Unity
for nombre, archivo in [('Puente', 'Madera'), ('Vara', 'Madera'), ('Tapiz', 'Tapiz'), ('Pendones', 'Pendon')]:
    ob = bpy.data.objects[nombre]
    for m in ob.data.materials:
        for x in m.node_tree.nodes:
            if x.type == 'TEX_IMAGE' and x.image:
                dst = tex_out / f'T_Fortaleza_{archivo}_Albedo.png'
                x.image.filepath_raw = str(dst); x.image.file_format = 'PNG'; x.image.save()
bpy.data.objects.remove(fuente); bpy.data.materials.remove(emit)

# ---------- misma corrección y misma transformación de exportación ----------
for ob in [bpy.data.objects[nm_] for nm_ in ['Fortaleza'] + PIEZAS]:
    for x in bpy.context.view_layer.objects: x.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    c = coords(ob.data); ob.data.vertices.foreach_set('co', corregir(c).ravel()); ob.data.update()
bpy.ops.wm.save_as_mainfile(filepath=str(src_out / 'Fortaleza_Unificada_Painter.blend'), compress=True)

# Un solo objeto, como la corregida: con varios objetos Unity no hornea ejes ni escala en los hijos.
# Ranuras de material en orden fijo; RawFortressUnifiedSetup.cs asigna por este mismo orden.
RANURAS = ['Material_0', 'Almenas', 'Matacanes', 'Madera', 'Tapiz', 'Pendon', 'Hierro']
RANURA_DE = {'Fortaleza': 0, 'Almenas': 1, 'Matacanes': 2, 'Puente': 3, 'Vara': 3, 'Tapiz': 4, 'Pendones': 5, 'Mensulas': 6}
canon = [None] * len(RANURAS)
for nm_, k in RANURA_DE.items():
    if canon[k] is None: canon[k] = bpy.data.objects[nm_].data.materials[0]
for m in bpy.data.materials:                      # aparta nombres ocupados (p. ej. el Material_0 de la v7)
    if m.name in RANURAS and all(m != c for c in canon): m.name = m.name + '_v7'
for k, m in enumerate(canon): m.name = RANURAS[k]
for nm_, k in RANURA_DE.items():
    me_ = bpy.data.objects[nm_].data
    # una sola capa UV con el nombre de la malla: si no, al unir hay dos capas y Unity lee la primera
    activa = me_.uv_layers.active.name
    for capa in [c.name for c in me_.uv_layers if c.name != activa]: me_.uv_layers.remove(me_.uv_layers[capa])
    me_.uv_layers[activa].name = 'UVMap'
    me_.materials.clear()
    for m in canon: me_.materials.append(m)
    me_.polygons.foreach_set('material_index', np.full(len(me_.polygons), k, np.int32))
for x in bpy.context.view_layer.objects: x.select_set(False)
for nm_ in ['Fortaleza'] + PIEZAS: bpy.data.objects[nm_].select_set(True)
bpy.context.view_layer.objects.active = fort
bpy.ops.object.join()
fort.name = 'Fortaleza_unificada'
assert [m.name for m in fort.data.materials] == RANURAS
assert [c.name for c in fort.data.uv_layers] == ['UVMap']
# Unity crea los submeshes por orden de aparición en las caras, no por ranura: ordenar caras por material
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.sort_elements(type='MATERIAL', elements={'FACE'})
bpy.ops.object.mode_set(mode='OBJECT')
orden = np.array([p.material_index for p in fort.data.polygons])
assert np.all(np.diff(orden) >= 0), 'Caras sin ordenar por material'
fort.rotation_mode = 'XYZ'                        # viene de glTF (cuaternión): sin esto se ignora el giro
fort.rotation_euler = (0, 0, -math.pi / 2); fort.location = (0, 0, -zmin)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
por_ranura = np.bincount(np.array([p.material_index for p in fort.data.polygons]), minlength=len(RANURAS))
fbx = art / 'SM_Fortaleza_unificada.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                         global_scale=1, apply_unit_scale=True, bake_anim=False, add_leaf_bones=False,
                         use_mesh_modifiers=False, path_mode='AUTO')

informe = {'fuente_v7': V7, 'error_deformacion': err, 'desvio_v7_original': desvio,
           'caras_originales': nf, 'ranuras_material': RANURAS,
           'caras_por_ranura': {RANURAS[k]: int(c) for k, c in enumerate(por_ranura)},
           'tono_matacanes_painter': [round(float(b), 4) for b in base],
           'fbx': str(fbx.relative_to(root)).replace('\\', '/'), 'texturas': str(tex_out.relative_to(root)).replace('\\', '/'),
           'painter_textures_untouched': True, 'source_glb_untouched': True}
(src_out / 'validacion.json').write_text(json.dumps(informe, indent=2, ensure_ascii=False), encoding='utf-8')
print('FORTALEZA_UNIFICADA_OK', json.dumps(informe, ensure_ascii=False), flush=True)
