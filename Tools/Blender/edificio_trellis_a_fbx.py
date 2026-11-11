"""Edificio de casilla: de la malla de TRELLIS.2 a un modelo de juego (baja, UV, color y normales horneados).

Dos pasos. El primero alinea la huella con los ejes, escala y renderiza cuatro vistas para elegir por qué lado queda la puerta;
el segundo gira esa cara a −Y (la que espera CampaignTileBuildingBuilder) y exporta el FBX. Hay dos formas de hacer el segundo:
  directo  el GLB ya viene aligerado de ComfyUI (nodo DecimateMesh a ~40000 triángulos): solo se gira, se escala y se sacan los PNG.
  juego    el GLB trae la malla alta (700000): se remalla por vóxeles, se decima, se desdobla y se hornea aquí. Da UV muy rotas en
           edificios (miles de islas); se conserva por si hiciera falta una malla alta de referencia.

Uso:
  blender -b --python edificio_trellis_a_fbx.py -- <glb> <carpeta_de_trabajo> <Nombre> vistas
  blender -b --python edificio_trellis_a_fbx.py -- <glb> <carpeta_de_trabajo> <Nombre> directo <vista_de_la_puerta -Y|+X|+Y|-X>
  blender -b --python edificio_trellis_a_fbx.py -- <glb> <carpeta_de_trabajo> <Nombre> juego <vista_de_la_puerta -Y|+X|+Y|-X> [triangulos]

La vista de la puerta es la del renderizado en la que se ve la fachada principal: la cámara está en ese lado mirando al edificio.
Variables: VOXEL (arista del remallado en metros por metro de huella, 0.004 por defecto), SUAVIZADO (pasadas, 6), TEX (resolución de los mapas, 2048),
NIVELAR_BASE=1 (modo directo: deja a una misma altura el borde de una base que Tripo entrega en cuesta),
TRIS_OBJETIVO (modo directo: decima a esos triángulos un modelo que se repite mucho en el mapa; en los yacimientos, 18000 apenas se nota y 9000 abre grietas).
Salida en <carpeta_de_trabajo>/<Nombre>/: SM_<Nombre>.fbx, T_<Nombre>_BaseColor.png, T_<Nombre>_Normal.png, informe.json,
<Nombre>_juego.blend y vistas de revisión.
"""
import bpy, bmesh, sys, os, json, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

args = sys.argv[sys.argv.index("--") + 1:]
GLB, WORK, NAME, MODE = args[0], args[1], args[2], args[3]
DOOR = args[4] if MODE in ("juego", "directo") else None
TARGET_TRIS = int(args[5]) if MODE == "juego" and len(args) > 5 else 30000
TEX = int(os.environ.get("TEX", "2048"))
TEX_NRM = int(os.environ.get("TEX_NORMALES", str(TEX)))  # el relieve se usa suave en el juego: puede ir a menos resolución que el color
OUT = os.path.join(WORK, NAME)
os.makedirs(OUT, exist_ok=True)
report = {"nombre": NAME}

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"


def select_only(*objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[-1]


# ---------- 1. importar, alinear la huella con los ejes, escalar a 1 m de lado mayor ----------
bpy.ops.import_scene.gltf(filepath=GLB)
meshes = [o for o in scene.objects if o.type == "MESH"]
select_only(*meshes)
bpy.ops.object.join()
high = bpy.context.view_layer.objects.active
high.name = NAME + "_alta"
select_only(high)
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(scene.objects):
    if o is not high:
        bpy.data.objects.remove(o, do_unlink=True)

me = high.data
co = np.empty(len(me.vertices) * 3, dtype=np.float64)
me.vertices.foreach_get("co", co)
co = co.reshape(-1, 3)

# rectángulo mínimo de la huella: se prueba cada medio grado y se queda el de menor área
xy = co[:, :2] - co[:, :2].mean(axis=0)
best = (1e18, 0.0)
for deg in np.arange(0.0, 90.0, 0.5):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    rx = xy[:, 0] * c + xy[:, 1] * s
    ry = -xy[:, 0] * s + xy[:, 1] * c
    area = (rx.max() - rx.min()) * (ry.max() - ry.min())
    if area < best[0]:
        best = (area, deg)
yaw_align = -best[1]   # girar lo contrario de lo medido deja los lados paralelos a los ejes
report["giro_alineado_grados"] = round(yaw_align, 1)

yaw = yaw_align
if MODE in ("juego", "directo"):
    # la puerta debe acabar en −Y: se gira la cara de la vista elegida hasta ahí
    yaw += {"-Y": 0.0, "+Y": 180.0, "+X": -90.0, "-X": 90.0}[DOOR]
a = math.radians(yaw)
c, s = math.cos(a), math.sin(a)
nx = co[:, 0] * c - co[:, 1] * s
ny = co[:, 0] * s + co[:, 1] * c
co2 = np.stack([nx, ny, co[:, 2]], axis=1)
lo, hi = co2.min(axis=0), co2.max(axis=0)
side = max(hi[0] - lo[0], hi[1] - lo[1])
k = 1.0 / side
# se transforma el objeto (y se aplica) para que las normales personalizadas giren con la malla
high.matrix_world = Matrix.Scale(k, 4) @ Matrix.Translation(Vector((-(lo[0] + hi[0]) / 2, -(lo[1] + hi[1]) / 2, -lo[2]))) @ Matrix.Rotation(a, 4, "Z")
select_only(high)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
me.update()
me.calc_loop_triangles()
co2 = np.empty(len(me.vertices) * 3, dtype=np.float64)
me.vertices.foreach_get("co", co2)
co2 = co2.reshape(-1, 3)
dims = (co2.max(axis=0) - co2.min(axis=0))
report["dimensiones_m"] = [round(float(v), 3) for v in dims]
report["tris_alta"] = len(me.loop_triangles)
report["radio_huella_m"] = round(float(np.sqrt(co2[:, 0] ** 2 + co2[:, 1] ** 2).max()), 3)
HEIGHT = float(dims[2])


def preparar_vista(obj, nodo_texture=True):
    """Workbench con la textura de color; sirve para revisar sin luces ni mapa de normales."""
    sc = scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x = sc.render.resolution_y = 640
    sc.render.image_settings.file_format = "PNG"
    w = bpy.data.worlds.new("w_revision")
    w.color = (0.52, 0.52, 0.55)
    sc.world = w
    for m in obj.data.materials:
        if m and m.use_nodes:
            tex = [n for n in m.node_tree.nodes if n.type == "TEX_IMAGE"]
            if tex:
                m.node_tree.nodes.active = tex[0]


def renderizar_vistas(obj, prefijo):
    preparar_vista(obj)
    cd = bpy.data.cameras.new("cam_revision")
    cd.type = "ORTHO"
    cd.ortho_scale = 1.55
    cam = bpy.data.objects.new("cam_revision", cd)
    scene.collection.objects.link(cam)
    scene.camera = cam
    tgt = Vector((0, 0, HEIGHT * 0.4))
    for nombre, d in (("-Y", (0, -1)), ("+X", (1, 0)), ("+Y", (0, 1)), ("-X", (-1, 0))):
        v = Vector((d[0], d[1], 0.62)).normalized()
        cam.location = tgt + v * 4
        cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(OUT, "%s_%s.png" % (prefijo, nombre.replace("+", "mas").replace("-", "menos")))
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)


if MODE == "vistas":
    renderizar_vistas(high, "alta")
    json.dump(report, open(os.path.join(OUT, "informe_vistas.json"), "w"), indent=1, ensure_ascii=False)
    print("VISTAS_OK", json.dumps(report, ensure_ascii=False))
    sys.exit(0)


def nivelar_base(mesh):
    """Cizalla vertical que lleva el borde de la base a un plano horizontal: z cambia según (x, y), así que lo vertical sigue vertical."""
    v = np.empty(len(mesh.vertices) * 3, dtype=np.float64)
    mesh.vertices.foreach_get("co", v)
    v = v.reshape(-1, 3)
    x, y = v[:, 0] - (v[:, 0].max() + v[:, 0].min()) / 2, v[:, 1] - (v[:, 1].max() + v[:, 1].min()) / 2
    radio, sector = np.hypot(x, y), ((np.arctan2(y, x) + math.pi) / (2 * math.pi) * 36).astype(int) % 36
    borde = []
    for s in range(36):
        en_sector = sector == s
        if en_sector.sum() >= 5:
            exterior = radio[en_sector] >= np.percentile(radio[en_sector], 85)
            borde.append(np.median(v[en_sector][exterior], axis=0))
    borde = np.array(borde)
    (a_x, a_y, a_0), *_ = np.linalg.lstsq(np.c_[borde[:, 0], borde[:, 1], np.ones(len(borde))], borde[:, 2], rcond=None)
    v[:, 2] -= a_x * v[:, 0] + a_y * v[:, 1] + a_0
    v[:, 2] -= v[:, 2].min()
    mesh.vertices.foreach_set("co", v.ravel())
    mesh.update()
    report["base_nivelada_pendiente_grados"] = round(math.degrees(math.atan(math.hypot(a_x, a_y))), 2)


if MODE == "directo":
    # El GLB ya viene aligerado, con UV propias y color y normales horneados (TRELLIS.2 con el decimado a pocos triángulos):
    # se sacan los PNG a la resolución pedida y se exporta la malla tal cual.
    if os.environ.get("NIVELAR_BASE"):
        nivelar_base(high.data)
    if int(os.environ.get("TRIS_OBJETIVO", "0")) > 0:
        decimar = high.modifiers.new("decimar", "DECIMATE")
        decimar.ratio = int(os.environ["TRIS_OBJETIVO"]) / report["tris_alta"]
        select_only(high)
        bpy.ops.object.modifier_apply(modifier=decimar.name)
    mat = high.data.materials[0]
    principled = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")

    def imagen_de(socket):
        nodo = socket.links[0].from_node if socket.links else None
        while nodo is not None and nodo.type != "TEX_IMAGE":
            entradas = [i for i in nodo.inputs if i.links]
            nodo = entradas[0].links[0].from_node if entradas else None
        return nodo.image if nodo is not None else None

    for socket_name, sufijo in (("Base Color", "BaseColor"), ("Normal", "Normal")):
        img = imagen_de(principled.inputs[socket_name])
        if img is None:
            report["sin_" + sufijo] = True
            continue
        if max(img.size) > TEX:
            img.scale(TEX, TEX)
        img.filepath_raw = os.path.join(OUT, "T_%s_%s.png" % (NAME, sufijo))
        img.file_format = "PNG"
        img.save()
        report["textura_" + sufijo] = list(img.size)
    high.name = "SM_" + NAME
    high.data.name = high.name
    high.data.calc_loop_triangles()
    report["tris_baja"] = len(high.data.loop_triangles)
    select_only(high)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "SM_%s.fbx" % NAME), use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1.0, apply_unit_scale=True,
                             mesh_smooth_type="FACE", path_mode="STRIP", bake_anim=False, add_leaf_bones=False)
    renderizar_vistas(high, "juego")
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "%s_juego.blend" % NAME))
    json.dump(report, open(os.path.join(OUT, "informe.json"), "w"), indent=1, ensure_ascii=False)
    print("INFORME", json.dumps(report, ensure_ascii=False))
    sys.exit(0)

# ---------- 2. baja: soldar, quitar polvo, remallar por vóxeles y decimar ----------------
low = high.copy()
low.data = high.data.copy()
low.name = "SM_" + NAME
scene.collection.objects.link(low)
low.data.materials.clear()
while low.data.uv_layers:
    low.data.uv_layers.remove(low.data.uv_layers[0])
bm = bmesh.new()
bm.from_mesh(low.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00004)
bm.verts.ensure_lookup_table()
seen, islands = set(), []
for v in bm.verts:
    if v.index in seen:
        continue
    stack, comp = [v], []
    seen.add(v.index)
    while stack:
        x = stack.pop(); comp.append(x)
        for e in x.link_edges:
            y = e.other_vert(x)
            if y.index not in seen:
                seen.add(y.index); stack.append(y)
    islands.append(comp)
islands.sort(key=len, reverse=True)
removed = 0
for comp in islands[1:]:
    if len(comp) < 200:
        bmesh.ops.delete(bm, geom=[x for x in comp if x.is_valid], context="VERTS"); removed += 1
report["islas_total"] = len(islands); report["islas_quitadas"] = removed
bm.to_mesh(low.data); bm.free()

select_only(low)
low.data.remesh_voxel_size = float(os.environ.get("VOXEL", "0.004"))
bpy.ops.object.voxel_remesh()
suav = low.modifiers.new("Suavizar", "SMOOTH"); suav.factor = 0.6; suav.iterations = int(os.environ.get("SUAVIZADO", "6"))
bpy.ops.object.modifier_apply(modifier=suav.name)
low.data.calc_loop_triangles()
report["tris_voxel"] = len(low.data.loop_triangles)
passes = []
for i in range(8):
    n = len(low.data.loop_triangles)
    if n <= TARGET_TRIS * 1.05:
        break
    mod = low.modifiers.new("Aligerar%d" % i, "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = max(TARGET_TRIS / n, 0.02)
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    low.data.calc_loop_triangles()
    passes.append(len(low.data.loop_triangles))
report["tris_tras_pasadas"] = passes
bm = bmesh.new(); bm.from_mesh(low.data)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=bm.edges)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
bm.to_mesh(low.data); bm.free()
low.data.calc_loop_triangles()
report["tris_baja"] = len(low.data.loop_triangles)
for p in low.data.polygons:
    p.use_smooth = True

# ---------- 3. UV ------------------------------------------------------------------------
# smart_project parte una malla rugosa en miles de islas; aquí se agrupan las caras por su orientación general
def unwrap_por_agrupacion(obj, k=12, suavizado=80, min_caras=200, margen=0.006, info=None):
    """UV por agrupación de caras: se suavizan las normales de las caras, se agrupan por dirección y cada grupo conexo se
    proyecta en un plano (en metros, así que todas las islas tienen la misma densidad de texel); luego se empaquetan."""
    me = obj.data
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(me); bm.free()
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    nf = len(bm.faces)
    n = np.array([f.normal[:] for f in bm.faces], dtype=np.float64)
    areas = np.array([f.calc_area() for f in bm.faces], dtype=np.float64)
    pares = np.array([(e.link_faces[0].index, e.link_faces[1].index) for e in bm.edges if len(e.link_faces) == 2], dtype=np.int64)
    A = np.concatenate([pares[:, 0], pares[:, 1]]); B = np.concatenate([pares[:, 1], pares[:, 0]])
    deg = np.maximum(np.bincount(A, minlength=nf), 1).astype(np.float64)
    ns = n.copy()
    for _ in range(suavizado):
        acc = np.zeros_like(ns); np.add.at(acc, A, ns[B])
        ns = ns + acc / deg[:, None]
        ns /= np.maximum(np.linalg.norm(ns, axis=1, keepdims=True), 1e-12)
    # k-medias sobre las normales suavizadas, con centros iniciales en los ejes y en las pendientes de tejado
    c = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1),
         (.6, 0, .8), (-.6, 0, .8), (0, .6, .8), (0, -.6, .8), (.5, .5, .7), (-.5, .5, .7)][:k]
    centros = np.array(c, dtype=np.float64); centros /= np.linalg.norm(centros, axis=1, keepdims=True)
    for _ in range(15):
        asign = np.argmax(ns @ centros.T, axis=1)
        for j in range(len(centros)):
            m = asign == j
            if m.any():
                v = (ns[m] * areas[m, None]).sum(axis=0); l = np.linalg.norm(v)
                if l > 1e-9: centros[j] = v / l
    asign = np.argmax(ns @ centros.T, axis=1)
    # componentes conexas dentro de cada grupo
    padre = list(range(nf))
    def raiz(x):
        while padre[x] != x:
            padre[x] = padre[padre[x]]; x = padre[x]
        return x
    for a, b in pares:
        if asign[a] == asign[b]:
            ra, rb = raiz(a), raiz(b)
            if ra != rb: padre[ra] = rb
    comp = np.array([raiz(i) for i in range(nf)])
    # las componentes diminutas se funden con la vecina con la que comparten más aristas
    for _ in range(14):
        tam = {}
        for c_ in comp: tam[c_] = tam.get(c_, 0) + 1
        pequenas = {c_ for c_, t in tam.items() if t < min_caras}
        if not pequenas: break
        votos = {}
        for a, b in pares:
            if comp[a] != comp[b]:
                if comp[a] in pequenas: votos.setdefault(comp[a], {}).setdefault(comp[b], 0); votos[comp[a]][comp[b]] += 1
                if comp[b] in pequenas: votos.setdefault(comp[b], {}).setdefault(comp[a], 0); votos[comp[b]][comp[a]] += 1
        cambio = {p: max(v.items(), key=lambda kv: kv[1])[0] for p, v in votos.items()}
        if not cambio: break
        comp = np.array([cambio.get(c_, c_) for c_ in comp])
    ids = {c_: i for i, c_ in enumerate(sorted(set(comp.tolist())))}
    comp = np.array([ids[c_] for c_ in comp])
    # proyección de cada componente sobre el plano de su normal media
    nc = comp.max() + 1
    nm = np.zeros((nc, 3)); np.add.at(nm, comp, ns * areas[:, None]); nm /= np.maximum(np.linalg.norm(nm, axis=1, keepdims=True), 1e-12)
    arriba = np.where(np.abs(nm[:, 2:3]) < 0.9, np.array([[0, 0, 1.0]]), np.array([[0, 1.0, 0]]))
    u = np.cross(arriba, nm); u /= np.maximum(np.linalg.norm(u, axis=1, keepdims=True), 1e-12)
    v = np.cross(nm, u)
    co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    lv = np.empty(len(me.loops), dtype=np.int64); me.loops.foreach_get("vertex_index", lv)
    lf = np.repeat(np.arange(len(me.polygons)), 3)
    p = co[lv]
    uv = np.stack([(p * u[comp[lf]]).sum(axis=1), (p * v[comp[lf]]).sum(axis=1)], axis=1)
    while me.uv_layers: me.uv_layers.remove(me.uv_layers[0])
    layer = me.uv_layers.new(name="UVMap")
    layer.data.foreach_set("uv", uv.ravel())
    me.update(); bm.free()
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT"); obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(margin=margen, rotate=True, scale=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    if info is not None: info.update({"islas_uv": int(nc), "grupos": int(len(centros))})
    return int(nc)


select_only(low)
uv_info = {}
unwrap_por_agrupacion(low, info=uv_info)
report.update(uv_info)

# ---------- 4. horneado ------------------------------------------------------------------
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.cycles.use_denoising = False
try:
    prefs = bpy.context.preferences.addons["cycles"].preferences
    for kind in ("OPTIX", "CUDA"):
        try:
            prefs.compute_device_type = kind
            prefs.get_devices()
            gpus = [d for d in prefs.devices if d.type != "CPU"]
            if gpus:
                for d in prefs.devices:
                    d.use = d.type != "CPU"
                scene.cycles.device = "GPU"
                report["bake_dispositivo"] = kind + ": " + ", ".join(d.name for d in gpus)
                break
        except Exception:
            continue
except Exception as e:
    report["bake_dispositivo"] = "CPU (" + str(e) + ")"

mat = bpy.data.materials.new("M_" + NAME)
mat.use_nodes = True
nt = mat.node_tree
for n in list(nt.nodes):
    if n.type != "OUTPUT_MATERIAL":
        nt.nodes.remove(n)
low.data.materials.append(mat)
img_col = bpy.data.images.new("T_%s_BaseColor" % NAME, TEX, TEX, alpha=False)
img_nrm = bpy.data.images.new("T_%s_Normal" % NAME, TEX_NRM, TEX_NRM, alpha=False)
img_nrm.colorspace_settings.name = "Non-Color"
n_col = nt.nodes.new("ShaderNodeTexImage"); n_col.image = img_col
n_nrm = nt.nodes.new("ShaderNodeTexImage"); n_nrm.image = img_nrm
n_bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
n_map = nt.nodes.new("ShaderNodeNormalMap")
nt.links.new(n_col.outputs["Color"], n_bsdf.inputs["Base Color"])
nt.links.new(n_nrm.outputs["Color"], n_map.inputs["Color"])
nt.links.new(n_map.outputs["Normal"], n_bsdf.inputs["Normal"])
nt.links.new(n_bsdf.outputs["BSDF"], nt.nodes["Material Output"].inputs["Surface"])
n_bsdf.inputs["Roughness"].default_value = 0.7
n_bsdf.inputs["Metallic"].default_value = 0.0

extr, dist = 0.012, 0.03


def _caja(a, r):
    """Desenfoque de caja separable de radio r (ventana de 2r+1) con sumas acumuladas; a es (H, W) o (H, W, C)."""
    k = 2 * r + 1
    for eje in (0, 1):
        relleno = [(r + 1, r) if i == eje else (0, 0) for i in range(a.ndim)]
        c = np.cumsum(np.pad(a, relleno, mode="edge"), axis=eje, dtype=np.float64)
        n = a.shape[eje]
        corte = lambda ini, fin: tuple(slice(ini, fin) if i == eje else slice(None) for i in range(a.ndim))
        a = (c[corte(k, k + n)] - c[corte(0, n)]) / k
    return a.astype(np.float32)


def _min_max(a, r):
    """Mínimo y máximo de cada píxel en su entorno de (2r+1)x(2r+1), por separado en cada eje."""
    lo, hi = a, a
    for eje in (0, 1):
        relleno = [(r, r) if i == eje else (0, 0) for i in range(a.ndim)]
        n = a.shape[eje]
        corte = lambda d: tuple(slice(d, d + n) if i == eje else slice(None) for i in range(a.ndim))
        p_lo, p_hi = np.pad(lo, relleno, mode="edge"), np.pad(hi, relleno, mode="edge")
        lo = np.minimum.reduce([p_lo[corte(d)] for d in range(2 * r + 1)])
        hi = np.maximum.reduce([p_hi[corte(d)] for d in range(2 * r + 1)])
    return lo, hi


def mejorar_color(out, cov):
    """Retoque del color horneado, solo dentro de las islas. Los píxeles de una imagen de 8 bits llegan de Blender ya codificados en sRGB
    (pixels no los pasa a lineal), así que se trabaja directamente sobre ellos:
    (1) afinado sin halos: cada píxel no sale del mínimo y el máximo de su entorno 3x3, así no fabrica motas negras ni brillos;
    (2) claridad opcional, contraste local de radio medio (tejas, sillares, tablones), con el mismo límite en un entorno 5x5;
    (3) al final se quitan las motas negras sueltas (grupos de hasta 4 píxeles muy oscuros rodeados de claro), que con la luz del juego
    se ven como manchas; las ventanas, los vanos y las juntas de las tejas son zonas o líneas más grandes y se quedan como están.
    Variables de entorno: AFINADO (cantidad), CLARIDAD (0 la desactiva) y SOMBRAS (luminancia por debajo de la cual un píxel es negro)."""
    umbral = float(os.environ.get("SOMBRAS", "0.10"))
    cantidad = float(os.environ.get("AFINADO", "0.8"))
    claridad = float(os.environ.get("CLARIDAD", "0.4"))
    k = max(1, TEX // 2048)  # los radios de los desenfoques gruesos se escalan con la resolución del mapa
    s = np.clip(out[..., :3], 0, 1).astype(np.float32)
    cm = cov.astype(np.float32)
    desenfocado = _caja(_caja(s * cm[..., None], 2), 2) / np.maximum(_caja(_caja(cm, 2), 2)[..., None], 1e-4)
    lo, hi = _min_max(s, 1)
    s = np.clip(s + cantidad * (s - desenfocado), lo, hi)
    if claridad > 0:
        grande = _caja(_caja(s * cm[..., None], 8 * k), 8 * k) / np.maximum(_caja(_caja(cm, 8 * k), 8 * k)[..., None], 1e-4)
        lo, hi = _min_max(s, 2)
        s = np.clip(s + claridad * (s - grande), lo, hi)
    lum = s @ np.array([0.3, 0.59, 0.11], dtype=np.float32)
    negro = (lum < umbral) & cov
    claro = (cov & ~negro).astype(np.float32)
    # en una ventana de 5x5: una mota tiene 4 negros o menos; una ventana o una junta, más
    negros_cerca = _caja(negro.astype(np.float32), 2) * 25
    mota = negro & (negros_cerca <= 4.5) & (_caja(claro, 2) > 0.5)
    relleno = _caja(s * claro[..., None], 2) / np.maximum(_caja(claro, 2)[..., None], 1e-4)
    s = np.where(mota[..., None], relleno, s)
    out[..., :3] = np.where(cov[..., None], s, out[..., :3]).astype(np.float32)
    return int(mota.sum())


def proyectar_color(high_obj, low_obj, target):
    """Color de la alta para cada píxel de la UV baja. Por defecto con un rayo que sale un poco por fuera de la baja y entra por su normal:
    toma la primera cara que encuentra, la exterior. El punto más cercano (COLOR_RAYOS=0, y donde el rayo no da con nada) cogía a veces
    la cara de dentro de una teja o de un hueco, que es oscura, y dejaba manchas negras y borrones donde la baja se aparta de la alta."""
    src = None
    for hm in high_obj.data.materials:
        pr = next((n for n in hm.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if pr and pr.inputs["Base Color"].links:
            node = pr.inputs["Base Color"].links[0].from_node
            if node.type == "TEX_IMAGE":
                src = node.image
    if src is None:
        raise RuntimeError("La alta no tiene textura de color")
    sw, sh = src.size
    sbuf = np.empty(sw * sh * 4, dtype=np.float32)
    src.pixels.foreach_get(sbuf)
    sbuf = sbuf.reshape(sh, sw, 4)
    hme = high_obj.data
    hverts = np.empty(len(hme.vertices) * 3, dtype=np.float32); hme.vertices.foreach_get("co", hverts); hverts = hverts.reshape(-1, 3)
    htris = np.empty(len(hme.polygons) * 3, dtype=np.int32); hme.polygons.foreach_get("vertices", htris); htris = htris.reshape(-1, 3)
    huv_l = np.empty(len(hme.loops) * 2, dtype=np.float32); hme.uv_layers.active.data.foreach_get("uv", huv_l); huv_l = huv_l.reshape(-1, 2)
    hloops = np.arange(len(hme.polygons) * 3).reshape(-1, 3)
    bvh = BVHTree.FromObject(high_obj, bpy.context.evaluated_depsgraph_get())
    lme = low_obj.data
    luv = lme.uv_layers.active.data
    lco = np.array([v.co[:] for v in lme.vertices], dtype=np.float64)
    lno = np.array([v.normal[:] for v in lme.vertices], dtype=np.float64)
    texels, pos3, nrm3 = [], [], []
    W = TEX
    for tri in lme.loop_triangles:
        uvs = np.array([luv[l].uv[:] for l in tri.loops], dtype=np.float64) * W
        p3 = lco[list(tri.vertices)]
        x0, x1 = int(np.floor(uvs[:, 0].min())), int(np.ceil(uvs[:, 0].max()))
        y0, y1 = int(np.floor(uvs[:, 1].min())), int(np.ceil(uvs[:, 1].max()))
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1), np.arange(y0, y1 + 1))
        px, py = xs.ravel() + 0.5, ys.ravel() + 0.5
        a_, b_, c_ = uvs
        den = (b_[1] - c_[1]) * (a_[0] - c_[0]) + (c_[0] - b_[0]) * (a_[1] - c_[1])
        if abs(den) < 1e-12:
            continue
        w0 = ((b_[1] - c_[1]) * (px - c_[0]) + (c_[0] - b_[0]) * (py - c_[1])) / den
        w1 = ((c_[1] - a_[1]) * (px - c_[0]) + (a_[0] - c_[0]) * (py - c_[1])) / den
        w2 = 1 - w0 - w1
        eps = -0.02
        ok = (w0 >= eps) & (w1 >= eps) & (w2 >= eps)
        if not ok.any():
            continue
        wv = np.stack([w0[ok], w1[ok], w2[ok]], axis=1)
        texels.append(np.stack([xs.ravel()[ok], ys.ravel()[ok]], axis=1))
        pos3.append(wv @ p3)
        nrm3.append(wv @ lno[list(tri.vertices)])
    texels = np.concatenate(texels); pos3 = np.concatenate(pos3); nrm3 = np.concatenate(nrm3)
    valid = (texels[:, 0] >= 0) & (texels[:, 0] < W) & (texels[:, 1] >= 0) & (texels[:, 1] < W)
    texels, pos3, nrm3 = texels[valid], pos3[valid], nrm3[valid]
    n = len(texels)
    tri_idx = np.empty(n, dtype=np.int64); hit = np.empty((n, 3), dtype=np.float64)
    rayos = os.environ.get("COLOR_RAYOS", "1") == "1"
    fuera, alcance = 0.02, 0.06  # en metros, con la huella a 1 m: el rayo sale 2 cm por fuera de la baja y mira hasta 4 cm por dentro
    sin_impacto = 0
    for i in range(n):
        p = Vector(pos3[i])
        if rayos:
            nn = Vector(nrm3[i])
            if nn.length > 1e-8:
                nn.normalize()
                loc, nor, idx, dst = bvh.ray_cast(p + nn * fuera, -nn, alcance)
                if idx is not None:
                    tri_idx[i] = idx; hit[i] = loc
                    continue
            sin_impacto += 1
        loc, nor, idx, dst = bvh.find_nearest(p)
        tri_idx[i] = idx; hit[i] = loc
    report["color_rayos_sin_impacto"] = sin_impacto
    tv = hverts[htris[tri_idx]].astype(np.float64)
    v0, v1, v2 = tv[:, 0], tv[:, 1], tv[:, 2]
    e0, e1, e2 = v1 - v0, v2 - v0, hit - v0
    d00, d01, d11 = (e0 * e0).sum(1), (e0 * e1).sum(1), (e1 * e1).sum(1)
    d20, d21 = (e2 * e0).sum(1), (e2 * e1).sum(1)
    den = d00 * d11 - d01 * d01
    den[np.abs(den) < 1e-18] = 1e-18
    bv = (d11 * d20 - d01 * d21) / den
    bw = (d00 * d21 - d01 * d20) / den
    bu = 1 - bv - bw
    tuv = huv_l[hloops[tri_idx]].astype(np.float64)
    uv = bu[:, None] * tuv[:, 0] + bv[:, None] * tuv[:, 1] + bw[:, None] * tuv[:, 2]
    sx = np.clip((uv[:, 0] * sw).astype(np.int64), 0, sw - 1)
    sy = np.clip((uv[:, 1] * sh).astype(np.int64), 0, sh - 1)
    col = sbuf[sy, sx, :3]
    out = np.zeros((W, W, 4), dtype=np.float32); out[..., 3] = 1.0
    cov = np.zeros((W, W), dtype=bool)
    out[texels[:, 1], texels[:, 0], :3] = col
    cov[texels[:, 1], texels[:, 0]] = True
    black = cov & (out[..., :3].max(axis=2) < 0.02)
    report["color_negros_rellenados"] = int(black.sum())
    cov &= ~black
    for _ in range(16 * max(1, TEX // 2048)):  # margen de relleno alrededor de las islas, proporcional a la resolución
        grow = np.zeros_like(cov)
        acc = np.zeros_like(out)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sh_cov = np.roll(cov, (dy, dx), axis=(0, 1))
            sh_col = np.roll(out, (dy, dx), axis=(0, 1))
            take = sh_cov & ~cov & ~grow
            acc[take] = sh_col[take]; grow |= take
        out[grow] = acc[grow]; cov |= grow
    if os.environ.get("MEJORAR", "1") == "1":
        report["color_motas_quitadas"] = mejorar_color(out, cov)
    target.pixels.foreach_set(out.ravel())
    target.update()
    return int(n), int(cov.sum())


def bake(kind, node, e, d, clear, **kw):
    nt.nodes.active = node
    select_only(high, low)
    bpy.ops.object.bake(type=kind, use_selected_to_active=True, cage_extrusion=e, max_ray_distance=d,
                        margin=16, margin_type="EXTEND", use_clear=clear, **kw)


report["color_texels"], report["color_cubiertos"] = proyectar_color(high, low, img_col)
bake("NORMAL", n_nrm, extr * 3, dist * 3, True, normal_space="TANGENT")
bake("NORMAL", n_nrm, extr, dist, False, normal_space="TANGENT")
_n = np.empty(TEX_NRM * TEX_NRM * 4, dtype=np.float32); img_nrm.pixels.foreach_get(_n); _n = _n.reshape(TEX_NRM, TEX_NRM, 4)
_black = (_n[..., :3].sum(axis=2) < 0.05) | (_n[..., 2] < 0.62)
_n[_black] = (0.5, 0.5, 1.0, 1.0)
report["normales_vacias_rellenadas"] = int(_black.sum())
img_nrm.pixels.foreach_set(_n.ravel()); img_nrm.update()
img_col.filepath_raw = os.path.join(OUT, "T_%s_BaseColor.png" % NAME); img_col.file_format = "PNG"; img_col.save()
img_nrm.filepath_raw = os.path.join(OUT, "T_%s_Normal.png" % NAME); img_nrm.file_format = "PNG"; img_nrm.save()

# ---------- 5. exportar ------------------------------------------------------------------
bpy.data.objects.remove(high, do_unlink=True)
select_only(low)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "SM_%s.fbx" % NAME), use_selection=True, object_types={"MESH"},
                         axis_forward="-Z", axis_up="Y", global_scale=1.0, apply_unit_scale=True,
                         mesh_smooth_type="FACE", path_mode="STRIP", bake_anim=False, add_leaf_bones=False)
# revisión: la baja con su color en las cuatro vistas
renderizar_vistas(low, "juego")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "%s_juego.blend" % NAME))
json.dump(report, open(os.path.join(OUT, "informe.json"), "w"), indent=1, ensure_ascii=False)
print("INFORME", json.dumps(report, ensure_ascii=False))
