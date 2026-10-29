"""Original strategic art: castle, four troops, two heroes and army marker.

Blender --background --factory-startup --python-exit-code 1 --python THIS -- --family Fortaleza
Families: Fortaleza, Tropas, Heroes, Hueste. No external art dependencies.
"""
from pathlib import Path
import argparse, json, math, random, sys
import bpy, bmesh
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
ROOT=SOURCE.parents[2]
sys.path.insert(0,str(SOURCE.parent/'Terrain'))
from fulgor_terrain_tools import stage,caption,source_material,material
parser=argparse.ArgumentParser()
parser.add_argument('--family',choices=['Fortaleza','Tropas','Heroes','Hueste'],required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
FAMILY=args.family
EXPORT=ROOT/'Assets/Fulgor/Art/RepertorioV01'/FAMILY
PREVIEW=ROOT/'Builds/ArtPreview/RepertorioV01'
EXPORT.mkdir(parents=True,exist_ok=True);PREVIEW.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.name=FAMILY+'_Estudio'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1

STONE=(.57,.43,.27);LIGHTSTONE=(.70,.57,.39);DARKSTONE=(.36,.28,.18)
RED=(.53,.025,.018);BLUE=(.025,.14,.48);GOLD=(.78,.44,.065)
STEEL=(.37,.43,.47);DARKMETAL=(.095,.115,.12);WOOD=(.22,.095,.027)
SKIN=(.55,.30,.15);LEATHER=(.12,.055,.022);CREAM=(.76,.62,.40)

MATS=[]
for kind,scale,bump,metallic,roughness in [('stone',140,.0012,0,.84),('cloth',850,.00025,0,.88),
    ('metal',260,.0002,.78,.33),('wood',240,.0007,0,.7),('skin',400,.00015,0,.61),('leather',400,.0004,0,.67)]:
    mat=source_material('M_Fuente_'+kind,scale,bump,.25)
    shader=mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Metallic'].default_value=metallic
    shader.inputs['Roughness'].default_value=roughness
    if kind=='metal':
        wave=mat.node_tree.nodes.new('ShaderNodeTexNoise');wave.inputs['Scale'].default_value=700
        shader.inputs['Roughness'].default_value=.38
    MATS.append(mat)
KINDS={'stone':0,'cloth':1,'metal':2,'wood':3,'skin':4,'leather':5}

class Mesh:
    def __init__(self):self.v=[];self.f=[];self.c=[];self.m=[];self.parts=[]
    def add(self,verts,faces,color,kind='stone',part='structure'):
        start=len(self.v);self.v.extend([tuple(v) for v in verts])
        self.f.extend([tuple(start+i for i in f) for f in faces]);self.c.extend([color]*len(faces))
        self.m.extend([KINDS[kind]]*len(faces));self.parts.extend([part]*len(faces))
    def box(self,p,s,color,kind='stone',part='structure',rotate=0):
        x,y,z=p;a,b,c=[n/2 for n in s]
        verts=[]
        for u,v,w in [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),(-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]:
            verts.append((x+u*math.cos(rotate)-v*math.sin(rotate),y+u*math.sin(rotate)+v*math.cos(rotate),z+w))
        self.add(verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],color,kind,part)
    def ellipsoid(self,p,s,color,kind='skin',part='head',segments=12,rings=7):
        verts=[(p[0],p[1],p[2]-s[2])]
        for j in range(1,rings):
            a=-math.pi/2+math.pi*j/rings
            for i in range(segments):
                b=math.tau*i/segments
                verts.append((p[0]+s[0]*math.cos(a)*math.cos(b),p[1]+s[1]*math.cos(a)*math.sin(b),p[2]+s[2]*math.sin(a)))
        top=len(verts);verts.append((p[0],p[1],p[2]+s[2]))
        faces=[(0,1+(i+1)%segments,1+i) for i in range(segments)]
        for j in range(rings-2):
            a=1+j*segments;b=a+segments
            for i in range(segments):k=(i+1)%segments;faces.append((a+i,a+k,b+k,b+i))
        faces.extend((top,top-segments+i,top-segments+(i+1)%segments) for i in range(segments))
        self.add(verts,faces,color,kind,part)
    def rod(self,a,b,r,color,kind='metal',part='weapon',segments=8,end_radius=None):
        a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,0,1)))
        if u.length<.001:u=Vector((1,0,0))
        u.normalize();v=axis.cross(u);end_radius=r if end_radius is None else end_radius
        vertices=[p+rad*(u*math.cos(math.tau*i/segments)+v*math.sin(math.tau*i/segments)) for p,rad in ((a,r),(b,end_radius)) for i in range(segments)]
        faces=[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
        faces.extend([tuple(range(segments-1,-1,-1)),tuple(range(segments,2*segments))])
        self.add(vertices,faces,color,kind,part)
    def taper(self,p,bottom,top,height,color,kind='cloth',part='torso'):
        verts=[]
        for z,w,d in ((p[2]-height/2,*bottom),(p[2]+height/2,*top)):
            verts.extend((p[0]+x*w/2,p[1]+y*d/2,z) for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)])
        self.add(verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],color,kind,part)
    def cloth(self,origin,width,length,color,part='cape',vertical=True):
        verts=[];cols=10;rows=10
        for side in (-1,1):
            for j in range(rows+1):
                t=j/rows
                for i in range(cols+1):
                    u=i/cols
                    if vertical:
                        verts.append((origin[0]+(u-.5)*width*(.70+.30*t),origin[1]+.045*t+.008*math.sin(u*math.tau*3+t)*t+side*.0007,origin[2]-length*t))
                    else:
                        verts.append((origin[0]+width*u,origin[1]+.018*math.sin(u*math.pi*3+j*.15)+side*.0007,origin[2]-length*t+.012*math.sin(u*5)))
        layer=(cols+1)*(rows+1);faces=[]
        for j in range(rows):
            for i in range(cols):
                a=j*(cols+1)+i;b=a+cols+1
                faces.extend(((a,a+1,b+1,b),(a+layer,b+layer,b+1+layer,a+1+layer)))
        border=list(range(cols+1))+[j*(cols+1)+cols for j in range(1,rows+1)]+[rows*(cols+1)+i for i in range(cols-1,-1,-1)]+[j*(cols+1) for j in range(rows-1,0,-1)]
        for a,b in zip(border,border[1:]+border[:1]):faces.append((a,b,b+layer,a+layer))
        self.add(verts,faces,color,'cloth',part)
    def append(self,other,offset=(0,0,0),scale=1):
        start=len(self.v);self.v.extend(tuple(Vector(v)*scale+Vector(offset)) for v in other.v)
        self.f.extend(tuple(start+i for i in f) for f in other.f);self.c.extend(other.c);self.m.extend(other.m);self.parts.extend(other.parts)
    def object(self,name):
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(self.v,[],self.f);mesh.update()
        attr=mesh.color_attributes.new(name='ArtTint',type='FLOAT_COLOR',domain='CORNER')
        part_attr=mesh.attributes.new(name='ArtPart',type='INT',domain='FACE')
        part_ids={name:i for i,name in enumerate(dict.fromkeys(self.parts))}
        for polygon,color,mat_index,part in zip(mesh.polygons,self.c,self.m,self.parts):
            polygon.material_index=mat_index;part_attr.data[polygon.index].value=part_ids[part]
            for index in polygon.loop_indices:attr.data[index].color=(*color,1)
            polygon.use_smooth=part in ('head','horse','hand','hair','limb','torso','skirt') or part.startswith('boot')
        for mat in MATS:mesh.materials.append(mat)
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
        obj=bpy.data.objects.new(name,mesh);scene.collection.objects.link(obj)
        # Parts are vertex groups so source geometry can be separated for later rigging.
        for part,index in part_ids.items():
            group=obj.vertex_groups.new(name=part)
            indices=set(i for poly in mesh.polygons if mesh.attributes['ArtPart'].data[poly.index].value==index for i in poly.vertices)
            group.add(list(indices),1,'REPLACE')
        obj['source_part_names']=json.dumps(part_ids)
        return obj

def castle_glyph(m,x,y,z,size,kind='cloth'):
    # Original geometric emblem; no claim to be an exact historical coat of arms.
    m.box((x,y,z),(size,.002,size*.44),GOLD,kind,'emblem')
    for dx in (-.32,0,.32):
        m.box((x+dx*size,y-.0006,z+size*.28),(size*.20,.0025,size*.40),GOLD,kind,'emblem')

def shield(m,x,y,z,color=RED,scale=1):
    w=.050*scale;h=.067*scale;depth=.006*scale
    outline=[(-w,-h*.55),(0,-h),(w,-h*.55),(w,h*.75),(-w,h*.75)]
    verts=[(x+a,y+d,z+b) for d in (-depth,depth) for a,b in outline]
    m.add(verts,[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],color,'wood','shield')
    for i in range(5):
        a,b=outline[i],outline[(i+1)%5]
        m.rod((x+a[0],y-depth-.001,z+a[1]),(x+b[0],y-depth-.001,z+b[1]),.0022*scale,GOLD,'metal','shield',5)
    castle_glyph(m,x,y-depth-.003,z,.041*scale)
    m.ellipsoid((x,y-depth-.004,z-.016*scale),(.006*scale,.003*scale,.006*scale),DARKMETAL,'metal','shield',8,4)

def face(m,p,helmet=True,sovereign=False):
    x,y,z=p
    m.ellipsoid(p,(.026,.023,.034),SKIN,'skin','head')
    m.ellipsoid((x,y-.024,z-.001),(.005,.007,.009),SKIN,'skin','head',8,5)
    for side in (-1,1):
        m.ellipsoid((x+side*.010,y-.021,z+.008),(.003,.002,.002),(.018,.014,.009),'skin','head',8,4)
        m.rod((x+side*.006,y-.022,z+.016),(x+side*.017,y-.020,z+.015),.0015,WOOD,'skin','head',5)
    m.rod((x-.007,y-.022,z-.015),(x+.007,y-.022,z-.015),.0014,(.26,.085,.045),'skin','head',5)
    if helmet:
        m.ellipsoid((x,y+.001,z+.020),(.029,.026,.027),STEEL,'metal','helmet',12,6)
        # Separate nose guard, rim and back neck protection keep the face visible.
        m.box((x,y-.026,z+.012),(.006,.004,.030),DARKMETAL,'metal','helmet')
        m.rod((x-.026,y-.014,z+.010),(x+.026,y-.014,z+.010),.0028,STEEL,'metal','helmet')
        m.box((x,y+.024,z-.010),(.038,.005,.024),DARKMETAL,'metal','helmet')
    else:
        m.ellipsoid((x,y+.008,z+.015),(.028,.023,.026),(.13,.055,.018),'leather','hair',12,6)
        m.ellipsoid((x,y+.002,z+.032),(.028,.025,.018),(.13,.055,.018),'leather','hair',16,6)
        for side in (-1,1):
            for j in range(6):m.ellipsoid((x+side*.025,y+.003,z-j*.007),(.005,.005,.006),WOOD,'leather','hair',8,4)
        m.rod((x,y,z+.024),(x,y,z+.032),.029,GOLD,'metal','crown',16)
        for i in range(6):
            a=math.tau*i/6
            m.ellipsoid((x+.026*math.cos(a),y+.026*math.sin(a),z+.036),(.003,.003,.005),GOLD,'metal','crown',6,4)

def person(role,color=RED,hero=False,sovereign=False,mounted=False):
    m=Mesh()
    # Body is articulated in the source groups; this first delivery is a static pose.
    m.ellipsoid((0,0,.262),(.048,.031,.061),color,'cloth','torso',16,10)
    # A flared elliptical hem with small folds replaces the rectangular blockout.
    skirt_vertices=[];skirt_segments=16
    for z,rx,ry in ((.151,.052,.034),(.181,.044,.029),(.213,.035,.024)):
        for i in range(skirt_segments):
            a=math.tau*i/skirt_segments;fold=1+.035*math.cos(a*8)
            skirt_vertices.append((rx*math.cos(a)*fold,ry*math.sin(a)*fold,z))
    skirt_faces=[tuple(range(skirt_segments-1,-1,-1))]
    for j in range(2):
        for i in range(skirt_segments):
            a=j*skirt_segments+i;b=j*skirt_segments+(i+1)%skirt_segments
            skirt_faces.append((a,b,b+skirt_segments,a+skirt_segments))
    skirt_faces.append(tuple(range(2*skirt_segments,3*skirt_segments)))
    m.add(skirt_vertices,skirt_faces,color,'cloth','skirt')
    m.box((0,0,.214),(.079,.051,.012),LEATHER,'leather','belt')
    m.box((0,-.028,.214),(.011,.004,.012),GOLD,'metal','belt')
    m.rod((0,0,.309),(0,0,.336),.014,SKIN,'skin','head',10)
    face(m,(0,-.002,.365),not sovereign,sovereign)
    # Shoulder plates and chainmail sleeves are readable at strategic scale.
    for side in (-1,1):
        m.ellipsoid((side*.051,0,.300),(.024,.032,.019),STEEL,'metal','armor',10,5)
        knee=(side*(.105 if mounted else .030),.004,.103)
        hip=(side*.030,0,.177)
        ankle=(side*(.108 if mounted else .035),-.004,.034 if not mounted else .064)
        m.rod(hip,knee,.018,DARKMETAL,'metal','leg_L' if side<0 else 'leg_R',8,end_radius=.014)
        m.rod(knee,ankle,.013,LEATHER,'leather','leg_L' if side<0 else 'leg_R',8,end_radius=.010)
        m.ellipsoid(knee,(.016,.018,.018),STEEL,'metal','armor',8,5)
        m.ellipsoid((ankle[0],-.017,ankle[2]-.018),(.014,.027,.015),LEATHER,'leather','boot_L' if side<0 else 'boot_R',12,6)
    left_hand=(-.075,-.048,.253)
    right_hand=(.078,-.046,.275 if role!='ballestero' else .249)
    for side,hand in ((-1,left_hand),(1,right_hand)):
        shoulder=(side*.052,0,.296);elbow=(side*.075,-.014,.263)
        m.rod(shoulder,elbow,.016,DARKMETAL,'metal','arm_L' if side<0 else 'arm_R',8,end_radius=.012)
        m.rod(elbow,hand,.011,LEATHER,'leather','arm_L' if side<0 else 'arm_R',8,end_radius=.009)
        m.ellipsoid(hand,(.011,.010,.014),SKIN,'skin','hand',8,5)
        for j in range(3):m.rod((hand[0]+(j-1)*.004,hand[1]-.008,hand[2]),(hand[0]+(j-1)*.004,hand[1]-.009,hand[2]-.008),.0015,SKIN,'skin','hand',5)
    if role in ('lancero','caballero','capitan'):
        a=(.080,-.052,.035);b=(.080,-.052,.530 if role=='lancero' else .485)
        m.rod(a,b,.0035,WOOD,'wood','spear',7)
        x,y,z=b
        m.add([(x-.010,y,z-.008),(x+.010,y,z-.008),(x,y-.003,z+.025),(x,y+.003,z+.025)],[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],STEEL,'metal','spear')
        if role!='lancero':m.cloth((x,y,z-.015),.075,.045,color,'pennon',False)
    elif role=='ballestero':
        m.rod((-.036,-.065,.240),(.050,-.098,.265),.006,WOOD,'wood','crossbow',6)
        for side in (-1,1):m.rod((.030,-.087,.260),(side*.077,-.083,.263),.0035,DARKMETAL,'metal','crossbow',6)
        m.rod((-.077,-.083,.263),(.012,-.115,.260),.0008,CREAM,'cloth','crossbow',4)
        m.rod((.077,-.083,.263),(.012,-.115,.260),.0008,CREAM,'cloth','crossbow',4)
        m.rod((.012,-.073,.265),(.012,-.138,.265),.0012,WOOD,'wood','crossbow',5)
        m.box((.043,.033,.226),(.022,.028,.065),LEATHER,'leather','quiver')
        for i in range(4):m.rod((.036+i*.005,.034,.239),(.036+i*.005,.034,.284),.001,WOOD,'wood','quiver',4)
    else:
        m.box((.080,-.050,.318),(.010,.004,.100),STEEL,'metal','sword')
        m.box((.080,-.050,.266),(.038,.007,.006),GOLD,'metal','sword')
        m.rod((.080,-.050,.246),(.080,-.050,.266),.0035,LEATHER,'leather','sword',6)
    if role!='ballestero':shield(m,-.075,-.064,.255,BLUE if hero else color,.9)
    if hero or sovereign:
        m.cloth((0,.026,.311),.145,.224,color,'cape')
        m.rod((-.047,-.027,.303),(.047,-.027,.303),.0025,GOLD,'metal','brooch',8)
        for x in (-.045,.045):m.ellipsoid((x,-.031,.302),(.006,.003,.006),GOLD,'metal','brooch',8,4)
        m.box((0,-.032,.267),(.008,.003,.053),GOLD,'cloth','tabard')
    else:castle_glyph(m,0,-.033,.266,.035)
    return m

def horse():
    m=Mesh();brown=(.25,.090,.027);mane=(.055,.025,.012)
    m.ellipsoid((0,0,.208),(.068,.132,.070),brown,'leather','horse',16,9)
    for side in (-1,1):
        for front in (-1,1):
            hip=(side*.045,front*.088,.202);knee=(side*.047,front*.102,.100);hoof=(side*.049,front*.105,.018)
            m.rod(hip,knee,.018,brown,'leather','horse_leg',8,end_radius=.011)
            m.rod(knee,hoof,.009,brown,'leather','horse_leg',8,end_radius=.007)
            m.box((hoof[0],hoof[1]-.007,.012),(.022,.032,.024),DARKMETAL,'leather','horse_leg')
    m.rod((0,-.093,.225),(0,-.156,.319),.045,brown,'leather','horse',12,end_radius=.027)
    m.ellipsoid((0,-.183,.326),(.028,.061,.030),brown,'leather','horse',12,7)
    m.ellipsoid((0,-.227,.310),(.023,.028,.020),(.16,.065,.025),'leather','horse',10,6)
    for side in (-1,1):
        m.ellipsoid((side*.017,-.161,.363),(.008,.012,.028),brown,'leather','horse',8,5)
        m.ellipsoid((side*.025,-.180,.335),(.003,.003,.004),(.009,.006,.004),'skin','horse',8,4)
        m.rod((side*.029,-.19,.34),(side*.026,-.225,.305),.0025,LEATHER,'leather','bridle',6)
        m.rod((side*.026,-.225,.305),(side*.047,-.05,.355),.0017,LEATHER,'leather','reins',5)
    for j in range(9):m.ellipsoid((0,-.11-j*.004,.255+j*.010),(.014,.018,.012),mane,'leather','mane',8,5)
    m.rod((0,.118,.233),(0,.166,.16),.014,mane,'leather','tail',8,end_radius=.010)
    m.rod((0,.166,.16),(.009,.177,.082),.010,mane,'leather','tail',8,end_radius=.004)
    m.box((0,.015,.280),(.137,.135,.009),RED,'cloth','saddlecloth')
    m.box((0,.015,.291),(.090,.085,.021),LEATHER,'leather','saddle')
    for side in (-1,1):
        m.box((side*.068,.016,.226),(.006,.111,.105),RED,'cloth','saddlecloth')
        m.rod((side*.071,-.038,.175),(side*.071,.065,.175),.002,GOLD,'cloth','trim',5)
    return m

def mounted_knight():
    m=horse();rider=person('caballero',RED,False,False,True)
    m.append(rider,(0,.016,.235),.62)
    return m

def fortress():
    m=Mesh();rng=random.Random(8401)
    # Warm individual ashlar courses, a real open gate and square Castilian silhouette.
    m.box((0,0,.012),(1.13,1.05,.024),DARKSTONE)
    def wall(cx,cy,length,height,axis='x',thickness=.072,base=.024):
        courses=max(1,round(height/.038));course_h=height/courses
        for row in range(courses):
            n=max(1,round(length/.09));offset=.5 if row%2 else 0
            cuts=[-length/2]+[-length/2+(i+offset)*length/n for i in range(1,n+1) if -length/2<( -length/2+(i+offset)*length/n)<length/2]+[length/2]
            for a,b in zip(cuts,cuts[1:]):
                p=(cx+(a+b)/2,cy,base+(row+.5)*course_h) if axis=='x' else (cx,cy+(a+b)/2,base+(row+.5)*course_h)
                size=(b-a-.0013,thickness,course_h-.0011) if axis=='x' else (thickness,b-a-.0013,course_h-.0011)
                k=rng.uniform(.88,1.10);m.box(p,size,tuple(v*k for v in STONE))
        for i in range(max(2,int(length/.096))):
            u=-length/2+.032+i*(length-.064)/max(1,int(length/.096)-1)
            p=(cx+u,cy,base+height+.025) if axis=='x' else (cx,cy+u,base+height+.025)
            m.box(p,(.044,thickness+.008,.050) if axis=='x' else (thickness+.008,.044,.050),LIGHTSTONE)
    wall(-.327,-.415,.31,.32);wall(.327,-.415,.31,.32)
    # Front opening arch, each voussoir is a wedge rather than a painted recess.
    spring=.177;inner=.125;outer=.171;segments=13
    for i in range(segments):
        a=i*math.pi/segments+.007;b=(i+1)*math.pi/segments-.007
        verts=[(r*math.cos(t),y,spring+r*math.sin(t)) for y in (-.458,-.372) for r,t in [(inner,a),(inner,b),(outer,b),(outer,a)]]
        m.add(verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],LIGHTSTONE)
    for side in (-1,1):wall(side*.145,-.415,.039,.151,thickness=.090)
    m.box((0,-.415,.337),(.352,.087,.035),LIGHTSTONE)
    for i in (-1,0,1):m.box((i*.113,-.415,.384),(.051,.087,.057),LIGHTSTONE)
    # Two half-open timber leaves leave a visibly clear passage.
    for side in (-1,1):
        for j in range(5):m.box((side*(.112-j*.015),-.365+j*.011,.091),(.014,.013,.132),WOOD,'wood','gate',side*.20)
        for z in (.055,.13):m.box((side*.084,-.349,z),(.080,.009,.008),DARKMETAL,'metal','gate')
    wall(0,.414,.95,.32)
    wall(-.476,0,.83,.32,'y');wall(.476,0,.83,.32,'y')
    for x in (-.46,.46):
        for y in (-.401,.401):
            # Four masonry corner towers with walkable recessed roofs.
            for row in range(12):
                z=.024+(row+.5)*.039
                for axis in ('x','y'):
                    for side in (-1,1):
                        for j in range(3):
                            d=(j-1)*.054;col=tuple(v*rng.uniform(.91,1.10) for v in STONE)
                            m.box((x+d,y+side*.078,z) if axis=='x' else (x+side*.078,y+d,z),(.052,.032,.037) if axis=='x' else (.032,.052,.037),col)
            m.box((x,y,.489),(.194,.194,.022),LIGHTSTONE)
            m.box((x,y,.491),(.124,.124,.010),DARKSTONE)
            for side in (-1,1):
                for t in (-1,0,1):
                    m.box((x+t*.071,y+side*.077,.527),(.039,.038,.056),LIGHTSTONE)
                    if t==0:m.box((x+side*.077,y,.527),(.038,.039,.056),LIGHTSTONE)
            # Arrow slit with raised stone jambs on the front face.
            m.box((x,y-.096,.341),(.009,.002,.055),(.060,.045,.025))
    # Keep positioned at the rear of the courtyard, with strong flat crenellations.
    for row in range(18):
        z=.024+(row+.5)*.037
        for side in (-1,1):
            for i in range(5):
                k=rng.uniform(.91,1.12)
                m.box((.07+(i-2)*.073,.168+side*.164,z),(.071,.035,.035),tuple(v*k for v in LIGHTSTONE))
            for i in range(4):m.box((.07+side*.177,.168+(i-1.5)*.082,z),(.033,.080,.035),STONE)
    m.box((.07,.168,.702),(.430,.386,.028),LIGHTSTONE)
    m.box((.07,.168,.718),(.340,.29,.008),DARKSTONE)
    for side in (-1,1):
        for j in range(5):m.box((.07+(j-2)*.090,.168+side*.170,.755),(.045,.045,.080),LIGHTSTONE)
        for j in range(3):m.box((.07+side*.19,.168+(j-1)*.110,.755),(.045,.045,.080),LIGHTSTONE)
    # Corbels, slit windows, entrance steps and iron-barred doorway.
    for x in (-.08,.07,.22):
        m.box((x,-.017,.56),(.019,.004,.069),(.055,.040,.025))
        for dx in (-.016,.016):m.box((x+dx,-.021,.56),(.008,.008,.078),STONE)
    for j in range(6):m.box((.07,-.047-j*.026,.027+j*.009),(.115,.030,.020),STONE)
    m.box((.07,-.019,.112),(.083,.006,.137),WOOD,'wood','gate')
    for j in range(5):m.box((.038+j*.016,-.024,.112),(.002,.003,.135),DARKMETAL,'metal','gate')
    for x in (-.067,.207):m.rod((x,-.09,.438),(x,-.09,.311),.003,WOOD,'wood','bannerpole',6)
    m.cloth((.070,-.095,.444),.105,.116,RED,'banner')
    castle_glyph(m,.070,-.11,.382,.055)
    # Courtyard paving and a small tiled service roof.
    for x in range(9):
        for y in range(5):m.box((-.30+x*.072,-.29+y*.060,.030),(.069,.057,.009),tuple(v*rng.uniform(.9,1.1) for v in DARKSTONE))
    m.box((-.29,.14,.112),(.18,.32,.17),STONE)
    for side in (-1,1):
        for row in range(4):
            for col in range(8):m.box((-.29+side*(row+.5)*.025,-.014+col*.043,.255-row*.012),(.029,.041,.011),(.54+rng.random()*.10,.15,.045),'stone',rotate=0)
    return m

def army_marker():
    m=Mesh()
    for x,role in ((-.097,'lancero'),(.097,'espadachin')):m.append(person(role), (x,0,0),.74)
    m.rod((0,.035,.008),(0,.035,.546),.0045,WOOD,'wood','standard',8)
    m.cloth((-.004,.032,.527),.137,.137,BLUE,'standard_cloth',False)
    castle_glyph(m,.064,.010,.449,.060)
    m.ellipsoid((0,.035,.551),(.008,.008,.009),GOLD,'metal','standard',8,5)
    return m

def bake(obj,resolution):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    obj.data.uv_layers.new(name='UV_Atlas')
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.004)
    bpy.ops.object.mode_set(mode='OBJECT')
    scene.render.engine='CYCLES';scene.cycles.samples=12;scene.render.bake.margin=4
    images={};textures=EXPORT/'Textures';textures.mkdir(exist_ok=True)
    stem=obj.name.removeprefix('SM_').removesuffix('_LOD0')
    for channel in ('BaseColor','Normal','Roughness','Metallic'):
        image=bpy.data.images.new('T_'+stem+'_'+channel,resolution,resolution,alpha=False)
        if channel!='BaseColor':image.colorspace_settings.name='Non-Color'
        for mat in obj.data.materials:
            tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;mat.node_tree.nodes.active=tex
        if channel=='Metallic':
            saved=[]
            for mat in obj.data.materials:
                nodes,links=mat.node_tree.nodes,mat.node_tree.links
                shader=nodes.get('Principled BSDF');output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
                old=output.inputs['Surface'].links[0].from_socket
                emit=nodes.new('ShaderNodeEmission');value=shader.inputs['Metallic'].default_value
                emit.inputs['Color'].default_value=(value,value,value,1);links.new(emit.outputs[0],output.inputs['Surface'])
                saved.append((mat,output,old,emit))
            bpy.ops.object.bake(type='EMIT',use_clear=True)
            for mat,output,old,emit in saved:mat.node_tree.links.new(old,output.inputs['Surface']);mat.node_tree.nodes.remove(emit)
        elif channel=='BaseColor':bpy.ops.object.bake(type='DIFFUSE',pass_filter={'COLOR'},use_clear=True)
        elif channel=='Normal':bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT',use_clear=True)
        else:bpy.ops.object.bake(type='ROUGHNESS',use_clear=True)
        image.filepath_raw=str(textures/(image.name+'.png'));image.file_format='PNG';image.save();image.pack();images[channel]=image
    mat=material('M_'+stem,(.3,.3,.3));nodes,links=mat.node_tree.nodes,mat.node_tree.links;shader=nodes.get('Principled BSDF')
    for channel,socket in [('BaseColor','Base Color'),('Roughness','Roughness'),('Metallic','Metallic')]:
        tex=nodes.new('ShaderNodeTexImage');tex.image=images[channel];links.new(tex.outputs['Color'],shader.inputs[socket])
    tex=nodes.new('ShaderNodeTexImage');tex.image=images['Normal'];normal=nodes.new('ShaderNodeNormalMap')
    links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
    obj.data.materials.clear();obj.data.materials.append(mat)
    for polygon in obj.data.polygons:polygon.material_index=0
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(obj.name+'.fbx')),use_selection=True,object_types={'MESH'},global_scale=1,
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_triangles=True,path_mode='RELATIVE')
    obj.data.calc_loop_triangles();points=[v.co for v in obj.data.vertices]
    bounds=[[min(p[i] for p in points),max(p[i] for p in points)] for i in range(3)]
    assert bounds[2][0]>=-.0001 and tuple(obj.location)==(0,0,0) and tuple(obj.scale)==(1,1,1)
    assert max(abs(p.x) for p in points)<.68 and max(abs(p.y) for p in points)<.68
    return dict(name=obj.name,triangles=len(obj.data.loop_triangles),vertices=len(points),materials=1,uv_channels=1,bounds=bounds,
        origin=list(obj.location),scale=list(obj.scale),texture_resolution=resolution,texture_maps=list(images),pose='static',rigged=False)

if FAMILY=='Fortaleza':specs=[('FortalezaCastellana_A',fortress(),2048)]
elif FAMILY=='Tropas':specs=[('Lancero_A',person('lancero'),1024),('Espadachin_A',person('espadachin'),1024),('Ballestero_A',person('ballestero',BLUE),1024),('CaballeroMontado_A',mounted_knight(),1024)]
elif FAMILY=='Heroes':specs=[('HeroeCapitan_A',person('capitan',BLUE,True),1024),('HeroinaSoberana_A',person('soberana',RED,True,True),1024)]
else:specs=[('HuesteEstandarte_A',army_marker(),1024)]

models=[];reports=[]
sources=bpy.data.collections.new('FUENTES_EDITABLES');scene.collection.children.link(sources)
for name,builder,resolution in specs:
    obj=builder.object('SRC_'+name)
    for collection in list(obj.users_collection):collection.objects.unlink(obj)
    sources.objects.link(obj);obj.hide_render=True;obj.hide_set(True)
    export=obj.copy();export.data=obj.data.copy();export.name='SM_'+name+'_LOD0';scene.collection.objects.link(export)
    export.hide_render=False;export.hide_set(False)
    report=bake(export,resolution);reports.append(report);models.append(export)
    export.use_fake_user=True
    for collection in list(export.users_collection):collection.objects.unlink(export)
    print('REPERTORIO_MODEL_READY',export.name,report['triangles'],flush=True)
sources.hide_render=True;sources.hide_viewport=True

def display_model(obj,x,y,scale=1):
    copy=obj.copy();copy.location=(x,y,0);copy.scale=(scale,)*3;scene.collection.objects.link(copy);return copy

gold=(.72,.45,.16)
if FAMILY=='Fortaleza':
    display_model(models[0],0,0)
    camera=stage(scene,(0,0,.34),1.85,PREVIEW/'Fortaleza_detalle_v01.png')
    scene.render.resolution_y=1450
    caption(camera,'FULGOR MEDIEVAL / ARQUITECTURA',-.83,.76,.040,gold)
    caption(camera,'FORTALEZA CASTELLANA',-.83,.63,.077)
    caption(camera,'Piedra calida / Puerta abierta / Torre del homenaje',-.83,-.71,.031,gold)
elif FAMILY=='Tropas':
    for i,obj in enumerate(models):display_model(obj,(i-1.5)*.42,0)
    camera=stage(scene,(0,0,.25),2.0,PREVIEW/'Tropas_galeria_v01.png')
    camera.location=(0,-3,1.7);camera.rotation_euler=(Vector((0,0,.25))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1100
    caption(camera,'FULGOR MEDIEVAL / HUESTES',-.91,.58,.043,gold)
    caption(camera,'CUATRO SILUETAS DE COMBATE',-.91,.46,.075)
    for i,label in enumerate(['LANCERO','ESPADACHIN','BALLESTERO','CABALLERO']):caption(camera,label,(i-1.5)*.42,-.34,.033,align='CENTER')
    caption(camera,'Modelos propios / Primera pose estatica / Rig y animacion pendientes',-.91,-.58,.030,gold)
elif FAMILY=='Heroes':
    for i,obj in enumerate(models):display_model(obj,(i-.5)*.38,0)
    camera=stage(scene,(0,0,.22),1.13,PREVIEW/'Heroes_detalle_v01.png')
    camera.location=(0,-3,1.6);camera.rotation_euler=(Vector((0,0,.22))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1300
    caption(camera,'FULGOR MEDIEVAL / HEROES',-.50,.40,.028,gold)
    caption(camera,'CAPITAN Y SOBERANA',-.50,.32,.049)
    caption(camera,'CAPITAN',-.19,-.30,.027,align='CENTER');caption(camera,'SOBERANA',.19,-.30,.027,align='CENTER')
    caption(camera,'Arquetipos originales / Identidad historica y rig pendientes',-.50,-.40,.018,gold)
else:
    display_model(models[0],0,0)
    camera=stage(scene,(0,0,.25),1.10,PREVIEW/'Hueste_detalle_v01.png')
    camera.location=(.7,-3,1.7);camera.rotation_euler=(Vector((0,0,.25))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_y=1300
    caption(camera,'FULGOR MEDIEVAL / MAPA',-.42,.34,.023,gold)
    caption(camera,'HUESTE CON ESTANDARTE',-.42,.27,.036)
    caption(camera,'Marcador estrategico / Pose estatica',-.42,-.34,.021,gold)
target=Vector((0,0,.34 if FAMILY=='Fortaleza' else .29 if FAMILY=='Hueste' else .25))
camera.location=target+(camera.location-target).normalized()*11
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.data.objects['PREVIEW_Suelo'].location.z=-.0005
scene.cycles.samples=64
bpy.ops.render.render(write_still=True)

# Additional oblique view of the castle and a small strategic-scale sheet.
if FAMILY=='Fortaleza':
    target=Vector((0,0,.32));camera.location=target+Vector((2.5,-4,2.38)).normalized()*11
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(PREVIEW/'Fortaleza_perspectiva_v01.png');bpy.ops.render.render(write_still=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(FAMILY+'_v01.blend')))
(EXPORT/'repertorio_manifest.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('FULGOR_REPERTORIO_COMPLETE',FAMILY,len(models),flush=True)

