# Fortaleza, tropas y héroes — primer kit V01

Primer repertorio propio para cubrir las categorías de arte que faltaban.
Ocho recursos estáticos, cuatro fuentes Blender y 32 mapas. No cierra el
repertorio de las doce facciones ni representa personajes históricos concretos.

## Contenido

| Recurso | Rasgos |
|---|---|
| Fortaleza castellana | Cuatro torres cuadradas, torre del homenaje, almenas, arco abierto, puertas de madera entreabiertas, patio y edificio auxiliar con tejas |
| Lancero | Lanza larga, casco, escudo y sobrevesta roja |
| Espadachín | Espada, escudo, protecciones y sobrevesta roja |
| Ballestero | Ballesta, cuerda, virote, carcaj y sobrevesta azul |
| Caballero montado | Caballo, silla, gualdrapa, bridas, riendas, jinete, escudo y lanza con gallardete |
| Héroe capitán | Capa azul, broches, escudo y lanza con gallardete |
| Heroína soberana | Capa roja, corona, trenzas, espada y escudo |
| Marcador de hueste | Dos miniaturas y estandarte azul para representación estratégica |

Piedra cálida, madera, terracota, rojo, azul y dorado mantienen la dirección
visual del kit de terreno. El emblema geométrico de castillo es una propuesta
propia; no se presenta como reproducción exacta de una heráldica histórica.

## Fuentes y exportaciones

- Fuentes: `Fortaleza_v01.blend`, `Tropas_v01.blend`,
  `Heroes_v01.blend` y `Hueste_v01.blend`.
- Generador: `crear_repertorio_v01.py`; depende de las herramientas de
  `../Terrain/fulgor_terrain_tools.py`.
- Verificador: `validar_repertorio_v01.py`.
- Derivados: `Assets/Fulgor/Art/RepertorioV01/<Familia>/`.
- Un FBX, un material, un atlas UV y cuatro mapas por recurso:
  BaseColor, Normal, Roughness y Metallic.
- Fortaleza: 2048 px; personajes y hueste: 1024 px.
- Color en sRGB; los otros tres mapas sin sRGB. Metadatos de Unity incluidos.
- Las texturas están empaquetadas en las fuentes Blender.
- La colección oculta `FUENTES_EDITABLES` conserva la geometría anterior
  al horneado, con materiales originales y grupos de vértices por pieza.
  Estos grupos organizan la fuente; **no son un esqueleto ni pesos de skinning**.
- Las mallas exportadas tienen origen inferior en 0 y escala 1.
  La cara frontal de las miniaturas mira hacia -Y de Blender.

El mapa Metallic conserva la diferencia de respuesta entre acero, tela, piel,
madera y piedra en los materiales de Blender. En Unity se preparará el material
URP y se convertirá/asignará Roughness a la convención de suavidad escogida;
la importación del FBX no completa esa conexión automáticamente.

## Reproducción

Desde la raíz del proyecto, generar las cuatro familias `Fortaleza`,
`Tropas`, `Heroes` y `Hueste`:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/RepertorioV01/crear_repertorio_v01.py -- --family Fortaleza
```

Tras completar las cuatro:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/RepertorioV01/validar_repertorio_v01.py -- --write-import-meta
```

El verificador crea únicamente los metadatos ausentes y conserva los GUID.
Omitir `-- --write-import-meta` para comprobar una entrega ya preparada.

Las imágenes se regeneran en `Builds/ArtPreview/RepertorioV01/`, ignorada por
Git: fortaleza frontal y oblicua, galería de tropas, héroes y marcador de hueste.
Son renders de los modelos Blender; la revisión en Unity queda pendiente.

## Comprobación realizada y límites

Se han reabierto los ocho FBX y sus cuatro fuentes: coinciden los triángulos
y las dimensiones; las mallas tienen un material y un canal UV, y están presentes
los 32 mapas empaquetados. Se comprueban escala, origen, tamaño de texturas,
metadatos y ausencia de GUID duplicados en Assets. El informe reproducible está
en `Builds/ArtPreview/RepertorioV01/validacion_v01.json`.

La fortaleza tiene una huella de 1,13 × 1,05 y altura aproximada 0,795,
dentro de las referencias espaciales del contrato. Sus 17.944 triángulos
superan el presupuesto inicial orientativo de 15.000: se necesita reducción
y LOD antes de tratarla como recurso de producción. El marcador también
necesita reducción para repetirse en un mapa grande.

Las figuras son miniaturas estilizadas de primera versión. Faltan rig,
poses alternativas, animaciones, refinamiento de anatomía, variantes por
facción y retratos/identidades de líderes concretos. La soberana y el capitán
son arquetipos de revisión visual, sin correspondencia histórica cerrada.
La corona y el equipo no certifican una reconstrucción de un periodo concreto.

Faltan LOD, materiales/prefabs URP, colisiones, asignación al mapa, movimiento,
combate y medición de coste. Las puertas, capas, estandartes y caballo están
estáticos. Esta entrega cubre el primer kit artístico, no el cierre de los
hitos de fortaleza y huestes jugables.

Todo el modelado, los materiales, el emblema y las herramientas son propios.
No se han descargado recursos externos. La política de Git LFS sigue pendiente
antes de publicar estos binarios.

## Métricas de los modelos

| Familia | Modelo | Triángulos |
|---|---|---:|
| Fortaleza | SM_FortalezaCastellana_A_LOD0 | 17944 |
| Tropas | SM_Lancero_A_LOD0 | 2208 |
| Tropas | SM_Espadachin_A_LOD0 | 2224 |
| Tropas | SM_Ballestero_A_LOD0 | 2148 |
| Tropas | SM_CaballeroMontado_A_LOD0 | 4512 |
| Heroes | SM_HeroeCapitan_A_LOD0 | 3256 |
| Heroes | SM_HeroinaSoberana_A_LOD0 | 3752 |
| Hueste | SM_HuesteEstandarte_A_LOD0 | 5052 |

## Revisión visual posterior

El usuario considera este acabado demasiado parecido a Lego. La propuesta
actual de formas más naturales está en [Repertorio V02](../RepertorioV02/README.md).
V01 se conserva como referencia de comparación y no se presenta como arte aprobado.
