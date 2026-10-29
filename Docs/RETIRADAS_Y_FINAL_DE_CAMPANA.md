# Retiradas y final de campaña

El objetivo **Conquest** de un escenario multihueste activa las retiradas estratégicas y las condiciones de conquista. Sustituye la misión de llegar a una casilla. Los escenarios anteriores con **ReachDestination** conservan sus reglas y guardados de misión; no cambian silenciosamente su tratamiento histórico de las retiradas.

## Salir de una batalla

Al aplicar el resultado, los supervivientes del bando derrotado intentan abandonar su casilla. Se conserva su salud exacta, moral, tipo de tropa e historial. El desplazamiento es de una casilla terrestre vecina, con movimiento restante cero. No añade tropas ni cura heridas.

Una salida válida:

- Admite el paso terrestre y su coste no supera el presupuesto completo de la hueste.
- Está libre de huestes, también aliadas.
- No queda junto a otras huestes o guarniciones hostiles en sus posiciones previas a la ocupación del vencedor.
- No es una ciudad ajena, aunque esté vacía.
- Si es una ciudad propia, respeta las seis plazas entre tropas retiradas, guarnición y leva pendiente.

Entre salidas válidas se prefiere alejarse del enemigo, acercarse a una ciudad propia y, en empate, el orden axial Q/R. El resultado es determinista. El vencedor puede quedar adyacente después de ocupar la casilla abandonada; no recupera movimiento para atacar de nuevo.

Si no hay salida válida, los supervivientes se rinden y salen del juego. La interfaz avisa antes de ejecutar la retirada. Los rendidos se indican aparte de las bajas tácticas: no se inventan muertes en el parte de batalla. Todavía no hay un sistema de prisioneros, rescates o recuperación de esas tropas.

La hueste atacante derrotada también se retira. El defensor vencedor mantiene su posición. En una victoria atacante, éste ocupa la casilla abandonada; si contiene una fortaleza ajena, asume el control y se aplican las reglas de propiedad. Los empates mantienen las posiciones supervivientes. Las retiradas tácticas parciales de un ejército vencedor no separan una hueste adicional.

## Guarnición y hueste juntas

Si había una hueste de campo, sus supervivientes y los de la guarnición se reúnen en ella para escapar, sin duplicar formaciones. Si sólo había guarnición, se crea una hueste con origen registrado en la ciudad. Sale con cero movimiento y puede seleccionarse y guardarse como cualquier otra hueste.

La guarnición abandonada queda vacía. Si se conquista la ciudad, sus levas pendientes se cancelan sin devolución; las obras civiles se conservan. Si no hay salida, se retiran todos los rendidos y se permite la ocupación. Reaplicar el resultado no crea otra hueste, mueve dos veces ni repite la conquista.

## Victoria y derrota

En **Conquest**:

- **Victoria:** queda presencia propia y ninguna ciudad ni hueste de otra facción.
- **Derrota:** no queda ninguna ciudad ni hueste propia.
- Perder la última ciudad no es derrota si escapa una hueste. Puede intentar recuperarla.
- Perder las huestes tampoco es derrota mientras quede una ciudad desde la que reclutar.
- No se decide un resultado final con una batalla todavía pendiente.

Las guarniciones pertenecen a sus ciudades y quedan incluidas en esa presencia territorial. Los rivales sin ciudades pero con tropas supervivientes impiden declarar victoria. El resultado se vuelve a calcular a partir de tropas y propietarios; no se confía en una bandera de victoria arbitraria del guardado.

El panel muestra **VICTORIA** o **DERROTA**. Se detienen marchas y fases rivales y se bloquean movimiento, reclutamiento, construcción y cierre de turnos. Siguen disponibles guardar, cargar, inspeccionar y nueva partida. Una partida anterior en curso restaura también sus marchas y permisos.

## Guardado v9

La identidad añade el objetivo y las reglas estratégicas. Los escenarios de conquista requieren v9. Los de misión siguen leyendo sus versiones anteriores: simple v1/v2/v3/v5/v6/v7/v8/v9 y multihueste v4/v5/v6/v7/v8/v9, respetando además los requisitos existentes de IA activa.

Las huestes evacuadas usan el registro de origen militar y se validan con sus formaciones iniciales o reclutadas. Se preservan heridas, moral, posición, movimiento y encuentros resueltos. Los rendidos no vuelven a aparecer al cargar. Un guardado terminado con marchas pendientes, un objetivo incompatible o un origen de tropas inválido se rechaza antes de sustituir la partida actual.

## Escena de revisión

`CampaignConclusionReview` activa **Objective Kind → Conquest** y los rivales estratégicos, reutilizando el arte existente. Permite probar una retirada real, recuperar una ciudad con supervivientes o combatir hasta un resultado final. La nueva partida conserva ese objetivo.

Las pruebas cubren salida y cerco, heridas y moral, evacuación de guarnición, límites, origen e idempotencia, victoria y derrota, bloqueos y carga de partidas anteriores. El recorrido automático del ejecutable comprueba retirada, fin de campaña, restauración JSON y nueva partida sin escribir la ranura del jugador. La apariencia y comodidad de los avisos siguen pendientes de revisión visual manual.
