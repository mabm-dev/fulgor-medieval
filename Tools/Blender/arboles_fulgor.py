"""Árboles del mapa de campaña: encina, roble, pino piñonero y matorral, hechos por programa.

Cada copa es un puñado de tarjetas (cuadrados con una textura de ramillete de hojas recortada por alfa) repartidas sobre unos lóbulos, con las
normales apuntando hacia fuera de la copa para que la luz la modele como un volumen, y el color de vértice más oscuro por dentro y por abajo.
El tronco y las ramas son cilindros de seis lados con una tira de corteza del mismo atlas.

Uso:
  blender -b --python arboles_fulgor.py -- <carpeta_de_salida>
Salida: SM_Arboles.fbx (un objeto por árbol, con el pie en el origen) y T_Arboles_BaseColor.png (atlas con alfa).
"""
import bpy, sys, os, math
import numpy as np

OUT = sys.argv[sys.argv.index("--") + 1]
os.makedirs(OUT, exist_ok=True)
ATLAS = 1024
BARK = 128                      # tira de corteza, abajo
CELL_W, CELL_H = 512, (ATLAS - BARK) // 2
# celdas de follaje: (columna, fila) con la fila 0 justo encima de la corteza
CELLS = {"encina": (0, 0), "roble": (1, 0), "pino": (0, 1), "seco": (1, 1)}


def hsv(h, s, v):
    i = int(h * 6.0) % 6
    f = h * 6.0 - int(h * 6.0)
    p, q, t = v * (1 - s), v * (1 - f * s), v * (1 - (1 - f) * s)
    return [(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)][i]


def ramillete(rng, hojas, largo, ancho, tono, aguja=False):
    """Una celda de follaje: hojas pintadas de abajo arriba, las de arriba más claras."""
    w, h = CELL_W, CELL_H
    img = np.zeros((h, w, 4), dtype=np.float32)
    radio = min(w, h) * 0.47
    # ramitas de las que cuelgan las hojas: así el borde sale irregular y no un disco
    ramas = [(w / 2 + rng.normal(0, radio * 0.42), h / 2 + rng.normal(0, radio * 0.42)) for _ in range(9)]
    for i in range(hojas):
        bx, by = ramas[rng.integers(len(ramas))]
        cx, cy = bx + rng.normal(0, radio * 0.26), by + rng.normal(0, radio * 0.26)
        if math.hypot(cx - w / 2, cy - h / 2) > radio * 0.93:
            continue
        ang = rng.uniform(0, math.tau)
        L = largo * rng.uniform(0.7, 1.25)
        W = ancho * rng.uniform(0.7, 1.2)
        x0, x1 = int(max(0, cx - L - 2)), int(min(w, cx + L + 3))
        y0, y1 = int(max(0, cy - L - 2)), int(min(h, cy + L + 3))
        if x1 <= x0 or y1 <= y0:
            continue
        yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        u = (xx - cx) * math.cos(ang) + (yy - cy) * math.sin(ang)
        v = -(xx - cx) * math.sin(ang) + (yy - cy) * math.cos(ang)
        t = np.clip(u / L, -1, 1)
        perfil = W * (1 - t * t) if not aguja else W * (1 - np.abs(t) ** 4)
        cubre = np.clip(perfil - np.abs(v) + 0.6, 0, 1) * (np.abs(u) < L)
        fondo = i / hojas                               # 0 las de abajo, 1 las de arriba
        luz = 0.58 + 0.42 * fondo
        c = np.array(hsv(tono[0] + rng.normal(0, tono[3]), np.clip(tono[1] + rng.normal(0, 0.06), 0, 1), np.clip(tono[2] * rng.uniform(0.8, 1.2), 0, 1)))
        # un lado de la hoja más claro que el otro y el nervio marcado
        sombra = luz * (0.86 + 0.2 * np.clip(v / max(W, 1e-3), -1, 1)) * (1.0 + 0.16 * np.exp(-(v / max(W * 0.18, 0.4)) ** 2))
        zona = img[y0:y1, x0:x1]
        a = cubre[..., None]
        zona[..., :3] = zona[..., :3] * (1 - a) + (c[None, None, :] * sombra[..., None]) * a
        zona[..., 3:4] = np.maximum(zona[..., 3:4], a)
    # donde no hay hoja, el color medio: sin cercos oscuros al reducir la textura
    hay = img[..., 3] > 0.5
    medio = img[..., :3][hay].mean(axis=0) if hay.any() else np.array([0.2, 0.3, 0.1])
    img[..., :3] = np.where(img[..., 3:4] > 0.02, img[..., :3] / np.maximum(img[..., 3:4], 0.02) * np.minimum(img[..., 3:4] / 0.02, 1) + medio * (1 - np.minimum(img[..., 3:4] / 0.02, 1)), medio)
    img[..., :3] = np.where(img[..., 3:4] > 0.02, img[..., :3], medio)
    return np.clip(img, 0, 1)


def corteza(rng):
    w, h = ATLAS, BARK
    x = np.arange(w)[None, :]
    vetas = np.zeros((h, w), dtype=np.float32)
    for k in (7, 17, 41, 97):
        fase = rng.uniform(0, math.tau, size=(h, 1))
        fase = np.cumsum(rng.normal(0, 0.05, size=(h, 1)), axis=0) + rng.uniform(0, math.tau)
        vetas += np.sin(x * math.tau * k / w + fase) / math.sqrt(k)
    vetas = (vetas - vetas.min()) / (vetas.max() - vetas.min())
    grano = rng.uniform(0.9, 1.1, size=(h, w))
    base = np.array([0.36, 0.29, 0.22])
    img = np.ones((h, w, 4), dtype=np.float32)
    img[..., :3] = base[None, None, :] * (0.6 + 0.7 * vetas[..., None]) * grano[..., None]
    return np.clip(img, 0, 1)


def atlas():
    rng = np.random.default_rng(1086)
    img = np.zeros((ATLAS, ATLAS, 4), dtype=np.float32)
    img[:BARK] = corteza(rng)
    celdas = {
        "encina": ramillete(rng, 950, 17, 7.5, (0.235, 0.40, 0.42, 0.012)),
        "roble": ramillete(rng, 720, 22, 10, (0.25, 0.52, 0.52, 0.018)),
        "pino": ramillete(rng, 2800, 30, 1.8, (0.29, 0.46, 0.38, 0.012), aguja=True),
        "seco": ramillete(rng, 720, 20, 9, (0.12, 0.55, 0.62, 0.03)),
    }
    for nombre, (cx, cy) in CELLS.items():
        img[BARK + cy * CELL_H:BARK + (cy + 1) * CELL_H, cx * CELL_W:(cx + 1) * CELL_W] = celdas[nombre]
    imagen = bpy.data.images.new("T_Arboles_BaseColor", ATLAS, ATLAS, alpha=True)
    imagen.alpha_mode = "STRAIGHT"
    imagen.colorspace_settings.name = "sRGB"
    # con filepath_raw y save() Blender escribe los valores tal cual: van ya en sRGB
    lineal = img.copy()
    c = lineal[..., :3]
    lineal[..., :3] = c
    imagen.pixels.foreach_set(lineal.ravel())
    imagen.filepath_raw = os.path.join(OUT, "T_Arboles_BaseColor.png")
    imagen.file_format = "PNG"
    imagen.save()


def uv_celda(nombre, rng):
    cx, cy = CELLS[nombre]
    u0, u1 = cx * CELL_W / ATLAS, (cx + 1) * CELL_W / ATLAS
    v0, v1 = (BARK + cy * CELL_H) / ATLAS, (BARK + (cy + 1) * CELL_H) / ATLAS
    if rng.random() < 0.5:
        u0, u1 = u1, u0
    return u0, v0, u1, v1


class Malla:
    def __init__(self):
        self.v, self.n, self.c, self.caras, self.uv = [], [], [], [], []

    def vert(self, p, n, c):
        self.v.append(tuple(p)); self.n.append(tuple(n)); self.c.append(c)
        return len(self.v) - 1

    def cara(self, idx, uvs):
        self.caras.append(tuple(idx)); self.uv.append(uvs)


def norm(a):
    a = np.asarray(a, dtype=np.float64)
    return a / max(np.linalg.norm(a), 1e-9)


def tubo(m, puntos, radios, luz0, luz1, lados=6):
    """Cilindro que sigue unos puntos; la corteza da una vuelta por tramo de la tira."""
    anillos = []
    for i, (p, r) in enumerate(zip(puntos, radios)):
        eje = norm(np.array(puntos[min(i + 1, len(puntos) - 1)]) - np.array(puntos[max(i - 1, 0)]))
        lado = norm(np.cross(eje, (1, 0, 0))) if abs(eje[0]) < 0.9 else norm(np.cross(eje, (0, 1, 0)))
        otro = np.cross(eje, lado)
        t = i / max(len(puntos) - 1, 1)
        luz = luz0 + (luz1 - luz0) * t
        anillo = []
        for k in range(lados + 1):
            a = math.tau * k / lados
            d = lado * math.cos(a) + otro * math.sin(a)
            anillo.append(m.vert(np.array(p) + d * r, d, (luz, luz, luz)))
        anillos.append(anillo)
    for i in range(len(anillos) - 1):
        for k in range(lados):
            a, b, c, d = anillos[i][k], anillos[i][k + 1], anillos[i + 1][k + 1], anillos[i + 1][k]
            u0, u1 = k / lados, (k + 1) / lados
            v0, v1 = 0.02 + 0.08 * (i % 2), 0.02 + 0.08 * ((i + 1) % 2) if False else 0.11
            m.cara((a, b, c, d), ((u0, 0.01), (u1, 0.01), (u1, BARK / ATLAS - 0.01), (u0, BARK / ATLAS - 0.01)))


def copa(m, rng, lobulos, centro, celda, tarjetas, tinte, tam=1.0):
    centro = np.array(centro)
    zmin = min(c[2] - r[2] for c, r in lobulos)
    zmax = max(c[2] + r[2] for c, r in lobulos)
    for c, r in lobulos:
        c = np.array(c); r = np.array(r)
        for _ in range(tarjetas):
            # punto de la superficie del lóbulo, con preferencia por arriba y los lados (desde abajo no se mira)
            d = norm(rng.normal(size=3) + np.array([0, 0, 0.55]))
            p = c + d * r * rng.uniform(0.82, 1.05)
            fuera = norm(d / r)                       # normal del elipsoide
            cara_n = norm(fuera + rng.normal(0, 0.35, size=3))
            a = norm(np.cross(cara_n, rng.normal(size=3)))
            b = np.cross(cara_n, a)
            s = float(np.mean(r)) * rng.uniform(0.95, 1.4) * tam
            u0, v0, u1, v1 = uv_celda(celda, rng)
            esquinas = [(-1, -1, u0, v0), (1, -1, u1, v0), (1, 1, u1, v1), (-1, 1, u0, v1)]
            idx, uvs = [], []
            for sx, sy, uu, vv in esquinas:
                q = p + a * (sx * s) + b * (sy * s)
                n = norm(q - centro)
                n = norm(n + np.array([0, 0, 0.35]))  # un poco hacia arriba: copa iluminada desde el cielo
                alto = np.clip((q[2] - zmin) / max(zmax - zmin, 1e-6), 0, 1)
                hondo = np.clip(np.linalg.norm((q - c) / r), 0, 1.3) / 1.3
                luz = (0.52 + 0.5 * alto) * (0.8 + 0.22 * hondo) * rng.uniform(0.94, 1.06)
                idx.append(m.vert(q, n, tuple(np.clip(np.array(tinte) * luz, 0, 1))))
                uvs.append((uu, vv))
            m.cara(idx, uvs)


def encina(rng, tinte):
    m = Malla()
    alto = rng.uniform(0.40, 0.50)
    tronco_h = alto * rng.uniform(0.30, 0.38)
    inclina = rng.normal(0, 0.02, size=2)
    puntos = [(0, 0, 0), (inclina[0] * 0.5, inclina[1] * 0.5, tronco_h * 0.5), (inclina[0], inclina[1], tronco_h), (inclina[0] * 1.3, inclina[1] * 1.3, alto * 0.62)]
    tubo(m, puntos, [0.034, 0.026, 0.022, 0.012], 0.95, 0.5)
    ancho = alto * rng.uniform(0.50, 0.60)
    centro = (inclina[0], inclina[1], alto * 0.58)
    lob = []
    for k in range(rng.integers(5, 7)):
        a = math.tau * k / 5.5 + rng.uniform(-0.4, 0.4)
        d = ancho * rng.uniform(0.35, 0.62)
        c = (centro[0] + math.cos(a) * d, centro[1] + math.sin(a) * d, alto * rng.uniform(0.52, 0.68))
        rr = ancho * rng.uniform(0.42, 0.55)
        lob.append((c, (rr, rr, rr * rng.uniform(0.62, 0.78))))
        tubo(m, [puntos[2], ((c[0] + puntos[2][0]) / 2, (c[1] + puntos[2][1]) / 2, c[2] * 0.86), c], [0.016, 0.011, 0.005], 0.6, 0.4, lados=4)
    lob.append(((centro[0], centro[1], alto * 0.76), (ancho * 0.6, ancho * 0.6, ancho * 0.36)))
    copa(m, rng, lob, (centro[0], centro[1], alto * 0.42), "encina", 9, tinte)
    return m


def roble(rng, tinte, celda="roble"):
    m = Malla()
    alto = rng.uniform(0.50, 0.62)
    tronco_h = alto * rng.uniform(0.34, 0.42)
    inclina = rng.normal(0, 0.02, size=2)
    puntos = [(0, 0, 0), (inclina[0] * 0.5, inclina[1] * 0.5, tronco_h * 0.5), (inclina[0], inclina[1], tronco_h), (inclina[0] * 1.5, inclina[1] * 1.5, alto * 0.75)]
    tubo(m, puntos, [0.032, 0.025, 0.020, 0.008], 0.95, 0.45)
    ancho = alto * rng.uniform(0.36, 0.44)
    centro = (inclina[0], inclina[1], alto * 0.62)
    lob = []
    for k in range(rng.integers(5, 8)):
        a = math.tau * k / 5.0 + rng.uniform(-0.5, 0.5)
        d = ancho * rng.uniform(0.25, 0.6)
        z = alto * rng.uniform(0.45, 0.8)
        c = (centro[0] + math.cos(a) * d, centro[1] + math.sin(a) * d, z)
        rr = ancho * rng.uniform(0.42, 0.56)
        lob.append((c, (rr, rr, rr * rng.uniform(0.8, 1.0))))
        if k < 4:
            tubo(m, [puntos[2], ((c[0] + puntos[2][0]) / 2, (c[1] + puntos[2][1]) / 2, (c[2] + tronco_h) / 2), c], [0.014, 0.010, 0.004], 0.6, 0.4, lados=4)
    lob.append(((centro[0], centro[1], alto * 0.86), (ancho * 0.5, ancho * 0.5, ancho * 0.42)))
    copa(m, rng, lob, (centro[0], centro[1], alto * 0.48), celda, 8, tinte)
    return m


def pino(rng, tinte):
    m = Malla()
    alto = rng.uniform(0.62, 0.74)
    inclina = rng.normal(0, 0.035, size=2)
    tronco_h = alto * 0.74
    puntos = [(0, 0, 0), (inclina[0] * 0.3, inclina[1] * 0.3, tronco_h * 0.35), (inclina[0] * 0.8, inclina[1] * 0.8, tronco_h * 0.7), (inclina[0], inclina[1], tronco_h)]
    tubo(m, puntos, [0.030, 0.024, 0.019, 0.014], 1.0, 0.55)
    ancho = alto * rng.uniform(0.36, 0.44)
    centro = (inclina[0], inclina[1], alto * 0.84)
    lob = []
    for k in range(rng.integers(5, 7)):
        a = math.tau * k / 5.0 + rng.uniform(-0.4, 0.4)
        d = ancho * rng.uniform(0.3, 0.68)
        c = (centro[0] + math.cos(a) * d, centro[1] + math.sin(a) * d, alto * rng.uniform(0.8, 0.88))
        rr = ancho * rng.uniform(0.40, 0.52)
        lob.append((c, (rr, rr, rr * rng.uniform(0.36, 0.48))))
        tubo(m, [puntos[3], ((c[0] + puntos[3][0]) / 2, (c[1] + puntos[3][1]) / 2, c[2] - 0.02), (c[0], c[1], c[2] - 0.01)], [0.012, 0.009, 0.004], 0.55, 0.4, lados=4)
    lob.append(((centro[0], centro[1], alto * 0.9), (ancho * 0.55, ancho * 0.55, ancho * 0.26)))
    copa(m, rng, lob, (centro[0], centro[1], alto * 0.66), "pino", 9, tinte, tam=0.9)
    return m


def matorral(rng, tinte, celda):
    m = Malla()
    alto = rng.uniform(0.10, 0.15)
    lob = []
    for k in range(rng.integers(2, 4)):
        c = (rng.normal(0, 0.05), rng.normal(0, 0.05), alto * rng.uniform(0.45, 0.6))
        rr = alto * rng.uniform(0.6, 0.85)
        lob.append((c, (rr, rr, rr * 0.75)))
    copa(m, rng, lob, (0, 0, -alto * 0.2), celda, 8, tinte)
    return m


def objeto(nombre, m):
    malla = bpy.data.meshes.new(nombre)
    malla.from_pydata(m.v, [], m.caras)
    malla.update()
    capa = malla.uv_layers.new(name="UVMap")
    color = malla.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    normales = []
    for poly, uvs in zip(malla.polygons, m.uv):
        for k, li in enumerate(poly.loop_indices):
            vi = malla.loops[li].vertex_index
            capa.data[li].uv = uvs[k]
            c = m.c[vi]
            color.data[li].color_srgb = (c[0], c[1], c[2], 1.0)
    for li in range(len(malla.loops)):
        normales.append(m.n[malla.loops[li].vertex_index])
    malla.normals_split_custom_set(normales)
    obj = bpy.data.objects.new(nombre, malla)
    bpy.context.scene.collection.objects.link(obj)
    return obj


bpy.ops.wm.read_factory_settings(use_empty=True)
atlas()
rng = np.random.default_rng(20261110)
arboles = []
for i, tinte in enumerate([(1.0, 1.0, 1.0), (0.9, 0.95, 0.88), (1.06, 1.02, 0.9)]):
    arboles.append(objeto("Arbol_Encina_%s" % "ABC"[i], encina(rng, tinte)))
for i, tinte in enumerate([(1.0, 1.0, 1.0), (0.92, 1.0, 0.9), (1.05, 1.0, 0.86)]):
    arboles.append(objeto("Arbol_Roble_%s" % "ABC"[i], roble(rng, tinte)))
arboles.append(objeto("Arbol_Roble_Seco", roble(rng, (1.0, 1.0, 1.0), "seco")))
for i, tinte in enumerate([(1.0, 1.0, 1.0), (0.9, 0.96, 0.92), (1.04, 1.02, 0.94)]):
    arboles.append(objeto("Arbol_Pino_%s" % "ABC"[i], pino(rng, tinte)))
for i, (tinte, celda) in enumerate([((0.95, 1.0, 0.9), "encina"), ((1.0, 1.0, 1.0), "roble"), ((1.06, 1.0, 0.84), "encina")]):
    arboles.append(objeto("Mata_%s" % "ABC"[i], matorral(rng, tinte, celda)))

tris = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in arboles}
for o in arboles:
    o.select_set(True)
bpy.context.view_layer.objects.active = arboles[0]
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "SM_Arboles.fbx"), use_selection=True, object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                         global_scale=1.0, apply_unit_scale=True, mesh_smooth_type="OFF", colors_type="SRGB", path_mode="STRIP", bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "arboles.blend"))
print("ARBOLES_OK", tris)
