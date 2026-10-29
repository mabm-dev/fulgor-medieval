# Interfaz de campaña inspirada en el legacy

El inicio reutiliza la portada y el marco ornamental originales, con la fuente Cinzel
convertida a TrueType para Unity. Los recursos están en `Resources/LegacyUI`.

El menú ofrece empezar, continuar, configuración, créditos y salir. Empezar abre
la elección de las campañas actualmente disponibles. No añade todavía selección
libre de reino, dificultad ni semilla. Continuar mantiene la validación de la ranura.
Los modales bloquean las opciones de fondo y permiten volver con Escape o el botón
Volver. Salir requiere confirmar dentro del menú.

Configuración permite ajustar el volumen general y alternar pantalla completa.
El volumen tiene una preferencia independiente del guardado de campaña. No se
incluyen todavía selección de resolución, calidad gráfica ni reasignación de teclas.

La campaña dispone de una cabecera con turno, recursos y previsión de grano y oro;
la pausa está a la derecha. Los paneles de selección y asentamiento permanecen a la
izquierda y las órdenes de hueste a la derecha. La cabecera bloquea la selección
del mapa que queda debajo. Se conservan los controles y estados del juego, incluida
la información limitada por niebla. Los paneles se ocultan al abrir ciudad o batalla
mediante las reglas ya existentes.

Esta entrega renueva el inicio y el marco de la campaña. Los interiores de gestión,
la selección ilustrada de reinos y la configuración avanzada quedan para siguientes
entregas. El botón de laboratorio táctico sigue disponible en esta versión de desarrollo.

La revisión opcional `-fulgorLegacyReview` genera capturas fuera del proyecto. Cuando
la sesión remota devuelve capturas de pantalla negras, se utiliza un render a textura
del propio motor; esta captura cambia temporalmente el modo de canvas y lo restaura.
Sirve para comprobar composición, no sustituye la revisión manual de todos los efectos
de presentación en cada equipo.
