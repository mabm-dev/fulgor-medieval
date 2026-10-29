from pathlib import Path
import bpy, math, json, hashlib, numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
src=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV11/Rodrigo_anatomia_v11.blend'
dest=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV12';out=ROOT/'Builds/ArtPreview/RodrigoCastillaV12'
dest.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
sha=hashlib.sha256(src.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(src))
s=bpy.context.scene;h=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
visibility=json.loads((ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV09/verificacion_v09.json').read_text())['original_visibility']
for o in s.objects:
    if o.name in visibility:o.hide_render=visibility[o.name]
s.view_layers[0].material_override=None
h.data.shape_keys.key_blocks['V09_Restaurar_parpados'].value=.78
bpy.context.view_layer.update()
ev=h.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
new=[h.matrix_world@v.co for v in me.vertices]
old=[h.matrix_world@v.co for v in h.data.vertices]
bvh=BVHTree.FromPolygons(new,[tuple(p.vertices) for p in me.polygons])
kd=KDTree(len(old))
for i,p in enumerate(old):kd.insert(p,i)
kd.balance();ev.to_mesh_clear()
# Separate material for physical beard strands, not thick polygon tubes.
beardmat=bpy.data.materials.new('V12_Barba_fibra');beardmat.use_nodes=True
nn=beardmat.node_tree.nodes;nn.clear();hair=nn.new('ShaderNodeBsdfHairPrincipled');hair.parametrization='MELANIN'
hair.inputs['Melanin'].default_value=.97;hair.inputs['Melanin Redness'].default_value=.12;hair.inputs['Roughness'].default_value=.53
o=nn.new('ShaderNodeOutputMaterial');beardmat.node_tree.links.new(hair.outputs[0],o.inputs['Surface'])
strand_count=0
for ob in list(s.objects):
    if not ob.name.startswith('Barba') or ob.type!='CURVE':continue
    sizes=[];points=[];radii=[]
    for sp in ob.data.splines:
        if not sp.points:continue
        root=ob.matrix_world@Vector(sp.points[0].co[:3]);_,idx,_=kd.find(root);shift=new[idx]-old[idx]
        sizes.append(len(sp.points));strand_count+=1
        for j,p in enumerate(sp.points):
            q=ob.matrix_world@Vector(p.co[:3])+shift
            points.extend(q);radii.append(.000085*max(.08,1-(j/(len(sp.points)-1))**1.5))
    cu=bpy.data.hair_curves.new(ob.name+'_V12');cu.add_curves(sizes)
    cu.attributes['position'].data.foreach_set('vector',points)
    cu.attributes.new('radius','FLOAT','POINT').data.foreach_set('value',radii)
    cu.materials.append(beardmat);neo=bpy.data.objects.new(cu.name,cu);s.collection.objects.link(neo)
    ob.hide_render=True;ob.hide_set(True)
# Reduce inflated hair silhouette while retaining the existing roots and flow.
for ob in s.objects:
    if ob.name.startswith('Pelo_fibras') and ob.type=='CURVES':
        a=ob.data.attributes['position'];positions=np.empty(len(a.data)*3,dtype=np.float32);a.data.foreach_get('vector',positions)
        positions=positions.reshape(-1,3)
        for i,p in enumerate(positions):
            q=ob.matrix_world@Vector(p);surf,normal,_,dist=bvh.find_nearest(q)
            if surf is not None and dist>.003:
                q=surf+(q-surf)*(.003/dist+.72*(1-.003/dist));positions[i]=ob.matrix_world.inverted()@q
        a.data.foreach_set('vector',positions.ravel())
for ob in s.objects:
    if ob.name.startswith('Ceja_anatomica'):ob.location.z-=.003
# Skin retains licensed CC0 source maps; rebalance tone and physical scale.
mat=h.data.materials[0];nodes=mat.node_tree.nodes;links=mat.node_tree.links
p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
input_color=p.inputs['Base Color'].links[0].from_socket
hue=nodes.new('ShaderNodeHueSaturation');hue.inputs['Saturation'].default_value=.9;hue.inputs['Value'].default_value=.92
links.new(input_color,hue.inputs['Color']);links.new(hue.outputs[0],p.inputs['Base Color'])
p.inputs['Subsurface Weight'].default_value=.16;p.inputs['Subsurface Scale'].default_value=.0012
p.inputs['Subsurface Radius'].default_value=(1,.45,.25);p.inputs['Specular IOR Level'].default_value=.33
rough=next(n for n in nodes if n.type=='MAP_RANGE');rough.inputs['To Min'].default_value=.58;rough.inputs['To Max'].default_value=.32
for nd in nodes:
    if nd.type=='BUMP':nd.inputs['Strength'].default_value=.16;nd.inputs['Distance'].default_value=.00007
# A thin wet surface on the eyes gives a visible highlight without whitening skin.
for ob in s.objects:
    if ob.name.startswith('Globo_ocular_anatomico'):
        bs=next(n for n in ob.data.materials[0].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        bs.inputs['Base Color'].default_value=(.65,.60,.53,1);bs.inputs['Roughness'].default_value=.13
        bs.inputs['Coat Weight'].default_value=.8;bs.inputs['Coat Roughness'].default_value=.05
    if ob.name.startswith('Iris_curvado'):
        bs=next(n for n in ob.data.materials[0].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        bs.inputs['Coat Weight'].default_value=1;bs.inputs['Coat Roughness'].default_value=.035;bs.inputs['Roughness'].default_value=.2
# Broad key and weaker fill reveal volume without the old orange cast.
for name,power,size,pos in [('Principal_calida',95,.7,(-.55,-.65,2.15)),('Relleno_neutro',18,1.2,(.65,-.4,1.85)),('Contraluz',60,.8,(.3,.5,2.0))]:
    ob=bpy.data.objects[name];ob.location=pos;ob.data.energy=power;ob.data.size=size;ob.data.color=(1,.96,.90) if name=='Principal_calida' else (1,1,1)
    ob.rotation_euler=(Vector((0,0,1.63))-ob.location).to_track_quat('-Z','Y').to_euler()
s.view_settings.exposure=-3.0;s.cycles.samples=96;s.render.resolution_x=1100;s.render.resolution_y=1300
s.camera=bpy.data.objects['CAM_Rostro']
for label,cam in [('Rostro','CAM_Rostro'),('Perfil','CAM_Perfil_Cuello'),('Tres_cuartos','V09_Tres_cuartos')]:
    s.camera=bpy.data.objects[cam]
    if label=='Tres_cuartos':s.camera.data.ortho_scale=.44
    s.render.filepath=str(out/(label+'_v12.png'));bpy.ops.render.render(write_still=True)
s.camera=bpy.data.objects['CAM_Rostro'];bpy.ops.wm.save_as_mainfile(filepath=str(dest/'Rodrigo_materiales_v12.blend'))
assert hashlib.sha256(src.read_bytes()).hexdigest()==sha
(dest/'verificacion_v12.json').write_text(json.dumps({'source_preserved':True,'source_sha256':sha,'beard_native_strands':strand_count,'painter_project_created':False,'status':'material and grooming study, not final','changes':['V09 eyelid correction at 0.78','beard relocated to V11 neck; native strands','hair excess reduced 28 percent','skin saturation, roughness and SSS rebalance','wet eye highlights']},indent=2),encoding='utf8')
print('V12_OK',strand_count,flush=True)

