"""Natural forms revision. Own geometry and procedural PBR; no external assets."""
from pathlib import Path
import argparse,json,math,random,sys
import bpy,bmesh
from mathutils import Vector
SOURCE=Path(__file__).resolve().parent;ROOT=SOURCE.parents[2]
sys.path[:0]=[str(SOURCE),str(SOURCE.parent/'Terrain')]
from fulgor_terrain_tools import stage,caption,source_material,material,load_objects
import fulgor_repertorio_tools as tools
parser=argparse.ArgumentParser();parser.add_argument('--family',choices=['Fortaleza','Tropas','Heroes','Hueste'],required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
FAMILY=args.family;EXPORT=ROOT/'Assets/Fulgor/Art/RepertorioV02'/FAMILY;PREVIEW=ROOT/'Builds/ArtPreview/RepertorioV02'
EXPORT.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.name=FAMILY+'_Natural_V02'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
STONE=(.48,.36,.23);LIGHTSTONE=(.60,.48,.32);DARKSTONE=(.29,.23,.16)
RED=(.46,.025,.017);BLUE=(.025,.115,.35);GOLD=(.64,.37,.085)
STEEL=(.44,.48,.50);DARKMETAL=(.19,.22,.23);WOOD=(.19,.077,.024)
SKIN=(.49,.285,.17);LEATHER=(.10,.044,.020);CREAM=(.69,.57,.37)
KINDS={'stone':0,'cloth':1,'metal':2,'wood':3,'skin':4,'leather':5,'mail':6}
MATS=[]
for kind,scale,distance,metallic,roughness in [('stone',210,.0015,0,.84),('cloth',1100,.00018,0,.88),('metal',520,.0001,.70,.43),
    ('wood',350,.0005,0,.73),('skin',800,.00007,0,.65),('leather',900,.0002,0,.72),('mail',1800,.00015,.60,.55)]:
    mat=source_material('M_Natural_'+kind,scale,distance,.29)
    shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Metallic'].default_value=metallic;shader.inputs['Roughness'].default_value=roughness
    nodes,links=mat.node_tree.nodes,mat.node_tree.links
    if kind=='stone':
        old=shader.inputs['Base Color'].links[0].from_socket
        tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=19;tex.inputs['Detail'].default_value=5
        coord=nodes.new('ShaderNodeTexCoord');links.new(coord.outputs['Position'] if 'Position' in coord.outputs else coord.outputs['Generated'],tex.inputs['Vector'])
        ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.48,.46,.37,1);ramp.color_ramp.elements[1].color=(1.1,1.05,.94,1)
        links.new(tex.outputs['Fac'],ramp.inputs[0]);mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.48
        links.new(old,mix.inputs[1]);links.new(ramp.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],shader.inputs['Base Color'])
    if kind=='mail':
        tex=nodes.new('ShaderNodeTexVoronoi');tex.feature='DISTANCE_TO_EDGE';tex.inputs['Scale'].default_value=1600
        bump=nodes.new('ShaderNodeBump');bump.inputs['Distance'].default_value=.00028;bump.inputs['Strength'].default_value=.55
        links.new(tex.outputs['Distance'],bump.inputs['Height']);links.new(bump.outputs[0],shader.inputs['Normal'])
    MATS.append(mat)
tools.MATS=MATS;tools.KINDS=KINDS;tools.EXPORT=EXPORT;tools.scene=scene
for key in ('STONE','LIGHTSTONE','DARKSTONE','RED','BLUE','GOLD','STEEL','DARKMETAL','WOOD','SKIN','LEATHER','CREAM'):setattr(tools,key,globals()[key])

class OrganicMesh(tools.Mesh):
    def __init__(self):super().__init__();self.rng=random.Random(10672)
    def box(self,p,s,color,kind='stone',part='structure',rotate=0):
        # Stone and hard equipment get small real bevels instead of perfect cube edges.
        rng=self.rng;stone=kind=='stone' and part=='structure'
        dimensions=list(s);center=list(p)
        # Half-length corner stones break the continuous vertical joints of V01.
        if stone:
            patterns=[((.052,.032,.037),0,.054,3,.039,.46),((.032,.052,.037),1,.054,3,.039,.401),
                      ((.071,.035,.035),0,.073,5,.037,.07),((.033,.080,.035),1,.082,4,.037,.168)]
            for expected,axis,spacing,count,course,reference in patterns:
                if all(abs(a-b)<.000001 for a,b in zip(s,expected)) and int((p[2]-.024)/course)%2:
                    anchor=(reference if p[axis]>0 else -reference) if count==3 else reference
                    index=round((p[axis]-anchor)/spacing+(count-1)/2)
                    if index==0:
                        extra=list(p);extra[axis]=anchor-count*spacing/2+spacing/4
                        size=list(s);size[axis]=spacing/2-.0015
                        self.box(extra,size,color,kind,part,rotate)
                    if index==count-1:
                        center[axis]=anchor+count*spacing/2-spacing/4
                        dimensions[axis]=spacing/2-.0015
                    else:
                        center[axis]+=spacing/2
                    break
        if stone and min(s)>.008:
            for i in range(3):
                dimensions[i]*=rng.uniform(.97,1.02)
                center[i]+=rng.uniform(-.00065,.00065)
            center[2]=max(center[2],dimensions[2]/2)
            rotate+=rng.uniform(-.009,.009)
            color=tuple(c*rng.uniform(.94,1.05) for c in color)
        bevel=min(min(dimensions)*.12,.0019 if stone else .0008)
        bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
        for v in bm.verts:v.co=Vector(tuple(v.co[i]*dimensions[i] for i in range(3)))
        bmesh.ops.bevel(bm,geom=list(bm.edges),offset=bevel,segments=2,affect='EDGES')
        bm.verts.ensure_lookup_table();bm.verts.index_update()
        verts=[]
        for vertex in bm.verts:
            x,y,z=vertex.co
            if stone:
                z+=.00045*math.sin(31*x+17*y+center[0]*17)*math.sin(center[2]*26)
            verts.append((center[0]+x*math.cos(rotate)-y*math.sin(rotate),center[1]+x*math.sin(rotate)+y*math.cos(rotate),max(0,center[2]+z)))
        faces=[tuple(v.index for v in face.verts) for face in bm.faces];bm.free()
        self.add(verts,faces,color,kind,part)
    def object(self,name):
        obj=super().object(name)
        for polygon in obj.data.polygons:
            polygon.use_smooth=self.parts[polygon.index] not in ('structure','emblem','blade','trim')
        return obj

def surface(m,rings,color,kind,part,segments=24,fold=0):
    vertices=[]
    for j,(x,y,z,rx,ry) in enumerate(rings):
        for i in range(segments):
            a=math.tau*i/segments
            wrinkle=fold*math.sin(a*7+j*.8)+fold*.32*math.sin(a*13-j*.6)
            vertices.append((x+(rx+wrinkle)*math.cos(a),y+(ry+wrinkle*.7)*math.sin(a),z+.0007*math.sin(a*5+j)))
    faces=[tuple(range(segments-1,-1,-1))]
    for j in range(len(rings)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.append(tuple(range((len(rings)-1)*segments,len(rings)*segments)))
    m.add(vertices,faces,color,kind,part)

def tube(m,points,radii,color,kind,part,segments=12):
    vertices=[]
    for j,p in enumerate(points):
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize();u=tangent.cross(Vector((0,1,0)))
        if u.length<.001:u=tangent.cross(Vector((1,0,0)))
        u.normalize();v=tangent.cross(u)
        r=radii[j] if isinstance(radii[j],tuple) else (radii[j],radii[j])
        for i in range(segments):
            a=math.tau*i/segments;vertices.append(Vector(p)+r[0]*u*math.cos(a)+r[1]*v*math.sin(a))
    faces=[tuple(range(segments-1,-1,-1))]
    for j in range(len(points)-1):
        for i in range(segments):a=j*segments+i;b=j*segments+(i+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.append(tuple(range((len(points)-1)*segments,len(points)*segments)))
    m.add(vertices,faces,color,kind,part)

def cloth(m,origin,width,length,color,part='cape',flag=False):
    cols=24;rows=18;vertices=[];layer=(cols+1)*(rows+1)
    for side in (-1,1):
        for j in range(rows+1):
            t=j/rows
            for i in range(cols+1):
                u=i/cols
                if flag:
                    x=origin[0]+u*width
                    y=origin[1]+.010*math.sin(u*8-t*1.2)*u+side*.00045
                    z=origin[2]-t*length-.008*u+.004*math.sin(u*6+t*2)*u
                else:
                    x=origin[0]+(u-.5)*width*(.61+.39*t)
                    y=origin[1]+.033*t+.006*math.sin(u*math.tau*5+.5*t)*t+side*.00045
                    z=origin[2]-t*length+.005*math.sin(u*5+1)*t*t
                vertices.append((x,y,z))
    faces=[]
    for j in range(rows):
        for i in range(cols):
            a=j*(cols+1)+i;b=a+cols+1;faces.extend(((a,a+1,b+1,b),(a+layer,b+layer,b+1+layer,a+1+layer)))
    border=list(range(cols+1))+[j*(cols+1)+cols for j in range(1,rows+1)]+[rows*(cols+1)+i for i in range(cols-1,-1,-1)]+[j*(cols+1) for j in range(rows-1,0,-1)]
    for a,b in zip(border,border[1:]+border[:1]):faces.append((a,b,b+layer,a+layer))
    m.add(vertices,faces,color,'cloth',part)

def painted_castle(m,x,y,z,size):
    # Thin paint-like relief, rather than thick toy blocks on the shield.
    outline=[(-.5,-.30),(.5,-.30),(.5,.28),(.35,.28),(.35,.08),(.22,.08),(.22,.40),(.08,.40),(.08,.14),(-.08,.14),(-.08,.40),(-.22,.40),(-.22,.08),(-.35,.08),(-.35,.28),(-.5,.28)]
    vertices=[(x+a*size,y+d,z+b*size) for d in (-.00005,.00005) for a,b in outline];n=len(outline)
    m.add(vertices,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],GOLD,'cloth','emblem')

def shield(m,x,y,z,color):
    # Gently convex kite shield with narrow leather edge and small rivets.
    n=32;outline=[]
    for i in range(n):
        a=math.tau*i/n
        zz=.052*math.sin(a);width=.036*(.64+.36*(math.sin(a)+1)/2)
        outline.append((width*math.cos(a),zz))
    vertices=[(x,y-.009,z)]+[(x+a,y-.001,z+b) for a,b in outline]+[(x,y+.003,z)]
    faces=[]
    for i in range(n):faces.extend(((0,1+i,1+(i+1)%n),(n+1,1+(i+1)%n,1+i)))
    m.add(vertices,faces,color,'wood','shield')
    for i in range(n):
        a,b=outline[i],outline[(i+1)%n]
        m.rod((x+a[0],y-.0015,z+a[1]),(x+b[0],y-.0015,z+b[1]),.0009,LEATHER,'leather','shield',6)
    painted_castle(m,x,y-.0095,z,.027)
    for i in range(0,n,4):
        a,b=outline[i];m.ellipsoid((x+a*.90,y-.002,z+b*.90),(.0013,.001,.0013),STEEL,'metal','shield',8,4)

def head(m,sovereign=False):
    # Profile rings shape jaw, cheekbones and brow; the nose is part of the face mesh.
    profiles=[(.374,.009,.010),(.380,.015,.013),(.389,.019,.015),(.399,.021,.016),(.410,.021,.017),(.420,.018,.015),(.428,.008,.007)]
    vertices=[];n=32
    for z,rx,ry in profiles:
        for i in range(n):
            a=math.tau*i/n;front=max(0,-math.sin(a));center=math.exp(-((math.cos(a))/.19)**2)
            nose=.0065*math.exp(-((z-.399)/.007)**2)*center*front
            lip=.0017*math.exp(-((z-.385)/.003)**2)*center*front
            vertices.append((rx*math.cos(a),-.003+ry*math.sin(a)-nose-lip,z))
    faces=[tuple(range(n-1,-1,-1))]
    for j in range(len(profiles)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
    faces.append(tuple(range((len(profiles)-1)*n,len(profiles)*n)))
    m.add(vertices,faces,SKIN,'skin','head')
    for side in (-1,1):
        m.ellipsoid((side*.021,-.001,.397),(.0038,.004,.0065),SKIN,'skin','ear',12,7)
        m.ellipsoid((side*.0086,-.0188,.404),(.0043,.0011,.0015),(.50,.46,.37),'skin','eyes',12,6)
        m.ellipsoid((side*.0086,-.0199,.404),(.00125,.00065,.0012),(.075,.047,.021),'skin','eyes',10,6)
        tube(m,[(side*.004,-.020,.408),(side*.009,-.0205,.409),(side*.014,-.018,.408)], [.0007,.0009,.0005],WOOD,'skin','brow',8)
    tube(m,[(-.005,-.019,.386),(0,-.020,.3855),(.005,-.019,.386)],[.0005,.00065,.0005],(.30,.12,.075),'skin','mouth',8)
    surface(m,[(0,0,.355,.010,.009),(0,0,.371,.009,.009),(0,0,.381,.012,.010)],SKIN,'skin','neck',20)
    if not sovereign:
        surface(m,[(0,0,.408,.023,.020),(0,0,.418,.023,.020),(0,.001,.430,.016,.014),(0,.002,.437,.001,.001)],STEEL,'metal','helmet',32)
        for i in range(32):
            a=math.tau*i/32;b=math.tau*(i+1)/32
            m.rod((.023*math.cos(a),.020*math.sin(a),.410),(.023*math.cos(b),.020*math.sin(b),.410),.0008,DARKMETAL,'metal','helmet_rim',6)
        tube(m,[(0,-.021,.416),(0,-.022,.409),(0,-.027,.396)],[.0018,.0017,.0013],STEEL,'metal','nasal',8)
        # Mail protection at the nape and sides, leaving the face open.
        for side in (-1,1):tube(m,[(side*.019,.007,.407),(side*.021,.010,.390),(side*.017,.011,.372)],[.008,.008,.006],DARKMETAL,'mail','coif',12)
    else:
        surface(m,[(0,.004,.412,.022,.019),(0,.004,.423,.020,.017),(0,.003,.431,.010,.009)],WOOD,'leather','hair',32)
        for side in (-1,1):
            tube(m,[(side*.020,.004,.418),(side*.023,.004,.403),(side*.023,.009,.385),(side*.019,.014,.366)],[.006,.005,.004,.002],WOOD,'leather','braid',12)
            for j in range(9):
                a=j*2.1;m.ellipsoid((side*(.022+.001*math.sin(a)),.009+.001*math.cos(a),.405-j*.004),(.003,.003,.003),(.22,.10,.032),'leather','braid',8,4)
        surface(m,[(0,.002,.418,.023,.020),(0,.002,.422,.023,.020)],GOLD,'metal','circlet',32)
        for i in range(5):
            a=math.tau*i/5;m.ellipsoid((.023*math.cos(a),.002+.020*math.sin(a),.424),(.0016,.0016,.0025),GOLD,'metal','circlet',8,5)

def person(role,color=RED,hero=False,sovereign=False,mounted=False):
    m=OrganicMesh()
    surface(m,[(0,0,z,rx,ry) for z,rx,ry in [(.180,.039,.026),(.194,.041,.027),(.211,.035,.023),(.232,.029,.023),(.258,.031,.025),(.285,.036,.027),(.313,.043,.026),(.336,.046,.024),(.351,.031,.018)]],color,'cloth','tunic',32,.0016)
    surface(m,[(0,0,.229,.031,.024),(0,0,.237,.031,.024)],LEATHER,'leather','belt',32)
    m.box((.005,-.025,.233),(.009,.003,.008),GOLD,'metal','buckle')
    head(m,sovereign)
    for side in (-1,1):
        shift=.009 if side==1 else -.006
        hip=(side*.018,0,.209)
        knee=(side*(.089 if mounted else .028),shift,.125 if not mounted else .156)
        ankle=(side*(.090 if mounted else .031),shift-.004,.031 if not mounted else .082)
        tube(m,[hip,(side*.026,shift/2,.168),knee],[(.014,.013),(.016,.015),(.013,.013)],DARKMETAL,'mail','thigh_L' if side<0 else 'thigh_R',14)
        tube(m,[knee,(ankle[0],ankle[1]+.004,(knee[2]+ankle[2])/2),ankle],[.012,.014,.008],(.16,.11,.070),'cloth','leg_L' if side<0 else 'leg_R',14)
        m.ellipsoid((ankle[0],ankle[1]-.010,ankle[2]-.015),(.011,.025,.016),LEATHER,'leather','boot_L' if side<0 else 'boot_R',16,8)
        for dz in (.038,.051):
            surface(m,[(ankle[0],ankle[1],dz if not mounted else dz+.05,.010,.010),(ankle[0],ankle[1],(dz if not mounted else dz+.05)+.002,.010,.010)],LEATHER,'leather','boot_strap',16)
        shoulder=(side*.044,0,.332);elbow=(side*.061,-.007,.291)
        hand=(side*.070,-.039,.278 if side>0 else .274)
        if role=='ballestero':hand=(side*.061,-.054,.266)
        tube(m,[shoulder,(side*.055,-.003,.310),elbow],[(.017,.018),(.015,.015),(.012,.012)],DARKMETAL,'mail','arm_L' if side<0 else 'arm_R',14)
        tube(m,[elbow,(side*.068,-.022,.283),hand],[.011,.012,.008],LEATHER,'leather','forearm_L' if side<0 else 'forearm_R',14)
        m.ellipsoid((side*.046,-.001,.331),(.018,.022,.010),STEEL,'metal','pauldron',20,10)
        m.ellipsoid(hand,(.008,.007,.011),SKIN,'skin','hand',16,8)
        for j in range(4):
            x=hand[0]+(j-1.5)*.003
            tube(m,[(x,hand[1]-.004,hand[2]+.001),(x,hand[1]-.008,hand[2]-.003),(x,hand[1]-.006,hand[2]-.007)],[.00135,.0013,.001],SKIN,'skin','fingers',8)
        tube(m,[(hand[0]-.006*side,hand[1],hand[2]+.004),(hand[0]-.008*side,hand[1]-.006,hand[2]+.001)],[.0018,.0014],SKIN,'skin','thumb',8)
    if role in ('lancero','caballero','capitan'):
        m.rod((.070,-.043,.015),(.070,-.043,.530),.0022,WOOD,'wood','spear',12)
        m.add([(.064,-.043,.527),(.076,-.043,.527),(.070,-.045,.552),(.070,-.041,.552)],[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],STEEL,'metal','blade')
        if hero or mounted:cloth(m,(.071,-.043,.516),.068,.040,color,'pennon',True)
    elif role=='ballestero':
        tube(m,[(0,-.045,.261),(0,-.070,.267),(0,-.107,.271)],[.0045,.004,.003],WOOD,'wood','crossbow',10)
        tube(m,[(-.064,-.068,.269),(-.037,-.077,.270),(0,-.081,.270),(.037,-.077,.270),(.064,-.068,.269)],[.0018,.0025,.0028,.0025,.0018],DARKMETAL,'metal','crossbow',10)
        for side in (-1,1):m.rod((side*.064,-.068,.269),(0,-.055,.268),.00045,CREAM,'cloth','string',5)
        m.rod((0,-.061,.273),(0,-.125,.273),.0008,WOOD,'wood','bolt',6)
        tube(m,[(.037,.025,.218),(.039,.028,.270)],[(.010,.009),(.011,.010)],LEATHER,'leather','quiver',12)
    else:
        x=.070;y=-.043;z=.280
        m.add([(x-.004,y,z),(x+.004,y,z),(x,y-.0018,z),(x,y+.0018,z),(x,y,z+.112)],[(0,2,4),(2,1,4),(1,3,4),(3,0,4),(0,3,1,2)],STEEL,'metal','blade')
        tube(m,[(x-.017,y,z),(x,y,z+.001),(x+.017,y,z)],[.0018,.0023,.0018],STEEL,'metal','guard',10)
        m.rod((x,y,z-.021),(x,y,z),.0026,LEATHER,'leather','grip',10)
        m.ellipsoid((x,y,z-.023),(.004,.003,.004),GOLD,'metal','pommel',12,6)
    if role!='ballestero':shield(m,-.070,-.055,.269,BLUE if hero else color)
    if hero:
        cloth(m,(0,.020,.345),.126,.214,color,'cape')
        for side in (-1,1):m.ellipsoid((side*.025,-.023,.339),(.003,.0015,.003),GOLD,'metal','brooch',12,6)
        tube(m,[(-.025,-.023,.339),(0,-.028,.330),(.025,-.023,.339)],[.0006,.0006,.0006],GOLD,'metal','chain',8)
    painted_castle(m,0,-.0295,.302,.019)
    return m

def horse():
    body=OrganicMesh();brown=(.24,.087,.030)
    for p,s in [((0,0,.208),(.059,.140,.066)),((0,-.080,.222),(.052,.063,.073)),((0,.087,.216),(.059,.066,.070))]:body.ellipsoid(p,s,brown,'leather','horse_anatomy',28,16)
    tube(body,[(0,-.100,.231),(0,-.128,.279),(0,-.150,.325),(0,-.158,.345)],[(.041,.040),(.036,.035),(.026,.027),(.021,.023)],brown,'leather','horse_anatomy',24)
    tube(body,[(0,-.155,.345),(0,-.186,.337),(0,-.219,.317),(0,-.237,.312)],[(.022,.023),(.024,.023),(.017,.018),(.018,.016)],brown,'leather','horse_anatomy',24)
    for side in (-1,1):
        for front in (-1,1):
            if front<0:points=[(side*.043,-.083,.218),(side*.045,-.091,.154),(side*.046,-.103,.110),(side*.047,-.107,.040),(side*.047,-.112,.018)]
            else:points=[(side*.044,.097,.220),(side*.047,.066,.147),(side*.048,.125,.081),(side*.048,.116,.034),(side*.048,.114,.017)]
            tube(body,points,[(.020,.022),(.014,.015),(.010,.012),(.006,.008),(.008,.008)],brown,'leather','horse_anatomy',16)
    # Merge anatomical volumes, then smooth and reduce: no separate toy cylinders.
    temporary=body.object('TMP_Horse_Anatomy');bpy.context.view_layer.objects.active=temporary
    bpy.ops.object.select_all(action='DESELECT');temporary.select_set(True)
    remesh=temporary.modifiers.new('Volume anatomique','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.0027;remesh.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=temporary.modifiers.new('Muscle transitions','SMOOTH');smooth.factor=.70;smooth.iterations=5;bpy.ops.object.modifier_apply(modifier=smooth.name)
    decimate=temporary.modifiers.new('Surface reduction','DECIMATE');decimate.ratio=.24;bpy.ops.object.modifier_apply(modifier=decimate.name)
    m=OrganicMesh();m.add([v.co.copy() for v in temporary.data.vertices],[tuple(p.vertices) for p in temporary.data.polygons],brown,'leather','horse_anatomy')
    bpy.data.objects.remove(temporary,do_unlink=True)
    for side in (-1,1):
        for y in (-.112,.114):
            surface(m,[(side*.047,y-.003,.001,.010,.014),(side*.047,y-.004,.016,.010,.013),(side*.047,y-.001,.024,.007,.009)],(.08,.065,.046),'leather','hoof',20)
        tube(m,[(side*.014,-.151,.356),(side*.017,-.149,.374),(side*.018,-.150,.383)],[.006,.004,.0005],brown,'leather','ear',12)
        m.ellipsoid((side*.022,-.179,.343),(.0027,.002,.0028),(.022,.014,.007),'skin','horse_eye',12,8)
        m.ellipsoid((side*.015,-.234,.317),(.002,.003,.0017),(.055,.025,.012),'leather','nostril',10,6)
        tube(m,[(side*.023,-.16,.35),(side*.024,-.185,.33),(side*.019,-.233,.311)],[.0014,.0015,.0014],LEATHER,'leather','bridle',8)
        tube(m,[(side*.019,-.233,.311),(side*.033,-.16,.29),(side*.039,-.075,.314),(side*.041,-.023,.333)],[.0009]*4,LEATHER,'leather','reins',8)
    tube(m,[(0,-.102,.268),(0,-.126,.312),(0,-.148,.352)],[.010,.008,.004],(.050,.026,.014),'leather','mane',14)
    tube(m,[(0,.132,.238),(.006,.170,.200),(.014,.184,.130),(.022,.191,.081)],[.012,.011,.009,.002],(.050,.025,.012),'leather','tail',16)
    # Saddlecloth conforms to the back and drops down the flanks.
    vertices=[];columns=20;rows=10
    for j in range(rows+1):
        y=-.052+j*.125/rows
        for i in range(columns+1):
            u=-1+2*i/columns;x=.075*u;z=.283-.079*abs(u)**1.55+.002*math.sin(i*1.2+j*.4)
            vertices.append((x,y,z))
    faces=[]
    for j in range(rows):
        for i in range(columns):a=j*(columns+1)+i;b=a+columns+1;faces.append((a,a+1,b+1,b))
    # Give the cloth actual thickness through the second surface.
    bottom=[(x,y,z-.001) for x,y,z in vertices];n=len(vertices);vertices+=bottom
    faces+=[tuple(i+n for i in reversed(face)) for face in faces[:]]
    m.add(vertices,faces,RED,'cloth','saddlecloth')
    m.ellipsoid((0,.013,.291),(.035,.048,.014),LEATHER,'leather','saddle',24,10)
    return m

def mounted_knight():
    m=horse();m.append(person('caballero',RED,False,False,True),(0,.009,.135),.68);return m

def castle_glyph(m,x,y,z,size,kind='cloth'):painted_castle(m,x,y,z,size)
tools.Mesh=OrganicMesh;tools.castle_glyph=castle_glyph
def fortress():
    m=tools.fortress_layout()
    # Break the ruler-straight coursing on the tower and keep faces.
    # Shift alternate short vertical wall blocks while retaining the layout envelope.
    # The small displacement follows position so shared bevel rings remain coherent.
    for index,v in enumerate(m.v):
        x,y,z=v
        if z>.04:
            x+=.0007*math.sin(z*390+x*40)*math.sin(y*21)
            y+=.0007*math.sin(z*310+y*31)*math.sin(x*19)
        m.v[index]=(x,y,z)
    rng=random.Random(553)
    # Rubble and weeds at the foot, using small irregular stones rather than a clean plinth.
    for i in range(95):
        edge=rng.choice(('front','side'))
        x,y=(rng.uniform(-.55,.55),rng.choice((-.515,.515))) if edge=='front' else (rng.choice((-.55,.55)),rng.uniform(-.50,.50))
        m.ellipsoid((x,y,.012),(rng.uniform(.006,.016),rng.uniform(.006,.014),rng.uniform(.004,.009)),DARKSTONE,'stone','rubble',10,6)
    return m

def army_marker():
    m=OrganicMesh()
    for x,role in ((-.092,'lancero'),(.092,'espadachin')):m.append(person(role),(x,0,0),.74)
    m.rod((0,.036,.003),(0,.036,.545),.0028,WOOD,'wood','standard',12)
    cloth(m,(.001,.036,.529),.125,.121,BLUE,'standard_cloth',True)
    painted_castle(m,.061,.027,.463,.045)
    m.ellipsoid((0,.036,.548),(.004,.004,.005),GOLD,'metal','finial',12,6)
    return m

if FAMILY=='Fortaleza':specs=[('FortalezaCastellanaNatural_A',fortress(),2048)]
elif FAMILY=='Tropas':specs=[('LanceroNatural_A',person('lancero'),1024),('EspadachinNatural_A',person('espadachin'),1024),('BallesteroNatural_A',person('ballestero',BLUE),1024),('CaballeroMontadoNatural_A',mounted_knight(),1024)]
elif FAMILY=='Heroes':specs=[('HeroeCapitanNatural_A',person('capitan',BLUE,True),1024),('HeroinaSoberanaNatural_A',person('soberana',RED,True,True),1024)]
else:specs=[('HuesteEstandarteNatural_A',army_marker(),1024)]
models=[];reports=[];source_collection=bpy.data.collections.new('FUENTES_EDITABLES');scene.collection.children.link(source_collection)
for name,builder,resolution in specs:
    obj=builder.object('SRC_'+name)
    for collection in list(obj.users_collection):collection.objects.unlink(obj)
    source_collection.objects.link(obj);obj.hide_render=True;obj.hide_set(True)
    exported=obj.copy();exported.data=obj.data.copy();exported.name='SM_'+name+'_LOD0';scene.collection.objects.link(exported);exported.hide_render=False;exported.hide_set(False)
    report=tools.bake(exported,resolution);report['revision']='Natural V02';reports.append(report);models.append(exported)
    exported.use_fake_user=True
    for collection in list(exported.users_collection):collection.objects.unlink(exported)
    print('NATURAL_MODEL_READY',exported.name,report['triangles'],flush=True)
source_collection.hide_render=True;source_collection.hide_viewport=True

def place(obj,x,y=0):
    copy=obj.copy();copy.location=(x,y,0);scene.collection.objects.link(copy);return copy
def lighting(camera):
    bpy.data.objects['PREVIEW_Suelo'].location.z=-.0005
    scene.world.use_nodes=True;bg=scene.world.node_tree.nodes.get('Background');bg.inputs[0].default_value=(.33,.37,.43,1);bg.inputs[1].default_value=.32
    bpy.ops.object.light_add(type='AREA',location=(-1,-4,3));lamp=bpy.context.object;lamp.data.energy=100;lamp.data.size=3
    lamp.rotation_euler=(-lamp.location).to_track_quat('-Z','Y').to_euler()
    scene.cycles.samples=64

gold=(.72,.45,.16)
if FAMILY=='Fortaleza':
    place(models[0],0)
    camera=stage(scene,(0,0,.34),1.9,PREVIEW/'Fortaleza_natural_v02.png')
    target=Vector((0,0,.33));camera.location=target+Vector((2.5,-4,2.38)).normalized()*11;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1450
    caption(camera,'FULGOR MEDIEVAL / REVISION NATURAL',-.85,.78,.037,gold)
    caption(camera,'FORTALEZA / PIEDRA ENVEJECIDA',-.85,.66,.066)
    caption(camera,'Geometria y mapas propios / Estudio sin integrar en Unity',-.85,-.72,.030,gold)
elif FAMILY=='Tropas':
    for i,obj in enumerate(models):place(obj,(i-1.5)*.42)
    camera=stage(scene,(0,0,.26),2.03,PREVIEW/'Tropas_natural_v02.png');target=Vector((0,0,.26))
    camera.location=target+Vector((.2,-7,2.7)).normalized()*11;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1100
    caption(camera,'FULGOR MEDIEVAL / REVISION NATURAL',-.92,.58,.042,gold);caption(camera,'HUESTES / PROPORCIONES Y EQUIPO',-.92,.46,.068)
    for i,label in enumerate(['LANCERO','ESPADACHIN','BALLESTERO','CABALLERO']):caption(camera,label,(i-1.5)*.42,-.34,.032,align='CENTER')
    caption(camera,'Anatomia, tejido y acero / Poses estaticas / Rig pendiente',-.92,-.59,.030,gold)
elif FAMILY=='Heroes':
    for i,obj in enumerate(models):place(obj,(i-.5)*.35)
    camera=stage(scene,(0,0,.26),1.05,PREVIEW/'Heroes_natural_v02.png');target=Vector((0,0,.26))
    camera.location=target+Vector((.2,-7,2.3)).normalized()*11;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1350
    caption(camera,'FULGOR MEDIEVAL / REVISION NATURAL',-.47,.39,.026,gold);caption(camera,'CAPITAN Y SOBERANA',-.47,.31,.048)
    caption(camera,'CAPITAN',-.175,-.29,.024,align='CENTER');caption(camera,'SOBERANA',.175,-.29,.024,align='CENTER')
    caption(camera,'Formas continuas y telas con caida / Rig pendiente',-.47,-.39,.020,gold)
else:
    place(models[0],0)
    camera=stage(scene,(0,0,.29),1.05,PREVIEW/'Hueste_natural_v02.png');target=Vector((0,0,.29))
    camera.location=target+Vector((.8,-7,2.7)).normalized()*11;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1300
    caption(camera,'FULGOR MEDIEVAL / REVISION NATURAL',-.46,.37,.025,gold);caption(camera,'HUESTE CON ESTANDARTE',-.46,.29,.041)
    caption(camera,'Marcador estrategico / Geometria y materiales propios',-.46,-.37,.020,gold)
lighting(camera);bpy.ops.render.render(write_still=True)

# Same camera/light before-after, using the real V01 sources.
comparison=bpy.data.scenes.new(FAMILY+'_Antes_Despues');bpy.context.window.scene=comparison;scene=comparison
old_names={'Fortaleza':['SM_FortalezaCastellana_A_LOD0'],'Heroes':['SM_HeroeCapitan_A_LOD0'],'Tropas':['SM_CaballeroMontado_A_LOD0'],'Hueste':['SM_HuesteEstandarte_A_LOD0']}
old=load_objects(SOURCE.parent/'RepertorioV01'/(FAMILY+'_v01.blend'),old_names[FAMILY])[0]
index=3 if FAMILY=='Tropas' else 0
distance=.80 if FAMILY=='Fortaleza' else .28
place(old,-distance);place(models[index],distance)
ortho=3.1 if FAMILY=='Fortaleza' else 1.22
camera=stage(scene,(0,0,.30 if FAMILY=='Fortaleza' else .27),ortho,PREVIEW/(FAMILY+'_comparacion_v02.png'))
scene.render.resolution_y=1250
target=Vector((0,0,.30 if FAMILY=='Fortaleza' else .27));direction=Vector((2.5,-4,2.38)) if FAMILY=='Fortaleza' else Vector((.2,-7,2.7))
camera.location=target+direction.normalized()*11;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
caption(camera,'FULGOR MEDIEVAL / ANTES Y DESPUES',-ortho*.445,ortho*.32,.031 if FAMILY!='Fortaleza' else .055,gold)
caption(camera,'V01',-distance,-ortho*.30,.030 if FAMILY!='Fortaleza' else .060,align='CENTER')
caption(camera,'NATURAL V02',distance,-ortho*.30,.030 if FAMILY!='Fortaleza' else .060,align='CENTER')
lighting(camera);bpy.ops.render.render(write_still=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(FAMILY+'_natural_v02.blend')))
(EXPORT/'repertorio_manifest.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_NATURAL_COMPLETE',FAMILY,len(models),flush=True)


