from pathlib import Path
import bpy,ast,json,hashlib,struct,math,numpy as np
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
DEST=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV07';OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV07'
manifest=json.loads((DEST/'hair_manifest_v07.json').read_text(encoding='utf8'))
bpy.ops.wm.open_mainfile(filepath=str(DEST/'Rodrigo_escultura_v07.blend'))
tree=ast.parse((DEST/'mejorar_pelo_rodrigo_v07.py').read_text(encoding='utf8'))
function=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='signature')
exec(compile(ast.Module(body=[function],type_ignores=[]),'<signature>','exec'))
bpy.context.view_layer.update()
for name,sig in manifest['protected_objects'].items():assert signature(bpy.data.objects[name])==sig,name
base=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV06/Rodrigo_escultura_v06.blend'
assert hashlib.sha256(base.read_bytes()).hexdigest()==manifest['base_v06_sha256']
strands=points=0;native=[]
for ob in bpy.data.objects:
 if not ob.name.startswith('Pelo_fibras_V07'):continue
 assert ob.type=='CURVES',ob.type
 count=len(ob.data.curve_offset_data)-1;strands+=count;points+=len(ob.data.attributes['position'].data)
 for attribute,field in [('position','vector'),('radius','value')]:
  arr=np.empty(len(ob.data.attributes[attribute].data)*(3 if field=='vector' else 1),np.float32)
  ob.data.attributes[attribute].data.foreach_get(field,arr);assert np.all(np.isfinite(arr))
  if attribute=='radius':assert np.min(arr)>0 and np.max(arr)<.000060
 assert any(n.type=='BSDF_HAIR_PRINCIPLED' for n in ob.data.materials[0].node_tree.nodes)
 native.append(ob.name)
assert strands==manifest['new_strands'],(strands,manifest['new_strands'])
renders=[]
for label in ['Rostro','Perfil_cuello','Espalda']:
 p=OUT/(label+'_v07.png');assert p.is_file()
 im=bpy.data.images.load(str(p),check_existing=False);assert tuple(im.size)==(1000,1200)
 renders.append(str(p));bpy.data.images.remove(im)
report={'saved_blend_reopened':True,'base_v06_unchanged':True,'protected_objects_verified':len(manifest['protected_objects']),
 'native_hair_objects':native,'strands':strands,'points':points,'coordinates_and_radii_finite':True,
 'head_beard_eyes_costume_cameras_lights_preserved':True,'renders_checked':renders,
 'game_ready':False,'artistic_acceptance':'pending'}
(DEST/'verificacion_v07.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('RODRIGO_V07_VERIFIED',json.dumps(report),flush=True)
