"""Local, reversible vertex corrections. Never unwrap, remesh or overwrite Painter."""
import bpy, numpy as np, math, json, hashlib
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
out=root/'ArtSource/Blender/FortalezaCorregida';out.mkdir(parents=True,exist_ok=True)
review=root/'Builds/FortressGeometry';review.mkdir(parents=True,exist_ok=True)
art=root/'Assets/Fulgor/Art/RawFortress'
prefix=art/'Painter4K/SM_Fortaleza_completa_bruta_Material_0_'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='D:/IA/ComfyUI_windows_portable/ComfyUI/output/3d/fortaleza_completa.glb')
obj=next(o for o in bpy.context.scene.objects if o.type=='MESH')
bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
obj.name='Fortaleza_Corregida_Painter'
mesh=obj.data
def uv_hash(m):
 h=hashlib.sha256()
 for uv in m.uv_layers:
  values=np.empty(len(m.loops)*2,np.float32);uv.data.foreach_get('uv',values);h.update(values.tobytes())
 return h.hexdigest()
before_uv=uv_hash(mesh)
co=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',co);co=co.reshape(-1,3)
fixed=co.copy();x,y,z=co.T
def smooth(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)

# Northern curtain: remove the broad bow, translating the attached walkway and
# middle turret continuously. The towers at both ends and the keep stay put.
knots=np.array([-.30,-.26,-.22,-.18,-.14,-.10,-.06,-.02,.02,.06,.10,.14,.18,.21,.30])
bow=np.array([0,0,.010,.025,.033,.032,.030,.028,.025,.020,.015,.010,.005,0,0])
offset=np.interp(x,knots,bow)
# Smooth the displacement field itself; retain the high-frequency stone relief.
offset=sum(np.interp(x+d,knots,bow) for d in [-.008,-.004,0,.004,.008])/5
wall_weight=smooth(.205,.275,y)*smooth(-.293,-.253,z)
fixed[:,1]-=offset*wall_weight

# Crown: correct the low middle saddles and the raised corners as a continuous
# height field. Crenellation gaps, stone relief, poles and UV coordinates survive.
cx,cy=-.020,-.013
crown=np.full(len(co),.002,dtype=np.float32)
for px,py in [(-.030,-.143),(-.020,.117)]:
 crown+=.0055*np.exp(-((x-px)**2+(y-py)**2)/(.027**2))
for px in [-.139,.100]:
 for py in [-.131,.104]:
  crown-=.005*np.exp(-((x-px)**2+(y-py)**2)/(.031**2))
keep_weight=(1-smooth(.165,.205,np.maximum(abs(x-cx),abs(y-cy))))*smooth(.180,.230,z)
fixed[:,2]+=crown*keep_weight

obj.shape_key_add(name='Original_sin_modificar')
key=obj.shape_key_add(name='Rectificar_muralla_y_coronacion')
key.data.foreach_set('co',fixed.ravel());key.value=1
obj['painter']='UV y topologia originales; usar el mismo Texture Set Material_0'
obj['correccion']='Desplazamiento local; sin remallado, decimacion ni nuevas UV'
wall_group=obj.vertex_groups.new(name='Zona_muralla_rectificada')
for indices in np.array_split(np.flatnonzero(wall_weight*offset>1e-6),16):
 if len(indices):wall_group.add(indices.tolist(),1,'REPLACE')
crown_group=obj.vertex_groups.new(name='Zona_coronacion_rectificada')
for indices in np.array_split(np.flatnonzero(keep_weight>.001),16):
 if len(indices):crown_group.add(indices.tolist(),1,'REPLACE')
assert uv_hash(mesh)==before_uv
# Recompute smooth normals for the moved surfaces instead of exporting the
# original custom directions; Unity will regenerate MikkTSpace tangents.
if mesh.has_custom_normals:
 mesh.normals_split_custom_set([(0.0,0.0,0.0)]*len(mesh.loops))

indices=np.empty(len(mesh.loops),np.int32);mesh.loops.foreach_get('vertex_index',indices);faces=indices.reshape(-1,3)
def areas(points):
 a=points[faces[:,1]]-points[faces[:,0]];b=points[faces[:,2]]-points[faces[:,0]]
 return np.linalg.norm(np.cross(a,b),axis=1)*.5
old_area,new_area=areas(co),areas(fixed)
assert not np.any((old_area>1e-12)&(new_area<=1e-12)), 'Correction collapsed faces'
np.save(review/'corrected_vertices.npy',fixed)

# Display exactly the existing Painter export in Blender. No .spp or texture writes.
mat=mesh.materials[0];mat.name='Material_0';mat.use_nodes=True
nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
shader=nodes.new('ShaderNodeBsdfPrincipled');output=nodes.new('ShaderNodeOutputMaterial');links.new(shader.outputs['BSDF'],output.inputs['Surface'])
color=nodes.new('ShaderNodeTexImage');color.image=bpy.data.images.load(str(prefix)+'AlbedoTransparency.png',check_existing=True)
links.new(color.outputs['Color'],shader.inputs['Base Color']);nodes.active=color
normal=nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(prefix)+'Normal.png',check_existing=True);normal.image.colorspace_settings.name='Non-Color'
nm=nodes.new('ShaderNodeNormalMap');links.new(normal.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],shader.inputs['Normal'])
packed=nodes.new('ShaderNodeTexImage');packed.image=bpy.data.images.load(str(prefix)+'MetallicSmoothness.png',check_existing=True);packed.image.colorspace_settings.name='Non-Color'
sep=nodes.new('ShaderNodeSeparateColor');links.new(packed.outputs['Color'],sep.inputs['Color']);links.new(sep.outputs['Red'],shader.inputs['Metallic'])
invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(packed.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],shader.inputs['Roughness'])

# FBX for Unity: evaluated correction, no blendshape dependency, same loops/UV.
deps=bpy.context.evaluated_depsgraph_get();baked=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
assert len(baked.vertices)==len(mesh.vertices) and len(baked.polygons)==len(mesh.polygons)
assert uv_hash(baked)==before_uv
export=bpy.data.objects.new('Fortaleza_completa_corregida',baked);bpy.context.collection.objects.link(export)
export.rotation_euler.z=-math.pi/2;export.location.z=-float(co[:,2].min())
bpy.ops.object.select_all(action='DESELECT');export.select_set(True);bpy.context.view_layer.objects.active=export
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.export_scene.fbx(filepath=str(art/'SM_Fortaleza_completa_corregida.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=False,path_mode='AUTO')
bpy.data.objects.remove(export,do_unlink=True);bpy.data.meshes.remove(baked)

scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='TEXTURE';scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.render.resolution_x=1400;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.clip_start=.001
views=[('plan',(0,0,2),(0,0,0),1.1),('keep',(1.2,-1.5,1.0),(-.015,-.015,.19),.65),('north_wall',(-1.5,1.6,.5),(-.08,.29,-.12),.8),('whole',(1.3,-1.8,1.3),(0,0,-.04),1.25)]
for name,pos,look,scale in views:
 cam.location=pos;cam.rotation_euler=(Vector(look)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
 for value,suffix in [(0,'before'),(1,'after')]:
  key.value=value;scene.render.filepath=str(review/(name+'_'+suffix+'.png'));bpy.ops.render.render(write_still=True)
key.value=1
cam.location=(1.3,-1.8,1.3);cam.rotation_euler=(Vector((0,0,-.04))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=1.25
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.shading.color_type='TEXTURE';area.spaces.active.region_3d.view_location=(0,0,0);area.spaces.active.region_3d.view_distance=1.5;area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
bpy.ops.file.pack_all();bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Fortaleza_Corregida_Painter.blend'),compress=True)
delta=np.linalg.norm(fixed-co,axis=1)
report={'vertices':len(co),'triangles':len(mesh.polygons),'uv_sha256_before':before_uv,'uv_sha256_after':uv_hash(mesh),'same_topology':True,'face_indices_sha256':hashlib.sha256(indices.tobytes()).hexdigest(),'new_collapsed_faces':int(((old_area>1e-12)&(new_area<=1e-12)).sum()),'minimum_face_area_ratio':float((new_area[old_area>1e-12]/old_area[old_area>1e-12]).min()),'modified_vertices':int((delta>1e-7).sum()),'max_displacement_source_units':float(delta.max()),'source_glb_untouched':True,'painter_spp_untouched':True,'painter_textures_untouched':True,'unity_fbx':'Assets/Fulgor/Art/RawFortress/SM_Fortaleza_completa_corregida.fbx'}
(out/'validacion.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FORTRESS_CORRECTION_OK',json.dumps(report),flush=True)
