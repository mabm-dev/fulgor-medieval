# Conquista, propiedad y rentas

Los escenarios multihueste permiten ocupar fortalezas de otras facciones. La propiedad es estado de campaña; no se modifica el asset del escenario.

## Ocupar una fortaleza

- Si está vacía, seleccionar su casilla muestra **OCUPAR FORTALEZA**. La hueste entra mediante las reglas habituales de rutas, ocupación y presupuesto de movimiento y asume el control al llegar.
- Si una hueste propia ya está dentro de una fortaleza ajena, la misma acción permite izar el estandarte sin otro desplazamiento.
- Si está defendida por una hueste rival, se utiliza el ataque adyacente ya implementado. Al ganar y quedar vacía la casilla, el atacante la ocupa como parte del resultado y conquista la fortaleza. No recupera el movimiento gastado en el ataque.
- Una derrota o retirada atacante no cambia la propiedad. Los defensores retirados vivos todavía mantienen su posición estratégica: si siguen ocupando la casilla, bloquean la conquista. No se eliminan supervivientes ni se inventa una ruta de retirada.
- Una batalla pendiente bloquea nuevas ocupaciones. Repetir la aplicación de un resultado no vuelve a mover la hueste, conceder rentas o conquistar.

El control se muestra en el panel del asentamiento y mediante un aro azul para ciudades propias y rojo para ajenas. El aro de selección dorado conserva su función. Las fortalezas propias permiten construir y gestionar; las ajenas se abren en inspección. La ciudad abierta actualiza sus permisos y nombre del propietario al cargar otra situación.

## Qué se conserva

Los edificios terminados y las obras en curso pertenecen a la ciudad y sobreviven a la conquista. Se mantienen el proyecto y su turno de finalización. El nuevo dueño recibe la aportación de sus edificios y asume su mantenimiento. No se cobra otra vez el coste de construcción, se completa una obra anticipadamente ni se concede botín instantáneo.

## Rentas territoriales provisionales

Cada casilla aporta sus rendimientos a la fortaleza más cercana según distancia hexagonal. Un empate se resuelve por ID del asentamiento, en orden ordinal, para que no dependa del orden de los assets. El dueño de la fortaleza recibe las rentas de su distrito. Al conquistarla se transfieren también esas rentas.

Con una sola fortaleza propia se mantiene el ingreso total anterior del mapa. Con varios propietarios, el jugador deja de cobrar las casillas asignadas a fortalezas ajenas. Sin asentamientos configurados se conserva el ingreso global del prototipo. La suma de rentas de todas las facciones conserva el rendimiento total del mapa.

Este reparto no representa fronteras históricas, provincias, conectividad de suministro ni un sistema de influencia. Los distritos económicos aún no se dibujan sobre el mapa. La conquista recalcula las rentas para el próximo cierre de turno; no modifica las reservas existentes ni añade ingresos retroactivos. Hacienda muestra el resultado y los gastos de los edificios propios.

## Guardado

El controlador escribe **v5** tanto en modo simple como multihueste. Incluye el propietario de cada asentamiento y un indicador explícito del modo de ejército. La firma incorpora también los IDs, nombres, posiciones y propietarios iniciales de las fortalezas.

Se valida toda la instantánea antes de publicarla: asentamientos completos y sin duplicados, facciones conocidas, tropas, recursos, edificios y obras. Las rentas se reconstruyen desde la propiedad y las construcciones, sin acumular modificadores.

Los lectores mantienen v1/v2/v3 para modo simple y v4 para multihueste. Como esas versiones no guardaban conquistas, restauran los propietarios iniciales del escenario. Una nueva partida también restablece propietarios, rentas y ciudades iniciales. El modo simple conserva inspección y gestión, pero no incorpora una nueva mecánica de conquista sin registro de huestes.

## Revisión y alcance

`CampaignConquestReview.unity` utiliza dos fortalezas, dos huestes propias y una guarnición rival pequeña para comprobar el recorrido completo. `CampaignConquestReviewBuilder.PrepareAndBuild` prepara y compila la escena. El modo explícito de revisión ejecuta el asalto, comprueba ocupación, permisos, ingresos y restauración JSON v5 sin escribir la ranura del usuario.

Se reutiliza el arte disponible. Quedan pendientes guarniciones separadas de las huestes, asedios, desplazamiento estratégico de retiradas, IA estratégica y condiciones completas de victoria o derrota de campaña.

## Validación

345 pruebas EditMode completas superadas, sin fallos ni omitidas, en una copia aislada del proyecto. Incluyen 23 pruebas nuevas de propiedad, reparto de rentas, ocupación tras victoria, supervivientes que bloquean una casilla, gestión de la ciudad, conservación de obras, restauración sin duplicados, rechazo atómico de propietarios dañados y compatibilidad con formatos anteriores.

La revisión visual manual de la ventana del ejecutable queda pendiente. Los informes y ejecutables de validación se conservan fuera del repositorio.

## Defensa y levas

Una guarnición superviviente impide ocupar la ciudad aunque no haya hueste de campo. Ambas defienden juntas si están presentes. La conquista cancela las levas militares pendientes sin reembolso; las obras civiles se conservan. El escritor actual v6 amplía v5 con [reclutamiento y guarniciones](RECLUTAMIENTO_Y_GUARNICIONES.md).

## Conquista por rivales

Con la [IA estratégica](RIVAL_ESTRATEGICO.md) activada, los rivales también pueden ocupar plazas vacías o conquistarlas después de destruir a sus defensores. Se aplican la misma propiedad, cancelación de levas e ingresos futuros, y el jugador pierde los permisos de gestión de la ciudad.

## Evacuación y final

En el objetivo Conquest v9, los supervivientes derrotados abandonan la plaza si pueden retirarse, o se rinden si no hay salida. Así se libera la ocupación sin borrar heridas ni duplicar tropas. Perder una ciudad puede permitir recuperarse con una hueste evacuada; la campaña termina al cumplirse las condiciones de [victoria o derrota](RETIRADAS_Y_FINAL_DE_CAMPANA.md).
