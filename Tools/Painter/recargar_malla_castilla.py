"""Ejecutar dentro de Painter con el proyecto de la fortaleza de Castilla abierto.

Recarga la malla del FBX actual (Tools/Blender/castilla_retoques.py) sin tocar las capas, los generadores ni las pinceladas.
Sirve cuando se cambia la geometría en Blender sin mover las UV, por ejemplo la orientación de las banderas.
Escribe su resultado en Builds/painter-recarga.txt.
"""
import traceback
from pathlib import Path

import substance_painter as sp

ROOT = Path(__file__).resolve().parents[2]    # raíz del repositorio (el script está en Tools/Painter)
FBX = ROOT / 'Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3/SM_Fortaleza_Castilla_Castilla3.fbx'
LOG = ROOT / 'Builds/painter-recarga.txt'


def escribir(texto):
    print(texto)
    LOG.parent.mkdir(parents=True, exist_ok=True)
    LOG.write_text(texto, encoding='utf-8')


def capas():
    return {ts.name(): [n.get_name() for n in sp.layerstack.get_root_layer_nodes(ts.get_stack())] for ts in sp.textureset.all_texture_sets()}


def terminado(estado):
    try:
        if estado != sp.project.ReloadMeshStatus.SUCCESS:
            raise RuntimeError('La recarga no terminó bien: %s' % estado)
        despues = capas()
        if despues != ANTES:
            raise RuntimeError('Cambiaron las capas tras la recarga: %s' % despues)
        sp.project.execute_when_not_busy(lambda: (sp.project.save(), escribir('OK: malla recargada, %d conjuntos de texturas y capas intactas, proyecto guardado.' % len(despues))))
    except Exception:
        escribir('ERROR al terminar la recarga:\n' + traceback.format_exc())


try:
    assert sp.project.is_open() and sp.project.is_in_edition_state(), 'Abre el proyecto y vuelve al modo de pintura.'
    assert FBX.is_file(), 'No existe %s' % FBX
    ANTES = capas()
    escribir('Recargando la malla… (esto puede tardar unos segundos)')
    sp.project.reload_mesh(str(FBX), sp.project.MeshReloadingSettings(import_cameras=False, preserve_strokes=True, auto_unwrap_settings=None), terminado)
except Exception:
    escribir('ERROR:\n' + traceback.format_exc())
