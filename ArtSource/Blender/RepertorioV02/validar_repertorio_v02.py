"""Validate original Blender sources and reimport static FBX deliveries.

Run after all four families. -- --write-import-meta creates missing Unity metadata.
This is file validation, not a Unity integration test.
"""
from pathlib import Path
import json,math,re,sys,uuid
import bpy

SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[2]
EXPORT=ROOT/'Assets/Fulgor/Art/RepertorioV02'
PREVIEW=ROOT/'Builds/ArtPreview/RepertorioV02'
FAMILIES={'Fortaleza':1,'Tropas':4,'Heroes':2,'Hueste':1}
reports=[]
for family,count in FAMILIES.items():
    manifest=json.loads((EXPORT/family/'repertorio_manifest.json').read_text())
    assert len(manifest)==count,family
    bpy.ops.wm.read_factory_settings(use_empty=True)
    names=[m['name'] for m in manifest]
    with bpy.data.libraries.load(str(SOURCE/(family+'_natural_v02.blend'))) as (available,loaded):
        assert all(n in available.objects for n in names)
        assert sum(n.startswith('SRC_') for n in available.objects)==count
        loaded.objects=names
    for obj,metric in zip(loaded.objects,manifest):
        assert obj and len(obj.data.uv_layers)==1 and len(obj.data.materials)==1,obj
        assert tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1),obj.name
        assert len(obj.vertex_groups)>=(3 if family=='Fortaleza' else 8),obj.name
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        obj.data.calc_loop_triangles()
        assert len(obj.data.loop_triangles)==metric['triangles'],obj.name
        images=[n.image for n in obj.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE']
        assert len(images)==4
        for image in images:
            assert image.packed_file and tuple(image.size)==(metric['texture_resolution'],)*2,image.name
            assert (EXPORT/family/'Textures'/(image.name+'.png')).is_file(),image.name
            expected='sRGB' if image.name.endswith('_BaseColor') else 'Non-Color'
            assert image.colorspace_settings.name==expected,image.name
        assert metric['bounds'][2][0]>=-.0001
        assert metric['bounds'][2][1] <= (.82 if family=='Fortaleza' else .57)
        if family=='Fortaleza':
            assert metric['bounds'][0][1]-metric['bounds'][0][0] <=1.18
        reports.append(dict(family=family,**metric))

for metric in reports:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(EXPORT/metric['family']/(metric['name']+'.fbx')))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(meshes)==1,metric['name']
    obj=meshes[0];obj.data.calc_loop_triangles()
    assert len(obj.data.loop_triangles)==metric['triangles'],metric['name']
    assert len(obj.data.materials)==1 and len(obj.data.uv_layers)==1,metric['name']
    points=[obj.matrix_world@v.co for v in obj.data.vertices]
    bounds=[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)]
    assert all(abs(a-b)<.0001 for p,q in zip(bounds,metric['bounds']) for a,b in zip(p,q)),metric['name']
    assert all(abs(v)<.0001 for v in obj.location)

fbx=list(EXPORT.rglob('*.fbx'));png=list(EXPORT.rglob('*.png'))
assert len(fbx)==8 and len(png)==32
if '--write-import-meta' in sys.argv:
    model_template=(ROOT/'Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx.meta').read_text()
    texture_root=ROOT/'Assets/Fulgor/Art/Terrain/MontanaNaturalV02/Textures'
    color_template=(texture_root/'T_MontanaNatural_BaseColor.png.meta').read_text()
    normal_template=(texture_root/'T_MontanaNatural_Normal.png.meta').read_text()
    def add_meta(path,content):
        target=Path(str(path)+'.meta')
        if not target.exists():
            content=re.sub(r'(?m)^guid: .+$','guid: '+uuid.uuid4().hex,content)
            target.write_text(content,encoding='utf-8')
    for path in [EXPORT]+[p for p in EXPORT.rglob('*') if p.is_dir()]:
        add_meta(path,'fileFormatVersion: 2\nguid: NEW\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    for path in fbx:add_meta(path,model_template)
    for path in png:
        content=normal_template if path.stem.endswith('_Normal') else color_template
        if not path.stem.endswith('_BaseColor'):content=content.replace('sRGBTexture: 1','sRGBTexture: 0')
        resolution=2048 if 'FortalezaCastellana' in path.name else 1024
        content=re.sub(r'maxTextureSize: \d+','maxTextureSize: '+str(resolution),content)
        add_meta(path,content)
    for path in EXPORT.rglob('*.json'):
        add_meta(path,'fileFormatVersion: 2\nguid: NEW\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
for path in [EXPORT]+list(EXPORT.rglob('*')):
    if path.suffix!='.meta':assert Path(str(path)+'.meta').is_file(),path
for path in png:
    content=Path(str(path)+'.meta').read_text()
    assert 'sRGBTexture: '+str(int(path.stem.endswith('_BaseColor'))) in content,path
    assert re.search(r'(?m)^  textureType: '+str(int(path.stem.endswith('_Normal')))+r'$',content),path
    assert 'maxTextureSize: '+str(2048 if 'FortalezaCastellana' in path.name else 1024) in content,path
guids={}
for path in (ROOT/'Assets').rglob('*.meta'):
    match=re.search(r'(?m)^guid: ([a-f0-9]{32})$',path.read_text(encoding='utf-8-sig'))
    if match:
        assert match[1] not in guids,'Duplicate GUID: '+str(path)
        guids[match[1]]=str(path)
result=dict(models=8,maps=32,source_families=4,source_checks='passed',fbx_reimport_checks='passed',metadata_checks='passed',
    unity_integration='not performed',animation='not implemented',models_detail=reports)
(PREVIEW/'validacion_v02.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('FULGOR_NATURAL_VALIDATION_OK',len(fbx),'models',len(png),'maps',flush=True)


