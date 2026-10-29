"""Local geometry and baking helpers derived from the original V01 tool."""
import bpy,bmesh,math,json,random
from mathutils import Vector
from fulgor_terrain_tools import material

class Mesh:

    def __init__(self):
        self.v = []
        self.f = []
        self.c = []
        self.m = []
        self.parts = []

    def add(self, verts, faces, color, kind='stone', part='structure'):
        start = len(self.v)
        self.v.extend([tuple(v) for v in verts])
        self.f.extend([tuple((start + i for i in f)) for f in faces])
        self.c.extend([color] * len(faces))
        self.m.extend([KINDS[kind]] * len(faces))
        self.parts.extend([part] * len(faces))

    def box(self, p, s, color, kind='stone', part='structure', rotate=0):
        x, y, z = p
        a, b, c = [n / 2 for n in s]
        verts = []
        for u, v, w in [(-a, -b, -c), (a, -b, -c), (a, b, -c), (-a, b, -c), (-a, -b, c), (a, -b, c), (a, b, c), (-a, b, c)]:
            verts.append((x + u * math.cos(rotate) - v * math.sin(rotate), y + u * math.sin(rotate) + v * math.cos(rotate), z + w))
        self.add(verts, [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)], color, kind, part)

    def ellipsoid(self, p, s, color, kind='skin', part='head', segments=12, rings=7):
        verts = [(p[0], p[1], p[2] - s[2])]
        for j in range(1, rings):
            a = -math.pi / 2 + math.pi * j / rings
            for i in range(segments):
                b = math.tau * i / segments
                verts.append((p[0] + s[0] * math.cos(a) * math.cos(b), p[1] + s[1] * math.cos(a) * math.sin(b), p[2] + s[2] * math.sin(a)))
        top = len(verts)
        verts.append((p[0], p[1], p[2] + s[2]))
        faces = [(0, 1 + (i + 1) % segments, 1 + i) for i in range(segments)]
        for j in range(rings - 2):
            a = 1 + j * segments
            b = a + segments
            for i in range(segments):
                k = (i + 1) % segments
                faces.append((a + i, a + k, b + k, b + i))
        faces.extend(((top, top - segments + i, top - segments + (i + 1) % segments) for i in range(segments)))
        self.add(verts, faces, color, kind, part)

    def rod(self, a, b, r, color, kind='metal', part='weapon', segments=8, end_radius=None):
        a, b = (Vector(a), Vector(b))
        axis = (b - a).normalized()
        u = axis.cross(Vector((0, 0, 1)))
        if u.length < 0.001:
            u = Vector((1, 0, 0))
        u.normalize()
        v = axis.cross(u)
        end_radius = r if end_radius is None else end_radius
        vertices = [p + rad * (u * math.cos(math.tau * i / segments) + v * math.sin(math.tau * i / segments)) for p, rad in ((a, r), (b, end_radius)) for i in range(segments)]
        faces = [(i, (i + 1) % segments, (i + 1) % segments + segments, i + segments) for i in range(segments)]
        faces.extend([tuple(range(segments - 1, -1, -1)), tuple(range(segments, 2 * segments))])
        self.add(vertices, faces, color, kind, part)

    def taper(self, p, bottom, top, height, color, kind='cloth', part='torso'):
        verts = []
        for z, w, d in ((p[2] - height / 2, *bottom), (p[2] + height / 2, *top)):
            verts.extend(((p[0] + x * w / 2, p[1] + y * d / 2, z) for x, y in [(-1, -1), (1, -1), (1, 1), (-1, 1)]))
        self.add(verts, [(0, 3, 2, 1), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7), (4, 5, 6, 7)], color, kind, part)

    def cloth(self, origin, width, length, color, part='cape', vertical=True):
        verts = []
        cols = 10
        rows = 10
        for side in (-1, 1):
            for j in range(rows + 1):
                t = j / rows
                for i in range(cols + 1):
                    u = i / cols
                    if vertical:
                        verts.append((origin[0] + (u - 0.5) * width * (0.7 + 0.3 * t), origin[1] + 0.045 * t + 0.008 * math.sin(u * math.tau * 3 + t) * t + side * 0.0007, origin[2] - length * t))
                    else:
                        verts.append((origin[0] + width * u, origin[1] + 0.018 * math.sin(u * math.pi * 3 + j * 0.15) + side * 0.0007, origin[2] - length * t + 0.012 * math.sin(u * 5)))
        layer = (cols + 1) * (rows + 1)
        faces = []
        for j in range(rows):
            for i in range(cols):
                a = j * (cols + 1) + i
                b = a + cols + 1
                faces.extend(((a, a + 1, b + 1, b), (a + layer, b + layer, b + 1 + layer, a + 1 + layer)))
        border = list(range(cols + 1)) + [j * (cols + 1) + cols for j in range(1, rows + 1)] + [rows * (cols + 1) + i for i in range(cols - 1, -1, -1)] + [j * (cols + 1) for j in range(rows - 1, 0, -1)]
        for a, b in zip(border, border[1:] + border[:1]):
            faces.append((a, b, b + layer, a + layer))
        self.add(verts, faces, color, 'cloth', part)

    def append(self, other, offset=(0, 0, 0), scale=1):
        start = len(self.v)
        self.v.extend((tuple(Vector(v) * scale + Vector(offset)) for v in other.v))
        self.f.extend((tuple((start + i for i in f)) for f in other.f))
        self.c.extend(other.c)
        self.m.extend(other.m)
        self.parts.extend(other.parts)

    def object(self, name):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.v, [], self.f)
        mesh.update()
        attr = mesh.color_attributes.new(name='ArtTint', type='FLOAT_COLOR', domain='CORNER')
        part_attr = mesh.attributes.new(name='ArtPart', type='INT', domain='FACE')
        part_ids = {name: i for i, name in enumerate(dict.fromkeys(self.parts))}
        for polygon, color, mat_index, part in zip(mesh.polygons, self.c, self.m, self.parts):
            polygon.material_index = mat_index
            part_attr.data[polygon.index].value = part_ids[part]
            for index in polygon.loop_indices:
                attr.data[index].color = (*color, 1)
            polygon.use_smooth = part in ('head', 'horse', 'hand', 'hair', 'limb', 'torso', 'skirt') or part.startswith('boot')
        for mat in MATS:
            mesh.materials.append(mat)
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name, mesh)
        scene.collection.objects.link(obj)
        for part, index in part_ids.items():
            group = obj.vertex_groups.new(name=part)
            indices = set((i for poly in mesh.polygons if mesh.attributes['ArtPart'].data[poly.index].value == index for i in poly.vertices))
            group.add(list(indices), 1, 'REPLACE')
        obj['source_part_names'] = json.dumps(part_ids)
        return obj

def fortress_layout():
    m = Mesh()
    rng = random.Random(8401)
    m.box((0, 0, 0.012), (1.13, 1.05, 0.024), DARKSTONE)

    def wall(cx, cy, length, height, axis='x', thickness=0.072, base=0.024):
        courses = max(1, round(height / 0.038))
        course_h = height / courses
        for row in range(courses):
            n = max(1, round(length / 0.09))
            offset = 0.5 if row % 2 else 0
            cuts = [-length / 2] + [-length / 2 + (i + offset) * length / n for i in range(1, n + 1) if -length / 2 < -length / 2 + (i + offset) * length / n < length / 2] + [length / 2]
            for a, b in zip(cuts, cuts[1:]):
                p = (cx + (a + b) / 2, cy, base + (row + 0.5) * course_h) if axis == 'x' else (cx, cy + (a + b) / 2, base + (row + 0.5) * course_h)
                size = (b - a - 0.0013, thickness, course_h - 0.0011) if axis == 'x' else (thickness, b - a - 0.0013, course_h - 0.0011)
                k = rng.uniform(0.88, 1.1)
                m.box(p, size, tuple((v * k for v in STONE)))
        for i in range(max(2, int(length / 0.096))):
            u = -length / 2 + 0.032 + i * (length - 0.064) / max(1, int(length / 0.096) - 1)
            p = (cx + u, cy, base + height + 0.025) if axis == 'x' else (cx, cy + u, base + height + 0.025)
            m.box(p, (0.044, thickness + 0.008, 0.05) if axis == 'x' else (thickness + 0.008, 0.044, 0.05), LIGHTSTONE)
    wall(-0.327, -0.415, 0.31, 0.32)
    wall(0.327, -0.415, 0.31, 0.32)
    spring = 0.177
    inner = 0.125
    outer = 0.171
    segments = 13
    for i in range(segments):
        a = i * math.pi / segments + 0.007
        b = (i + 1) * math.pi / segments - 0.007
        verts = [(r * math.cos(t), y, spring + r * math.sin(t)) for y in (-0.458, -0.372) for r, t in [(inner, a), (inner, b), (outer, b), (outer, a)]]
        m.add(verts, [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], LIGHTSTONE)
    for side in (-1, 1):
        wall(side * 0.145, -0.415, 0.039, 0.151, thickness=0.09)
    m.box((0, -0.415, 0.337), (0.352, 0.087, 0.035), LIGHTSTONE)
    for i in (-1, 0, 1):
        m.box((i * 0.113, -0.415, 0.384), (0.051, 0.087, 0.057), LIGHTSTONE)
    for side in (-1, 1):
        for j in range(5):
            m.box((side * (0.112 - j * 0.015), -0.365 + j * 0.011, 0.091), (0.014, 0.013, 0.132), WOOD, 'wood', 'gate', side * 0.2)
        for z in (0.055, 0.13):
            m.box((side * 0.084, -0.349, z), (0.08, 0.009, 0.008), DARKMETAL, 'metal', 'gate')
    wall(0, 0.414, 0.95, 0.32)
    wall(-0.476, 0, 0.83, 0.32, 'y')
    wall(0.476, 0, 0.83, 0.32, 'y')
    for x in (-0.46, 0.46):
        for y in (-0.401, 0.401):
            for row in range(12):
                z = 0.024 + (row + 0.5) * 0.039
                for axis in ('x', 'y'):
                    for side in (-1, 1):
                        for j in range(3):
                            d = (j - 1) * 0.054
                            col = tuple((v * rng.uniform(0.91, 1.1) for v in STONE))
                            m.box((x + d, y + side * 0.078, z) if axis == 'x' else (x + side * 0.078, y + d, z), (0.052, 0.032, 0.037) if axis == 'x' else (0.032, 0.052, 0.037), col)
            m.box((x, y, 0.489), (0.194, 0.194, 0.022), LIGHTSTONE)
            m.box((x, y, 0.491), (0.124, 0.124, 0.01), DARKSTONE)
            for side in (-1, 1):
                for t in (-1, 0, 1):
                    m.box((x + t * 0.071, y + side * 0.077, 0.527), (0.039, 0.038, 0.056), LIGHTSTONE)
                    if t == 0:
                        m.box((x + side * 0.077, y, 0.527), (0.038, 0.039, 0.056), LIGHTSTONE)
            m.box((x, y - 0.096, 0.341), (0.009, 0.002, 0.055), (0.06, 0.045, 0.025))
    for row in range(18):
        z = 0.024 + (row + 0.5) * 0.037
        for side in (-1, 1):
            for i in range(5):
                k = rng.uniform(0.91, 1.12)
                m.box((0.07 + (i - 2) * 0.073, 0.168 + side * 0.164, z), (0.071, 0.035, 0.035), tuple((v * k for v in LIGHTSTONE)))
            for i in range(4):
                m.box((0.07 + side * 0.177, 0.168 + (i - 1.5) * 0.082, z), (0.033, 0.08, 0.035), STONE)
    m.box((0.07, 0.168, 0.702), (0.43, 0.386, 0.028), LIGHTSTONE)
    m.box((0.07, 0.168, 0.718), (0.34, 0.29, 0.008), DARKSTONE)
    for side in (-1, 1):
        for j in range(5):
            m.box((0.07 + (j - 2) * 0.09, 0.168 + side * 0.17, 0.755), (0.045, 0.045, 0.08), LIGHTSTONE)
        for j in range(3):
            m.box((0.07 + side * 0.19, 0.168 + (j - 1) * 0.11, 0.755), (0.045, 0.045, 0.08), LIGHTSTONE)
    for x in (-0.08, 0.07, 0.22):
        m.box((x, -0.017, 0.56), (0.019, 0.004, 0.069), (0.055, 0.04, 0.025))
        for dx in (-0.016, 0.016):
            m.box((x + dx, -0.021, 0.56), (0.008, 0.008, 0.078), STONE)
    for j in range(6):
        m.box((0.07, -0.047 - j * 0.026, 0.027 + j * 0.009), (0.115, 0.03, 0.02), STONE)
    m.box((0.07, -0.019, 0.112), (0.083, 0.006, 0.137), WOOD, 'wood', 'gate')
    for j in range(5):
        m.box((0.038 + j * 0.016, -0.024, 0.112), (0.002, 0.003, 0.135), DARKMETAL, 'metal', 'gate')
    for x in (-0.067, 0.207):
        m.rod((x, -0.09, 0.438), (x, -0.09, 0.311), 0.003, WOOD, 'wood', 'bannerpole', 6)
    m.cloth((0.07, -0.095, 0.444), 0.105, 0.116, RED, 'banner')
    castle_glyph(m, 0.07, -0.11, 0.382, 0.055)
    for x in range(9):
        for y in range(5):
            m.box((-0.3 + x * 0.072, -0.29 + y * 0.06, 0.03), (0.069, 0.057, 0.009), tuple((v * rng.uniform(0.9, 1.1) for v in DARKSTONE)))
    m.box((-0.29, 0.14, 0.112), (0.18, 0.32, 0.17), STONE)
    for side in (-1, 1):
        for row in range(4):
            for col in range(8):
                m.box((-0.29 + side * (row + 0.5) * 0.025, -0.014 + col * 0.043, 0.255 - row * 0.012), (0.029, 0.041, 0.011), (0.54 + rng.random() * 0.1, 0.15, 0.045), 'stone', rotate=0)
    return m

def bake(obj, resolution):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.data.uv_layers.new(name='UV_Atlas')
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.00015 if 'Fortaleza' in obj.name else 0.004)
    bpy.ops.object.mode_set(mode='OBJECT')
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 12
    scene.render.bake.margin = 4
    images = {}
    textures = EXPORT / 'Textures'
    textures.mkdir(exist_ok=True)
    stem = obj.name.removeprefix('SM_').removesuffix('_LOD0')
    for channel in ('BaseColor', 'Normal', 'Roughness', 'Metallic'):
        image = bpy.data.images.new('T_' + stem + '_' + channel, resolution, resolution, alpha=False)
        if 'Fortaleza' in obj.name:
            image.generated_color={'BaseColor':(.40,.30,.20,1),'Normal':(.5,.5,1,1),'Roughness':(.84,.84,.84,1),'Metallic':(0,0,0,1)}[channel]
        if channel != 'BaseColor':
            image.colorspace_settings.name = 'Non-Color'
        for mat in obj.data.materials:
            tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
            tex.image = image
            mat.node_tree.nodes.active = tex
        if channel == 'Metallic':
            saved = []
            for mat in obj.data.materials:
                nodes, links = (mat.node_tree.nodes, mat.node_tree.links)
                shader = nodes.get('Principled BSDF')
                output = next((n for n in nodes if n.type == 'OUTPUT_MATERIAL'))
                old = output.inputs['Surface'].links[0].from_socket
                emit = nodes.new('ShaderNodeEmission')
                value = shader.inputs['Metallic'].default_value
                emit.inputs['Color'].default_value = (value, value, value, 1)
                links.new(emit.outputs[0], output.inputs['Surface'])
                saved.append((mat, output, old, emit))
            bpy.ops.object.bake(type='EMIT', use_clear=('Fortaleza' not in obj.name))
            for mat, output, old, emit in saved:
                mat.node_tree.links.new(old, output.inputs['Surface'])
                mat.node_tree.nodes.remove(emit)
        elif channel == 'BaseColor':
            bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, use_clear=('Fortaleza' not in obj.name))
        elif channel == 'Normal':
            bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', use_clear=('Fortaleza' not in obj.name))
        else:
            bpy.ops.object.bake(type='ROUGHNESS', use_clear=('Fortaleza' not in obj.name))
        image.filepath_raw = str(textures / (image.name + '.png'))
        image.file_format = 'PNG'
        image.save()
        image.pack()
        images[channel] = image
    mat = material('M_' + stem, (0.3, 0.3, 0.3))
    nodes, links = (mat.node_tree.nodes, mat.node_tree.links)
    shader = nodes.get('Principled BSDF')
    for channel, socket in [('BaseColor', 'Base Color'), ('Roughness', 'Roughness'), ('Metallic', 'Metallic')]:
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = images[channel]
        links.new(tex.outputs['Color'], shader.inputs[socket])
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = images['Normal']
    normal = nodes.new('ShaderNodeNormalMap')
    links.new(tex.outputs['Color'], normal.inputs['Color'])
    links.new(normal.outputs['Normal'], shader.inputs['Normal'])
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.material_index = 0
    bpy.ops.export_scene.fbx(filepath=str(EXPORT / (obj.name + '.fbx')), use_selection=True, object_types={'MESH'}, global_scale=1, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False, use_triangles=True, path_mode='RELATIVE')
    obj.data.calc_loop_triangles()
    points = [v.co for v in obj.data.vertices]
    bounds = [[min((p[i] for p in points)), max((p[i] for p in points))] for i in range(3)]
    assert bounds[2][0] >= -0.0001 and tuple(obj.location) == (0, 0, 0) and (tuple(obj.scale) == (1, 1, 1))
    assert max((abs(p.x) for p in points)) < 0.68 and max((abs(p.y) for p in points)) < 0.68
    return dict(name=obj.name, triangles=len(obj.data.loop_triangles), vertices=len(points), materials=1, uv_channels=1, bounds=bounds, origin=list(obj.location), scale=list(obj.scale), texture_resolution=resolution, texture_maps=list(images), pose='static', rigged=False)



