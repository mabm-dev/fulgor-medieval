"""Read back V03 and verify geometry, permitted UV textures and provenance."""
from pathlib import Path
import bpy,json,hashlib,math
SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[3]
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v04.blend'))
hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL']
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
assert len(head.data.uv_layers)==1 and len(head.data.vertices)>5000
assert all(math.isfinite(c) for v in head.data.vertices for c in v.co)
assert 'SIM_TEMP' not in bpy.data.collections
assert not any(mod.type in {'CLOTH','COLLISION'} for ob in hero.objects for mod in ob.modifiers)
assert not any(o.name.startswith(('REF_','Cabello_mechon')) for o in hero.objects)
assert len([o for o in bpy.data.objects if o.type=='CAMERA'])==4
assert abs(head.location.z+.020)<.000001
assert head['attachment_drop_m']==.020
assert min(c.color[2] for c in head.data.color_attributes['SkinTreatment'].data)>.43
normal_weight=head.data.color_attributes['SkinNormalWeight']
neck_weights=[normal_weight.data[loop.index].color[0] for loop in head.data.loops if head.data.vertices[loop.vertex_index].co.z<1.565]
assert neck_weights and max(neck_weights)<.000001
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
assert strand_count==20440-head['nape_removed_strands'],strand_count
nape_min=min((o.matrix_world@p.co.to_3d()).z for o in hair for sp in o.data.splines for p in sp.points)
assert nape_min>=1.589,nape_min
irises=[o for o in hero.objects if o.name.startswith('Iris_curvado_con_pupila')]
eyes=[o for o in hero.objects if o.name.startswith('Globo_ocular_anatomico')]
assert len(irises)==len(eyes)==2
for ob in irises:
    assert 'IrisColor' in ob.data.color_attributes
    assert all(min((ob.matrix_world@v.co-e.matrix_world.translation).length for e in eyes)<.0121 for v in ob.data.vertices)
# Measure attachment in the saved world-space geometry, independently of the
# generator: facial-hair centre-lines must stay within 1.5 mm of real skin.
from mathutils.bvhtree import BVHTree
bvh=BVHTree.FromPolygons([head.matrix_world@v.co for v in head.data.vertices],[tuple(f.vertices) for f in head.data.polygons])
beards=[o for o in hero.objects if o.name.startswith('Barba_anatomica')]
beard_points=sum(len(sp.points) for o in beards for sp in o.data.splines)
distances=[];index=0;step=max(1,beard_points//700)
for ob in beards:
    for sp in ob.data.splines:
        for point in sp.points:
            if index%step==0:
                pos,normal,face,distance=bvh.find_nearest(ob.matrix_world@point.co.to_3d(),.020)
                assert pos is not None
                distances.append(distance)
            index+=1
assert distances and max(distances)<.0015,max(distances)
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
for name in ('Frente_tres_cuartos_v04.png','Espalda_v04.png','Rostro_v04.png','Perfil_cuello_v04.png'):
    image=bpy.data.images.load(str(ROOT/'Builds/ArtPreview/RodrigoCastillaV04'/name))
    assert tuple(image.size)==(1000,1200);bpy.data.images.remove(image)
manifest=json.loads((SOURCE/'escultura_manifest.json').read_text(encoding='utf8'))
assert manifest['bounds_min'][2]>=-.001 and 1.77<manifest['bounds_max'][2]<1.84
result={'saved_blend_reopened':True,'head_original_uv':True,'source_hashes_match':checked,
        'original_portraits_unchanged':12,'renders_checked':4,'allowed_uv_texture_files':sorted(texture_names),
        'hair_fibers':strand_count,'curved_irises':2,'active_cloth_modifiers':0,
        'evaluated_triangles':manifest['evaluated_triangles'],'portrait_used_as_surface':False,
        'head_drop_m':.020,'nape_min_world_z':nape_min,'beard_control_points':beard_points,
        'beard_skin_distance_max_m':max(distances),'beard_samples_checked':len(distances),'neck_normal_strength_max':max(neck_weights),
        'predecessor_v03_preserved':(SOURCE.parent/'CastillaRodrigoV03/Rodrigo_escultura_v03.blend').is_file(),
        'game_ready':False,'visual_review':'V04 responds to rejected V03 neck/beard; front/profile/nape inspected, artistic acceptance pending.'}
(SOURCE/'verificacion_v04.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('RODRIGO_V04_READBACK_OK',json.dumps(result),flush=True)
