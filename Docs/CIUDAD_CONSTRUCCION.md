# Ciudad: construcción y crecimiento visual

Al empezar una partida la vista de Castilla contiene la fortaleza y dos viviendas. El mercado, la plaza empedrada, el pozo, el almacén y otras seis viviendas permanecen ocultos hasta completar sus obras. Los árboles y el terreno son parte del entorno inicial.

En **Edificios**, **Otra obra** cambia el proyecto seleccionado. El panel muestra coste y duración; **Construir** descuenta los recursos globales del reino y pone una obra en marcha. Solo hay una obra simultánea por ciudad. Si faltan recursos, se pueden reunir mediante los turnos de **Hacienda**. Durante la obra aparecen cimientos, madera, materiales apilados y andamios en la huella del futuro edificio. En las obras de dos turnos la segunda fase levanta paredes y cerchas; el pozo usa un anillo de piedra y un soporte de madera. Al terminar se retira toda la estructura temporal y aparece el edificio terminado. El panel muestra los turnos restantes y permite avanzar el turno. Los turnos reales de campaña completan las obras también con la vista cerrada y en escenarios multihueste. No transcurren turnos por dejar la pantalla abierta.

| Obra | Madera | Piedra | Oro | Mano de obra | Turnos |
|---|---:|---:|---:|---:|---:|
| Mercado | 30 | 10 | 15 | 10 | 2 |
| Pozo | 10 | 25 | 5 | 8 | 1 |
| Almacén | 35 | 15 | 10 | 10 | 2 |
| Cada vivienda adicional | 20 | 10 | 5 | 6 | 1 |

Los costes, duraciones, ingresos y mantenimiento se definen en `CityProjects`. Los edificios terminados aportan recursos al reino a partir del cierre siguiente a su finalización y pagan mantenimiento. Consulta [Gestión y economía de la ciudad](GESTION_Y_ECONOMIA_CIUDAD.md) para los valores y la previsión de Hacienda. Se mantienen las rentas del mapa; todavía no hay capacidad máxima de almacenamiento, población simulada, cancelación de obras, reclutamiento ni mejoras de nivel.

El mercado terminado muestra el edificio, la plaza, dos puestos con mercancías y sus vendedores. El almacén muestra también sus cajas y el pequeño cercado. La actividad inicial es de cuatro vecinos, una gallina y un perro; las viviendas adicionales aumentan los paseantes, el mercado añade vendedores y gallinas y el almacén incorpora las ovejas. Con toda la villa construida hay catorce personas y ocho animales. Son ambientación visual, sin población simulada. La fase de obra se reconstruye desde la orden y el turno guardados; no necesita otro formato de guardado. La estructura se combina en tres grupos de materiales, sin colliders de piezas. El encuadre se recalcula al cambiar los edificios o las obras visibles y conserva el giro y el zoom solicitado.

## Estado y guardado

`CityDevelopment` mantiene edificios terminados y una orden con turno de finalización. `CampaignTurnController` conserva un estado por asentamiento; la vista temporal solo lo representa. Abrir y cerrar no borra la obra. Una partida nueva vuelve a fortaleza y dos casas. La inspección de una ciudad ajena no puede construir ni gastar recursos.

El guardado local usa v5 en ambos modos e incluye propiedad, edificios y obras. Se leen v1/v2/v3 en modo simple y v4 en multihueste; esas versiones restauran los propietarios iniciales. Se validan propietarios, edificios, duplicados, identidad y turnos antes de sustituir la partida actual. Consulta [Guardado multihueste](GUARDADO_MULTIHUESTE.md) y [Conquista](CONQUISTA_Y_PROPIEDAD.md). Las batallas pendientes bloquean construcción y cierre de turno hasta resolverlas.

El prefab completo es una plantilla de disposición: `CityGrowthPresentation` activa sus edificios y detalles según el estado de campaña. Los identificadores de viviendas de esta plantilla proceden de sus etiquetas `Vivienda 1` a `Vivienda 8`; esas etiquetas deben conservarse al reconstruirla. Los menús anteriores de revisión artística construyen explícitamente la villa completa para comprobar su aspecto final.

## Validación

275 pruebas EditMode completas, sin fallos ni omitidas. Se comprueban costes, falta de cada recurso, obra única, plazos, ausencia de duplicados, estado inicial, guardado compatible, datos dañados, apertura y cierre, nueva partida, inspección, crecimiento de la actividad, persistencia de las fases de obra y reemplazo por edificios terminados. Se incluyen los efectos económicos, mantenimiento, previsiones y navegación de Hacienda descritos en la documentación de gestión. Todos los proyectos se comprueban durante su construcción y con giros de cámara. La revisión visual captura el inicio, la obra pendiente, el mercado terminado y la villa ampliada; comprueba doce ángulos de cámara con todos los edificios construidos. Informes y capturas se guardan fuera del repositorio.
