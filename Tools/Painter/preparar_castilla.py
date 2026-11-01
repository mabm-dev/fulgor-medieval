"""Ejecutar dentro de Substance 3D Painter (consola de Python de Painter, ver LEEME.md).

Crea el proyecto de la fortaleza de Castilla (Tripo + retoques de Blender) a partir de
Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3:

  * un Texture Set por material del FBX: el del atlas de Tripo (4096) y Pendon, Tapiz y Almenas (1024),
  * el color de Tripo como capa base del Texture Set principal,
  * horneado de los mapas AO, curvatura, grosor, posición y normales del mundo desde la propia malla,
    que son los que usan los generadores de desgaste y suciedad de Painter,
  * capas de acabado con máscara negra para pintar a mano (piedra, madera, tela, oro, humedad, polvo).

La creación del proyecto es asíncrona: el resto del trabajo se hace cuando Painter deja de estar ocupado.
No cambia la geometría, ni las UV, ni exporta nada sobre Unity (para eso, exportar_castilla_unity.py).
"""
from datetime import datetime
from pathlib import Path

import substance_painter as sp
import substance_painter.event as event

ROOT = Path(__file__).resolve().parents[2]    # raíz del repositorio (el script está en Tools/Painter)
ART = ROOT / 'Assets/Fulgor/Art/Buildings/FortalezaCastillaCastilla3'
OUTPUT = ROOT / 'ArtSource/Substance/FortalezaCastilla'
NOM = 'Fortaleza_Castilla_Castilla3'
FBX = ART / ('SM_%s.fbx' % NOM)
EXTRAS = {'Pendon': ART / ('T_%s_Pendon.png' % NOM), 'Tapiz': ART / ('T_%s_Tapiz.png' % NOM), 'Almenas': ART / ('T_%s_Almenas.png' % NOM)}
BASE = ART / ('T_%s_BaseColor.jpg' % NOM)
RES_PRINCIPAL, RES_EXTRA = 4096, 1024


def color(hex_value):
    """La paleta está en sRGB; Painter espera valores lineales."""
    v = [int(hex_value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in v]
    return sp.colormanagement.Color(*lin)


def gris(valor):
    return sp.colormanagement.Color(valor, valor, valor)


def capa(stack, nombre, canales):
    pos = sp.layerstack.InsertPosition.from_textureset_stack(stack)
    n = sp.layerstack.insert_fill(pos)
    n.set_name(nombre)
    n.active_channels = set(canales)
    return n


def configurar():
    """Se ejecuta cuando Painter ya terminó de crear el proyecto."""
    try:
        sets = {ts.name(): ts for ts in sp.textureset.all_texture_sets()}
        print('Texture Sets del FBX:', list(sets), '| edición:', sp.project.is_in_edition_state())
        if not sets:
            raise RuntimeError('Painter no creó ningún Texture Set. Cierra el proyecto (File > Close) y vuelve a ejecutarlo; '
                               'si se repite, importa el FBX a mano con File > New y avisa del resultado.')
        principal = next((ts for n, ts in sets.items() if n not in EXTRAS), None)
        if principal is None:
            raise RuntimeError('No encuentro el Texture Set principal entre: %s' % list(sets))
        Canal = sp.textureset.ChannelType

        # --- Texture Set principal: color de Tripo y capas de acabado ---------------------------------------
        def nombres(ts):
            return [n.get_name() for n in sp.layerstack.get_root_layer_nodes(ts.get_stack())]

        if any(n.startswith('00 Original') for n in nombres(principal)):
            print('El Texture Set principal ya tiene sus capas; no las repito.')
        else:
            principal.set_resolution(sp.textureset.Resolution(RES_PRINCIPAL, RES_PRINCIPAL))
            stack = principal.get_stack()
            recurso = sp.resource.import_project_resource(str(BASE), sp.resource.Usage.TEXTURE)
            original = capa(stack, '00 Original - color de Tripo', [Canal.BaseColor])
            original.set_source(Canal.BaseColor, recurso.identifier())

            # Máscaras negras: no cambian nada hasta que se pinta en blanco sobre ellas.
            acabados = [
                ('10 Piedra caliza - pintar mascara blanca sobre piedra', 'B29D79', .25, .88),
                ('20 Madera - pintar mascara blanca sobre madera', '57432E', .32, .8),
                ('30 Tela Castilla - pintar mascara blanca sobre telas', '922E2C', .65, .9),
                ('40 Oro - pintar mascara blanca sobre emblemas', 'D5AC4A', .4, .55),
                ('50 Humedad - pintar mascara blanca en zocalos', '4F5141', .16, .94),
                ('60 Polvo - pintar mascara blanca en zonas expuestas', 'B9AD91', .12, .95),
            ]
            for nombre, tinte, opacidad, rugosidad in acabados:
                c = capa(stack, nombre, [Canal.BaseColor, Canal.Roughness])
                c.set_source(Canal.BaseColor, color(tinte))
                c.set_source(Canal.Roughness, gris(rugosidad))
                c.set_opacity(opacidad, Canal.BaseColor)
                c.add_mask(sp.layerstack.MaskBackground.Black)
            sp.layerstack.set_selected_nodes([original])

        # --- Texture Sets extra: pendón, tapiz y sillares de los merlones -----------------------------------
        # Cada uno va por separado: si uno falla se informa y se sigue con los demás (el script se puede repetir).
        for nombre, ruta in EXTRAS.items():
            ts = sets.get(nombre)
            if ts is None:
                print('Aviso: el FBX no tiene el material', nombre)
                continue
            if any(n.startswith('00 Original') for n in nombres(ts)):
                print(nombre, ': ya tiene su capa.')
                continue
            try:
                if not ruta.is_file():
                    raise RuntimeError('No existe %s' % ruta)
                print(nombre, ': importando', ruta.name)
                r = sp.resource.import_project_resource(str(ruta), sp.resource.Usage.TEXTURE)
                c = capa(ts.get_stack(), '00 Original', [Canal.BaseColor])
                c.set_source(Canal.BaseColor, r.identifier())
                try:
                    ts.set_resolution(sp.textureset.Resolution(RES_EXTRA, RES_EXTRA))
                except Exception as error_res:
                    print(nombre, ': no se pudo bajar la resolución a %d (%s); se queda como está.' % (RES_EXTRA, error_res))
                print(nombre, ': OK')
            except Exception as error:
                import traceback
                print('ERROR en el Texture Set', nombre, ':', error)
                print(traceback.format_exc())

        OUTPUT.mkdir(parents=True, exist_ok=True)
        ruta_actual = sp.project.file_path()
        if ruta_actual:
            sp.project.save()
            proyecto = Path(ruta_actual)
        else:
            proyecto = OUTPUT / ('Fortaleza_Castilla_%s.spp' % datetime.now().strftime('%Y%m%d_%H%M%S'))
            sp.project.save_as(str(proyecto))
        print('Proyecto guardado:', proyecto)
        hornear(sets, principal, proyecto)
    except Exception as error:
        import traceback
        print('ERROR al configurar el proyecto:', error)
        print(traceback.format_exc())


def hornear(sets, principal, proyecto):
    """Horneado de mapas desde la malla, solo en el Texture Set principal."""
    try:
        Uso = sp.textureset.MeshMapUsage
        activos = [getattr(Uso, n) for n in ('AO', 'Curvature', 'Thickness', 'Position', 'WorldSpaceNormal') if hasattr(Uso, n)]
        for ts in sets.values():
            sp.baking.BakingParameters.from_texture_set(ts).set_textureset_enabled(ts.name() == principal.name())
        p = sp.baking.BakingParameters.from_texture_set(principal)
        p.set_enabled_bakers(activos)
        comunes = p.common()
        sp.baking.BakingParameters.set({comunes['OutputSize']: (12, 12)})   # 2^12 = 4096

        def al_terminar(_evento):
            event.DISPATCHER.disconnect(event.BakingProcessEnded, al_terminar)
            # Painter sigue ocupado un instante al terminar de hornear: se guarda cuando se libere
            sp.project.execute_when_not_busy(lambda: (sp.project.save(), print('Horneado terminado y proyecto guardado:', proyecto)))
            print('Siguiente paso: pintar las máscaras de las capas 10-60 y, si quieres desgaste automático,')
            print('añadir a una máscara un generador (por ejemplo "MG Mask Editor" o "MG Dirt") que use la curvatura y el AO horneados.')

        event.DISPATCHER.connect_strong(event.BakingProcessEnded, al_terminar)
        sp.baking.bake_async(principal)
        print('Horneando AO, curvatura, grosor, posición y normales del mundo… espera a que termine.')
    except Exception as error:      # el horneado se puede hacer a mano en Texture Set Settings > Bake Mesh Maps
        print('No se pudo lanzar el horneado automático:', error)
        print('Hazlo a mano: Texture Set Settings > Bake Mesh Maps, solo en el Texture Set principal.')


def main():
    faltan = [str(p) for p in [FBX, BASE] if not p.is_file()]
    if faltan:
        raise RuntimeError('Faltan archivos:\n' + '\n'.join(faltan))
    if sp.project.is_open():
        sets = sp.textureset.all_texture_sets()
        if sets:
            # Painter crea una "Capa 1" vacía en cada Texture Set de un proyecto nuevo; solo cuentan las capas de este script
            # el proyecto de un intento anterior sigue abierto: se continúa sobre él completando lo que falte
            print('Hay un proyecto abierto con Texture Sets; completo lo que falte.')
            configurar()
            return
        print('Cierro el proyecto vacío que dejó un intento anterior.')
        sp.project.close()
    ajustes = sp.project.Settings(normal_map_format=sp.project.NormalMapFormat.OpenGL,
                                  default_texture_resolution=RES_PRINCIPAL, import_cameras=False)
    sp.project.create(mesh_file_path=str(FBX), settings=ajustes)
    print('Proyecto creándose… el resto del script continúa cuando Painter termine.')
    sp.project.execute_when_not_busy(configurar)


main()
