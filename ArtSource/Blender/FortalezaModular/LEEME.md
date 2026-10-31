# Fortaleza modular: montaje inicial

`Fortaleza_modular.blend` es una composición editable creada con las piezas GLB aportadas para esta fortaleza. La fortaleza completa no se ha insertado en el montaje. Los archivos de origen se han conservado sin cambios.

## Organización en Blender

- **01 Torre principal:** torre del homenaje.
- **02 Murallas:** ocho tramos que se pueden mover por separado.
- **03 Torres:** cuatro torres cuadradas y dos torreones laterales.
- **04 Entrada:** entrada fortificada con puente.
- **05 Piezas alternativas:** torre cuadrada y puerta de madera de reserva. La colección está oculta en vista y render; se puede activar desde el Outliner.
- **06 Presentación:** cámara y suelo neutro para revisar la composición. El suelo no es el terreno del juego.

Las murallas y torres repetidas son objetos independientes que comparten datos de malla. Puedes mover, girar o escalar cada objeto; si editas su geometría, afectará a los demás que comparten la malla. Para editar una copia por separado, hazla de usuario único desde Blender.

## Correspondencia con los archivos originales

| Archivo | Contenido observado |
|---|---|
| `torre_centralF2bf16.glb` | Torre del homenaje y construcciones adheridas |
| `entrada_fortaleza.glb` | Entrada fortificada y puente |
| `muralla_larga.glb` | Tramo de muralla |
| `torre_cuadrada2.glb` | Torre cuadrada usada en esquinas |
| `torreon_redondo.glb` | Torreón redondo lateral |
| `puerta_frotaleza.glb` | Torre cuadrada alternativa |
| `torre_cuadrada.glb` | Puerta de madera alternativa |
| `fortaleza_completa.glb` | Fortaleza de una pieza; no se ha usado en este montaje |

Las texturas están incorporadas al `.blend`. La composición utiliza el suelo de cada pieza, que puede incluir elementos adheridos: los objetos son módulos independientes, pero su geometría interna no se ha separado en bloques, ventanas, tejados o banderas.

## Pendiente para Unity

El montaje es una referencia de composición, no un prefab final. Las piezas originales contienen cerca de 1,5 millones de triángulos cada una; el conjunto visible ronda los 24 millones al contar instancias. Necesita optimización, LOD, revisión de uniones y adaptación de escala y pivotes antes de su uso en el juego. No se ha integrado ni probado en Unity.
