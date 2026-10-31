"""Apply an initial editable color pass to the already opened fortress project."""
from pathlib import Path
from datetime import datetime
import runpy
import substance_painter as sp

ROOT=Path(__file__).resolve().parents[2]
helpers=runpy.run_path(str(ROOT/'Tools/Painter/preparar_fortaleza.py'),run_name='fulgor_helpers')
fill=helpers['fill'];color=helpers['color'];grey=helpers['grey']
assert sp.project.is_open() and sp.project.is_in_edition_state()
channel=sp.textureset.ChannelType
stack=sp.textureset.get_active_stack()
if any(node.get_name().startswith('FULGOR 10 -') for node in sp.layerstack.get_root_layer_nodes(stack)):
 raise RuntimeError('Esta pasada ya está aplicada. Ajusta las capas existentes para evitar duplicarlas.')
resources={}
for name,file in [('base','T_Fortaleza_BaseColor.png'),('normal','T_Fortaleza_Normal.png')]:
 resources[name]=sp.resource.import_project_resource(str(ROOT/'Assets/Fulgor/Art/RawFortress'/file),sp.resource.Usage.TEXTURE)
original=fill(stack,'FULGOR 00 - Texturas originales',[channel.BaseColor,channel.Normal])
original.set_source(channel.BaseColor,resources['base'].identifier());original.set_source(channel.Normal,resources['normal'].identifier())
warm=fill(stack,'FULGOR 10 - Armonia calida suave',[channel.BaseColor])
warm.set_source(channel.BaseColor,color('BBA584'));warm.set_opacity(.06,channel.BaseColor)
flag=fill(stack,'FULGOR 20 - Rojo Castilla, mascara por color original',[channel.BaseColor])
flag.set_source(channel.BaseColor,color('922E2C'));flag.set_opacity(.3,channel.BaseColor);flag.add_mask(sp.layerstack.MaskBackground.Black)
mask=sp.resource.import_project_resource(str(ROOT/'ArtSource/Substance/Fortaleza/Masks/T_Banderas_Mask.png'),sp.resource.Usage.TEXTURE)
effect=sp.layerstack.insert_fill(sp.layerstack.InsertPosition.inside_node(flag,sp.layerstack.NodeStack.Mask))
effect.set_name('Rojos de la textura original');effect.set_source(None,mask.identifier())
for name,tint,roughness in [('Piedra','B29D79',.88),('Madera','57432E',.8),('Humedad','4F5141',.94),('Polvo','B9AD91',.95)]:
 layer=fill(stack,'FULGOR - '+name+' (mascara para ajustar)',[channel.BaseColor,channel.Roughness])
 layer.set_source(channel.BaseColor,color(tint));layer.set_source(channel.Roughness,grey(roughness));layer.set_opacity(.2,channel.BaseColor);layer.add_mask(sp.layerstack.MaskBackground.Black)
sp.layerstack.set_selected_nodes([flag])
path=ROOT/'ArtSource/Substance/Fortaleza'/('Fortaleza_Castilla_Color_'+datetime.now().strftime('%Y%m%d_%H%M%S')+'.spp')
sp.project.save_as(str(path))
print('Guardado',path)
