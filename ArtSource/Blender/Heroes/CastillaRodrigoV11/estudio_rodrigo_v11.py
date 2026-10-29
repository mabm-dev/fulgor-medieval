from pathlib import Path
import bpy,numpy as np,json,hashlib
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
source=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV10/Rodrigo_anatomia_v10.blend'
dest=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV11'
out=ROOT/'Builds/ArtPreview/RodrigoCastillaV11'
dest.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
digest=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source))
s=bpy.context.scene;h=bpy.data.objects['Cabeza_anatomica_CC0_adaptada'];n=len(h.data.vertices)
def read_points(data):
    a=np.empty(n*3,dtype=np.float32);data.foreach_get('co',a);return a.reshape(-1,3)
basis=read_points(h.data.shape_keys.key_blocks[0].data)
current=basis.copy()
for key in list(h.data.shape_keys.key_blocks)[1:]:
    current+=key.value*(read_points(key.data)-read_points(key.relative_key.data))
edges=np.empty(len(h.data.edges)*2,dtype=np.int32);h.data.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2)
src=np.concatenate([edges[:,0],edges[:,1]]);dst=np.concatenate([edges[:,1],edges[:,0]])
degree=np.bincount(src,minlength=n)
x,y,z=current.T
def ramp(a,b,q):
    t=np.clip((q-a)/(b-a),0,1);return t*t*(3-2*t)
# Compact mask around the inherited central throat crease. Freeze face,
# posterior neck and the entire cut rim; use current V10 geometry as input.
mask=(1-ramp(.026,.050,np.abs(x)))*(1-ramp(-.037,-.012,y))*ramp(1.528,1.542,z)*(1-ramp(1.574,1.598,z))
mask[degree==0]=0
coords=current.copy()
for _ in range(80):
    sums=np.zeros_like(coords);np.add.at(sums,src,coords[dst])
    coords+=.42*mask[:,None]*(sums/degree.clip(1)[:,None]-coords)
delta=coords-current;length=np.linalg.norm(delta,axis=1)
factor=np.ones(n,dtype=np.float32);nz=length>1e-9
factor[nz]=.006*np.tanh(length[nz]/.006)/length[nz]
delta*=factor[:,None]
key=h.shape_key_add(name='V11_Pliegue_submentoniano',from_mix=False)
key.data.foreach_set('co',(basis+delta).ravel());key.value=1
assert np.max(np.linalg.norm(delta,axis=1))<.00601
assert np.array_equal(delta[z>=1.598],np.zeros_like(delta[z>=1.598]))
uv_before=[tuple(q.uv) for q in h.data.uv_layers.active.data]
s.cycles.samples=96
for label in ['Frente','Tres_cuartos','Perfil']:
    s.camera=bpy.data.objects['V09_'+label]
    for suffix,value in [('Antes',0),('Despues',1)]:
        key.value=value;s.render.filepath=str(out/(label+'_'+suffix+'.png'));bpy.ops.render.render(write_still=True)
key.value=1;s.camera=bpy.data.objects['V09_Tres_cuartos']
bpy.ops.object.select_all(action='DESELECT');h.select_set(True);bpy.context.view_layer.objects.active=h
bpy.ops.wm.obj_export(filepath=str(dest/'Rodrigo_cabeza_V11_para_Painter.obj'),export_selected_objects=True,export_materials=False,export_uv=True)
bpy.ops.wm.save_as_mainfile(filepath=str(dest/'Rodrigo_anatomia_v11.blend'))
assert uv_before==[tuple(q.uv) for q in h.data.uv_layers.active.data]
assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
report={'source_sha256':digest,'source_preserved':True,'uv_preserved':True,'upper_face_preserved':True,'max_displacement_mm':float(np.linalg.norm(delta,axis=1).max()*1000),'changed_vertices':int(np.sum(np.linalg.norm(delta,axis=1)>1e-7)),'status':'anatomical study, not final; no Substance painting yet'}
(dest/'verificacion_v11.json').write_text(json.dumps(report,indent=2),encoding='utf8')
(dest/'README.md').write_text('# Rodrigo V11\n\nCorrección localizada y reversible del pliegue bajo el mentón, calculada sobre la geometría V10 con los párpados V09 activos. Se conservan las versiones anteriores, la cara superior y las UV. Shape key V11_Pliegue_submentoniano (0 anterior; 1 revisión).\n\nRenders antes/después con idéntica iluminación y cámara en Builds/ArtPreview/RodrigoCastillaV11. OBJ con UV exportado para Painter; no se ha creado un SPP ni pintado la piel en Substance. Pelo y barba siguen ocultos. Estudio sin aprobación final ni integración en Unity.\n',encoding='utf8')
print('V11_OK',json.dumps(report),flush=True)

