"""Versión ligera de una fortaleza generada con IA (TRELLIS, Tripo...) para el mapa de campaña.

Uso:
  blender -b --python Tools/Blender/fortaleza_mapa_ligera.py -- modelo.glb --salida=Tripo [--tris=60000] [--res=4096] [--limpiar-base]

Importa el GLB, opcionalmente recorta la base de roca que sobresale por debajo del suelo del patio, reduce la
malla conservando sus UV y hornea desde el original completo el color y las normales. La cámara del mapa no
gira, así que el detalle que se pierde en geometría queda en la normal.
Escribe en Assets/Fulgor/Art/Buildings/FortalezaCastilla<salida>/ y ArtSource/Blender/FortalezaMapa<salida>/.
La puerta debe mirar a −Y; el giro hacia la cámara y la escala se aplican en Unity.
"""
import json
import math
import os
import sys
import bpy
import bmesh
import numpy as np

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
fuente = next(a for a in args if not a.startswith('--'))
opt = {a.split('=')[0]: (a.split('=')[1] if '=' in a else True) for a in args if a.startswith('--')}
SALIDA = opt.get('--salida', 'Mapa')
TRIS = int(opt.get('--tris', 60000))
RES = int(opt.get('--res', 4096))
LIMPIAR = '--limpiar-base' in opt
# distancias del horneado (en unidades del modelo): con reducciones muy fuertes hay que ampliarlas o salen grietas negras
EXTRUSION = float(opt.get('--extrusion', 0.006))
RAYO = float(opt.get('--rayo', 0.03))
RECTO = '--recorte-recto' in opt      # recorta la base con cuatro planos rectos (corte limpio, sin triángulos sueltos) en vez de borrar caras
MARGEN_LADO = float(opt.get('--margen-lado', 0.04))       # cerco de base a los lados y detrás, en unidades del modelo
MARGEN_FRENTE = float(opt.get('--margen-frente', 0.12))   # delante de la puerta se deja más para conservar el puente
ENDEREZAR = '--enderezar' in opt     # mide hacia dónde miran los muros y gira el modelo para alinearlos con los ejes
MARGEN = int(opt.get('--margen-base', 8))   # celdas (de 512) de base que se conservan alrededor de los muros al recortar
RECOLOR = '--recolor-base' in opt     # tiñe la base de arena hacia el verde-tierra del terreno
GIRAR = float(opt.get('--girar', 0)) # giro en Z, en grados, para dejar la puerta mirando a −Y
POR_PIEZAS = '--por-piezas' in opt   # reducir cada pieza por separado antes de fusionarlas (la reducción global arruga la malla)
DIRECTO = '--directo' in opt         # reducir preservando las UV y usar la textura original (sin horneado: evita grietas por piezas solapadas)
SIN_NORMAL = '--sin-normal' in opt    # no hornear mapa de normales (sale ruidoso con reducciones muy fuertes de piezas solapadas)
CONSERVAR = '--conservar' in opt      # malla ya ligera y con UV propias (Malla Smart): no se reduce ni se hornea

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ART = os.path.join(REPO, 'Assets', 'Fulgor', 'Art', 'Buildings', 'FortalezaCastilla' + SALIDA)
SRC = os.path.join(REPO, 'ArtSource', 'Blender', 'FortalezaMapa' + SALIDA)
NOM = 'Fortaleza_Castilla_' + SALIDA
os.makedirs(ART, exist_ok=True); os.makedirs(SRC, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(fuente))
mallas = [o for o in bpy.data.objects if o.type == 'MESH']
BAJA_PRE = None
if len(mallas) > 1:
    # generación por partes de Tripo: se fusionan en una sola malla conservando sus posiciones
    if POR_PIEZAS:
        total = sum(len(o.data.polygons) for o in mallas)
        copias = []
        for o in mallas:
            c = o.copy(); c.data = o.data.copy(); bpy.context.collection.objects.link(c)
            bpy.ops.object.select_all(action='DESELECT'); c.select_set(True); bpy.context.view_layer.objects.active = c
            n = len(c.data.polygons)
            m = c.modifiers.new('reducir', 'DECIMATE'); m.decimate_type = 'COLLAPSE'; m.use_collapse_triangulate = True
            m.ratio = min(1.0, max(TRIS / total, 400.0 / max(n, 1)))     # al menos ~400 triángulos por pieza
            bpy.ops.object.modifier_apply(modifier='reducir')
            copias.append(c)
        bpy.ops.object.select_all(action='DESELECT')
        for c in copias: c.select_set(True)
        bpy.context.view_layer.objects.active = copias[0]
        bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
        bpy.ops.object.join()
        BAJA_PRE = bpy.context.view_layer.objects.active
        mallas = [o for o in mallas if o.name in bpy.data.objects]
    # se marca la pieza de la base (la de mayor extensión en planta) para poder teñirla después
    def extension(o):
        v = np.array([o.matrix_world @ p.co for p in o.data.vertices]); return float((v[:, 0].max() - v[:, 0].min()) * (v[:, 1].max() - v[:, 1].min()))
    base_obj = max(mallas, key=extension)
    for o in mallas:
        at = o.data.attributes.new(name='es_base', type='INT', domain='FACE')
        at.data.foreach_set('value', np.full(len(o.data.polygons), 1 if o is base_obj else 0, dtype=np.int32))
    bpy.ops.object.select_all(action='DESELECT')
    for o in mallas: o.select_set(True)
    bpy.context.view_layer.objects.active = mallas[0]
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
    bpy.ops.object.join()
    mallas = [o for o in bpy.data.objects if o.type == 'MESH' and o is not BAJA_PRE]
assert len(mallas) == 1, [o.name for o in mallas]
alta = mallas[0]; alta.name = 'Alta'
alta.rotation_mode = 'XYZ'
bpy.context.view_layer.objects.active = alta; alta.select_set(True)
if alta.parent is not None:        # los GLB de Tripo cuelgan la malla de un RootNode vacío
    bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
if GIRAR:
    alta.rotation_euler = (0, 0, math.radians(GIRAR))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
if ENDEREZAR:
    # orientación dominante de las caras verticales (muros y torres), módulo 90°, con su área como peso
    m_ = alta.data; k = len(m_.polygons)
    nn_ = np.empty(k * 3, dtype=np.float32); aa_ = np.empty(k, dtype=np.float32)
    m_.polygons.foreach_get('normal', nn_); m_.polygons.foreach_get('area', aa_); nn_ = nn_.reshape(-1, 3)
    vert = np.abs(nn_[:, 2]) < 0.25
    ang = np.degrees(np.arctan2(nn_[vert, 1], nn_[vert, 0])) % 90.0
    ang = np.where(ang > 45.0, ang - 90.0, ang)                       # [-45, 45)
    hist, bordes = np.histogram(ang, bins=180, range=(-45, 45), weights=aa_[vert])
    hist = np.convolve(np.concatenate([hist[-3:], hist, hist[:3]]), np.ones(5) / 5, mode='same')[3:-3]
    sesgo = float(bordes[np.argmax(hist)] + (bordes[1] - bordes[0]) / 2)
    print('ENDEREZAR sesgo medido (grados):', round(sesgo, 2))
    alta.rotation_euler = (0, 0, math.radians(-sesgo))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    ENDEREZADO = round(-sesgo, 2)
me = alta.data
info = {'fuente': os.path.abspath(fuente).replace('\\', '/'), 'caras_originales': len(me.polygons)}
if ENDEREZAR: info['enderezado_grados'] = ENDEREZADO

if RECTO:
    # la huella de la fortaleza (muros, torres y edificios) se mide con las caras altas; la base se corta con cuatro planos
    n = len(me.polygons)
    cen = np.empty(n * 3, dtype=np.float32); nor = np.empty(n * 3, dtype=np.float32); area = np.empty(n, dtype=np.float32)
    me.polygons.foreach_get('center', cen); me.polygons.foreach_get('normal', nor); me.polygons.foreach_get('area', area)
    cen = cen.reshape(-1, 3); nor = nor.reshape(-1, 3)
    z0, z1 = cen[:, 2].min(), cen[:, 2].max(); alto = z1 - z0
    horiz = nor[:, 2] > 0.9
    hist, bordes = np.histogram(cen[horiz, 2], bins=200, range=(z0, z1), weights=area[horiz])
    suelo = bordes[np.argmax(hist)] + (bordes[1] - bordes[0]) / 2
    est = cen[:, 2] > suelo + 0.12 * alto
    xa, xb = float(cen[est, 0].min()), float(cen[est, 0].max())
    ya, yb = float(cen[est, 1].min()), float(cen[est, 1].max())
    bm = bmesh.new(); bm.from_mesh(me)
    for co, no in (((xa - MARGEN_LADO, 0, 0), (-1, 0, 0)), ((xb + MARGEN_LADO, 0, 0), (1, 0, 0)),
                   ((0, ya - MARGEN_FRENTE, 0), (0, -1, 0)), ((0, yb + MARGEN_LADO, 0), (0, 1, 0))):
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))     # el corte deja algún cuadrilátero
    bm.to_mesh(me); bm.free(); me.update()
    info.update({'recorte_recto': True, 'huella_x': [round(xa, 3), round(xb, 3)], 'huella_y': [round(ya, 3), round(yb, 3)],
                 'caras_tras_recorte': len(me.polygons)})
if LIMPIAR and not RECTO:
    # el suelo del patio es el plano horizontal más extenso: se recorta lo que queda por debajo de él
    n = len(me.polygons)
    cen = np.empty(n * 3, dtype=np.float32); nor = np.empty(n * 3, dtype=np.float32); area = np.empty(n, dtype=np.float32)
    me.polygons.foreach_get('center', cen); me.polygons.foreach_get('normal', nor); me.polygons.foreach_get('area', area)
    cen = cen.reshape(-1, 3); nor = nor.reshape(-1, 3)
    z0, z1 = cen[:, 2].min(), cen[:, 2].max()
    horiz = nor[:, 2] > 0.9
    hist, bordes = np.histogram(cen[horiz, 2], bins=200, range=(z0, z1), weights=area[horiz])
    suelo = bordes[np.argmax(hist)] + (bordes[1] - bordes[0]) / 2
    alto = z1 - z0
    # huella de la fortaleza: celdas con estructura alta (muros, torres, edificios), cerradas y rellenadas por dentro
    N = 512
    xmin, xmax, ymin, ymax = cen[:, 0].min(), cen[:, 0].max(), cen[:, 1].min(), cen[:, 1].max()
    ix = np.clip(((cen[:, 0] - xmin) / (xmax - xmin) * (N - 1)).astype(int), 0, N - 1)
    iy = np.clip(((cen[:, 1] - ymin) / (ymax - ymin) * (N - 1)).astype(int), 0, N - 1)
    ocupado = np.zeros((N, N), bool)
    estructura = cen[:, 2] > suelo + 0.12 * alto
    ocupado[iy[estructura], ix[estructura]] = True

    def dilatar(m, k):
        for _ in range(k):
            d = m.copy(); d[1:] |= m[:-1]; d[:-1] |= m[1:]; d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]; m = d
        return m
    cerrado = dilatar(ocupado, MARGEN)
    fuera = np.zeros((N, N), bool)
    fuera[0, :] = ~cerrado[0, :]; fuera[-1, :] = ~cerrado[-1, :]; fuera[:, 0] = ~cerrado[:, 0]; fuera[:, -1] = ~cerrado[:, -1]
    while True:
        d = fuera.copy(); d[1:] |= fuera[:-1]; d[:-1] |= fuera[1:]; d[:, 1:] |= fuera[:, :-1]; d[:, :-1] |= fuera[:, 1:]
        d &= ~cerrado
        if (d == fuera).all(): break
        fuera = d
    borrar = np.nonzero(fuera[iy, ix])[0]
    if len(me.loops) == 3 * n:
        # también se quitan los triángulos que, teniendo el centro dentro, asoman por fuera con algún vértice (astillas)
        vco = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get('co', vco); vco = vco.reshape(-1, 3)
        vx = np.clip(((vco[:, 0] - xmin) / (xmax - xmin) * (N - 1)).astype(int), 0, N - 1)
        vy = np.clip(((vco[:, 1] - ymin) / (ymax - ymin) * (N - 1)).astype(int), 0, N - 1)
        lv = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get('vertex_index', lv)
        borrar = np.nonzero(fuera[vy, vx][lv.reshape(-1, 3)].any(axis=1))[0]
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[int(i)] for i in borrar], context='FACES')
    bm.to_mesh(me); bm.free(); me.update()
    sueltas = 0
    if len(me.loops) == 3 * len(me.polygons):
        # fragmentos de roca desconectados: islas pequeñas y bajas (las banderas y piezas altas se conservan)
        tri = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get('vertex_index', tri); tri = tri.reshape(-1, 3)
        vco = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get('co', vco); vco = vco.reshape(-1, 3)
        lab = np.arange(len(me.vertices))
        while True:
            m = lab[tri].min(axis=1); nuevo = lab.copy()
            for k in range(3): np.minimum.at(nuevo, tri[:, k], m)
            nuevo = nuevo[nuevo]
            if (nuevo == lab).all(): break
            lab = nuevo
        cara_isla = lab[tri[:, 0]]
        islas, cuenta = np.unique(cara_isla, return_counts=True)
        zmax = np.full(len(me.vertices), -1e9, dtype=np.float32); np.maximum.at(zmax, lab, vco[:, 2])
        # solo las que quedan en la franja pegada al exterior: el patio y los edificios interiores también son islas
        banda = dilatar(fuera, 20)
        nvert = np.bincount(lab, minlength=len(me.vertices)).astype(np.float64)
        sxv = np.zeros(len(me.vertices)); syv = np.zeros(len(me.vertices))
        np.add.at(sxv, lab, vco[:, 0]); np.add.at(syv, lab, vco[:, 1])
        cxi = np.clip(((sxv[islas] / nvert[islas] - xmin) / (xmax - xmin) * (N - 1)).astype(int), 0, N - 1)
        cyi = np.clip(((syv[islas] / nvert[islas] - ymin) / (ymax - ymin) * (N - 1)).astype(int), 0, N - 1)
        chicas = islas[(cuenta < 400) & (zmax[islas] < suelo + 0.10 * alto) & banda[cyi, cxi]]
        quitar = np.nonzero(np.isin(cara_isla, chicas))[0]
        if len(quitar):
            bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
            bmesh.ops.delete(bm, geom=[bm.faces[int(i)] for i in quitar], context='FACES')
            bm.to_mesh(me); bm.free(); me.update()
        sueltas = int(len(chicas))
    info.update({'suelo_patio': round(float(suelo), 4), 'caras_recortadas': int(len(borrar)), 'islas_sueltas_quitadas': sueltas})

# pivote en el centro de la huella y al nivel del suelo
xs = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get('co', xs); xs = xs.reshape(-1, 3)
cx, cy = (xs[:, 0].min() + xs[:, 0].max()) / 2, (xs[:, 1].min() + xs[:, 1].max()) / 2
xs[:, 0] -= cx; xs[:, 1] -= cy; xs[:, 2] -= xs[:, 2].min()
me.vertices.foreach_set('co', xs.ravel()); me.update()

def tenir_base(me, im):
    """Tiñe en la textura los texels de las caras marcadas como base hacia el verde-tierra del terreno, conservando el relieve."""
    n = len(me.polygons)
    if len(me.loops) != 3 * n or 'es_base' not in me.attributes or not me.uv_layers: return 0
    uv = np.empty(len(me.loops) * 2, dtype=np.float32); me.uv_layers.active.data.foreach_get('uv', uv); uv = uv.reshape(n, 3, 2)
    base = np.empty(n, dtype=np.int32); me.attributes['es_base'].data.foreach_get('value', base)
    W, H = im.size
    mascara = np.zeros((H, W), bool)
    for t in uv[base == 1]:
        xs, ys = t[:, 0] * W, t[:, 1] * H
        x0, x1 = int(max(0, np.floor(xs.min()))), int(min(W - 1, np.ceil(xs.max())))
        y0, y1 = int(max(0, np.floor(ys.min()))), int(min(H - 1, np.ceil(ys.max())))
        den = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
        if abs(den) < 1e-9 or x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        l1 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / den
        l2 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / den
        mascara[y0:y1 + 1, x0:x1 + 1] |= (l1 >= -0.03) & (l2 >= -0.03) & (1 - l1 - l2 >= -0.03)
    espacio = im.colorspace_settings.name; im.colorspace_settings.name = 'Non-Color'
    px = np.empty(W * H * 4, dtype=np.float32); im.pixels.foreach_get(px); px = px.reshape(H, W, 4)
    w = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    c = px[mascara][:, :3]; lum = c @ w
    objetivo = np.array([0.43, 0.50, 0.24], dtype=np.float32)
    nuevo = objetivo[None, :] * (lum / float(objetivo @ w))[:, None] * 0.85
    px[mascara, :3] = np.clip(c * 0.3 + nuevo * 0.7, 0, 1)
    im.pixels.foreach_set(px.ravel()); im.update(); im.colorspace_settings.name = espacio
    return int(mascara.sum())


if CONSERVAR:
    baja = alta; baja.name = NOM
    mat0 = baja.data.materials[0]; mat0.name = 'M_' + NOM
    for n in mat0.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image is not None:
            im = n.image
            es_normal = 'normal' in im.name.lower()
            if RECOLOR and not es_normal: info['base_teñida_texels'] = tenir_base(baja.data, im)
            destino = os.path.join(ART, 'T_%s_%s.%s' % (NOM, 'Normal' if es_normal else 'BaseColor', 'png' if es_normal else 'jpg'))
            im.filepath_raw = destino; im.file_format = 'PNG' if es_normal else 'JPEG'; im.save()
            info['textura_' + ('normal' if es_normal else 'color')] = os.path.basename(destino)
    tris = sum(len(p.vertices) - 2 for p in baja.data.polygons)
    xy = np.array([(v.co.x, v.co.y) for v in baja.data.vertices])
    info.update({'triangulos': tris, 'dimensiones': [round(d, 4) for d in baja.dimensions],
                 'radio_huella': round(float(np.hypot(xy[:, 0], xy[:, 1]).max()), 4)})
    bpy.ops.object.select_all(action='DESELECT'); baja.select_set(True); bpy.context.view_layer.objects.active = baja
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, NOM + '.blend'), compress=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(ART, 'SM_%s.fbx' % NOM), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
                             apply_unit_scale=True, mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
    json.dump(info, open(os.path.join(SRC, 'validacion.json'), 'w'), indent=2)
    print('MAPA_OK', info)
    sys.exit(0)

# --- copia reducida ---
baja = alta.copy(); baja.data = alta.data.copy(); baja.name = NOM
bpy.context.collection.objects.link(baja)
bpy.context.view_layer.objects.active = baja
tris_alta = sum(len(p.vertices) - 2 for p in baja.data.polygons)
mod = baja.modifiers.new('reducir', 'DECIMATE')
if BAJA_PRE is not None:              # la reducción ya se hizo pieza a pieza
    baja.data = BAJA_PRE.data; bpy.data.objects.remove(BAJA_PRE, do_unlink=True)
mod.decimate_type = 'COLLAPSE'; mod.ratio = 1.0 if BAJA_PRE is not None else min(1.0, TRIS / tris_alta); mod.use_collapse_triangulate = True
if DIRECTO: mod.delimit = {'UV'}      # no colapsar a través de los límites de las islas UV
bpy.ops.object.modifier_apply(modifier='reducir')
if DIRECTO:
    mat0 = alta.data.materials[0]; mat0.name = 'M_' + NOM
    baja.data.materials.clear(); baja.data.materials.append(mat0)
    for n in mat0.node_tree.nodes:
        if n.type == 'TEX_IMAGE' and n.image is not None:
            im = n.image
            if im.size[0] > RES: im.scale(RES, RES)
            destino = os.path.join(ART, 'T_%s_BaseColor.jpg' % NOM)
            im.filepath_raw = destino; im.file_format = 'JPEG'; im.save()
            info['textura_color'] = os.path.basename(destino)
            break
    for resto in ('T_%s_BaseColor.png' % NOM, 'T_%s_Normal.png' % NOM):    # restos de horneados anteriores
        for ext in ('', '.meta'):
            if os.path.exists(os.path.join(ART, resto + ext)): os.remove(os.path.join(ART, resto + ext))
    bpy.data.objects.remove(alta, do_unlink=True)
    tris = sum(len(p.vertices) - 2 for p in baja.data.polygons)
    xy = np.array([(v.co.x, v.co.y) for v in baja.data.vertices])
    info.update({'triangulos': tris, 'directo': True, 'dimensiones': [round(d, 4) for d in baja.dimensions],
                 'radio_huella': round(float(np.hypot(xy[:, 0], xy[:, 1]).max()), 4)})
    bpy.ops.object.select_all(action='DESELECT'); baja.select_set(True); bpy.context.view_layer.objects.active = baja
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, NOM + '.blend'), compress=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(ART, 'SM_%s.fbx' % NOM), use_selection=True,
                             object_types={'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
                             apply_unit_scale=True, mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
    json.dump(info, open(os.path.join(SRC, 'validacion.json'), 'w'), indent=2)
    print('MAPA_OK', info)
    sys.exit(0)
baja.data.materials.clear()
mat = bpy.data.materials.new('M_' + NOM); mat.use_nodes = True
baja.data.materials.append(mat)
nt = mat.node_tree; bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')

def imagen(nombre, color):
    im = bpy.data.images.new(nombre, RES, RES, alpha=False)
    im.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    return im

col = imagen('T_%s_BaseColor' % NOM, True)
nor = imagen('T_%s_Normal' % NOM, False)
nodo = nt.nodes.new('ShaderNodeTexImage')

# --- horneado desde el original ---
sc = bpy.context.scene
sc.render.engine = 'CYCLES'; sc.cycles.device = 'GPU'
prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
for d in prefs.devices:
    d.use = True
sc.cycles.samples = 4
bk = sc.render.bake
bk.use_selected_to_active = True; bk.use_cage = False
bk.cage_extrusion = EXTRUSION; bk.max_ray_distance = RAYO; bk.margin = 12; bk.margin_type = 'EXTEND'
bpy.ops.object.select_all(action='DESELECT')
alta.select_set(True); baja.select_set(True); bpy.context.view_layer.objects.active = baja
for im, tipo in [(col, 'DIFFUSE')] + ([] if SIN_NORMAL else [(nor, 'NORMAL')]):
    nodo.image = im; nt.nodes.active = nodo
    if tipo == 'DIFFUSE':
        bk.use_pass_direct = False; bk.use_pass_indirect = False; bk.use_pass_color = True
        bpy.ops.object.bake(type='DIFFUSE')
    else:
        bk.normal_space = 'TANGENT'
        bpy.ops.object.bake(type='NORMAL')
    im.filepath_raw = os.path.join(ART, im.name + '.png'); im.file_format = 'PNG'; im.save()
    im.source = 'FILE'; im.filepath = im.filepath_raw; im.reload(); im.pack()

nodo.image = col
nt.links.new(nodo.outputs['Color'], bsdf.inputs['Base Color'])
if SIN_NORMAL:
    for resto in ('T_%s_Normal.png' % NOM, 'T_%s_Normal.png.meta' % NOM):   # evita que Unity use un normal de un horneado anterior
        if os.path.exists(os.path.join(ART, resto)): os.remove(os.path.join(ART, resto))
else:
    nn = nt.nodes.new('ShaderNodeTexImage'); nn.image = nor
    nm = nt.nodes.new('ShaderNodeNormalMap')
    nt.links.new(nn.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
bsdf.inputs['Roughness'].default_value = 0.9

bpy.data.objects.remove(alta, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.object.select_all(action='DESELECT'); baja.select_set(True); bpy.context.view_layer.objects.active = baja

tris = sum(len(p.vertices) - 2 for p in baja.data.polygons)
xy = np.array([(v.co.x, v.co.y) for v in baja.data.vertices])
info.update({'triangulos': tris, 'textura': RES, 'dimensiones': [round(d, 4) for d in baja.dimensions],
             'radio_huella': round(float(np.hypot(xy[:, 0], xy[:, 1]).max()), 4)})
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, NOM + '.blend'), compress=True)
bpy.ops.export_scene.fbx(filepath=os.path.join(ART, 'SM_%s.fbx' % NOM), use_selection=True,
                         object_types={'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
                         apply_unit_scale=True, mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
json.dump(info, open(os.path.join(SRC, 'validacion.json'), 'w'), indent=2)
print('MAPA_OK', info)
