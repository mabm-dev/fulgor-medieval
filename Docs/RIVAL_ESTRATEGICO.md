# Rival estratégico básico

Los escenarios multihueste pueden activar **Enable Strategic Rivals**. Se controlan las facciones iniciales distintas del jugador, conocidas por sus huestes y ciudades. Las escenas anteriores mantienen la opción desactivada; no cambian sus turnos al actualizar el código.

## Economía y formación

Cada rival empieza con reservas cero, igual que el jugador. Cobra el territorio asignado a sus ciudades por las reglas existentes y los ingresos de sus edificios; paga su mantenimiento civil. Una facción sin ciudades no recibe el ingreso de todo el mapa como sustitución.

El reclutamiento usa el mismo catálogo, costes y plazos que el jugador. La IA rota su perfil preferido y prueba los otros lotes si no puede pagarlo. No crea soldados directamente ni dispone de reservas gratuitas. Respeta una leva por ciudad y las seis plazas de defensa, incluida la reserva de una leva pendiente.

Las tropas terminadas refuerzan una hueste estacionada si tiene espacio. Si la casilla está libre, pueden formar una nueva hueste: la IA conserva una formación de guarnición cuando tiene tropas suficientes y otra hueste, y limita a tres sus huestes de nueva creación por facción. Si no le quedan huestes puede sacar su única formación para recuperarse. Los traslados consumen movimiento y las nuevas huestes salen con cero puntos, según las reglas compartidas.

Todavía no construye edificios civiles automáticamente. Tampoco se ha añadido mantenimiento militar periódico, suministro ni curación; se mantienen las mismas limitaciones que para el jugador.

## Decisiones y movimiento

La IA prioriza atacar un objetivo adyacente razonable, ocupar una fortaleza vacía del jugador y acercarse a sus plazas defendidas o huestes. Estima la fuerza a partir de supervivientes y atributos; evita ataques contra una defensa claramente superior. La estimación incluye guarnición y hueste estacionada y no simula el resultado futuro ni consulta la semilla del combate.

Las rutas y costes son los ordinarios. Evita casillas ocupadas y guarniciones hostiles, usa pasos completos y no recupera movimiento al decidir. Cada hueste recibe como máximo una acción estratégica por fase: atacar o avanzar. No ataca otra vez después de una marcha en esa misma fase. Las decisiones son deterministas para un mismo estado.

El tablero actual es visible; no existe niebla de guerra. Es una política inicial dirigida al jugador, sin diplomacia, alianzas, estrategia avanzada ni objetivos entre rivales. Una plaza ajena vacía encontrada al marchar sigue las reglas generales de ocupación.

## Orden del turno y batallas

1. Cerrar ingresos y mantenimiento del jugador y de los rivales, una sola vez.
2. Reponer movimiento, terminar levas y obras y continuar las marchas propias.
3. Si la misión no terminó, preparar levas y refuerzos rivales.
4. Actuar con las huestes rivales por identificador en orden ordinal.

Un ataque rival abre el combate real y pausa esta fase. **El jugador controla al defensor** y la IA dirige al atacante. Sus tropas se muestran en azul, tanto al atacar como al defender. Las órdenes manuales y la retirada se aplican a su bando; puede delegar ambos bandos con el automático ya existente.

Volver durante un combate lo deja pendiente. Turno, movimiento, construcción, reclutamiento y guardado quedan bloqueados. Al terminar y aplicar el resultado, la fase continúa con la siguiente hueste sin repetir ingresos, levas ni acciones ya consumidas. También funciona con una defensa formada exclusivamente por guarnición.

Si un atacante rival destruye a todos los defensores, ocupa la fortaleza, cambia su propietario y modifica los ingresos futuros del jugador. Las obras civiles se conservan y las levas del anterior dueño se cancelan sin devolución. Los defensores retirados vivos siguen bloqueando la ocupación: la retirada estratégica con desplazamiento todavía no está implementada.

## Guardado v8 y límites

Se guardan activación de la IA, facciones, reservas y turno económico cerrado, además de huestes, propiedad, levas, guarniciones y marchas. Se valida el conjunto antes de sustituir la partida. No se admite guardar ni cargar durante una fase rival pendiente.

La firma incluye la activación y reglas de IA. Las partidas anteriores a v8 siguen cargando en escenarios sin IA: los formatos simples compatibles son v1/v2/v3/v5/v6/v7/v8 y los multihueste v4/v5/v6/v7/v8. Un escenario con rivales activos requiere v8; no se inventan reservas pasadas al migrar. Una carga incompatible conserva la partida actual.

El objetivo de victoria es el configurado por el escenario. El modo Conquest v9 añade [retiradas y final de campaña](RETIRADAS_Y_FINAL_DE_CAMPANA.md). No hay diplomacia ni ajustes definitivos de equilibrio.

## Revisión

`CampaignRivalReview` reutiliza las fortalezas y tropas existentes. Contiene dos huestes propias y dos rivales: una próxima y una reserva lejana. Fin de turno permite observar movimiento y ataques; el panel táctico identifica si se controla al atacante o al defensor. El recorrido automático opcional comprueba defensas, continuidad, costes de levas, tropas terminadas y restauración JSON sin escribir la ranura del jugador. La revisión visual manual sigue pendiente.

En Conquest, un defensor derrotado evacua la casilla si hay salida o se rinde si está cercado. El rival puede ocuparla; la fase se detiene si esa acción termina la campaña. El comportamiento de retirada estacionaria descrito arriba se mantiene sólo en los escenarios anteriores de misión.
