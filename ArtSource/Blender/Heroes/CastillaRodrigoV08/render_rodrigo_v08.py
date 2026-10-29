from pathlib import Path
import bpy,json,hashlib
from mathutils import Vector
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
base=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV07/Rodrigo_escultura_v07.blend'
dest=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV08';dest.mkdir(parents=True,exist_ok=True)
out=ROOT/'Builds/ArtPreview/RodrigoCastillaV08';out.mkdir(parents=True,exist_ok=True)
original=hashlib.sha256(base.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(base));s=bpy.context.scene
lights=[('Principal_calida',420,2.2,(1,.94,.86)),('Relleno_neutro',95,2.5,(.83,.90,1)),('Contraluz',230,2.0,(1,.92,.82))]
for name,power,size,color in lights:
 ob=bpy.data.objects[name];ob.data.energy=power;ob.data.size=size;ob.data.color=color
 ob.rotation_euler=(Vector((0,0,1.62))-ob.location).to_track_quat('-Z','Y').to_euler()
bg=s.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.10,.105,.115,1);bg.inputs[1].default_value=.25
s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.10
s.render.resolution_x=1600;s.render.resolution_y=1920;s.render.resolution_percentage=100
s.render.image_settings.color_depth='16';s.render.image_settings.file_format='PNG'
s.cycles.samples=256;s.cycles.use_adaptive_sampling=True;s.cycles.adaptive_threshold=.005
s.cycles.use_denoising=True
gpu=[]
try:
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices:
  d.use=d.type=='OPTIX' and '5080' in d.name
  if d.use:gpu.append(d.name)
 if gpu:s.cycles.device='GPU'
except Exception as e:print('GPU_FALLBACK',str(e),flush=True)
s.name='Rodrigo_estudio_luz_V08';s.camera=bpy.data.objects['CAM_Rostro']
s['revision']='V08 lighting and render only; V07 character geometry and materials unchanged'
blend=dest/'Rodrigo_render_v08.blend';bpy.ops.wm.save_as_mainfile(filepath=str(blend))
for label,cam in [('Rostro','CAM_Rostro'),('Perfil','CAM_Perfil_Cuello')]:
 s.camera=bpy.data.objects[cam];s.render.filepath=str(out/(label+'_v08.png'));bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['CAM_Rostro'];bpy.ops.wm.save_as_mainfile(filepath=str(blend))
assert hashlib.sha256(base.read_bytes()).hexdigest()==original
(dest/'render_manifest.json').write_text(json.dumps({'base_v07_sha256':original,'base_preserved':True,'geometry_and_materials_edited':False,'size':[1600,1920],'samples_max':256,'noise_threshold':.005,'gpu':gpu,'lights':lights,'artistic_acceptance':'pending'},indent=2),encoding='utf8')
(dest/'README.md').write_text('# Rodrigo V08: luz y render\n\nRevisión del estudio V07: luz principal menos anaranjada, relleno reducido para modelar el volumen y contraluz más suave. Fondo ambiental neutral. Cycles, 1600 x 1920, hasta 256 muestras, umbral 0.005 y PNG de 16 bits.\n\nNo se modifica geometría ni materiales del personaje. V07 conservada por SHA-256. Renders de rostro y perfil en Builds/ArtPreview/RodrigoCastillaV08. Sigue siendo un estudio estilizado, sin exportación Unity ni aprobación artística.\n',encoding='utf8')
print('V08_RENDER_OK',gpu,flush=True)
