from pathlib import Path

from PIL import Image


RAIZ = Path(__file__).resolve().parents[1]
IMAGENES = RAIZ / "public" / "imagenes"

# Primer separador original, situado bajo «Cargar partida».
origen = Image.open(IMAGENES / "menu-inicio-referencia.png").convert("RGB")
recorte = origen.crop((70, 468, 375, 506))

salida = Image.new("RGBA", recorte.size, (0, 0, 0, 0))
pixeles_origen = recorte.load()
pixeles_salida = salida.load()

for y in range(recorte.height):
    for x in range(recorte.width):
        rojo, verde, azul = pixeles_origen[x, y]
        brillo_calido = (rojo + verde) / 2
        es_dorado = (
            rojo > 52
            and verde > 38
            and rojo > azul * 1.34
            and verde > azul * 1.12
        )
        alfa = min(255, round((brillo_calido - 37) * 4.2)) if es_dorado else 0
        pixeles_salida[x, y] = (rojo, verde, azul, max(0, alfa))

mascara = salida.getchannel("A").point(lambda valor: 255 if valor > 45 else 0)
caja = mascara.getbbox()
if caja is None:
    raise RuntimeError("No se encontró el separador dorado en la zona indicada")

margen = 4
izquierda = max(0, caja[0] - margen)
arriba = max(0, caja[1] - margen)
derecha = min(salida.width, caja[2] + margen)
abajo = min(salida.height, caja[3] + margen)

salida.crop((izquierda, arriba, derecha, abajo)).save(
    IMAGENES / "separador-menu.png",
    optimize=True,
)
