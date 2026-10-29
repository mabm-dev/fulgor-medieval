"""Read back the saved source and check geometry/reference separation."""
from pathlib import Path
import bpy,json,math,hashlib
SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[3]
blend=SOURCE/'Rodrigo_escultura_v01.blend'
bpy.ops.wm.open_mainfile(filepath=str(blend))
hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL']
assert len(hero.objects)>100
assert not any(o.type in {'CAMERA','LIGHT','EMPTY'} for o in hero.objects)
assert len([o for o in bpy.data.objects if o.type=='CAMERA'])==3
assert len(bpy.data.images)>=2
assert all(i.packed_file for i in bpy.data.images if i.type=='IMAGE')
assert not any(n.type=='TEX_IMAGE' for m in bpy.data.materials if m.use_nodes for n in m.node_tree.nodes)
report=json.loads((SOURCE/'escultura_manifest.json').read_text(encoding='utf8'))
assert report['bounds_min'][2]>=-.001
assert 1.79<report['bounds_max'][2]<1.84
assert report['evaluated_triangles']>100000
portraits=ROOT/'ArtSource/Portraits/Heroes'
originals=json.loads((portraits/'verificacion-originales.json').read_text(encoding='utf-8-sig'))
for row in originals['portraits']:
    assert hashlib.sha256((portraits/row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
renders=ROOT/'Builds/ArtPreview/RodrigoCastillaV01'
for name in ('Frente_tres_cuartos_v01.png','Espalda_v01.png','Rostro_v01.png'):
    image=bpy.data.images.load(str(renders/name));assert tuple(image.size)==(1000,1200)
    assert (renders/name).stat().st_mtime>blend.stat().st_mtime-180
result={'saved_blend_reopened':True,'character_objects':len(hero.objects),'evaluated_triangles':report['evaluated_triangles'],
        'packed_reference_images':True,'portrait_render_surfaces':False,'original_portraits_unchanged':12,
        'renders_checked':3,'game_ready':False,'visual_review':'Still stylized. Face likeness, hair mass and cloth require artistic rework.'}
(SOURCE/'verificacion_escultura.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('RODRIGO_READBACK_OK',json.dumps(result),flush=True)
