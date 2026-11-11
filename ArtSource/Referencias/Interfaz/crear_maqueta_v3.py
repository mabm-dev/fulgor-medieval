from pathlib import Path
import base64, json

folder=Path(__file__).parent
html=(folder/'Maqueta_mapa_castilla_v2.html').read_text(encoding='utf-8')
files={
 'realm':r'C:\Users\tarif\AppData\Local\Temp\portapapeles-3b3fb781-78a2-48a6-afbd-0ea1b2b15ccb.png',
 'resources':r'C:\Users\tarif\AppData\Local\Temp\portapapeles-5f40a38e-7c8a-4ccd-a556-d6e16656cbd9.png',
 'legend':r'C:\Users\tarif\AppData\Local\Temp\portapapeles-3f4ae946-803f-4991-8e24-6c80c6fe2898.png',
 'bar':r'C:\Users\tarif\AppData\Local\Temp\portapapeles-a1722e59-7964-4ea2-801d-0cef1c8c5206.png'}
assets={k:'data:image/png;base64,'+base64.b64encode(Path(v).read_bytes()).decode() for k,v in files.items()}
style='''<style>
.realm,.resources{display:none!important}.reference-top{position:absolute;inset:0 0 auto;display:flex;justify-content:space-between;pointer-events:none;align-items:flex-start}.reference-top img{display:block;max-width:100%;height:auto}.realm-art{width:26%}.resource-art{width:42%}
.hex{width:5%;height:4.8%;background:#ffe2910a}.hex:hover,.hex:focus-visible,.hex.active{background:#ffe58040}.hex::after{inset:2%;clip-path:polygon(25% 0,75% 0,100% 50%,75% 100%,25% 100%,0 50%,25% 0,26% 3%,3% 50%,26% 97%,74% 97%,97% 50%,74% 3%,26% 3%);background:#f5dc94}.hex.occupied::before{content:'⚒';position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);font-size:24px;color:#ffe4a6;text-shadow:0 2px 3px #000}.hex .marker{display:none}
.bar{background:transparent!important;border:0!important;box-shadow:none!important;width:min(100%,1530px)!important;height:auto!important;aspect-ratio:1530/271;bottom:0!important;padding:0!important}.bar .list{position:absolute;left:15.05%;top:23.2%;width:69.9%;height:54%;display:flex;gap:.5%}.building{background:transparent!important;border:2px solid transparent!important;box-shadow:none!important}.building.active{border:2px solid #ffe4a2!important;box-shadow:inset 0 0 5px #ffe5a380!important}.building img,.building .icon{display:none!important}.bar:before{content:'';position:absolute;inset:0;background:var(--bar-image) center/100% 100% no-repeat}.label{bottom:19%;background:#211912c9;border-color:#9a804f}.legend{width:min(200px,23%);max-height:none;overflow:visible;top:18%;right:4%;padding:10px 12px 11px;border:0;background:var(--legend-image) center/100% 100% no-repeat;min-height:240px;box-shadow:none}.legend h2,.legend #cellname{display:none}.legend .row{position:relative;z-index:1;background:#c6aa7f;border:0;color:#3c2814;padding:5px 6px;min-height:32px;font-size:11px}.legend .row:first-of-type{margin-top:2px}.legend .result{position:relative;z-index:1;background:#35231a;color:#f1cd80;padding:8px 6px;margin-top:2px;font-size:17px}.legend p{margin:2px 0;background:#c7ae85;padding:3px;font-size:10px}.legend button{font-size:11px}.legend .toast{background:#c7ae85}.coords{top:13%}@media(max-width:700px){.realm-art{width:34%}.resource-art{width:48%}.legend{width:170px;right:1%;min-height:225px}.bar{bottom:0!important}.label{bottom:20%}}
</style>'''
html=html.replace('</head>',style+'</head>')
newtop='<div class="reference-top"><img class="realm-art" src="'+assets['realm']+'" alt="Castilla"><img class="resource-art" src="'+assets['resources']+'" alt="Recursos de ejemplo"></div>'
html=html.replace('<div class="coords">',newtop+'<div class="coords">',1)
html=html.replace('<main class="viewport">','<main class="viewport" style="--bar-image:url(&quot;'+assets['bar']+'&quot;);--legend-image:url(&quot;'+assets['legend']+'&quot;)">',1)
html=html.replace('const cells=[{x:47.5,y:44.7,kind:', 'const cells=[{x:47.5,y:46.2,kind:')
html=html.replace("{x:52.5,y:44.7,kind:","{x:52.5,y:46.2,kind:")
html=html.replace("{x:52.5,y:55,kind:","{x:52.5,y:53.8,kind:")
html=html.replace("{x:47.5,y:55,kind:","{x:47.5,y:53.8,kind:")
html=html.replace('Maqueta interactiva: el fondo', 'Maqueta interactiva: los marcos y las ilustraciones de construcción son la referencia facilitada; el fondo')
(folder/'Maqueta_mapa_castilla_v3.html').write_text(html,encoding='utf-8')
print('Versión 3 creada')
