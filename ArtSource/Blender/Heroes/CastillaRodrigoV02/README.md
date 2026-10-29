# Rodrigo — revisión anatómica V02

Referencia de identidad: retrato actual de Rodrigo en `ArtSource/Portraits/Heroes/`.
Conserva V01 para comparación. Es una mejora del estudio de personaje, no arte
final aprobado ni un modelo listo para Unity.

## Cambios verificables

- Cabeza anterior sustituida por una base anatómica oficial de MakeHuman,
  adaptada mediante targets y cambios propios. Párpados, nariz, boca, orejas y
  mandíbula tienen una topología humana continua. Se conserva la UV original
  de la parte de cabeza/cuello utilizada.
- Cuello adaptado al vestuario; apertura de ojos, relieve de cejas y pequeñas
  arrugas revisados. Variación de color de piel y transición a barba.
- Cabello y barba con fibras geométricas finas agrupadas en pocos objetos;
  se reducen los mechones gruesos del primer estudio. Sin retrato pegado sobre
  la cara ni planos que simulen un personaje tridimensional.
- Capa asimétrica, tejido, ribete y broche. La pieza de hombros se asienta con
  38 pasos de simulación contra el vestuario y se aplica a la malla. La fuente
  guardada no depende de una simulación activa ni contiene sus colisionadores
  temporales. Cuero y pantalón reciben pequeñas irregularidades geométricas.

## Procedencia y reproducción

`Rodrigo_escultura_v02.blend` es la fuente editable. El generador
`crear_rodrigo_v02.py` reutiliza las utilidades y el vestuario propios de
`../CastillaRodrigoV01/crear_rodrigo_escultura_v01.py`; esa dependencia se
conserva. Se ejecuta con Blender 5.2 en modo background y genera tres renders
en `Builds/ArtPreview/RodrigoCastillaV02/`.

La cabeza y siete targets proceden de assets gráficos oficiales MakeHuman,
**CC0-1.0**; no se copia ni importa código de la aplicación MakeHuman. Fuente,
commit, archivos originales, hashes y licencias en
`ArtSource/ThirdParty/MakeHumanCore/`. Los pesos usados están en el generador,
en la propiedad `target_weights` de la cabeza y en `escultura_manifest.json`.
El resto del vestuario, las curvas de pelo, materiales y adaptaciones son
trabajo propio del proyecto. Las referencias 2D actuales se conservan intactas.

`verificar_rodrigo_v02.py` reabre la fuente y comprueba la UV de cabeza,
ausencia de simuladores temporales, originales y assets externos mediante
hashes, tres renders y límites de altura. Resultado en `verificacion_v02.json`.
Esto verifica archivos y estructura, no una aprobación de calidad artística.

## Límites reales

La revisión tiene anatomía más humana que V01, pero todavía presenta estilización:
faltan una semejanza más fiel a Rodrigo, un peinado más suelto, desgaste pintado
y mejor caída en algunas partes del vestuario. Piernas y espalda siguen siendo
propuestas inferidas a partir de la lámina, pues el retrato no las muestra.

Altura de autor aproximada de 1,81 m. La fuente evaluada tiene varios millones
de triángulos: no se exporta al tablero. Pendientes retopología, atlas y bake
del vestuario/pelo/malla, rig, animaciones, LOD y escala estratégica del contrato
Blender–Unity. No se han creado FBX/prefab ni ejecutado pruebas en Unity.

No se ha publicado esta revisión en GitHub. Revisar la política de binarios/LFS
antes de añadir las fuentes densas a Git.
