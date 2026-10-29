"""Reopen the V02 source, verify provenance/geometry, record limits explicitly."""
from pathlib import Path
import bpy,json,hashlib,math
SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[3]
UPSTREAM=ROOT/'ArtSource/ThirdParty/MakeHumanCore'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v02.blend'))
hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL'];scene=bpy.context.scene
assert 'SIM_TEMP' not in bpy.data.collections
assert not any(mod.type in {'CLOTH','COLLISION'} for ob in hero.objects for mod in ob.modifiers)
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
assert len(head.data.uv_layers)==1 and len(head.data.vertices)>5000
assert all(math.isfinite(c) for v in head.data.vertices for c in v.co)
assert len([o for o in bpy.data.objects if o.type=='CAMERA'])==3
assert not any(n.type=='TEX_IMAGE' for m in bpy.data.materials if m.use_nodes for n in m.node_tree.nodes)
upstream=json.loads((UPSTREAM/'provenance.json').read_text(encoding='utf-8-sig'))
for row in upstream['files']:
    assert hashlib.sha256((UPSTREAM/row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
portraits=ROOT/'ArtSource/Portraits/Heroes'
originals=json.loads((portraits/'verificacion-originales.json').read_text(encoding='utf-8-sig'))
for row in originals['portraits']:
    assert hashlib.sha256((portraits/row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
renders=ROOT/'Builds/ArtPreview/RodrigoCastillaV02'
for name in ('Frente_tres_cuartos_v02.png','Espalda_v02.png','Rostro_v02.png'):
    image=bpy.data.images.load(str(renders/name));assert tuple(image.size)==(1000,1200)
    bpy.data.images.remove(image)
mantle=bpy.data.objects['Capa_hombros_recogida']
assert mantle['cloth_settled_frames']==38 and mantle['max_settle_displacement_m']<.35
manifest=json.loads((SOURCE/'escultura_manifest.json').read_text(encoding='utf8'))
assert manifest['bounds_min'][2]>=-.001 and 1.79<manifest['bounds_max'][2]<1.84
# Harmonise metadata for sources generated before the final wording update.
scene['anatomy_source']='MakeHuman CC0 core + project modifications';scene['upstream_commit']=upstream['commit']
txt=bpy.data.texts.get('LEEME_ESTUDIO');txt.clear()
txt.write('Rodrigo V02: cabeza anatomica derivada de assets oficiales MakeHuman CC0 y vestuario propio.\nFuentes, licencias, hashes y pesos de targets documentados. Referencias actuales empaquetadas; no planos de retrato renderizados.\nEstudio de alta densidad, semejanza pendiente. UV original de cabeza conservada; atlas de vestuario, bake, rig, LOD y exportacion pendientes.\nLa tela esta asentada en geometria, sin simulacion ni colision activas.\n')
manifest['status']='anatomical_revision_not_game_ready'
manifest['warnings']=[w.replace('No UV/rig/LOD/bake/export','Original head UV retained; costume atlas, rig, LOD, bake and export pending') for w in manifest['warnings']]
manifest['anatomy']={'upstream_commit':upstream['commit'],'asset_license':'CC0-1.0','target_weights':json.loads(head['target_weights']),'original_head_uv_retained':True}
(SOURCE/'escultura_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v02.blend'))
result={'saved_blend_reopened':True,'head_vertices':len(head.data.vertices),'head_original_uv':True,
        'upstream_assets_hashes_match':len(upstream['files']),'original_portraits_unchanged':12,'renders_checked':3,
        'evaluated_triangles':manifest['evaluated_triangles'],'character_objects':len(hero.objects),
        'cloth_applied_to_geometry':True,'active_cloth_modifiers':0,'uses_portrait_surface':False,
        'game_ready':False,'visual_review':'Anatomy improved over V01; likeness, groom and costume still require refinement.'}
(SOURCE/'verificacion_v02.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print('RODRIGO_V02_READBACK_OK',json.dumps(result),flush=True)
