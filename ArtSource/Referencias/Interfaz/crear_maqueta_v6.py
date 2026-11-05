from pathlib import Path

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v5.html').read_text(encoding='utf-8')
html=html.replace("{x:52.5,y:46.2,kind:'llanura'}", "{x:52.5,y:46.2,kind:'bosque'}")
html=html.replace("{x:54.8,y:49.8,kind:'llanura'}", "{x:54.8,y:49.8,kind:'llanura',river:true}")
html=html.replace('let selected=1,hover=null;', 'let selected=1,hover=null,pinned=null;')
html=html.replace("selected=i;document.querySelector('#label')", "selected=i;pinned=null;legend.classList.remove('visible');document.querySelector('#label')")
html=html.replace("b.onmouseenter=()=>show(i);b.onfocus=()=>show(i);b.onclick=()=>show(i);", "b.onmouseenter=()=>{if(pinned===null)show(i)};b.onfocus=()=>{if(pinned===null)show(i)};b.onclick=()=>{pinned=i;show(i)};")
old="document.querySelector('#bonus').textContent='—';"
new="const bonus=(c.river&&t.resource==='grano')?1:0;document.querySelector('#bonus').innerHTML=bonus?icon('grano')+' +1':'—';"
html=html.replace(old,new)
html=html.replace("(1+t.amount):icon(passive)+' +1　'+icon(t.resource)+' +'+t.amount)","(1+t.amount+bonus):icon(passive)+' +1　'+icon(t.resource)+' +'+(t.amount+bonus))")
html=html.replace("const cell=cells[i],type=types[selected],base=", "const cell=cells[i],type=types[selected],extra=(cell.river&&type.resource==='grano')?1:0,base=")
html=html.replace("(1+type.amount)+' <small>por turno", "(1+type.amount+extra)+' <small>por turno")
html=html.replace("+type.amount+' <small>por turno", "+(type.amount+extra)+' <small>por turno")
html=html.replace("'Casilla '+(i+1)+' · '+c.kind", "'Casilla '+(i+1)+' · '+c.kind+(c.river?' · río':'')")
html=html.replace("if(hover===null)return;const c=cells[hover]", "if(hover===null)return;const c=cells[hover]")
html=html.replace('Los puntos de selección, los edificios superpuestos y los rendimientos son simulados', 'Los terrenos, el río, los puntos de selección y los rendimientos son ejemplos simulados')
extra='''<style>.hex.occupied .marker{display:block!important;z-index:5!important;opacity:1!important;width:180%!important;height:auto!important;filter:drop-shadow(0 3px 3px #111)!important;mix-blend-mode:normal!important;animation:construir .65s ease-out both}.hex.occupied{z-index:3}.legend{z-index:12}.legend button:not(:disabled){cursor:pointer}.building{cursor:pointer}.viewport{background-size:cover}.terrain>img{image-rendering:auto}</style>'''
html=html.replace('</head>',extra+'</head>',1)
(root/'Maqueta_mapa_castilla_v6.html').write_text(html,encoding='utf-8')
print('Maqueta v6 creada')
