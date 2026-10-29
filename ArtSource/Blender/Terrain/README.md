# Estudios de terreno Blender

La dirección actual combina **color cálido y formas naturales**. La montaña
de referencia está en `Montana_natural_v02.blend`; la revisión de llanura,
bosque y colina, junto al conjunto, en `Bosque_colina_natural_v02.blend`.
Los estudios de llanura v01 y v02 color se conservan para comparación. Agua,
costas, cereal y caminos tienen una primera revisión natural en
`Agua_cereal_caminos_natural_v01.blend`.

Creado el 21 de octubre de 2026 con Blender 5.2.1 LTS mediante bpy.
Geometría, colores y composición propios para Fulgor Medieval. No incorpora
texturas, fuentes de terceros descargadas ni modelos externos.

## Variantes

| Malla | Uso propuesto | Triángulos |
|---|---|---:|
| SM_Llanura_A_LOD0 | Pastizal seco, caliza y tonos oliva | 328 |
| SM_Llanura_B_LOD0 | Cereal dispuesto en hileras | 454 |
| SM_Llanura_C_LOD0 | Senda de tierra con vegetación lateral | 271 |

Las mallas comparten una paleta de ocho materiales. La altura del suelo es
0,12, el radio exterior 0,97 y los pivotes de exportación están en (0,0,0),
centro inferior. Se comprueban los límites del hexágono y la escala 1:1 antes
de dar por terminada la generación. Solo se exportan mallas, sin cámara,
luces, rótulos ni suelo de presentación.

El .blend contiene una lámina con las tres variantes separadas. Sus posiciones
en esa lámina son de presentación: los FBX se exportan antes de desplazarlas.

## Reconstrucción

Ejecutar desde la raíz del proyecto:

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_llanura_v01.py -- --paleta calida
```

El script usa la paleta cálida por defecto; `--paleta original` reproduce la v01.
Reconstruye los archivos de la variante elegida y guarda la lámina cálida en
`Builds/ArtPreview/Llanura_v02_color.png`, junto a un JSON de dimensiones y recuentos.
Se ejecuta en un proceso separado con escena inicial de fábrica para conservar
las escenas abiertas del usuario.

## Estado y límites

Propuesta artística inicial. Los derivados están en Assets, pero la escena de
campaña todavía utiliza las mallas procedurales anteriores. Quedan pendientes
la importación comprobada en Unity, materiales compartidos URP, prefabs,
costuras con los otros terrenos y revisión cerca/lejos. El color del render de
Blender no certifica el resultado de materiales en Unity.

La UV es una proyección plana provisional; no es un atlas de producción ni una
UV de lightmap. Los vegetales usan triángulos simples: revisar el culling en URP
y resolver caras posteriores antes de integrarlos. El sufijo LOD0 identifica
la primera malla; aún no existen niveles LOD inferiores.

La senda C es una variante visual decorativa, sin conexión automática al grafo
de rutas del escenario ni resolución de cruces. No cambia las reglas de juego.

## Bosque, colina y montaña — primer prototipo cálido

`Biomas_color_v01.blend` contiene dos escenas: una galería de las tres casillas
y una composición con la llanura cálida aprobada como primer prototipo.
El script propio `crear_biomas_color_v01.py` genera geometría y materiales
originales, sin recursos externos. Para reconstruir la composición necesita
`Llanura_v02_color.blend`, generado por el comando anterior.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_biomas_color_v01.py
```

| Malla | Triángulos | Altura máxima Blender |
|---|---:|---:|
| SM_Bosque_A_LOD0 | 422 | 0,909 |
| SM_Colina_A_LOD0 | 242 | 0,600 |
| SM_Montana_A_LOD0 | 217 | 1,280 |

Los FBX se encuentran en `Assets/Fulgor/Art/Terrain/BiomasColorV01/`, con escala
1:1 y pivote inferior en el origen. La base de cada casilla sigue la altura
actual del terreno en Presentation: 0,18 / 0,28 / 0,42. Los modelos quedan
contenidos en el hexágono de radio 0,97, comprobado por el generador.

Las dos láminas se guardan en `Builds/ArtPreview/Biomas_color_v01.png` y
`Builds/ArtPreview/Biomas_conjunto_v01.png`. Se añade un JSON con recuentos y
dimensiones. Las cámaras, luces y textos no se incluyen en los FBX.

La vegetación del bosque y las cumbres son parte de una malla por casilla en
este estudio. Quedan pendientes variantes adicionales, niveles LOD, atlas o
reducción de submallas/materiales, colisiones y las transiciones entre alturas.
Los colores son materiales simples: no hay texturas de terceros. Todavía no
se han integrado en la escena de campaña ni validado en Unity.

## Montaña natural v02

Revisión solicitada porque las primeras montañas parecían dibujos animados.
`Montana_natural_v02.blend` conserva la comparación anterior/nueva y una escena
con los otros terrenos. La nueva malla es una superficie continua con crestas,
estribaciones y cortes de erosión. Los colores de roca y hierba se mezclan según
pendiente y altura, con detalle de grano y estratos generado en Blender.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_montana_natural_v02.py
```

Necesita los .blend previos para montar la comparación y el conjunto.
La geometría y los materiales son propios: no se descargan datos ni recursos.
El shader fuente se hornea a un atlas de color y un mapa normal tangente de
1024×1024. Los renders usan esas texturas horneadas, con un solo material.
Las imágenes se guardan en Assets y se empaquetan también dentro del .blend.

- FBX: `Assets/Fulgor/Art/Terrain/MontanaNaturalV02/SM_MontanaNatural_A_LOD0.fbx`.
- Texturas: subcarpeta `Textures/`; BaseColor en sRGB, Normal como mapa normal.
- Geometría: 5566 triángulos, 2785 vértices, pivote (0,0,0), escala (1,1,1).
- Altura máxima aproximada: 1,425, incluida base de 0,42.
- Comparación: `Builds/ArtPreview/Montana_comparacion_v02.png`.
- Conjunto: `Builds/ArtPreview/Montana_conjunto_v02.png`.

La densidad aumenta frente a los 217 triángulos de v01 para estudiar el acabado.
No se ha medido su coste en el tablero. Faltan LOD, integración de material URP,
validación del normal, colisiones y transiciones. La UV ya separa superficie y
laterales para el atlas de color; no constituye una UV de lightmap validada.
Los otros terrenos todavía tienen el acabado del prototipo anterior.

## Bosque, colina y llanura naturales v02

`Bosque_colina_natural_v02.blend` contiene una galería de bosque/colina y una
escena con los cuatro terrenos de dirección natural. La montaña procede de la
v02 aprobada; bosque, colina y llanura son nuevas revisiones. Se conservan las
fuentes anteriores para comparar.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_bosque_colina_natural_v02.py
```

El generador utiliza `fulgor_terrain_tools.py`, también fuente propia, para
construir superficies, preparar UV, hornear materiales, exportar y renderizar.
Necesita `Montana_natural_v02.blend` para montar el conjunto. No descarga datos.

| Recurso | Triángulos | Atlas color + normal |
|---|---:|---:|
| SM_BosqueNatural_A_LOD0 | 10702 | 2048×2048 |
| SM_ColinaNatural_A_LOD0 | 3502 | 1024×1024 |
| SM_LlanuraNatural_A_LOD0 | 2798 | 1024×1024 |

Los derivados están en `Assets/Fulgor/Art/Terrain/BiomasNaturalV02/`, con
texturas propias en su subcarpeta Textures. Cada casilla usa un material con
atlas de color y normal tangente; las imágenes se empaquetan en el .blend.
Las hojas y la hierba tienen geometría en ambas caras para mantener la silueta
con culling normal. No hay texturas externas ni transparencia de hojas.

Las bases conservan 0,18 / 0,28 / 0,12 de altura y sus bordes hexagonales. El
generador comprueba límites, pivote inferior y escala 1:1 antes de exportar.
Las cámaras y el suelo de presentación no forman parte de los FBX.

La densidad es para revisar detalle cercano: faltan LOD y medición en tablero.
Los árboles están unidos al suelo en este estudio; separar vegetación para
animación, variación e instancing se decidirá en la integración. Faltan URP,
colisiones, transiciones y validación visual en Unity. Cereal, senda, agua y
costa se amplían en el estudio natural v01 descrito a continuación.

Láminas: `Builds/ArtPreview/Bosque_colina_natural_v02.png` y
`Builds/ArtPreview/Biomas_naturales_conjunto_v02.png`.

## Agua, costa, cereal y caminos naturales v01

`Agua_cereal_caminos_natural_v01.blend` incorpora tres escenas de revisión:
galería de agua/orillas, galería de cereal/caminos y composición rural con
piezas enlazadas. Mantiene fuentes anteriores para comparar.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_agua_cereal_caminos_natural_v01.py
```

Utiliza `fulgor_terrain_tools.py` y necesita el .blend de bosque/colina natural
para la composición. Todo el modelado, la paleta y los mapas son propios.

| Malla | Triángulos | Mapas |
|---|---:|---|
| SM_MarNatural_A_LOD0 | 5566 | Color, normal, roughness, 1024 px |
| SM_CostaNatural_A_LOD0 | 5918 | Color, normal, roughness, 1024 px |
| SM_LagoNatural_A_LOD0 | 5918 | Color, normal, roughness, 1024 px |
| SM_RioNatural_A_LOD0 | 5918 | Color, normal, roughness, 1024 px |
| SM_CaminoNatural_Recto_LOD0 | 3310 | Color y normal, 1024 px |
| SM_CaminoNatural_Curva_LOD0 | 3310 | Color y normal, 1024 px |
| SM_CaminoNatural_Cruce_LOD0 | 3310 | Color y normal, 1024 px |
| SM_CerealNatural_A_LOD0 | 6214 | Color y normal, 2048 px |

Los FBX y los mapas están en `Assets/Fulgor/Art/Terrain/AguaRuralNaturalV01/`.
Cada malla exportada tiene un material y pivote inferior en el origen, escala
1:1 y huella contenida en el hexágono. Los renders usan mapas horneados, que
también se empaquetan en la fuente editable.

El cereal tiene tallos y espigas con volumen cerrado. Costa, lago y río añaden
piedras y juncos. Las orillas mezclan gradualmente color y rugosidad, evitando
un cambio de material por cada triángulo en la línea de agua.

Los caminos se orientan por normales de caras del hexágono en el plano Blender:
recta 0°/180°, curva 60°/180°, bifurcación 0°/60°/180°. La demostración enlaza
rectas y una curva. El radio 0,97 deja la separación del prototipo; aún faltan
conectores, variantes para otros patrones y comprobación de continuidad en Unity.

El agua es opaca y estática en este estudio, con pequeñas ondulaciones en malla
y normal. No incluye animación, refracción, profundidad física ni shader de
agua de Unity. Roughness se importa como datos lineales: habrá que convertir
a smoothness (1 - roughness) al preparar el material URP. Normal también es un
mapa de datos; el canal de color es sRGB.

Las casillas mixtas de río/costa no cambian la lógica del generador ni añaden
puentes, suministro o navegación. Cereal y caminos son variantes visuales sin
asignación automática al mapa de juego. La asignación por vecinos, los trazados
lógicos y la integración con MapRouteGraph siguen pendientes.

Se han comprobado generación y límites geométricos y revisado los renders. No
se han ejecutado pruebas en Unity. La densidad es para estudio cercano: LOD,
presupuesto de renderizado, colisiones y transiciones siguen pendientes.

Láminas en `Builds/ArtPreview/`: `Aguas_costas_naturales_v01.png`,
`Cereal_caminos_naturales_v01.png` y `Paisaje_rural_natural_v01.png`.

## Agua y paisaje rural: revisión natural v02

`Agua_cereal_caminos_natural_v02.blend` revisa los ocho modelos del estudio v01.
Usa agua azul por preferencia posterior del usuario y conserva la paleta cálida
del terreno. Las playas tienen perfiles irregulares,
el lago es asimétrico y el cauce varía de ancho. Piedras y juncos se agrupan;
el cereal tiene más densidad, alturas, inclinaciones y pequeños claros. Los
caminos incorporan rodadas con relieve, grava y hombros menos uniformes.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_agua_cereal_caminos_natural_v02.py
```

Utiliza `fulgor_terrain_tools.py` y `Bosque_colina_natural_v02.blend` para el
conjunto. No modifica las fuentes v01. Los derivados se guardan separados en
`Assets/Fulgor/Art/Terrain/AguaRuralNaturalV02/`.

| Malla | Triángulos | Mapas |
|---|---:|---|
| SM_MarNatural_A_LOD0 | 8878 | Color, normal, roughness, 1024 px |
| SM_CostaNatural_A_LOD0 | 9768 | Color, normal, roughness, 1024 px |
| SM_LagoNatural_A_LOD0 | 9766 | Color, normal, roughness, 1024 px |
| SM_RioNatural_A_LOD0 | 9750 | Color, normal, roughness, 1024 px |
| SM_CaminoNatural_Recto_LOD0 | 6310 | Color y normal, 1024 px |
| SM_CaminoNatural_Curva_LOD0 | 6230 | Color y normal, 1024 px |
| SM_CaminoNatural_Cruce_LOD0 | 6330 | Color y normal, 1024 px |
| SM_CerealNatural_A_LOD0 | 16366 | Color y normal, 2048 px |

Cada pieza tiene un material, pivote inferior en el origen, escala 1:1 y
geometría dentro del hexágono de radio 0,97. El generador comprueba estos
límites antes de exportar y guarda las métricas en `Builds/ArtPreview/`.
Los renders usan los mapas horneados que también se empaquetan en el .blend.

El mayor detalle aumenta la densidad de malla; no supone una mejora de
rendimiento. Faltan LOD, materiales URP, colisiones y medición en tablero.
El agua sigue siendo opaca y estática. Roughness y normal son datos lineales;
URP requiere convertir roughness a smoothness. Las conexiones de caminos
conservan los ángulos de v01; otros patrones y las transiciones entre vecinos
siguen pendientes. No se ha integrado esta revisión ni probado en Unity.

Láminas: `Aguas_costas_naturales_v02.png`, `Cereal_caminos_naturales_v02.png` y
`Paisaje_rural_natural_v02.png`, en `Builds/ArtPreview/`.

## Conexiones de agua azul v03

`Conexiones_agua_v03.blend` contiene una galería de siete piezas y una
desembocadura compuesta por diez hexágonos. La paleta azul conserva la elección
del usuario. La geometría, los colores y los mapas son propios.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_conexiones_agua_v03.py
```

El generador utiliza `fulgor_terrain_tools.py`. Sus funciones de geometría y
exportación aceptan ahora un radio opcional; el valor por defecto sigue siendo
0,97 para reproducir los estudios anteriores. Este kit usa radio 1: sus bordes
llenan la cuadrícula axial del contrato Blender–Unity sin separación añadida.

| Malla | Triángulos | Mapas |
|---|---:|---|
| SM_Agua_Mar_LOD0 | 7918 | Color, normal y roughness, 1024 px |
| SM_Agua_RioRecto_LOD0 | 8078 | Color, normal y roughness, 1024 px |
| SM_Agua_RioCurva_LOD0 | 8078 | Color, normal y roughness, 1024 px |
| SM_Agua_Desembocadura_LOD0 | 8078 | Color, normal y roughness, 1024 px |
| SM_Agua_CostaRecta_LOD0 | 8078 | Color, normal y roughness, 1024 px |
| SM_Agua_Cabo_LOD0 | 8078 | Color, normal y roughness, 1024 px |
| SM_Agua_Bahia_LOD0 | 8078 | Color, normal y roughness, 1024 px |

FBX y texturas: `Assets/Fulgor/Art/Terrain/ConexionesAguaV03/`. Un material por
modelo exportado; todavía no hay atlas compartido para el kit. Escala 1:1,
pivote inferior en el origen. El agua está a 0,04 y el terreno seco a 0,12,
con detalle interior que se desvanece hacia el borde.

Las seis caras se numeran 0 a 5 por sus normales, a 0/60/120/180/240/300 grados
en Blender. Cada cara define `sea`, `land`, `river` o `shore+`/`shore-`.
El río tiene ancho 0,29 en los puertos; las curvas enlazan las caras 180 y 60.
Los signos de costa describen el lado seco según la tangente de cada cara.
Dos caras enfrentadas invierten su tangente. Estos son datos del estudio de
arte, todavía sin un selector de variantes o conversión de direcciones en Unity.

El generador comprueba origen, escala y límites; además compara altura y color
base de diseño en 41 puntos por borde de las uniones de la composición. Esto
no certifica continuidad de normales, filtrado de atlas, ni todos los patrones
posibles. La revisión de renders usa los mapas horneados. Las métricas y los
perfiles quedan en `Builds/ArtPreview/Conexiones_agua_v03_metrics.json`.

Siguen pendientes agua animada, LOD, materiales URP compartidos, colisiones,
asignación automática por vecinos y medición del tablero. No se ha cambiado la
escena de campaña ni ejecutado pruebas en Unity. El radio 1 de este kit no
rellena los huecos de las piezas antiguas de radio 0,97.

Láminas en `Builds/ArtPreview/`: `Conexiones_agua_v03.png` y
`Desembocadura_conjunto_v03.png`.

## Puente de piedra v01

`Puente_piedra_v01.blend` contiene una vista de detalle y un conjunto con
caminos y río. El puente tiene un arco abierto, dovelas con variación de color,
sillares, pavimento, pretiles y albardas. Geometría, color, desgaste y mapas
son propios; no se han usado recursos externos.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python ArtSource/Blender/Terrain/crear_puente_piedra_v01.py
```

Depende de `fulgor_terrain_tools.py` y de `Conexiones_agua_v03.blend`, del que
carga el río recto para la presentación. Exporta en
`Assets/Fulgor/Art/Terrain/PuentePiedraV01/`:

- `SM_PuentePiedra_A_LOD0`: pieza independiente, un material con color y normal
  de 2048 px. Sus rampas llegan a las caras 0/180 del hexágono de radio 1.
- `SM_CaminoPuente_Recto_LOD0`: casilla completa de radio 1, un material con
  color y normal de 1024 px; conexiones 0/180.

La malla del puente tiene 3852 triángulos y la casilla de camino 4030. Son
valores del estudio, sin medición de rendimiento en el tablero.

El puente se coloca con el mismo origen del terreno: el terreno seco está a
0,12 y sus cimientos bajan a 0,04, junto al nivel del agua. No se debe alinear
su mínimo de malla con la parte superior de la casilla. Escala 1:1, sin
corrección de Transform; puente y río se conservan como recursos separados.
El conjunto utiliza un río a 60 grados respecto al camino; no pretende validar
otras orientaciones o anchos de cauce. La abertura central deja 0,32 sobre el
agua y la calzada llega a 0,455 antes del pavimento.

Se comprueban límites de exportación, pivote, escala, altura de acceso y una
holgura geométrica del arco; se revisan los dos renders. No incluye collider,
navmesh, reglas de cruce ni integración con `MapRouteGraph`. El agua sigue
estática. Materiales URP, LOD y medición de coste siguen pendientes; tampoco
se han ejecutado pruebas en Unity.

Láminas: `Builds/ArtPreview/Puente_piedra_detalle_v01.png` y
`Builds/ArtPreview/Puente_piedra_conjunto_v01.png`. Las métricas se guardan junto
a ellas en `Puente_piedra_v01_metrics.json`.

## Molino de agua v01

`Molino_agua_v01.blend` contiene un molino de piedra y enlucido cálido, madera,
tejado de tejas individuales, chimenea, puerta, ventanas, caja y sacos. La rueda
tiene dos aros, radios, paletas y eje. Todo el modelado y los mapas son propios.

```powershell
& 'D:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/Blender/Terrain/crear_molino_agua_v01.py
```

Utiliza `fulgor_terrain_tools.py`, `Conexiones_agua_v03.blend` y
`Puente_piedra_v01.blend`. El generador del puente conserva ahora su plantilla
nombrada de camino con un usuario ficticio de Blender para que se pueda cargar
desde otros estudios, aunque en su composición solo se usen copias.

Exporta a `Assets/Fulgor/Art/Buildings/MolinoAguaV01/`:

- `SM_MolinoAgua_Edificio_LOD0`: edificio con pivote inferior, un material y
  color/normal de 2048 px.
- `SM_MolinoAgua_Rueda_LOD0`: rueda independiente con pivote en el centro del
  eje, un material y color/normal de 1024 px.

El edificio tiene 8928 triángulos y la rueda 2044. Esta densidad sirve para la
revisión cercana; no se ha medido su coste en un tablero representativo.

Ambos recursos tienen origen 0 y escala 1:1 al exportar. La rueda es una
excepción explícita al pivote inferior: su malla ocupa alturas negativas y
positivas alrededor del eje Y de Blender. `bake_export` acepta un mínimo de
altura opcional para esta pieza; el valor por defecto continúa en 0 para los
otros estudios. La orientación del eje tras importar el FBX se comprobará en
Unity antes de animarla; no se debe inferir del nombre de los ejes de Blender.

En el conjunto de radio 1, el edificio se coloca en `(0, 0.49, 0.12)` y la
rueda en `(0.15, 0.12, 0.255)` en Blender, sobre el río recto sin rotar.
Las paletas entran ligeramente en el agua a altura 0,04. Estos desplazamientos
forman parte de la composición de estudio, no de un prefab de Unity. La rueda
está estática; no incluye canales de derivación ni simulación hidráulica.

Se recalculan las normales hacia fuera antes de hornear y exportar; se
comprueban origen, escala, límites y que las paletas entren en el agua sin
atravesar la base de la casilla. Se revisan las vistas de detalle y conjunto.
Faltan animación, materiales URP, LOD, colisiones, asignación al mapa, producción
económica y medición de coste. No se han ejecutado pruebas en Unity.

Láminas: `Builds/ArtPreview/Molino_agua_detalle_v01.png` y
`Builds/ArtPreview/Molino_agua_conjunto_v01.png`. Métricas junto a ellas en
`Molino_agua_v01_metrics.json`.

## Revisión posterior V04

La revisión de detalle de los 23 modelos actuales está en
[DetalleV04](../DetalleV04/README.md). Conserva estas fuentes como dependencias
de generación y referencia de comparación. La integración en Unity, los LOD
y la medición siguen pendientes.
