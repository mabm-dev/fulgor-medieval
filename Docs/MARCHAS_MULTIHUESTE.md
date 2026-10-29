# Marchas de varias huestes

Un escenario multihueste permite ordenar un destino lejano a cada hueste propia. El primer tramo se ejecuta al dar la orden, usando el movimiento restante. Los siguientes se ejecutan después de cerrar el turno y reponer movimiento. No se cobran recursos por marchar.

## Órdenes y costes

- Seleccionar una hueste propia la hace activa. Seleccionar un destino conserva esa hueste y muestra el coste de ruta; si supera su movimiento, indica **varios turnos**.
- Se avanza por casillas completas y se descuenta el coste real de terreno o camino. No se acumulan puntos de un turno a otro para cruzar un paso inasequible.
- Una hueste sin movimiento puede recibir una orden para el siguiente turno. Dar otra orden no repone movimiento.
- Cada hueste conserva su destino al seleccionar otra. **Cancelar marcha** cancela solamente la orden de la hueste activa.
- Si se alcanza el destino, la orden se retira. Cargar una partida restaura la posición y los puntos guardados sin ejecutar otro tramo.

Los itinerarios excluyen pasos cuyo coste individual supere el presupuesto completo de la hueste. Se busca una alternativa transitable si existe. La propiedad, las guarniciones y la capacidad de defensa se comprueban también en los destinos intermedios.

## Bloqueos y prioridad

La ruta se recalcula contra las posiciones actuales antes de cada tramo. No se atraviesa otra hueste, aunque sea aliada, ni una guarnición hostil. Las fortalezas propias que no puedan admitir las formaciones de la hueste también bloquean el paso. Si hay una ruta alternativa, se utiliza; si no, la orden permanece esperando y el panel muestra el motivo. Cuando el paso queda libre, se reanuda al cerrar otro turno.

Las órdenes se procesan una vez por cierre, por identificador de hueste en orden ordinal. Cada hueste ve las posiciones ya actualizadas por las anteriores. No hay movimientos simultáneos, intercambio de casillas ni dos huestes en un destino. Si una hueste posterior libera un paso, la que ya había esperado vuelve a intentarlo el próximo turno.

Una orden de marcha no inicia una batalla por su cuenta. Atacar una hueste o guarnición rival sigue siendo una acción manual y cancela la marcha del atacante cuando el combate empieza. Una batalla pendiente congela todas las marchas y bloquea el cierre del turno. Tras resolverla se eliminan las órdenes de huestes que ya no existan.

## Fortalezas, objetivo y traslados

Al entrar en una fortaleza ajena vacía, el tramo termina allí para publicar la ocupación y sus efectos económicos. Si el destino estaba más lejos, la orden continúa pendiente para otro turno. Una plaza defendida exige un ataque manual. No se ignoran tropas supervivientes ni se conceden capturas por atravesar una casilla.

Al alcanzar el objetivo de la misión se detiene la hueste en esa casilla y se cancelan las demás marchas. Las huestes posteriores no avanzan después de la victoria. Depositar o recibir una formación cancela la orden de esa hueste, además de consumir el movimiento según las reglas de traslado militar. Crear una nueva hueste no cancela la marcha de otra.

## Guardado v7

La versión v7 incluye los destinos por hueste y su cantidad declarada, además de los datos militares v6. La cantidad permite detectar una lista truncada o ausente incluso cuando el serializador de Unity la convierte en una colección vacía. La firma de escenario incluye las reglas de marcha.

Al cargar se valida la existencia, facción y unicidad de cada hueste, el destino terrestre accesible y que todavía no haya llegado. Los ocupantes temporales no invalidan una orden legítima en espera. La carga completa se reconstruye antes de sustituir la partida actual.

Las partidas simples leen v1/v2/v3/v5/v6/v7/v8/v9 y conservan su marcha individual anterior. Las multihueste leen v4/v5/v6/v7/v8/v9. Las versiones anteriores a v7 parten sin órdenes de varias huestes; no se convierten partidas entre modos. Se conservan la propiedad, economía, levas, guarniciones y supervivientes introducidos por las versiones anteriores.

## Revisión

La escena `CampaignMarchReview` reutiliza los modelos existentes y reduce a dos los puntos por turno de las huestes propias para observar los tramos. Seleccionar un destino lejano, dar la orden, cambiar de hueste, cancelar y cerrar turnos permite comprobar el ciclo. El recorrido automático opcional del ejecutable utiliza un argumento explícito y no escribe en la ranura del jugador.

Las pruebas cubren costes, pasos imposibles, rutas alternativas, bloqueos, prioridad, objetivo, ocupación intermedia, cancelación, guardado y recuperación atómica ante datos dañados. La revisión visual manual sigue siendo necesaria para valorar legibilidad y animación.

## Rival estratégico

El escritor actual v8 añade [rivales estratégicos](RIVAL_ESTRATEGICO.md). Sus decisiones son independientes de las órdenes guardadas del jugador y utilizan las mismas rutas y presupuestos. Las marchas propias no inician ataques automáticamente; una IA activa sí puede decidir atacar durante su fase separada.

El modo Conquest v9 detiene las órdenes al terminar la campaña y conserva las tropas evacuadas según las reglas de [retirada y final](RETIRADAS_Y_FINAL_DE_CAMPANA.md).
