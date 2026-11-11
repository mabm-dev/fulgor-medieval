from pathlib import Path

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v4.html').read_text(encoding='utf-8')
# El primer edificio tiene índice 0: también debe considerarse construido.
html=html.replace("(c.building?' occupied':'')", "(c.building!==undefined?' occupied':'')")
css='''<style>
.viewport{width:100%;height:auto;aspect-ratio:16/10;min-height:0}
.bar{width:100%!important;height:auto!important;aspect-ratio:3 / 1!important;bottom:0!important;overflow:visible!important}
.bar:before{background-size:100% 100%!important;background-position:center!important}
.bar .list{left:10.3%!important;top:34%!important;width:79.2%!important;height:34%!important;gap:.5%!important}
.building,.building.active,.building:hover,.building:focus{border:0!important;outline:0!important;box-shadow:none!important;background:transparent!important}
.building:focus-visible{outline:2px solid #fff3c5!important;outline-offset:2px}
.label{bottom:22%!important;max-width:80%;white-space:normal;text-align:center}
.hex.occupied .marker{display:block!important;position:absolute;left:50%;top:50%;width:135%;height:auto;max-width:none;max-height:none;transform:translate(-50%,-75%);mix-blend-mode:multiply;filter:drop-shadow(0 2px 2px #242012);animation:construir .65s ease-out both;pointer-events:none}
@keyframes construir{0%{opacity:0;transform:translate(-50%,-105%) scale(.35)}70%{opacity:1;transform:translate(-50%,-72%) scale(1.08)}100%{opacity:1;transform:translate(-50%,-75%) scale(1)}}
.legend{z-index:3}.bar{z-index:4}.reference-top{z-index:5}.label{z-index:3}
@media(max-width:700px){.viewport{aspect-ratio:auto;min-height:600px}.bar{aspect-ratio:3/1!important;height:auto!important}.label{bottom:25%!important}.legend{top:17%;max-width:42vw}.terrain{width:400%!important}}
@media(max-width:420px){.viewport{min-height:630px}.legend{width:155px!important;font-size:11px}.legend .row{font-size:10px}.label{font-size:10px;bottom:22%!important}.realm-art{width:40%!important}.resource-art{width:55%!important}}
@media(prefers-reduced-motion:reduce){.hex.occupied .marker{animation:none}}
</style>'''
html=html.replace('</head>',css+'</head>',1)
html=html.replace('Obra colocada en la maqueta.', 'Edificio colocado en esta casilla de prueba.')
html=html.replace('Maqueta interactiva: los marcos', 'Maqueta interactiva: al construir aparece una imagen animada sobre la casilla. Los marcos')
(root/'Maqueta_mapa_castilla_v5.html').write_text(html,encoding='utf-8')
print('Maqueta v5 creada')
