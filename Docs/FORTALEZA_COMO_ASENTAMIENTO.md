# Fortaleza como asentamiento — primera integración

La fortaleza está integrada en `Assets/Fulgor/Scenes/CampaignPrototype.unity` y en la revisión artística `Assets/Fulgor/Scenes/ArtReview/SettlementCampaignReview.unity`. En esta semilla ocupa la llanura 13,8. La revisión artística reutiliza el castillo existente junto con terreno, agua, LOD y huestes apoyadas en el relieve.

Al pulsar el castillo se abre la [vista de ciudad](VISTA_DE_CIUDAD.md) y se selecciona su casilla. Un contorno señala el asentamiento y el panel muestra nombre, propietario y huestes presentes. **Localizar** selecciona la fortaleza y centra la cámara. **Marchar aquí** envía a la hueste activa usando las rutas, los costes, las reglas de ocupación y la marcha de varios turnos existentes. Solo el botón específico de marcha a un asentamiento propio está habilitado; se pueden inspeccionar propietarios distintos.

La propiedad es estática y pertenece a la configuración del escenario; esta entrega no introduce conquista, diplomacia, restricciones nuevas de acceso a casillas, asedios, reclutamiento, bonificaciones de defensa, ingresos de edificios ni consumo extra de recursos. La presencia de una hueste describe su posición lógica en la casilla, no una guarnición alojada dentro del edificio. La animación visual puede estar aún terminando una marcha ya resuelta por la lógica. Los estandartes provisionales mantienen su representación actual.

## Datos y componentes

- `CampaignSettlement` y `CampaignSettlementRegistry`: identidad, nombre, propietario y coordenada axial; validación de IDs y coordenadas únicos y ubicación terrestre transitable. No dependen de Unity.
- `CampaignSettlementDefinition`: recurso de configuración con los datos anteriores, nombre visible de la facción y prefab. La definición de Castilla está en `Assets/Fulgor/Settlements/FortalezaCastilla.asset`.
- `CampaignSettlementController`: lista de definiciones y facción del jugador en el objeto del tablero. Construye las vistas y su HUD cuando la generación del tablero está preparada, consulta la hueste activa y atiende los botones. Se reconstruye al reiniciar la campaña y limpia los objetos anteriores.
- `CampaignSettlementView`: volumen de selección independiente de los LOD y contorno visual. `CampaignBoardSelection.ResolveHit` traduce el clic sobre la fortaleza a la misma casilla lógica; el HUD bloquea clics hacia el mapa y se oculta junto con la interfaz de campaña en el laboratorio táctico.

El prefab y sus texturas son los propios de Castilla ya aceptados provisionalmente; no se incorporan modelos nuevos ni recursos de otros juegos. La colocación actual se ha preparado en una llanura sin oro. Otros edificios o terrenos necesitan su propia preparación de suelo, prefab y heráldica. Las superficies para colocación deben ser legibles por CPU y cumplir el contrato de `TerrainGroundSurface`.

La configuración estática se vuelve a leer al iniciar o regenerar el tablero. Guardar y cargar conserva el sistema de campaña existente: no se ha añadido estado mutable de propiedad al formato de guardado. Una futura conquista o gestión de guarnición necesitará datos y persistencia propios.

## Validación

220 pruebas EditMode completas, sin fallos ni omitidas. Se añaden 13 casos de identidad, unicidad y colocación y tres pruebas de integración que entran en Play Mode: seleccionar y marchar hasta el castillo, mostrar la presencia, mantener la propiedad, bloquear clics bajo el HUD, inspeccionar otra facción sin usar el botón de marcha aliada y regenerar ambos tableros sin duplicar fortalezas.

Las dos escenas se han abierto además en Play Mode para revisar propietario, contorno, clic sobre el modelo y distribución de los paneles. Las capturas y los logs quedan en la entrega externa.

La campaña básica regenera el asentamiento y su vista. La escena artística conserva su limitación anterior: después de reiniciar el tablero, el castillo se reutiliza y se vincula a la casilla nueva, pero el resto de capas artísticas aún requiere reiniciar Play para reaplicarse. El perfilado de rendimiento en una compilación y la integración general del terreno artístico siguen pendientes.

Menú de preparación: `Fulgor > Campaña > Preparar fortaleza como asentamiento`. Reconstruye la definición de ubicación/prefab y los enlaces de las dos escenas para el escenario de Castilla actual. Para otros escenarios se configuran sus propias definiciones en el controlador.
