"""Ejecutar dentro de Substance 3D Painter con el proyecto de la fortaleza de Castilla abierto.

Exporta SOLO el Texture Set principal (el atlas de Tripo, ya pintado) a
Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3/Painter con el preset de Unity URP (Metallic Standard):
  <set>_AlbedoTransparency.png, <set>_Normal.png y <set>_MetallicSmoothness.png.
Pendon, Tapiz y Almenas no se exportan: su textura ya es la definitiva y no se pinta en Painter.
CastillaMapModelReviewBuilder usa esos archivos si existen; si no, mantiene la textura de Tripo.
"""
from pathlib import Path

import substance_painter as sp

ROOT = Path(__file__).resolve().parents[2]    # raíz del repositorio (el script está en Tools/Painter)
SALIDA = ROOT / 'Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3/Painter'
EXTRAS = {'Pendon', 'Tapiz', 'Almenas'}


def main():
    if not (sp.project.is_open() and sp.project.is_in_edition_state()):
        raise RuntimeError('Abre el proyecto de la fortaleza y espera a que termine de cargar.')
    principal = next((ts for ts in sp.textureset.all_texture_sets() if ts.name() not in EXTRAS), None)
    if principal is None:
        raise RuntimeError('No encuentro el Texture Set principal.')
    preset = next((p for p in sp.export.list_resource_export_presets()
                   if p.resource_id.name == 'Unity Universal Render Pipeline (Metallic Standard)'), None)
    if preset is None:
        raise RuntimeError('No existe el preset de exportación de Unity URP en esta versión de Painter.')
    SALIDA.mkdir(parents=True, exist_ok=True)
    config = {
        'exportShaderParams': False,
        'exportPath': str(SALIDA),
        'defaultExportPreset': preset.resource_id.url(),
        'exportList': [{'rootPath': principal.name()}],
        'exportParameters': [{'parameters': {'fileFormat': 'png', 'bitDepth': '8', 'sizeLog2': 12, 'paddingAlgorithm': 'infinite'}}],
    }
    resultado = sp.export.export_project_textures(config)
    if resultado.status != sp.export.ExportStatus.Success:
        raise RuntimeError(resultado.message)
    print('EXPORTADO en', SALIDA)
    for clave, rutas in resultado.textures.items():
        for r in rutas:
            print('  ', r)


main()
