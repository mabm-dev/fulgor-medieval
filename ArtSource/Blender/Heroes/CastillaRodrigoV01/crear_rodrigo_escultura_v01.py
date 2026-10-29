"""Rodrigo: editable geometric costume/anatomy study, NOT a game-ready hero.

Run with Blender 5.2 --background --factory-startup --python-exit-code 1 --python FILE.
All visible character detail is geometry/procedural material, no image planes.
Portrait identity and original costume remain the comparison target, not a claim
of portrait-exact likeness. Lower body/back follow the inferred concept sheet.
"""
from pathlib import Path
import bpy, math, random, json
from mathutils import Vector
from math import sin, cos, pi, exp

random.seed(1207)
SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[3]
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV01'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.name='Rodrigo_Estudio_geometrico_V01'
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
hero=bpy.data.collections.new('RODRIGO_GEOMETRIA_REAL');scene.collection.children.link(hero)
stage=bpy.data.collections.new('ESTUDIO_LUCES_CAMARAS');scene.collection.children.link(stage)

def move(obj,col=hero):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    col.objects.link(obj)
    return obj

def mat(name,color,rough=.65,metal=0,noise=0,bump=.001):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    if noise:
        tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=noise
        tex.inputs['Detail'].default_value=3.3;tex.inputs['Roughness'].default_value=.73
        coords=n.new('ShaderNodeTexCoord');l.new(coords.outputs['Generated'],tex.inputs['Vector'])
        ramp=n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position=.17;ramp.color_ramp.elements[0].color=(*(v*.48 for v in color),1)
        ramp.color_ramp.elements[1].position=.83;ramp.color_ramp.elements[1].color=(*(min(1,v*1.25) for v in color),1)
        l.new(tex.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs['Color'],p.inputs['Base Color'])
        b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=.28;b.inputs['Distance'].default_value=bump
        l.new(tex.outputs['Fac'],b.inputs['Height']);l.new(b.outputs[0],p.inputs['Normal'])
    return m

skin=mat('Piel_oliva_calida',(.34,.165,.085),.58,noise=220,bump=.00012)
skin.node_tree.nodes.get('Principled BSDF').inputs['Subsurface Weight'].default_value=.10
lip=mat('Labios_sutiles',(.27,.105,.065),.58)
hairm=[mat('Cabello_castano_'+str(i),c,.76,noise=170,bump=.0003) for i,c in enumerate([(.014,.008,.005),(.025,.013,.007),(.039,.019,.010),(.055,.026,.013)])]
leather=mat('Cuero_curtido_oscuro',(.075,.034,.016),.83,noise=125,bump=.0008)
edge=mat('Cuero_cantos_gastados',(.18,.094,.041),.72,noise=90,bump=.0008)
cloth=mat('Lana_granate',(.20,.018,.025),.92,noise=190,bump=.0008)
cloth.node_tree.nodes.get('Principled BSDF').inputs['Sheen Weight'].default_value=.32
trim=mat('Bordado_ocre_apagado',(.29,.14,.047),.72,noise=75,bump=.0003)
steel=mat('Acero_ennegrecido',(.19,.21,.22),.37,.82,noise=95,bump=.0002)
bronze=mat('Laton_envejecido',(.36,.22,.071),.35,.77,noise=42,bump=.00025)
trousers=mat('Pano_marron',(.055,.044,.031),.95,noise=140,bump=.0009)
dark=mat('Hendiduras',(.018,.010,.007),.82)
eyeWhite=mat('Esclerotica_marfil',(.29,.24,.19),.34)
iris=mat('Iris_avellana',(.065,.034,.012),.25)

def mesh(name,v,f,material,sub=0):
    me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update()
    ob=bpy.data.objects.new(name,me);hero.objects.link(ob);ob.data.materials.append(material)
    for poly in me.polygons:poly.use_smooth=True
    if sub:
        mod=ob.modifiers.new('Subdivision_forma','SUBSURF');mod.levels=sub;mod.render_levels=sub
    return ob

def ell(name,p,s,m,seg=32,rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=p)
    o=move(bpy.context.object);o.name=name;o.scale=s;o.data.materials.append(m)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for poly in o.data.polygons:poly.use_smooth=True
    return o

def line(name,pts,r,m):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=2
    cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
    for p,co in zip(sp.points,pts):p.co=(*co,1)
    ob=bpy.data.objects.new(name,cu);hero.objects.link(ob);ob.data.materials.append(m)
    return ob

def loft(name,rings,m,n=48,fold=0,sub=1):
    # rings: x,y,z, radius_x,radius_y
    v=[];f=[]
    for j,(x,y,z,rx,ry) in enumerate(rings):
        for i in range(n):
            a=2*pi*i/n;q=1+fold*(sin(a*9+j*.6)+.35*sin(a*17-j))
            v.append((x+rx*sin(a)*q,y-ry*cos(a)*q,z))
    for j in range(len(rings)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;f.append((a,b,b+n,a+n))
    f.append(tuple(reversed(range(n))));f.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name,v,f,m,sub)

def tube(name,points,radii,m,n=16):
    v=[];f=[]
    for j,(co,r) in enumerate(zip(points,radii)):
        p=Vector(co);t=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        t.normalize();ref=Vector((0,1,0));u=t.cross(ref).normalized();w=t.cross(u).normalized()
        if isinstance(r,(float,int)):rx=ry=r
        else:rx,ry=r
        for k in range(n):a=2*pi*k/n;v.append(p+u*rx*cos(a)+w*ry*sin(a))
    for j in range(len(points)-1):
        for k in range(n):a=j*n+k;b=j*n+(k+1)%n;f.append((a,b,b+n,a+n))
    f.extend([tuple(reversed(range(n))),tuple((len(points)-1)*n+k for k in range(n))])
    return mesh(name,v,f,m,1)

# Feet and relaxed lower body. Source authored at human height; no runtime scale.
for s in (-1,1):
    x=s*.092
    ell('Bota_puntera', (x,-.060,.047),(.061,.146,.047),leather)
    sole=ell('Suela_cosida',(x,-.049,.019),(.063,.151,.019),dark)
    line('Costura_suela',[(x+.061*sin(a),-.049-.143*cos(a),.031) for a in [2*pi*i/96 for i in range(97)]],.0012,edge)
    loft('Bota_cana',[(x,0,.065,.046,.061),(x,.015,.105,.045,.047),(x,.02,.16,.043,.043),(x,.014,.22,.047,.045),(x,.006,.29,.048,.045),(x,0,.365,.055,.051),(x,0,.39,.057,.051)],leather,fold=.044)
    loft('Bota_vuelta_superior',[(x,0,.361,.058,.053),(x,0,.371,.06,.054),(x,0,.396,.060,.054),(x,0,.401,.057,.052)],edge,sub=1)
    loft('Pantalon',[(x,0,.385,.048,.045),(x,0,.43,.057,.054),(x,.008,.47,.068,.064),(x,.012,.52,.063,.068),(x,.005,.60,.077,.076),(x*.90,0,.70,.084,.083),(x*.84,0,.80,.091,.092),(x*.75,0,.90,.094,.10)],trousers,fold=.033)
    for z in (.13,.185,.245):
        line('Arruga_bota',[(x+.046*sin(a),.014-.046*cos(a),z+.006*sin(3*a)) for a in [pi*(i/32-.5) for i in range(33)]],.0022,edge)

loft('Cota_faldon_base',[(0,0,.80,.174,.12),(0,0,.87,.178,.125),(0,0,.98,.158,.11),(0,0,1.08,.15,.105)],steel,fold=.015)
loft('Cuero_torax_base',[(0,0,.94,.15,.105),(0,0,1.02,.149,.106),(0,0,1.12,.157,.113),(0,0,1.23,.179,.12),(0,0,1.34,.203,.119),(0,.005,1.425,.197,.102),(0,.007,1.46,.137,.083),(0,.006,1.485,.083,.065)],leather,sub=2)
loft('Cuello_anatomico',[(0,.004,1.43,.079,.064),(0,.003,1.48,.059,.057),(0,.007,1.53,.048,.049),(0,.010,1.58,.049,.050)],skin,sub=2)
loft('Gorguera_cota',[(0,.004,1.425,.12,.085),(0,.004,1.47,.085,.070),(0,.004,1.511,.055,.055)],steel,sub=2)

# Separate overlapping leather lames, rows of rivets, stitching and skirt panels.
for j in range(6):
    z=.995+j*.067;rx=.153+max(0,z-1.10)*.22;ry=.11+max(0,z-1.1)*.035
    loft('Lamina_cuero_torso_%02d'%j,[(0,0,z,.997*rx,ry),(0,0,z+.006,rx+.004,ry+.004),(0,0,z+.059,rx+.011,ry+.007),(0,0,z+.064,rx+.008,ry+.004)],leather,sub=1)
    for a in (-1.05,-.52,0,.52,1.05):
        ell('Remache_torso',((rx+.011)*sin(a),-(ry+.009)*cos(a),z+.050),(.0027,.0019,.0027),bronze,12,8)
    line('Costura_lamina',[( (rx+.010)*sin(a),-(ry+.008)*cos(a),z+.012) for a in [-1.24+i*2.48/50 for i in range(51)]],.0008,edge)
for k in range(8):
    a0=2*pi*k/8+.025;a1=2*pi*(k+1)/8-.025
    v=[];f=[]
    for j in range(9):
        z=.84+j*.019;rx=.185-j*.003;ry=.137-j*.0023
        for i in range(10):
            a=a0+(a1-a0)*i/9;v.append((rx*sin(a),-ry*cos(a),z+.004*sin(i*pi/9)))
    for j in range(8):
        for i in range(9):q=j*10+i;f.append((q,q+1,q+11,q+10))
    o=mesh('Faldon_cuero_segmentado',v,f,leather,1);mod=o.modifiers.new('Espesor_cuero','SOLIDIFY');mod.thickness=.003
    for i in range(1,5):
        a=a0+(a1-a0)*i/5;ell('Remache_faldon',(.163*sin(a),-.122*cos(a),.980),(.0026,.0026,.0026),bronze,12,8)

# Arms with tapered sleeves, bracers and fingers with individual phalanges.
arm_paths=[]
for s in (-1,1):
    path=[(s*.178,0,1.40),(s*.213,0,1.36),(s*.238,-.006,1.28),(s*.261,-.016,1.19),(s*.276,-.029,1.13)]
    arm_paths.append(path)
    tube('Manga_cota',path,[.071,.071,.064,.052,.047],steel,24)
    tube('Brazo_bajo',[(s*.269,-.02,1.17),(s*.286,-.040,1.08),(s*.298,-.056,.98),(s*.301,-.061,.935)],[.050,.045,.037,.032],skin)
    tube('Brazal_cuero',[(s*.275,-.028,1.14),(s*.284,-.038,1.12),(s*.295,-.052,1.01),(s*.301,-.06,.957)],[.052,.053,.043,.037],leather,24)
    for z,x,r in [(1.12,.282,.053),(1.04,.292,.047),(.972,.300,.039)]:
        tube('Correa_brazal',[(s*x,-.048,z-.008),(s*x,-.048,z+.008)],[r,r],edge,24)
        ell('Remache_brazal',(s*x,-.048-r,z),(.003,.002,.003),bronze,12,8)
    hand_before=set(hero.objects)
    ell('Palma',(s*.302,-.066,.902),(.033,.021,.044),skin)
    # Four subtly bent fingers, visible knuckles and nails.
    for i in range(4):
        xx=s*(.279+i*.015);length=[.055,.065,.063,.048][i]
        path=[(xx,-.072,.895),(xx+s*.003,-.079,.875),(xx+s*.004,-.090,.895-length),(xx+s*.002,-.098,.889-length)]
        tube('Dedo_mano',path,[.0085,.008,.007,.0055],skin,12)
        ell('Una_mano',(xx+s*.002,-.099,.897-length),(.0046,.001,.007),edge,12,8)
    tube('Pulgar',[(s*.273,-.066,.920),(s*.260,-.087,.901),(s*.259,-.104,.881),(s*.267,-.108,.874)],[.013,.012,.010,.007],skin)
    # Fuse the palm and fingers into a continuous study mesh.
    skin_hand=[ob for ob in hero.objects if ob not in hand_before and ob.data.materials[0]==skin]
    bpy.ops.object.select_all(action='DESELECT')
    for ob in skin_hand:
        ob.select_set(True);bpy.context.view_layer.objects.active=ob
        for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.objects.active=skin_hand[0];bpy.ops.object.join()
    ob=bpy.context.object;ob.name='Mano_anatomica_continua'
    mod=ob.modifiers.new('Unir_volumenes','REMESH');mod.mode='VOXEL';mod.voxel_size=.0014
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=ob.modifiers.new('Suavizar_union','SMOOTH');mod.factor=.6;mod.iterations=3
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in ob.data.polygons:poly.use_smooth=True

# Real linked mail (combined mesh for deterministic manageable object count).
link_v=[];link_f=[]
def mail_link(center,u,w,r=.0041,phase=0):
    c=Vector(center);u=Vector(u).normalized();w=Vector(w).normalized();normal=u.cross(w).normalized()
    base=len(link_v);N=10;K=4;wire=.0008
    for i in range(N):
        a=2*pi*i/N+phase;rad=u*cos(a)+w*sin(a)
        for j in range(K):b=2*pi*j/K;link_v.append(c+rad*(r+wire*cos(b))+normal*wire*sin(b))
    for i in range(N):
        for j in range(K):link_f.append((base+i*K+j,base+((i+1)%N)*K+j,base+((i+1)%N)*K+(j+1)%K,base+i*K+(j+1)%K))
for s,path in zip((-1,1),arm_paths):
    for j in range(33):
        t=j/32;zz=1.145+t*.25;xx=s*(.273-t*.07);rad=.049+t*.019
        for i in range(40):
            a=2*pi*(i+.5*(j%2))/40
            mail_link((xx+rad*sin(a),-.012-rad*cos(a),zz),(cos(a),sin(a),0),(s*.23,0,1),phase=(j%2)*.35)
for j in range(7):
    for i in range(126):
        a=2*pi*(i+.5*(j%2))/126;rx=.178;ry=.126
        mail_link((rx*sin(a),-ry*cos(a),.803+j*.008),(cos(a),sin(a),0),(0,0,1),phase=(j%2)*.4)
mesh('Cota_anillas_geometria',link_v,link_f,steel)
link_v=[];link_f=[]
for j in range(12):
    t=j/11;rx=.060+(1-t)*.050;ry=.056+(1-t)*.020;z=1.43+.077*t
    for i in range(72):
        a=2*pi*(i+.5*(j%2))/72
        mail_link((rx*sin(a),.003-ry*cos(a),z),(cos(a),sin(a),0),(0,0,1),r=.0037,phase=(j%2)*.3)
mesh('Gorguera_anillas',link_v,link_f,steel)

# Belt, diagonal baldric, engraved brooch, sword scabbard. No invented heraldry.
loft('Cinturon',[(0,0,.988,.158,.118),(0,0,.997,.161,.12),(0,0,1.036,.16,.12),(0,0,1.04,.156,.116)],dark,sub=1)
for x in [-.13+i*.019 for i in range(15)]:
    y=-.123*math.sqrt(max(.03,1-(x/.162)**2))
    for z in (1.001,1.027):ell('Tacha_cinturon',(x,y-.002,z),(.0027,.0023,.0027),bronze,12,8)
line('Hebilla_cinturon',[(-.028,-.127,1.0),(.029,-.127,1.0),(.029,-.127,1.036),(-.028,-.127,1.036),(-.028,-.127,1.0)],.004,bronze)
line('Hebilla_lengua',[(0,-.134,1.016),(.03,-.134,1.016)],.0024,bronze)
# Flat strap follows the front cuirass.
v=[]
for j in range(25):
    t=j/24;x=-.13+.29*t;z=1.408-.44*t
    for dx in (-.014,.014):v.append((x+dx,(-.108 if z>1.34 else -.130-.008*sin(pi*t)),z))
o=mesh('Bandolera_cuero',v,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(24)],leather,1)
o.modifiers.new('Espesor_correa','SOLIDIFY').thickness=.004
for z in (1.29,1.26):line('Hebilla_bandolera',[(-.060,-.149,z),(-.025,-.149,z-.022),(-.012,-.149,z-.004),(-.046,-.149,z+.018),(-.060,-.149,z)],.0024,bronze)
tube('Vaina_espada',[(.174,-.101,.978),(.193,-.11,.90),(.238,-.116,.68),(.282,-.116,.45)],[ (.021,.012),(.023,.013),(.019,.011),(.012,.008)],leather)
for z,x in [(.94,.184),(.69,.237),(.46,.28)]:tube('Abrazadera_vaina',[(x,-.11,z-.008),(x,-.11,z+.008)],[ (.024,.015),(.024,.015)],bronze)
tube('Empunadura',[(.172,-.1,.986),(.155,-.1,1.09)],[.013,.012],dark)
for i in range(10):
    z=.995+i*.009;x=.170-(z-.995)*.16
    line('Trenzado_empunadura',[(x+.013*sin(a),-.1-.013*cos(a),z+.002*a/pi) for a in [2*pi*k/24 for k in range(25)]],.0012,edge)
tube('Guarda_espada',[(.109,-.1,.984),(.144,-.105,.998),(.185,-.105,1.004),(.221,-.1,1.015)],[.005,.006,.006,.005],bronze)
ell('Pomo_espada',(.153,-.1,1.112),(.023,.012,.023),bronze)

# Anatomical head: continuous surface, sculpted profile and broad mature planes.
head_rings=[(1.535,.023,.025,.002),(1.545,.039,.039,-.005),(1.560,.050,.050,-.002),(1.58,.061,.059,.003),(1.60,.068,.066,.008),(1.62,.073,.071,.009),(1.64,.075,.075,.008),(1.66,.073,.077,.008),(1.68,.072,.078,.008),(1.70,.071,.078,.009),(1.72,.072,.078,.010),(1.74,.073,.076,.012),(1.76,.069,.070,.014),(1.78,.056,.056,.016),(1.793,.033,.031,.017),(1.799,.004,.004,.017)]
def lerp_ring(z):
    for a,b in zip(head_rings,head_rings[1:]):
        if z<=b[0]:
            t=max(0,(z-a[0])/(b[0]-a[0]));return tuple(a[i]*(1-t)+b[i]*t for i in (1,2,3))
    return head_rings[-1][1:]
def gauss(x,z,cx,cz,sx,sz):return exp(-((x-cx)/sx)**2-((z-cz)/sz)**2)
def face(a,z,offset=0):
    rx,ry,cy=lerp_ring(z);x=rx*sin(a);front=max(0,cos(a))**10
    d=.033*gauss(x,z,0,1.665,.014,.012)+.023*gauss(x,z,0,1.692,.011,.036)
    d+=.009*gauss(x,z,0,1.618,.033,.021)+.007*gauss(x,z,0,1.572,.033,.016)
    for s in (-1,1):
        d+=.012*gauss(x,z,s*.034,1.712,.026,.009)
        d-=.012*gauss(x,z,s*.032,1.695,.021,.012)
        d+=.012*gauss(x,z,s*.047,1.662,.021,.018)
        d+=.010*gauss(x,z,s*.013,1.654,.009,.007)
    return (x+offset*sin(a),cy-ry*cos(a)-front*d-offset*cos(a),z)
v=[];f=[];N=128;Z=150
for j in range(Z):
    z=1.535+(1.799-1.535)*j/(Z-1)
    for i in range(N):v.append(face(2*pi*i/N,z))
for j in range(Z-1):
    for i in range(N):a=j*N+i;b=j*N+(i+1)%N;f.append((a,b,b+N,a+N))
f.extend([tuple(reversed(range(N))),tuple((Z-1)*N+i for i in range(N))])
mesh('Rostro_superficie_anatomica',v,f,skin,1)
for s in (-1,1):
    ell('Oreja',(s*.075,.008,1.669),(.014,.019,.035),skin)
    ell('Concha_oreja',(s*.084,-.004,1.666),(.005,.012,.020),lip)
    line('Helice_oreja',[(s*(.083+.006*cos(a)),.007+.015*sin(a),1.67+.027*cos(a)) for a in [2*pi*i/50 for i in range(51)]],.0035,skin)
    # Narrow almond opening rather than exposed spherical eyes.
    cx=s*.031;z=1.695
    ell('Ojo',(cx,-.0605,z),(.0142,.008,.0054),eyeWhite)
    ell('Iris',(cx,-.0680,z),(.0045,.0012,.0045),iris,24,16)
    ell('Pupila',(cx,-.0690,z),(.0021,.0005,.0027),dark,20,12)
    for upper in (True,False):
        vv=[];ff=[]
        for j in range(7):
            t=j/6
            for i in range(41):
                a=pi*i/40;xx=cx+.0143*cos(a)
                zi=z+( .0051 if upper else -.0045)*sin(a)
                yi=-.0605-.008*sin(a)
                zo=zi+( .011 if upper else -.010)*sin(a)
                aa=math.asin(max(-.99,min(.99,xx/lerp_ring(zo)[0])))
                yo=face(aa,zo)[1]-.0006
                vv.append((xx,yi*(1-t)+yo*t,zi*(1-t)+zo*t))
        for j in range(6):
            for i in range(40):q=j*41+i;ff.append((q,q+1,q+42,q+41))
        # The broad loft created an artificial oval crease. Keep a fine rim
        # for this volume study; a proper eyelid needs anatomical sculpting.
        line('Borde_parpado',[vv[i] for i in range(41)],.0008,skin)
    for k in range(90):
        t=k/89;xx=cx-.023+t*.045;zz=1.712+.004*sin(pi*t)+random.uniform(-.0012,.0012)
        aa=math.asin(max(-.99,min(.99,xx/lerp_ring(zz)[0])));yy=face(aa,zz)[1]-.001
        line('Ceja_pelo',[(xx,yy,zz),(xx+s*.002,yy-.0005,zz+.0012)],.00045,hairm[0])
    ell('Narina',(s*.011,-.103,1.652),(.0045,.0018,.0022),dark,20,12)
    # Subtle nasolabial fold, not a painted deep line.
    pts=[]
    for i in range(21):
        t=i/20;xx=s*(.018+.010*t);zz=1.650-.029*t
        aa=math.asin(xx/lerp_ring(zz)[0]);p=face(aa,zz,.0004);pts.append(p)
    line('Pliegue_nasolabial',pts,.0004,skin)
for upper in (True,False):
    vv=[];ff=[]
    for j in range(7):
        t=j/6
        for i in range(51):
            x=-.023+i*.046/50;w=max(0,1-(x/.023)**2)
            z=1.622+( .004 if upper else -.0045)*w*t
            a=math.asin(max(-.99,min(.99,x/lerp_ring(z)[0])));y=face(a,z)[1]-.0007-.002*sin(pi*t)*w
            vv.append((x,y,z))
    for j in range(6):
        for i in range(50):q=j*51+i;ff.append((q,q+1,q+52,q+51))
    mesh('Labio_anatomico',vv,ff,lip,1)
line('Linea_boca',[(x,face(math.asin(x/lerp_ring(1.622)[0]),1.622)[1]-.001,1.622) for x in [-.023+i*.046/40 for i in range(41)]],.00045,dark)

# Short beard and moustache follow the sculpt surface; individually curved fibers.
# Dense dark undercoat, trimmed around lips, continuous with the jaw.
v=[];f=[];N=105;J=32
for j in range(J):
    t=j/(J-1)
    for i in range(N):
        a=-1.72+3.44*i/(N-1)
        low=1.542;high=1.606+.054*min(1,abs(sin(a)))
        v.append(face(a,low+(high-low)*t,.0019))
for j in range(J-1):
    for i in range(N-1):q=j*N+i;f.append((q,q+1,q+N+1,q+N))
mesh('Barba_subpelo',v,f,hairm[1],1)
for k in range(2900):
    a=random.uniform(-1.7,1.7);z=random.uniform(1.546,1.681)
    limit=1.607+.055*min(1,abs(sin(a)))
    if z>limit or (abs(a)<.37 and z>1.608):continue
    length=random.uniform(.003,.008);pts=[]
    for i in range(5):
        t=i/4;p=face(a+.035*t,z-length*t,.002+.0014*sin(t*pi));pts.append(p)
    line('Barba_fibra',pts,random.uniform(.00018,.00035),random.choice(hairm[:3]))
for s in (-1,1):
    for k in range(100):
        t=k/99;x=s*(.004+.021*t);z=1.638+random.uniform(-.002,.002)
        a=math.asin(x/lerp_ring(z)[0]);yy=face(a,z)[1]-.0018
        line('Bigote',[(x,yy,z),(x+s*.004,yy-.001,z-.004),(x+s*.005,yy,z-.006)],.00045,random.choice(hairm[:3]))

# Wavy hair cap plus swept locks, sideburns, back length down to the neck.
def hairpoint(a,t,extra=0):
    low=1.623+.125*max(0,cos(a))**1.3
    z=low+(1.810-low)*t
    rx,ry,cy=lerp_ring(min(1.798,z))
    if z>1.79:
        fade=max(0,(1.810-z)/.020)**.65
        rx=.043*fade;ry=.041*fade
    wav=.0022*sin(9*a+14*t)+.0012*sin(17*a-10*t)
    spread=max(0,1-t)**.4
    return ((rx+(.007+wav+extra)*spread)*sin(a),cy-(ry+(.008+wav+extra)*spread)*cos(a),z)
v=[];f=[];N=100;J=30
for j in range(J):
    for i in range(N):v.append(hairpoint(2*pi*i/N,j/(J-1)))
for j in range(J-1):
    for i in range(N):q=j*N+i;b=j*N+(i+1)%N;f.append((q,b,b+N,q+N))
mesh('Cabellera_base',v,f,hairm[0],1)
for k in range(420):
    a=random.uniform(-pi,pi);t0=random.uniform(.01,.35);t1=random.uniform(.72,.99)
    pts=[]
    for j in range(24):
        t=t0+(t1-t0)*j/23
        aa=a+.28*sin(t*pi)+.055*sin(t*19+a*8)
        pts.append(hairpoint(aa,t,.0015+random.random()*.0003))
    line('Mechon_ondulado',pts,random.uniform(.0004,.00085),random.choices(hairm,[.35,.36,.24,.05])[0])

# Heavy open-front wool cloak. Circumferential drape, folds hang from shoulders.
def cape(a,t,outer=0):
    # a=0 front; opening +- .78 rad; back at pi.
    ztop=1.505-.010*cos(a);z=ztop*(1-t)+(.31+.018*sin(3*a))*t
    rx=.135+.18*(1-exp(-12*t))+.025*t;ry=.074+.107*(1-exp(-10*t))+.025*t
    wave=(.005+.014*t)*sin(16*a+1.2*t)+.004*sin(27*a-3*t)*t
    return ((rx+wave+outer)*sin(a),.022-(ry+wave+outer)*cos(a),z)
v=[];f=[];U=145;V=65;amin=.78;amax=2*pi-.78
for j in range(V):
    for i in range(U):v.append(cape(amin+(amax-amin)*i/(U-1),j/(V-1)))
for j in range(V-1):
    for i in range(U-1):q=j*U+i;f.append((q,q+1,q+U+1,q+U))
o=mesh('Capa_granate_caida_real',v,f,cloth,1);o.modifiers.new('Espesor_lana','SOLIDIFY').thickness=.004
for a in (amin,amax):
    for d in (0,.035):line('Ribete_capa',[cape(a+(d if a==amin else -d),i/100,.003) for i in range(101)],.0024,trim)
    for j in range(55):
        t=.04+j*.017
        line('Bordado_capa',[cape(a+(.017 if a==amin else -.017)+.012*sin(pi*k/8),t+.010*k/8,.0037) for k in range(9)],.0009,trim)
for t in (.977,.997):line('Ribete_inferior',[cape(amin+(amax-amin)*i/180,t,.003) for i in range(181)],.0026,trim)
# Swept shoulder wrap across chest in multiple weighted, sagging folds.
v=[];f=[]
for j in range(16):
    t=j/15
    for i in range(80):
        u=i/79;x=(-.225+.450*u)*(1+.12*t*(1-u))
        top=1.45+.045*sin(pi*u);bottom=1.285+.150*u
        z=top*(1-t)+bottom*t-.008*sin(3*pi*u)*sin(pi*t)
        y=-.090+.030*sin(pi*u)-.105*math.sqrt(t)*sin(pi*u)-.004*sin(t*6*pi+u*2)*sin(pi*t)*sin(pi*u)
        v.append((x,y,z))
for j in range(15):
    for i in range(79):q=j*80+i;f.append((q,q+1,q+81,q+80))
o=mesh('Capa_pliegues_hombros',v,f,cloth,1);o.modifiers.new('Espesor_lana','SOLIDIFY').thickness=.003
line('Ribete_hombro',[v[15*80+i] for i in range(80)],.002,trim)
ell('Broche_laton',(.082,-.159,1.423),(.026,.006,.026),bronze)
for r in (.019,.023):line('Broche_grabado',[(.082+r*cos(a),-.166,1.423+r*sin(a)) for a in [2*pi*i/64 for i in range(65)]],.0012,edge)
for k in range(8):
    a=2*pi*k/8
    line('Broche_flor',[(.082+.004*cos(a),-.167,1.423+.004*sin(a)),(.082+.016*cos(a+.2),-.167,1.423+.016*sin(a+.2)),(.082+.008*cos(a+.4),-.167,1.423+.008*sin(a+.4))],.001,bronze)

# Stage and cameras; these are excluded from the character collection/manifest.
floor=mat('Estudio_arena',(.16,.125,.093),.85)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002));o=move(bpy.context.object,stage);o.name='Suelo_estudio';o.data.materials.append(floor)
scene.world=bpy.data.worlds.new('Mundo_estudio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.13,.10,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.40
def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
def area(name,p,power,size,color):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);stage.objects.link(o);o.location=p;aim(o,(0,0,1.0))
area('Principal_calida',(-3,-4,5),420,3.0,(1,.82,.65))
area('Relleno_neutro',(3,-2,3),210,2.5,(.78,.86,1))
area('Contraluz',(1,3,4),520,2.0,(1,.68,.42))
def camera(name,p,target,scale):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale;d.lens=70
    o=bpy.data.objects.new(name,d);stage.objects.link(o);o.location=p;aim(o,target);return o
cams={
    'Frente_tres_cuartos':camera('CAM_3Q',(2.8,-6,2.65),(0,0,.93),2.12),
    'Espalda':camera('CAM_Espalda',(-2.6,6,2.55),(0,0,.93),2.12),
    'Rostro':camera('CAM_Rostro',(.48,-2.8,1.88),(0,-.005,1.64),.53),
}
scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.render.image_settings.file_format='PNG'
scene.camera=cams['Frente_tres_cuartos']
scene['status']='Editable procedural geometry study; portrait likeness NOT approved; no rig/UV/LOD/game export.'
scene['reference_portrait']='ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png'
scene['reference_sheet']='ArtSource/Concept/CastillaRodrigoV01/Rodrigo_castilla_vistas_v01.png'
scene['authoring_height_m']=1.813
for p in [ROOT/scene['reference_portrait'],ROOT/scene['reference_sheet']]:
    im=bpy.data.images.load(str(p));im.pack();im.use_fake_user=True
txt=bpy.data.texts.new('LEEME_ESTUDIO')
txt.write('Modelo geometrico editable de Rodrigo. Referencias actuales empaquetadas SOLO en datablocks, ninguna imagen se usa como personaje.\nEstudio procedural de volumen y vestuario, no escultura final realista ni semejanza facial aprobada.\nAltura humana de autor, NO escala de exportacion del tablero. Retopologia, UV, bake, rig y LOD pendientes.\n')
# Evaluate counts and validity without creating any game exports.
deps=bpy.context.evaluated_depsgraph_get();manifest={'status':'geometry_study_not_game_ready','objects':{},'warnings':['Facial likeness requires visual approval','Back/lower body inferred','No UV/rig/LOD/bake/export','Dense geometric mail and hair require retopology/baking']}
minv=[float('inf')]*3;maxv=[-float('inf')]*3;tris=0
for ob in hero.objects:
    ev=ob.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles()
    assert all(math.isfinite(c) for vert in me.vertices for c in vert.co),ob.name
    count=len(me.loop_triangles);tris+=count
    for vert in me.vertices:
        p=ob.matrix_world@vert.co
        for i in range(3):minv[i]=min(minv[i],p[i]);maxv[i]=max(maxv[i],p[i])
    manifest['objects'][ob.name]={'type':ob.type,'evaluated_triangles':count};ev.to_mesh_clear()
manifest.update(evaluated_triangles=tris,character_object_count=len(hero.objects),bounds_min=minv,bounds_max=maxv,contains_reference_planes=False,uses_portrait_as_render_surface=False)
(SOURCE/'escultura_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v01.blend'))
for name,cam in cams.items():
    scene.camera=cam;scene.render.filepath=str(OUT/(name+'_v01.png'));bpy.ops.render.render(write_still=True)
scene.camera=cams['Frente_tres_cuartos']
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Rodrigo_escultura_v01.blend'))
print('RODRIGO_GEOMETRY_OK',tris,len(hero.objects),flush=True)
