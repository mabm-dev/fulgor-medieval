from pathlib import Path
import bpy, numpy as np, hashlib, json

ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
SOURCE=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV09/Rodrigo_anatomia_v09.blend'
DEST=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV10'
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV10'
DEST.mkdir(parents=True,exist_ok=True); OUT.mkdir(parents=True,exist_ok=True)
sha=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
s=bpy.context.scene;h=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
n=len(h.data.vertices)
original=np.empty(n*3,dtype=np.float32);h.data.vertices.foreach_get('co',original);original=original.reshape(-1,3)
uv=np.empty(len(h.data.uv_layers.active.data)*2,dtype=np.float32)
h.data.uv_layers.active.data.foreach_get('uv',uv)
edges=np.empty(len(h.data.edges)*2,dtype=np.int32);h.data.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2)
src=np.concatenate((edges[:,0],edges[:,1]));dst=np.concatenate((edges[:,1],edges[:,0]))
degree=np.bincount(src,minlength=n).clip(1)
# Smooth only the graft transition. Preserve face, chin tip and open neck rim.
z=original[:,2];y=original[:,1]
def smoothstep(a,b,v):
    t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
weight=(1-smoothstep(1.585,1.635,z))*smoothstep(1.515,1.54,z)*smoothstep(-.095,-.065,y)
weight[np.bincount(src,minlength=n)==0]=0
# Freeze boundary vertices: avoids shrinking or displacing the export rim.
edge_counts={}
for poly in h.data.polygons:
    vs=list(poly.vertices)
    for a,b in zip(vs,vs[1:]+vs[:1]):
        pair=tuple(sorted((a,b)));edge_counts[pair]=edge_counts.get(pair,0)+1
boundary=set(i for e,count in edge_counts.items() if count==1 for i in e)
if boundary:weight[list(boundary)]=0
coords=original.copy()
for _ in range(40):
    sums=np.zeros_like(coords);np.add.at(sums,src,coords[dst])
    coords+=.48*weight[:,None]*(sums/degree[:,None]-coords)
# Bound the correction smoothly; the inherited throat crease is deep and
# unrestricted averaging would fill it by almost a centimetre.
offset=coords-original
length=np.linalg.norm(offset,axis=1)
factor=np.ones(n,dtype=np.float32)
nonzero=length>1e-9
factor[nonzero]=.004*np.tanh(length[nonzero]/.004)/length[nonzero]
coords=original+offset*factor[:,None]
key=h.shape_key_add(name='V10_Transicion_mandibula_cuello',from_mix=False)
key.data.foreach_set('co',coords.ravel());key.value=1
delta=np.linalg.norm(coords-original,axis=1)
assert np.max(delta)<.006, str((float(delta.max()),original[delta.argmax()].tolist(),coords[delta.argmax()].tolist()))
assert np.array_equal(coords[z>=1.635],original[z>=1.635]),'Face changed'
assert h.data.shape_keys.key_blocks['V09_Restaurar_parpados'].value==1
uv_after=np.empty_like(uv);h.data.uv_layers.active.data.foreach_get('uv',uv_after)
assert np.array_equal(uv,uv_after)
s.cycles.samples=96
for label in ['Frente','Tres_cuartos','Perfil']:
    s.camera=bpy.data.objects['V09_'+label]
    for suffix,value in [('Antes',0),('Despues',1)]:
        key.value=value;s.render.filepath=str(OUT/(label+'_'+suffix+'.png'))
        bpy.ops.render.render(write_still=True)
key.value=1;s.camera=bpy.data.objects['V09_Tres_cuartos']
bpy.ops.object.select_all(action='DESELECT');h.select_set(True);bpy.context.view_layer.objects.active=h
bpy.ops.wm.obj_export(filepath=str(DEST/'Rodrigo_cabeza_V10_para_Painter.obj'),export_selected_objects=True,export_materials=False,export_uv=True)
bpy.ops.wm.save_as_mainfile(filepath=str(DEST/'Rodrigo_anatomia_v10.blend'))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sha
report={'source_sha256':sha,'source_preserved':True,'uv_preserved':True,'face_above_local_z_1_635_preserved':True,'boundary_preserved':True,'vertex_count':n,'changed_vertices':int(np.sum(delta>1e-7)),'max_displacement_mm':float(delta.max()*1000),'status':'anatomical study, not approved; not textured in Painter','operation':'40 weighted Laplacian iterations, 0.48; restricted to neck transition; reversible shape key'}
(DEST/'verificacion_v10.json').write_text(json.dumps(report,indent=2),encoding='utf8')
(DEST/'README.md').write_text('# Rodrigo V10 — transición de mandíbula y cuello\n\nSuavizado localizado del empalme del cuello para reducir el escalón bajo la mandíbula y la irregularidad de la nuca. Shape key independiente V10_Transicion_mandibula_cuello: 0 anterior, 1 revisión. Se conserva la corrección de párpados V09. Rostro superior, UV y borde abierto del cuello conservados. Pelo y ropa siguen ocultos para evaluar la anatomía.\n\nComparativas con idéntica cámara e iluminación en Builds/ArtPreview/RodrigoCastillaV10. Exportación OBJ con UV para Painter; aún no hay SPP ni texturas nuevas. Estudio pendiente de aprobación artística y ajuste posterior de barba. No listo para Unity.\n',encoding='utf8')
print('V10_OK',json.dumps(report),flush=True)

