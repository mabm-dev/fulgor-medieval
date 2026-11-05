from pathlib import Path
import base64,re
root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v6.html').read_text(encoding='utf-8')
panel='data:image/png;base64,'+base64.b64encode((root/'panel_sin_seleccion.png').read_bytes()).decode()
html=re.sub(r'(--bar-image:url\(&quot;).*?(&quot;\))',lambda m:m.group(1)+panel+m.group(2),html,count=1)
old="let img=document.createElement('img');img.className='marker';img.src=imageData[types[c.building].img];img.alt='';b.appendChild(img)"
new="let marker=document.createElement('span');marker.className='marker';marker.innerHTML=icon(types[c.building].resource);marker.setAttribute('aria-label',types[c.building].name);b.appendChild(marker)"
assert old in html
html=html.replace(old,new)
html=html.replace("c.building!==undefined?'Esta casilla ya está ocupada.'", "c.building!==undefined?'Ocupada: '+types[c.building].name+'. Una construcción por casilla.'")
html=html.replace('al construir aparece una imagen animada sobre la casilla.', 'cada casilla admite una construcción y muestra un pequeño marcador.')
css='''<style>
.building.active{outline:2px solid #ffe39b!important;outline-offset:-3px;box-shadow:inset 0 0 6px #ffe4a088!important}
.hex.occupied .marker{display:grid!important;place-items:center!important;width:25px!important;height:25px!important;max-width:25px!important;max-height:25px!important;left:50%;top:50%;transform:translate(-50%,-50%)!important;border:1px solid #d8b46a;border-radius:50%;background:#2e241aee;mix-blend-mode:normal!important;filter:none!important;animation:none!important;z-index:6}
.hex.occupied .marker img{display:block!important;width:17px;height:17px;margin:0}
.hex.occupied::before,.hex.occupied::after{animation:none!important;opacity:.8}
</style>'''
html=html.replace('</head>',css+'</head>',1)
(root/'Maqueta_mapa_castilla_v7.html').write_text(html,encoding='utf-8')
print('Maqueta v7 creada')
