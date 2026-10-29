"""Local beard back-edge trim requested after V05 profile review."""
from pathlib import Path
import bpy,hashlib,json,math
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
BASE=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV05/Rodrigo_escultura_v05.blend'
DEST=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV06';DEST.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV06';OUT.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
before=sha(BASE)
bpy.ops.wm.open_mainfile(filepath=str(BASE))
scene=bpy.context.scene;head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
def smooth(t):
 t=max(0,min(1,t));return t*t*(3-2*t)
# Y increases toward the nape. A near-vertical edge under each earlobe,
# feathered over 1.5 mm, removes the rearward neck extension symmetrically.
LIMIT=-.015
def keep(p):
 rear=1-smooth((p.y-(LIMIT-.0015))/.0015)
 upper=smooth((p.z-1.630)/.008)
 return rear+(1-rear)*upper
attr=head.data.color_attributes['SkinTreatment'];modified=0
upper_before=[];geometry=hashlib.sha256()
for v in head.data.vertices:geometry.update(str(tuple(v.co)).encode())
for loop in head.data.loops:
 p=head.matrix_world@head.data.vertices[loop.vertex_index].co
 c=attr.data[loop.index].color[:]
 if p.z>=1.638:upper_before.append((loop.index,c))
 f=keep(p)
 if f<1:
  neutral=(.96,.93,.88,1)
  attr.data[loop.index].color=tuple(neutral[j]+(c[j]-neutral[j])*f for j in range(4));modified+=1
trimmed=removed=0;kept_roots=[]
for ob in bpy.data.objects:
 if ob.type!='CURVE' or not ob.name.startswith(('Barba_anatomica','Barba_cuello_V05')):continue
 for sp in list(ob.data.splines):
  root=ob.matrix_world@sp.points[0].co.xyz
  if keep(root)<.05:
   ob.data.splines.remove(sp);removed+=1;continue
  kept_roots.append(list(root))
  for p in sp.points:
   world=ob.matrix_world@p.co.xyz
   f=keep(world)
   if f<1:p.radius*=f;trimmed+=1
assert removed>500 and modified>0
assert all(tuple(attr.data[i].color)==tuple(c) for i,c in upper_before)
check=hashlib.sha256()
for v in head.data.vertices:check.update(str(tuple(v.co)).encode())
assert geometry.hexdigest()==check.hexdigest()
assert not any(p[1]>=LIMIT and p[2]<=1.630 for p in kept_roots)
scene.name='Rodrigo_borde_barba_bajo_oreja_V06'
scene['revision']='V06: vertical back beard edge under earlobes; rearward neck patch removed'
report={'base_v05_sha256':before,'back_edge_world_y':LIMIT,'edge_feather_m':.0015,
 'removed_strands':removed,'tapered_points':trimmed,'color_corners_updated':modified,
 'head_geometry_unchanged':True,'upper_treatment_unchanged_above_z':1.638,
 'rear_neck_beard_roots_remaining':0,'game_ready':False}
(DEST/'verificacion_v06.json').write_text(json.dumps(report,indent=2),encoding='utf8')
(DEST/'README.md').write_text('# Rodrigo V06: borde de barba bajo la oreja\n\nRevisión de V05 solicitada por el usuario: borde descendente bajo el lóbulo y retirada del exceso posterior sobre el cuello, de forma simétrica. Se retiran fibras y pigmento de raíz; la cabeza, pelo, ojos, ropa y cámaras se conservan. Borde suavizado en 1,5 mm.\n\nFuente de alta densidad, pendiente de aprobación artística y adaptación para Unity. V05 se conserva. Ver `verificacion_v06.json`.\n',encoding='utf8')
blend=DEST/'Rodrigo_escultura_v06.blend'
scene.camera=bpy.data.objects['CAM_3Q'];bpy.ops.wm.save_as_mainfile(filepath=str(blend))
for label,cam in [('Perfil_cuello','CAM_Perfil_Cuello'),('Rostro','CAM_Rostro')]:
 scene.camera=bpy.data.objects[cam];scene.render.filepath=str(OUT/(label+'_v06.png'))
 bpy.ops.render.render(write_still=True)
assert sha(BASE)==before
print('RODRIGO_V06_OK',json.dumps(report),flush=True)
