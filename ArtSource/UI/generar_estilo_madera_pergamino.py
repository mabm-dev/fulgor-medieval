"""Genera las texturas de interfaz «madera y pergamino» de Fulgor Medieval.

Uso (desde la raíz del repositorio, requiere Pillow y NumPy):

    python3 ArtSource/UI/generar_estilo_madera_pergamino.py

Escribe PNG en Assets/Fulgor/Resources/LegacyUI/Estilo/. Todo es procedural y
determinista (semilla fija): madera oscura, pergamino con herrajes de hierro,
botones de madera con filo de latón, estandarte de tela azul, marco de piedra,
y medallón para iconos. Solo interfaz: no afecta al mapa ni a sus recursos. Cualquier
textura puede sustituirse por arte pintado con el mismo nombre y tamaño.
"""

import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SALIDA = os.path.join(RAIZ, "Assets", "Fulgor", "Resources", "LegacyUI", "Estilo")
GEN = np.random.default_rng(1212)

LATON = (196, 156, 72)
LATON_CLARO = (238, 206, 128)
HIERRO = (44, 38, 34)
TINTA = (24, 15, 8)


def ruido(alto, ancho, escala_y, escala_x):
    """Ruido periódico (se repite sin costuras) filtrado en frecuencia."""
    blanco = GEN.standard_normal((alto, ancho))
    fy = np.fft.fftfreq(alto)[:, None]
    fx = np.fft.fftfreq(ancho)[None, :]
    filtro = np.exp(-((fy * escala_y) ** 2 + (fx * escala_x) ** 2))
    campo = np.real(np.fft.ifft2(np.fft.fft2(blanco) * filtro))
    campo -= campo.min()
    return campo / max(1e-9, campo.max())


def colorear(campo, oscuro, claro):
    oscuro = np.array(oscuro, dtype=float)
    claro = np.array(claro, dtype=float)
    rgb = oscuro + (claro - oscuro) * campo[..., None]
    return np.clip(rgb, 0, 255)


def a_imagen(rgb, alfa=None):
    rgb = np.clip(rgb, 0, 255).astype(np.uint8)
    if alfa is None:
        alfa = np.full(rgb.shape[:2], 255, np.uint8)
    return Image.fromarray(np.dstack([rgb, np.clip(alfa, 0, 255).astype(np.uint8)]), "RGBA")


def madera(alto, ancho, oscuro=(30, 18, 10), claro=(84, 54, 32), tablas=8):
    veta = ruido(alto, ancho, 2.0, 180.0)
    fibra = ruido(alto, ancho, 0.6, 40.0)
    campo = 0.65 * veta + 0.35 * fibra
    campo = 0.5 + 0.5 * np.sin(campo * 18.0)
    rgb = colorear(campo * 0.55 + fibra * 0.45, oscuro, claro)
    alto_tabla = alto // tablas
    for t in range(tablas):
        y = t * alto_tabla
        tono = GEN.uniform(0.82, 1.08)
        rgb[y:y + alto_tabla] *= tono
        rgb[y:y + 2] *= 0.45
        rgb[y + 2:y + 3] *= 1.15
    return rgb


def pergamino_rgb(alto, ancho):
    fibra = ruido(alto, ancho, 1.5, 3.0)
    manchas = ruido(alto, ancho, 30.0, 30.0)
    campo = 0.55 * fibra + 0.45 * manchas
    rgb = colorear(campo, (196, 168, 118), (240, 224, 186))
    yy, xx = np.mgrid[0:alto, 0:ancho]
    borde = np.minimum.reduce([xx, yy, ancho - 1 - xx, alto - 1 - yy]).astype(float)
    quemado = np.clip(1 - borde / (min(alto, ancho) * 0.22), 0, 1) ** 1.6
    rgb *= (1 - 0.45 * quemado)[..., None]
    rgb[..., 2] *= (1 - 0.15 * quemado)
    return rgb


def bisel(imagen, caja, ancho, claro, oscuro):
    dibujo = ImageDraw.Draw(imagen)
    x0, y0, x1, y1 = caja
    for i in range(ancho):
        dibujo.line([(x0 + i, y1 - i), (x0 + i, y0 + i), (x1 - i, y0 + i)], fill=claro)
        dibujo.line([(x1 - i, y0 + i), (x1 - i, y1 - i), (x0 + i, y1 - i)], fill=oscuro)


def herraje(imagen, x, y, tamano, giro):
    """Escuadra de hierro con remache dorado en una esquina."""
    capa = Image.new("RGBA", (tamano, tamano), (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    g = int(tamano / 3.2)
    d.polygon([(0, 0), (tamano, 0), (tamano - g, g), (g, g), (g, tamano - g), (0, tamano)], fill=HIERRO + (255,))
    d.line([(1, 1), (tamano - 2, 1)], fill=(96, 86, 76, 255), width=2)
    d.line([(1, 1), (1, tamano - 2)], fill=(96, 86, 76, 255), width=2)
    r = g // 2 + 1
    d.ellipse([g // 2 - r // 2, g // 2 - r // 2, g // 2 + r, g // 2 + r], fill=LATON + (255,), outline=TINTA + (255,))
    for cx, cy in ((tamano - g, g // 2), (g // 2, tamano - g)):
        d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=LATON + (255,), outline=TINTA + (255,))
    capa = capa.rotate(giro, expand=False)
    imagen.alpha_composite(capa, (x, y))


def guardar(imagen, nombre):
    imagen.save(os.path.join(SALIDA, nombre))


def tex_madera_fondo():
    guardar(a_imagen(madera(512, 512)), "madera_fondo.png")


def tex_marco_madera():
    """Panel de madera con filo de latón: barra superior, pie y navegación."""
    lado = 128
    img = a_imagen(madera(lado, lado, (34, 21, 12), (92, 60, 36), tablas=4))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, lado - 1, lado - 1], outline=(10, 6, 3, 255), width=2)
    d.rectangle([4, 4, lado - 5, lado - 5], outline=LATON + (255,), width=2)
    d.line([(6, 6), (lado - 7, 6)], fill=LATON_CLARO + (255,), width=1)
    for cx in (7, lado - 8):
        for cy in (7, lado - 8):
            d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=LATON_CLARO + (255,), outline=TINTA + (255,))
    guardar(img, "marco_madera.png")


def tex_pergamino():
    lado = 256
    img = a_imagen(pergamino_rgb(lado, lado))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, lado - 1, lado - 1], outline=(46, 28, 14, 255), width=3)
    d.rectangle([7, 7, lado - 8, lado - 8], outline=(120, 86, 48, 200), width=1)
    t = 46
    herraje(img, 0, 0, t, 0)
    herraje(img, lado - t, 0, t, -90)
    herraje(img, lado - t, lado - t, t, 180)
    herraje(img, 0, lado - t, t, 90)
    guardar(img, "pergamino.png")


def boton(nombre, oscuro, claro, filo, brillo):
    ancho, alto, k = 128, 48, 4
    rgb = madera(alto * k, ancho * k, oscuro, claro, tablas=1)
    img = a_imagen(rgb)
    mascara = Image.new("L", (ancho * k, alto * k), 0)
    ImageDraw.Draw(mascara).rounded_rectangle([0, 0, ancho * k - 1, alto * k - 1], radius=7 * k, fill=255)
    img.putalpha(mascara)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, ancho * k - 1, alto * k - 1], radius=7 * k, outline=(10, 6, 3, 255), width=2 * k)
    d.rounded_rectangle([2 * k, 2 * k, ancho * k - 1 - 2 * k, alto * k - 1 - 2 * k], radius=6 * k, outline=filo + (255,), width=2 * k)
    d.line([(9 * k, 5 * k), (ancho * k - 9 * k, 5 * k)], fill=brillo + (110,), width=k)
    guardar(img.resize((ancho, alto), Image.LANCZOS), nombre)


def tex_botones():
    boton("boton_madera.png", (46, 26, 15), (100, 62, 36), LATON, LATON_CLARO)
    boton("boton_madera_activo.png", (92, 56, 30), (156, 104, 58), LATON_CLARO, (255, 240, 200))
    boton("boton_madera_pulsado.png", (28, 16, 9), (64, 40, 22), LATON, LATON)
    boton("boton_madera_inactivo.png", (40, 36, 32), (70, 64, 58), (110, 100, 86), (140, 130, 120))


def tex_estandarte():
    ancho, alto = 256, 128
    trama = ruido(alto, ancho, 0.8, 0.8)
    pliegue = 0.5 + 0.5 * np.sin(np.linspace(0, 6 * math.pi, ancho))[None, :]
    campo = 0.6 * trama + 0.4 * pliegue
    rgb = colorear(campo, (14, 22, 40), (40, 58, 96))
    img = a_imagen(rgb)
    d = ImageDraw.Draw(img)
    for y in (6, alto - 10):
        d.rectangle([0, y, ancho, y + 3], fill=LATON + (255,))
        d.line([(0, y), (ancho, y)], fill=LATON_CLARO + (255,))
    d.rectangle([0, 0, ancho - 1, alto - 1], outline=(6, 8, 14, 255), width=2)
    guardar(img, "estandarte.png")


def tex_piedra():
    lado = 256
    base = ruido(lado, lado, 6.0, 6.0)
    rgb = colorear(base, (88, 86, 80), (156, 152, 142))
    img = a_imagen(rgb)
    d = ImageDraw.Draw(img)
    grosor = 30
    paso = 42
    # Sillares alrededor; el centro queda transparente para ver la ciudad.
    for i, x in enumerate(range(0, lado, paso)):
        for y0 in (0, lado - grosor):
            desplazado = x + (paso // 2 if y0 else 0)
            bisel(img, (desplazado, y0, desplazado + paso - 3, y0 + grosor - 3), 2, (190, 186, 176, 255), (50, 48, 44, 255))
    for y in range(grosor, lado - grosor, paso):
        for x0 in (0, lado - grosor):
            bisel(img, (x0, y, x0 + grosor - 3, y + paso - 3), 2, (190, 186, 176, 255), (50, 48, 44, 255))
    alfa = np.array(img)[..., 3]
    alfa[grosor:lado - grosor, grosor:lado - grosor] = 0
    img.putalpha(Image.fromarray(alfa))
    d.rectangle([grosor - 2, grosor - 2, lado - grosor + 1, lado - grosor + 1], outline=(30, 28, 24, 255), width=2)
    guardar(img, "marco_piedra.png")


def tex_medallon():
    lado, k = 128, 4
    g = lado * k
    disco = a_imagen(madera(g, g, (40, 24, 13), (96, 62, 36), tablas=1))
    mascara = Image.new("L", (g, g), 0)
    ImageDraw.Draw(mascara).ellipse([2 * k, 2 * k, g - 2 * k, g - 2 * k], fill=255)
    disco.putalpha(mascara)
    d = ImageDraw.Draw(disco)
    d.ellipse([2 * k, 2 * k, g - 2 * k, g - 2 * k], outline=(10, 6, 3, 255), width=3 * k)
    d.ellipse([6 * k, 6 * k, g - 6 * k, g - 6 * k], outline=LATON + (255,), width=5 * k)
    d.arc([7 * k, 7 * k, g - 7 * k, g - 7 * k], 200, 330, fill=LATON_CLARO + (255,), width=2 * k)
    d.ellipse([12 * k, 12 * k, g - 12 * k, g - 12 * k], outline=(14, 8, 4, 180), width=3 * k)
    guardar(disco.resize((lado, lado), Image.LANCZOS), "medallon.png")


def main():
    os.makedirs(SALIDA, exist_ok=True)
    tex_madera_fondo()
    tex_marco_madera()
    tex_pergamino()
    tex_botones()
    tex_estandarte()
    tex_piedra()
    tex_medallon()


if __name__ == "__main__":
    main()
