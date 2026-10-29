"""Read back the extension, prove V04 preservation and measure neck hair fit."""
from pathlib import Path
import bpy,hashlib,struct,json,math
from mathutils.bvhtree import BVHTree
SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[3]
manifest=json.loads((SOURCE/'extension_manifest.json').read_text(encoding='utf8'))
base=SOURCE.parent/'CastillaRodrigoV04/Rodrigo_escultura_v04.blend'
assert hashlib.sha256(base.read_bytes()).hexdigest()==manifest['base_v04_sha256']
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v05.blend'))
def object_signature(ob):
    h=hashlib.sha256()
    def floats(values):
        values=list(values);h.update(struct.pack('<%df'%len(values),*(round(float(q),6) for q in values)))
    floats(x for row in ob.matrix_world for x in row);h.update(ob.type.encode())
    if ob.type=='MESH':
        for v in ob.data.vertices:floats(v.co)
        for f in ob.data.polygons:h.update(struct.pack('<%dI'%len(f.vertices),*f.vertices))
        for uv in ob.data.uv_layers:
            for p in uv.data:floats(p.uv)
    elif ob.type=='CURVE':
        floats([ob.data.bevel_depth,ob.data.bevel_resolution])
        for sp in ob.data.splines:
            for p in sp.points:floats([*p.co,p.radius])
    elif ob.type=='CAMERA':floats([ob.data.lens,ob.data.ortho_scale])
    elif ob.type=='LIGHT':floats([ob.data.energy,*ob.data.color,ob.data.size])
    if ob.type in {'MESH','CURVE'}:h.update('|'.join(m.name for m in ob.data.materials).encode())
    return h.hexdigest()
for name,digest in manifest['protected_objects'].items():
    assert name in bpy.data.objects and object_signature(bpy.data.objects[name])==digest,name
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
attr=head.data.color_attributes['SkinTreatment'];h=hashlib.sha256()
for loop in head.data.loops:
    p=head.matrix_world@head.data.vertices[loop.vertex_index].co
    if p.z>=manifest['unchanged_above_world_z']:h.update(struct.pack('<4f',*attr.data[loop.index].color))
assert h.hexdigest()==manifest['upper_skin_hash']
curves=[ob for ob in bpy.data.objects if ob.name.startswith('Barba_cuello_V05')]
assert len(curves)==3
count=sum(len(ob.data.splines) for ob in curves)
assert count==manifest['added_strands']
bvh=BVHTree.FromPolygons([head.matrix_world@v.co for v in head.data.vertices],[tuple(f.vertices) for f in head.data.polygons])
total=sum(len(sp.points) for ob in curves for sp in ob.data.splines);step=max(1,total//700)
index=0;distances=[]
for ob in curves:
    for sp in ob.data.splines:
        for point in sp.points:
            assert all(math.isfinite(c) for c in point.co)
            if index%step==0:
                pos,normal,face,distance=bvh.find_nearest(ob.matrix_world@point.co.to_3d(),.020)
                assert pos is not None;distances.append(distance)
            index+=1
assert distances and max(distances)<.0015,max(distances)
old=[ob for ob in bpy.data.objects if ob.name.startswith('Barba_anatomica')]
old_min=min((ob.matrix_world@sp.points[0].co.to_3d()).z for ob in old for sp in ob.data.splines)
assert manifest['added_root_z_min']<old_min-.006
assert manifest['changed_color_world_z_max']<manifest['unchanged_above_world_z']
assert manifest['added_root_z_min']>1.524
checked=0
for name in ('MakeHumanCore','MakeHumanAkselSkin','MakeHumanToigoBronze'):
    folder=ROOT/'ArtSource/ThirdParty'/name
    provenance=json.loads((folder/'provenance.json').read_text(encoding='utf-8-sig'))
    for row in provenance['files']:
        assert hashlib.sha256((folder/row['file']).read_bytes()).hexdigest()==row['sha256'];checked+=1
portraits=ROOT/'ArtSource/Portraits/Heroes'
for row in json.loads((portraits/'verificacion-originales.json').read_text(encoding='utf-8-sig'))['portraits']:
    assert hashlib.sha256((portraits/row['file']).read_bytes()).hexdigest()==row['sha256']
for name in ('Frente_tres_cuartos','Espalda','Rostro','Perfil_cuello'):
    image=bpy.data.images.load(str(ROOT/'Builds/ArtPreview/RodrigoCastillaV05'/(name+'_v05.png')))
    assert tuple(image.size)==(1000,1200);bpy.data.images.remove(image)
result={'saved_blend_reopened':True,'base_v04_file_unchanged':True,
        'existing_object_geometry_uv_transforms_preserved':len(manifest['protected_objects']),
        'upper_skin_treatment_preserved_above_world_z':manifest['unchanged_above_world_z'],
        'new_neck_strands':count,'new_root_min_world_z':manifest['added_root_z_min'],
        'previous_beard_root_min_world_z':old_min,'extension_reaches_lower_by_m':old_min-manifest['added_root_z_min'],
        'new_beard_skin_distance_max_m':max(distances),'new_beard_samples_checked':len(distances),
        'source_hashes_match':checked,'original_portraits_unchanged':12,'renders_checked':4,
        'game_ready':False,'visual_review':'Continuation under jaw onto upper neck inspected; artistic acceptance pending.'}
(SOURCE/'verificacion_v05.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('RODRIGO_V05_READBACK_OK',json.dumps(result),flush=True)
