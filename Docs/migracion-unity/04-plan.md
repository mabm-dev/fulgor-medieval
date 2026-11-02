# Plan de migración y criterios de cierre

Los estados describen el repositorio revisado el 2 de noviembre de 2026 (v0.7.0). Cada hito
necesita su propia prueba jugable, visual o de rendimiento además de Edit Mode.

| Orden | Objetivo | Estado | Criterio de cierre pendiente |
|---|---|---|---|
| 0 | Base técnica | Cerrada: 94/94 Edit Mode en integración continua y escena revisada con gráficos en la build de Windows | — |
| 1 | Contrato Blender–Unity | Puerta técnica superada | Comprobar siluetas, pivotes e iluminación cerca y lejos con gráficos |
| 2 | Kit vertical de terreno | Arte de detalle de biomas, agua (costa, bahía, cabo, desembocadura, río), caminos y cultivos integrado en el mapa | Revisión visual final de cerca y de lejos |
| 3 | Ríos, caminos y costa | Caminos y riberas como rutas lógicas (`MapRouteGraph`) con coste de movimiento; agua conectada | Ríos como trazados continuos de varias casillas |
| 4 | Fortaleza castellana | Prefab de mapa (66.750 triángulos, atlas de 4096) asignado a Castilla y LOD de revisión | Repetir el flujo con los otros reinos |
| 5 | Misión rápida 24×16 | Jugable: nueva partida, movimiento, combate, ocupación, victoria/derrota y guardado v11 | Héroes |
| 6 | Huestes y héroes | Batallón de nueve soldados en el mapa y tropas con modelo en combate | Héroes en reglas, identidad por facción, rig y animación |
| 7 | Campaña regional | Economía, logística, niebla, diplomacia y rival con IA jugables en la conquista de Castilla | Población, fueros y campañas de los otros once reinos |
| 8 | Mundo | Generador acepta dimensiones variables | Escenario mayor y presupuesto de CPU/GPU/memoria medido |
| 9 | Distribución | Sin implementar | Build reproducible, licencias, accesibilidad y prueba en equipos |

## Próximo trabajo

1. Conservar la medición ya documentada del tablero de 384 casillas (732,5 FPS
   medios en aquella build y máquina). Repetirla tras validar estos cambios;
   no extrapolarla a la rama actual. Los ~61–64 ms corresponden a construcción.
   Ver la [investigación de rendimiento](../v0.7-rendimiento-tablero.md).
2. Medir CPU, GPU, draw calls, memoria y colliders en un mapa mayor. Elegir
   instancing, LOD o carga regional según esos datos.
3. Separar arranque y construcción del tablero de CampaignBoardPreview para
   aceptar escenarios configurables y regenerar sin duplicar recursos. **En curso:**
   `CampaignScenarioDefinition` ya permite parametrizar tamaño, semilla, relleno,
   oro y blockout desde un asset de Unity. El menú contextual permite regenerar
   **solo en Play Mode**: reinicia selección, turno y recursos; desactiva y retira
   la geometría anterior antes de reconstruirla. La previsualización persistente
   en modo edición sigue pendiente.
4. Crear una primera casilla de llanura de producción y validarla a zoom cercano
   y lejano; después extender el lenguaje visual a los otros biomas.
   **Propuesta v01 creada en Blender:** pastizal, cereal y senda, con fuentes y
   FBX. Pendientes materiales URP, prefabs, costuras e integración visual;
   ver [fuentes de llanura](../../ArtSource/Blender/Terrain/README.md).
   Bosque, colina y montaña cuentan también con primeros modelos cálidos,
   FBX y una composición de revisión junto a la llanura. Aún no sustituyen el
   blockout de la escena de campaña.
   La montaña natural v02 recibe aceptación visual del usuario. Se añaden
   bosque, colina y llanura de acabado natural con atlas de color/normal y un
   material por casilla. La integración, los LOD y la medición siguen pendientes.
   Agua, costa, lago, río, cereal y tres piezas de camino cuentan con estudios
   naturales, mapas horneados y composición rural. Siguen pendientes el shader
   de agua animada, variantes de unión, asignación al mapa y validación en Unity.
   La revisión rural v02 mejora orillas, densidad del cereal y relieve de rodadas;
   cuenta con ocho FBX y mapas propios. Es un estudio visual, con mayor densidad
   geométrica: requiere LOD y medición antes de sustituir el blockout.
   El estudio de conexiones de agua v03 añade siete piezas azules de radio 1:
   mar, río recto/curvo, desembocadura, costa, cabo y bahía. Una composición
   enlaza río y mar; se comprueban perfiles de borde de sus vecinos en Blender.
   Falta llevar esos perfiles a datos de vecinos, materiales y prefabs en Unity.
   Se añade un estudio de puente de piedra con arco abierto y accesos hasta el
   borde de casilla. El puente se exporta separado del río para componerlos;
   aún no habilita rutas terrestres sobre agua ni incluye colisiones de juego.
   El molino de agua v01 amplía el estudio de arquitectura rural: edificio y
   rueda se exportan separados, con paleta cálida y presentación junto al río.
   Faltan prefab, animación de rueda, lógica económica, LOD y revisión en Unity.
5. Definir datos lógicos para ríos/caminos y comenzar el bucle de misión,
   preservando las pruebas de paridad del generador. **En curso:**
   `MapRouteGraph` admite tramos explícitos y valida sus extremos; distingue las
   rutas de la simple vecindad. El movimiento terrestre provisional bloquea el
   agua y aplica costes por terreno. Los escenarios ya permiten introducir
   tramos axiales en el Inspector y validarlos con `Validate scenario routes`.
   Se validan antes de sustituir el tablero actual. El prototipo conserva la
   lista vacía; quedan pendientes los trazados de diseño y su representación.
   `MapPathfinder` añade búsqueda A* y el panel permite consultar itinerarios
   con Mayús + clic. Falta validar la interacción en Unity e incorporar huestes,
   puntos de movimiento y ocupación antes de dar por cerrado el movimiento.

Siguen abiertas las decisiones de tamaño de campañas, formato de guardado
Unity, presupuesto de arte y política de Git LFS. Ningún número orientativo de
triángulos o FPS se convierte en requisito sin medir una build representativa.

### Arte: revisión de detalle V04

Los 23 modelos actuales cuentan con una revisión de mayor detalle en
[DetalleV04](../../ArtSource/Blender/DetalleV04/README.md): seis fuentes Blender,
23 FBX y 69 mapas de color, normal y rugosidad. Hay comparativas antes/después
y una herramienta de comprobación de los entregables. Los modelos anteriores
se conservan como referencia.

La siguiente fase técnica de arte es preparar LOD y materiales URP compartidos,
normalizar tamaños de casilla y verificar costuras, pivotes y lectura a las
distancias de cámara del juego. Después se medirá un conjunto representativo
antes de sustituir el blockout. Agua y rueda siguen estáticas; no se da por
terminada la integración ni se atribuyen cifras de rendimiento a este arte.

### Primer kit de fortaleza, huestes y héroes

[Repertorio V01](../../ArtSource/Blender/RepertorioV01/README.md) añade una
fortaleza castellana, lancero, espadachín, ballestero, caballero montado,
capitán, soberana y marcador de hueste con estandarte. Ocho mallas estáticas,
cuatro fuentes Blender y 32 mapas PBR; exportaciones reabiertas y verificadas.

Se conserva la colección de geometría editable con grupos por piezas.
Faltan refinamiento de personajes, identidad de líderes concretos, variantes
de las facciones, esqueleto, animaciones, LOD y materiales/prefabs en Unity.
La fortaleza tiene 17.944 triángulos: supera el presupuesto inicial de 15.000
y requiere reducción antes de integrarse como arte de producción.
La entrega no cierra los hitos jugables ni sustituye las pruebas del juego.

### Revisión natural del repertorio V02

El usuario considera V01 demasiado parecido a Lego. La nueva propuesta
[Repertorio V02](../../ArtSource/Blender/RepertorioV02/README.md) revisa los ocho
modelos con anatomía más proporcionada, superficies continuas, telas con caída,
caballo remallado, escudos convexos y piedra con biseles y cursos alternos.
Conserva V01 para comparar. Fuentes, FBX y mapas están verificados como archivos.

Son estudios densos pendientes de aceptación visual, retopología/reducción,
LOD, rig, animación e integración. Esta revisión no cierra los hitos jugables
ni certifica rendimiento. Las caras y poses aún requieren refinamiento.
