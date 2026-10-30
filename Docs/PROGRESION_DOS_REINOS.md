# Progresión del prototipo: dos facciones

## Alcance acordado

Mantener una facción controlada y un rival; comprobar mapa y situaciones
básicas antes de ampliar a doce campañas. La selección visual de doce reinos
no acredita doce campañas ni sus privilegios. No ampliar el número de rivales
durante la validación del árbol de progresión.

## Resultado de la comprobación

- El escenario PlayableConquest tiene mapa 24 × 16, semilla 12345, Castilla
  con dos huestes de 10 y 20 lanceros y el rival genérico con 50 y 5.
  Es una fixture de situaciones, no un duelo equilibrado: 30 frente a 55.
- Existen economía territorial, obras, reclutamiento, guarniciones, marchas,
  combate, diplomacia, logística, recuperación y persistencia.
- No se ha localizado un sistema C# de edades o tecnología, un ascenso de edad
  persistido, privilegios económicos/militares por reino ni progresión de héroes.
- Los edificios no tienen prerrequisitos de otro edificio ni de edad. No es
  correcto dibujar un cuartel o una herrería como requisito activo para tropas.
- Los atributos documentados de gobernantes no se aplican actualmente como
  bonificaciones a las huestes. La diferenciación diplomática no equivale a
  pasivas militares o económicas.

## Obras activas

Orden de recursos: grano, madera, piedra, oro, mano de obra.

| Obra | Coste inicial | Turnos | Ingreso por turno | Mantenimiento por turno |
|---|---|---:|---|---|
| Mercado | 0 / 30 / 10 / 15 / 10 | 2 | 8 oro | 2 grano |
| Pozo | 0 / 10 / 25 / 5 / 8 | 1 | 6 grano | 1 oro |
| Almacén | 0 / 35 / 15 / 10 / 10 | 2 | 4 madera, 3 piedra | 2 oro |
| Vivienda adicional | 0 / 20 / 10 / 5 / 6 | 1 | 3 mano de obra | 1 grano |

Fortaleza y dos casas iniciales. Seis viviendas adicionales posibles. Una obra
simultánea por ciudad; pago al inicio. Los efectos proceden de obras terminadas,
sin cobrar ingresos retroactivos. El almacén no aplica capacidad máxima.

## Levas activas

| Lote | Cantidad | Coste inicial, mismo orden | Turnos |
|---|---:|---|---:|
| Lanceros concejiles | 20 | 15 / 10 / 0 / 12 / 12 | 2 |
| Hombres de armas a pie | 15 | 15 / 10 / 15 / 25 / 15 | 3 |
| Ballesteros de mesnada | 15 | 10 / 20 / 0 / 20 / 12 | 2 |
| Jinetes ligeros | 10 | 20 / 10 / 0 / 35 / 15 | 3 |

Ciudad propia, tesoro suficiente, una leva por ciudad, sin encuentros abiertos
y plaza disponible. Guarnición, hueste estacionada y leva reservada comparten
seis formaciones. La leva terminada aparece en guarnición. Se puede transferir
a hueste propia en la ciudad o adyacente; para reunir una hueste nueva la
casilla de ciudad debe estar libre. Una conquista cancela la leva enemiga.

## Estado de tropas y efectos reales

- Por formación: ceil(cantidad/20) grano, doble para caballería, y
  ceil(cantidad/25) oro al cierre. La capacidad de pagar se comprueba para
  todo el mantenimiento militar de la facción, no por hueste aislada.
- Suministro: alcance de seis puntos de coste desde ciudades propias;
  posiciones y ciudades enemigas bloquean su red.
- Suministro + paga + reposo: hasta dos puntos de salud total y cinco de
  moral por formación y cierre. La recuperación está limitada y no repone bajas.
- Paga y suministro sin reposo: presupuesto de movimiento completo, sin
  recuperación por reposo. Reposo significa no haber gastado movimiento.
- Sin suministro o sin pagar: diez puntos de moral menos, mínimo cero;
  movimiento siguiente reducido a la mitad, mínimo uno; no cura.
- Moral firme 51–100, vacilante 26–50, quebrada 0–25. Las bajas reducen moral
  con influencia de disciplina. No hay aún multiplicador de daño ni huida
  automática por esos estados.
- Menos efectivos disminuyen el daño total. Defender añade dos de defensa;
  bosque añade dos y escarpado tres al defensor.

## Héroes, fueros y privilegios: separar fuentes

El legacy prueba ascenso del capitán al alcanzar tres victorias, ganando y
sobreviviendo a la última; conserva nombre, arquetipo y mando de su hueste.
Sus órdenes Reagrupar (+15 moral), Grito de guerra (+5) y Mantener línea (+5)
actúan sobre la formación objetivo, con techo 100. No están integradas en Unity.
No se ha definido una tabla Unity de nivel de héroe y porcentaje de mejora.

Los fueros del legacy tampoco son privilegios activos de los doce reinos:
frontera añade una mano de obra y resta un oro; señorío añade un oro y una
piedra y resta una madera, con mínimos cero. Son modificadores de asentamiento.
El diseño de facciones exige pasiva, orden, edificio y tropas exclusivos y
una debilidad, pero todavía no fija sus valores aplicados en Unity.

## Edades: propuesta para validar, no regla existente

Alta, Plena y Baja Edad Media pueden organizar tres etapas del test. Deben
tratarse como propuesta de progresión jugable del modo Leyendas, separada de
la fecha histórica de cada crónica. No asignar costes o bonos ficticios como
si ya estuvieran implementados. Faltan: requisitos por etapa, pago, duración,
desbloqueos, bonos exactos, prevención de acumulación y persistencia.
Aldea, villa y ciudad del legacy son categorías de asentamiento, no edades.

## Secuencia de pruebas y criterio de avance

1. Mapa: rentas por dueño, rutas, visibilidad y bloqueo de destinos.
2. Obras: construir, faltar un recurso, esperar, terminar, mantener y cargar.
3. Tropas: reclutar lote, esperar, transferir/reunir, alcanzar capacidad.
4. Campaña: mover, combatir, conservar bajas, descansar y cortar suministro.
5. Diplomacia: pactar, impedir hostilidad, caducar tratado y cargar propuesta.
6. Edad: una transición completa, coste único y desbloqueo único; repetir
   carga no entrega premios de nuevo. Pendiente de implementación.
7. Héroes y privilegios: efecto condicionado por estado, ascenso único y
   misma regla para jugador y rival. Pendiente de implementación.

No dar por funcional una progresión de edades por haber aprobado el núcleo
actual. La revisión de partida completa de todos estos pasos sigue pendiente.

## Verificación y fuentes

68/68 pruebas dirigidas aprobadas en esta revisión: CityDevelopmentTests,
CampaignMilitaryTests, CampaignArmyLogisticsTests, BattleFormationStateTests
y CampaignDiplomacyTests. Resultado en Logs/progression-audit-tests.xml.
No se ejecutó una suite completa ni se creó otra build: esta entrega audita
y representa reglas, no cambia mecánicas.

Fuentes activas: CityDevelopment.cs, RecruitmentCatalog.cs, TroopCatalog.cs,
CampaignMilitaryState.cs, CampaignArmyLogistics.cs, BattleFormationState.cs,
BattleAttackResolver.cs, CampaignIncomeCalculator.cs y PlayableConquest.asset.
Fuentes de diseño: Docs/diseno/facciones-y-lideres.md y combate-tactico.md.
Referencias legacy: content/buildings.ts, systems/settlementEconomy.ts,
systems/captainPromotion.ts y systems/battleAction.ts. Sus cifras se presentan
como legacy, no como reglas activas de Unity.
