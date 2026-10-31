"""Keep the original SPP intact; reload the UV-compatible correction in a copy."""
from pathlib import Path
import hashlib, json
import substance_painter as sp

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtSource/Substance/Fortaleza/Fortaleza_Castilla_Corregida_Painter.spp'
STATUS=ROOT/'Builds/FortressGeometry/painter-corrected-status.json'
assert sp.project.is_open() and sp.project.is_in_edition_state()
assert not OUT.exists(), 'La copia corregida ya existe; no sobrescribir trabajo posterior.'
ORIGINAL=Path(sp.project.file_path())
assert ORIGINAL.name=='Fortaleza_Castilla_Color_20261031_041918.spp', str(ORIGINAL)
def digest(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for chunk in iter(lambda:f.read(4*1024*1024),b''):h.update(chunk)
 return h.hexdigest()
ORIGINAL_HASH=digest(ORIGINAL)
TEXTURES={str(p):digest(p) for p in (ROOT/'Assets/Fulgor/Art/RawFortress/Painter4K').glob('*.png')}
LAYERS={ts.name():[n.get_name() for n in sp.layerstack.get_root_layer_nodes(ts.get_stack())] for ts in sp.textureset.all_texture_sets()}
sp.project.save_as(str(OUT))
STATUS.write_text(json.dumps({'status':'reloading','original':str(ORIGINAL),'copy':str(OUT)}),encoding='utf-8')
def finished(status):
 try:
  assert status==sp.project.ReloadMeshStatus.SUCCESS, str(status)
  current={ts.name():[n.get_name() for n in sp.layerstack.get_root_layer_nodes(ts.get_stack())] for ts in sp.textureset.all_texture_sets()}
  assert current==LAYERS, 'Texture sets or layers changed'
  assert digest(ORIGINAL)==ORIGINAL_HASH, 'Original SPP changed'
  assert all(digest(Path(p))==h for p,h in TEXTURES.items()), 'Painter exports changed'
  sp.project.save()
  STATUS.write_text(json.dumps({'status':'OK','copy':str(OUT),'mesh':sp.project.last_imported_mesh_path(),'layers':current,'original_spp_sha256':ORIGINAL_HASH,'original_spp_unchanged':True,'exported_textures_unchanged':True,'auto_unwrap':False},indent=2),encoding='utf-8')
 except Exception as error:
  STATUS.write_text(json.dumps({'status':'FAILED','error':str(error),'original':str(ORIGINAL),'copy':str(OUT)}),encoding='utf-8')
  raise
sp.project.reload_mesh(str(ROOT/'Assets/Fulgor/Art/RawFortress/SM_Fortaleza_completa_corregida.fbx'),sp.project.MeshReloadingSettings(import_cameras=False,preserve_strokes=True,auto_unwrap_settings=None),finished)
