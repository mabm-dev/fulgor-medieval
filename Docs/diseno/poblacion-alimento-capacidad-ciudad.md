# Población, alimento y capacidad de ciudad

Propuesta inicial para diseñar y equilibrar, no reglas implementadas ni cifras definitivas. Su objetivo es que una ciudad crezca según sus viviendas y su acceso real a recursos, sin construir una casa por familia. Se debe validar con el mapa de prueba, varios territorios y ambas facciones antes de elegir números.

## Estado del prototipo

La población aún no se simula. Las seis viviendas adicionales posibles aportan mano de obra y consumen grano; el pozo aporta grano, el mercado consume grano y los recursos del reino son globales. Las rentas territoriales actuales asignan cada casilla a la fortaleza más próxima sin un radio máximo: una ciudad sin otras fortalezas recibe los rendimientos del mapa completo. Cada llanura aporta actualmente una unidad de grano por turno en `CampaignIncomeCalculator`.

Ya existe un cálculo de mantenimiento militar en `CampaignArmyLogistics`: una unidad de grano por cada 20 soldados por formación, redondeando hacia arriba, y el doble para caballería. La aplicación en un escenario concreto depende de su configuración. Las fórmulas propuestas aquí deben integrarse con ese cálculo sin contabilizar la misma tropa dos veces.

Estas reglas actuales no sirven por sí solas para inferir la capacidad local de alimento o una población máxima real. Sustituirlas o conciliarlas requiere una decisión explícita y compatibilidad de guardados: no sumar a las granjas nuevas ingresos de llanura antiguos por duplicado ni conservar el grano gratuito del pozo si sus funciones cambian sin reajustar la economía.

## Territorio y explotación

1. Decisión de diseño: la ciudad comienza con su casilla central y el primer anillo hexagonal de seis casillas contiguas, sujeto a los límites del mapa y a resolver los casos de casillas ya ocupadas o asignadas. Las casillas posteriores no se conceden automáticamente por distancia: la ciudad se gana oportunidades de expansión mediante méritos. Se debe decidir cómo se seleccionan, pagan y resuelven los conflictos de esas casillas.
2. Contar solo casillas controladas, explotables y accesibles. Si dos ciudades comparten radio, una casilla se asigna como máximo a una; proponer reasignación controlada por el jugador. Deben definirse control, accesibilidad, bloqueo y límites del mapa.
3. Decisión de diseño: cada casilla incorporada aporta un efecto pasivo mínimo según su terreno o recurso; al trabajarla mediante un edificio aporta además un efecto activo. Pasivo y activo son componentes distintos y se suman una sola vez. Sus valores, alcance y condiciones aún están pendientes. La renta territorial actual deberá adaptarse a esta regla para no duplicar los ingresos.
4. Granja, pesca o ganadería se benefician de terreno adecuado; los ríos o mares necesitan datos de mapa y reglas reales. La disponibilidad de agua, fertilidad y comercio está por implementar o verificar por escenario; no se presupone que todo mapa tenga un río.
5. Las explotaciones requieren trabajadores. Estos forman parte de la población, no son población adicional. Minas, granjas, leñeras, defensa y oficios compiten por trabajadores disponibles. Todavía no se ha fijado qué porcentaje de población puede trabajar, ni cómo se asigna.

## Dos límites y reservas

- **Límite de viviendas:** capacidad del centro urbano más la de barrios y sus mejoras.
- **Población sostenible por alimento:** producción neta efectiva disponible para civiles por turno, después de mantenimiento alimentario de edificios y ejércitos, multiplicada por los habitantes que alimenta cada unidad. Redondear de forma coherente con el consumo por grupos.
- **Objetivo sostenible local:** el menor de ambos límites. Es una previsión, no un borrado inmediato de habitantes cuando cae la producción. Si hay recursos distintos al alimento esenciales para el crecimiento o la salud, estos pueden añadir condiciones o límites tras diseñarlos.
- **Reservas:** permiten soportar un déficit durante un tiempo y no son producción recurrente. La autonomía se calcula como reservas utilizables dividido por déficit por turno, cuando este sea positivo.
- **Intercambio entre ciudades/comercio:** debe contar como ingreso recurrente solo el suministro confirmado y repetible. Hay que elegir cómo se reparte el grano global entre ciudades y ejércitos y verificar capacidad de rutas; el mismo envío no se puede asignar a dos destinos. Un envío puntual alimenta reservas, no incrementa el máximo sostenible permanente.

La población actual puede superar el objetivo sostenible por pérdida de una granja o una ruta. Propuesta: consumir reservas; si falta comida, detener crecimiento y aplicar consecuencias graduales por definir. Informar antes de aplicar cualquier castigo. Huelgas, migración o mortalidad no están decididas.

## Expansión territorial por méritos

Después del anillo inicial, propuestas para valorar: una ciudad gana una oportunidad de expansión al cumplir méritos relacionados con población y abastecimiento, investigación, comercio o relaciones, o seguridad del territorio. El jugador elige una casilla adicional contigua al territorio de esa ciudad; el territorio controlado por otras ciudades o facciones no se transfiere automáticamente. La conquista, las cesiones diplomáticas y la reasignación entre ciudades requieren sus propias reglas. Cada oportunidad debe gastarse una sola vez.

Esos tipos de mérito, los umbrales, los costes, las condiciones de casillas de agua, la conectividad y el funcionamiento de la IA están pendientes de aprobación. Evitar una única condición que obligue a comerciar o conquistar para crecer. El mapa debe distinguir claramente una casilla en propiedad/asignada, una explotable y una ocupada por una construcción: al incorporar la casilla se obtiene su pasivo mínimo; su efecto activo requiere trabajarla.

La ciudad puede aumentar su techo de población sostenible mediante el pasivo alimentario de una nueva casilla o mediante su explotación activa cuando exista mano de obra y la producción llegue realmente a la ciudad. El crecimiento territorial no concede viviendas. Recalcular las previsiones cuando cambien la propiedad, el acceso, una explotación, una ruta, los trabajadores o el consumo.

### Ejemplo de pasivo y activo

Con valores ilustrativos pendientes de equilibrio: una llanura incorporada aporta **+1 alimento/turno pasivo**; construir y trabajar una granja añade **+3 activos**, para un total de **4**. Si una condición de río apta añade +1 a la granja, el total sería 5. La existencia y disponibilidad de río, los costes y los trabajadores se decidirán en las reglas definitivas. Si se pierde la construcción pero se conserva la casilla, en este ejemplo se pierde el activo y el pasivo permanece; qué ocurre en ocupaciones, asedios o bloqueos debe definirse expresamente.

## Ejemplo numérico hipotético

No son cifras del mapa actual ni estadísticas verificadas. Se usan para mostrar cómo encajan territorio, granjas, ejército y viviendas.

- La ciudad ya ha obtenido y asignado mediante méritos suficientes casillas adicionales para contar con 18 exteriores utilizables; no se conceden automáticamente por radio. Seis se dedican a granjas y el resto a otros terrenos o usos.
- Centro urbano: +4 de alimento/turno. Seis llanuras explotadas: +1 pasivo y +3 activos cada una. Tres de las seis granjas tienen una bonificación de +1 por agua. Las otras doce casillas del ejemplo no aportan alimento. Producción total **4 + 6×1 + 6×3 + 3×1 = 31**.
- Personal: hipótesis de 10 trabajadores por granja; 60 de los habitantes trabajan en las seis granjas. Hace falta que la población trabajadora disponible alcance 60. En una ciudad de 200 habitantes, los otros 140 son el resto de población; esto no significa que los 140 puedan trabajar. El número de trabajadores disponibles se definirá aparte.
- Consumo de ejemplo: 1 alimento por cada 10 habitantes, redondeado hacia arriba; mantenimiento de edificios 2 y ejército asignado 3 por turno, sin añadir un segundo consumo militar en otra parte del balance.
- Vivienda disponible: 200 habitantes. Alimento para población: 31 − 2 − 3 = 26; soporta **260 habitantes** según la regla de ejemplo. Máximo sostenible con esa vivienda: **mínimo(200, 260) = 200**.
- A 200 habitantes, consumo civil 20, edificios 2 y ejército 3 = 25 por turno; superávit 6. Por tanto la reserva crece 6 por turno, si no hay otros ingresos o gastos.
- Si se pierde el control de una casilla con granja de río y también deja de producir su explotación, la producción cae 5: pasa a 26. Con 200 habitantes queda 1 de superávit. Si se pierde el control de dos de esas casillas, cae 10: producción 21, gasto 25 y déficit 4 por turno. Una reserva utilizable de 20 duraría 5 turnos sin cambios adicionales. Si solo se pierde una granja pero se conserva su casilla, caería 4 y permanecería el pasivo de 1.

Si se amplía la vivienda a 300, el techo sostenible alimentario del ejemplo sigue en 260; construir más barrios no crea alimento. Si el territorio dispone de más tierra y trabajadores, se puede aumentar la producción; si no, hace falta un suministro exterior sostenible u otra estrategia. Las cifras y la escala de habitantes, unidades de alimento y duración de turnos deben ajustarse conjuntamente.

## Validación antes de aprobar valores

- Identificar el mapa y posiciones reales de las dos ciudades y medir el primer anillo y posibles expansiones por terreno para varias semillas y configuraciones sin agua.
- Probar inicio, una ciudad desarrollada y una ciudad máxima en terreno favorable, medio y desfavorable; contar casillas disponibles para agricultura, viviendas, industria y defensa.
- Simular decenas de turnos por ciudad y en conjunto: mano de obra elegible, producción, mantenimiento civil y militar, intercambios, crecimiento, almacenes y saldo.
- Repetir con una explotación ocupada, asedio, pérdida o recuperación de una ruta, comercio interrumpido, dos ciudades compitiendo por casillas y una ciudad alimentando a otra. Controlar que las sumas y las transferencias no dupliquen recursos.
- Establecer objetivos medibles de turnos para crecer, porcentaje de población que puede trabajar, reserva deseable y tamaño de huestes. Escoger los valores después de la simulación y las pruebas de juego, no antes.
- Mostrar en Hacienda: población actual, viviendas, alimento producido, consumo civil, consumo militar, otros gastos, importaciones confirmadas, saldo y autonomía de reservas; dar por separado capacidad teórica y sostenible.
