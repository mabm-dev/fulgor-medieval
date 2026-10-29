from pathlib import Path
import cv2,numpy as np,json,hashlib
from PIL import Image,ImageFilter
ROOT=next(parent for parent in Path(__file__).resolve().parents if (parent / 'Assets').is_dir() and (parent / 'ProjectSettings').is_dir())
SOURCE=ROOT/'ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png'
BASE=ROOT/'ArtSource/Concept/CastillaRodrigoV02/Rodrigo_retoque_cuello_v01.png'
OUT=Path(__file__).resolve().parent
source=np.array(Image.open(SOURCE).convert('RGB'));base=np.array(Image.open(BASE).convert('RGB'))
# Traced points on the front face of the canonical clasp, not its sidewall.
sp=np.array([(368,149),(399,180),(382,292),(328,410),(255,497),(198,523),(161,490),(153,429),(184,324),(239,225),(307,163)],np.float32)/6+np.array([680,355],np.float32)
# Front face of the target clasp, inside its existing bronze mounting rim.
tp=np.array([(193,125),(288,140),(395,221),(459,328),(474,426),(423,493),(337,512),(234,467),(145,380),(104,284),(114,191)],np.float32)/3+np.array([560,1130],np.float32)
es=cv2.fitEllipse(sp.reshape(-1,1,2));et=((657.0,1238.5),(116.0,157.0),137.98)
def basis(e):
 (cx,cy),(a,b),angle=e
 t=np.deg2rad(angle);minor=np.array([np.cos(t),np.sin(t)])*(a/2)
 major=np.array([-np.sin(t),np.cos(t)])*(b/2)
 if major[1]>0:major=-major
 if minor[0]<0:minor=-minor
 return np.array([cx,cy]),np.stack([minor,major],axis=1)
cs,bs=basis(es);ct,bt=basis(et)
A=bt@np.linalg.inv(bs);M=np.column_stack([A,ct-A@cs]).astype(np.float32)
warped=cv2.warpAffine(source,M,(base.shape[1],base.shape[0]),flags=cv2.INTER_CUBIC,borderMode=cv2.BORDER_REFLECT)
# Integrate the bright source metal into the softer portrait light without
# synthesizing details or changing any relief shapes.
warped=np.clip(warped.astype(np.float32)*.86,0,255).astype(np.uint8)
mask=np.zeros(base.shape[:2],np.uint8)
cv2.ellipse(mask,et,255,-1,lineType=cv2.LINE_AA)
mask=np.array(Image.fromarray(mask).filter(ImageFilter.GaussianBlur(.7)))
alpha=mask[:,:,None].astype(np.float32)/255
result=np.round(base*(1-alpha)+warped*alpha).astype(np.uint8)
assert np.array_equal(base[mask==0],result[mask==0])
path=OUT/'Rodrigo_broche_original_compuesto_v03.png';Image.fromarray(result).save(path)
Image.fromarray(result).crop((560,1130,760,1350)).resize((600,660)).save(OUT/'broche_compuesto_detalle.png')
report={'method':'Deterministic affine transfer of original brooch face, no generative AI',
 'source':str(SOURCE),'base':str(BASE),'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
 'base_sha256':hashlib.sha256(BASE.read_bytes()).hexdigest(),'source_ellipse':es,'target_ellipse':et,
 'affine_transform':M.tolist(),'exposure_multiplier':.86,'edge_feather_px':.7,
 'pixels_outside_composite_mask_identical':True,'dimensions':[base.shape[1],base.shape[0]],
 'limitation':'Original clasp occupies only about 40 x 65 source pixels; enlargement cannot recover absent relief detail.'}
(OUT/'Rodrigo_broche_original_compuesto_v03.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report))
