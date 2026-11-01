"""Retoques de la fortaleza de Castilla generada con Tripo (f3texturizado.glb, ya fusionada por fortaleza_mapa_ligera.py).

Uso: blender -b --python Tools/Blender/castilla_retoques.py -- [--salida=Castilla3] [--sin-merlones] [--sin-reconstruir]

1. Quita las banderas y astiles que Tripo generó rotos: borra todo lo que sobresale por encima del remate de cada torre.
2. Reconstruye los merlones de la torre del homenaje y de las cuatro torres de esquina con cajas de aristas vivas
   (material 'Almenas', con una textura de sillares generada con el color medio de los merlones originales).
3. Añade pendones limpios (astil + tela con la heráldica de Castilla), una bandera en lo alto de la torre del homenaje y un
   tapiz nuevo, nítido, sobre el borroso de la fachada (material 'Tapiz' con recorte alfa).
4. Aplana las cabezas de los merlones que no se reconstruyen (torres de la puerta y torretas) y endurece su sombreado.
Las coordenadas son las de ESTE modelo, en las unidades de la malla fusionada (huella de ~1 m, pivote en el centro, suelo en z = 0).
Materiales del FBX, en orden: 0 atlas de Tripo, 1 Pendon, 2 Tapiz, 3 Almenas.
"""
import json
import math
import os
import shutil
import sys
import bmesh
import bpy
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
opt = {a.split('=')[0]: (a.split('=')[1] if '=' in a else True) for a in args if a.startswith('--')}
SALIDA = opt.get('--salida', 'Castilla3')
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FUENTE = os.path.join(REPO, 'ArtSource', 'Blender', 'FortalezaMapaCastilla2', 'Fortaleza_Castilla_Castilla2.blend')
ART = os.path.join(REPO, 'Assets', 'Fulgor', 'Art', 'Buildings', 'FortalezaCastilla' + SALIDA)
SRC = os.path.join(REPO, 'ArtSource', 'Blender', 'FortalezaMapa' + SALIDA)
UNIF = os.path.join(REPO, 'Assets', 'Fulgor', 'Art', 'RawFortress', 'Unificada')
PENDON = os.path.join(UNIF, 'T_Fortaleza_Pendon_Albedo.png')
TAPIZ = os.path.join(UNIF, 'T_Fortaleza_Tapiz_Albedo.png')
NOM = 'Fortaleza_Castilla_' + SALIDA
os.makedirs(ART, exist_ok=True); os.makedirs(SRC, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=FUENTE)
obj = [o for o in bpy.data.objects if o.type == 'MESH'][0]
obj.name = NOM
me = obj.data
info = {'fuente': os.path.relpath(FUENTE, REPO).replace('\\', '/')}


def datos():
    co = np.empty(len(me.vertices) * 3, dtype=np.float32); me.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    tri = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get('vertex_index', tri); tri = tri.reshape(-1, 3)
    return co, tri


def borrar_caras(mask):
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[int(i)] for i in np.nonzero(mask)[0]], context='FACES')
    bm.to_mesh(me); bm.free(); me.update()


# ------------------------------------------------------------------ 1. quitar banderas y astiles
# (cx, cy, radio, altura del remate): se borra toda cara con algún vértice por encima del remate dentro del radio
TORRES = {
    'esquina_delantera_izq': (-0.372, -0.398, 0.060, 0.424),
    'puerta_izq': (-0.130, -0.388, 0.060, 0.335),
    'puerta_der': (0.122, -0.374, 0.060, 0.335),
}
co, tri = datos()
zmax = co[tri][:, :, 2].max(axis=1); cen = co[tri].mean(axis=1)
quitar = np.zeros(len(tri), bool)
for nombre, (cx, cy, r, techo) in TORRES.items():
    quitar |= (np.hypot(cen[:, 0] - cx, cen[:, 1] - cy) < r) & (zmax > techo)
# la bandera de la torre izquierda de la puerta dejó jirones fuera del círculo: se limpia también su caja completa
quitar |= (cen[:, 0] > -0.21) & (cen[:, 0] < -0.06) & (cen[:, 1] > -0.44) & (cen[:, 1] < -0.33) & (zmax > 0.335)
# restos de astiles que quedaron por debajo del remate: (cx, cy, radio, altura mínima)
for (rx, ry, rr, rz) in [(-0.391, -0.371, 0.010, 0.384), (-0.388, -0.377, 0.013, 0.374)]:
    quitar |= (np.hypot(cen[:, 0] - rx, cen[:, 1] - ry) < rr) & (zmax > rz)
info['caras_quitadas_banderas'] = int(quitar.sum())
borrar_caras(quitar)

# ------------------------------------------------------------------ materiales nuevos
img = [i for i in bpy.data.images if i.size[0] >= 1024][0]
W, H = img.size
px = np.empty(W * H * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(H, W, 4)[:, :, :3]
lum = px @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
oscuro = lum < 0.05
ker = oscuro.copy()
for dy in (-3, 0, 3):
    for dx in (-3, 0, 3):
        ker &= np.roll(np.roll(oscuro, dy, axis=0), dx, axis=1)
ys, xs = np.nonzero(ker)
UV_ASTIL = ((xs[len(xs) // 2] + 0.5) / W, (ys[len(xs) // 2] + 0.5) / H)     # un texel oscuro y liso del atlas (madera oscura)


def material(nombre, ruta_imagen, alfa=False):
    m = bpy.data.materials.new(nombre); m.use_nodes = True
    t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(ruta_imagen)
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    m.node_tree.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if alfa: m.node_tree.links.new(t.outputs['Alpha'], b.inputs['Alpha'])
    b.inputs['Roughness'].default_value = 0.85
    m.use_backface_culling = False
    me.materials.append(m)
    return len(me.materials) - 1


def ruido(w, h, celdas, rng):
    """Ruido de valores suave en [0, 1] de tamaño (h, w), con `celdas` = (columnas, filas) de la rejilla base."""
    g = rng.random((celdas[1] + 1, celdas[0] + 1)).astype(np.float32)
    ys = np.linspace(0, celdas[1], h); xs = np.linspace(0, celdas[0], w)
    y0 = np.clip(np.floor(ys).astype(int), 0, celdas[1] - 1); x0 = np.clip(np.floor(xs).astype(int), 0, celdas[0] - 1)
    fy = (ys - y0)[:, None].astype(np.float32); fx = (xs - x0)[None, :].astype(np.float32)
    fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
    return (g[y0][:, x0] * (1 - fx) * (1 - fy) + g[y0][:, x0 + 1] * fx * (1 - fy) +
            g[y0 + 1][:, x0] * (1 - fx) * fy + g[y0 + 1][:, x0 + 1] * fx * fy)


def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def envejecer_tapiz(origen, destino):
    """Tapiz usado: tejido, pliegues verticales, manchas y desteñido, chorreones desde arriba y borde inferior deshilachado."""
    im = bpy.data.images.load(origen); im.colorspace_settings.name = 'sRGB'
    w, h = im.size
    a = np.empty(w * h * 4, np.float32); im.pixels.foreach_get(a); a = a.reshape(h, w, 4)       # fila 0 = abajo
    rng = np.random.default_rng(11)
    v = np.linspace(0, 1, h, dtype=np.float32)[:, None]; u = np.linspace(0, 1, w, dtype=np.float32)[None, :]
    rgb, alfa = a[..., :3].copy(), a[..., 3].copy()
    lum = (rgb @ np.array([0.2126, 0.7152, 0.0722], np.float32))[..., None]
    # tejido fino y pliegues verticales que se acentúan hacia abajo
    tejido = 1 + 0.07 * (ruido(w, h, (w // 3, h // 3), rng) - 0.5) + 0.03 * np.sin(u * 2 * np.pi * (w / 6))[..., None].squeeze(-1) * np.sin(v * 2 * np.pi * (h / 6))
    desv = ruido(w, h, (3, 8), rng)
    pliegue = 1 - 0.30 * (0.5 + 0.5 * np.sin(2 * np.pi * (3.3 * u + 0.5 * desv))) * (0.35 + 0.65 * (1 - v))
    cuelga = 1 - 0.20 * smooth(v, 0.93, 1.0)                                       # sombra bajo la barra
    # manchas, desteñido y chorreones de suciedad
    mancha = smooth(ruido(w, h, (4, 9), rng), 0.45, 0.85)
    gris = lum * np.array([1.0, 0.92, 0.80], np.float32)
    rgb = rgb * (1 - 0.30 * mancha[..., None]) + gris * (0.30 * mancha[..., None])
    rgb = rgb * (1 - 0.05) + lum * 0.05                                              # desaturación general
    chorreo = smooth(ruido(w, h, (18, 3), rng), 0.55, 0.92) * (0.35 + 0.65 * v)
    suciedad = 1 - 0.38 * chorreo * (0.5 + 0.5 * ruido(w, h, (6, 14), rng))
    rgb = rgb * (tejido * pliegue * cuelga * suciedad * 1.14)[..., None]      # 1,14: compensa el sombreado para igualar la viveza del pendón
    # borde: se aclara el deshilachado y se come el alfa donde hay roce, sobre todo abajo
    er = alfa.copy()
    for d in (1, 2, 3, 4, 5, 6):
        for eje in (0, 1):
            for s in (d, -d):
                er = np.minimum(er, np.roll(alfa, s, axis=eje))
    borde = (alfa > 0.5) & (er < 0.5)
    ruido_fino = ruido(w, h, (w // 5, h // 5), rng)
    corte = borde & (ruido_fino > (0.62 - 0.22 * (1 - v)))
    alfa = np.where(corte, 0.0, alfa)
    rgb = np.where(borde[..., None], rgb * 0.78, rgb)
    salida = np.dstack([np.clip(rgb, 0, 1), alfa]).astype(np.float32)
    out = bpy.data.images.new('tapiz_viejo', w, h, alpha=True); out.colorspace_settings.name = 'sRGB'
    out.pixels.foreach_set(salida.ravel())
    out.filepath_raw = destino; out.file_format = 'PNG'; out.save()


TAPIZ_VIEJO = os.path.join(ART, 'T_%s_Tapiz.png' % NOM)
envejecer_tapiz(TAPIZ, TAPIZ_VIEJO)
IDX_PENDON = material('Pendon', PENDON)
IDX_TAPIZ = material('Tapiz', TAPIZ_VIEJO, alfa=True)

# ------------------------------------------------------------------ 2. merlones nuevos (cajas de aristas vivas)
# (nombre, cx, cy, semilado de la coronación)
CORONAS_NUEVAS = [('homenaje', 0.005, 0.015, 0.20), ('esq_der_del', 0.372, -0.398, 0.085), ('esq_izq_del', -0.372, -0.398, 0.085),
                  ('esq_der_tras', 0.372, 0.400, 0.085), ('esq_izq_tras', -0.372, 0.400, 0.085)]
ESCALA_TEX = 0.028         # lado en unidades del modelo de la textura de sillares
IDX_ALMENAS = None
cajas_nuevas = []
if '--sin-reconstruir' not in opt:
    bm0 = bmesh.new(); bm0.from_mesh(me)
    bvh = BVHTree.FromBMesh(bm0)
    PASO = 0.002
    borrar = np.zeros(len(me.polygons), bool)
    aplastar = []
    co, tri = datos(); cen = co[tri].mean(axis=1)
    colores = []
    for nombre, cx, cy, h in CORONAS_NUEVAS:
        xs_ = np.arange(cx - h, cx + h, PASO); ys_ = np.arange(cy - h, cy + h, PASO)
        G = np.full((len(ys_), len(xs_)), np.nan, np.float32)
        for j, y in enumerate(ys_):
            for i, x in enumerate(xs_):
                hit = bvh.ray_cast(Vector((x, y, 1.2)), Vector((0, 0, -1)))
                if hit[0] is not None: G[j, i] = hit[0].z
        cj, ci = int(len(ys_) / 2), int(len(xs_) / 2)
        suelo = float(np.nanmedian(G[cj - 4:cj + 5, ci - 4:ci + 5]))
        # remate de los merlones = altura más frecuente por encima del suelo (bins de 4 mm, suavizados)
        valores = G[(G > suelo + 0.020) & ~np.isnan(G)]
        if len(valores) == 0: print('CORONA', nombre, 'sin alturas'); continue
        bins = np.arange(suelo + 0.020, float(valores.max()) + 0.008, 0.004)
        hist, _ = np.histogram(valores, bins=bins)
        top_est = float(bins[int(np.argmax(np.convolve(hist, [1, 1], 'same')))] + 0.006)
        M = (G > top_est - 0.010) & (G < top_est + 0.012)
        torreta = (G > top_est + 0.012)                     # torretas y garitones: se respetan, con un margen de 8 mm
        for _ in range(4):
            t2 = torreta.copy(); t2[1:] |= torreta[:-1]; t2[:-1] |= torreta[1:]; t2[:, 1:] |= torreta[:, :-1]; t2[:, :-1] |= torreta[:, 1:]; torreta = t2
        M &= ~torreta
        # componentes conexas de M
        etiqueta = np.zeros(M.shape, np.int32); n_et = 0; comps = []
        for j0, i0 in zip(*np.nonzero(M)):
            if etiqueta[j0, i0]: continue
            n_et += 1; pila = [(j0, i0)]; etiqueta[j0, i0] = n_et; celdas = []
            while pila:
                a, b = pila.pop(); celdas.append((a, b))
                for da, db in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    c, d = a + da, b + db
                    if 0 <= c < M.shape[0] and 0 <= d < M.shape[1] and M[c, d] and not etiqueta[c, d]:
                        etiqueta[c, d] = n_et; pila.append((c, d))
            comps.append(np.array(celdas))
        utiles = []
        for c in comps:
            if len(c) < 6: continue
            x0, x1 = xs_[c[:, 1].min()], xs_[c[:, 1].max()] + PASO; y0, y1 = ys_[c[:, 0].min()], ys_[c[:, 0].max()] + PASO
            zt = float(np.percentile(G[c[:, 0], c[:, 1]], 90))
            utiles.append({'x0': x0, 'x1': x1, 'y0': y0, 'y1': y1, 'zt': zt, 'n': len(c)})
        if not utiles: print('CORONA', nombre, 'sin merlones'); continue
        ztop = float(np.median([u['zt'] for u in utiles]))
        # torretas: componentes claramente más altas o anchas que un merlon
        ancho_med = float(np.median([max(u['x1'] - u['x0'], u['y1'] - u['y0']) for u in utiles]))
        merlones = [u for u in utiles if u['zt'] < ztop + 0.015 and max(u['x1'] - u['x0'], u['y1'] - u['y0']) < 0.040]
        # los que son muy largos se parten en partes iguales
        lado_tipico = float(np.median([min(u['x1'] - u['x0'], u['y1'] - u['y0']) for u in merlones])) if merlones else 0.02
        final = []
        for u in merlones:
            dx, dy = u['x1'] - u['x0'], u['y1'] - u['y0']
            if max(dx, dy) > 1.7 * max(lado_tipico, 0.018):
                k = int(round(max(dx, dy) / max(lado_tipico * 1.15, 0.020))); k = max(k, 2)
                for s in range(k):
                    if dx >= dy: final.append(dict(u, x0=u['x0'] + dx * s / k, x1=u['x0'] + dx * (s + 1) / k))
                    else: final.append(dict(u, y0=u['y0'] + dy * s / k, y1=u['y0'] + dy * (s + 1) / k))
            else:
                final.append(u)
        # base de los merlones: suelo del adarve entre ellos (mediana de las celdas bajas del anillo)
        mf, mc = np.nonzero(M)      # filas = y, columnas = x
        bx0, bx1, by0, by1 = xs_[mc.min()], xs_[mc.max()], ys_[mf.min()], ys_[mf.max()]
        gx, gy = np.meshgrid(xs_, ys_)
        borde = np.minimum.reduce([gx - bx0, bx1 - gx, gy - by0, by1 - gy])
        anillo = (borde >= 0) & (borde < 0.030) & ~M & ~torreta & ~np.isnan(G) & (G > suelo + 0.004) & (G < ztop - 0.008)
        zbase = float(np.median(G[anillo])) if anillo.sum() else ztop - 0.022
        print('CORONA', nombre, 'suelo %.3f top %.3f base %.3f merlones %d (de %d) torretas %d' % (suelo, ztop, zbase, len(final), len(utiles), len(utiles) - len(merlones)))
        for u in final:
            u['zt'] = ztop; u['zb'] = zbase - 0.001
            cajas_nuevas.append(u)
        # se retira todo el parapeto viejo (merlones y dientes entre ellos) por encima del suelo del adarve, salvo las torretas
        ci_ = np.clip(((cen[:, 0] - xs_[0]) / PASO).astype(int), 0, len(xs_) - 1); cj_ = np.clip(((cen[:, 1] - ys_[0]) / PASO).astype(int), 0, len(ys_) - 1)
        banda = (cen[:, 0] > bx0 - 0.006) & (cen[:, 0] < bx1 + 0.006 + PASO) & (cen[:, 1] > by0 - 0.006) & (cen[:, 1] < by1 + 0.006 + PASO)
        interior = (cen[:, 0] > bx0 + 0.030) & (cen[:, 0] < bx1 - 0.030) & (cen[:, 1] > by0 + 0.030) & (cen[:, 1] < by1 - 0.030)
        borrar |= banda & ~interior & ~torreta[cj_, ci_] & (cen[:, 2] > zbase + 0.002) & (cen[:, 2] < ztop + 0.012)
        aplastar.append({'bx0': bx0, 'bx1': bx1, 'by0': by0, 'by1': by1, 'zbase': zbase, 'ztop': ztop, 'torreta': torreta.copy(), 'x0': xs_[0], 'y0': ys_[0], 'n': (len(xs_), len(ys_))})
        # color medio de los merlones originales (caras verticales por debajo de su cabeza)
        for u in final:
            m = (cen[:, 0] > u['x0']) & (cen[:, 0] < u['x1']) & (cen[:, 1] > u['y0']) & (cen[:, 1] < u['y1']) & (cen[:, 2] > zbase + 0.003) & (cen[:, 2] < ztop - 0.001)
            colores.append(m)
    bm0.free()
    # color medio sobre las caras de los merlones originales, tomado de la textura del atlas
    uvl = np.empty(len(me.loops) * 2, dtype=np.float32); me.uv_layers.active.data.foreach_get('uv', uvl); uvl = uvl.reshape(len(me.polygons), 3, 2).mean(axis=1)
    col = px[np.clip((uvl[:, 1] * H).astype(int), 0, H - 1), np.clip((uvl[:, 0] * W).astype(int), 0, W - 1)]
    sel = np.zeros(len(me.polygons), bool)
    for m in colores: sel |= m
    base_col = (np.median(col[sel], axis=0) if sel.sum() > 20 else np.array([0.42, 0.33, 0.21])) * 1.25   # algo más claro: los merlones originales llevan manchas
    info['color_merlon_lineal'] = [round(float(c), 3) for c in base_col]
    info['merlones_nuevos'] = len(cajas_nuevas)
    info['caras_borradas_merlones'] = int(borrar.sum())
    borrar_caras(borrar)
    co2, _t = datos(); nuevo_z = co2[:, 2].copy(); aplastados = 0
    for a in aplastar:
        ci = np.clip(((co2[:, 0] - a['x0']) / PASO).astype(int), 0, a['n'][0] - 1); cj = np.clip(((co2[:, 1] - a['y0']) / PASO).astype(int), 0, a['n'][1] - 1)
        banda_v = (co2[:, 0] > a['bx0'] - 0.006) & (co2[:, 0] < a['bx1'] + 0.006 + PASO) & (co2[:, 1] > a['by0'] - 0.006) & (co2[:, 1] < a['by1'] + 0.006 + PASO)
        interior_v = (co2[:, 0] > a['bx0'] + 0.030) & (co2[:, 0] < a['bx1'] - 0.030) & (co2[:, 1] > a['by0'] + 0.030) & (co2[:, 1] < a['by1'] - 0.030)
        alto_v = (co2[:, 2] > a['zbase'] + 0.0008) & (co2[:, 2] < a['ztop'] + 0.012)
        sel_v = banda_v & ~interior_v & ~a['torreta'][cj, ci] & alto_v
        nuevo_z[sel_v] = a['zbase']; aplastados += int(sel_v.sum())
    for v in me.vertices: v.co.z = float(nuevo_z[v.index])
    me.update(); info['vertices_aplastados'] = aplastados

    # textura de sillares con el color medio (se guarda como PNG sRGB)
    R = 512
    rng = np.random.default_rng(7)
    lin2srgb = lambda c: np.where(c <= 0.0031308, 12.92 * c, 1.055 * np.power(np.clip(c, 0, None), 1 / 2.4) - 0.055)
    tex = np.ones((R, R, 3), np.float32) * base_col[None, None, :]
    alto_f, ancho_b = 64, 128
    for fila in range(R // alto_f):
        desplaza = (fila % 2) * (ancho_b // 2)
        for b in range(-1, R // ancho_b + 1):
            x0 = b * ancho_b + desplaza; tono = 1.0 + rng.uniform(-0.10, 0.10)
            x0c, x1c = max(x0, 0), min(x0 + ancho_b, R)
            if x1c > x0c: tex[fila * alto_f:(fila + 1) * alto_f, x0c:x1c] *= tono
            if 0 <= x0 < R: tex[fila * alto_f:(fila + 1) * alto_f, max(x0 - 1, 0):x0 + 2] *= 0.62      # junta vertical
        tex[fila * alto_f:fila * alto_f + 3, :] *= 0.80                                                   # junta horizontal
    tex *= (1.0 + rng.normal(0, 0.035, (R, R, 1))).astype(np.float32)
    s = lin2srgb(np.clip(tex, 0, 1))
    im = bpy.data.images.new('almenas_tmp', R, R, alpha=False); im.colorspace_settings.name = 'sRGB'
    im.pixels.foreach_set(np.dstack([tex, np.ones((R, R, 1), np.float32)]).astype(np.float32).ravel())     # el espacio sRGB convierte al guardar
    ruta_alm = os.path.join(ART, 'T_%s_Almenas.png' % NOM)
    im.filepath_raw = ruta_alm; im.file_format = 'PNG'; im.save()
    IDX_ALMENAS = material('Almenas', ruta_alm)

# ------------------------------------------------------------------ 3. geometría nueva: merlones, pendones, bandera y tapiz
bm = bmesh.new(); bm.from_mesh(me)
uv_layer = bm.loops.layers.uv.active


def cara(vs, uvs, mat, plana=False):
    f = bm.faces.new(vs); f.material_index = mat; f.smooth = not plana
    for l, uv in zip(f.loops, uvs): l[uv_layer].uv = uv
    return f


def merlon(u, bisel=0.0015):
    x0, x1, y0, y1, zb, zt = u['x0'] + 0.0004, u['x1'] - 0.0004, u['y0'] + 0.0004, u['y1'] - 0.0004, u['zb'], u['zt']
    za = zt - bisel; k = 1.0 / ESCALA_TEX
    A = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    B = [(x0 + bisel, y0 + bisel), (x1 - bisel, y0 + bisel), (x1 - bisel, y1 - bisel), (x0 + bisel, y1 - bisel)]
    for i in range(4):
        (ax, ay), (bx, by), (cx_, cy_), (dx_, dy_) = A[i], A[(i + 1) % 4], B[(i + 1) % 4], B[i]
        along = lambda px_, py_: (px_ * abs(by - ay) / max(abs(by - ay), 1e-9) if False else (px_ if abs(bx - ax) > abs(by - ay) else py_)) * k
        v = [bm.verts.new((ax, ay, zb)), bm.verts.new((bx, by, zb)), bm.verts.new((bx, by, za)), bm.verts.new((ax, ay, za))]
        cara(v, [(along(ax, ay), zb * k), (along(bx, by), zb * k), (along(bx, by), za * k), (along(ax, ay), za * k)], IDX_ALMENAS, True)
        v = [bm.verts.new((ax, ay, za)), bm.verts.new((bx, by, za)), bm.verts.new((cx_, cy_, zt)), bm.verts.new((dx_, dy_, zt))]
        cara(v, [(along(ax, ay), za * k), (along(bx, by), za * k), (along(cx_, cy_), zt * k), (along(dx_, dy_), zt * k)], IDX_ALMENAS, True)
    v = [bm.verts.new((px_, py_, zt)) for (px_, py_) in B]
    cara(v, [(px_ * k, py_ * k) for (px_, py_) in B], IDX_ALMENAS, True)


for u in cajas_nuevas:
    merlon(u)


def astil(x, y, z0, alto, radio=0.0019, lados=6):
    base = [bm.verts.new((x + radio * math.cos(2 * math.pi * k / lados), y + radio * math.sin(2 * math.pi * k / lados), z0)) for k in range(lados)]
    cima = [bm.verts.new((v.co.x, v.co.y, z0 + alto)) for v in base]
    for k in range(lados):
        j = (k + 1) % lados
        cara([base[k], base[j], cima[j], cima[k]], [UV_ASTIL] * 4, 0)
    cara(list(reversed(cima)), [UV_ASTIL] * lados, 0)
    bola = bm.verts.new((x, y, z0 + alto + 0.004))
    for k in range(lados):
        j = (k + 1) % lados
        cara([cima[k], cima[j], bola], [UV_ASTIL] * 3, 0)


def tela(x, y, ztop, ancho, alto, seg=8, onda=0.005):
    """Rectángulo colgado del astil hacia +X con una onda suave; la textura ocupa toda la cara."""
    filas = []
    for z in (ztop, ztop - alto):
        filas.append([bm.verts.new((x + 0.002 + ancho * (s / seg), y + onda * math.sin((s / seg) * math.pi * 2.0) * (0.4 + 0.6 * (s / seg)), z)) for s in range(seg + 1)])
    for s in range(seg):
        u0, u1 = s / seg, (s + 1) / seg
        # orden de vértices tal que la normal mira hacia −Y (la cara de la puerta); al revés, Painter oculta la tela
        cara([filas[1][s], filas[1][s + 1], filas[0][s + 1], filas[0][s]], [(u0, 0), (u1, 0), (u1, 1), (u0, 1)], IDX_PENDON)


# (x, y, z del suelo de la torre, altura del astil, ancho y alto de la tela)
MASTILES = [(-0.130, -0.388, 0.285, 0.090, 0.068, 0.045),      # torre izquierda de la puerta
            (0.122, -0.374, 0.285, 0.090, 0.068, 0.045),       # torre derecha de la puerta
            (0.005, 0.015, 0.670, 0.130, 0.080, 0.052)]        # lo alto de la torre del homenaje
for (x, y, z0, alto, an, al) in MASTILES:
    astil(x, y, z0, alto)
    tela(x, y, z0 + alto - 0.005, an, al)
info['pendones'] = len(MASTILES)

# tapiz central de la fachada delantera de la torre del homenaje: lámina nítida delante del tapiz borroso original
TAPIZ_X, TAPIZ_Y, TAPIZ_ZTOP, TAPIZ_ALTO = 0.003, -0.157, 0.452, 0.188
tw = TAPIZ_ALTO * 512.0 / 1152.0 * 1.13
# tela con pliegues verticales que se marcan más abajo y el borde inferior ligeramente torcido (no cuelga perfectamente recta)
seg_u, seg_v = 10, 8
rejilla = []
for j in range(seg_v + 1):
    vv = j / seg_v                                            # 0 abajo, 1 arriba (donde cuelga de la barra)
    fila = []
    for i in range(seg_u + 1):
        uu = i / seg_u
        amplitud = 0.0010 + 0.0034 * (1 - vv)
        y = TAPIZ_Y - amplitud * (0.5 + 0.5 * math.sin(2 * math.pi * (2.6 * uu + 0.25 * (1 - vv) + 0.2)))
        x = TAPIZ_X - tw / 2 + tw * uu + 0.0045 * (1 - vv) ** 1.5 - 0.0012 * (1 - vv) * math.sin(5 * uu)
        z = TAPIZ_ZTOP - TAPIZ_ALTO + TAPIZ_ALTO * vv - 0.0035 * (1 - vv) * uu     # el borde de abajo cae algo hacia la derecha
        fila.append(bm.verts.new((x, y, z)))
    rejilla.append(fila)
for j in range(seg_v):
    for i in range(seg_u):
        cara([rejilla[j][i], rejilla[j][i + 1], rejilla[j + 1][i + 1], rejilla[j + 1][i]],
             [(i / seg_u, j / seg_v), ((i + 1) / seg_u, j / seg_v), ((i + 1) / seg_u, (j + 1) / seg_v), (i / seg_u, (j + 1) / seg_v)], IDX_TAPIZ)
# barra de madera de la que cuelga
barra = [(TAPIZ_X - tw / 2 - 0.004, TAPIZ_ZTOP + 0.001), (TAPIZ_X + tw / 2 + 0.004, TAPIZ_ZTOP + 0.005)]
x0b, x1b, zb0, zb1, yb0, yb1 = barra[0][0], barra[1][0], TAPIZ_ZTOP, TAPIZ_ZTOP + 0.005, TAPIZ_Y - 0.0045, TAPIZ_Y + 0.0015
for vs in ([(x0b, yb0, zb0), (x1b, yb0, zb0), (x1b, yb0, zb1), (x0b, yb0, zb1)], [(x1b, yb0, zb0), (x1b, yb1, zb0), (x1b, yb1, zb1), (x1b, yb0, zb1)],
           [(x0b, yb0, zb1), (x1b, yb0, zb1), (x1b, yb1, zb1), (x0b, yb1, zb1)], [(x0b, yb1, zb0), (x0b, yb0, zb0), (x0b, yb0, zb1), (x0b, yb1, zb1)]):
    cara([bm.verts.new(p) for p in vs], [UV_ASTIL] * 4, 0)
info['tapiz'] = {'x': TAPIZ_X, 'y': TAPIZ_Y, 'ancho': round(tw, 4), 'alto': TAPIZ_ALTO}

bm.normal_update(); bm.to_mesh(me); bm.free(); me.update()

# ------------------------------------------------------------------ 4. cabezas planas en las coronaciones que no se reconstruyen
# (cx, cy, semilado, z mínima de la franja alta): torretas del homenaje y torres de la puerta
CORONAS = [(0.005, 0.015, 0.20, 0.655), (-0.130, -0.388, 0.075, 0.290), (0.122, -0.374, 0.075, 0.290)]
if '--sin-merlones' not in opt:
    from mathutils import kdtree
    RADIO, ALTO_PLANO = 0.010, 0.0055
    kd = kdtree.KDTree(len(me.vertices))
    for v in me.vertices: kd.insert((v.co.x, v.co.y, 0.0), v.index)
    kd.balance()
    zs = np.array([v.co.z for v in me.vertices]); normales = np.array([v.normal.z for v in me.vertices])
    cx_ = np.array([v.co.x for v in me.vertices]); cy_ = np.array([v.co.y for v in me.vertices])
    en_corona = np.zeros(len(zs), bool)
    for (cx, cy, h, zmin) in CORONAS:
        en_corona |= (np.abs(cx_ - cx) < h) & (np.abs(cy_ - cy) < h) & (zs > zmin)
    nuevo = zs.copy(); movidos = 0
    for i in np.nonzero(en_corona & (normales > 0.35))[0]:
        vecinos = [j for (_, j, _) in kd.find_range((cx_[i], cy_[i], 0.0), RADIO)]
        top = zs[vecinos].max()
        if top - zs[i] < ALTO_PLANO and top > zs[i]:
            nuevo[i] = top; movidos += 1
    for v in me.vertices: v.co.z = float(nuevo[v.index])
    me.update()
    info['vertices_aplanados'] = movidos
    cc = np.array([p.center for p in me.polygons]); mats = np.array([p.material_index for p in me.polygons])
    en_c = np.zeros(len(cc), bool)
    for (cx, cy, h, zmin) in CORONAS:
        en_c |= (np.abs(cc[:, 0] - cx) < h) & (np.abs(cc[:, 1] - cy) < h) & (cc[:, 2] > zmin)
    for i in np.nonzero(en_c & (mats == 0))[0]: me.polygons[int(i)].use_smooth = False
    me.update()

# ------------------------------------------------------------------ 5. pendones de Tripo junto a la puerta: se reaviva su color
# Están en el muro delantero, a ambos lados de la puerta (x ≈ ±0,23). En la textura salen apagados y lavados:
# se sube la saturación del rojo y del oro, se oscurece un poco el rojo y se deja el relieve del tejido.
def color_referencia():
    """Rojo y oro medios (lineales) y su luminancia en la textura del pendón limpio: son el color de las cinco banderas."""
    r = bpy.data.images.load(PENDON); r.colorspace_settings.name = 'sRGB'
    p = np.empty(r.size[0] * r.size[1] * 4, np.float32); r.pixels.foreach_get(p); p = p.reshape(-1, 4)[:, :3]
    rojo = (p[:, 0] > 2.5 * p[:, 1]) & (p[:, 0] > 0.1)
    oro = (p[:, 0] > 0.4) & (p[:, 1] > 0.25) & (p[:, 2] < 0.35 * p[:, 0])
    W = np.array([0.2126, 0.7152, 0.0722], np.float32)
    return p[rojo].mean(axis=0), float((p[rojo] @ W).mean()), p[oro].mean(axis=0), float((p[oro] @ W).mean())


def reavivar_pendones_puerta():
    W_, H_ = img.size
    arr = np.empty(W_ * H_ * 4, np.float32); img.pixels.foreach_get(arr); arr = arr.reshape(H_, W_, 4)
    uvd = np.empty(len(me.loops) * 2, np.float32); me.uv_layers.active.data.foreach_get('uv', uvd); uvd = uvd.reshape(-1, 2)
    mascara = np.zeros((H_, W_), bool)
    for p in me.polygons:
        if p.material_index != 0 or len(p.vertices) != 3: continue
        c = p.center
        if not (abs(abs(c.x) - 0.230) < 0.027 and c.y < -0.368 and 0.140 < c.z < 0.222): continue
        t = uvd[p.loop_start:p.loop_start + 3]
        xs, ys = t[:, 0] * W_, t[:, 1] * H_
        x0, x1 = int(max(0, np.floor(xs.min()))), int(min(W_ - 1, np.ceil(xs.max())))
        y0, y1 = int(max(0, np.floor(ys.min()))), int(min(H_ - 1, np.ceil(ys.max())))
        den = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
        if abs(den) < 1e-9 or x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        l1 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / den
        l2 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / den
        mascara[y0:y1 + 1, x0:x1 + 1] |= (l1 >= -0.02) & (l2 >= -0.02) & (1 - l1 - l2 >= -0.02)
    c = arr[mascara][:, :3]
    lum = (c @ np.array([0.2126, 0.7152, 0.0722], np.float32))[:, None]
    rojo = ((c[:, 0] > 1.4 * c[:, 1]) & (c[:, 0] > 1.2 * c[:, 2]))[:, None]
    oro = ((c[:, 0] > 0.18) & (c[:, 1] > 0.55 * c[:, 0]) & (c[:, 2] < 0.75 * c[:, 1]))[:, None]
    ref_rojo, lum_rojo, ref_oro, lum_oro = color_referencia()
    nuevo = c.copy()
    relieve_r = np.clip(lum / max(float(lum[rojo[:, 0]].mean()), 1e-4), 0.82, 1.22) ** 0.8
    relieve_o = np.clip(lum / max(float(lum[oro[:, 0]].mean()), 1e-4), 0.85, 1.18) ** 0.8
    nuevo = np.where(rojo, ref_rojo[None, :] * relieve_r, nuevo)
    nuevo = np.where(oro, ref_oro[None, :] * relieve_o, nuevo)
    arr[mascara, :3] = np.clip(nuevo, 0, 1)
    img.pixels.foreach_set(arr.ravel()); img.update()
    return int(mascara.sum()), int(rojo.sum()), int(oro.sum())


info['pendones_puerta_texels'] = reavivar_pendones_puerta()

# ------------------------------------------------------------------ exportación
info['triangulos'] = sum(len(p.vertices) - 2 for p in me.polygons)
bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active = obj
for n in obj.data.materials[0].node_tree.nodes:
    if n.type == 'TEX_IMAGE' and n.image is not None:
        n.image.filepath_raw = os.path.join(ART, 'T_%s_BaseColor.jpg' % NOM); n.image.file_format = 'JPEG'; n.image.save()
        break
shutil.copyfile(PENDON, os.path.join(ART, 'T_%s_Pendon.png' % NOM))
bpy.ops.export_scene.fbx(filepath=os.path.join(ART, 'SM_%s.fbx' % NOM), use_selection=True, object_types={'MESH'},
                         axis_forward='-Z', axis_up='Y', global_scale=1.0, apply_unit_scale=True,
                         mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, NOM + '.blend'), compress=True)
json.dump(info, open(os.path.join(SRC, 'validacion.json'), 'w'), indent=2, default=float)
print('RETOQUES_OK', info)
