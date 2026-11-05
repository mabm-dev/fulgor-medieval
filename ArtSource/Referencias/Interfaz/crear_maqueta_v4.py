from pathlib import Path
import base64,re

root=Path(__file__).parent
html=(root/'Maqueta_mapa_castilla_v3.html').read_text(encoding='utf-8')
def data(name):
 return 'data:image/png;base64,'+base64.b64encode((root/('limpio_'+name+'.png')).read_bytes()).decode()
for css,name in [('realm-art','reino'),('resource-art','recursos')]:
 html=re.sub(r'(<img class="'+css+r'" src=")[^"]+(" alt=)',lambda m:m.group(1)+data(name)+m.group(2),html,count=1)
for var,name in [('bar-image','panel'),('legend-image','leyenda')]:
 html=re.sub(r'(--'+var+r':url\(&quot;).*?(&quot;\))',lambda m:m.group(1)+data(name)+m.group(2),html,count=1)
css='''<style>
.reference-top img{object-fit:contain}.realm-art{width:27%!important}.resource-art{width:42%!important;margin-top:-4.3%!important}.bar{width:min(100%,1530px)!important;height:160px!important;aspect-ratio:auto!important;bottom:0!important}.bar:before{background-size:100% 250%!important;background-position:center!important}.bar .list{left:10.2%!important;top:14%!important;width:79.2%!important;height:72%!important;gap:.7%!important}.label{bottom:164px!important}.legend{background:none!important;background-color:#c4aa81!important;border:3px ridge #846637!important;border-radius:5px;box-shadow:0 5px 12px #0009;padding:10px!important;width:208px!important;max-height:none!important;overflow:visible!important}.legend h2{display:block!important;color:#362414;font-size:15px;margin:0 0 6px}.legend #cellname{display:block!important}.legend .row{background:transparent!important;border-bottom:1px solid #a58960!important}.legend .result{display:grid!important;gap:3px;background:#39271d!important;padding:7px!important;margin:5px -2px;color:#f4d493!important;line-height:1.2}.legend .result .result-line{white-space:nowrap;display:flex;align-items:center;gap:4px}.legend p,.legend .toast{background:none!important}.legend button:disabled{opacity:.4}.hex{background:transparent!important;clip-path:none!important;width:3.3%!important;height:3.3%!important;overflow:visible!important}.hex::after,.hex::before{content:''!important;position:absolute;left:50%;top:50%;border:1px solid #ffe2a1;border-radius:50%;background:transparent!important;transform:translate(-50%,-50%) scale(.7);animation:pulso-anillo 2.3s ease-in-out infinite;pointer-events:none}.hex::after{width:35%;height:48%;inset:auto!important;left:50%!important;top:50%!important}.hex::before{width:58%;height:77%;animation-delay:.2s;opacity:.75}.hex:hover,.hex:focus-visible,.hex.active{filter:brightness(1.3);background:#ffe5a111!important;border-radius:50%}.hex.occupied::before{content:''!important}.hex.occupied{background:#f0c77944!important;border-radius:50%}@keyframes pulso-anillo{0%,100%{transform:translate(-50%,-50%) scale(.72);opacity:.45}50%{transform:translate(-50%,-50%) scale(1.15);opacity:.95}}@media(prefers-reduced-motion:reduce){.hex::before,.hex::after{animation:none}}@media(max-width:700px){.bar{height:110px!important}.label{bottom:115px!important}.legend{width:180px!important}.resource-art{margin-top:-5%!important}}
</style>'''
html=html.replace('</head>',css+'</head>',1)
logic='''<script>
const showPrevious=show;show=function(i){showPrevious(i);const cell=cells[i],type=types[selected],base=cell.kind==='colina'?'piedra':cell.kind==='bosque'?'madera':'grano';const total=document.querySelector('#total');if(base===type.resource){total.innerHTML='<span class="result-line">'+icon(base)+' +'+(1+type.amount)+' <small>por turno</small></span>'}else{total.innerHTML='<span class="result-line">'+icon(base)+' +1 <small>por turno</small></span><span class="result-line">'+icon(type.resource)+' +'+type.amount+' <small>por turno</small></span>'}};
</script>'''
html=html.replace('</body>',logic+'</body>',1)
(root/'Maqueta_mapa_castilla_v4.html').write_text(html,encoding='utf-8')
print('Maqueta v4 creada')
