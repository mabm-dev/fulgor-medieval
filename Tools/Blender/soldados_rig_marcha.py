"""Riggea y anima la marcha de un grupo de soldados de Tripo (cinco soldados + escudos, lanzas y bandera por piezas).

Uso:
  blender -b --python Tools/Blender/soldados_rig_marcha.py -- modelo.glb NombreGrupo [CarpetaDeArte]

Las piezas 0 a 4 son los soldados; el resto (escudos, lanzas, bandera) se pega al soldado más cercano.
Si a un soldado le falta el tramo bajo de una pierna (Tripo lo deja flotando), se copia el de la otra pierna.
Cada soldado recibe un cuerpo y dos piernas de dos huesos; todos cuelgan de un solo esqueleto «Soldados»
con una acción «Marcha»: el fotograma 0 es la pose neutra (parados) y del 1 al 25 un ciclo en bucle de dos pasos. Exporta
Assets/Fulgor/Art/<CarpetaDeArte, por defecto Units>/<NombreGrupo>/SM_<NombreGrupo>.fbx
con el pivote en el centro de la huella y los pies en cero. La textura no se escribe: se reutiliza la ya importada.
Los soldados deben mirar a −Y.
"""
import math
import os
import sys

import bmesh
import bpy
from mathutils import Quaternion, Vector, kdtree

SOLDADOS = 5
FOTOGRAMAS = 24
AMPLITUD_PASO = math.radians(26)
FLEXION_RODILLA = math.radians(42)

args = sys.argv[sys.argv.index('--') + 1:]
fuente, nombre = os.path.abspath(args[0]), args[1]
carpeta = args[2] if len(args) > 2 else 'Units'
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
destino = os.path.join(REPO, 'Assets', 'Fulgor', 'Art', carpeta, nombre)
os.makedirs(destino, exist_ok=True)


def suave(x, a, b):
    """0 en a, 1 en b (a puede ser mayor que b)."""
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def media_xy(vs):
    return Vector((sum(v.x for v in vs) / len(vs), sum(v.y for v in vs) / len(vs)))


def pies(vs, zmin, h):
    """Divide en dos los vértices bajos (k-medias en planta): una pierna cada grupo."""
    bajos = [v for v in vs if v.z < zmin + .32 * h]
    orden = sorted(bajos, key=lambda v: v.x)
    a, b = orden[0].xy.copy(), orden[-1].xy.copy()
    A, B = bajos, []
    for _ in range(25):
        A = [v for v in bajos if (v.xy - a).length <= (v.xy - b).length]
        B = [v for v in bajos if (v.xy - a).length > (v.xy - b).length]
        if not A or not B:
            break
        a, b = media_xy(A), media_xy(B)
    return a, b, A, B


def altura(o):
    zs = [v.co.z for v in o.data.vertices]
    return min(zs), max(zs) - min(zs)


# --- importación ---------------------------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=fuente)
mallas = sorted((o for o in bpy.data.objects if o.type == 'MESH'), key=lambda o: int(o.name.rsplit('_', 1)[1]))
for o in mallas:
    mundo = o.matrix_world.copy()
    o.parent = None
    o.matrix_world = mundo
bpy.ops.object.select_all(action='DESELECT')
for o in mallas:
    o.select_set(True)
bpy.context.view_layer.objects.active = mallas[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for o in list(bpy.data.objects):
    if o.type == 'EMPTY':
        bpy.data.objects.remove(o)
cuerpos, accesorios = mallas[:SOLDADOS], mallas[SOLDADOS:]

# --- pierna que falta ----------------------------------------------------------------------------------------
for o in cuerpos:
    zmin, h = altura(o)
    vs = [v.co.copy() for v in o.data.vertices]
    a, b, A, B = pies(vs, zmin, h)
    flotante = [g for g in (A, B) if min(v.z for v in g) > zmin + .06 * h]
    if not flotante:
        continue
    muñon = flotante[0]
    sano = B if muñon is A else A
    s, g = media_xy(muñon), media_xy(sano)
    zcorte = min(v.z for v in muñon) + .015
    bm = bmesh.new()
    bm.from_mesh(o.data)
    caras = [f for f in bm.faces if all(v.co.z < zcorte and (v.co.xy - g).length < (v.co.xy - s).length for v in f.verts)]
    copia = bmesh.ops.duplicate(bm, geom=caras)
    bmesh.ops.translate(bm, verts=[e for e in copia['geom'] if isinstance(e, bmesh.types.BMVert)],
                        vec=Vector((s.x - g.x, s.y - g.y, 0)))
    bm.to_mesh(o.data)
    bm.free()
    print('PIERNA REPUESTA en', o.name, 'caras copiadas', len(caras))

# --- accesorios al soldado más cercano -----------------------------------------------------------------------
arboles = []
for o in cuerpos:
    t = kdtree.KDTree(len(o.data.vertices))
    for i, v in enumerate(o.data.vertices):
        t.insert(v.co, i)
    t.balance()
    arboles.append(t)
asignados = [[] for _ in cuerpos]
for o in accesorios:
    muestra = [v.co for v in o.data.vertices][::7]
    cerca = [sum(t.find(p)[2] for p in muestra) / len(muestra) for t in arboles]
    asignados[cerca.index(min(cerca))].append(o)
    print('ACCESORIO', o.name, '->', cuerpos[cerca.index(min(cerca))].name)

# --- huella al origen, pies en z = 0 -------------------------------------------------------------------------
todos = cuerpos + accesorios
xs = [v.co.x for o in todos for v in o.data.vertices]
ys = [v.co.y for o in todos for v in o.data.vertices]
zmin_global = min(v.co.z for o in cuerpos for v in o.data.vertices)
desplazo = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, zmin_global))
for o in todos:
    o.data.transform(__import__('mathutils').Matrix.Translation(-desplazo))

# --- datos de cada soldado -----------------------------------------------------------------------------------
soldados = []
for o in cuerpos:
    zmin, h = altura(o)
    vs = [v.co.copy() for v in o.data.vertices]
    a, b, A, B = pies(vs, zmin, h)
    centro = (a + b) / 2
    cadera = (b - a)
    lat = Vector((cadera.x, cadera.y, 0)).normalized()
    arriba = media_xy([v for v in vs if zmin + .55 * h < v.z < zmin + .85 * h])
    pelvis = media_xy([v for v in vs if zmin + .40 * h < v.z < zmin + .50 * h])
    f = Vector((arriba.x - pelvis.x, arriba.y - pelvis.y, 0))
    f -= lat * f.dot(lat)
    f = f.normalized() if f.length > 1e-4 else Vector((0, -1, 0))
    cadera_z, rodilla_z, tobillo_z = zmin + .52 * h, zmin + .28 * h, zmin + .07 * h
    piernas = []
    for lado, p in (('A', a), ('B', b)):
        cad = Vector((centro.x + (p.x - centro.x) * .45, centro.y + (p.y - centro.y) * .45, cadera_z))
        tob = Vector((p.x, p.y, tobillo_z))
        rod = cad.lerp(tob, (cadera_z - rodilla_z) / (cadera_z - tobillo_z)) + Vector((f.x, f.y, 0)) * .015
        piernas.append((lado, cad, rod, tob))
    soldados.append(dict(zmin=zmin, h=h, centro=centro, lat=lat, f=f, cadera_z=cadera_z, rodilla_z=rodilla_z, piernas=piernas))

# --- esqueleto -----------------------------------------------------------------------------------------------
datos_arm = bpy.data.armatures.new('Soldados')
arm = bpy.data.objects.new('Soldados', datos_arm)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
eb = datos_arm.edit_bones
raiz = eb.new('Raiz')
raiz.head, raiz.tail = (0, 0, 0), (0, 0, .05)
for i, s in enumerate(soldados):
    c = eb.new('S%d_Cuerpo' % i)
    c.head = (s['centro'].x, s['centro'].y, s['cadera_z'])
    c.tail = (s['centro'].x, s['centro'].y, s['cadera_z'] + .1 * s['h'])
    c.parent = raiz
    for lado, cad, rod, tob in s['piernas']:
        alta = eb.new('S%d_Pierna%s_Alta' % (i, lado))
        alta.head, alta.tail, alta.parent = cad, rod, c
        baja = eb.new('S%d_Pierna%s_Baja' % (i, lado))
        baja.head, baja.tail, baja.parent = rod, tob, alta
        baja.use_connect = True
bpy.ops.object.mode_set(mode='OBJECT')

# --- pesos y unión de accesorios -----------------------------------------------------------------------------
for i, o in enumerate(cuerpos):
    s = soldados[i]
    origen = o.vertex_groups.new(name='_cuerpo')
    origen.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    if asignados[i]:
        bpy.ops.object.select_all(action='DESELECT')
        for extra in asignados[i]:
            extra.select_set(True)
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.join()
    o.name = 'SM_%s_%d' % (nombre, i)
    o.data.name = o.name
    grupos = {n: o.vertex_groups.new(name=n) for n in ['S%d_Cuerpo' % i] + [
        'S%d_Pierna%s_%s' % (i, l, t) for l in 'AB' for t in ('Alta', 'Baja')]}
    origen = o.vertex_groups['_cuerpo']  # tras unir, el grupo es otro objeto interno
    idx_origen = origen.index
    for v in o.data.vertices:
        propio = any(g.group == idx_origen for g in v.groups)
        p = v.co
        if not propio:
            grupos['S%d_Cuerpo' % i].add([v.index], 1.0, 'REPLACE')
            continue
        # pierna más cercana (distancia al segmento cadera-tobillo)
        mejor, d_mejor = None, 1e9
        for lado, cad, rod, tob in s['piernas']:
            seg = tob - cad
            t = max(0.0, min(1.0, (p - cad).dot(seg) / seg.length_squared))
            d = (p - (cad + seg * t)).length
            if d < d_mejor:
                mejor, d_mejor = lado, d
        pierna = suave(p.z, s['cadera_z'] + .02 * s['h'], s['cadera_z'] - .10 * s['h'])
        pierna *= 1 - .7 * suave(d_mejor, .045, .10)
        baja = suave(p.z, s['rodilla_z'] + .03 * s['h'], s['rodilla_z'] - .03 * s['h'])
        reparto = ((('S%d_Pierna%s_Baja' % (i, mejor)), pierna * baja),
                   (('S%d_Pierna%s_Alta' % (i, mejor)), pierna * (1 - baja)),
                   (('S%d_Cuerpo' % i), 1 - pierna))
        for n, w in reparto:
            if w > 1e-4:
                grupos[n].add([v.index], w, 'REPLACE')
    o.vertex_groups.remove(origen)
    mod = o.modifiers.new('Esqueleto', 'ARMATURE')
    mod.object = arm
    o.parent = arm

# --- animación de marcha -------------------------------------------------------------------------------------
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
accion = bpy.data.actions.new('Marcha')
arm.animation_data_create().action = accion


def giro(hueso, eje, angulo):
    q_reposo = hueso.bone.matrix_local.to_quaternion()
    return q_reposo.inverted() @ Quaternion(eje, angulo) @ q_reposo


for i, s in enumerate(soldados):
    lat = s['lat']
    # signo con el que un giro positivo sobre «lat» lleva el pie hacia delante
    adelante = (Quaternion(lat, .3) @ Vector((0, 0, -1))).dot(Vector((s['f'].x, s['f'].y, 0)))
    signo = 1 if adelante > 0 else -1
    s['signo'] = signo
for fr in range(FOTOGRAMAS + 2):
    reposo = fr == 0  # fotograma 0: pose neutra
    t = max(fr - 1, 0) / FOTOGRAMAS
    w = 2 * math.pi * t
    for i, s in enumerate(soldados):
        cuerpo = arm.pose.bones['S%d_Cuerpo' % i]
        cuerpo.rotation_mode = 'QUATERNION'
        cuerpo.location = (0, 0 if reposo else .012 * s['h'] * abs(math.cos(w)), 0)
        cuerpo.keyframe_insert('location', frame=fr)
        cuerpo.rotation_quaternion = (1, 0, 0, 0)
        cuerpo.keyframe_insert('rotation_quaternion', frame=fr)
        for k, lado in enumerate('AB'):
            fase = 0 if k == 0 else math.pi
            paso = 0 if reposo else AMPLITUD_PASO * math.sin(w + fase)
            flexion = 0 if reposo else FLEXION_RODILLA * max(0.0, math.cos(w + fase))
            alta = arm.pose.bones['S%d_Pierna%s_Alta' % (i, lado)]
            baja = arm.pose.bones['S%d_Pierna%s_Baja' % (i, lado)]
            for hueso, ang in ((alta, s['signo'] * paso), (baja, -s['signo'] * flexion)):
                hueso.rotation_mode = 'QUATERNION'
                hueso.rotation_quaternion = giro(hueso, s['lat'], ang)
                hueso.keyframe_insert('rotation_quaternion', frame=fr)
bpy.context.scene.frame_start, bpy.context.scene.frame_end = 0, FOTOGRAMAS + 1
bpy.ops.object.mode_set(mode='OBJECT')

# --- exportación ---------------------------------------------------------------------------------------------
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
for o in bpy.data.objects:
    if o.type == 'MESH':
        o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(destino, 'SM_%s.fbx' % nombre), use_selection=True,
                         object_types={'ARMATURE', 'MESH'}, axis_forward='-Z', axis_up='Y', global_scale=1.0,
                         apply_unit_scale=True, mesh_smooth_type='FACE', path_mode='STRIP', add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
                         bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
                         bake_anim_simplify_factor=0.0)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, 'ArtSource', 'Blender', nombre + '.blend')) if '--guardar-blend' in sys.argv else None
print('LISTO', destino, 'soldados', len(soldados), 'huesos', len(datos_arm.bones))
