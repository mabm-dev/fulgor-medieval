# Inicio y sesiones de campaña

La compilación jugable abre `FulgorStart`. Desde el inicio se puede elegir **Conquista de Castilla**, **Misión de exploración** o **Continuar** un guardado compatible. No es necesario buscar una escena de revisión concreta.

La conquista reutiliza el escenario con rivales, ciudades, reclutamiento, retiradas y condiciones de victoria/derrota. La misión conserva el modo de una hueste y el objetivo anterior de llegar a una casilla. El catálogo referencia sus escenarios y ciudades; no copia reglas de combate ni cambia los datos iniciales.

## Guardar y continuar

Se mantiene la ranura local existente. La conquista con [niebla y exploración](NIEBLA_Y_EXPLORACION.md) escribe v11 y conserva las reglas de [logística militar](LOGISTICA_MILITAR.md); los escenarios con logística pero sin niebla usan v10; la misión y los escenarios sin ambas opciones conservan v9. Los guardados v9/v10 compatibles de conquista se continúan sin repetir turnos ni cobrar gastos anteriores. Empezar una campaña nueva no escribe en esa ranura. **Guardar** y **Guardar y volver al inicio** sí la sustituyen. El inicio lo explica antes de elegir.

Continuar reconstruye y valida el guardado contra los escenarios disponibles antes de cambiar de escena: mapa y reglas, propietarios, tropas, heridas, moral, levas, guarniciones, reservas rivales, obras y marchas. Un archivo dañado, de otra configuración o de una versión no admitida no habilita el botón. Si varias entradas coincidieran, el catálogo rechaza la selección ambigua.

Los guardados anteriores de la misión que cumplan sus reglas siguen admitidos. No se inventa una conversión a conquista. Los guardados v9 de la escena de conquista previa también coinciden porque el cambio de nombre de escena no altera la identidad lógica del escenario.

Tras validar se transporta una copia del contenido a la escena destino, sin volver a leer ni modificar la ranura. Así, un cambio externo de la ranura durante la transición no hace cargar otra partida. La carga conserva turno, posición y movimiento y no ejecuta otro cierre.

`CaptureCampaignSnapshot` permite obtener datos sin escribir. `TrySaveCampaign` y `TryLoadCampaignSnapshot` devuelven el resultado de la operación; los botones anteriores siguen funcionando. El menú sólo abandona la partida después de un guardado correcto si se eligió guardar y volver.

## Inicio y pausa desde el mapa

El botón **Inicio / pausa** abre tres acciones:

- **Seguir jugando:** vuelve a la misma partida.
- **Guardar y volver al inicio:** guarda y cambia de escena sólo si la operación y su validación tienen éxito.
- **Volver sin guardar:** descarta los avances de la sesión actual y conserva la ranura anterior.

El panel advierte de esa diferencia. Mientras está abierto se bloquean selección, cámara y animación de marcha del mapa. Escape permite abrir o cerrar la pausa; respeta los modales de ciudad y batalla para no abrir dos vistas con la misma pulsación.

No se abandona una batalla o fase rival pendiente desde este menú, porque todavía no se serializa un encuentro táctico abierto. El acceso se oculta en las vistas de ciudad y batalla. Una partida terminada sí permite volver al inicio y continuar con sus órdenes bloqueadas.

## Escenas y compilación

Las tres escenas jugables están en `Scenes/Playable`: inicio, conquista y misión. Se colocan primero en la lista de compilación, con el inicio como entrada. Las escenas antiguas se conservan y quedan desactivadas en esa lista, disponibles para revisión desde el editor.

El catálogo se guarda como un asset del proyecto. Sus identidades son estables y sus nombres de escena no contienen rutas de esta máquina. La preparación y compilación pueden regenerar los assets jugables sin editar las escenas originales.

## Verificación y límites

Las pruebas cubren selección, compatibilidad de formatos anteriores, datos dañados, ambigüedad, pausa, guardado, retorno sin guardar, continuación de marchas y partidas terminadas. El recorrido opcional del ejecutable abre ambos modos y restaura sus instantáneas sin escribir la ranura del jugador.

Sigue habiendo una sola ranura, sin selección de archivos ni guardados automáticos. Los escenarios son fijos; todavía no se ofrecen semillas, dificultad o configuración de reinos. La revisión visual manual del menú y del flujo completo sigue pendiente.
