# Escenarios y rutas del prototipo

El asset `Assets/Fulgor/Scenarios/CampaignPrototype.asset` configura la escena
actual. Permite elegir tamaño, semilla, relleno, oro y relieve de blockout.
Se pueden crear otros assets desde **Create > Fulgor > Campaign Scenario** y
asignarlos al campo Scenario del componente CampaignBoardPreview.

## Introducir rutas

En la lista **Routes** del Inspector, cada elemento es un tramo entre dos
casillas vecinas. From y To usan coordenadas axiales: X es Q, Y es R.

- Road une dos casillas terrestres. Reduce a 1 el coste de entrada en bosque,
  colina o montaña; la llanura ya cuesta 1.
- RiverBank marca un borde entre tierra y agua. No permite atravesar agua ni
  define por sí mismo un río continuo. No equivale a una ruta naval o un puente.

No se infieren rutas por proximidad. Los tramos son bidireccionales y los
duplicados invertidos se cuentan una sola vez. La lista del escenario actual
está vacía hasta definir trazados adecuados para su semilla.

El menú contextual del asset ofrece **Validate scenario routes**. Genera el
mapa lógico y valida los tramos, sin construir objetos de escena. Detecta
extremos fuera del mapa, casillas no vecinas y tipos incompatibles con el
terreno. Cambiar la semilla o las dimensiones puede invalidar rutas existentes.

## Aplicar cambios

En Play Mode, usa **Regenerate board (Play Mode)** en CampaignBoardPreview.
El mapa y sus rutas se validan antes de retirar la geometría anterior. Un
error en esos datos conserva el tablero anterior. Una regeneración correcta
reinicia el turno, los recursos y la selección.

## Consultar itinerarios

En Play Mode, selecciona una casilla de origen con clic y usa **Mayús + clic**
en otra casilla. El panel muestra origen, destino, pasos y coste total, o indica
que no existe un itinerario terrestre. La casilla de origen permanece
seleccionada para poder consultar otros destinos. Esc o un clic normal limpian
la consulta; regenerar también la descarta.

`MapPathfinder` calcula un itinerario de coste mínimo con A*, sin depender de
Unity. Evita agua y aprovecha carreteras explícitas; los empates se resuelven
de forma determinista. El resultado incluye ambos extremos y el coste de entrar
en cada casilla recorrida. Origen igual a destino devuelve cero pasos y coste.
Las riberas no habilitan cruces de agua.

El panel solo consulta rutas: aún no descuenta puntos de movimiento ni tiene en
cuenta unidades, ocupación, enemigos, suministro o zonas de control. El dibujo
del trazado y las órdenes de movimiento de huestes siguen pendientes. Estas
incorporaciones requieren revisión en Unity; la compilación externa no verifica
el Inspector ni el juego.

## Movimiento de una hueste — 22 de octubre de 2026

ArmyMovementState incorpora posición y presupuesto inmutable de movimiento terrestre. TryMove ejecuta la ruta completa o rechaza la orden sin consumir puntos; usa costes de terreno y carreteras de MapRouteGraph. BeginTurn repone el presupuesto sin acumularlo. Nueve casos NUnit comprobados mediante ejecución externa .NET (incluido rodeo más barato, agua, presupuesto agotado y estado anterior intacto). No equivale a una ejecución Edit Mode en Unity.

CampaignTurnController crea una hueste con 6 puntos en la primera casilla terrestre válida del escenario. El panel muestra posición, puntos, coste y razón de rechazo; seleccionar una casilla habilita MOVER A LA CASILLA SELECCIONADA solo si la orden es válida. Fin de turno repone los puntos junto con el ingreso provisional. La regeneración inicia una hueste nueva y sustituye las suscripciones de botones y selección. Un mapa sin tierra deja el movimiento desactivado.

Core y Presentation compilan externamente con las referencias locales de Unity: cero errores, cinco avisos de campos serializados existentes. Falta verificar interacción y regeneración en Play Mode. La hueste se representa por coordenadas en el panel: todavía no tiene marcador en el mapa, ocupación de casillas, facciones, combate ni rutas de varios turnos. El origen provisional no es una decisión final de diseño de misión.

## Marcador funcional de hueste

CampaignArmyMarker añade un estandarte azul provisional de tres piezas y un material compartido. Sigue el estado de la única hueste al iniciar, mover y cambiar de turno. Se coloca sobre el límite superior del collider de la casilla para evitar quedar enterrado en el relieve. Sus colliders se deshabilitan y se retiran; las piezas usan Ignore Raycast para conservar la selección del terreno.

Al regenerar, reconstruye la referencia a las casillas activas del tablero y reutiliza el marcador. Se oculta si no hay hueste o no existe su casilla. Libera su material al destruirse. Compilación externa Core y Presentation sin errores; comportamiento y escala pendientes de Play Mode. No añade animación, facciones ni combate. Las métricas actuales de construcción del tablero se registran antes de este marcador: sus tres renderers y su material no forman parte de ese recuento.

## Primera misión: alcanzar un destino

ReachDestinationMission define un objetivo terrestre diferente del inicio de la hueste. Por defecto recorre la componente accesible mediante BFS y elige la coordenada de mayor distancia axial al inicio; no significa la ruta de mayor coste. Empates deterministas según el orden de vecinos. Si no hay otra casilla accesible, el escenario conserva exploración libre sin objetivo imposible.

CampaignScenarioDefinition permite activar useExplicitDestination e introducir destination (X=Q, Y=R). El destino debe pertenecer a la componente terrestre del inicio y ser distinto de él. Se valida antes de retirar el tablero existente, y también con el menú de validación del escenario.

Al mover la hueste al destino, el panel muestra victoria y el controlador impide más movimientos y turnos. Regenerar crea una misión nueva. El objetivo se indica por coordenadas en el panel; todavía no tiene marcador propio ni combate, derrota, límite de turnos o guardado.

Verificación: 13 comprobaciones NUnit de movimiento y misión ejecutadas externamente con .NET, todas correctas. Core y Presentation compilan con cero errores; siete avisos de campos serializados. Pendiente comprobar el ciclo completo y regeneración en Play Mode. Estas comprobaciones no se suman a los resultados históricos de Edit Mode Unity.

## Guardado local del prototipo

Botones GUARDAR y CARGAR en el panel de turno. Una ranura local PlayerPrefs (Fulgor.Campaign.Prototype.Save.v1), serializada con JsonUtility. Guardar sustituye esa ranura. Se conserva turno, los cinco recursos, posición de hueste y puntos de movimiento gastados. Los ingresos se obtienen del escenario activo; la victoria se reconstruye desde la posición y el objetivo inicial. No guarda selección, cámara, preferencias gráficas ni geometría.

CampaignSaveData versión 1 valida formato, firma de escenario (terrenos, oro, rutas, dimensiones, semilla y objetivo), turno, recursos, presupuesto de movimiento y posición accesible desde el inicio. Primero construye ambos estados restaurados y después el controlador los aplica. Una carga incompatible informa del error sin sustituir los estados actuales. No genera otro escenario al cargar: hay que abrir el mismo escenario. Sin hueste terrestre, los botones se deshabilitan. La regeneración no elimina el guardado.

Verificado: 18 comprobaciones de lógica (movimiento, misión y guardado) ejecutadas externamente con .NET, todas correctas; Core y Presentation compilan sin errores. Pendiente verificar JsonUtility, persistencia PlayerPrefs entre sesiones, botones, marcador y restauración tras victoria en Unity Play Mode. El formato es provisional y no promete compatibilidad con futuras versiones del juego.

## Controles del ciclo de misión

El panel incorpora VER HUESTE, VER OBJETIVO y NUEVA PARTIDA. Los localizadores seleccionan la casilla correspondiente, limpian la consulta de ruta anterior y acercan la cámara hasta zoom 6 o mantienen uno más cercano, respetando los límites existentes del tablero. Son operaciones de navegación: no gastan movimiento ni ejecutan órdenes. Funcionan también tras victoria. Se deshabilitan si falta la hueste o el objetivo.

NUEVA PARTIDA utiliza RegenerateBoard: mismo escenario y semilla, turno 1, recursos iniciales, hueste y misión nuevas. Conserva la ranura local guardada y permite iniciar de nuevo tras victoria. Los botones retiran sus listeners al reinicializar para evitar órdenes duplicadas. La validación de rutas y objetivo sigue ocurriendo antes de sustituir el tablero.

Compilación externa de Core y Presentation: cero errores y siete avisos de campos serializados. Pendiente Play Mode: localizar en extremos del mapa y con distintas resoluciones, mover y agotar puntos, victoria, guardar/cargar, reiniciar y repetir para detectar suscripciones duplicadas. El panel crece a 578 unidades de referencia; falta verificar su ajuste en pantallas pequeñas.

## Órdenes de marcha de varios turnos

El botón de movimiento pasa a MARCHAR A LA CASILLA SELECCIONADA. Un destino terrestre accesible puede aceptarse aunque su coste total exceda los puntos actuales. ArmyMovementState.TryAdvance recorre pasos completos de una ruta mínima hasta agotar el presupuesto disponible; nunca paga parcialmente una entrada ni salta casillas. Si no cabe el siguiente paso, espera. El controlador conserva MarchDestination y continúa después de reponer puntos con FIN DE TURNO.

CANCELAR MARCHA retira la orden sin mover la hueste ni devolver puntos gastados. Elegir un destino diferente sustituye la orden anterior; una orden inalcanzable no la sustituye. Llegar al destino cancela la orden. Alcanzar el objetivo de misión durante el recorrido detiene la hueste y declara victoria, aunque la orden apuntase más lejos. Nueva partida limpia la marcha.

El payload de guardado pasa a versión 2 conservando la ranura PlayerPrefs existente. Lee también versión 1, interpretada sin marcha pendiente. Guarda y valida el destino pendiente antes de aplicar el estado; cargar no ejecuta por sí mismo la marcha. La marcha continúa al siguiente FIN DE TURNO. Una partida completada no admite una orden pendiente. El panel indica el destino pendiente y permite cancelarlo.

Verificación de esta incorporación: Core y Presentation compilan externamente sin errores; siete avisos de campos serializados. No se han ejecutado nuevas pruebas de juego, siguiendo la preferencia del usuario de hacerlas más adelante. Pendientes: agotamiento con costes mixtos, cancelación, sustitución, guardar/cargar a mitad de marcha, objetivo situado en el camino y ajuste del panel (620 unidades de referencia).
