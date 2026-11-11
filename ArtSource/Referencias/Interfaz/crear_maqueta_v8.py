from pathlib import Path

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v7.html').read_text(encoding='utf-8')
html=html.replace('b.onclick=()=>{pinned=i;show(i)};', "b.onclick=()=>{pinned=i;show(i);const build=document.querySelector('#place');if(!build.disabled)build.click()};")
html=html.replace('Pulsa para colocar el edificio de prueba.', 'Clic izquierdo para añadir a la cola.')
html=html.replace('Edificio colocado en esta casilla de prueba.', 'Obra añadida a la cola de prueba.')
html=html.replace('<aside class="legend"', '<aside class="queue" aria-label="Cola de construcción"><div class="queue-title">OBRAS DE LA CIUDAD</div><div id="queue-list"></div></aside><aside class="legend"',1)
style='''<style>
.hex.occupied .marker{width:38px!important;height:38px!important;max-width:38px!important;max-height:38px!important}.hex.occupied .marker img{width:26px!important;height:26px!important}
.legend #place{display:none}.queue{position:absolute;left:1.5%;top:18%;width:200px;z-index:3;background:linear-gradient(150deg,#3e2b1d,#211810);border:3px double #a48750;color:#eddbb2;padding:9px 10px;box-shadow:0 4px 12px #0009;font-size:12px}.queue-title{font-size:11px;color:#ddc188;letter-spacing:.5px;padding-bottom:7px;border-bottom:1px solid #816a42}.queue-item{padding:7px 0;border-bottom:1px solid #675337;display:flex;align-items:center;justify-content:space-between;gap:6px}.queue-turns{white-space:nowrap;color:#e8ca81}.queue-empty{color:#c5b492;line-height:1.4;padding:8px 0}.queue-item span:first-child{min-width:0;overflow-wrap:anywhere}@media(max-width:700px){.queue{width:145px;font-size:10px;top:18%}.queue-title{font-size:9px}.legend{width:155px!important;right:1%;font-size:10px}.hex.occupied .marker{width:32px!important;height:32px!important}.hex.occupied .marker img{width:22px!important;height:22px!important}}@media(max-width:420px){.queue{width:115px}.legend{width:135px!important}}
</style>'''
html=html.replace('</head>',style+'</head>',1)
logic='''<script>
const previousDrawCells=drawCells;
drawCells=function(){previousDrawCells();const list=document.querySelector('#queue-list');list.innerHTML='';let count=0;cells.forEach((cell,i)=>{if(cell.building===undefined)return;count++;const line=document.createElement('div');line.className='queue-item';const name=document.createElement('span');name.textContent=types[cell.building].name+' · casilla '+(i+1);const turns=document.createElement('span');turns.className='queue-turns';turns.textContent='⏳ 2 turnos';line.appendChild(name);line.appendChild(turns);list.appendChild(line)});if(!count){const empty=document.createElement('div');empty.className='queue-empty';empty.textContent='Sin obras pendientes';list.appendChild(empty)}};drawCells();
</script>'''
html=html.replace('</body>',logic+'</body>',1)
html=html.replace('cada casilla admite una construcción y muestra un pequeño marcador.', 'cada casilla admite una construcción; sus obras y turnos aparecen en la cola simulada.')
(root/'Maqueta_mapa_castilla_v8.html').write_text(html,encoding='utf-8')
print('Maqueta v8 creada')
