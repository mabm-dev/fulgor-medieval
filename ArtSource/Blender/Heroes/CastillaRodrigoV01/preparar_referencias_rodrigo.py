"""Build a native Blender reference setup from the current Rodrigo art.

Reference planes and orthographic cameras only. No character mesh/export.
"""
from pathlib import Path
import json
import bpy
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[3]
CONCEPT=ROOT/'ArtSource/Concept/CastillaRodrigoV01'
IMAGE_PATH=CONCEPT/'Rodrigo_castilla_vistas_v01.png'
ORIGINAL=ROOT/'ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png'
bpy.ops.wm.read_factory_settings(use_empty=True)
image=bpy.data.images.load(str(IMAGE_PATH));image.pack()
original=bpy.data.images.load(str(ORIGINAL));original.pack();original.use_fake_user=True
mat=bpy.data.materials.new('M_Referencia_Rodrigo');mat.use_nodes=True
nodes,links=mat.node_tree.nodes,mat.node_tree.links
nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');emission=nodes.new('ShaderNodeEmission')
tex=nodes.new('ShaderNodeTexImage');tex.image=image
links.new(tex.outputs['Color'],emission.inputs['Color']);links.new(emission.outputs[0],out.inputs[0])

GROUND_V=.075;TOP_V=.980;PROPOSED_HEIGHT=1.80
BOARD_HEIGHT=PROPOSED_HEIGHT/(TOP_V-GROUND_V)
views={'Frente':(0,.3555),'Perfil':(.3555,.6445),'Espalda':(.6445,1)}
manifest={'source_portrait':str(ORIGINAL.relative_to(ROOT)),'reference_sheet':str(IMAGE_PATH.relative_to(ROOT)),
          'reference_type':'generated 2D, not calibrated orthographic scans','authoring_height_m_provisional':PROPOSED_HEIGHT,
          'contains_character_mesh':False,'views':views}

def board(scene,name,u0,u1,x=0,axis='front',behind=.08):
    width=(u1-u0)*image.size[0]/image.size[1]*BOARD_HEIGHT
    z0=-GROUND_V*BOARD_HEIGHT;z1=z0+BOARD_HEIGHT
    if axis=='front':verts=[(x-width/2,behind,z0),(x+width/2,behind,z0),(x+width/2,behind,z1),(x-width/2,behind,z1)]
    elif axis=='side':verts=[(-behind,width/2,z0),(-behind,-width/2,z0),(-behind,-width/2,z1),(-behind,width/2,z1)]
    else:verts=[(width/2,-behind,z0),(-width/2,-behind,z0),(-width/2,-behind,z1),(width/2,-behind,z1)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[(0,1,2,3)]);mesh.update()
    uv=mesh.uv_layers.new(name='UV_Referencia')
    for loop,coord in zip(uv.data,[(u0,0),(u1,0),(u1,1),(u0,1)]):loop.uv=coord
    mesh.materials.append(mat)
    obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
    obj['purpose']='REFERENCE_ONLY_NOT_GAME_GEOMETRY';obj.hide_select=True
    return obj

def camera(scene,name,position,target,ortho):
    data=bpy.data.cameras.new(name);data.type='ORTHO';data.ortho_scale=ortho
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj)
    obj.location=position;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj

presentation=bpy.context.scene;presentation.name='01_Lamina_de_referencia'
presentation.unit_settings.system='METRIC';presentation.unit_settings.scale_length=1
for index,(name,(u0,u1)) in enumerate(views.items()):board(presentation,'REF_2D_'+name,u0,u1,(index-1)*1.14)
presentation.camera=camera(presentation,'CAM_Lamina',(0,-7,.85),(0,0,.85),3.5)
presentation.render.engine='CYCLES';presentation.cycles.samples=8
presentation.render.resolution_x=1700;presentation.render.resolution_y=1050;presentation.render.resolution_percentage=100
presentation.view_settings.view_transform='Standard'
presentation.render.image_settings.file_format='PNG'
preview=ROOT/'Builds/ArtPreview/RodrigoCastillaV01';preview.mkdir(parents=True,exist_ok=True)
presentation.render.filepath=str(preview/'Referencias_Blender_v01.png')
bpy.ops.render.render(write_still=True)

modeling=bpy.data.scenes.new('02_Modelado_con_referencias')
modeling.unit_settings.system='METRIC';modeling.unit_settings.scale_length=1
front=board(modeling,'REF_FRENTE',*views['Frente'])
side=board(modeling,'REF_PERFIL',*views['Perfil'],axis='side')
back=board(modeling,'REF_ESPALDA',*views['Espalda'],axis='back');back.hide_viewport=True
for obj in (front,side,back):obj.hide_render=True
front_camera=camera(modeling,'CAM_Frente',(0,-7,.85),(0,0,.85),2.1)
camera(modeling,'CAM_Perfil',(7,0,.85),(0,0,.85),2.1)
camera(modeling,'CAM_Espalda',(0,7,.85),(0,0,.85),2.1)
modeling.camera=front_camera
sculpt_collection=bpy.data.collections.new('ESCULTURA_PENDIENTE');modeling.collection.children.link(sculpt_collection)

readme=bpy.data.texts.new('LEEME_REFERENCIAS')
readme.write('Referencia vigente: '+str(ORIGINAL.relative_to(ROOT))+'\n'
    'La primera escena presenta tres paneles de una imagen generada.\n'
    'La segunda tiene planos de referencia frontal/perfil y una referencia de espalda oculta.\n'
    'Los planos se distinguen por REF_; no son el cuerpo ni se deben exportar a Unity.\n'
    'La colección ESCULTURA_PENDIENTE está vacía intencionadamente.\n'
    'Altura de trabajo propuesta: 1.80 m, no dato histórico ni escala del mapa.\n'
    'Referencia aproximada: las vistas generadas no están calibradas.\n'
    'El retrato y la lámina están empaquetados; no hay texturas externas necesarias.\n')
bpy.context.window.scene=presentation
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Rodrigo_referencias_v01.blend'))
(SOURCE/'referencias_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
assert image.packed_file and original.packed_file
assert len(sculpt_collection.objects)==0
print('RODRIGO_REFERENCE_SETUP_OK',flush=True)

