from pathlib import Path
import bpy, json, hashlib, math
from mathutils import Vector

ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
SOURCE=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV08/Rodrigo_render_v08.blend'
DEST=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV09'
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV09'
DEST.mkdir(parents=True,exist_ok=True);OUT.mkdir(parents=True,exist_ok=True)
sha=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
s=bpy.context.scene
s.render.engine='CYCLES'
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
eyes=[o for o in s.objects if o.name.startswith('Globo_ocular_anatomico')]
assert len(eyes)==2
centers=[head.matrix_world.inverted()@o.matrix_world.translation for o in eyes]
ez=sum(c.z for c in centers)/2
uv_before=[tuple(d.uv) for d in head.data.uv_layers.active.data]
assert not head.data.shape_keys, 'Unexpected existing keys: inspect before changing'
head.shape_key_add(name='Basis')
key=head.shape_key_add(name='V09_Restaurar_parpados');key.value=1
# Undo precisely the artificial V04 vertical eyelid dilation. The forward
# function is monotonic over this interval; bisection avoids iterative drift.
def forward(z,x):
    for c in centers:
        influence=math.exp(-((x-c.x)/.018)**4-((z-ez)/.012)**4)
        z+=((z-ez)*.55+.0006)*influence
    return z
changed=0;max_delta=0
for vertex,point in zip(head.data.vertices,key.data):
    x,y,z=vertex.co
    if y>-.025 or abs(z-ez)>.045:continue
    lo=z-.03;hi=z+.03
    for _ in range(40):
        mid=(lo+hi)/2
        if forward(mid,x)<z:lo=mid
        else:hi=mid
    point.co.z=(lo+hi)/2
    delta=abs(point.co.z-z)
    if delta>1e-7:changed+=1
    max_delta=max(max_delta,delta)
assert uv_before==[tuple(d.uv) for d in head.data.uv_layers.active.data]
head['V09_note']='Reversible eyelid correction; hair hidden for anatomical study. UV unchanged.'
# Hide accessories rather than deleting them; the complete character remains
# in the file, but the review shows only head and eyeballs.
visibility={o.name:o.hide_render for o in s.objects}
for o in s.objects:
    if o.type not in {'LIGHT','CAMERA'}:o.hide_render=o!=head and o not in eyes
clay=bpy.data.materials.new('V09_Arcilla_neutra');clay.use_nodes=True
clay.node_tree.nodes.clear()
p=clay.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
output=clay.node_tree.nodes.new('ShaderNodeOutputMaterial')
clay.node_tree.links.new(p.outputs['BSDF'],output.inputs['Surface'])
p.inputs['Base Color'].default_value=(.32,.30,.27,1)
p.inputs['Roughness'].default_value=.65
s.view_layers[0].material_override=clay
for name,power,size,pos in [('Principal_calida',170,1.0,(-.55,-.65,2.15)),('Relleno_neutro',45,1.4,(.65,-.4,1.85)),('Contraluz',100,1,(.3,.5,2.0))]:
    o=bpy.data.objects[name];o.location=pos;o.data.energy=power;o.data.size=size;o.data.color=(1,1,1)
    o.rotation_euler=(Vector((0,0,1.65))-o.location).to_track_quat('-Z','Y').to_euler()
bg=s.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.13,.13,.13,1);bg.inputs[1].default_value=.3
s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-2
s.render.resolution_x=900;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.image_settings.color_depth='8';s.cycles.samples=64;s.cycles.adaptive_threshold=.015
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
gpu=[]
for d in prefs.devices:
    d.use=d.type=='OPTIX' and '5080' in d.name
    if d.use:gpu.append(d.name)
s.cycles.device='GPU' if gpu else 'CPU'
target=Vector((0,.005,1.64))
cams=[]
for label,pos in [('Frente',(0,-1.5,1.64)),('Tres_cuartos',(.95,-1.25,1.64)),('Perfil',(1.5,0,1.64))]:
    data=bpy.data.cameras.new('V09_'+label);ob=bpy.data.objects.new(data.name,data);s.collection.objects.link(ob)
    ob.location=pos;ob.rotation_euler=(target-ob.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO';data.ortho_scale=.34;cams.append((label,ob))
for label,cam in cams:
    s.camera=cam
    for suffix,value in [('Antes',0),('Despues',1)]:
        key.value=value;s.render.filepath=str(OUT/(label+'_'+suffix+'.png'))
        bpy.ops.render.render(write_still=True)
key.value=1;s.camera=cams[1][1]
bpy.ops.object.select_all(action='DESELECT');head.hide_set(False);head.select_set(True);bpy.context.view_layer.objects.active=head
# OBJ retains original UV layout and evaluated correction for Painter import.
bpy.ops.wm.obj_export(filepath=str(DEST/'Rodrigo_cabeza_V09_para_Painter.obj'),export_selected_objects=True,export_materials=False,export_uv=True)
bpy.ops.wm.save_as_mainfile(filepath=str(DEST/'Rodrigo_anatomia_v09.blend'))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sha
report={'source_sha256':sha,'source_preserved':True,'changed_vertices':changed,'max_displacement_mm':max_delta*1000,'uv_preserved':True,'head_vertices':len(head.data.vertices),'eye_centers_local':[list(c) for c in centers],'gpu':gpu,'original_visibility':visibility,'status':'anatomy study; pending visual approval; not painted in Substance; not Unity-ready'}
(DEST/'verificacion_v09.json').write_text(json.dumps(report,indent=2),encoding='utf8')
(DEST/'README.md').write_text('# Rodrigo V09: revisión anatómica\n\nSe revierte la dilatación vertical artificial de los párpados introducida en V04. Corrección reversible con la shape key V09_Restaurar_parpados (0 anterior, 1 corregido). No se altera el resto de la anatomía ni la ropa. Pelo, barba y accesorios se ocultan para esta revisión, no se eliminan. Material gris mediante override de View Layer.\n\nComparativas de frente, tres cuartos y perfil en Builds/ArtPreview/RodrigoCastillaV09. V08 conservada por SHA-256 y UV intactas. OBJ de cabeza con UV preparado para importar en Substance Painter; aún no hay proyecto SPP ni texturas pintadas. No es un recurso final ni tiene aprobación artística. La barba necesitará reajustarse cuando se cierre la anatomía.\n',encoding='utf8')
print('V09_DONE',changed,max_delta*1000,flush=True)
