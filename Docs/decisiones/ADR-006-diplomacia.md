# ADR-006: diplomacia y persistencia compatible

Estado: aceptado para el prototipo.

## Decisión

Las reglas de diplomacia viven en el núcleo de campaña, separadas del consejo
visual. Las relaciones simétricas entre el jugador y cada reino del escenario
controlan ataques y ocupación; el controlador aplica el mismo permiso a las
acciones del jugador y de la IA. Se conservan estados e intenciones del legacy.
Las propuestas pendientes se resuelven en orden al cierre del turno y validan
los dos tesoros antes de transferir recursos.

El guardado añade `diplomacyVersion = 1` y un bloque con esquema propio.
El indicador explícito es necesario porque JsonUtility puede reconstruir un
objeto serializable vacío a partir de una referencia nula. La ausencia del
indicador conserva las reglas hostiles de las partidas anteriores. Tanto el
catálogo de continuación como la carga activa validan el nuevo bloque antes
de publicar el estado restaurado. No cambian las firmas de escenario.

## Consecuencias

Los guardados anteriores continúan siendo legibles en el nuevo ejecutable.
No se garantiza conservar la diplomacia si se vuelve a guardar desde una
versión anterior. Los futuros acuerdos sobre héroes cautivos requieren antes
un modelo de captura y liberación de héroes. El alcance y las pruebas se
detallan en [Diplomacia de campaña](../migracion-unity/10-diplomacia.md).
