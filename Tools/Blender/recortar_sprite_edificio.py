"""Recorta la imagen de un edificio sobre fondo negro y la deja con transparencia para usarla como sprite.

Uso: blender -b --python recortar_sprite_edificio.py -- entrada.(png|webp|jpg) salida.png [umbral]

El fondo se toma solo si es oscuro y está conectado al borde de la imagen, así que las sombras
interiores del edificio no se vacían. El borde se suaviza y se le quita el halo negro.
"""
import sys
import bpy
import numpy as np

args = sys.argv[sys.argv.index('--') + 1:]
src, dst = args[0], args[1]
umbral = float(args[2]) if len(args) > 2 else 0.075

im = bpy.data.images.load(src)
w, h = im.size
px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[..., :3]
brillo = px.max(axis=2)

# fondo: píxeles oscuros conectados al borde (dilatación iterativa, sin scipy)
oscuro = brillo < umbral
fondo = np.zeros_like(oscuro)
fondo[0, :] = oscuro[0, :]; fondo[-1, :] = oscuro[-1, :]; fondo[:, 0] = oscuro[:, 0]; fondo[:, -1] = oscuro[:, -1]
while True:
    d = fondo.copy()
    d[1:] |= fondo[:-1]; d[:-1] |= fondo[1:]; d[:, 1:] |= fondo[:, :-1]; d[:, :-1] |= fondo[:, 1:]
    d &= oscuro
    if (d == fondo).all():
        break
    fondo = d

# alfa: 0 en el fondo; en la franja de 2 px junto al fondo, proporcional al brillo (antialias del original)
alfa = (~fondo).astype(np.float32)
cerca = np.zeros_like(fondo)
f = fondo
for _ in range(2):
    g = f.copy(); g[1:] |= f[:-1]; g[:-1] |= f[1:]; g[:, 1:] |= f[:, :-1]; g[:, :-1] |= f[:, 1:]
    f = g
cerca = f & ~fondo
alfa[cerca] = np.clip(brillo[cerca] / (umbral * 2.5), 0, 1)
# quitar el halo: el color del borde viene premultiplicado por el negro del fondo
rgb = px.copy()
m = cerca & (alfa > 0.05)
rgb[m] = np.clip(px[m] / alfa[m, None], 0, 1)

# recorte al contenido con 8 px de margen
ys, xs = np.nonzero(alfa > 0)
y0, y1 = max(ys.min() - 8, 0), min(ys.max() + 9, h)
x0, x1 = max(xs.min() - 8, 0), min(xs.max() + 9, w)
out = np.dstack([rgb, alfa])[y0:y1, x0:x1]
oh, ow = out.shape[:2]

res = bpy.data.images.new('sprite', ow, oh, alpha=True)
res.alpha_mode = 'STRAIGHT'
res.pixels.foreach_set(out.ravel())
res.filepath_raw = dst
res.file_format = 'PNG'
res.save()
print('SPRITE_OK', ow, oh, 'fondo %.1f%%' % (100 * fondo.mean()), 'recorte', x0, y0, x1, y1)
