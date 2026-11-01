"""Ejecutar dentro de Painter con el proyecto de la fortaleza de Castilla abierto (después de preparar_castilla.py).

Pone un generador en la máscara de las capas '60 Polvo' (polvo) y '50 Humedad' (humedad) y de '10 Piedra' (desgaste de bordes),
que usan los mapas horneados de AO, curvatura y posición. Los nombres de los generadores dependen de la instalación, así que
se buscan entre los disponibles; el resultado, con la lista completa, se escribe en Builds/painter-generadores.txt.
Es repetible: si una máscara ya tiene un generador, no añade otro.
"""
import traceback
from pathlib import Path

import substance_painter as sp

ROOT = Path(__file__).resolve().parents[2]    # raíz del repositorio (el script está en Tools/Painter)
LOG = ROOT / 'Builds/painter-generadores.txt'
SALIDA = []

# (prefijo de la capa, palabras clave del generador por orden de preferencia, opacidad de la capa)
PLAN = [
    ('60 Polvo', ['dust', 'dirt'], .35),
    ('50 Humedad', ['leak', 'fluid', 'dirt'], .4),
    ('10 Piedra', ['edge', 'wear', 'dirt'], .3),
]


def log(*partes):
    texto = ' '.join(str(p) for p in partes)
    print(texto)
    SALIDA.append(texto)


def guardar_log():
    LOG.parent.mkdir(parents=True, exist_ok=True)
    LOG.write_text('\n'.join(SALIDA), encoding='utf-8')


def generadores():
    res = []
    for r in sp.resource.search(sp.resource.StandardQuery.ALL_RESOURCES):
        try:
            if sp.resource.Usage.GENERATOR in r.usages():
                res.append((r.gui_name(), r))
        except Exception:
            continue
    return res


def elegir(disponibles, claves):
    for clave in claves:
        for nombre, r in disponibles:
            if clave in nombre.lower():
                return nombre, r
    return None, None


def principal():
    return next(ts for ts in sp.textureset.all_texture_sets() if ts.name() not in ('Pendon', 'Tapiz', 'Almenas'))


def main():
    try:
        if not (sp.project.is_open() and sp.project.is_in_edition_state()):
            raise RuntimeError('Abre el proyecto y vuelve al modo de pintura.')
        disp = generadores()
        log('Generadores disponibles (%d): %s' % (len(disp), ', '.join(sorted(n for n, _ in disp))))
        ts = principal()
        capas = {n.get_name(): n for n in sp.layerstack.get_root_layer_nodes(ts.get_stack())}
        for prefijo, claves, opacidad in PLAN:
            capa = next((n for nombre, n in capas.items() if nombre.startswith(prefijo)), None)
            if capa is None:
                log('No encuentro la capa', prefijo)
                continue
            nombre, recurso = elegir(disp, claves)
            if recurso is None:
                log('Capa', prefijo, ': ningún generador coincide con', claves)
                continue
            if not capa.has_mask():
                capa.add_mask(sp.layerstack.MaskBackground.Black)
            pos = sp.layerstack.InsertPosition.inside_node(capa, sp.layerstack.NodeStack.Mask)
            efecto = sp.layerstack.insert_generator_effect(pos, recurso.identifier())
            capa.set_opacity(opacidad, sp.textureset.ChannelType.BaseColor)
            log('Capa', prefijo, ': generador', nombre, 'añadido a su máscara; opacidad', opacidad)
        sp.project.execute_when_not_busy(lambda: (sp.project.save(), log('Proyecto guardado'), guardar_log()))
    except Exception as error:
        log('ERROR:', error)
        log(traceback.format_exc())
    guardar_log()


main()
