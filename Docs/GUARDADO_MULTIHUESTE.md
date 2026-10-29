# Guardado de campañas con varias huestes

GUARDAR y CARGAR están disponibles en los escenarios que activan el despliegue de varias huestes. La misma partida conserva:

- Turno y reservas del reino.
- Todas las huestes supervivientes, propias y ajenas, con posición y movimiento restante.
- Cada formación superviviente, con salud exacta y moral. No se redondean las heridas a tropas completas.
- Hueste propia seleccionada y misión completada, aunque otra hueste estuviera seleccionada al guardar.
- Edificios terminados y obras pendientes de las ciudades. Su economía se vuelve a derivar de los edificios al cargar.
- Identificadores de combates resueltos, tanto del registro como de cada hueste, para impedir su reaplicación.

Las huestes y formaciones eliminadas permanecen eliminadas. Es posible guardar una campaña sin huestes propias, incluso con el registro vacío. Cargar también descarta las animaciones de movimiento que pertenecían a la partida reemplazada.

## Compatibilidad y validación

El controlador escribe **v9** en ambos modos, con propiedad, guarniciones, reclutamiento, marchas individuales, reservas de los rivales y un indicador explícito simple/multihueste. Sigue leyendo v1/v2/v3/v5/v6/v7/v8 en modo simple y v4/v5/v6/v7/v8 en multihueste; las versiones anteriores a v5 restauran los propietarios iniciales del escenario; las anteriores a v6 no contienen guarniciones ni levas. No se convierte una partida de un modo al otro ni se interpreta una sola hueste como un ejército completo. Consulta [Conquista y propiedad](CONQUISTA_Y_PROPIEDAD.md).

La identidad de una campaña multihueste incluye mapa, rutas, objetivo, facción del jugador, despliegues iniciales, capacidades de movimiento, formaciones y atributos de tropas. Cambiar esas reglas requiere una migración futura del guardado. El orden de declaración de huestes o formaciones no cambia la identidad.

Los datos se validan y reconstruyen antes de sustituir la partida abierta: IDs conocidos y sin duplicados, posiciones terrestres accesibles desde el origen de cada hueste y sin solapamiento, movimiento dentro del presupuesto, salud y moral válidas, historial coherente, selección propia, edificios y obras válidos. Un error conserva la campaña actual y muestra el motivo general de incompatibilidad en el HUD.

No se serializa una batalla táctica en curso. Mientras exista un encuentro sin resolver se bloquean GUARDAR y CARGAR; una llamada directa tampoco sustituye la ranura ni el combate. Esta limitación es deliberada hasta guardar el estado completo del motor táctico, incluidos activaciones y generador aleatorio.

Se mantiene **una ranura local compartida**, sin autoguardado, copias rotatorias ni sincronización. Guardar reemplaza su contenido. La firma del escenario sirve para detectar incompatibilidad; no es una protección contra la edición intencionada de partidas.

## Escena de revisión

`Assets/Fulgor/Scenes/ArtReview/CampaignMultiArmyReview.unity` utiliza el mapa y los edificios existentes con dos huestes de Castilla y una rival. Se prepara mediante `MultiArmyReviewBuilder.Prepare` y se puede compilar por separado con `MultiArmyReviewBuilder.Build`. La escena principal conserva su configuración anterior.

1. Seleccionar un destino libre próximo y mover la primera hueste.
2. Pulsar SIGUIENTE HUESTE y GUARDAR.
3. Mover o terminar turnos y pulsar CARGAR.
4. Comprobar posición, movimiento gastado, hueste seleccionada y reservas.
5. En la ciudad, guardar con una obra pendiente y repetir la carga: el progreso y los ingresos no se duplican.

Los marcadores son los provisionales existentes. El escenario no añade IA estratégica, reclutamiento ni marcha multihueste de varios turnos. El [combate de campaña](COMBATE_EN_CAMPANA.md) ya permite abrir encuentros desde el mapa, usar las tropas reales y aplicar sus resultados. El laboratorio conserva sus partidas independientes.

## Verificación

La batería local completa de Unity supera 345 pruebas EditMode, sin fallos ni omitidas, incluyendo persistencia, combate, conquista y economía. Esta ejecución no sustituye a una validación remota de integración continua.

Las pruebas de Core cubren persistencia, bajas, heridas, moral, historial de batalla, registro vacío, independencia de la instantánea y rechazo de datos inválidos. Las pruebas de escena ejercitan los botones reales, la serialización JSON, la restauración conjunta de economía y obras, la selección, los marcadores, la victoria y el rechazo sin mutar la partida actual. Las pruebas conservan y restauran la ranura local previa.

La decisión de formato está documentada en [ADR-004](decisiones/ADR-004-guardado-multihueste.md). Los resultados, capturas y ejecutables de validación se conservan fuera del repositorio.

## Tropas reclutadas y trasladadas

La versión v6 guarda los orígenes de reclutas y huestes nuevas y permite que una formación superviviente cambie de hueste o permanezca en guarnición. Se valida la unicidad global, la facción, la salud máxima, los turnos y la capacidad de defensa antes de publicar la carga. Consulta [Reclutamiento y guarniciones](RECLUTAMIENTO_Y_GUARNICIONES.md).

## Órdenes de marcha

La versión v7 conserva los destinos individuales, valida su cantidad y restaura los puntos gastados sin ejecutar otro tramo. Una ruta temporalmente bloqueada puede cargarse en espera. Consulta [Marchas multihueste](MARCHAS_MULTIHUESTE.md).

## Rivales estratégicos

El escritor v8 conserva las reservas rivales y el turno económico cerrado. Su activación forma parte de la firma. Los escenarios con IA activa requieren v8 y los anteriores siguen cargando con IA desactivada. Las fases rivales pendientes bloquean guardar y cargar. Consulta [Rival estratégico](RIVAL_ESTRATEGICO.md).

## Objetivo de conquista y retiradas

El escritor v9 incorpora la identidad del objetivo. Conquest requiere v9 y conserva las huestes evacuadas mediante el registro de origen militar. El final se deriva de la presencia actual y un guardado terminado no puede contener marchas pendientes. Las misiones anteriores conservan su compatibilidad. Consulta [Retiradas y final de campaña](RETIRADAS_Y_FINAL_DE_CAMPANA.md).
