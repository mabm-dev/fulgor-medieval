# Registro de huestes y encuentros estratégicos

**Estado actual:** el [guardado v7](../GUARDADO_MULTIHUESTE.md) conserva huestes, supervivientes, ciudades, propiedad, guarniciones, reclutamiento y órdenes de marcha. El [combate en campaña](../COMBATE_EN_CAMPANA.md) usa tropas reales. Las [marchas multihueste](../MARCHAS_MULTIHUESTE.md) permiten destinos de varios turnos con prioridad determinista y espera ante bloqueos. Las secciones siguientes describen etapas históricas y sus limitaciones pendientes quedan sustituidas por esta actualización.

CampaignArmyRegistry reúne huestes persistentes y estados de movimiento sobre el mismo MapRouteGraph. La construcción exige IDs distintos, huestes con supervivientes, casillas terrestres y posiciones sin solapamiento. Las vistas públicas no permiten alterar posiciones o efectivos directamente. Cada operación estratégica devuelve un registro nuevo; los encuentros activos contienen el motor táctico mutable que debe avanzar el controlador correspondiente.

## Movimiento y ocupación

TryMove rechaza huestes inexistentes, bloqueadas en combate o destinos ocupados. Busca rutas sin atravesar ninguna otra hueste, aliada o enemiga; utiliza la extensión opcional blocked de MapPathfinder y ArmyMovementState.TryMove. Sin blocked, las llamadas anteriores mantienen su comportamiento. Rechazar una orden no consume puntos ni cambia posiciones. No se ha conectado esta regla a las marchas de varios turnos del HUD actual.

## Encuentros

BeginEncounter exige ID no utilizado, huestes existentes y adyacentes, facciones opuestas y ninguna hueste ya en combate. Crea CampaignEncounter y bloquea a sus participantes. No se inicia simplemente por ocupar una casilla enemiga. El coste estratégico de iniciar batalla y la interfaz de confirmación siguen por definir.

TryGetEncounter permite al futuro adaptador abrir el motor táctico del encuentro correcto. BeginTurn rechaza el avance si hay algún encuentro abierto. Cuando se permite, repone el movimiento de todas las huestes registradas: aún no es un sistema de turnos separado por facción.

ResolveEncounter exige combate terminado, prepara ambos resultados y actualiza los efectivos manteniendo las posiciones estratégicas anteriores. Elimina del mapa a huestes disueltas y libera sus casillas. Conserva retirados vivos con heridas y moral; todavía no calcula una ruta estratégica de retirada ni desplaza automáticamente al vencedor a la casilla vencida. El ID resuelto se registra para que una repetición sea inocua.

## Integración pendiente

El controlador de campaña y el HUD siguen usando ArmyMovementState de una sola hueste. Este registro aún no los sustituye, no añade enemigos visibles y no está incluido en CampaignSaveData. Faltan datos de despliegue estratégico en escenarios, marcadores por hueste, selección y propiedad por facción, apertura/cierre de la interfaz táctica con el encuentro real, guardado del registro y encuentros abiertos, y consecuencias de héroes.

Compilación externa de Core y Presentation correcta, cero errores. Pruebas aplazadas: solapamientos, rutas alrededor de aliados/enemigos, presupuesto, bloqueos durante batalla, duplicado de encuentro, resolución anticipada rechazada, disolución y liberación de casilla, sobrevivientes retirados, resultados de ambos bandos y reposición de movimiento.

## Despliegues configurables desde escenarios

CampaignScenarioDefinition incorpora useArmyDeployments y una lista armies. Cada entrada define ID de hueste, facción, posición axial y puntos por turno. Sus formaciones tienen ID persistente, perfil y cantidad (cero usa la cantidad del catálogo). El despliegue inicial crea formaciones sanas con la moral inicial del perfil. Exige entre una y seis por hueste, IDs de formación únicos en todo el escenario, huestes válidas y posiciones terrestres libres.

TroopCatalog centraliza cuatro perfiles: lanceros, hombres de armas, ballesteros y jinetes ligeros. BattlePrototype utiliza ese catálogo en lugar de duplicar números. Valores de referencia v0.5.0; moral inicial provisional 70. La extracción conserva los números del laboratorio y no cambia por sí sola la versión de sus reglas.

La acción de Inspector pasa a Validate scenario (routes, mission and armies). RegenerateBoard crea y valida el registro antes de ClearGeneratedBoard: un error de despliegue no retira el tablero anterior. Si la construcción termina, publica InitialArmyRegistry; se limpia al retirar el tablero. Los escenarios existentes mantienen useArmyDeployments desactivado y no se modifica el asset CampaignPrototype.

Esta entrega configura y valida datos, todavía no representa huestes múltiples ni cambia el controlador de una hueste. El objetivo de misión sigue tomando como inicio la primera casilla terrestre del mapa; su conexión con la hueste del jugador queda pendiente junto al HUD. Activar los datos no debe interpretarse como cierre de la misión multihueste.

Compilación externa correcta: cero errores, catorce avisos de campos serializados. Pruebas aplazadas: duplicados, huestes vacías, perfiles inválidos, cantidades negativas, agua/fuera de mapa, casillas compartidas y preservación del tablero ante configuración inválida. Pendiente probar el Inspector y la equivalencia del catálogo compartido en Unity.

## Control multihueste desde el HUD

Los escenarios con useArmyDeployments activo incorporan playerFactionId y deben contener al menos una hueste de esa facción. CampaignTurnController recibe el registro validado al construir el tablero. La misión toma como origen la primera hueste propia ordenada por ID; en modo simple conserva el origen anterior. El asset CampaignPrototype existente sigue en modo simple porque aún no se ha diseñado y probado su despliegue.

Seleccionar una casilla ocupada por una hueste propia la hace activa; seleccionar otro destino conserva la hueste activa. SIGUIENTE HUESTE recorre las propias por ID y las localiza con la cámara. El panel muestra ID, cantidad de tropas, posición, movimiento y coste o motivo de rechazo. Solo se pueden mover huestes de la facción del jugador. Las demás bloquean el destino y el paso.

CampaignArmyMarkerLayer crea un marcador provisional por hueste: azul propia y rojo ajena. Usa el relieve de cada casilla, reutiliza marcadores al actualizar el registro y oculta los correspondientes a huestes eliminadas. Regenerar retira los marcadores anteriores antes de crear los del nuevo tablero. No es arte de producción ni añade IA estratégica.

FIN DE TURNO prepara reposición del registro y nuevo estado de turno antes de aplicar ambos. Respeta el bloqueo de encuentros abiertos. La producción sigue siendo provisional para el mapa completo, sin propiedad económica por facción.

Limitaciones explícitas: el modo multihueste todavía no abre encuentros desde la selección ni aplica resultados mediante UI. GUARDAR/CARGAR se deshabilitan en este modo hasta serializar todo el registro, evitando usar el formato antiguo de una sola hueste. Las marchas pendientes también se desactivan: el movimiento multihueste exige presupuesto para el itinerario completo. El modo simple mantiene guardado y marchas anteriores.

Compilación externa correcta, cero errores y catorce avisos de campos serializados. Pruebas aplazadas: selección propia/ajena, cambio de hueste, costes y ocupación, marcadores sobre relieve, reposición, objetivo desde el inicio propio y regeneración repetida entre modo simple y multihueste. Falta crear y validar un asset de escenario multihueste representativo.
