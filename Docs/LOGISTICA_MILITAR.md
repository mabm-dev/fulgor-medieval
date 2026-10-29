# Mantenimiento, suministro y recuperación militar

La conquista jugable activa la logística militar. Es una opción del escenario;
la misión de exploración y los escenarios anteriores mantienen su configuración.
La misma regla se aplica al jugador y a los rivales estratégicos activos.

## Cierre del turno

1. Se reciben ingresos y se abona mantenimiento civil.
2. Se cobra el mantenimiento de las huestes y guarniciones que ya existían.
3. Se comprueban suministro y reposo, se aplican recuperación o penalizaciones y
   se repone el movimiento del siguiente turno.
4. Terminan las levas y obras que correspondan; continúan las marchas y actúa el rival.

Una leva pendiente no consume mantenimiento militar. Las tropas recién terminadas
entran en el cálculo del siguiente cierre completo. Cambiar una formación de hueste
a guarnición no elimina ni duplica su mantenimiento.

El cierre se bloquea con un combate abierto. Reanudar una fase rival pausada por una
batalla no vuelve a ingresar rentas, cobrar mantenimiento ni curar tropas.

## Costes provisionales

Por cada formación se redondea hacia arriba:

| Recurso | Consumo por turno |
|---|---|
| Grano | 1 por cada 20 soldados supervivientes |
| Grano de caballería | El doble del consumo anterior, por los caballos |
| Oro | 1 por cada 25 soldados supervivientes |

Ejemplos: 50 lanceros consumen 3 de grano y 2 de oro; 25 jinetes consumen 4 de grano
y 1 de oro. No se cobran madera, piedra ni mano de obra. Las bajas reducen la siguiente
previsión. Las reservas del rival pagan sus propias tropas.

Se abona lo disponible sin deuda. Si un reino no cubre todo su mantenimiento militar
en grano y oro, sus unidades tienen pago incompleto ese cierre. No se favorece a una
hueste por el orden de su identificador.

## Abastecimiento

Una formación de campo recibe suministro si existe una ruta terrestre de hasta
**6 puntos de movimiento** desde alguna ciudad propia. Los caminos explícitos y el
coste del terreno cuentan. El agua, las ciudades ajenas y las casillas ocupadas por
huestes enemigas cortan el paso; perder o conquistar una ciudad cambia la red.
No se bloquean adicionalmente las casillas adyacentes al enemigo.

La guarnición se abastece en su ciudad mientras ésta no esté ocupada por un enemigo.
Un reino sin ciudades no tiene fuentes de suministro. Se sigue pagando a las unidades
aisladas: disponer de reservas y disponer de una ruta son condiciones distintas.

## Reposo y recuperación

Una hueste descansa cuando no ha gastado movimiento durante el turno que se cierra.
Los ataques iniciados y los traslados de formaciones consumen movimiento y evitan
ese reposo. Las guarniciones estacionadas pueden recuperarse al cierre.

Con pago completo, suministro y reposo, cada formación recupera hasta **2 puntos de
salud agregada** y **5 de moral**, con los límites de su plantilla y de los soldados
que siguen vivos. No reaparecen soldados muertos, no se incorporan reclutas gratis
ni se borran los historiales de batalla. Una formación de moral superior a su valor
normal no pierde ese exceso por descansar.

Sin suministro o con pago incompleto, pierde **10 de moral** (mínimo cero), no se cura
y comienza el siguiente turno con la mitad de su movimiento normal, redondeada hacia
abajo y con mínimo uno. La penalización se calcula sobre su presupuesto normal en cada
cierre; no se vuelve a dividir la mitad anterior. No provoca muertes ni deserciones.

## Decisiones logísticas del rival

Antes de reclutar, el rival exige conservar grano y oro para dos cierres de mantenimiento
militar después de pagar la leva. La previsión incluye huestes, guarniciones, levas
pendientes y la nueva formación. Es una reserva conservadora: no anticipa ingresos futuros.

Una hueste que ha cobrado y tiene moral inferior a 40 o heridas recuperables busca una
ciudad propia. Allí descansa hasta superar ese umbral y curar las heridas de sus
supervivientes. Las bajas definitivas y la salud superior a la plantilla no se consideran
heridas pendientes. El movimiento y la curación siguen las reglas ordinarias.

El refugio se descarta si hay una hueste o guarnición enemiga visible a un hexágono o
menos. Entre los refugios accesibles se elige el de menor coste de ruta, con desempate
estable por identificador. Si no existe refugio, se mantienen las decisiones estratégicas
ordinarias. Con niebla se usan rutas conocidas y amenazas observadas; no se consulta la
posición de tropas ocultas para elegir el refugio.

La ruta de recuperación evita entrar en casillas ocupadas por enemigos observados o
adyacentes a ellos. Se permite salir de una casilla inicial amenazada hacia una libre.
Después de cada paso se actualiza la observación y se recalcula la ruta: una amenaza
recién descubierta puede obligar a rodear o detenerse. Se gasta el movimiento real y
no se inicia un ataque automático durante esta marcha. Funciona con y sin niebla.

No se evalúan todavía escoltas, emboscadas ni amenazas a distancia. Tampoco implica que toda hueste aislada se retire si conserva
buena moral y no tiene heridas.

## Información y persistencia

Hacienda incluye el coste militar en la previsión total y lo desglosa del mantenimiento
civil. El texto de la hueste indica suministro y coste; Huestes explica las reglas.
El resumen del cierre rival añade incidencias propias y puntos de salud recuperados.

Los escenarios con logística y sin niebla escriben **v10**. La conquista con niebla
escribe **v11**; ambos incluyen la firma de las reglas logísticas.
Salud, moral, reservas y movimiento reducido se guardan en sus campos existentes;
el suministro se calcula a partir del mapa y propietarios actuales. Cargar no avanza
turnos, cobra ni recupera salud. Los informes del último cierre son temporales.

Los guardados **v9** compatibles pueden continuar en la conquista nueva. Conservan
exactamente sus tropas, reservas, turnos y movimiento; la logística comienza en el
próximo cierre y el siguiente guardado usa v10, o v11 si tiene niebla. No se cobran turnos anteriores.
Una versión de reglas desconocida, una firma distinta o un modo incompatible se
rechaza antes de reemplazar la partida. Los escenarios sin logística ni niebla conservan v9.

No se incluyen todavía fatiga persistente, transporte de víveres, saqueo, hospitales,
refuerzos automáticos, hambre civil ni un equilibrio definitivo de estos valores.
