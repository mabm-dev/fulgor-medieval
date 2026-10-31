from pathlib import Path
import substance_painter as sp
ROOT=Path(__file__).resolve().parents[2]
assert sp.project.is_open() and sp.project.is_in_edition_state()
for ts in sp.textureset.all_texture_sets():ts.set_resolution(sp.textureset.Resolution(4096,4096))
base=next(n for n in sp.layerstack.get_root_layer_nodes(sp.textureset.get_active_stack()) if n.get_name()=='FULGOR 00 - Texturas originales')
base.active_channels={sp.textureset.ChannelType.BaseColor,sp.textureset.ChannelType.Normal,sp.textureset.ChannelType.Roughness,sp.textureset.ChannelType.Metallic}
for path,channel in [(ROOT/'Assets/Fulgor/Art/RawFortress/T_Fortaleza_BaseColor.png',sp.textureset.ChannelType.BaseColor),(ROOT/'Assets/Fulgor/Art/RawFortress/T_Fortaleza_Normal.png',sp.textureset.ChannelType.Normal),(ROOT/'ArtSource/Substance/Fortaleza/PBR_Original/Roughness.png',sp.textureset.ChannelType.Roughness),(ROOT/'ArtSource/Substance/Fortaleza/PBR_Original/Metallic.png',sp.textureset.ChannelType.Metallic)]:
 r=sp.resource.import_project_resource(str(path),sp.resource.Usage.TEXTURE)
 base.set_source(channel,r.identifier())
sp.project.save()
preset=next(p for p in sp.export.list_resource_export_presets() if p.resource_id.name=='Unity Universal Render Pipeline (Metallic Standard)')
out=ROOT/'Assets/Fulgor/Art/RawFortress/Painter4K';out.mkdir(exist_ok=True)
config={'exportShaderParams':False,'exportPath':str(out),'defaultExportPreset':preset.resource_id.url(),'exportList':[{'rootPath':ts.name()} for ts in sp.textureset.all_texture_sets()],'exportParameters':[{'parameters':{'fileFormat':'png','bitDepth':'8','sizeLog2':12,'paddingAlgorithm':'infinite'}}]}
result=sp.export.export_project_textures(config)
if result.status!=sp.export.ExportStatus.Success:raise RuntimeError(result.message)
print('EXPORTADO',result.textures)
