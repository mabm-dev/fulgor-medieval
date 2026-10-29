"""Check the 23 detail exports and create missing Unity import metadata.

Run in Blender background, adding -- --write-import-meta to create .meta files.
Does not open Unity or change gameplay scenes.
"""
from pathlib import Path
import json
import math
import re
import sys
import uuid
import bpy

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
EXPORT = ROOT / 'Assets/Fulgor/Art/DetalleV04'
PREVIEW = ROOT / 'Builds/ArtPreview/DetalleV04'
EXPECTED = {'Montana': 1, 'Biomas': 3, 'Rural': 8, 'Agua': 7, 'Puente': 2, 'Molino': 2}
write_meta = '--write-import-meta' in sys.argv
reports = []
for family, count in EXPECTED.items():
    manifest = json.loads((EXPORT/family/'detalle_manifest.json').read_text(encoding='utf-8'))
    assert len(manifest) == count, family
    names = [m['name'] for m in manifest]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    with bpy.data.libraries.load(str(SOURCE/(family+'_detalle_v04.blend')), link=False) as (available, loaded):
        assert all(n in available.objects for n in names), family
        loaded.objects = names
    for obj, metric in zip(loaded.objects, manifest):
        assert obj and obj.type == 'MESH', metric['name']
        assert len(obj.data.uv_layers) == 1, obj.name
        assert len(obj.data.materials) == 1, obj.name
        assert tuple(obj.location) == (0,0,0) and tuple(obj.scale) == (1,1,1), obj.name
        obj.data.calc_loop_triangles()
        assert len(obj.data.loop_triangles) == metric['triangles'], obj.name
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), obj.name
        images = [n.image for n in obj.data.materials[0].node_tree.nodes if n.type == 'TEX_IMAGE']
        assert len(images) == 3 and all(i.packed_file for i in images), obj.name
        for image in images:
            assert tuple(image.size) == (metric['texture_resolution'],)*2, image.name
            assert (EXPORT/family/'Textures'/(image.name+'.png')).is_file(), image.name
        if family == 'Agua':
            with bpy.data.libraries.load(str(SOURCE.parent/'Terrain'/metric['source_blend']), link=False) as (available, loaded_original):
                loaded_original.objects = [metric['source_model']]
            original = loaded_original.objects[0]
            def boundary(mesh):
                result = []
                for vertex in mesh.vertices:
                    x,y,z = vertex.co
                    gap = min(math.sqrt(3)/2-abs(x), (math.sqrt(3)-abs(x)-math.sqrt(3)*abs(y))/2)
                    if abs(gap) < 1e-5:
                        result.append(tuple(round(c,5) for c in vertex.co))
                return set(result)
            assert boundary(original.data) == boundary(obj.data), 'Modified water border: '+obj.name
        reports.append(dict(family=family, **metric))

# Reimport the deliverable rather than relying only on the generator's JSON.
for metric in reports:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = EXPORT/metric['family']/(metric['name']+'.fbx')
    bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(meshes) == 1, path
    obj = meshes[0]
    obj.data.calc_loop_triangles()
    assert len(obj.data.loop_triangles) == metric['triangles'], path
    assert len(obj.data.uv_layers) == 1 and len(obj.data.materials) == 1, path
    points = [obj.matrix_world @ v.co for v in obj.data.vertices]
    bounds = [[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)]
    assert all(abs(a-b)<1e-4 for pair,expected in zip(bounds,metric['bounds']) for a,b in zip(pair,expected)), path
    assert all(abs(c)<1e-5 for c in obj.location), path

fbx = list(EXPORT.rglob('*.fbx'))
png = list(EXPORT.rglob('*.png'))
assert len(fbx) == 23 and len(png) == 69

if write_meta:
    model_template = (ROOT/'Assets/Fulgor/Art/Validation/SM_VisualScaleValidation.fbx.meta').read_text()
    texture_root = ROOT/'Assets/Fulgor/Art/Terrain/MontanaNaturalV02/Textures'
    color_template = (texture_root/'T_MontanaNatural_BaseColor.png.meta').read_text()
    normal_template = (texture_root/'T_MontanaNatural_Normal.png.meta').read_text()
    def add_meta(path, text):
        target = Path(str(path)+'.meta')
        if not target.exists():
            text = re.sub(r'(?m)^guid: .+$','guid: '+uuid.uuid4().hex,text)
            target.write_text(text,encoding='utf-8')
    for directory in [EXPORT]+[p for p in EXPORT.rglob('*') if p.is_dir()]:
        add_meta(directory,'fileFormatVersion: 2\nguid: NEW\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    for path in fbx:
        add_meta(path,model_template)
    for path in png:
        content = normal_template if path.stem.endswith('_Normal') else color_template
        if path.stem.endswith('_Roughness'):
            content = content.replace('sRGBTexture: 1','sRGBTexture: 0')
        resolution = 1024 if 'MolinoAgua_Rueda_' in path.name else 2048
        content = re.sub(r'maxTextureSize: \d+','maxTextureSize: '+str(resolution),content)
        add_meta(path,content)
    for path in EXPORT.rglob('*.json'):
        add_meta(path,'fileFormatVersion: 2\nguid: NEW\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')

# Verify import settings, including existing metadata, without rewriting GUIDs.
for path in [EXPORT]+list(EXPORT.rglob('*')):
    if path.suffix == '.meta':
        continue
    assert Path(str(path)+'.meta').is_file(), 'Missing metadata: '+str(path)
for path in png:
    content = Path(str(path)+'.meta').read_text()
    expected_srgb = 1 if path.stem.endswith('_BaseColor') else 0
    expected_type = 1 if path.stem.endswith('_Normal') else 0
    resolution = 1024 if 'MolinoAgua_Rueda_' in path.name else 2048
    assert 'sRGBTexture: '+str(expected_srgb) in content, path
    assert re.search(r'(?m)^  textureType: '+str(expected_type)+r'$',content), path
    assert 'maxTextureSize: '+str(resolution) in content, path
guid_paths = {}
for path in (ROOT/'Assets').rglob('*.meta'):
    match = re.search(r'(?m)^guid: ([a-f0-9]{32})$',path.read_text(encoding='utf-8-sig'))
    if match:
        assert match[1] not in guid_paths, 'Duplicate GUID: '+str(path)
        guid_paths[match[1]] = str(path)

result = {
    'models':len(fbx),'maps':len(png),'source_families':len(EXPECTED),
    'source_blend_checks':'passed','fbx_reimport_checks':'passed',
    'water_boundary_geometry':'unchanged for seven modular models',
    'metadata_checks':'passed','unity_integration':'not performed',
    'triangles_total_unique_models':sum(m['triangles'] for m in reports),
    'models_detail':reports
}
(PREVIEW/'validacion_v04.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('FULGOR_DETAIL_VALIDATION_OK',len(fbx),'models',len(png),'maps',flush=True)
