# Agua conectada — revisión

Superficies y shader originales de Fulgor. Escena: `Assets/Fulgor/Scenes/ArtReview/WaterConnectionsReview.unity`, basada en la revisión de terreno y fortaleza aceptada provisionalmente. La campaña y las revisiones anteriores se conservan.

El campo de orillas se calcula en espacio de mundo a partir de las casillas de agua y sus vecinos. Las dos caras de una unión comparten altura, normal y color. Los cauces estrechos admiten curvas y ramales; se ensanchan al entrar en cuerpos de agua. El contorno se integra dentro de la casilla acuática y termina a altura 0,12 junto al terreno; el agua queda a 0,04.

Un material URP con color por vértice, luz y sombras recibidas, ondas suaves de normal en espacio de mundo y una franja discreta de espuma. La geometría y sus colliders permanecen estáticos; el movimiento es visual. No utiliza texturas ni modelos de otros juegos.

La clasificación distingue masa interior, masa que toca el límite y cauces estrechos por topología. Es una aproximación visual; no simula caudal, pendientes, dirección de corriente, inundaciones ni navegación. La casilla lógica de agua sigue teniendo las mismas reglas y coste de movimiento, aunque visualmente incluya riberas.

Los modelos preparados del kit anterior se conservan como referencia. Esta superficie cubre combinaciones de vecinos que requieren más variantes que los siete módulos originales.

Revisión visual pendiente de medición en compilación. Las capturas y los informes se guardan en la entrega externa. No se añaden rutas locales ni datos de infraestructura.

## Abrir la revisión

- `WaterConnectionsReview.unity`: tablero de campaña con agua conectada; al entrar en Play enfoca una masa de agua.
- `WaterConnectionCases.unity`: galería independiente de seis ejemplos; entrar en Play permite acercar y desplazar la cámara.

La semilla de la revisión tiene 36 casillas de agua: 24 de mar y 12 de masa interior; no contiene cauces estrechos con esta clasificación. Los tramos de río, curva, ramales y desembocadura se demuestran en la galería sin alterar la máscara lógica del mapa.

Se han validado 70 pruebas: las 64 combinaciones de vecinos, apertura de curvas y ramales, clasificación de masas de agua, clasificación anterior y humo de campaña. Las mallas vecinas comparten también las subdivisiones de borde. Ambos escenarios se han abierto y comprobado en Play Mode.

La superficie refina las riberas y los bordes con subdivisión conforme. En esta semilla utiliza 238.540 triángulos de agua; el tablero revisado queda en 1.737.272 triángulos de terreno. Estos recuentos no son una medición de FPS ni de rendimiento en compilación.
