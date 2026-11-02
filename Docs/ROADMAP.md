# Roadmap

La numeración continúa la del prototipo web: `v0.1` a `v0.5` se publicaron en
la web y `v0.6` es la primera versión en Unity
([ADR-003](decisiones/ADR-003-migracion-unity.md)). Cada versión Unity agrupa
hitos del [plan de migración](migracion-unity/04-plan.md).

## Prototipo web (cerrado)

| Versión | Contenido | Referencia |
|---|---|---|
| v0.1 | Identidad y entrada: menú, selección de reino y guardado mínimo | — |
| v0.2 | Mapa hexagonal navegable de 24 × 16 con semilla reproducible | tag `v0.2.0` |
| v0.3 | Primer turno completo: cinco recursos, producción y consumo | tag `v0.3.0` |
| v0.4 | Reino y frontera: asentamientos, construcción, suministro y niebla | tag `v0.4.0` |
| v0.5 | Combate táctico: campo de 13 × 9, formaciones, héroes y resolución automática | tag `v0.5.0` |

La IA rival, la diplomacia, el mapa peninsular y la vista 3D se desarrollaron
después en la web sin versión etiquetada. Están en la rama `legacy/web`, que es
un archivo histórico. Unity es la única línea activa; los requisitos actuales
viven en este roadmap y en los documentos de diseño.

## v0.6 — Base Unity

Hitos 0 y 1 del plan de migración.

- [x] Proyecto Unity 6000.6 con URP para Windows.
- [x] Núcleo C# sin `UnityEngine`: coordenadas, aleatoriedad y generador de
      mapas.
- [x] Paridad del generador con el prototipo web.
- [x] Tablero de 24 × 16 con cámara, selección y panel de casilla.
- [x] Contrato visual Blender–Unity e importador de modelos estáticos.
- [x] Pruebas Edit Mode de núcleo, presentación, importación y escena.
- [x] Integración continua de las pruebas.
- [x] Revisión de la escena en el editor con gráficos.
- [x] Medición de una build de Windows.

## v0.7 — Mapa de campaña y primera campaña jugable (cerrada)

Hitos 2 y 3. La versión se cerró al quedar jugable la conquista con Castilla,
que adelanta buena parte de v0.8 y v0.9; lo pendiente de este bloque pasa a
las versiones siguientes.

- [x] Arte de terreno de detalle integrado en el mapa (biomas, agua, caminos
      y cultivos).
- [x] Kit de terreno con variantes de agua: costa, bahía, cabo, desembocadura
      y río.
- [x] Costa, caminos y riberas como datos lógicos de rutas, no solo
      decorativos.
- [ ] Mapa peninsular diseñado para Unity, con el mapa anterior como
      antecedente.
- [x] Escenarios configurables sin duplicar recursos al regenerar el tablero.
- [ ] Rendimiento del tablero completo, según la
      [investigación de rendimiento](v0.7-rendimiento-tablero.md). Hecho: color
      propio solo en hover y selección y modo benchmark; falta combinar mallas.

## v0.8 — Misión rápida jugable

Hitos 4 y 5. Implementa las reglas de misión definidas para Unity.

- [x] Nueva partida y selección de reino (doce reinos; solo Castilla jugable).
- [x] Turno con recursos, producción, consumo y previsión al cierre.
- [x] Movimiento, rutas, marchas de varios turnos y suministro.
- [x] Combate táctico con formaciones, moral y resolución automática.
- [ ] Héroes en combate y en el mapa.
- [x] Ocupación de asentamientos, victoria y derrota.
- [x] Guardado y carga versionados.
- [x] Fortaleza castellana como primer modelo completo.

## v0.9 — Campaña regional

Hitos 6 y 7. Implementa asentamientos, IA rival y diplomacia en Unity.

- [ ] Huestes y héroes con modelo, identidad y animación. Hecho: batallón
      de nueve soldados y tropas con modelo; faltan héroes y animación.
- [ ] Asentamientos, población, construcción y fueros. Hecho: conquista,
      construcción y reclutamiento; faltan población y fueros.
- [x] Niebla de guerra.
- [x] Reino rival con IA estratégica y economía propia.
- [ ] Diplomacia, rescate de héroes cautivos y ascenso de capitanes. Hecho:
      paz, pactos, comercio y tratados; el resto depende de los héroes.
- [ ] Campañas jugables para los otros once reinos.

## v0.10 — Tutorial

- Campaña guiada de 12 a 15 turnos.
- Ayuda contextual, reinicio y reanudación.

## v0.11 — Vertical slice

Hito 8.

- Partida completa de 20 a 30 turnos.
- Escenario mayor con presupuesto de CPU, GPU y memoria medido.
- Equilibrio, accesibilidad, rendimiento y presentación.

## v1.0 — Distribución

Hito 9.

- Build reproducible para Windows.
- Licencias de recursos y herramientas auditadas.
- Pruebas en varios equipos.

## Criterio de avance

Una versión no termina al crear su pantalla. Termina cuando se puede recorrer
su caso de uso en el editor y en una build de Windows, tiene reglas probadas,
guarda correctamente, pasa la integración continua y se ha revisado a ojo.
