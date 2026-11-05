from pathlib import Path

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v9.html').read_text(encoding='utf-8')
css='''<style>
/* La ilustración del panel no debe capturar clics fuera de los botones. */
.bar{pointer-events:none!important}.bar:before{pointer-events:none!important}.bar .list{pointer-events:none!important}.bar .building{pointer-events:auto!important}
/* Se eleva la vista para que el anillo inferior quede sobre el panel. */
.terrain{top:37%!important}
/* El área activa cubre más terreno sin hacer más grandes los indicadores. */
.hex{width:4.7%!important;height:4.3%!important;background:transparent!important}
.hex::after{width:25px!important;height:19px!important}
.hex::before{width:43px!important;height:32px!important}
.hex:hover,.hex:focus-visible,.hex.active{background:transparent!important}
.hex.occupied{background:transparent!important}
@media(max-width:700px){.terrain{top:37%!important}.hex{width:4.5%!important;height:4.2%!important}}
</style>'''
html=html.replace('</head>',css+'</head>',1)
(root/'Maqueta_mapa_castilla_v10.html').write_text(html,encoding='utf-8')
print('Maqueta v10 creada')
