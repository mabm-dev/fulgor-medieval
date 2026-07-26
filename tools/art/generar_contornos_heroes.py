from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


RAIZ = Path(__file__).resolve().parents[1]
IMAGENES = RAIZ / "public" / "imagenes"
HEROES = ("castilla", "leon", "aragon", "navarra", "granada")

# Estandarte que se elimina y zona central que se restaura cuando coincide
# con la cabeza, la corona, los hombros o las armas del personaje.
RECORTES_BANDERA = {
    "castilla": ((0, 0, 101, 116), [(42, 92), (88, 92), (103, 116), (91, 139), (38, 139)]),
    "leon": ((18, 0, 122, 196), [(67, 96), (111, 96), (137, 132), (151, 196), (38, 196), (52, 132)]),
    "aragon": ((24, 0, 105, 158), [(37, 91), (89, 91), (108, 127), (115, 158), (12, 158), (25, 127)]),
    "navarra": ((28, 0, 108, 151), [(39, 91), (84, 91), (103, 119), (113, 151), (11, 151), (27, 119)]),
    "granada": ((33, 0, 116, 158), [(38, 102), (91, 102), (108, 128), (119, 158), (8, 158), (25, 128)]),
}


for heroe in HEROES:
    origen = Image.open(IMAGENES / f"heroe-cut-{heroe}.png").convert("RGBA")
    alfa_original = origen.getchannel("A")

    rectangulo, restaurar = RECORTES_BANDERA[heroe]
    alfa_heroe = alfa_original.copy()
    ImageDraw.Draw(alfa_heroe).rectangle(
        (0, 0, alfa_original.width - 1, rectangulo[3]),
        fill=0,
    )
    mascara_restaurar = Image.new("L", alfa_original.size, 0)
    ImageDraw.Draw(mascara_restaurar).polygon(restaurar, fill=255)
    alfa_heroe = Image.composite(alfa_original, alfa_heroe, mascara_restaurar)

    # El margen evita que el halo se recorte contra los bordes del PNG.
    margen = 16
    alfa = Image.new(
        "L",
        (alfa_heroe.width + margen * 2, alfa_heroe.height + margen * 2),
        0,
    )
    alfa.paste(alfa_heroe, (margen, margen))

    # Dos halos exteriores difusos, sin línea sólida ni relleno interior.
    cercano = ImageChops.subtract(
        alfa.filter(ImageFilter.MaxFilter(13)),
        alfa,
    )
    cercano = cercano.filter(ImageFilter.GaussianBlur(2.4))
    cercano = cercano.point(lambda valor: round(valor * 0.52))

    lejano = ImageChops.subtract(
        alfa.filter(ImageFilter.MaxFilter(21)),
        alfa,
    )
    lejano = lejano.filter(ImageFilter.GaussianBlur(5.2))
    lejano = lejano.point(lambda valor: round(valor * 0.24))

    anillo = ImageChops.lighter(cercano, lejano)
    anillo = ImageChops.multiply(anillo, ImageChops.invert(alfa))

    silueta = Image.new("RGBA", alfa.size, (255, 255, 255, 0))
    silueta.putalpha(alfa)
    silueta.save(IMAGENES / f"heroe-silueta-{heroe}.png", optimize=True)

    contorno = Image.new("RGBA", alfa.size, (255, 255, 255, 0))
    contorno.putalpha(anillo)
    contorno.save(IMAGENES / f"heroe-contorno-{heroe}.png", optimize=True)
