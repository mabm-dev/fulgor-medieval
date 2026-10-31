# Vista de ciudad — primera versión

Un clic normal en la fortaleza abre una vista de ciudad separada del mapa. La casilla queda seleccionada como contexto de gestión. También se puede seleccionar la fortaleza con **Localizar** y usar **Entrar en la ciudad**. Mayús + clic conserva la previsualización de rutas. Funciona en la campaña básica y en `SettlementCampaignReview.unity`.

## Vista y paneles

La vista contiene la fortaleza existente y una villa original. Al inicio aparecen dos viviendas; mercado, pozo, almacén, plaza y más casas se incorporan al terminar sus [obras de construcción](CIUDAD_CONSTRUCCION.md). Se muestra con una cámara propia en una RenderTexture, separada de las capas del tablero. Los edificios se pueden seleccionar con clic para examinarlos; la cámara mantiene un encuadre fijo durante la selección y la construcción, sin giro ni zoom.

Los paneles son:

- **Ciudad:** presentación general y acceso visual a fortaleza/villa.
- **Edificios:** inspección, selección de proyectos, costes, duración y construcción con los recursos del reino.
- **Hacienda:** reservas e ingresos reales del reino y botón de fin de turno. Los recursos actuales son globales a la campaña; no se presentan como reservas exclusivas de esta ciudad.
- **Huestes:** huestes presentes, formaciones cuando existen en el escenario multihueste y posición/movimiento de la hueste activa. Permite volver al mapa para localizarla.
- **Órdenes:** marcha a la ciudad y cancelación de una marcha pendiente, usando el sistema de movimiento existente.

Las ciudades de otra facción permiten inspección; sus acciones propias de gestión están deshabilitadas. Finalizar turno llama a la campaña real y actualiza reservas, ingresos y movimiento; abrir y cerrar la vista no consume recursos, movimiento ni turnos. La marcha desde ciudad cierra la vista y emite la orden existente hacia la casilla del asentamiento.

**Volver al mapa** o **Esc** cierra la vista. La cámara de campaña conserva su encuadre, zoom y selección; se restauran sus capas de render, fondo y los estados previos de los HUD. Se bloquean selección, navegación y animación visual de huestes del mapa mientras la ciudad está abierta. El clic de cierre no se propaga al tablero. Regenerar el tablero o desactivar su controlador cierra la ciudad y elimina la cámara, textura, luces y modelos temporales. La vista no carga otra campaña ni duplica su estado.

La revisión [Ciudad viva](CIUDAD_VIVA.md) añade perspectiva, zoom limitado, vecinos, animales y detalles de mercado a esta misma pantalla.

## Alcance de la gestión

La [construcción progresiva](CIUDAD_CONSTRUCCION.md) permite una obra simultánea por ciudad, pagada con recursos del reino y completada al avanzar turnos. Se conserva al cerrar la vista y mediante el guardado local de campaña, ahora en versión 3 con compatibilidad con las versiones anteriores. No hay todavía mejoras de nivel, reclutamiento, guarniciones, población simulada ni economía local.

La configuración `CampaignSettlementDefinition.cityPrefab` referencia `Assets/Fulgor/Prefabs/City/PF_CastillaCity.prefab`. La cámara de vista previa usa la capa 30 y su propia luz; la cámara del mapa conserva únicamente la capa de interfaz mientras está abierta.

## Arte y validación

Modelos originales de Fulgor generados en Blender: casa de cal, casa alta, mercado, pozo y elementos de almacén, con materiales propios de cal, piedra, madera, teja e hierro. Se reutiliza el prefab de fortaleza de Castilla previamente aceptado. La plaza usa geometría propia generada en Unity. No se incluyen modelos, texturas ni datos de otros juegos. Los editables y el generador se conservan en la entrega externa; los FBX del repositorio tienen eliminadas las rutas locales internas.

227 pruebas EditMode completas, sin fallos ni omitidas. Siete pruebas nuevas entran en Play Mode y verifican: activación desde la casilla, bloqueo del mapa, restauración de cámara/HUD, ingresos y turno reales, marcha existente, inspección de otra facción, regeneración, enfoque de edificios y entrada mediante botón. Se ha revisado además la pantalla en 1600×1000, 1280×720 y 1024×768; el centro conserva la proporción del modelo y los controles se ajustan al espacio entre paneles. Los textos largos disponen de desplazamiento vertical.

La escena y los modelos son una primera versión visual. Quedan pendientes el acabado de la villa, texturas/desgaste adicionales, mejoras de nivel de edificios, gestión local y perfilado de memoria y rendimiento en una compilación. La vista crea una RenderTexture de 1440×1000 mientras está abierta y la libera al cerrar.

Menú de reconstrucción: `Fulgor > Campaña > Preparar vista de ciudad`. Prepara materiales, prefab de la villa y referencia de la definición de Castilla, sin reconstruir las escenas anteriores. Los informes y capturas quedan fuera del repositorio.
