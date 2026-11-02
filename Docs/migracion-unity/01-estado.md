# Estado comprobado de la migración

> Instantánea del 21 de octubre de 2026, antes de la campaña jugable. El estado
> vigente está en el [README](../../README.md), el [roadmap](../ROADMAP.md) y el
> [plan de migración](04-plan.md).

Revisado el 21 de octubre de 2026 a partir del proyecto Unity 6000.6.0f1
y una copia aislada de Assets, Packages y ProjectSettings para validación batch.

| Área | Estado comprobado | Límite |
|---|---|---|
| Core | Coordenadas axiales, Mulberry32 y mapa procedural determinista | No están portados los demás sistemas de juego |
| Paridad del generador | Semilla 12345, 24×16, firma 49AD3DF7 | No demuestra paridad de la campaña completa |
| Tablero | 384 casillas, cinco tipos de terreno, oro, selección, cámara y panel | Prototipo de presentación, sin bucle de juego |
| Relieve y bordes | Blockout vertical, uniones, esquinas y costas; clasificación visual de mar, lago y río | Variantes de agua y llanura aún iguales; ríos no son datos lógicos |
| Blender–Unity | Plantilla y FBX de validación; importador de modelos estáticos con escala y ejes controlados | No son recursos artísticos definitivos |
| Pruebas | 94/94 Edit Mode en copia aislada y en integración continua, sin gráficos | Sin pruebas Play Mode |
| Build de Windows | Revisión visual completa: cámara cercana y lejana, costa, oro, hover, selección y panel | 384 casillas y 1746 renderers; ~61–64 ms de construcción del tablero, no de cada fotograma; medición de referencia disponible en el informe de rendimiento |

La escena CampaignPrototype serializa semilla 12345, densidad 0.97, oro visible
y blockout vertical activo. El lote con -nographics valida lógica, importación
e integración de escena; el aspecto se comprobó aparte en la build de Windows.
Existe una medición de referencia de FPS, CPU y GPU para una build anterior,
recogida en la [investigación de rendimiento](../v0.7-rendimiento-tablero.md).
Debe repetirse tras validar los cambios locales; no certifica el rendimiento
de la rama actual.

Las 384 casillas corresponden a la misión pequeña y al laboratorio de migración.
No constituyen el mapa peninsular final. Los modelos de plantilla representan
escala y pivotes de hexágono, fortaleza y hueste, no arte de producción.

Queda pendiente medir una build con un mapa mayor que 384 casillas.

## Cambios locales revisados, aún pendientes de integración

- Turnos e ingresos provisionales según los terrenos del mapa.
- Escenario configurable con asset y regeneración exclusiva de Play Mode.
  Regenerar inicia una campaña nueva y limpia selección y geometría anterior.
- Rutas explícitas con validación de extremos y consulta de movimiento
  terrestre entre vecinos. No se generan carreteras ni ríos automáticamente.
  Los tramos pueden configurarse en el asset del escenario y se validan antes
  de regenerar. Ver [escenarios y rutas](05-escenarios-y-rutas.md).
- Búsqueda de itinerarios de coste mínimo con A* y consulta de pasos/coste
  desde el panel mediante Mayús + clic. Pendiente de prueba en Unity; todavía
  no mueve huestes ni contempla ocupación o puntos de movimiento.
- Compilación externa de Core y Presentation comprobada con referencias de
  Unity 6000.6.0f1. Esto no sustituye las pruebas de Unity ni la revisión visual,
  que se mantienen pendientes. Los resultados históricos de pruebas anteriores
  no validan estas incorporaciones.

## Arte añadido el 22 de octubre de 2026

`ArtSource/Blender/RepertorioV01/` incorpora el primer kit de fortaleza,
cuatro tropas, dos arquetipos de héroe y marcador de hueste. Se verifican
ocho FBX, cuatro fuentes Blender y 32 mapas; se conservan fuentes editables.
Son poses estáticas pendientes de refinamiento, rig, animación, LOD,
materiales/prefabs y revisión en Unity. No cambian los resultados históricos
de pruebas ni añaden movimiento o combate al juego.

## Movimiento provisional de hueste

Se añade ArmyMovementState con presupuesto, movimiento completo o rechazo y reposición de puntos por turno. El controlador y el panel ya conectan selección, orden y fin de turno. Nueve comprobaciones externas de lógica pasan; Core y Presentation compilan con cero errores. Pendiente Play Mode, marcador de mapa, ocupación y combate. Detalles en 05-escenarios-y-rutas.md. No modifica los resultados históricos de pruebas Unity.

## Ampliación del bucle de misión (pendiente de prueba en Unity)

El prototipo dispone de hueste con puntos de movimiento y marcador, objetivo accesible y victoria, guardar/cargar local, localizadores de cámara, nueva partida y órdenes de marcha de varios turnos con cancelación. La compilación externa de Core y Presentation es correcta. La validación de interacción, persistencia y ciclo completo en Unity sigue pendiente. Las pruebas externas anteriores no validan automáticamente las marchas recién añadidas. Véase 05-escenarios-y-rutas.md para el alcance y las comprobaciones pendientes. Siguen sin implementarse rival, ocupación entre varias huestes, combate y derrota.

## Primera base de combate táctico

BattleTurnOrder implementa orden por bando/tipo/posición visual, espera una vez por ronda, defensa +2 hasta próxima activación y exclusión de formaciones retiradas o eliminadas. Cierra la cola cuando un bando queda sin formaciones. Compila externamente; pruebas aplazadas. Todavía sin tropas, daño, tablero táctico ni integración de encuentros con campaña. Véase 06-combate-tactico.md.
