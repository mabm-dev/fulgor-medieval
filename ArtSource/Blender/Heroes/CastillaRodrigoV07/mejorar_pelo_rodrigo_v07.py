"""Rebuild only Rodrigo's hair: fine, varied groom with physical hair shader."""
from pathlib import Path
import bpy,math,random,json,hashlib,struct
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from math import sin,cos,pi
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
BASE=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV06/Rodrigo_escultura_v06.blend'
DEST=ROOT/'ArtSource/Blender/Heroes/CastillaRodrigoV07';DEST.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'Builds/ArtPreview/RodrigoCastillaV07';OUT.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
base_sha=sha(BASE)
bpy.ops.wm.open_mainfile(filepath=str(BASE))
scene=bpy.context.scene;hero=bpy.data.collections['RODRIGO_GEOMETRIA_REAL']
head=bpy.data.objects['Cabeza_anatomica_CC0_adaptada']
def signature(ob):
 h=hashlib.sha256()
 def floats(v):
  v=list(v);h.update(struct.pack('<%df'%len(v),*v))
 floats(q for row in ob.matrix_world for q in row)
 if ob.type=='MESH':
  for v in ob.data.vertices:floats(v.co)
  for p in ob.data.polygons:h.update(struct.pack('<%dI'%len(p.vertices),*p.vertices))
  for uv in ob.data.uv_layers:
   for v in uv.data:floats(v.uv)
  for attr in ob.data.color_attributes:
   for v in attr.data:floats(v.color)
 elif ob.type=='CURVE':
  floats([ob.data.bevel_depth,ob.data.bevel_resolution])
  for sp in ob.data.splines:
   for p in sp.points:floats([*p.co,p.radius])
 elif ob.type=='CAMERA':floats([ob.data.ortho_scale,ob.data.lens])
 elif ob.type=='LIGHT':floats([ob.data.energy,*ob.data.color,ob.data.size])
 if ob.type in {'MESH','CURVE'}:h.update('|'.join(m.name for m in ob.data.materials).encode())
 return h.hexdigest()
bpy.context.view_layer.update()
hair_old=[o for o in hero.objects if o.name.startswith(('Cabello_fibras','Cabello_masa_anatomica'))]
protected={o.name:signature(o) for o in bpy.data.objects if o not in hair_old}
old_count=sum(len(o.data.splines) for o in hair_old if o.type=='CURVE')
print('PROTECTED',len(protected),'OLD_HAIR',old_count,flush=True)
deps=bpy.context.evaluated_depsgraph_get();evaluated=head.evaluated_get(deps);mesh=evaluated.to_mesh()
vertices=[head.matrix_world@v.co for v in mesh.vertices]
bvh=BVHTree.FromPolygons(vertices,[tuple(p.vertices) for p in mesh.polygons]);evaluated.to_mesh_clear()
eyes=[o for o in hero.objects if o.name.startswith('Globo_ocular_anatomico')]
eye_z=sum(o.matrix_world.translation.z for o in eyes)/len(eyes);center=Vector((0,.015,eye_z+.005))
def minimum(a):return (.58 if cos(a)>0 else .88)*cos(a)-.10*sin(a)**2+.035*sin(5*a)+.010*sin(11*a+.8)
def surface(a,t):
 phi=minimum(a)+(pi/2-minimum(a))*t
 d=Vector((sin(a)*cos(phi),-cos(a)*cos(phi),sin(phi)))
 p,n,_,_=bvh.ray_cast(center,d,1)
 if p is None:raise RuntimeError('Scalp ray missed anatomical head')
 return p,n
# Cache an anatomical scalp grid so every strand follows the same real head.
NA=256;NT=144;TMIN=-.065;TMAX=.999
grid=[]
for j in range(NT+1):
 grid.append([surface(2*pi*i/NA,TMIN+(TMAX-TMIN)*j/NT) for i in range(NA)])
def scalp(a,t,extra=0):
 ai=(a%(2*pi))*NA/(2*pi);i=int(ai);u=ai-i
 tt=max(0,min(NT-.00001,(t-TMIN)/(TMAX-TMIN)*NT));j=int(tt);v=tt-j
 p=Vector();n=Vector()
 for ii,jj,w in [(i,j,(1-u)*(1-v)),((i+1)%NA,j,u*(1-v)),(i,j+1,(1-u)*v),((i+1)%NA,j+1,u*v)]:
  pp,nn=grid[jj][ii];p+=pp*w;n+=nn*w
 n.normalize()
 fade=max(0,min(1,t*9));shell=.0011+.0022*fade
 return p+n*(shell+extra),n
for ob in hair_old:bpy.data.objects.remove(ob,do_unlink=True)
# A shallow, dark underlayer sits close to the skull rather than forming
# the previous inflated helmet. Fine fibers cover its visible surface.
verts=[];faces=[];N=160;J=62
for j in range(J):
 for i in range(N):
  p,_=scalp(2*pi*i/N,.012+.988*j/(J-1));verts.append(p)
for j in range(J-1):
 for i in range(N):
  q=j*N+i;b=j*N+(i+1)%N;faces.append((q,b,b+N,q+N))
data=bpy.data.meshes.new('Pelo_subcapa_V07');data.from_pydata(verts,[],faces);data.update()
cap=bpy.data.objects.new('Pelo_subcapa_V07',data);hero.objects.link(cap)
capmat=bpy.data.materials.new('Pelo_base_oscura_V07');capmat.use_nodes=True
bsdf=capmat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(.009,.0045,.0027,1)
bsdf.inputs['Roughness'].default_value=.86;bsdf.inputs['Specular IOR Level'].default_value=.10
# Feather the underlayer into individual hair roots; avoid either a hard cap
# edge or a bright exposed scalp stripe between the independently placed locks.
coverage=data.color_attributes.new(name='RootCoverage',type='FLOAT_COLOR',domain='CORNER')
for loop in data.loops:
 t=.012+.988*(loop.vertex_index//N)/(J-1);q=max(0,min(1,(t-.012)/.10));q=q*q*(3-2*q)
 coverage.data[loop.index].color=(q,q,q,1)
nodes=capmat.node_tree.nodes;links=capmat.node_tree.links
attribute=nodes.new('ShaderNodeVertexColor');attribute.layer_name='RootCoverage'
transparent=nodes.new('ShaderNodeBsdfTransparent');mix=nodes.new('ShaderNodeMixShader')
links.new(attribute.outputs['Color'],mix.inputs[0]);links.new(transparent.outputs[0],mix.inputs[1]);links.new(bsdf.outputs[0],mix.inputs[2])
links.new(mix.outputs[0],nodes.get('Material Output').inputs['Surface'])
data.materials.append(capmat)
for p in data.polygons:p.use_smooth=True
# Separate materials keep beard and eyebrow shaders entirely untouched.
grooms=[]
for k,color in enumerate([(.021,.010,.0055,1),(.029,.014,.007,1),(.040,.020,.011,1)]):
 mat=bpy.data.materials.new('Pelo_fisico_castano_V07_'+str(k));mat.use_nodes=True
 nodes=mat.node_tree.nodes;nodes.clear();output=nodes.new('ShaderNodeOutputMaterial')
 hair=nodes.new('ShaderNodeBsdfHairPrincipled');hair.parametrization='MELANIN'
 hair.inputs['Melanin'].default_value=(.96,.92,.86)[k]
 hair.inputs['Melanin Redness'].default_value=.18
 hair.inputs['Random Color'].default_value=.12
 hair.inputs['Roughness'].default_value=.46
 if 'Radial Roughness' in hair.inputs:hair.inputs['Radial Roughness'].default_value=.54
 if 'Random Roughness' in hair.inputs:hair.inputs['Random Roughness'].default_value=.25
 if 'Coat' in hair.inputs:hair.inputs['Coat'].default_value=0
 mat.node_tree.links.new(hair.outputs[0],output.inputs['Surface'])
 cu=bpy.data.curves.new('Pelo_fibras_V07_'+str(k),'CURVE');cu.dimensions='3D';cu.resolution_u=1
 cu.bevel_depth=.000045;cu.bevel_resolution=1;cu.materials.append(mat)
 ob=bpy.data.objects.new(cu.name,cu);hero.objects.link(ob);grooms.append(ob)
random.seed(73021);strand_count=0;nape_min=1.590;cut_nape=0
def strand(coords,width=1):
 global strand_count,cut_nape
 # Keep the cleaned V04/V06 nape; no long straight tails on the neck.
 clipped=[]
 for p in coords:
  if p.y>.014 and p.z<nape_min:
   cut_nape+=1;break
  clipped.append(p)
 if len(clipped)<4:return
 cu=random.choices(grooms,[.58,.34,.08])[0].data
 sp=cu.splines.new('POLY');sp.points.add(len(clipped)-1)
 for j,(point,p) in enumerate(zip(sp.points,clipped)):
  q=j/(len(clipped)-1);point.co=(*p,1)
  point.radius=width*(.55+.45*sin(pi*q*.85))*(1-q**5)*.95+.015
 strand_count+=1
# Broad, overlapping guide families, with independent strand phases and
# roots, avoid the visibly identical parallel bundles of the previous groom.
for guide in range(280):
 a0=random.uniform(-pi,pi);phase=random.uniform(0,2*pi)
 if cos(a0)>.30:
  start=random.uniform(-.045,.28);end=random.uniform(.63,.98)
  sweep=(.48 if a0>-.25 else -.34)*random.uniform(.65,1.25)
  if guide%13==0:start,end=random.uniform(.45,.75),random.uniform(.01,.13)
 else:
  start=random.uniform(.42,.95);end=random.uniform(-.045,.12)
  sweep=(.22 if a0>0 else -.22)*random.uniform(.65,1.2)
 height=random.uniform(.008,.023)
 for child in range(85):
  da=random.gauss(0,.035);dt=random.gauss(0,.024);ph=phase+random.uniform(-.6,.6)
  coords=[];offset=random.uniform(.0002,.0023);wid=random.uniform(.65,1.08)
  for j in range(30):
   q=j/29;t=start*(1-q)+(end+dt)*q+.025*sin(q*7+ph)*sin(pi*q)
   aa=a0+da+sweep*q+.080*sin(q*9+ph)*sin(pi*q)
   lift=offset+height*sin(pi*q)**1.4+.0006*sin(q*15+ph)*sin(pi*q)
   p,_=scalp(aa,t,lift);p.z-=.0016*q*q;coords.append(p)
  strand(coords,wid)
print('LONG_GUIDE_STRANDS',strand_count,flush=True)
# Dense, independently sampled shorter fibers break up the root bands and
# cover the shell without introducing another continuous solid hair mass.
for k in range(15000):
 a=random.uniform(-pi,pi);t0=random.uniform(-.015,.985);ph=random.uniform(0,2*pi)
 length=random.uniform(.10,.30);direction=1 if cos(a)>.30 else -1;coords=[]
 for j in range(13):
  q=j/12;t=max(-.04,min(.999,t0+direction*length*q))
  aa=a+(.12 if a>-.25 else -.12)*q+.012*sin(ph+q*9)*sin(pi*q)
  p,_=scalp(aa,t,.0004+random.uniform(.0002,.0007)+.0015*sin(pi*q));coords.append(p)
 strand(coords,random.uniform(.50,.82))
# Loose frontal locks flow from the upper scalp toward the hairline, with
# out-of-surface curvature rather than all being combed flat onto the cap.
for guide in range(65):
 a=random.uniform(-1.2,1.2);start=random.uniform(.36,.72);end=random.uniform(-.045,.09)
 phase=random.uniform(0,2*pi);height=random.uniform(.008,.018)
 sweep=(.36 if a>-.25 else -.30)*random.uniform(.7,1.3)
 for child in range(65):
  da=random.gauss(0,.015);dt=random.gauss(0,.013);coords=[]
  for j in range(34):
   q=j/33;t=start*(1-q)+(end+dt)*q+.020*sin(q*8+phase)*sin(pi*q)
   aa=a+da+sweep*q+.10*sin(q*7+phase)*sin(pi*q)
   p,_=scalp(aa,t,.0015+height*sin(pi*q)**1.2);p.z-=.002*q*q;coords.append(p)
  strand(coords,random.uniform(.70,1.05))
# Sparse silhouette flyaways remain fine and shorter than the main locks.
for k in range(850):
 a=random.uniform(-pi,pi);t0=random.uniform(.04,.94);ph=random.uniform(0,2*pi)
 direction=1 if cos(a)>.30 else -1;coords=[]
 for j in range(20):
  q=j/19;t=max(-.04,min(.999,t0+direction*.28*q))
  p,n=scalp(a+.18*q+.012*sin(ph+q*8),t,.0004+.004*sin(pi*q)**2)
  coords.append(p)
 strand(coords,.42)
# Cycles' hair BSDF needs native strand primitives. Beveled legacy curves
# become tubes and do not provide the same physical hair tangent semantics.
# Retain authored positions/radii while switching only the hair container.
native_grooms=[]
for ob in grooms:
 sizes=[];positions=[];radii=[]
 for sp in ob.data.splines:
  sizes.append(len(sp.points))
  for p in sp.points:
   positions.extend(p.co[:3]);radii.append(ob.data.bevel_depth*p.radius)
 native=bpy.data.hair_curves.new(ob.name+'_nativo');native.add_curves(sizes)
 native.attributes['position'].data.foreach_set('vector',positions)
 radius=native.attributes.new('radius','FLOAT','POINT');radius.data.foreach_set('value',radii)
 native.materials.append(ob.data.materials[0])
 neo=bpy.data.objects.new(ob.name+'_nativo',native);hero.objects.link(neo);native_grooms.append(neo)
 bpy.data.objects.remove(ob,do_unlink=True)
grooms=native_grooms
for name,sig in protected.items():assert signature(bpy.data.objects[name])==sig,name
assert sha(BASE)==base_sha
scene.name='Rodrigo_pelo_fino_organico_V07';scene['revision']='V07 hair-only rebuild; V06 anatomy, beard, eyes, costume, cameras and lighting preserved'
scene.render.engine='CYCLES';scene.cycles.samples=96;scene.cycles.use_denoising=True
scene.camera=bpy.data.objects['CAM_3Q']
manifest={'revision':'V07 hair-only','base_v06_sha256':base_sha,'protected_objects':protected,
 'previous_strands':old_count,'new_strands':strand_count,'strand_max_radius_m':.000045,
 'material':'Native CURVES; Principled Hair BSDF, melanin .96/.92/.86, roughness .46, radial .54, coat 0; feathered root underlayer',
 'nape_limit_z_m':nape_min,'clipped_nape_samples':cut_nape,'game_ready':False,
 'render_samples':96,'artistic_acceptance':'pending'}
(DEST/'hair_manifest_v07.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(DEST/'Rodrigo_escultura_v07.blend'))
for label,cam in [('Rostro','CAM_Rostro'),('Perfil_cuello','CAM_Perfil_Cuello'),('Espalda','CAM_Espalda')]:
 scene.camera=bpy.data.objects[cam];scene.render.filepath=str(OUT/(label+'_v07.png'))
 bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['CAM_3Q'];bpy.ops.wm.save_as_mainfile(filepath=str(DEST/'Rodrigo_escultura_v07.blend'))
assert sha(BASE)==base_sha
print('RODRIGO_V07_HAIR_OK',strand_count,'protected',len(protected),flush=True)
