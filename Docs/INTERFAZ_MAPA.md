# Interfaz del mapa de campaña

El HUD ordena la partida de prueba en cinco zonas. Los datos proceden del estado real de la campaña; no se añaden edades, privilegios ni bonificaciones de héroes.

- **Tesoro, arriba:** grano, madera, piedra, oro y mano de obra con existencias y variación prevista al siguiente cierre. El signo acompaña al color de la variación.
- **Territorio y asentamiento, izquierda:** datos de la casilla respetando la niebla, comparación de rutas, control de la ciudad y acceso a su gestión.
- **Hueste activa, derecha:** posición, movimiento restante, orden y suministro. La acción principal conserva el contexto de marchar, atacar, ocupar o volver a una batalla. Localizar y cambiar de hueste quedan juntos.
- **Objetivo y cierre, abajo a la derecha:** turno actual, localización del objetivo y botón principal de finalizar turno.
- **Herramientas, abajo:** guardar, cargar, reiniciar y laboratorio separados de las órdenes; navegación de cámara en el centro.

Los paneles interceptan el puntero para evitar órdenes sobre el terreno situado debajo. Las ventanas de ciudad, diplomacia y pausa conservan la suspensión de interacción con el mapa.

## Revisión reproducible

`CampaignMapHudReview` se activa únicamente con `-fulgorMapHudReview <directorio-de-salida>` en un jugador iniciado desde el menú. Abre la conquista de Castilla sin escribir la ranura de guardado.

Comprueba paneles dentro de pantalla, ausencia de solapamientos, altura disponible para textos, bloqueo del puntero sobre el HUD y centro del mapa libre en 1920 × 1080, 1280 × 720 y 1024 × 768. Después comprueba selección cíclica de huestes, localización, acceso a la ciudad, diplomacia, pausa y reanudación. Guarda capturas y un resumen JSON en el directorio indicado.

Esta revisión comprueba la interfaz en situaciones iniciales y una ciudad propia; no sustituye una partida completa ni certifica todas las combinaciones posibles de órdenes y mensajes.

## Validación de esta revisión

- 14 de 14 pruebas dirigidas aprobadas: mapa, marcha, logística y diplomacia.
- Ejecutable compilado con cero errores. Persisten 25 advertencias de la compilación.
- Revisión del jugador completada: disposición, textos, selección de huestes, ciudad, diplomacia, pausa y conservación de la ranura.
- Capturas a resolución exacta mediante RenderTexture: 1920 × 1080, 1280 × 720 y 1024 × 768. El escalado reproduce la combinación de ancho y alto del CanvasScaler. El bloqueo del puntero también se verifica en la ventana real del jugador.
- Ejecutable y evidencias en `Builds/MapaInterfaz/`; resultados dirigidos en `Logs/map-hud-tests.xml`.
