"""Genera los iconos y marcos de interfaz de Fulgor Medieval (estilo oro épico).

Uso (desde la raíz del repositorio, requiere Pillow):

    python3 ArtSource/UI/generar_interfaz_fulgor.py

Escribe PNG en Assets/Fulgor/Resources/LegacyUI/Icons/. Todo se dibuja a
cuatro veces su tamaño y se reduce, para bordes suaves sin depender de
herramientas externas. La paleta es la del prototipo web: degradado de oro
#f6ecc0 → #e8c96a → #d4af37 → #9a7a24 sobre noche casi negra.
"""

import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SALIDA = os.path.join(RAIZ, "Assets", "Fulgor", "Resources", "LegacyUI", "Icons")

ORO = [(0.0, (246, 236, 192)), (0.38, (232, 201, 106)), (0.62, (212, 175, 55)), (1.0, (154, 122, 36))]
TINTA = (26, 18, 5, 255)
S = 1024
ICONO = 128


def degradado(tamano, paradas):
    ancho, alto = tamano
    columna = Image.new("RGBA", (1, alto))
    for y in range(alto):
        t = y / max(1, alto - 1)
        for (t0, c0), (t1, c1) in zip(paradas, paradas[1:]):
            if t0 <= t <= t1:
                k = (t - t0) / (t1 - t0)
                columna.putpixel((0, y), tuple(round(a + (b - a) * k) for a, b in zip(c0, c1)) + (255,))
                break
    return columna.resize((ancho, alto))


def mascara():
    return Image.new("L", (S, S), 0)


def girar(puntos, centro, grados):
    cx, cy = centro
    a = math.radians(grados)
    return [(cx + (x - cx) * math.cos(a) - (y - cy) * math.sin(a),
             cy + (x - cx) * math.sin(a) + (y - cy) * math.cos(a)) for x, y in puntos]


def elipse(centro, rx, ry, grados=0, pasos=72):
    cx, cy = centro
    puntos = [(cx + rx * math.cos(2 * math.pi * i / pasos), cy + ry * math.sin(2 * math.pi * i / pasos))
              for i in range(pasos)]
    return girar(puntos, centro, grados)


def rectangulo(centro, ancho, alto, grados=0):
    cx, cy = centro
    puntos = [(cx - ancho / 2, cy - alto / 2), (cx + ancho / 2, cy - alto / 2),
              (cx + ancho / 2, cy + alto / 2), (cx - ancho / 2, cy + alto / 2)]
    return girar(puntos, centro, grados)


def componer(capas, contorno=34):
    """Cada capa es (forma, detalle). Se pintan en orden: contorno oscuro, oro y detalle."""
    lienzo = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    oro = degradado((S, S), ORO)
    tinta = Image.new("RGBA", (S, S), TINTA)
    for forma, detalle in capas:
        borde = forma.filter(ImageFilter.MaxFilter(contorno + 1))
        lienzo.paste(tinta, (0, 0), borde)
        lienzo.paste(oro, (0, 0), forma)
        if detalle is not None:
            lienzo.paste(tinta, (0, 0), ImageChops.multiply(detalle, forma))
    return lienzo.resize((ICONO, ICONO), Image.LANCZOS)


def icono_grano():
    tallo = mascara()
    f = ImageDraw.Draw(tallo)
    f.line([(512, 930), (500, 700), (512, 250)], fill=255, width=46, joint="curve")
    f.polygon(girar(elipse((380, 720), 150, 42), (380, 720), -35), fill=255)
    f.polygon(girar(elipse((650, 760), 150, 42), (650, 760), 35), fill=255)
    capas = [(tallo, None)]
    # Cada grano es su propia capa, de abajo arriba, para que el contorno los separe.
    for i in range(5):
        y = 640 - i * 105
        for lado in (-1, 1):
            grano, surco = mascara(), mascara()
            ImageDraw.Draw(grano).polygon(elipse((512 + lado * 70, y), 54, 98, lado * 28), fill=255)
            ImageDraw.Draw(surco).line([(512 + lado * 52, y + 50), (512 + lado * 88, y - 50)], fill=255, width=12)
            capas.append((grano, surco))
    punta = mascara()
    ImageDraw.Draw(punta).polygon(elipse((512, 150), 52, 100), fill=255)
    capas.append((punta, None))
    return capas


def icono_madera():
    capas = []
    for centro, r in (((330, 690), 205), ((694, 690), 205), ((512, 360), 205)):
        forma, detalle = mascara(), mascara()
        ImageDraw.Draw(forma).ellipse([centro[0] - r, centro[1] - r, centro[0] + r, centro[1] + r], fill=255)
        dibujo = ImageDraw.Draw(detalle)
        for radio in (140, 82, 28):
            dibujo.ellipse([centro[0] - radio, centro[1] - radio, centro[0] + radio, centro[1] + radio],
                           outline=255, width=18)
        dibujo.line([(centro[0] + 20, centro[1] - 10), (centro[0] + 170, centro[1] - 90)], fill=255, width=16)
        capas.append((forma, detalle))
    return capas


def icono_piedra():
    capas = []
    for caja in ((95, 600, 495, 900), (529, 600, 929, 900), (300, 300, 724, 580), (380, 90, 644, 280)):
        forma, detalle = mascara(), mascara()
        ImageDraw.Draw(forma).rounded_rectangle(caja, radius=36, fill=255)
        x0, y0, x1, y1 = caja
        dibujo = ImageDraw.Draw(detalle)
        dibujo.line([(x0 + 40, y0 + 46), (x1 - 40, y0 + 46)], fill=255, width=14)
        dibujo.line([(x0 + (x1 - x0) * 0.62, y0 + 46), (x0 + (x1 - x0) * 0.55, y1 - 30)], fill=255, width=14)
        capas.append((forma, detalle))
    return capas


def icono_oro():
    capas = []
    for i in range(4):
        y = 820 - i * 105
        forma, detalle = mascara(), mascara()
        ImageDraw.Draw(forma).rounded_rectangle([90, y - 70, 560, y + 70], radius=70, fill=255)
        ImageDraw.Draw(detalle).line([(150, y), (500, y)], fill=255, width=14)
        capas.append((forma, detalle))
    forma, detalle = mascara(), mascara()
    ImageDraw.Draw(forma).ellipse([440, 330, 940, 830], fill=255)
    dibujo = ImageDraw.Draw(detalle)
    dibujo.ellipse([510, 400, 870, 760], outline=255, width=20)
    estrella = [(690 + (130 if i % 2 == 0 else 52) * math.sin(math.pi * i / 5),
                 580 - (130 if i % 2 == 0 else 52) * math.cos(math.pi * i / 5)) for i in range(10)]
    dibujo.polygon(estrella, fill=255)
    capas.append((forma, detalle))
    return capas


def icono_mano_de_obra():
    capas = []
    centro = (512, 512)
    # Pico: mango y cabeza curva.
    forma = mascara()
    f = ImageDraw.Draw(forma)
    f.polygon(rectangulo(centro, 70, 860, 45), fill=255)
    cabeza = [(150, 300), (260, 190), (420, 150), (330, 250), (280, 330)]
    f.polygon(girar([(x, y) for x, y in cabeza], (300, 300), 0), fill=255)
    f.polygon(girar([(380, 380), (260, 420), (150, 520), (290, 470)], (300, 300), 0), fill=255)
    f.polygon(rectangulo((300, 300), 150, 150, 45), fill=255)
    capas.append((forma, None))
    # Martillo encima, cruzado.
    forma, detalle = mascara(), mascara()
    f = ImageDraw.Draw(forma)
    f.polygon(rectangulo(centro, 76, 860, -45), fill=255)
    f.polygon(rectangulo((725, 300), 330, 190, -45), fill=255)
    ImageDraw.Draw(detalle).line(girar([(725, 230), (725, 370)], (725, 300), -45), fill=255, width=16)
    capas.append((forma, detalle))
    return capas


def icono_poblacion():
    capas = []
    for cx, escala in ((260, 0.8), (764, 0.8), (512, 1.0)):
        forma = mascara()
        f = ImageDraw.Draw(forma)
        base = 930
        cabeza_r = 120 * escala
        hombros = 230 * escala
        alto = 330 * escala
        f.ellipse([cx - cabeza_r, base - alto - 2.1 * cabeza_r, cx + cabeza_r, base - alto - 0.1 * cabeza_r], fill=255)
        f.pieslice([cx - hombros, base - alto, cx + hombros, base + alto], 180, 360, fill=255)
        capas.append((forma, None))
    return capas


def icono_turno():
    forma, detalle = mascara(), mascara()
    f, d = ImageDraw.Draw(forma), ImageDraw.Draw(detalle)
    f.rounded_rectangle([240, 90, 784, 170], radius=30, fill=255)
    f.rounded_rectangle([240, 854, 784, 934], radius=30, fill=255)
    f.polygon([(300, 170), (724, 170), (560, 512), (724, 854), (300, 854), (464, 512)], fill=255)
    d.polygon([(380, 230), (644, 230), (512, 470)], fill=255)
    d.polygon([(512, 640), (650, 800), (374, 800)], fill=255)
    return [(forma, detalle)]


def marco(nombre, lado, borde, fondo_arriba, fondo_abajo, filo, brillo, ornamentos):
    """Textura 9-slice: fondo en degradado, filo dorado interior y esquinas en rombo."""
    k = 4
    grande = lado * k
    imagen = degradado((grande, grande), [(0.0, fondo_arriba), (1.0, fondo_abajo)])
    dibujo = ImageDraw.Draw(imagen)
    dibujo.rectangle([0, 0, grande - 1, grande - 1], outline=(8, 6, 2, 255), width=k)
    dibujo.rectangle([k, k, grande - 1 - k, grande - 1 - k], outline=filo, width=k)
    if brillo:
        dibujo.rectangle([3 * k, 3 * k, grande - 1 - 3 * k, grande - 1 - 3 * k], outline=brillo, width=k)
    if ornamentos:
        r = 4 * k
        for cx in (2 * k + r // 2, grande - 2 * k - r // 2):
            for cy in (2 * k + r // 2, grande - 2 * k - r // 2):
                dibujo.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)],
                               fill=filo, outline=(8, 6, 2, 255))
    imagen.resize((lado, lado), Image.LANCZOS).save(os.path.join(SALIDA, nombre))
    return borde


def separador():
    k = 4
    ancho, alto = 256, 12
    imagen = Image.new("RGBA", (ancho * k, alto * k), (0, 0, 0, 0))
    dibujo = ImageDraw.Draw(imagen)
    medio = alto * k // 2
    for x in range(ancho * k):
        t = 1 - abs(x - ancho * k / 2) / (ancho * k / 2)
        dibujo.line([(x, medio - k // 2), (x, medio + k // 2)], fill=(212, 175, 55, round(255 * t ** 0.6)))
    c = ancho * k // 2
    dibujo.polygon([(c, medio - 5 * k), (c + 5 * k, medio), (c, medio + 5 * k), (c - 5 * k, medio)],
                   fill=(232, 201, 106, 255), outline=(26, 18, 5, 255))
    imagen.resize((ancho, alto), Image.LANCZOS).save(os.path.join(SALIDA, "separador.png"))


def main():
    os.makedirs(SALIDA, exist_ok=True)
    iconos = {
        "icono_grano": icono_grano,
        "icono_madera": icono_madera,
        "icono_piedra": icono_piedra,
        "icono_oro": icono_oro,
        "icono_mano_de_obra": icono_mano_de_obra,
        "icono_poblacion": icono_poblacion,
        "icono_turno": icono_turno,
    }
    for nombre, crear in iconos.items():
        componer(crear()).save(os.path.join(SALIDA, f"{nombre}.png"))
    marco("panel.png", 96, 24, (24, 19, 7), (6, 5, 2), (212, 175, 55, 150), (212, 175, 55, 40), True)
    marco("boton.png", 48, 12, (40, 32, 11), (10, 8, 3), (212, 175, 55, 190), None, False)
    marco("boton_activo.png", 48, 12, (238, 212, 130), (184, 145, 46), (246, 236, 192, 255), None, False)
    marco("boton_pulsado.png", 48, 12, (154, 122, 36), (90, 70, 20), (212, 175, 55, 255), None, False)
    marco("ficha.png", 48, 12, (22, 18, 7), (12, 10, 4), (212, 175, 55, 70), None, False)
    separador()


if __name__ == "__main__":
    main()
