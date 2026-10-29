"""Shared local bpy helpers for original Fulgor terrain studies."""
import math

import bpy
from mathutils import Vector


def material(name, color, emission=False):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color,1)
    result.use_nodes = True
    shader = result.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color,1)
    shader.inputs['Roughness'].default_value = .9
    if emission:
        shader.inputs['Emission Color'].default_value = (*color,1)
        shader.inputs['Emission Strength'].default_value = 1
    return result


def source_material(name, scale, bump_distance, strength=.35):
    result = material(name,(.2,.3,.05))
    nodes,links = result.node_tree.nodes,result.node_tree.links
    shader = nodes.get('Principled BSDF')
    attribute = nodes.new('ShaderNodeVertexColor')
    attribute.layer_name = 'ArtTint'
    geometry = nodes.new('ShaderNodeNewGeometry')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = scale
    noise.inputs['Detail'].default_value = 3
    links.new(geometry.outputs['Position'],noise.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].color = (.48,.43,.35,1)
    ramp.color_ramp.elements[1].color = (1,1,.94,1)
    links.new(noise.outputs['Fac'],ramp.inputs['Fac'])
    mix = nodes.new('ShaderNodeMixRGB')
    mix.blend_type = 'MULTIPLY'
    mix.inputs[0].default_value = strength
    links.new(attribute.outputs['Color'],mix.inputs[1])
    links.new(ramp.outputs['Color'],mix.inputs[2])
    links.new(mix.outputs['Color'],shader.inputs['Base Color'])
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .28
    bump.inputs['Distance'].default_value = bump_distance
    links.new(noise.outputs['Fac'],bump.inputs['Height'])
    links.new(bump.outputs['Normal'],shader.inputs['Normal'])
    return result


def tint_object(obj, color_function, mat):
    attr = obj.data.color_attributes.new(name='ArtTint',type='FLOAT_COLOR',domain='CORNER')
    for face in obj.data.polygons:
        for index in face.loop_indices:
            p = obj.data.vertices[obj.data.loops[index].vertex_index].co
            attr.data[index].color = (*color_function(p),1)
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def boundary(index, count, radius=.97):
    sector,fraction = divmod(index,count//6)
    t = fraction/(count//6)
    a,b = math.radians(30+sector*60),math.radians(90+sector*60)
    return (radius*((1-t)*math.cos(a)+t*math.cos(b)),
            radius*((1-t)*math.sin(a)+t*math.sin(b)))


def terrain(name, height_function, color_function, mat, count=72, rings=20, radius=.97):
    vertices = [(0,0,height_function(0,0))]
    for ring in range(1,rings+1):
        for i in range(count):
            x,y = boundary(i,count,radius)
            x,y = x*ring/rings,y*ring/rings
            vertices.append((x,y,height_function(x,y)))
    faces = [(0,1+i,1+(i+1)%count) for i in range(count)]
    for ring in range(rings-1):
        a,b = 1+ring*count,1+(ring+1)*count
        for i in range(count):
            j = (i+1)%count
            faces.extend(((a+i,b+i,b+j),(a+i,b+j,a+j)))
    top_faces = len(faces)
    bottom = len(vertices)
    vertices.extend((*boundary(i,count,radius),0) for i in range(count))
    outer = 1+(rings-1)*count
    for i in range(count):
        j = (i+1)%count
        faces.append((outer+i,bottom+i,bottom+j,outer+j))
    faces.append(tuple(range(bottom+count-1,bottom-1,-1)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.scene.collection.objects.link(obj)
    tint_object(obj,lambda p: color_function(p) if p.z > .001 else (.22,.10,.035),mat)
    # Sides are earth, not the vegetation color of the upper surface.
    attr = mesh.color_attributes['ArtTint']
    for face in mesh.polygons:
        face.use_smooth = face.index < top_faces
        if face.index >= top_faces:
            for index in face.loop_indices:
                attr.data[index].color = (.25,.105,.038,1)
    return obj


def join(parts,name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    return obj


def bake_export(obj,folder,resolution,bake_roughness=False,radius=.97,min_height=0,source_uv_to_remove=None):
    textures = folder/'Textures'
    textures.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name='UV_Atlas')
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(55),island_margin=.006)
    bpy.ops.object.mode_set(mode='OBJECT')
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 16
    scene.render.bake.margin = 3
    stem = obj.name.removeprefix('SM_').removesuffix('_LOD0')
    images = {}
    maps = [('DIFFUSE','BaseColor'),('NORMAL','Normal')]
    if bake_roughness:
        maps.append(('ROUGHNESS','Roughness'))
    for kind,suffix in maps:
        image = bpy.data.images.new('T_'+stem+'_'+suffix,resolution,resolution,alpha=False)
        if kind != 'DIFFUSE':
            image.colorspace_settings.name = 'Non-Color'
        for mat in obj.data.materials:
            node = mat.node_tree.nodes.new('ShaderNodeTexImage')
            node.image = image
            mat.node_tree.nodes.active = node
        if kind == 'DIFFUSE':
            bpy.ops.object.bake(type=kind,pass_filter={'COLOR'},use_clear=True)
        elif kind == 'NORMAL':
            bpy.ops.object.bake(type=kind,normal_space='TANGENT',use_clear=True)
        else:
            bpy.ops.object.bake(type=kind,use_clear=True)
        image.filepath_raw = str(textures/(image.name+'.png'))
        image.file_format = 'PNG'
        image.save()
        images[suffix] = image
    baked = material('M_'+stem,(.3,.3,.2))
    nodes,links = baked.node_tree.nodes,baked.node_tree.links
    shader = nodes.get('Principled BSDF')
    color = nodes.new('ShaderNodeTexImage')
    color.image = images['BaseColor']
    links.new(color.outputs['Color'],shader.inputs['Base Color'])
    normaltex = nodes.new('ShaderNodeTexImage')
    normaltex.image = images['Normal']
    normal = nodes.new('ShaderNodeNormalMap')
    normal.inputs['Strength'].default_value = .5
    links.new(normaltex.outputs['Color'],normal.inputs['Color'])
    links.new(normal.outputs['Normal'],shader.inputs['Normal'])
    if bake_roughness:
        roughness = nodes.new('ShaderNodeTexImage')
        roughness.image = images['Roughness']
        links.new(roughness.outputs['Color'],shader.inputs['Roughness'])
    for face in obj.data.polygons:
        face.material_index = 0
    obj.data.materials.clear()
    obj.data.materials.append(baked)
    # Rebakes may preserve an explicit source UV while packing a fresh output atlas.
    if source_uv_to_remove and source_uv_to_remove in obj.data.uv_layers:
        obj.data.uv_layers.remove(obj.data.uv_layers[source_uv_to_remove])
    points = [v.co for v in obj.data.vertices]
    assert all(abs(p.x) <= radius*math.sqrt(3)/2+.0001 and
               abs(p.x)+math.sqrt(3)*abs(p.y) <= math.sqrt(3)*radius+.0001 for p in points)
    assert min(p.z for p in points) >= min_height-.0001
    assert tuple(obj.location) == (0,0,0) and tuple(obj.scale) == (1,1,1)
    obj.data.calc_loop_triangles()
    report = {'name':obj.name,'triangles':len(obj.data.loop_triangles),
              'vertices':len(points),'materials':1,'texture_resolution':resolution,
              'bounds':[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)],
              'origin':list(obj.location),'scale':list(obj.scale),
              'texture_maps':list(images.keys())}
    bpy.ops.export_scene.fbx(filepath=str(folder/(obj.name+'.fbx')),use_selection=True,
                             object_types={'MESH'},global_scale=1,apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
                             add_leaf_bones=False,bake_anim=False,use_triangles=True,path_mode='RELATIVE')
    for image in images.values():
        image.pack()
    return report


def stage(scene,target,ortho,output):
    bpy.context.window.scene = scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015))
    bpy.context.object.name = 'PREVIEW_Suelo'
    bpy.context.object.data.materials.append(material('M_Fondo',(.042,.026,.014)))
    bpy.ops.object.camera_add(location=Vector(target)+Vector((0,-7,8)))
    camera = bpy.context.object
    camera.rotation_euler = (Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type,camera.data.ortho_scale = 'ORTHO',ortho
    scene.camera = camera
    for location,power,size in (((-3,-4,7),700,5),((4,2,5),160,4)):
        bpy.ops.object.light_add(type='AREA',location=location)
        lamp = bpy.context.object
        lamp.data.energy,lamp.data.size = power,size
        lamp.data.color = (1,.94,.84)
        lamp.rotation_euler = (-lamp.location).to_track_quat('-Z','Y').to_euler()
    scene.world = bpy.data.worlds.new(scene.name+'_World')
    scene.world.color = (.16,.16,.16)
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.render.resolution_x,scene.render.resolution_y = 1600,1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(output)
    scene.view_settings.view_transform,scene.view_settings.exposure = 'Standard',-.4
    return camera


def caption(camera,text,x,y,size,color=(.81,.78,.65),align='LEFT'):
    curve = bpy.data.curves.new('Preview_Text',type='FONT')
    curve.body,curve.size,curve.align_x = text,size,align
    obj = bpy.data.objects.new('PREVIEW_'+text[:16],curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent,obj.location = camera,(x,y,-7)
    curve.materials.append(material('M_Rotulo',color,True))


def load_objects(path,names):
    with bpy.data.libraries.load(str(path),link=False) as (_,loaded):
        loaded.objects = names
    if any(obj is None for obj in loaded.objects):
        raise ValueError('Required source meshes missing: '+str(path))
    return loaded.objects
