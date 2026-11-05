# Interfaz del mapa general de campaña

La propuesta aprobada se lleva al mapa 3D existente, sin sustituirlo por una captura. La cabecera de Castilla, los marcos y las texturas proceden del arte disponible; los retratos de tropas proceden de las referencias individuales de Castilla. El recorte y los marcos se componen como UI nativa, con cifras y etiquetas independientes.

## Distribución y acciones

- Cabecera del reino a la izquierda; reservas y balance por turno arriba. El menú abre la pausa existente con guardar, cargar y selección de ranuras.
- Barra inferior: identificación de hueste, efectivos, moral ponderada por efectivos, movimiento restante y hasta seis formaciones. Las plazas vacías no inventan tropas. Pulsar una formación muestra sus atributos y conserva el ribete interior de selección.
- Los comandos mantienen las reglas existentes: mover/atacar según el destino, localizar, cambiar de hueste, cancelar marcha y finalizar turno.
- Navegación a ciudades, diplomacia y objetivos. La lista de ciudades propias permite abrir su gestión y admite desplazamiento cuando crece.
- Avisos de obras pendientes, huestes con movimiento sin orden de marcha y propuestas diplomáticas recibidas. Los contadores proceden del estado actual.
- Rótulos de ciudades anclados a su posición real, visibles solo cuando la observación del jugador lo permite. La selección de un rótulo localiza la ciudad; la gestión se abre desde su panel o la lista de ciudades.
- El HUD de campaña se oculta al entrar en la gestión de ciudad y reaparece al volver. Las tropas conservan su visibilidad propia de cada modo.

## Alcance

La imagen conceptual no constituye un cambio de reglas. No se añaden órdenes de defender/esperar, un árbol de investigación, niveles de héroe ni una época simulada: esos sistemas aún no forman parte de esta implementación. Se mantienen las acciones disponibles y los datos reales. El retrato de mando es ilustrativo, no introduce un héroe nuevo en el guardado.

La nueva presentación se aplica a campañas jugables con `CampaignPlaySession`. Las escenas de prototipos mantienen sus controles de revisión. El escalado de la interfaz de campaña toma como referencia el ancho de 1920; la geometría de los paneles evita superponer recursos, menú, barra de hueste y cierre de turno.

## Verificación

La prueba de integración `CampaignHudUsesLiveRosterAndCityNavigation` comprueba composición real, cambio de hueste, consulta de atributos, separación de paneles, entrada/salida de ciudad y apertura de diplomacia. Se conserva la prueba de construir, cancelar, guardar y completar sobre casillas reales.
