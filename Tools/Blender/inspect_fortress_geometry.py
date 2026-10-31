import bpy, numpy as np, json
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
out=root/'Builds/FortressGeometry';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='D:/IA/ComfyUI_windows_portable/ComfyUI/output/3d/fortaleza_completa.glb')
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
mesh=obj.data
co=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',co);co=co.reshape(-1,3)
np.save(out/'source_vertices.npy',co)
norm=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('normal',norm);np.save(out/'source_normals.npy',norm.reshape(-1,3))
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH'
scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=1600;scene.render.resolution_y=1600;scene.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.clip_start=.001
for name,position,look,scale in [('plan',(0,0,2),(0,0,0),1.1),('keep',(1.2,-1.5,1.0),(-.015,-.015,.19),.65),('wall',(-1.6,-1.5,.65),(-.25,-.24,-.12),.65)]:
 cam.location=position;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 scene.render.filepath=str(out/(name+'_source.png'));bpy.ops.render.render(write_still=True)
print('GEOMETRY_INSPECTION_OK',len(co),flush=True)
