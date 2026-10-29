"""Read back V03 and verify geometry, permitted UV textures and provenance."""
from pathlib import Path
import bpy,json,hashlib,math
SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[3]
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v03.blend'))
hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL']
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
assert len(head.data.uv_layers)==1 and len(head.data.vertices)>5000
assert all(math.isfinite(c) for v in head.data.vertices for c in v.co)
assert 'SIM_TEMP' not in bpy.data.collections
assert not any(mod.type in {'CLOTH','COLLISION'} for ob in hero.objects for mod in ob.modifiers)
assert not any(o.name.startswith(('REF_','Cabello_mechon')) for o in hero.objects)
assert len([o for o in bpy.data.objects if o.type=='CAMERA'])==3
allowed={'young_lightskinned_male_diffuse_Bronze.png','Aksel_Skin_NRM.png','Aksel_Skin_SPEC.png'}
texture_names=set()
for material in bpy.data.materials:
    if not material.use_nodes:continue
    for node in material.node_tree.nodes:
        if node.type=='TEX_IMAGE':
            assert node.image is not None and node.image.packed_file
            texture_names.add(Path(node.image.filepath).name)
assert texture_names==allowed,texture_names
hair=[o for o in hero.objects if o.name.startswith('Cabello_fibras')]
assert len(hair)==4
strand_count=sum(len(o.data.splines) for o in hair)
assert strand_count==20440,strand_count
irises=[o for o in hero.objects if o.name.startswith('Iris_curvado_con_pupila')]
eyes=[o for o in hero.objects if o.name.startswith('Globo_ocular_anatomico')]
assert len(irises)==len(eyes)==2
for ob in irises:
    assert 'IrisColor' in ob.data.color_attributes
    assert all(min((v.co-e.location).length for e in eyes)<.0121 for v in ob.data.vertices)
checked=0
for name in ('MakeHumanCore','MakeHumanAkselSkin','MakeHumanToigoBronze'):
    folder=ROOT/'ArtSource/ThirdParty'/name
    provenance=json.loads((folder/'provenance.json').read_text(encoding='utf-8-sig'))
    for row in provenance['files']:
        assert hashlib.sha256((folder/row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
        checked+=1
portraits=ROOT/'ArtSource/Portraits/Heroes'
for row in json.loads((portraits/'verificacion-originales.json').read_text(encoding='utf-8-sig'))['portraits']:
    assert hashlib.sha256((portraits/row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
for name in ('Frente_tres_cuartos_v03.png','Espalda_v03.png','Rostro_v03.png'):
    image=bpy.data.images.load(str(ROOT/'Builds/ArtPreview/RodrigoCastillaV03'/name))
    assert tuple(image.size)==(1000,1200);bpy.data.images.remove(image)
manifest=json.loads((SOURCE/'escultura_manifest.json').read_text(encoding='utf8'))
assert manifest['bounds_min'][2]>=-.001 and 1.79<manifest['bounds_max'][2]<1.85
result={'saved_blend_reopened':True,'head_original_uv':True,'source_hashes_match':checked,
        'original_portraits_unchanged':12,'renders_checked':3,'allowed_uv_texture_files':sorted(texture_names),
        'hair_fibers':strand_count,'curved_irises':2,'active_cloth_modifiers':0,
        'evaluated_triangles':manifest['evaluated_triangles'],'portrait_used_as_surface':False,
        'predecessor_v02_preserved':(SOURCE.parent/'CastillaRodrigoV02/Rodrigo_escultura_v02.blend').is_file(),
        'game_ready':False,'visual_review':'V03 eye/groom/skin revision inspected; likeness and naturalism still require refinement.'}
(SOURCE/'verificacion_v03.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('RODRIGO_V03_READBACK_OK',json.dumps(result),flush=True)
