# Rodrigo de Castilla — estudio geométrico V01

Estado: **borrador de volumen y vestuario; semejanza y realismo no aprobados**.
No sustituye el retrato vigente ni es un héroe terminado para Unity.

## Archivos

- `Rodrigo_referencias_v01.blend`: preparación anterior, solo láminas/cámaras.
- `Rodrigo_escultura_v01.blend`: nuevo personaje tridimensional editable.
- `crear_rodrigo_escultura_v01.py`: generador determinista para Blender 5.2.
- `escultura_manifest.json`: objetos, triángulos evaluados y límites geométricos.
- `verificar_rodrigo_escultura.py` y `verificacion_escultura.json`: comprobación
  de reapertura del archivo, referencias empaquetadas, originales y renders.

El nombre de archivo «escultura» identifica esta etapa de trabajo; no significa
que haya una escultura anatómica final ni semejanza facial resuelta.

Referencia principal: `ArtSource/Portraits/Heroes/heroe_rodrigo_diaz_castilla.png`
del commit `6f302d6`. La lámina derivada en
`ArtSource/Concept/CastillaRodrigoV01/` completa piernas y espalda de forma
inferida. Ninguna imagen legacy/web se utiliza para este personaje.

## Qué contiene

Colección `RODRIGO_GEOMETRIA_REAL`: cabeza de superficie continua, cabello y
barba geométricos, manos fusionadas, ropa, botas, cuero por capas, correajes,
broche, espada envainada, anillas de cota y capa de lana granate con ribete.
Materiales procedurales propios. Las referencias quedan empaquetadas como
imágenes de consulta, sin planos ni superficies de imagen del personaje.
La colección de estudio separa luces, suelo y tres cámaras.

Altura de autor aproximada de 1,81 m, con pies en el suelo. Es una escala humana
de trabajo, **no la escala de exportación estratégica del contrato visual**.
La geometría evaluada supera un millón de triángulos y contiene miles de
objetos de detalle: es una fuente densa, no una malla jugable ni un LOD.

Renders de comprobación, fuera del inventario de recursos del juego:
`Builds/ArtPreview/RodrigoCastillaV01/Frente_tres_cuartos_v01.png`,
`Espalda_v01.png` y `Rostro_v01.png`.

## Revisión visual y trabajo pendiente

Los renders se inspeccionaron. Se corrigieron huecos del cabello, uniones de
manos, párpados y el apoyo de la capa. **El resultado sigue demasiado
estilizado frente al objetivo naturalista.** No debe marcarse como arte acabado.

1. Rehacer planos anatómicos, párpados, nariz, boca y mandíbula comparando el
   retrato; evitar ampliar esta misma cabeza a los demás héroes.
2. Esculpir masas de pelo ondulado y transiciones de barba más naturales.
3. Dar caída, peso, asimetría y arrugas de compresión a la ropa, además de
   resolver encuentros de capa y mangas. El detalle añadido no sustituye esto.
4. Pintar desgaste y variación de materiales; revisar broche y correajes.
5. Solo después de la revisión visual: retopología, UV, bake de anillas/pelo,
   rig, animaciones, LOD y exportación a escala estratégica. No se ha añadido
   ningún FBX ni prefab Unity de este estudio.

No hay aprobación visual del usuario ni pruebas en Unity. La comprobación
técnica de archivos no demuestra calidad artística, rendimiento ni rig válido.
No se ha publicado este archivo en GitHub; revisar la política de binarios/LFS
antes de añadir la fuente densa a Git.
