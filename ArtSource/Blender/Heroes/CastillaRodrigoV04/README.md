# Rodrigo — corrección de cuello y barba V04

Motivo: el usuario señaló que V03 no se veía bien, especialmente cuello y barba.
V03 no queda aprobada. V04 es una nueva propuesta pendiente de revisión visual.
El retrato actual sigue siendo la referencia de identidad, sin modificarlo.

## Corrección

- Cabeza completa bajada 20 mm respecto al vestuario, manteniendo ojos, pelo,
  barba y anatomía alineados. El corte inferior queda dentro del cuello de malla.
- Se elimina la proyección cilíndrica del cuello. Se conservan mejor garganta
  y planos bajo el mentón, limitando solo el exceso de anchura del injerto.
  La nuca recibe una transición amplia para evitar la arista del perfil.
- Se anula el relieve del mapa normal en la zona baja del cuello; conserva
  color UV y geometría, sin venas exageradas por el normal de piel.
- Se reduce el oscurecimiento máximo de la piel bajo la barba de 86% a 50%.
  Fibras más finas, curvadas y proyectadas sobre la superficie facial: siguen
  mandíbula y mentón en vez de colgar rectas como una cortina. Mayor densidad
  que la primera prueba rasurada, con bordes progresivos y unión al bigote.
- Se recortan fibras largas de la nuca con alturas de terminación variables y
  puntas afinadas. Las cifras quedan en el manifiesto y propiedades de cabeza.

## Fuentes y comprobación

`Rodrigo_escultura_v04.blend`: fuente editable; conserva V01/V02/V03.
`crear_rodrigo_v04.py`: generador Blender 5.2; sigue necesitando los bloques
propios de vestuario/utilidades del generador V01 y los assets CC0 documentados.
No se añaden nuevos assets externos en V04. UV y mapas de piel de V03 se conservan.

Cuatro renders en `Builds/ArtPreview/RodrigoCastillaV04/`: cuerpo, espalda,
rostro y `Perfil_cuello_v04.png`. El perfil permite revisar mandíbula, garganta
y nuca, además de la presentación frontal.

`verificar_rodrigo_v04.py` reabre la fuente y comprueba la colocación de cabeza,
ausencia de relieve añadido en el cuello bajo, UV, mapas, nuca y cuatro renders.
Mide en la geometría guardada la distancia entre una muestra de puntos de barba
y la piel: umbral 1,5 mm. Verifica por SHA-256 las fuentes externas y los doce
retratos originales. Resultado en `verificacion_v04.json`.

La verificación técnica no demuestra naturalidad ni semejanza aprobada. Quedan
pendientes el parecido facial, un acabado más natural de pelo/barba y las
mejoras de ropa, manos y postura. Fuente densa a escala humana aproximada de
1,80 m, sin retopología, bake/atlas final, rig, LOD ni exportación a Unity.
No se ejecutan pruebas de juego ni se publica la revisión en GitHub.


## Continuidad hacia el cuello: V05

El usuario indicó que lo de arriba estaba bien, pero la barba no bajaba hasta
el cuello. V05 conserva esta base, incluida la barba superior, y amplía solo
la continuidad bajo la mandíbula hacia el cuello alto. La aceptación es parcial;
no tratar todo el personaje como aprobado. Véase ../CastillaRodrigoV05/.
