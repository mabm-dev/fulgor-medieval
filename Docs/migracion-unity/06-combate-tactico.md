# Base de turnos del combate táctico en Unity

**Estado actual:** el [combate en campaña](../COMBATE_EN_CAMPANA.md) ya conecta el mapa, las tropas reales y la aplicación del resultado. La batería local completa supera 322 pruebas. Las notas siguientes conservan las etapas históricas de implementación; sus referencias a pruebas aplazadas o integración pendiente no describen el estado actual de esas conexiones.

Implementación inicial: Assets/Fulgor/Core/BattleTurnOrder.cs. Reglas de referencia: Docs/diseno/combate-tactico.md y Docs/casos-de-uso/CU-06-resolver-combate.md.

## Implementado

- Entre una y seis formaciones por bando, con ID único y posición de despliegue no compartida.
- Orden determinista: atacante, defensor; infantería, distancia, caballería; fila visual superior, columna izquierda e ID como desempate. La fila visual 0 representa la parte superior: el adaptador del futuro tablero debe convertir sus coordenadas a esta convención.
- Una activación por formación y ronda. Esperar la aplaza al final de su propio bando una sola vez por ronda; si no quedan otras formaciones de ese bando, vuelve a estar activa inmediatamente y ya no puede volver a esperar.
- Defender finaliza la activación y da +2 hasta comenzar la siguiente activación de esa formación. CompleteActivation será llamado por el motor después de resolver una orden legal de movimiento o ataque.
- RemoveFromBattle retira una formación destruida o retirada: deja de actuar, no vuelve a aparecer en rondas posteriores y deja de ocupar su posición lógica de despliegue. No calcula bajas ni desencadena decisiones automáticas de retirada.
- Al agotarse las formaciones de un bando, se fija el resultado y se bloquean todas las órdenes posteriores.

## Pendiente

La cola es una pieza del motor, todavía no una batalla jugable ni una resolución automática completa. Faltan estado y estadísticas de las formaciones, ocupación del tablero táctico tras movimientos, validación de órdenes, daño y bajas, moral, héroes y mando, decisiones del rival y aplicación del resultado a la campaña. La misma cola debe utilizarse en manual y automático.

No se ha introducido una fórmula alternativa de victoria por fuerza agregada. El resultado de campaña, cautiverio, heridas y tropas supervivientes se implementará mediante eventos de combate según el documento de diseño.

## Verificación pendiente

Compilación externa de Core y Presentation correcta, cero errores. Pruebas de comportamiento aplazadas por indicación del usuario. Al retomarlas: orden independiente del orden de entrada, espera por ronda, espera siendo el último del bando, duración de defensa al cambiar de ronda, retirada/destrucción activa y pendiente, eliminación del último integrante de un bando y bloqueo de órdenes después del resultado.

## Estado de formaciones, salud y bajas

FormationAttributes recoge ataque, defensa, daño mínimo/máximo por integrante, salud por integrante, movimiento, iniciativa, alcance, moral y disciplina. Son datos validados e inmutables: no se fija todavía un catálogo ni se decide una fórmula numérica de ataque. Los rasgos y las órdenes del héroe siguen pendientes.

BattleFormationState conserva salud acumulada, cantidad superviviente, bajas y salud del integrante herido. El daño resuelto se satura a cero salud, conserva heridas entre golpes y nunca crea bajas negativas. Los estados anteriores permanecen intactos. Withdraw conserva tropas y salud, pero las excluye del combate. No hay curación automática ni retirada por moral.

BattleRoster mantiene las formaciones y su cola de turnos sincronizadas. TryApplyResolvedDamage aplica una cantidad ya calculada por el futuro resolutor de ataques; elimina de la cola a una formación destruida y fija el resultado al quedarse un bando sin fuerzas. No es una orden de ataque: no valida alcance, actor ni terreno y no consume activación. Los controladores futuros deben usar el resolutor de órdenes, no llamar a esta operación como sustituto del ataque.

TryWithdrawArmy aplica la retirada de todo un bando de forma atómica, sin activar formaciones ni avanzar rondas intermedias. Conserva supervivientes y heridas en el roster para el futuro resultado estratégico. Tras cerrar el combate se bloquean más cambios por daño y órdenes. El cautiverio, heridas de héroes y aplicación del resultado al mapa siguen pendientes.

Compilación externa correcta, cero errores. Pruebas aplazadas: daño menor a la salud por integrante, golpes sucesivos, daño exactamente letal, daño superior a toda la salud, cantidades grandes, objetivo retirado/destruido, retirada de la hueste activa y bloqueo tras victoria. Estas piezas todavía no se serializan en el guardado de campaña ni tienen interfaz táctica.

## Resolutor inicial de ataques

BattleAttackResolver conecta BattleRoster, posiciones axiales y terrenos tácticos. Valida turno activo, batalla abierta, objetivo vivo y no retirado, bando contrario y alcance hexagonal. El despliegue inicial exige una posición por formación viva, casillas existentes y sin ocupación compartida. Copia los datos de posición y terreno para que los diccionarios del llamador no alteren la batalla después de crearla.

Referencia histórica consultada: tag v0.5.0, commit 62709bdb503a6d35a1920b643ab1653ce9e526d7, src/game/systems/battleAttack.ts y battlefieldTerrain.ts. El tag v0.6.0 ya corresponde a la migración y no contiene ese archivo. Se recupera únicamente la regla como referencia: el código Unity es C# en Core.

Fórmula: impacto por integrante = máximo entre cero y ataque + tirada inclusiva de daño - defensa - defensa del terreno - bonus de defender. Daño = impacto por cantidad viva atacante. Terrenos tácticos separados de la campaña: despejado +0, arbolado +2, escarpado +3. El +2 de defender sigue la duración de BattleTurnOrder. No hay contraataque implícito, línea de visión, cobertura adicional ni moral todavía.

Diferencia deliberada respecto al cálculo antiguo de bajas: se conserva salud acumulada y heridas, por lo que un daño positivo pequeño puede herir sin matar. No se promete paridad completa con la batalla web ni se aplica su mínimo de una baja por impacto. Los multiplicadores y reglas avanzadas del héroe siguen pendientes.

El azar Mulberry32 usa una única semilla por resolutor. Una orden rechazada no avanza su estado ni la cola. Un ataque válido consume la activación, incluso con daño cero. Una destrucción cierra inmediatamente la batalla si ya no quedan enemigos. BattleAttackResult expone tirada, bonus de terreno y defensa, daño, bajas, estado de azar y resultado. No está integrado en el guardado de campaña.

Compilación externa correcta (cero errores). Pruebas aplazadas por indicación del usuario. Pendientes: mismos comandos/semilla producen mismos resultados, ataques rechazados no alteran azar ni activación, límites de alcance, protección del terreno y defender, daño cero, muerte del último enemigo y persistencia de heridas. Las posiciones son las del despliegue; falta implementar órdenes de movimiento táctico y actualizar ocupación durante la batalla antes de construir la interfaz.

## Movimiento táctico y ocupación

BattleMovementCommands amplía el mismo BattleAttackResolver: movimiento y ataques comparten posiciones, terreno y roster. El campo del prototipo está limitado a coordenadas axiales Q=0..12, R=0..8. Las casillas ausentes no son transitables. Referencia histórica: v0.5.0, src/game/systems/battleMovement.ts.

TryPreviewMove calcula una ruta de coste mínimo con Dijkstra y desempates deterministas. Incluye ambos extremos y cobra la entrada a las casillas: despejado 1, arbolado 2 y escarpado 2. Valida formación activa, batalla abierta, destino existente, diferente del origen y libre. No atraviesa formaciones vivas de ningún bando. Si el coste supera el movimiento, devuelve rechazo con la ruta para que la futura interfaz pueda explicarlo. Previsualizar no consume activación ni azar.

TryMove aplica únicamente una orden válida y consume toda la activación, igual que la regla histórica. No permite mover y atacar en esa misma activación ni recorrer parcialmente un destino demasiado lejano. Un rechazo conserva posición y turno. Las posiciones de formaciones destruidas o retiradas no bloquean rutas ni se devuelven como ocupantes vivos. Los ataques posteriores utilizan las posiciones actualizadas.

Tras mover se actualiza el orden visual que usará la próxima ronda (R creciente de arriba abajo y Q creciente de izquierda a derecha). La cola pendiente de la ronda actual permanece estable para evitar activaciones duplicadas o saltadas. El Slot expuesto por BattleFormationState conserva el despliegue original; la posición actual debe consultarse mediante TryGetPosition, y el orden actual mediante el roster.

Compilación externa correcta, cero errores. Pruebas aplazadas. Pendientes: ruta alrededor de aliados/enemigos, destino ocupado, coste de terreno, presupuesto exacto/insuficiente, rechazo sin alterar turno o azar, casilla liberada tras destrucción, ataque después de un desplazamiento y orden visual de la ronda siguiente. No hay zonas de control, ataques de oportunidad ni línea de visión todavía. La interfaz y el enlace con encuentros de campaña siguen pendientes.

## Control automático básico

BattleAutoController recibe el resolutor común, no una segunda copia del roster. ChooseOrder consulta el estado y las previsualizaciones sin consumir azar ni activaciones. TryStep ejecuta exactamente una decisión mediante TryAttack, TryMove o TryDefend. Devuelve ronda, orden, ruta y resultado de ataque para el futuro parte de batalla.

Política provisional: enemigos vivos ordenados por distancia hexagonal e ID; atacar al primero dentro del alcance. Si no hay objetivo en alcance, buscar por coste real una casilla libre desde la que se pueda atacar a un enemigo y avanzar por su ruta hasta donde alcance el movimiento. El coste y la ocupación son los del mismo motor táctico. Si no puede avanzar, defender. Los empates de destino usan R y Q ascendentes. No usa decisiones aleatorias, retirada, héroes ni un cálculo alternativo de bajas.

Se consultó battleAi.ts de v0.5.0 como referencia, pero esta política no reproduce toda aquella IA: no incorpora la separación de arqueros amenazados, órdenes heroicas ni apoyo a moral. Es un controlador inicial para conectar después la fase defensora y el modo automático, no una IA estratégica ni una batalla ya integrada en campaña.

Run permite una ejecución acotada entre 1 y 10 000 activaciones (1000 por defecto). Si no hay vencedor al terminar el límite, conserva Outcome=InProgress y devuelve LimitReached; no impone victoria, empate ni cambios estratégicos ficticios. Una batalla estancada, bloqueada o con daño cero puede alcanzar el límite. La presentación deberá preferir TryStep entre fotogramas para mantener respuesta y pausas legibles; aún no se ha medido el coste de la búsqueda de decisiones.

Compilación externa correcta, cero errores. Pruebas aplazadas: decisiones repetibles e independientes del orden de diccionarios, elección sin alterar estado/azar, rodeos, alcance, bando activo, detención inmediata al finalizar, límite de activaciones y equivalencia de una orden automática con su ejecución manual por el mismo resolutor.

## Laboratorio táctico accesible desde campaña

Nuevo botón LAB. COMBATE en el panel de campaña. BattleLabController abre una interfaz textual provisional IMGUI, sin modelos ni renders nuevos. Suspende la entrada de cámara/selección y oculta el HUD de campaña hasta cerrar. El estado estratégico se conserva y la batalla no lo modifica.

BattlePrototype crea un campo axial 13×9 con semilla 12345: tirada por casilla, 60 % despejado, 25 % arbolado y 15 % escarpado, según la referencia histórica. Despliega cuatro formaciones por bando en columnas 1 y 11. Atributos y cantidades recuperados del catálogo v0.5.0: lanceros, hombres de armas, ballesteros y jinetes ligeros; moral de laboratorio fijada en 70 y aún sin efecto. Rasgos, héroes y balance no están incorporados. Es una fixture independiente, no una hueste real de la campaña.

Controles: atacar al ID indicado, mover a Q/R, esperar, defender y retirar el bando activo. Permite un paso automático o ejecutar ambos bandos con pausa entre activaciones; puede pausarse, reiniciarse o volver a campaña. Muestra ronda, formación activa, bando, resultado, tropas, salud, posición y defensa, junto a las seis últimas entradas del parte. El límite de 1000 activaciones automáticas conserva una batalla sin resolver, sin inventar vencedor.

Cerrar conserva la batalla en memoria y pausa la ejecución; reiniciar crea la misma fixture. No hay tablero gráfico, selección con clic sobre casillas ni persistencia del laboratorio entre sesiones. Los resultados no se aplican a recursos, hueste, objetivo ni guardado de campaña.

Verificado únicamente por compilación externa: cero errores y siete avisos de campos serializados. Se adaptó el ID de ventana a GetEntityId de la versión Unity instalada. Pruebas de interfaz y comportamiento aplazadas por indicación del usuario. Pendientes: abrir/cerrar sin entradas de campaña, campos numéricos, victoria durante una orden, retirada, automático/pausa/límite, reinicio, tamaños de pantalla y regreso al HUD.

## Tablero esquemático del laboratorio

La interfaz del laboratorio incorpora 117 botones de casilla dispuestos como cuadrícula axial desplazada, sin arte ni modelos nuevos. Cada casilla muestra Q,R y terreno (D/A/E) u ocupante. Los bandos se distinguen en azul/rojo, la formación activa en amarillo y el destino seleccionado mediante [1]. Seleccionar una casilla actualiza Q/R y, si está ocupada, el ID de objetivo; no ejecuta órdenes. Las entradas numéricas siguen disponibles.

La previsualización usa TryPreviewMove del núcleo y muestra pasos, coste y motivo de rechazo en español. Resalta las casillas de ruta y distingue un itinerario existente pero demasiado caro de un destino ocupado o sin ruta. Cambiar de formación, ejecutar una orden o avanzar automático recalcula la consulta a partir de posiciones actuales. Las casillas pueden consultarse al terminar la batalla, aunque las órdenes permanecen bloqueadas.

El listado de fuerzas se coloca a la derecha y el parte debajo del tablero. La ventana pasa a 1220×820 unidades con escala ajustada al tamaño de pantalla. Es una interfaz de desarrollo con botones rectangulares, no un tablero táctico con geometría hexagonal de producción. Compilación externa correcta; selección, tipografía, escalado y actualización tras órdenes pendientes de prueba en Unity.

## Guardar y reanudar el laboratorio táctico

BattleLabSession registra únicamente órdenes aceptadas (atacar, mover, esperar, defender y retirar bando). Las entradas se copian para impedir que el llamador modifique el registro posteriormente. La vista y las decisiones automáticas usan TryExecute; las previsualizaciones continúan sin alterar estado ni registro.

GUARDAR BATALLA y CARGAR BATALLA utilizan una ranura PlayerPrefs distinta: Fulgor.BattleLab.Save.v1. El payload incluye versión, identificador de reglas tactical-prototype-1, semilla y secuencia de órdenes. No almacena gráficos ni referencias Unity. Se reconstruye una fixture nueva y se repiten sus órdenes para recuperar ronda, posiciones, bajas, heridas, defensa, espera y azar. Un error de versión, semilla u orden rechaza toda la carga sin sustituir la batalla actual. La ejecución automática queda pausada después de cargar.

Límites: 2000 órdenes por registro, 1 000 000 de caracteres antes de deserializar en la vista y 1000 activaciones automáticas por sesión de laboratorio. Tras cargar, el contador automático se fija conservadoramente al total de órdenes recuperadas. El formato solo cubre la fixture de BattlePrototype; no reanuda encuentros reales de campaña ni versiones futuras con reglas distintas. Cambiar reglas o contenido exige cambiar el identificador de reglas del formato.

Las seis líneas recientes del parte no se guardan: se conserva el registro mecánico de órdenes, y la vista informa del número recuperado. Cerrar o reiniciar el laboratorio no borra la ranura persistida. El guardado de campaña es independiente.

Compilación externa correcta, cero errores y siete avisos de campos serializados. Pruebas aplazadas: ida y vuelta tras espera/defensa, daños parciales, cambios de ronda, movimiento, victoria y retirada; mismas órdenes y semilla recuperan el mismo azar; orden inválida o exceso de longitud no sustituyen estado; PlayerPrefs entre sesiones y controles con automático pausado.

## Moral por bajas

BattleFormationState conserva moral actual separada de los atributos iniciales. Se recupera la regla de battleMorale.ts en v0.5.0: pérdida = techo((bajas × 100 / cantidad antes del golpe) × (1,5 − disciplina / 200)), limitada a la moral disponible. El cálculo usa aritmética entera equivalente para evitar diferencias de redondeo. Moral y disciplina se validan entre 0 y 100. Los golpes que hieren sin causar bajas no reducen moral.

Estados: firme si moral >50; vacilante entre 26 y 50; quebrada entre 0 y 25. Una formación quebrada sigue en batalla y puede recibir órdenes: la retirada continúa siendo explícita para toda la hueste. No se añaden penalizaciones de ataque, bloqueo de activaciones ni decisiones de huida por moral. El apoyo de héroes queda pendiente.

BattleAttackResult incluye moral anterior y posterior del objetivo. El parte del laboratorio muestra el cambio después del ataque; la lista de tropas muestra moral, estado y salud del integrante herido. Una retirada conserva la moral actual. La interfaz separa la información en dos líneas por formación para facilitar su lectura.

El identificador de reglas del guardado táctico pasa a tactical-prototype-2. Los guardados anteriores del laboratorio se rechazan por incompatibilidad; la ranura y los datos no se borran automáticamente. El guardado de campaña no cambia. Un nuevo registro de órdenes reconstruye también moral mediante los mismos ataques.

Compilación externa correcta, cero errores. Pruebas aplazadas: límites 25/26 y 50/51, golpe sin bajas, pérdida acumulada, disciplina 0/100, eliminación total, retiro explícito con moral quebrada y guardar/cargar conservando moral. La semilla y la fórmula de daño no cambian por esta incorporación.

## Parte final de batalla

BattleReport crea una instantánea inmutable únicamente si la batalla terminó. Conserva resultado, ronda y estados de las formaciones ordenados por ID. BattleSideReport suma efectivos iniciales, bajas, supervivientes, salud restante y formaciones en campo/retiradas/destruidas. Los retirados vivos cuentan como supervivientes, no como bajas. Los estados de formación son inmutables y conservan heridas y moral. No determina el destino de héroes ni modifica la campaña.

El laboratorio muestra el parte final al cerrar el combate, permite alternar con VER FUERZAS y añade GUARDAR PARTE TXT. El texto incluye semilla, número de órdenes, estado del azar, resumen de ambos bandos y estado de cada formación. La exportación se realiza solo al pulsar el botón, en Application.persistentDataPath/BattleReports, con nombre único; informa de la ruta en el registro y consola. No sustituye el guardado táctico ni permite reanudar desde el TXT.

Una batalla pendiente por límite de activaciones no admite parte final ni exportación. Al cargar una batalla ya terminada, el parte se obtiene nuevamente desde el estado reconstruido. Reiniciar el laboratorio no borra los informes previamente exportados.

Compilación externa correcta, cero errores. Pruebas aplazadas: efectivos iniciales = bajas + supervivientes, retiradas sin muertes ficticias, salud y moral finales, instantánea estable, rechazo de parte en combate abierto, exportación TXT y visualización tras cargar un combate cerrado. La aplicación estratégica del resultado sigue pendiente.

## Base de reconciliación con huestes persistentes

CampaignFormationState conserva identidad persistente, atributos, salud y moral. CampaignArmyState agrupa hasta seis formaciones y registra qué encuentros ya se aplicaron. Una hueste sin formaciones supervivientes queda disuelta. Los IDs y facciones deben proporcionarlos los futuros encuentros estratégicos, no se generan a partir del laboratorio.

ApplyBattleReport recibe un parte final, bando, ID de encuentro y correspondencia explícita entre IDs tácticos y persistentes. Exige cobertura exacta del bando, IDs únicos, cantidades iniciales coincidentes y salud por integrante compatible; impide aumentos de salud o moral durante esta primera reconciliación. Conserva supervivientes y heridas, incluidas formaciones retiradas, y elimina solo las destruidas. El estado anterior permanece intacto. Aplicar nuevamente un ID registrado devuelve el mismo estado.

Deploy/FromSurvivors permiten iniciar otra batalla desde la cantidad viva y salud restante, manteniendo moral y sin curación. El nuevo combate toma como cantidad inicial sus efectivos actuales y limpia el indicador de retirada táctica. No se arrastran coordenadas ni la cola del combate anterior.

CampaignEncounterResult.Reconcile prepara los resultados de ambos bandos antes de devolverlos; el llamador debe sustituir ambos estados conjuntamente. Si una validación falla no se modifica ninguno de los originales. Rechaza huestes iguales, facciones iguales e historial donde solo uno de los bandos consta como aplicado. Si ambos ya lo aplicaron, devuelve AlreadyApplied sin repetir consecuencias.

Alcance: infraestructura del núcleo todavía sin registro de huestes en el controlador de campaña, disparador de encuentros ni serialización de esos nuevos estados/historiales. El laboratorio continúa independiente y no tiene botón de aplicar su fixture a la campaña. La muerte/cautiverio del comandante, heridas de héroes, eventos estratégicos, botín y retirada en el mapa siguen pendientes. La restricción de no aumentar moral deberá revisarse al incorporar apoyo heroico.

Compilación externa correcta, cero errores. Pruebas aplazadas: conservación de heridas y moral entre batallas, retirados vivos, hueste disuelta, correspondencias erróneas, repetición de un encuentro, fallo del segundo bando sin alterar el primero e historiales inconsistentes.

## Encuentro táctico construido desde dos huestes

CampaignEncounter recibe ID único, semilla y dos CampaignArmyState de facciones distintas, ambas con supervivientes. Rechaza huestes disueltas, identidades repetidas y un encuentro ya aplicado. Ordena las formaciones por ID persistente, asigna IDs tácticos locales y conserva internamente su correspondencia para ambos bandos. Despliega hasta seis formaciones por lado sin solapamientos, columnas Q=1 y Q=11 y filas distribuidas entre R=1 y R=7.

CampaignFormationState incorpora el tipo de tropa persistente (infantería, distancia o caballería). Deploy exige conservarlo y FromSurvivors mantiene salud parcial y moral actuales, sin curación. La reconciliación también conserva ese tipo.

TacticalBattlefield extrae el generador compartido 13×9 que antes estaba dentro de BattlePrototype. Mantiene orden de tiradas, umbrales y estado de azar posterior; laboratorio y encuentros utilizan la misma generación. La extracción no cambia por sí misma las reglas ni el identificador de guardado del laboratorio; su equivalencia queda pendiente de prueba.

El encuentro expone roster y resolutor para el futuro adaptador de interfaz. Resolve requiere una batalla terminada y las dos huestes actuales. Si no consta ya aplicado, compara IDs, facciones, formaciones, atributos, salud, moral e historial con las instantáneas iniciales y rechaza cambios concurrentes. Solo después prepara ambos resultados mediante CampaignEncounterResult. La repetición de un encuentro aplicado por ambos bandos es idempotente.

Alcance actual: servicio del núcleo. Todavía no existe registro estratégico de huestes, disparador de contacto sobre mapa ni apertura de este encuentro desde el HUD. El laboratorio permanece como fixture separada. Tampoco se guarda un encuentro real abierto ni se aplican consecuencias a héroes o comandantes.

Compilación externa correcta, cero errores. Pruebas aplazadas: despliegue con 1..6 formaciones, orden independiente de entrada, conservación de heridas/moral/tipo, campo idéntico al laboratorio, rechazo de huestes alteradas y resolución conjunta repetida sin duplicar consecuencias.
