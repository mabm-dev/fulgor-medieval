# Combate entre huestes de campaña

Los escenarios multihueste permiten seleccionar una hueste rival y atacarla desde el HUD. La interfaz táctica utiliza las formaciones reales de ambas huestes, con sus atributos, salud parcial y moral. El laboratorio independiente sigue disponible para sus pruebas y partidas propias.

## Inicio y coste

- El atacante debe ser la hueste propia activa y el objetivo debe pertenecer a otra facción.
- Ambos deben ocupar casillas adyacentes, estar libres de otros encuentros y tener al menos un punto de movimiento en el caso del atacante.
- El botón de movimiento cambia a **ATACAR HUESTE ENEMIGA** al seleccionar al rival. Indica si falta acercarse o esperar al siguiente turno.
- Iniciar un ataque consume todo el movimiento restante del atacante y no cobra recursos. Un rechazo no modifica el registro ni el movimiento.
- El ID y la semilla del encuentro se construyen de forma determinista a partir de la campaña, el turno, los participantes y los encuentros ya resueltos. No se usa el hash aleatorio de cadenas del proceso.

## Control táctico

El jugador controla las formaciones de su bando: atacante en sus ataques y defensor cuando un rival estratégico le ataca. El otro bando utiliza el controlador automático compartido. **AUTO AMBOS BANDOS** permite delegar también al atacante; puede pausarse. Las órdenes manuales del jugador no se aceptan durante una activación enemiga.

Mover, atacar, esperar y defender utilizan el mismo ejecutor de órdenes que el laboratorio. **RETIRAR MI HUESTE** permite retirar el bando del jugador conservando los supervivientes y heridas, incluso durante una activación rival. No se reinicia una batalla de campaña ni se reemplaza por un guardado del laboratorio.

La ejecución automática se limita a bloques de 1000 activaciones. Si no termina, no inventa un vencedor. Puede reanudarse otro bloque o retirarse la hueste propia.

## Volver al mapa y aplicar el resultado

**VOLVER A CAMPAÑA** durante una batalla la deja pendiente. El mapa muestra **VOLVER A LA BATALLA** y conserva el estado táctico exacto en memoria. Mientras exista un encuentro pendiente se bloquean movimiento estratégico, construcción, cierre de turno y guardado/carga de campaña. Se puede inspeccionar el mapa o iniciar expresamente una nueva partida.

Al terminar se muestra el parte de ambos bandos. **APLICAR Y VOLVER** reconcilia los resultados una sola vez: conserva salud y moral exactas de los supervivientes, elimina formaciones y huestes destruidas, actualiza sus marcadores y libera los bloqueos. La hueste atacante conserva el movimiento gastado; su siguiente ataque requiere reponerlo al cerrar el turno.

En campo abierto se mantienen las posiciones estratégicas. Si se combate sobre una fortaleza y el defensor es destruido, el atacante vencedor ocupa la casilla y asume el control como parte de la resolución. Un defensor retirado vivo todavía bloquea la entrada: no se calcula una ruta estratégica de retirada. Consulta [Conquista y propiedad](CONQUISTA_Y_PROPIEDAD.md). Ganar una batalla o capturar una fortaleza no equivale todavía a ganar la campaña.

## Persistencia y límites

Los guardados v4 conservan el registro resultante, las bajas, heridas, moral, movimiento y el historial de encuentros aplicados. Las batallas abiertas todavía requieren resolverse antes de guardar. El parte visible es temporal; sus efectos e historial sí quedan en el guardado.

La escena de revisión `Assets/Fulgor/Scenes/ArtReview/CampaignCombatReview.unity` coloca dos huestes propias y una rival adyacente a la primera. Reutiliza el mapa, terreno y ciudad existentes. `CampaignCombatReviewBuilder.PrepareAndBuild` prepara y compila esta escena. Su modo de revisión explícito comprueba una batalla automática y la restauración del registro sin escribir la ranura del usuario.

La interfaz táctica sigue siendo esquemática. Quedan fuera de esta entrega la conquista, las guarniciones, la IA estratégica, los héroes, la retirada sobre el mapa y la representación 3D del combate.

## Validación

345 pruebas EditMode completas superadas, sin fallos ni omitidas, ejecutadas en una copia independiente del proyecto. Incluyen las 15 pruebas de integración de combate y las 23 de conquista y propiedad, además del resto de la batería. Los informes y ejecutables se conservan fuera del repositorio.

La comprobación automática de la build verifica el recorrido y sus consecuencias. La revisión visual manual de la ventana del ejecutable queda pendiente: la captura de pantalla en segundo plano devuelve imágenes vacías en la sesión de validación disponible.

## Guarniciones de ciudad

Una plaza defendida muestra ATACAR GUARNICIÓN. Se combinan hasta seis formaciones entre guarnición y hueste estacionada, conservando la procedencia de cada una para devolver las bajas y supervivientes a su destino original. Una retirada no convierte ni elimina tropas. El defensor temporal no se guarda como hueste. Consulta [Reclutamiento y guarniciones](RECLUTAMIENTO_Y_GUARNICIONES.md).

## Ataques rivales

El [rival estratégico](RIVAL_ESTRATEGICO.md) puede iniciar un ataque en su fase. La interfaz asigna el control defensor, conserva el azul para las tropas del jugador y reanuda las acciones rivales restantes después de aplicar el resultado. No se cobra otra vez el cierre de turno.

## Resolución estratégica de conquista

Con Objective Kind = Conquest se aplican [retiradas estratégicas](RETIRADAS_Y_FINAL_DE_CAMPANA.md): salida vecina con heridas y moral conservadas, o rendición sin salida. La guarnición puede evacuar como hueste. El panel avisa de la salida prevista y separa rendidos de bajas tácticas. Las misiones anteriores conservan sus reglas.
