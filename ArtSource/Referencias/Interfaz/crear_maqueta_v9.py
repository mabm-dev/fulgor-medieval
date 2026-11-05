from pathlib import Path

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v8.html').read_text(encoding='utf-8')
html=html.replace('const cells=[', 'let queueOrder=[];const cells=[',1)
old="marker.innerHTML=icon(types[c.building].resource);marker.setAttribute('aria-label',types[c.building].name);b.appendChild(marker)"
new="marker.innerHTML=icon(types[c.building].resource);const badge=document.createElement('span');badge.className='queue-badge';badge.textContent=String(queueOrder.indexOf(i)+1);marker.appendChild(badge);marker.setAttribute('aria-label',types[c.building].name+' · obra '+(queueOrder.indexOf(i)+1));b.appendChild(marker)"
assert old in html
html=html.replace(old,new,1)
html=html.replace('c.building=selected;drawCells();show(hover);', 'c.building=selected;queueOrder.push(hover);drawCells();show(hover);',1)
oldclick="b.onclick=()=>{pinned=i;show(i);const build=document.querySelector('#place');if(!build.disabled)build.click()};"
newclick=oldclick+"b.oncontextmenu=(event)=>{event.preventDefault();if(c.building===undefined)return;delete c.building;queueOrder=queueOrder.filter(index=>index!==i);pinned=i;drawCells();show(i);document.querySelector('#toast').textContent='Obra retirada de la cola.'};"
assert oldclick in html
html=html.replace(oldclick,newclick,1)
html=html.replace("cells.forEach((cell,i)=>{if(cell.building===undefined)return;count++;", "queueOrder.forEach((i)=>{const cell=cells[i];if(cell.building===undefined)return;count++;")
html=html.replace("name.textContent=types[cell.building].name+' · casilla '+(i+1);", "name.textContent=count+'. '+types[cell.building].name+' · casilla '+(i+1);")
css='''<style>
.hex.occupied .marker{width:53px!important;height:53px!important;max-width:53px!important;max-height:53px!important}.hex.occupied .marker img{width:36px!important;height:36px!important}.queue-badge{position:absolute;right:-8px;top:-8px;width:23px;height:23px;display:grid;place-items:center;background:#5a291f;color:#fff2c6;border:2px solid #e2bb68;border-radius:50%;font:bold 12px Georgia,serif;box-shadow:0 2px 3px #0008}.legend #detail{font-weight:bold}.legend #detail.valid{color:#285d25}.legend #detail.invalid{color:#a1231d}.legend #detail.occupied{color:#8d6100}@media(max-width:700px){.hex.occupied .marker{width:45px!important;height:45px!important;max-width:45px!important;max-height:45px!important}.hex.occupied .marker img{width:30px!important;height:30px!important}}
</style>'''
html=html.replace('</head>',css+'</head>',1)
logic='''<script>
const showWithBenefits=show;show=function(i){showWithBenefits(i);const cell=cells[i],building=types[selected],detail=document.querySelector('#detail');detail.className=cell.building!==undefined?'occupied':building.terrain.includes(cell.kind)?'valid':'invalid';if(cell.building!==undefined)detail.textContent='Ocupada: '+types[cell.building].name+' · clic derecho para retirar.'};
</script>'''
html=html.replace('</body>',logic+'</body>',1)
html=html.replace('cada casilla admite una construcción; sus obras y turnos aparecen en la cola simulada.', 'clic izquierdo construye; clic derecho retira la obra. Cada casilla admite una construcción y los turnos son simulados.')
(root/'Maqueta_mapa_castilla_v9.html').write_text(html,encoding='utf-8')
print('Maqueta v9 creada')
