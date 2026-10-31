"""Run inside Substance 3D Painter using runpy.run_path(..., run_name='__main__').

Creates a new project from the original fortress FBX, restores the original
base color and normal, and adds masked finish layers for manual painting.
Does not change geometry or export over Unity textures.
"""
from pathlib import Path
from datetime import datetime
import substance_painter as sp

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/Fulgor/Art/RawFortress'
OUTPUT = ROOT / 'ArtSource/Substance/Fortaleza'
RESOLUTION = 2048


def color(hex_value):
    """Palette values are sRGB; convert to linear uniform color values."""
    values = [int(hex_value[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    linear = [v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in values]
    return sp.colormanagement.Color(*linear)


def grey(value):
    return sp.colormanagement.Color(value, value, value)


def fill(stack, name, channels):
    position = sp.layerstack.InsertPosition.from_textureset_stack(stack)
    layer = sp.layerstack.insert_fill(position)
    layer.set_name(name)
    layer.active_channels = set(channels)
    return layer


def main():
    required = [ART / 'SM_Fortaleza_completa_bruta.fbx',
                ART / 'T_Fortaleza_BaseColor.png', ART / 'T_Fortaleza_Normal.png']
    missing = [str(p) for p in required if not p.is_file()]
    if missing:
        raise RuntimeError('Faltan archivos de entrada:\n' + '\n'.join(missing))
    if not hasattr(sp, 'layerstack') or not hasattr(sp.layerstack, 'insert_fill'):
        raise RuntimeError('Esta versión de Painter no expone la API layerstack requerida. Consulta Ayuda > Documentación de scripting.')
    if sp.project.is_open():
        raise RuntimeError('Guarda y cierra el proyecto abierto antes de ejecutar este script.')
    OUTPUT.mkdir(parents=True, exist_ok=True)
    project_path = OUTPUT / ('Fortaleza_color_' + datetime.now().strftime('%Y%m%d_%H%M%S_%f') + '.spp')
    settings = sp.project.Settings(
        normal_map_format=sp.project.NormalMapFormat.OpenGL,
        default_texture_resolution=RESOLUTION,
        import_cameras=False)
    sp.project.create(mesh_file_path=str(required[0]), settings=settings)
    base_resource = sp.resource.import_project_resource(str(required[1]), sp.resource.Usage.TEXTURE)
    normal_resource = sp.resource.import_project_resource(str(required[2]), sp.resource.Usage.TEXTURE)
    channel = sp.textureset.ChannelType
    stack = sp.textureset.get_active_stack()
    original = fill(stack, '00 Original - color y normal', [channel.BaseColor, channel.Normal])
    original.set_source(channel.BaseColor, base_resource.identifier())
    original.set_source(channel.Normal, normal_resource.identifier())

    # Black masks keep all new finishes off until the user paints their areas.
    # Color opacity preserves detail from the original color texture.
    presets = [
        ('10 Piedra caliza - pintar mascara blanca sobre piedra', 'B29D79', .25, .88),
        ('20 Madera - pintar mascara blanca sobre madera', '57432E', .32, .8),
        ('30 Tela Castilla - pintar mascara blanca sobre banderas', '922E2C', .65, .9),
        ('40 Oro - pintar mascara blanca sobre emblemas', 'D5AC4A', .4, .55),
        ('50 Humedad - pintar mascara blanca en zocalos', '4F5141', .16, .94),
        ('60 Polvo - pintar mascara blanca en zonas expuestas', 'B9AD91', .12, .95),
    ]
    for name, tint, opacity, roughness in presets:
        layer = fill(stack, name, [channel.BaseColor, channel.Roughness])
        layer.set_source(channel.BaseColor, color(tint))
        layer.set_source(channel.Roughness, grey(roughness))
        layer.set_opacity(opacity, channel.BaseColor)
        layer.add_mask(sp.layerstack.MaskBackground.Black)
    sp.layerstack.set_selected_nodes([original])
    sp.project.save_as(str(project_path))
    print('Proyecto preparado:', project_path)
    print('Las capas nuevas tienen máscara negra. Pinta en blanco sobre cada máscara para revelar el acabado.')
    print('La rugosidad de cada capa es una propuesta; no se ha importado el mapa packed de Unity como roughness.')


if __name__ == '__main__':
    main()
